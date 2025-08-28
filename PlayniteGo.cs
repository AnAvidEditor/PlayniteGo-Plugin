using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Image = SixLabors.ImageSharp.Image;
using Size = SixLabors.ImageSharp.Size;

namespace PlayniteGo
{
    // --- Data Models for Export ---

    public class TimeData
    {
        public long MainStory { get; set; }
        public long MainExtra { get; set; }
        public long Completionist { get; set; }
        public long MainStoryAverage { get; set; }
        public long MainExtraAverage { get; set; }
        public long CompletionistAverage { get; set; }
    }

    public class ExportPayload
    {
        public List<GameExport> Games { get; set; }
        public List<GameExport> UpdatedGames { get; set; }
        public List<Guid> DeletedGameIds { get; set; }
        public ExportedStats Stats { get; set; }
        public ExportedFilterOptions FilterOptions { get; set; }
    }

    public class ExportedStats
    {
        public SummaryStats AllGames { get; set; }
        public SummaryStats PlayedGames { get; set; }
        public SummaryStats UnplayedGames { get; set; }
        public BacklogStats Backlog { get; set; }
    }

    public class SummaryStats
    {
        public int TotalGames { get; set; }
        public int GamesPlayedCount { get; set; }
        public int TotalPlaytimeHours { get; set; }
        public GameTime MostPlayedGame { get; set; }
        public GameTime LeastPlayedGame { get; set; }
        public List<CountData> CompletionStatusCounts { get; set; }
        public List<CountData> AllSources { get; set; }
        public List<CountData> AllPlatforms { get; set; }
        public List<CountData> TopGenres { get; set; }
        public List<CountData> TopDevelopers { get; set; }
        public List<CountData> TopPublishers { get; set; }
        public List<CountData> TopTags { get; set; }
        public List<CountData> TopFeatures { get; set; }
        public List<CountData> GamesByDecade { get; set; }
        public List<GameScore> TopCriticRated { get; set; }
        public List<GameScore> BottomCriticRated { get; set; }
        public List<GameScore> TopCommunityRated { get; set; }
        public List<GameScore> BottomCommunityRated { get; set; }
        public List<GameScore> TopUserRated { get; set; }
        public List<GameScore> BottomUserRated { get; set; }
        public GameScore OldestRelease { get; set; }
        public GameScore NewestRelease { get; set; }
        public GameScore OldestAdded { get; set; }
        public GameScore NewestAdded { get; set; }
    }

    public class BacklogStats
    {
        public int TotalUnplayedGames { get; set; }
        public int TotalUnplayedHours { get; set; }
        public DateTime? CompletionDate { get; set; }
        public GameTime LongestBacklogGame { get; set; }
        public GameTime ShortestBacklogGame { get; set; }
    }

    public class RangeData
    {
        public int LowerBound { get; set; }
        public int UpperBound { get; set; }
    }

    public class ExportedFilterOptions
    {
        public List<string> Sources { get; set; }
        public List<string> CompletionStatuses { get; set; }
        public List<string> Platforms { get; set; }
        public List<string> Genres { get; set; }
        public List<string> Developers { get; set; }
        public List<string> Publishers { get; set; }
        public List<string> Features { get; set; }
        public List<string> Tags { get; set; }
        public List<string> Series { get; set; }
        public List<string> AgeRatings { get; set; }
        public List<string> Regions { get; set; }
        public List<string> Categories { get; set; }
        public RangeData PlaytimeRange { get; set; }
        public RangeData ReleaseYearRange { get; set; }
        public RangeData InstallSizeRange { get; set; }
        public RangeData HltbMainStoryRange { get; set; }
    }

    public class CountData
    {
        public string Name { get; set; }
        public int Count { get; set; }
    }

    public class GameScore
    {
        public string Name { get; set; }
        public int Score { get; set; }
    }

    public class GameTime
    {
        public string Name { get; set; }
        public int Hours { get; set; }
    }

    public class GameExport
    {
        // --- CORE DATA ---
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Source { get; set; }
        public ulong Playtime { get; set; }
        public string CoverImagePath { get; set; }
        public string BackgroundImagePath { get; set; }
        public string Description { get; set; }
        public DateTime? AddedDate { get; set; }
        public DateTime? LastActivity { get; set; }
        public int? CommunityScore { get; set; }
        public int? CriticScore { get; set; }
        public string CompletionStatus { get; set; }
        public bool Hidden { get; set; }
        public bool IsInstalled { get; set; }
        public bool Favorite { get; set; }
        public string SortingName { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public int? UserScore { get; set; }
        public string Notes { get; set; }
        public ulong? InstallSize { get; set; }
        public HltbApi.HltbDataItem HowLongToBeatData { get; set; }

        // --- CORE RELATIONSHIPS (RAW DATA) ---
        public List<string> Platforms { get; set; }
        public List<string> Genres { get; set; }
        public List<string> Features { get; set; }
        public List<string> Developers { get; set; }
        public List<string> Publishers { get; set; }
        public List<LinkExport> Links { get; set; }
        public List<string> AgeRatings { get; set; }
        public List<string> Series { get; set; }
        public List<string> Tags { get; set; }
        public List<string> Categories { get; set; }
        public List<string> Regions { get; set; }
        public List<string> RelatedDeveloperGameIds { get; set; }
        public List<string> RelatedPublisherGameIds { get; set; }
        public List<string> RelatedSeriesGameIds { get; set; }

        // --- NEW: PRE-COMPUTED & PRE-FORMATTED FIELDS FOR THIN CLIENT ---
        public string PlainTextDescription { get; set; }
        public int? ReleaseYear { get; set; }
        public long? HltbMainStoryInSeconds { get; set; }
        public string DisplayPlatformNames { get; set; }
        public string DisplayFirstGenre { get; set; }
        public string DisplayContributors { get; set; }
        public string DisplayPlaytime { get; set; }
        public string DisplayHltbMain { get; set; }
        public string DisplayInstallSize { get; set; }
        public string DisplayAddedDate { get; set; }
        public string DisplayLastPlayed { get; set; }
        public bool HasControllerSupport { get; set; }
        public bool HasVRSupport { get; set; }
        public bool HasUltrawideSupport { get; set; }
        public bool HasHDRSupport { get; set; }
    }


    public class LinkExport
    {
        public string Name { get; set; }
        public string Url { get; set; }
    }

