using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Playnite.SDK;
using Playnite.SDK.Data;

namespace PlayniteGo
{
    public class HltbManager
    {
        private readonly ILogger logger = LogManager.GetLogger();
        
        // Static caches to persist data across multiple exports
        private static Dictionary<string, HltbData> _exactMatchCache;
        private static Dictionary<string, HltbData> _normalizedCache;
        private static HashSet<Guid> _localHltbAvailableIds;
        private static DateTime _csvLastWriteTime = DateTime.MinValue;
        private static DateTime _localDirLastWriteTime = DateTime.MinValue;
        private static readonly object _lock = new object();

        private const string HltbExtensionId = "e08cd51f-9c9a-4ee3-a094-fde03b55492f";

        public string ExtensionsDataPath { get; set; }

        public class HltbData
        {
            public int MainStory { get; set; }
            public int MainExtra { get; set; }
            public int Completionist { get; set; }
        }

        // --- Models for Local Extension JSON ---
        public class HltbUserGameData
        {
            public List<HltbItem> Items { get; set; }
        }

        public class HltbItem
        {
            public HltbPluginData GameHltbData { get; set; }
        }

        public class HltbPluginData
        {
            public long MainStoryMedian { get; set; }
            public long MainExtraMedian { get; set; }
            public long CompletionistMedian { get; set; }
        }

        public void LoadDatabase(string csvPath)
        {
            lock (_lock)
            {
                if (!File.Exists(csvPath))
                {
                    logger.Error($"[PlayniteGo] HltbManager: CRITICAL ERROR - File does not exist at path: {csvPath}");
                    return;
                }

                // Check if CSV has been modified
                var currentCsvTime = File.GetLastWriteTimeUtc(csvPath);
                bool csvChanged = currentCsvTime != _csvLastWriteTime;

                // --- OPTIMIZATION: Pre-scan local extension directory ---
                // We pass true if CSV changed to force local cache refresh just in case, 
                // though they are technically independent.
                InitializeLocalCache(csvChanged);

                if (_exactMatchCache != null && _exactMatchCache.Count > 0 && !csvChanged)
                {
                    logger.Info("[PlayniteGo] HltbManager: Database already loaded and file unchanged. Skipping reload.");
                    return;
                }

                logger.Info($"[PlayniteGo] HltbManager: Loading CSV (Changed: {csvChanged}). Path: '{csvPath}'");

                // Reset caches
                _exactMatchCache = new Dictionary<string, HltbData>(StringComparer.OrdinalIgnoreCase);
                _normalizedCache = new Dictionary<string, HltbData>();
                _csvLastWriteTime = currentCsvTime;

                try
                {
                    int rowCount = 0;
                    using (var reader = new StreamReader(csvPath))
                    {
                        // 1. Read Header
                        var headerLine = reader.ReadLine();
                        if (string.IsNullOrEmpty(headerLine))
                        {
                            logger.Error("[PlayniteGo] HltbManager: CSV file is empty.");
                            return;
                        }

                        // FIX: Trim whitespace AND quotes from headers
                        var headers = ParseCsvLine(headerLine).Select(h => h.Trim().Trim('"').ToLowerInvariant()).ToList();
                        logger.Info($"[PlayniteGo] HltbManager: Headers found: {string.Join(", ", headers)}");

                        // 2. Identify Columns
                        int nameIdx = headers.IndexOf("name");
                        int mainIdx = headers.IndexOf("main_story");
                        int extraIdx = headers.IndexOf("main_plus_sides");
                        int compIdx = headers.IndexOf("completionist");

                        if (nameIdx == -1 || mainIdx == -1)
                        {
                            logger.Error($"[PlayniteGo] HltbManager: Missing columns! nameIdx={nameIdx}, mainIdx={mainIdx}");
                            return;
                        }

                        // 3. Parse Rows
                        while (!reader.EndOfStream)
                        {
                            var line = reader.ReadLine();
                            if (string.IsNullOrWhiteSpace(line)) continue;

                            var parts = ParseCsvLine(line);

                            if (parts.Count <= mainIdx) continue;

                            string name = parts[nameIdx].Trim('"');
                            // FIX: Trim quotes from number strings before parsing
                            int main = ParseHours(parts[mainIdx].Trim('"'));
                            int extra = (extraIdx != -1 && parts.Count > extraIdx) ? ParseHours(parts[extraIdx].Trim('"')) : 0;
                            int completeVal = (compIdx != -1 && parts.Count > compIdx) ? ParseHours(parts[compIdx].Trim('"')) : 0;

                            var data = new HltbData
                            {
                                MainStory = main,
                                MainExtra = extra,
                                Completionist = completeVal
                            };

                            if (!_exactMatchCache.ContainsKey(name)) _exactMatchCache[name] = data;

                            string normKey = NormalizeGameName(name);
                            if (!string.IsNullOrEmpty(normKey) && !_normalizedCache.ContainsKey(normKey))
                                _normalizedCache[normKey] = data;

                            rowCount++;
                        }
                    }
                    logger.Info($"[PlayniteGo] HltbManager: Successfully loaded {rowCount} rows. Cache size: {_exactMatchCache.Count}");
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "[PlayniteGo] HltbManager: Exception while loading CSV.");
                    // Invalidate timestamp on error so we try again next time
                    _csvLastWriteTime = DateTime.MinValue;
                }
            }
        }

