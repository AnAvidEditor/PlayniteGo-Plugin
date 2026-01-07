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
        private Dictionary<string, HltbData> _exactMatchCache;
        private Dictionary<string, HltbData> _normalizedCache;
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
            logger.Info($"[PlayniteGo] HltbManager: Attempting to load CSV from: '{csvPath}'");

            _exactMatchCache = new Dictionary<string, HltbData>(StringComparer.OrdinalIgnoreCase);
            _normalizedCache = new Dictionary<string, HltbData>();

            // --- OPTIMIZATION: Pre-scan local extension directory ---
            InitializeLocalCache();

            if (!File.Exists(csvPath))
            {
                logger.Error($"[PlayniteGo] HltbManager: CRITICAL ERROR - File does not exist at path: {csvPath}");
                return;
            }

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

        private HashSet<Guid> _localHltbAvailableIds;

        private void InitializeLocalCache()
        {
            _localHltbAvailableIds = new HashSet<Guid>();
            if (string.IsNullOrEmpty(ExtensionsDataPath)) return;

            try 
            {
                string dirPath = Path.Combine(ExtensionsDataPath, HltbExtensionId, "HowLongToBeat");
                if (Directory.Exists(dirPath))
                {
                    // Enumerate files is faster than GetFiles for large directories as we don't need the array immediately
                    // But we want to store IDs.
                    var files = Directory.GetFiles(dirPath, "*.json");
                    foreach (var file in files)
                    {
                        if (Guid.TryParse(Path.GetFileNameWithoutExtension(file), out Guid id))
                        {
                            _localHltbAvailableIds.Add(id);
                        }
                    }
                    logger.Info($"[PlayniteGo] HltbManager: Found {_localHltbAvailableIds.Count} local HLTB data files.");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "[PlayniteGo] HltbManager: Failed to initialize local HLTB cache.");
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
                logger.Warn($"[PlayniteGo] HLTB: Lookup for '{gameName}' failed because database is not initialized.");
                return null;
            }

            // 1. Try Exact Match (CSV)
            if (_exactMatchCache.TryGetValue(gameName, out var data))
            {
                logger.Info($"[PlayniteGo] HLTB: Exact CSV match found for '{gameName}' (Main: {data.MainStory}h)");
                return data;
            }

            // 2. Try Normalized Match (CSV)
            var normKey = NormalizeGameName(gameName);
            if (!string.IsNullOrEmpty(normKey) && _normalizedCache.TryGetValue(normKey, out var normData))
            {
                logger.Info($"[PlayniteGo] HLTB: Normalized CSV match found for '{gameName}' -> '{normKey}' (Main: {normData.MainStory}h)");
                return normData;
            }

            // 3. Try Local Extension Cache Fallback
            if (gameId.HasValue && _localHltbAvailableIds != null && _localHltbAvailableIds.Contains(gameId.Value))
            {
                var extData = GetTimeFromLocalExtension(gameId.Value);
                if (extData != null)
                {
                    logger.Info($"[PlayniteGo] HLTB: Local Extension match found for '{gameName}' ({gameId}) (Main: {extData.MainStory}h)");
                    return extData;
                }
            }

            logger.Debug($"[PlayniteGo] HLTB: No match found for '{gameName}'");
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
            string outName = name.ToLowerInvariant();
            if (outName.StartsWith("the ")) outName = outName.Substring(4);
            return Regex.Replace(outName, "[^a-z0-9]", "");
        }
    }
}