    namespace HltbApi
    {
        public class HltbData
        {
            public List<HltbDataItem> Items { get; set; }
        }

        public class HltbDataItem
        {
            public string Name { get; set; }
            public int Id { get; set; }
            [SerializationPropertyName("UrlImg")] public string GameImage { get; set; }
            public string Url { get; set; }
            [SerializationPropertyName("GameHltbData")] public GameTimeData TimeData { get; set; }
        }

        public class GameTimeData
        {
            [SerializationPropertyName("MainStoryAverage")] public ulong MainStoryAverage { get; set; }
            [SerializationPropertyName("MainExtraAverage")] public ulong MainPlusExtraAverage { get; set; }
            [SerializationPropertyName("CompletionistAverage")] public ulong CompletionistAverage { get; set; }
        }
    }


    public class PlayniteGo : GenericPlugin
    {
        public class ExportState
        {
            public DateTime LastExportDate { get; set; }
        }

        private static readonly ILogger logger = LogManager.GetLogger();
        private PlayniteGoSettingsViewModel settings { get; set; }
        public override Guid Id { get; } = Guid.Parse("af7bd5e5-0ae0-4276-bb2a-cdf7fadea92e");
        private const string exportedIdsFileName = "exportedGameIds.json";
        private const string stateFileName = "exportState.json";
        private enum ImageType { Cover, Background }

        public static Guid steamPluginId = Guid.Parse("cb91dfc9-b977-43bf-8e70-55f46e410fab");
        public static Guid hltbPluginId = Guid.Parse("e08cd51f-9c9a-4ee3-a094-fde03b55492f");

        public PlayniteGo(IPlayniteAPI api) : base(api)
        {
            settings = new PlayniteGoSettingsViewModel(this);
            Properties = new GenericPluginProperties { HasSettings = true };
        }

        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
        {
            var menuItems = new List<MainMenuItem>
            {
                new MainMenuItem
                {
                    Description = "Full Export to App",
                    MenuSection = "@PlayniteGo",
                    Action = (actionArgs) => PerformFullExport()
                },
                new MainMenuItem
                {
                    Description = "Sync Changes to App",
                    MenuSection = "@PlayniteGo",
                    Action = (actionArgs) => PerformIncrementalExport()
                },
                new MainMenuItem
                {
                    Description = "-", // Separator
                    MenuSection = "@PlayniteGo"
                },
                new MainMenuItem
                {
                    Description = "Copy Diagnostic Report to Clipboard",
                    MenuSection = "@PlayniteGo|Help & Support",
                    Action = (actionArgs) =>
                    {
                        var report = GenerateDiagnosticReport();
                        Clipboard.SetText(report);
                        PlayniteApi.Dialogs.ShowMessage("Diagnostic data has been copied to your clipboard.", "PlayniteGo");
                    }
                },
                new MainMenuItem
                {
                    Description = "Save Diagnostic Report to File...",
                    MenuSection = "@PlayniteGo|Help & Support",
                    Action = (actionArgs) =>
                    {
                        var report = GenerateDiagnosticReport();
                        var result = PlayniteApi.Dialogs.SaveFile("Text Files (*.txt)|*.txt");
                        if (!string.IsNullOrEmpty(result))
                        {
                            File.WriteAllText(result, report);
                        }
                    }
                }
            };

            return menuItems;
        }

        private void PerformFullExport()
        {
            try
            {
                if (!CheckPrerequisites(out string hltbPath, out bool hltbFound)) return;

                var allGames = PlayniteApi.Database.Games.ToList();
                if (!allGames.Any())
                {
                    PlayniteApi.Dialogs.ShowMessage("No games in library to export.", "PlayniteGo Export");
                    return;
                }

                var result = PlayniteApi.Dialogs.SaveFile("Zip Files (*.zip)|*.zip");
                if (string.IsNullOrEmpty(result)) return;

                ExecuteFullExport(allGames, result, hltbPath, "Full library export...");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to perform full export.");
                PlayniteApi.Dialogs.ShowErrorMessage("An unexpected error occurred during the full export. Please check the log file for details.", "PlayniteGo Error");
            }
        }

        private void PerformIncrementalExport()
        {
            try
            {
                if (!CheckPrerequisites(out string hltbPath, out bool hltbFound)) return;

                var lastExportDate = LoadLastExportDate();
                if (lastExportDate == DateTime.MinValue)
                {
                    PlayniteApi.Dialogs.ShowMessage("A full export has not been performed yet. Please run 'Full Export to App' first to create a baseline.", "Sync Error");
                    return;
                }

                var modifiedGames = PlayniteApi.Database.Games.Where(g => g.Modified != null && g.Modified > lastExportDate).ToList();
                var newGames = PlayniteApi.Database.Games.Where(g => g.Added != null && g.Added > lastExportDate).ToList();
                var gamesToUpdate = newGames.Union(modifiedGames).Distinct().ToList();

                var previousIds = LoadPreviouslyExportedIds();
                var currentIds = Enumerable.ToHashSet(PlayniteApi.Database.Games.Select(g => g.Id));
                var deletedGameIds = previousIds.Where(id => !currentIds.Contains(id)).ToList();

                if (!gamesToUpdate.Any() && !deletedGameIds.Any())
                {
                    PlayniteApi.Dialogs.ShowMessage("No changes to sync since the last export.", "Sync Complete");
                    return;
                }

                var result = PlayniteApi.Dialogs.SaveFile("Zip Files (*.zip)|*.zip");
                if (string.IsNullOrEmpty(result)) return;

                ExecuteIncrementalExport(gamesToUpdate, deletedGameIds, result, hltbPath, "Syncing library changes...");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to perform incremental export.");
                PlayniteApi.Dialogs.ShowErrorMessage("An unexpected error occurred during the sync. Please check the log file for details.", "PlayniteGo Error");
            }
        }

        private void ExecuteFullExport(List<Game> gamesToProcess, string exportZipPath, string hltbPath, string progressMessage)
        {
            var allGameIds = Enumerable.ToHashSet(gamesToProcess.Select(g => g.Id));
            var payload = new ExportPayload { Games = new List<GameExport>() };
            var exportDate = DateTime.UtcNow;

            bool success = ProcessAndZip(gamesToProcess, payload, exportZipPath, allGameIds, hltbPath, progressMessage, exportDate);
            if (success)
            {
                PlayniteApi.Dialogs.ShowMessage($"Successfully exported {payload.Games.Count} games.", "Export Complete");
            }
        }