        // --- OPTIMIZATION: Manual CSV Parser (Faster than Regex) ---
        private List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            int start = 0;

            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (line[i] == ',' && !inQuotes)
                {
                    result.Add(line.Substring(start, i - start));
                    start = i + 1;
                }
            }
            result.Add(line.Substring(start));
            return result;
        }

        private void InitializeLocalCache(bool forceRefresh = false)
        {
            if (string.IsNullOrEmpty(ExtensionsDataPath)) return;
            
            string dirPath = Path.Combine(ExtensionsDataPath, HltbExtensionId, "HowLongToBeat");
            if (!Directory.Exists(dirPath)) return;

            var currentDirTime = Directory.GetLastWriteTimeUtc(dirPath);
            bool dirChanged = currentDirTime != _localDirLastWriteTime;

            if (_localHltbAvailableIds != null && !dirChanged && !forceRefresh) return;

            logger.Info("[PlayniteGo] HltbManager: Refreshing local HLTB cache...");
            _localHltbAvailableIds = new HashSet<Guid>();
            _localDirLastWriteTime = currentDirTime;

            try 
            {
                // Enumerate files is faster than GetFiles for large directories as we don't need the array immediately
                // But we want to store IDs.
                foreach (var file in Directory.EnumerateFiles(dirPath, "*.json"))
                {
                    if (Guid.TryParse(Path.GetFileNameWithoutExtension(file), out Guid id))
                    {
                        _localHltbAvailableIds.Add(id);
                    }
                }
                logger.Info($"[PlayniteGo] HltbManager: Found {_localHltbAvailableIds.Count} local HLTB data files.");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "[PlayniteGo] HltbManager: Failed to initialize local HLTB cache.");
                _localDirLastWriteTime = DateTime.MinValue;
            }
        }

        private int ParseHours(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw) || raw.Equals("NaN", StringComparison.OrdinalIgnoreCase)) return 0;
            if (double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
            {
                return (int)Math.Round(result);
            }
            return 0;
        }

        public HltbData GetTime(string gameName, Guid? gameId = null)
        {
            if (_exactMatchCache == null)
            {
                // This might happen if LoadDatabase failed or wasn't called.
                // Depending on flow, we might want to warn or just return null.
                return null;
            }

            // 1. Try Exact Match (CSV)
            if (_exactMatchCache.TryGetValue(gameName, out var data))
            {
                return data;
            }

            // 2. Try Normalized Match (CSV)
            var normKey = NormalizeGameName(gameName);
            if (!string.IsNullOrEmpty(normKey) && _normalizedCache.TryGetValue(normKey, out var normData))
            {
                return normData;
            }

            // 3. Try Local Extension Cache Fallback
            if (gameId.HasValue && _localHltbAvailableIds != null && _localHltbAvailableIds.Contains(gameId.Value))
            {
                var extData = GetTimeFromLocalExtension(gameId.Value);
                if (extData != null)
                {
                    return extData;
                }
            }

            return null;
        }

        private HltbData GetTimeFromLocalExtension(Guid gameId)
        {
            try
            {
                string jsonPath = Path.Combine(ExtensionsDataPath, HltbExtensionId, "HowLongToBeat", $"{gameId}.json");
                if (File.Exists(jsonPath))
                {
                    string json = File.ReadAllText(jsonPath);
                    var wrapper = Serialization.FromJson<HltbUserGameData>(json);
                    var item = wrapper?.Items?.FirstOrDefault();
                    if (item?.GameHltbData != null)
                    {
                        // Times are in seconds, convert to hours
                        return new HltbData
                        {
                            MainStory = (int)Math.Round(item.GameHltbData.MainStoryMedian / 3600.0),
                            MainExtra = (int)Math.Round(item.GameHltbData.MainExtraMedian / 3600.0),
                            Completionist = (int)Math.Round(item.GameHltbData.CompletionistMedian / 3600.0)
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"[PlayniteGo] Failed to read local HLTB extension data for {gameId}");
            }
            return null;
        }

        private string NormalizeGameName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            
            // Optimization: Use StringBuilder instead of Regex for cleaner hot-path execution
            // Logic: Lowercase, remove "the " prefix, remove non-alphanumeric
            
            var lower = name.ToLowerInvariant();
            if (lower.StartsWith("the ")) lower = lower.Substring(4);

            var sb = new System.Text.StringBuilder(lower.Length);
            foreach (char c in lower)
            {
                if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }
    }
}