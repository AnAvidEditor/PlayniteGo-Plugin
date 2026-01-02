using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Playnite.SDK;

namespace PlayniteGo
{
    public class HltbManager
    {
        private readonly ILogger logger = LogManager.GetLogger();
        private Dictionary<string, HltbData> _exactMatchCache;
        private Dictionary<string, HltbData> _normalizedCache;

        public class HltbData
        {
            public int MainStory { get; set; }
            public int MainExtra { get; set; }
            public int Completionist { get; set; }
        }

        public void LoadDatabase(string csvPath)
        {
            logger.Info($"[PlayniteGo] HltbManager: Attempting to load CSV from: '{csvPath}'");

            _exactMatchCache = new Dictionary<string, HltbData>(StringComparer.OrdinalIgnoreCase);
            _normalizedCache = new Dictionary<string, HltbData>();

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
                    var headers = headerLine.Split(',').Select(h => h.Trim().Trim('"').ToLowerInvariant()).ToList();
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

                        // Regex handles commas inside quotes e.g. "Pokemon, Y"
                        var parts = Regex.Split(line, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");

                        if (parts.Length <= mainIdx) continue;

                        string name = parts[nameIdx].Trim('"');
                        // FIX: Trim quotes from number strings before parsing
                        int main = ParseHours(parts[mainIdx].Trim('"'));
                        int extra = (extraIdx != -1 && parts.Length > extraIdx) ? ParseHours(parts[extraIdx].Trim('"')) : 0;
                        int completeVal = (compIdx != -1 && parts.Length > compIdx) ? ParseHours(parts[compIdx].Trim('"')) : 0;

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

        private int ParseHours(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw) || raw.Equals("NaN", StringComparison.OrdinalIgnoreCase)) return 0;
            if (double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
            {
                return (int)Math.Round(result);
            }
            return 0;
        }

        public HltbData GetTime(string gameName)
        {
            if (_exactMatchCache == null || _exactMatchCache.Count == 0)
            {
                logger.Warn($"[PlayniteGo] HLTB: Lookup for '{gameName}' failed because database is not loaded.");
                return null;
            }

            if (_exactMatchCache.TryGetValue(gameName, out var data))
            {
                logger.Info($"[PlayniteGo] HLTB: Exact match found for '{gameName}' (Main: {data.MainStory}h)");
                return data;
            }

            var normKey = NormalizeGameName(gameName);
            if (!string.IsNullOrEmpty(normKey) && _normalizedCache.TryGetValue(normKey, out var normData))
            {
                logger.Info($"[PlayniteGo] HLTB: Normalized match found for '{gameName}' -> '{normKey}' (Main: {normData.MainStory}h)");
                return normData;
            }

            logger.Debug($"[PlayniteGo] HLTB: No match found for '{gameName}'");
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