        private void ExecuteIncrementalExport(List<Game> gamesToProcess, List<Guid> deletedGameIds, string exportZipPath, string hltbPath, string progressMessage)
        {
            var currentIds = Enumerable.ToHashSet(PlayniteApi.Database.Games.Select(g => g.Id));
            var payload = new ExportPayload
            {
                UpdatedGames = new List<GameExport>(),
                DeletedGameIds = deletedGameIds
            };
            var exportDate = DateTime.UtcNow;

            bool success = ProcessAndZip(gamesToProcess, payload, exportZipPath, currentIds, hltbPath, progressMessage, exportDate);
            if (success)
            {
                PlayniteApi.Dialogs.ShowMessage($"Successfully synced {payload.UpdatedGames.Count} updates and {payload.DeletedGameIds.Count} deletions.", "Sync Complete");
            }
        }

        private bool ProcessAndZip(List<Game> gamesToProcess, ExportPayload payload, string exportZipPath, HashSet<Guid> currentIds, string hltbPath, string progressMessage, DateTime exportDate)
        {
            bool wasSuccess = false;
            PlayniteApi.Dialogs.ActivateGlobalProgress(args =>
            {
                var allGamesInLibrary = PlayniteApi.Database.Games.ToList();

                Func<Game, HltbApi.HltbDataItem> getHltbData = (game) =>
                {
                    if (game == null || string.IsNullOrEmpty(hltbPath)) return null;
                    string hltbDetailsPath = Path.Combine(hltbPath, "HowLongToBeat", $"{game.Id}.json");
                    if (!File.Exists(hltbDetailsPath)) return null;
                    try
                    {
                        var hltbData = Serialization.FromJson<HltbApi.HltbData>(File.ReadAllText(hltbDetailsPath));
                        return hltbData?.Items?.FirstOrDefault();
                    }
                    catch { return null; }
                };

                // ✅ --- START OF OPTIMIZED DATA AGGREGATION ---

                args.Text = "Analyzing library...";
                args.IsIndeterminate = true;

                var playedGames = new List<Game>();
                var unplayedGames = new List<Game>();

                var uniqueSources = new HashSet<string>();
                var uniqueCompletionStatuses = new HashSet<string>();
                var uniquePlatforms = new HashSet<string>();
                var uniqueGenres = new HashSet<string>();
                var uniqueDevelopers = new HashSet<string>();
                var uniquePublishers = new HashSet<string>();
                var uniqueFeatures = new HashSet<string>();
                var uniqueTags = new HashSet<string>();
                var uniqueSeries = new HashSet<string>();
                var uniqueAgeRatings = new HashSet<string>();
                var uniqueRegions = new HashSet<string>();
                var uniqueCategories = new HashSet<string>();

                var developerLookup = new Dictionary<string, List<Guid>>();
                var publisherLookup = new Dictionary<string, List<Guid>>();
                var seriesLookup = new Dictionary<string, List<Guid>>();

                // ✅ This single loop gathers all data needed for filters, stats, and lookups.
                foreach (var game in allGamesInLibrary)
                {
                    // Stats categorization
                    if (game.Playtime > 0) { playedGames.Add(game); } else { unplayedGames.Add(game); }

                    // Filter options
                    if (game.Source != null) uniqueSources.Add(game.Source.Name);
                    if (game.CompletionStatus != null) uniqueCompletionStatuses.Add(game.CompletionStatus.Name);
                    if (game.Platforms != null) foreach (var p in game.Platforms) uniquePlatforms.Add(p.Name);
                    if (game.Genres != null) foreach (var g in game.Genres) uniqueGenres.Add(g.Name);
                    if (game.Developers != null) foreach (var d in game.Developers) uniqueDevelopers.Add(d.Name);
                    if (game.Publishers != null) foreach (var p in game.Publishers) uniquePublishers.Add(p.Name);
                    if (game.Features != null) foreach (var f in game.Features) uniqueFeatures.Add(f.Name);
                    if (game.Tags != null) foreach (var t in game.Tags) uniqueTags.Add(t.Name);
                    if (game.Series != null) foreach (var s in game.Series) uniqueSeries.Add(s.Name);
                    if (game.AgeRatings != null) foreach (var a in game.AgeRatings) uniqueAgeRatings.Add(a.Name);
                    if (game.Regions != null) foreach (var r in game.Regions) uniqueRegions.Add(r.Name);
                    if (game.Categories != null) foreach (var c in game.Categories) uniqueCategories.Add(c.Name);

                    // Lookup tables for related games
                    if (game.Developers != null) foreach (var dev in game.Developers) { if (!developerLookup.ContainsKey(dev.Name)) developerLookup[dev.Name] = new List<Guid>(); developerLookup[dev.Name].Add(game.Id); }
                    if (game.Publishers != null) foreach (var pub in game.Publishers) { if (!publisherLookup.ContainsKey(pub.Name)) publisherLookup[pub.Name] = new List<Guid>(); publisherLookup[pub.Name].Add(game.Id); }
                    if (game.Series != null) foreach (var ser in game.Series) { if (!seriesLookup.ContainsKey(ser.Name)) seriesLookup[ser.Name] = new List<Guid>(); seriesLookup[ser.Name].Add(game.Id); }
                }

                var filterOptions = new ExportedFilterOptions
                {
                    Sources = uniqueSources.OrderBy(n => n).ToList(),
                    CompletionStatuses = uniqueCompletionStatuses.OrderBy(n => n).ToList(),
                    Platforms = uniquePlatforms.OrderBy(n => n).ToList(),
                    Genres = uniqueGenres.OrderBy(n => n).ToList(),
                    Developers = uniqueDevelopers.OrderBy(n => n).ToList(),
                    Publishers = uniquePublishers.OrderBy(n => n).ToList(),
                    Features = uniqueFeatures.OrderBy(n => n).ToList(),
                    Tags = uniqueTags.OrderBy(n => n).ToList(),
                    Series = uniqueSeries.OrderBy(n => n).ToList(),
                    AgeRatings = uniqueAgeRatings.OrderBy(n => n).ToList(),
                    Regions = uniqueRegions.OrderBy(n => n).ToList(),
                    Categories = uniqueCategories.OrderBy(n => n).ToList()
                };

                var gamesWithReleaseYear = allGamesInLibrary.Where(g => g.ReleaseDate != null).Select(g => g.ReleaseDate.Value.Year).ToList();
                var gamesWithHltb = allGamesInLibrary.Select(g => getHltbData(g)?.TimeData?.MainStoryAverage ?? 0).Where(t => t > 0).ToList();

                int maxPlaytimeHours = allGamesInLibrary.Any() ? (int)Math.Ceiling(allGamesInLibrary.Max(g => g.Playtime) / 3600.0) : 1;
                int minReleaseYear = gamesWithReleaseYear.Any() ? gamesWithReleaseYear.Min() : 1990;
                int maxReleaseYear = gamesWithReleaseYear.Any() ? gamesWithReleaseYear.Max() : DateTime.Now.Year;
                int maxInstallSizeGb = allGamesInLibrary.Any(g => g.InstallSize > 0) ? (int)Math.Ceiling(allGamesInLibrary.Max(g => g.InstallSize ?? 0) / 1073741824.0) : 1;
                int maxHltbHours = gamesWithHltb.Any() ? (int)Math.Ceiling(gamesWithHltb.Max() / 3600.0) : 1;

                filterOptions.PlaytimeRange = new RangeData { LowerBound = 0, UpperBound = Math.Max(1, maxPlaytimeHours) };
                filterOptions.ReleaseYearRange = new RangeData { LowerBound = minReleaseYear, UpperBound = maxReleaseYear };
                filterOptions.InstallSizeRange = new RangeData { LowerBound = 0, UpperBound = Math.Max(1, maxInstallSizeGb) };
                filterOptions.HltbMainStoryRange = new RangeData { LowerBound = 0, UpperBound = Math.Max(1, maxHltbHours) };

                Func<List<Game>, SummaryStats> calculateSummaryStats = (games) =>
                {
                    if (!games.Any()) return new SummaryStats { TotalGames = 0, CompletionStatusCounts = new List<CountData>(), AllPlatforms = new List<CountData>() };

                    var playedGamesStats = games.Where(g => g.Playtime > 0).ToList();
                    var gamesWithReleaseDate = games.Where(g => g.ReleaseDate != null).ToList();
                    var gamesWithAddedDate = games.Where(g => g.Added != null).ToList();

                    var mostPlayed = playedGamesStats.OrderByDescending(g => g.Playtime).FirstOrDefault();
                    var leastPlayed = playedGamesStats.OrderBy(g => g.Playtime).FirstOrDefault();
                    var oldestRelease = gamesWithReleaseDate.OrderBy(g => g.ReleaseDate.Value).FirstOrDefault();
                    var newestRelease = gamesWithReleaseDate.OrderByDescending(g => g.ReleaseDate.Value).FirstOrDefault();
                    var oldestAdded = gamesWithAddedDate.OrderBy(g => g.Added.Value).FirstOrDefault();
                    var newestAdded = gamesWithAddedDate.OrderByDescending(g => g.Added.Value).FirstOrDefault();

                    return new SummaryStats
                    {
                        TotalGames = games.Count,
                        GamesPlayedCount = playedGamesStats.Count,
                        TotalPlaytimeHours = (int)games.Sum(g => (long)g.Playtime) / 3600,
                        MostPlayedGame = mostPlayed == null ? null : new GameTime { Name = mostPlayed.Name, Hours = (int)(mostPlayed.Playtime / 3600) },
                        LeastPlayedGame = leastPlayed == null ? null : new GameTime { Name = leastPlayed.Name, Hours = (int)(leastPlayed.Playtime / 3600) },
                        CompletionStatusCounts = games.GroupBy(g => g.CompletionStatus?.Name ?? "Not Set").Select(g => new CountData { Name = g.Key, Count = g.Count() }).OrderByDescending(s => s.Count).ToList(),
                        AllSources = games.Where(g => g.Source != null).GroupBy(g => g.Source.Name).Select(g => new CountData { Name = g.Key, Count = g.Count() }).OrderByDescending(s => s.Count).ToList(),
                        AllPlatforms = games.Where(g => g.Platforms != null).SelectMany(g => g.Platforms).GroupBy(p => p.Name).Select(group => new CountData { Name = group.Key, Count = group.Count() }).OrderByDescending(x => x.Count).ToList(),
                        TopGenres = games.Where(g => g.Genres != null).SelectMany(g => g.Genres).GroupBy(g => g.Name).Select(group => new CountData { Name = group.Key, Count = group.Count() }).OrderByDescending(x => x.Count).Take(5).ToList(),
                        TopDevelopers = games.Where(g => g.Developers != null).SelectMany(g => g.Developers).GroupBy(g => g.Name).Select(group => new CountData { Name = group.Key, Count = group.Count() }).OrderByDescending(x => x.Count).Take(5).ToList(),
                        TopPublishers = games.Where(g => g.Publishers != null).SelectMany(g => g.Publishers).GroupBy(g => g.Name).Select(group => new CountData { Name = group.Key, Count = group.Count() }).OrderByDescending(x => x.Count).Take(5).ToList(),
                        TopTags = games.Where(g => g.Tags != null).SelectMany(g => g.Tags).GroupBy(g => g.Name).Select(group => new CountData { Name = group.Key, Count = group.Count() }).OrderByDescending(x => x.Count).Take(5).ToList(),
                        TopFeatures = games.Where(g => g.Features != null).SelectMany(g => g.Features).GroupBy(g => g.Name).Select(group => new CountData { Name = group.Key, Count = group.Count() }).OrderByDescending(x => x.Count).Take(5).ToList(),
                        GamesByDecade = games.Where(g => g.ReleaseDate != null).GroupBy(g => (g.ReleaseDate.Value.Year / 10) * 10).Select(g => new CountData { Name = $"{g.Key}s", Count = g.Count() }).OrderBy(x => x.Name).ToList(),
                        TopCriticRated = games.Where(g => g.CriticScore != null && g.CriticScore > 0).OrderByDescending(g => g.CriticScore).Take(5).Select(g => new GameScore { Name = g.Name, Score = g.CriticScore.Value }).ToList(),
                        BottomCriticRated = games.Where(g => g.CriticScore != null && g.CriticScore > 0).OrderBy(g => g.CriticScore).Take(5).Select(g => new GameScore { Name = g.Name, Score = g.CriticScore.Value }).ToList(),
                        TopCommunityRated = games.Where(g => g.CommunityScore != null && g.CommunityScore > 0).OrderByDescending(g => g.CommunityScore).Take(5).Select(g => new GameScore { Name = g.Name, Score = g.CommunityScore.Value }).ToList(),
                        BottomCommunityRated = games.Where(g => g.CommunityScore != null && g.CommunityScore > 0).OrderBy(g => g.CommunityScore).Take(5).Select(g => new GameScore { Name = g.Name, Score = g.CommunityScore.Value }).ToList(),
                        TopUserRated = games.Where(g => g.UserScore != null && g.UserScore > 0).OrderByDescending(g => g.UserScore).Take(5).Select(g => new GameScore { Name = g.Name, Score = g.UserScore.Value }).ToList(),
                        BottomUserRated = games.Where(g => g.UserScore != null && g.UserScore > 0).OrderBy(g => g.UserScore).Take(5).Select(g => new GameScore { Name = g.Name, Score = g.UserScore.Value }).ToList(),
                        OldestRelease = oldestRelease == null ? null : new GameScore { Name = oldestRelease.Name, Score = oldestRelease.ReleaseDate.Value.Year },
                        NewestRelease = newestRelease == null ? null : new GameScore { Name = newestRelease.Name, Score = newestRelease.ReleaseDate.Value.Year },
                        OldestAdded = oldestAdded == null ? null : new GameScore { Name = oldestAdded.Name, Score = oldestAdded.Added.Value.Year },
                        NewestAdded = newestAdded == null ? null : new GameScore { Name = newestAdded.Name, Score = newestAdded.Added.Value.Year }
                    };
                };

                var backlogGames = unplayedGames.Select(g => new { Game = g, Hltb = getHltbData(g) }).Where(x => x.Hltb?.TimeData?.MainStoryAverage > 0).ToList();
                var longestBacklogGame = backlogGames.OrderByDescending(g => g.Hltb.TimeData.MainStoryAverage).FirstOrDefault();
                var shortestBacklogGame = backlogGames.Where(g => g.Hltb.TimeData.MainStoryAverage > 0).OrderBy(g => g.Hltb.TimeData.MainStoryAverage).FirstOrDefault();
                long totalBacklogSeconds = backlogGames.Sum(g => (long)g.Hltb.TimeData.MainStoryAverage);

                payload.Stats = new ExportedStats
                {
                    AllGames = calculateSummaryStats(allGamesInLibrary),
                    PlayedGames = calculateSummaryStats(playedGames),
                    UnplayedGames = calculateSummaryStats(unplayedGames),
                    Backlog = new BacklogStats
                    {
                        TotalUnplayedGames = backlogGames.Count,
                        TotalUnplayedHours = (int)(totalBacklogSeconds / 3600),
                        CompletionDate = totalBacklogSeconds > 0 ? (DateTime?)DateTime.UtcNow.AddSeconds(totalBacklogSeconds) : null,
                        LongestBacklogGame = longestBacklogGame == null ? null : new GameTime { Name = longestBacklogGame.Game.Name, Hours = (int)(longestBacklogGame.Hltb.TimeData.MainStoryAverage / 3600) },
                        ShortestBacklogGame = shortestBacklogGame == null ? null : new GameTime { Name = shortestBacklogGame.Game.Name, Hours = (int)(shortestBacklogGame.Hltb.TimeData.MainStoryAverage / 3600) }
                    }
                };
                payload.FilterOptions = filterOptions;

                // ✅ --- END OF OPTIMIZED DATA AGGREGATION ---

                var tempDir = Path.Combine(Path.GetTempPath(), "PlayniteExport_" + Guid.NewGuid());
                var imagesDir = Path.Combine(tempDir, "images");
                Directory.CreateDirectory(imagesDir);

                try
                {
                    args.ProgressMaxValue = gamesToProcess.Count;
                    args.IsIndeterminate = false; // Switch to determinate progress for game processing.
                    var processedGames = new ConcurrentBag<GameExport>();
                    int progress = 0;

                    Parallel.ForEach(gamesToProcess, (game) =>
                    {
                        if (args.CancelToken.IsCancellationRequested) return;
                        var gameExport = CreateGameExport(game, hltbPath, imagesDir, developerLookup, publisherLookup, seriesLookup, currentIds);
                        if (gameExport != null) processedGames.Add(gameExport);
                        Interlocked.Increment(ref progress);
                        args.CurrentProgressValue = progress;
                        args.Text = $"Processing: {game.Name} ({progress}/{gamesToProcess.Count})";
                    });

                    if (args.CancelToken.IsCancellationRequested) return;

                    if (payload.Games != null) payload.Games = processedGames.OrderBy(g => g.Name).ToList();
                    if (payload.UpdatedGames != null) payload.UpdatedGames = processedGames.OrderBy(g => g.Name).ToList();

                    var jsonContent = Serialization.ToJson(payload, true);
                    File.WriteAllText(Path.Combine(tempDir, "library.json"), jsonContent);

                    if (args.CancelToken.IsCancellationRequested) return;

                    args.Text = "Compressing files...";
                    args.IsIndeterminate = true;
                    if (File.Exists(exportZipPath)) File.Delete(exportZipPath);
                    ZipFile.CreateFromDirectory(tempDir, exportZipPath, CompressionLevel.Optimal, false);

                    if (args.CancelToken.IsCancellationRequested) return;

                    SaveLastExportDate(exportDate);
                    SaveExportedIds(currentIds);
                    wasSuccess = true;
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Failed during export process.");
                    PlayniteApi.MainView.UIDispatcher.Invoke(() => { PlayniteApi.Dialogs.ShowErrorMessage($"Export failed: {ex.Message}", "Error"); });
                    wasSuccess = false;
                }
                finally
                {
                    if (Directory.Exists(tempDir)) { try { Directory.Delete(tempDir, true); } catch (Exception ex) { logger.Error(ex, "Failed to cleanup temp directory."); } }
                }
            }, new GlobalProgressOptions(progressMessage, true) { IsIndeterminate = false });
            return wasSuccess;
        }



