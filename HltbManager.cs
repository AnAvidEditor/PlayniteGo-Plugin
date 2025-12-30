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
            _exactMatchCache = new Dictionary<string, HltbData>(StringComparer.OrdinalIgnoreCase);
            _normalizedCache = new Dictionary<string, HltbData>();

            if (!File.Exists(csvPath))
            {
                logger.Error($"HLTB CSV not found at: {csvPath}");
                return;
            }

            try
            {
                using (var reader = new StreamReader(csvPath))
                {
                    // 1. Read Header
                    var headerLine = reader.ReadLine();
                    if (string.IsNullOrEmpty(headerLine)) return;

                    var headers = headerLine.Split(',').Select(h => h.Trim().ToLowerInvariant()).ToList();

                    // 2. Identify Columns
                    int nameIdx = headers.IndexOf("name");
                    int mainIdx = headers.IndexOf("main_story");
                    int extraIdx = headers.IndexOf("main_plus_sides");
                    int compIdx = headers.IndexOf("completionist");

                    if (nameIdx == -1 || mainIdx == -1)
                    {
                        logger.Error("Could not identify 'name' or 'main_story' columns in the CSV.");
                        return;
                    }

                    // 3. Parse Rows
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();

                        // FIX #1: Corrected String Escaping for C# Regex
                        // This regex handles commas inside quotes correctly
                        var parts = Regex.Split(line, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");

                        if (parts.Length <= mainIdx) continue;

                        string name = parts[nameIdx].Trim('"');

                        // Parse decimal hours (e.g. "10.62") to integer
                        int main = ParseHours(parts[mainIdx]);
                        int extra = (extraIdx != -1 && parts.Length > extraIdx) ? ParseHours(parts[extraIdx]) : 0;

                        // FIX #2: Removed the broken/unused 'int complete =' line that referenced undefined variables
                        int completeVal = (compIdx != -1 && parts.Length > compIdx) ? ParseHours(parts[compIdx]) : 0;

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
                    }
                }
                logger.Info($"Loaded {_exactMatchCache.Count} HLTB entries.");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to load HLTB CSV.");
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
            if (_exactMatchCache == null) return null;
            if (_exactMatchCache.TryGetValue(gameName, out var data)) return data;

            var normKey = NormalizeGameName(gameName);
            if (!string.IsNullOrEmpty(normKey) && _normalizedCache.TryGetValue(normKey, out var normData)) return normData;

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