        private GameExport CreateGameExport(Game game, string hltbPath, string imagesDir,
            Dictionary<string, List<Guid>> developerLookup,
            Dictionary<string, List<Guid>> publisherLookup,
            Dictionary<string, List<Guid>> seriesLookup,
            HashSet<Guid> allExportedGameIds)
        {
            var hltbData = GetHltbData(game, hltbPath);

            var gameExport = new GameExport
            {
                // --- CORE DATA ---
                Id = game.Id,
                Name = game.Name,
                Source = game.Source?.Name,
                Playtime = game.Playtime,
                Description = game.Description,
                AddedDate = game.Added,
                LastActivity = game.LastActivity,
                CommunityScore = game.CommunityScore,
                CriticScore = game.CriticScore,
                CompletionStatus = game.CompletionStatus?.Name,
                Hidden = game.Hidden,
                IsInstalled = game.IsInstalled,
                Favorite = game.Favorite,
                SortingName = game.SortingName,
                ReleaseDate = game.ReleaseDate?.Date,
                UserScore = game.UserScore,
                Notes = game.Notes,
                InstallSize = game.InstallSize,
                HowLongToBeatData = hltbData,

                // --- CORE RELATIONSHIPS (RAW DATA) ---
                Platforms = game.Platforms?.Select(p => p.Name).ToList() ?? new List<string>(),
                Genres = game.Genres?.Select(g => g.Name).ToList() ?? new List<string>(),
                Features = game.Features?.Select(f => f.Name).ToList() ?? new List<string>(),
                Developers = game.Developers?.Select(d => d.Name).ToList() ?? new List<string>(),
                Publishers = game.Publishers?.Select(p => p.Name).ToList() ?? new List<string>(),
                Links = game.Links?.Select(l => new LinkExport { Name = l.Name, Url = l.Url }).ToList() ?? new List<LinkExport>(),
                AgeRatings = game.AgeRatings?.Select(ar => ar.Name).ToList() ?? new List<string>(),
                Series = game.Series?.Select(s => s.Name).ToList() ?? new List<string>(),
                Tags = game.Tags?.Select(t => t.Name).ToList() ?? new List<string>(),
                Categories = game.Categories?.Select(c => c.Name).ToList() ?? new List<string>(),
                Regions = game.Regions?.Select(r => r.Name).ToList() ?? new List<string>(),

                // --- IMAGE PROCESSING ---
                CoverImagePath = ProcessAndCopyLocalImage(game.CoverImage, imagesDir, ImageType.Cover),
                BackgroundImagePath = ProcessAndCopyLocalImage(game.BackgroundImage, imagesDir, ImageType.Background),
            };

            // --- PRE-COMPUTATION LOGIC ---
            gameExport.PlainTextDescription = StripHtml(game.Description);
            gameExport.ReleaseYear = game.ReleaseDate?.Year;
            gameExport.HltbMainStoryInSeconds = (long?)(hltbData?.TimeData?.MainStoryAverage);

            gameExport.DisplayPlatformNames = FormatPlatformNames(game.Platforms);
            gameExport.DisplayFirstGenre = FormatFirstGenre(game.Genres?.FirstOrDefault()?.Name);
            gameExport.DisplayContributors = FormatContributors(game.Developers, game.Publishers);
            gameExport.DisplayPlaytime = FormatPlaytime(game.Playtime);
            gameExport.DisplayHltbMain = FormatHltbMain(hltbData?.TimeData?.MainStoryAverage);
            gameExport.DisplayInstallSize = FormatInstallSize(game.InstallSize);
            gameExport.DisplayAddedDate = FormatShortDate(game.Added);
            gameExport.DisplayLastPlayed = FormatShortDate(game.LastActivity);

            var allFeatures = new HashSet<string>(
                (game.Features?.Select(f => f.Name.ToLowerInvariant()) ?? Enumerable.Empty<string>())
                .Concat(game.Tags?.Select(t => t.Name.ToLowerInvariant()) ?? Enumerable.Empty<string>())
            );

            gameExport.HasControllerSupport = allFeatures.Any(f => f.Contains("controller"));
            gameExport.HasVRSupport = allFeatures.Any(f => f == "vr" || f == "virtual reality" || f == "virtual-reality");
            gameExport.HasUltrawideSupport = allFeatures.Any(f => f.Contains("ultrawide") || f.Contains("ultra-wide"));
            gameExport.HasHDRSupport = allFeatures.Any(f => f.Contains("hdr"));


            var relatedDeveloperIds = new HashSet<Guid>();
            if (game.Developers != null)
            {
                foreach (var dev in game.Developers)
                {
                    if (developerLookup.TryGetValue(dev.Name, out var ids))
                    {
                        foreach (var id in ids)
                        {
                            if (id != game.Id && allExportedGameIds.Contains(id))
                            {
                                relatedDeveloperIds.Add(id);
                            }
                        }
                    }
                }
            }
            gameExport.RelatedDeveloperGameIds = relatedDeveloperIds.Select(id => id.ToString()).ToList();

            var relatedPublisherIds = new HashSet<Guid>();
            if (game.Publishers != null)
            {
                foreach (var pub in game.Publishers)
                {
                    if (publisherLookup.TryGetValue(pub.Name, out var ids))
                    {
                        foreach (var id in ids)
                        {
                            if (id != game.Id && allExportedGameIds.Contains(id))
                            {
                                relatedPublisherIds.Add(id);
                            }
                        }
                    }
                }
            }
            gameExport.RelatedPublisherGameIds = relatedPublisherIds.Select(id => id.ToString()).ToList();

            var relatedSeriesIds = new HashSet<Guid>();
            if (game.Series != null)
            {
                foreach (var ser in game.Series)
                {
                    if (seriesLookup.TryGetValue(ser.Name, out var ids))
                    {
                        foreach (var id in ids)
                        {
                            if (id != game.Id && allExportedGameIds.Contains(id))
                            {
                                relatedSeriesIds.Add(id);
                            }
                        }
                    }
                }
            }
            gameExport.RelatedSeriesGameIds = relatedSeriesIds.Select(id => id.ToString()).ToList();

            return gameExport;
        }

        // --- HELPER METHODS RE-ADDED ---

        private HltbApi.HltbDataItem GetHltbData(Game game, string hltbPath)
        {
            if (game == null || string.IsNullOrEmpty(hltbPath)) return null;
            string hltbDetailsPath = Path.Combine(hltbPath, "HowLongToBeat", $"{game.Id}.json");
            if (!File.Exists(hltbDetailsPath)) return null;
            try
            {
                var hltbData = Serialization.FromJson<HltbApi.HltbData>(File.ReadAllText(hltbDetailsPath));
                return hltbData?.Items?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to parse HLTB data for game {game.Name} ({game.Id}).");
                return null;
            }
        }

        private bool CheckPrerequisites(out string hltbPath, out bool hltbFound)
        {
            hltbPath = GetEffectivePath(hltbPluginId);
            hltbFound = !string.IsNullOrEmpty(hltbPath);

            if (!hltbFound)
            {
                PlayniteApi.Dialogs.ShowMessage("Could not determine the path for the HowLongToBeat plugin data. Please ensure the plugin is installed.", "PlayniteGo");
                return false;
            }

            return true;
        }

        private string GetEffectivePath(Guid pluginId)
        {
            var plugin = PlayniteApi.Addons.Plugins.FirstOrDefault(p => p.Id == pluginId);
            if (plugin == null) return null;
            return plugin.GetPluginUserDataPath();
        }

        private string ProcessAndCopyLocalImage(string databasePath, string destinationFolder, ImageType type)
        {
            if (string.IsNullOrEmpty(databasePath) || databasePath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                return databasePath;
            }

            string sourcePath = PlayniteApi.Database.GetFullFilePath(databasePath);
            if (!File.Exists(sourcePath))
            {
                logger.Warn($"Image source file not found: {sourcePath}");
                return null;
            }

            string originalFileName = Path.GetFileName(databasePath);
            string newFileName = originalFileName;
            string destinationPath = Path.Combine(destinationFolder, originalFileName);

            if (settings.Settings.ImageExportFormat == "Copy Original")
            {
                try
                {
                    if (!File.Exists(destinationPath) || File.GetLastWriteTimeUtc(sourcePath) > File.GetLastWriteTimeUtc(destinationPath))
                    {
                        File.Copy(sourcePath, destinationPath, true);
                    }
                    return originalFileName;
                }
                catch (Exception ex)
                {
                    logger.Error(ex, $"Failed to copy original file: {sourcePath}");
                    return null;
                }
            }

            try
            {
                using (var image = Image.Load(sourcePath))
                {
                    double targetWidthDimension = (type == ImageType.Cover) ? settings.Settings.CoverWidth : settings.Settings.BackgroundWidth;

                    if (targetWidthDimension <= 0) // Skip resizing if width is not set
                    {
                        if (!File.Exists(destinationPath) || File.GetLastWriteTimeUtc(sourcePath) > File.GetLastWriteTimeUtc(destinationPath))
                        {
                            File.Copy(sourcePath, destinationPath, true);
                        }
                        return originalFileName;
                    }

                    double scaleFactor = targetWidthDimension / image.Width;

                    int targetWidth = (scaleFactor < 1.0) ? (int)(image.Width * scaleFactor) : image.Width;
                    int targetHeight = (scaleFactor < 1.0) ? (int)(image.Height * scaleFactor) : image.Height;

                    if (targetWidth == 0) targetWidth = 1;
                    if (targetHeight == 0) targetHeight = 1;

                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Size = new Size(targetWidth, targetHeight),
                        Mode = SixLabors.ImageSharp.Processing.ResizeMode.Max
                    }));

                    if (settings.Settings.ImageExportFormat == "WebP")
                    {
                        newFileName = Path.ChangeExtension(originalFileName, ".webp");
                        destinationPath = Path.Combine(destinationFolder, newFileName);
                        if (!File.Exists(destinationPath) || File.GetLastWriteTimeUtc(sourcePath) > File.GetLastWriteTimeUtc(destinationPath))
                        {
                            image.SaveAsWebp(destinationPath, new WebpEncoder { Quality = settings.Settings.ImageQuality });
                        }
                    }
                    else // JPEG
                    {
                        newFileName = Path.ChangeExtension(originalFileName, ".jpg");
                        destinationPath = Path.Combine(destinationFolder, newFileName);
                        if (!File.Exists(destinationPath) || File.GetLastWriteTimeUtc(sourcePath) > File.GetLastWriteTimeUtc(destinationPath))
                        {
                            image.SaveAsJpeg(destinationPath, new JpegEncoder { Quality = settings.Settings.ImageQuality });
                        }
                    }
                    return newFileName;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to process image: {sourcePath}. Copying original file as a fallback.");
                try
                {
                    string fallbackDestPath = Path.Combine(destinationFolder, originalFileName);
                    if (!File.Exists(fallbackDestPath) || File.GetLastWriteTimeUtc(sourcePath) > File.GetLastWriteTimeUtc(fallbackDestPath))
                    {
                        File.Copy(sourcePath, fallbackDestPath, true);
                    }
                    return originalFileName;
                }
                catch (Exception copyEx)
                {
                    logger.Error(copyEx, $"Failed to copy original file as fallback: {sourcePath}");
                    return null;
                }
            }
        }

        private void SaveLastExportDate(DateTime date)
        {
            var state = new { LastExportDate = date };
            var filePath = Path.Combine(GetPluginUserDataPath(), stateFileName);
            File.WriteAllText(filePath, Serialization.ToJson(state));
        }

        private DateTime LoadLastExportDate()
        {
            var filePath = Path.Combine(GetPluginUserDataPath(), stateFileName);
            if (!File.Exists(filePath)) return DateTime.MinValue;

            try
            {
                var stateText = File.ReadAllText(filePath);
                if (string.IsNullOrWhiteSpace(stateText)) return DateTime.MinValue;
                var state = Serialization.FromJson<ExportState>(stateText);
                return state.LastExportDate;
            }
            catch (FileNotFoundException)
            {
                return DateTime.MinValue;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to load or parse last export date.");
                return DateTime.MinValue;
            }
        }

        private void SaveExportedIds(HashSet<Guid> ids)
        {
            var filePath = Path.Combine(GetPluginUserDataPath(), exportedIdsFileName);
            File.WriteAllText(filePath, Serialization.ToJson(ids));
        }

        private HashSet<Guid> LoadPreviouslyExportedIds()
        {
            var filePath = Path.Combine(GetPluginUserDataPath(), exportedIdsFileName);
            if (!File.Exists(filePath)) return new HashSet<Guid>();

            try
            {
                var json = File.ReadAllText(filePath);
                if (string.IsNullOrWhiteSpace(json)) return new HashSet<Guid>();
                return Serialization.FromJson<HashSet<Guid>>(json) ?? new HashSet<Guid>();
            }
            catch (FileNotFoundException)
            {
                return new HashSet<Guid>();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to load or parse previously exported IDs.");
                return new HashSet<Guid>();
            }
        }

        // --- PRE-COMPUTATION HELPER METHODS ---

        private static string StripHtml(string html)
        {
            if (string.IsNullOrEmpty(html)) return null;
            return Regex.Replace(html, "<.*?>", string.Empty).Trim();
        }

        private static string FormatPlatformNames(IEnumerable<Platform> platforms)
        {
            if (platforms == null || !platforms.Any()) return null;
            return string.Join(", ", platforms.Select(p => {
                switch (p.Name.ToLowerInvariant())
                {
                    case "pc (windows)": return "PC";
                    case "macintosh": return "Mac";
                    case "pc (linux)": return "Linux";
                    default: return p.Name;
                }
            }));
        }

        private static string FormatFirstGenre(string genre)
        {
            if (string.IsNullOrEmpty(genre)) return null;
            switch (genre)
            {
                case "Real Time Strategy": return "RTS";
                case "Hack and Slash/Beat 'em up": return "Hack and Slash";
                case "Massively Multiplayer": return "MMO";
                default: return genre;
            }
        }

        private static string FormatContributors(IEnumerable<Company> developers, IEnumerable<Company> publishers)
        {
            var devNames = developers?.Select(d => d.Name).ToList() ?? new List<string>();
            var pubNames = publishers?.Select(p => p.Name).ToList() ?? new List<string>();
            var contributors = new List<string>();
            if (devNames.Any()) contributors.Add(string.Join(", ", devNames));
            if (pubNames.Any()) contributors.Add(string.Join(", ", pubNames));
            return string.Join(" • ", contributors);
        }

        private static string FormatPlaytime(ulong playtimeInSeconds)
        {
            if (playtimeInSeconds <= 0) return "";
            return $"{playtimeInSeconds / 3600}h";
        }

        private static string FormatHltbMain(ulong? hltbMainInSeconds)
        {
            if (hltbMainInSeconds == null || hltbMainInSeconds <= 0) return null;
            return $"{hltbMainInSeconds / 3600}h";
        }

        private static string FormatInstallSize(ulong? installSizeInBytes)
        {
            if (installSizeInBytes == null || installSizeInBytes <= 0) return null;
            const long gb = 1073741824;
            const long mb = 1048576;
            if (installSizeInBytes >= gb)
            {
                return string.Format("{0:0.##} GB", (double)installSizeInBytes / gb);
            }
            return string.Format("{0:0} MB", (double)installSizeInBytes / mb);
        }

        private static string FormatShortDate(DateTime? date)
        {
            return date?.ToString("d", CultureInfo.CurrentCulture);
        }

        private string GenerateDiagnosticReport()
        {
            var sb = new StringBuilder();
            var logPath = Path.Combine(PlayniteApi.Paths.ConfigurationPath, "playnite.log");
            var settingsPath = GetPluginUserDataPath();
            var configPath = PlayniteApi.Paths.ConfigurationPath;

            sb.AppendLine("--- PlayniteGo Diagnostic Report ---");
            sb.AppendLine($"Report Generated: {DateTime.Now}");
            sb.AppendLine();

            // --- System & Version Info ---
            sb.AppendLine("## System & Version Info ##");
            try
            {
                sb.AppendLine($"- PlayniteGo Version: {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}");
                sb.AppendLine($"- Playnite Version: {PlayniteApi.ApplicationInfo.ApplicationVersion}");
                sb.AppendLine($"- OS Version: {Environment.OSVersion.VersionString}");
            }
            catch (Exception ex)
            {
                sb.AppendLine("Error getting version info: " + ex.Message);
            }
            sb.AppendLine();

            // --- Plugin Settings ---
            sb.AppendLine("## PlayniteGo Settings ##");
            try
            {
                var settingsJson = Serialization.ToJson(settings.Settings, true);
                sb.AppendLine(settingsJson);
            }
            catch (Exception ex)
            {
                sb.AppendLine("Error getting plugin settings: " + ex.Message);
            }
            sb.AppendLine();

            // --- Installed Plugins ---
            sb.AppendLine("## Installed Plugins ##");
            try
            {
                sb.AppendLine("## Installed Plugins ##");
                
                // --- Enabled Plugins ---
                // The API in this environment appears to have non-standard types.
                // We will print the information we can reliably get.
                sb.AppendLine("\n### Enabled Plugins ###");
                foreach (var plugin in PlayniteApi.Addons.Plugins)
                {
                    sb.AppendLine($"- Type: {plugin.GetType().FullName}, ID: {plugin.Id}");
                }

                // --- Disabled Plugins ---
                if (PlayniteApi.Addons.DisabledAddons is System.Collections.Generic.IEnumerable<string> disabledAddonIds && disabledAddonIds.Any())
                {
                    sb.AppendLine("\n### Disabled Plugins ###");
                    foreach (var idString in disabledAddonIds)
                    {
                        sb.AppendLine($"- ID: {idString} (DISABLED)");
                    }
                }

                // --- All Addons (as reported by API) ---
                 if (PlayniteApi.Addons.Addons is System.Collections.Generic.IEnumerable<string> allAddonIds && allAddonIds.Any())
                {
                    sb.AppendLine("\n### All Addon IDs ###");
                    foreach (var idString in allAddonIds)
                    {
                        sb.AppendLine($"- ID: {idString}");
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine("Error getting installed plugins: " + ex.Message);
            }
            sb.AppendLine();

            // --- Playnite Config ---
            sb.AppendLine("## Playnite Config ##");
            try
            {
                var configFilePath = Path.Combine(configPath, "config.json");
                if (File.Exists(configFilePath))
                {
                    sb.AppendLine(File.ReadAllText(configFilePath));
                }
                else
                {
                    sb.AppendLine("config.json not found.");
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine("Error reading Playnite config: " + ex.Message);
            }
            sb.AppendLine();

            // --- Recent Log Entries ---
            sb.AppendLine("--- Recent Playnite Log Entries ---");
            try
            {
                if (File.Exists(logPath))
                {
                    var lastLines = File.ReadLines(logPath).Reverse().Take(200).Reverse();
                    foreach (var line in lastLines)
                    {
                        sb.AppendLine(line);
                    }
                }
                else
                {
                    sb.AppendLine("playnite.log file not found.");
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"Error reading log file: {ex.Message}");
            }

            return sb.ToString();
        }


        public override ISettings GetSettings(bool firstRunSettings) => settings;

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new PlayniteGoSettingsView(settings);
        }
    }
}