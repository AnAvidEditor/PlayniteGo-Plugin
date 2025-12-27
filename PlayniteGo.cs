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
using System.Net;
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

    public class ExportPayload
    {
        // --- ✅ NEW: ADDED SCHEMA VERSION ---
        public int SchemaVersion { get; set; } = 1;
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
        public ReleaseDate? ReleaseDate { get; set; }
        public int? UserScore { get; set; }
        public string Notes { get; set; }
        public ulong? InstallSize { get; set; }

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
        public string DisplayPlatformNames { get; set; }
        public string DisplayFirstGenre { get; set; }
        public string DisplayContributors { get; set; }
        public string DisplayPlaytime { get; set; }
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


    public class PlayniteGo : GenericPlugin
    {
        public class ExportState
        {
            public DateTime LastExportDate { get; set; }
        }

        public class ImageCacheSettings
        {
            public string ImageExportFormat { get; set; }
            public int CoverWidth { get; set; }
            public int BackgroundWidth { get; set; }
            public int ImageQuality { get; set; }
        }

        private static readonly ILogger logger = LogManager.GetLogger();
        private PlayniteGoSettingsViewModel settings;
        public override Guid Id { get; } = Guid.Parse("af7bd5e5-0ae0-4276-bb2a-cdf7fadea92e");
        private const string exportedIdsFileName = "exportedGameIds.json";
        private const string stateFileName = "exportState.json";
        private enum ImageType { Cover, Background }


        private const double SecondsInHour = 3600.0;
        private const double BytesInGigabyte = 1073741824.0;
        private const double BytesInMegabyte = 1048576.0;

        private static readonly string[] ControllerKeywords = { "controller" };
        private static readonly string[] VrKeywords = { "vr", "virtual reality", "virtual-reality" };
        private static readonly string[] UltrawideKeywords = { "ultrawide", "ultra-wide" };
        private static readonly string[] HdrKeywords = { "hdr" };

        private static bool CheckSupport(Game g, string[] keywords)
        {
            if (g.Features?.Any(f => keywords.Any(k => f.Name.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0)) == true) return true;
            if (g.Tags?.Any(t => keywords.Any(k => t.Name.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0)) == true) return true;
            return false;
        }

        public PlayniteGo(IPlayniteAPI api) : base(api)
        {
            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };

            // This single line creates the ViewModel, which in turn handles creating and loading the settings.
            settings = new PlayniteGoSettingsViewModel(this);
        }

        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
        {
            var menuItems = new List<MainMenuItem>
            {
                new MainMenuItem
                {
                    Description = "Export Full Library (Destructive)",
                    MenuSection = "@PlayniteGo",
                    Action = (actionArgs) => PerformFullExport()
                },
                new MainMenuItem
                {
                    Description = "Export Changes (Since Last Export)",
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
                var allGames = PlayniteApi.Database.Games.ToList();
                if (!allGames.Any())
                {
                    PlayniteApi.Dialogs.ShowMessage("No games in library to export.", "PlayniteGo Export");
                    return;
                }

                var result = PlayniteApi.Dialogs.SaveFile("Zip Files (*.zip)|*.zip");
                if (string.IsNullOrEmpty(result)) return;

                ExecuteFullExport(allGames, result, "Full library export...");
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

                ExecuteIncrementalExport(gamesToUpdate, deletedGameIds, result, "Syncing library changes...");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to perform incremental export.");
                PlayniteApi.Dialogs.ShowErrorMessage("An unexpected error occurred during the sync. Please check the log file for details.", "PlayniteGo Error");
            }
        }

        private void ExecuteFullExport(List<Game> gamesToProcess, string exportZipPath, string progressMessage)
        {
            var allGameIds = Enumerable.ToHashSet(gamesToProcess.Select(g => g.Id));
            var payload = new ExportPayload { Games = new List<GameExport>() };
            var exportDate = DateTime.UtcNow;

            bool success = ProcessAndZip(gamesToProcess, payload, exportZipPath, allGameIds, progressMessage, exportDate);
            if (success)
            {
                PlayniteApi.Dialogs.ShowMessage($"Successfully exported {payload.Games.Count} games.", "Export Complete");
            }
        }

        private void ExecuteIncrementalExport(List<Game> gamesToProcess, List<Guid> deletedGameIds, string exportZipPath, string progressMessage)
        {
            var currentIds = Enumerable.ToHashSet(PlayniteApi.Database.Games.Select(g => g.Id));
            var payload = new ExportPayload
            {
                UpdatedGames = new List<GameExport>(),
                DeletedGameIds = deletedGameIds
            };
            var exportDate = DateTime.UtcNow;

            bool success = ProcessAndZip(gamesToProcess, payload, exportZipPath, currentIds, progressMessage, exportDate);
            if (success)
            {
                PlayniteApi.Dialogs.ShowMessage($"Successfully synced {payload.UpdatedGames.Count} updates and {payload.DeletedGameIds.Count} deletions.", "Sync Complete");
            }
        }

        private bool ProcessAndZip(List<Game> gamesToProcess, ExportPayload payload, string exportZipPath, HashSet<Guid> currentIds, string progressMessage, DateTime exportDate)
        {
            bool wasSuccess = false;
            var tempDir = Path.Combine(Path.GetTempPath(), "PlayniteExport_" + Guid.NewGuid());
            var imagesDir = Path.Combine(tempDir, "images");

            try
            {
                Directory.CreateDirectory(imagesDir);

                var imageCacheDir = Path.Combine(GetPluginUserDataPath(), "ImageCache");
                var cacheSettingsFile = Path.Combine(imageCacheDir, "cache.settings.json");
                Directory.CreateDirectory(imageCacheDir);

                var currentCacheSettings = new ImageCacheSettings
                {
                    ImageExportFormat = settings.Settings.ImageExportFormat,
                    CoverWidth = settings.Settings.CoverWidth,
                    BackgroundWidth = settings.Settings.BackgroundWidth,
                    ImageQuality = settings.Settings.ImageQuality
                };

                logger.Info($"Starting Export. Settings: Format={currentCacheSettings.ImageExportFormat}, CoverW={currentCacheSettings.CoverWidth}, BgW={currentCacheSettings.BackgroundWidth}, Quality={currentCacheSettings.ImageQuality}");

                if (File.Exists(cacheSettingsFile))
                {
                    try
                    {
                        var savedSettings = Serialization.FromJson<ImageCacheSettings>(File.ReadAllText(cacheSettingsFile));
                        if (!Serialization.Equals(savedSettings, currentCacheSettings))
                        {
                            logger.Info("Image settings changed. Invalidating cache.");
                            Directory.Delete(imageCacheDir, true);
                            Directory.CreateDirectory(imageCacheDir);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Failed to read or compare image cache settings. Invalidating cache.");
                        Directory.Delete(imageCacheDir, true);
                        Directory.CreateDirectory(imageCacheDir);
                    }
                }

                PlayniteApi.Dialogs.ActivateGlobalProgress(args =>
                {
                    var allGamesInLibrary = PlayniteApi.Database.Games.ToList();

                    // --- BUG FIX #2: Refactored stats generation to be complete for all categories ---
                    args.Text = $"Analyzing {allGamesInLibrary.Count} games for stats and filters...";
                    var playedGames = allGamesInLibrary.Where(g => g.Playtime > 0).ToList();
                    var unplayedGames = allGamesInLibrary.Where(g => g.Playtime <= 0).ToList();

                    // --- Lookups for related games, built once from the full library ---
                    var developerLookup = new Dictionary<string, List<Guid>>();
                    var publisherLookup = new Dictionary<string, List<Guid>>();
                    var seriesLookup = new Dictionary<string, List<Guid>>();
                    foreach (var game in allGamesInLibrary)
                    {
                        if (game.Developers != null) foreach (var d in game.Developers) { if (!developerLookup.ContainsKey(d.Name)) developerLookup[d.Name] = new List<Guid>(); developerLookup[d.Name].Add(game.Id); }
                        if (game.Publishers != null) foreach (var p in game.Publishers) { if (!publisherLookup.ContainsKey(p.Name)) publisherLookup[p.Name] = new List<Guid>(); publisherLookup[p.Name].Add(game.Id); }
                        if (game.Series != null) foreach (var s in game.Series) { if (!seriesLookup.ContainsKey(s.Name)) seriesLookup[s.Name] = new List<Guid>(); seriesLookup[s.Name].Add(game.Id); }
                    }

                    // --- Generate complete stats for each category ---
                    var allGamesStats = GenerateSummaryStatsForCollection(allGamesInLibrary);
                    var playedGamesStats = GenerateSummaryStatsForCollection(playedGames);
                    var unplayedGamesStats = GenerateSummaryStatsForCollection(unplayedGames);

                    payload.Stats = new ExportedStats
                    {
                        AllGames = allGamesStats,
                        PlayedGames = playedGamesStats,
                        UnplayedGames = unplayedGamesStats,
                    };

                    // --- Backlog Calculation ---
                    payload.Stats.Backlog = new BacklogStats
                    {
                        TotalUnplayedGames = unplayedGames.Count,
                        TotalUnplayedHours = 0,
                        CompletionDate = null,
                        LongestBacklogGame = null,
                        ShortestBacklogGame = null
                    };

                    // --- Filter Options Calculation (based on All Games) ---
                    payload.FilterOptions = new ExportedFilterOptions
                    {
                        Sources = allGamesStats.AllSources.Select(d => d.Name).OrderBy(n => n).ToList(),
                        CompletionStatuses = allGamesStats.CompletionStatusCounts.Select(d => d.Name).OrderBy(n => n).ToList(),
                        Platforms = allGamesStats.AllPlatforms.Select(d => d.Name).OrderBy(n => n).ToList(),
                        Genres = allGamesInLibrary.SelectMany(g => g.Genres ?? new List<Genre>()).Select(g => g.Name).Distinct().OrderBy(n => n).ToList(),
                        Developers = allGamesInLibrary.SelectMany(g => g.Developers ?? new List<Company>()).Select(d => d.Name).Distinct().OrderBy(n => n).ToList(),
                        Publishers = allGamesInLibrary.SelectMany(g => g.Publishers ?? new List<Company>()).Select(p => p.Name).Distinct().OrderBy(n => n).ToList(),
                        Features = allGamesInLibrary.SelectMany(g => g.Features ?? new List<GameFeature>()).Select(f => f.Name).Distinct().OrderBy(n => n).ToList(),
                        Tags = allGamesInLibrary.SelectMany(g => g.Tags ?? new List<Tag>()).Select(t => t.Name).Distinct().OrderBy(n => n).ToList(),
                        Series = allGamesInLibrary.SelectMany(g => g.Series ?? new List<Series>()).Select(s => s.Name).Distinct().OrderBy(n => n).ToList(),
                        AgeRatings = allGamesInLibrary.SelectMany(g => g.AgeRatings ?? new List<AgeRating>()).Select(a => a.Name).Distinct().OrderBy(n => n).ToList(),
                        Regions = allGamesInLibrary.SelectMany(g => g.Regions ?? new List<Region>()).Select(r => r.Name).Distinct().OrderBy(n => n).ToList(),
                        Categories = allGamesInLibrary.SelectMany(g => g.Categories ?? new List<Category>()).Select(c => c.Name).Distinct().OrderBy(n => n).ToList()
                    };

                    ulong maxPlaytimeSeconds = allGamesInLibrary.Any() ? allGamesInLibrary.Max(g => g.Playtime) : 0;
                    ulong maxInstallSizeBytes = allGamesInLibrary.Any() ? allGamesInLibrary.Max(g => g.InstallSize ?? 0) : 0;
                    var gamesWithReleaseYear = allGamesInLibrary.Where(g => g.ReleaseDate != null).Select(g => g.ReleaseDate.Value.Year).ToList();

                    int maxPlaytimeHours = maxPlaytimeSeconds > 0 ? (int)Math.Ceiling(maxPlaytimeSeconds / SecondsInHour) : 0;
                    int maxInstallSizeGb = maxInstallSizeBytes > 0 ? (int)Math.Ceiling(maxInstallSizeBytes / BytesInGigabyte) : 0;
                    int minReleaseYear = gamesWithReleaseYear.Any() ? gamesWithReleaseYear.Min() : 1990;
                    int maxReleaseYear = gamesWithReleaseYear.Any() ? gamesWithReleaseYear.Max() : DateTime.Now.Year;

                    payload.FilterOptions.PlaytimeRange = new RangeData { LowerBound = 0, UpperBound = Math.Max(1, maxPlaytimeHours) };
                    payload.FilterOptions.InstallSizeRange = new RangeData { LowerBound = 0, UpperBound = Math.Max(1, maxInstallSizeGb) };
                    payload.FilterOptions.ReleaseYearRange = new RangeData { LowerBound = minReleaseYear, UpperBound = maxReleaseYear };

                    // --- Game Data Processing ---
                    args.ProgressMaxValue = gamesToProcess.Count;
                    args.IsIndeterminate = false;
                    var processedGames = new ConcurrentBag<GameExport>();
                    int progress = 0;

                    Parallel.ForEach(gamesToProcess, (game) =>
                    {
                        if (args.CancelToken.IsCancellationRequested) return;
                        try 
                        {
                            var gameExport = CreateGameExport(game, imagesDir, imageCacheDir, developerLookup, publisherLookup, seriesLookup, currentIds);
                            if (gameExport != null) processedGames.Add(gameExport);
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex, $"Failed to export game: {game.Name}");
                        }

                        Interlocked.Increment(ref progress);
                        args.CurrentProgressValue = progress;
                        args.Text = $"Processing: {game.Name} ({progress}/{gamesToProcess.Count})";
                    });

                    if (args.CancelToken.IsCancellationRequested) return;

                    logger.Info($"Processed {processedGames.Count} games out of {gamesToProcess.Count}.");

                    if (payload.Games != null) payload.Games = processedGames.OrderBy(g => g.Name).ToList();
                    if (payload.UpdatedGames != null) payload.UpdatedGames = processedGames.OrderBy(g => g.Name).ToList();

                    var jsonContent = Serialization.ToJson(payload, true);
                    var obfuscatedContent = ObfuscateJson(jsonContent);
                    File.WriteAllText(Path.Combine(tempDir, "library.json"), obfuscatedContent);

                    if (args.CancelToken.IsCancellationRequested) return;

                    args.Text = "Compressing files...";
                    args.IsIndeterminate = true;
                    if (File.Exists(exportZipPath)) File.Delete(exportZipPath);
                    ZipFile.CreateFromDirectory(tempDir, exportZipPath, CompressionLevel.Optimal, false);

                    if (args.CancelToken.IsCancellationRequested) return;

                    SaveLastExportDate(exportDate);
                    SaveExportedIds(currentIds);
                    File.WriteAllText(cacheSettingsFile, Serialization.ToJson(currentCacheSettings));
                    wasSuccess = true;
                }, new GlobalProgressOptions(progressMessage, true) { IsIndeterminate = true });
            }
            catch (IOException ioEx)
            {
                logger.Error(ioEx, "Export failed due to a file system error.");
                PlayniteApi.MainView.UIDispatcher.Invoke(() => {
                    PlayniteApi.Dialogs.ShowErrorMessage(
                        $"Export failed. A file system error occurred, which could be due to a lack of disk space or an issue with the destination folder.\n\nDetails: {ioEx.Message}",
                        "File Error");
                });
                wasSuccess = false;
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
            return wasSuccess;
        }

        private GameExport CreateGameExport(Game game, string tempImagesDir, string imageCacheDir,
            Dictionary<string, List<Guid>> developerLookup,
            Dictionary<string, List<Guid>> publisherLookup,
            Dictionary<string, List<Guid>> seriesLookup,
            HashSet<Guid> allExportedGameIds)
        {
            var gameExport = new GameExport
            {
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
                ReleaseDate = game.ReleaseDate,
                UserScore = game.UserScore,
                Notes = game.Notes,
                InstallSize = game.InstallSize,
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
                CoverImagePath = ProcessAndCopyLocalImage(game.CoverImage, tempImagesDir, imageCacheDir, ImageType.Cover),
                BackgroundImagePath = ProcessAndCopyLocalImage(game.BackgroundImage, tempImagesDir, imageCacheDir, ImageType.Background),
            };

            gameExport.PlainTextDescription = StripHtml(game.Description);
            gameExport.ReleaseYear = game.ReleaseDate?.Year;
            gameExport.DisplayPlatformNames = FormatPlatformNames(game.Platforms);
            gameExport.DisplayFirstGenre = FormatFirstGenre(game.Genres);
            gameExport.DisplayContributors = FormatContributors(game.Developers, game.Publishers);
            gameExport.DisplayPlaytime = FormatPlaytime(game.Playtime);
            gameExport.DisplayInstallSize = FormatInstallSize(game.InstallSize);
            gameExport.DisplayAddedDate = FormatShortDate(game.Added);
            gameExport.DisplayLastPlayed = FormatShortDate(game.LastActivity);
            gameExport.HasControllerSupport = CheckSupport(game, ControllerKeywords);
            gameExport.HasVRSupport = CheckSupport(game, VrKeywords);
            gameExport.HasUltrawideSupport = CheckSupport(game, UltrawideKeywords);
            gameExport.HasHDRSupport = CheckSupport(game, HdrKeywords);

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

        private string ProcessAndCopyLocalImage(string databasePath, string tempImagesDir, string imageCacheDir, ImageType type)
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
            string targetFileName = originalFileName;

            if (settings.Settings.ImageExportFormat == "WebP")
            {
                targetFileName = Path.ChangeExtension(originalFileName, ".webp");
            }
            else if (settings.Settings.ImageExportFormat == "JPEG")
            {
                targetFileName = Path.ChangeExtension(originalFileName, ".jpg");
            }

            string cachedFilePath = Path.Combine(imageCacheDir, targetFileName);
            string finalExportPath = Path.Combine(tempImagesDir, targetFileName);

            if (File.Exists(cachedFilePath) && new FileInfo(cachedFilePath).Length > 0 && File.GetLastWriteTimeUtc(sourcePath) <= File.GetLastWriteTimeUtc(cachedFilePath))
            {
                try
                {
                    File.Copy(cachedFilePath, finalExportPath, true);
                    return targetFileName;
                }
                catch (Exception ex)
                {
                    logger.Error(ex, $"Failed to copy from cache. File: {cachedFilePath}");
                }
            }

            try
            {
                if (settings.Settings.ImageExportFormat == "Copy Original")
                {
                    File.Copy(sourcePath, cachedFilePath, true);
                }
                else
                {
                    using (var image = Image.Load(sourcePath))
                    {
                        double targetWidthDimension = (type == ImageType.Cover) ? settings.Settings.CoverWidth : settings.Settings.BackgroundWidth;

                        if (targetWidthDimension > 0)
                        {
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
                        }

                        if (settings.Settings.ImageExportFormat == "WebP")
                        {
                            image.SaveAsWebp(cachedFilePath, new WebpEncoder { Quality = settings.Settings.ImageQuality });
                        }
                        else // JPEG
                        {
                            image.SaveAsJpeg(cachedFilePath, new JpegEncoder { Quality = settings.Settings.ImageQuality });
                        }
                    }
                }

                File.Copy(cachedFilePath, finalExportPath, true);
                return targetFileName;
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to process and cache image: {sourcePath}.");
                return null;
            }
        }

        // --- BUG FIX #2 (REFACTOR): New helper method to generate complete stats for any game collection ---
        private SummaryStats GenerateSummaryStatsForCollection(List<Game> games)
        {
            if (games == null || !games.Any()) return new SummaryStats { TotalGames = 0, GamesPlayedCount = 0, TotalPlaytimeHours = 0 };

            // --- Dictionaries for counting categories ---
            var sourceCounts = new Dictionary<string, int>();
            var completionStatusCounts = new Dictionary<string, int>();
            var platformCounts = new Dictionary<string, int>();
            var genreCounts = new Dictionary<string, int>();
            var developerCounts = new Dictionary<string, int>();
            var publisherCounts = new Dictionary<string, int>();
            var featureCounts = new Dictionary<string, int>();
            var tagCounts = new Dictionary<string, int>();

            void IncrementCount(Dictionary<string, int> dict, string key)
            {
                if (string.IsNullOrEmpty(key)) return;
                dict.TryGetValue(key, out int currentCount);
                dict[key] = currentCount + 1;
            }

            // --- High/Low trackers ---
            long totalPlaytimeSeconds = 0;
            Game mostPlayed = null;
            Game leastPlayed = null;
            Game oldestRelease = null;
            Game newestRelease = null;
            Game oldestAdded = null;
            Game newestAdded = null;

            // --- Single loop to gather all data ---
            foreach (var game in games)
            {
                // Basic stats
                totalPlaytimeSeconds += (long)game.Playtime;
                if (game.Playtime > 0)
                {
                    if (mostPlayed == null || game.Playtime > mostPlayed.Playtime) mostPlayed = game;
                    if (leastPlayed == null || game.Playtime < leastPlayed.Playtime) leastPlayed = game;
                }
                if (game.ReleaseDate != null)
                {
                    if (oldestRelease == null || game.ReleaseDate.Value.CompareTo(oldestRelease.ReleaseDate.Value) < 0) oldestRelease = game;
                    if (newestRelease == null || game.ReleaseDate.Value.CompareTo(newestRelease.ReleaseDate.Value) > 0) newestRelease = game;
                }
                if (game.Added != null)
                {
                    if (oldestAdded == null || game.Added.Value < oldestAdded.Added.Value) oldestAdded = game;
                    if (newestAdded == null || game.Added.Value > newestAdded.Added.Value) newestAdded = game;
                }

                // Category counts
                if (game.Source != null) IncrementCount(sourceCounts, game.Source.Name);
                IncrementCount(completionStatusCounts, game.CompletionStatus?.Name ?? "Not Set");
                if (game.Platforms != null) foreach (var p in game.Platforms) IncrementCount(platformCounts, p.Name);
                if (game.Genres != null) foreach (var g in game.Genres) IncrementCount(genreCounts, g.Name);
                if (game.Developers != null) foreach (var d in game.Developers) IncrementCount(developerCounts, d.Name);
                if (game.Publishers != null) foreach (var p in game.Publishers) IncrementCount(publisherCounts, p.Name);
                if (game.Features != null) foreach (var f in game.Features) IncrementCount(featureCounts, f.Name);
                if (game.Tags != null) foreach (var t in game.Tags) IncrementCount(tagCounts, t.Name);
            }

            Func<Dictionary<string, int>, List<CountData>> toCountData = (dict) => dict.Select(kvp => new CountData { Name = kvp.Key, Count = kvp.Value }).OrderByDescending(x => x.Count).ToList();

            return new SummaryStats
            {
                TotalGames = games.Count,
                GamesPlayedCount = games.Count(g => g.Playtime > 0),
                TotalPlaytimeHours = (int)(totalPlaytimeSeconds / SecondsInHour),
                MostPlayedGame = mostPlayed == null ? null : new GameTime { Name = mostPlayed.Name, Hours = (int)(mostPlayed.Playtime / SecondsInHour) },
                LeastPlayedGame = leastPlayed == null ? null : new GameTime { Name = leastPlayed.Name, Hours = (int)(leastPlayed.Playtime / SecondsInHour) },

                // Highs & Lows
                TopCriticRated = games.Where(g => g.CriticScore != null && g.CriticScore > 0).OrderByDescending(g => g.CriticScore).Take(5).Select(g => new GameScore { Name = g.Name, Score = g.CriticScore.Value }).ToList(),
                BottomCriticRated = games.Where(g => g.CriticScore != null && g.CriticScore > 0).OrderBy(g => g.CriticScore).Take(5).Select(g => new GameScore { Name = g.Name, Score = g.CriticScore.Value }).ToList(),
                TopCommunityRated = games.Where(g => g.CommunityScore != null && g.CommunityScore > 0).OrderByDescending(g => g.CommunityScore).Take(5).Select(g => new GameScore { Name = g.Name, Score = g.CommunityScore.Value }).ToList(),
                BottomCommunityRated = games.Where(g => g.CommunityScore != null && g.CommunityScore > 0).OrderBy(g => g.CommunityScore).Take(5).Select(g => new GameScore { Name = g.Name, Score = g.CommunityScore.Value }).ToList(),
                TopUserRated = games.Where(g => g.UserScore != null && g.UserScore > 0).OrderByDescending(g => g.UserScore).Take(5).Select(g => new GameScore { Name = g.Name, Score = g.UserScore.Value }).ToList(),
                BottomUserRated = games.Where(g => g.UserScore != null && g.UserScore > 0).OrderBy(g => g.UserScore).Take(5).Select(g => new GameScore { Name = g.Name, Score = g.UserScore.Value }).ToList(),
                OldestRelease = oldestRelease == null ? null : new GameScore { Name = oldestRelease.Name, Score = oldestRelease.ReleaseDate.Value.Year },
                NewestRelease = newestRelease == null ? null : new GameScore { Name = newestRelease.Name, Score = newestRelease.ReleaseDate.Value.Year },
                OldestAdded = oldestAdded == null ? null : new GameScore { Name = oldestAdded.Name, Score = oldestAdded.Added.Value.Year },
                NewestAdded = newestAdded == null ? null : new GameScore { Name = newestAdded.Name, Score = newestAdded.Added.Value.Year },

                // Count Data
                GamesByDecade = games.Where(g => g.ReleaseDate != null).GroupBy(g => (g.ReleaseDate.Value.Year / 10) * 10).Select(g => new CountData { Name = $"{g.Key}s", Count = g.Count() }).OrderBy(x => x.Name).ToList(),
                AllSources = toCountData(sourceCounts),
                AllPlatforms = toCountData(platformCounts),
                CompletionStatusCounts = toCountData(completionStatusCounts),
                TopGenres = toCountData(genreCounts).Take(5).ToList(),
                TopDevelopers = toCountData(developerCounts).Take(5).ToList(),
                TopPublishers = toCountData(publisherCounts).Take(5).ToList(),
                TopTags = toCountData(tagCounts).Take(5).ToList(),
                TopFeatures = toCountData(featureCounts).Take(5).ToList()
            };
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

        private static string StripHtml(string html)
        {
            if (string.IsNullOrEmpty(html)) return null;

            string text = html;
            text = Regex.Replace(text, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"</(p|div|h[1-6]|li)>", "\n", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"<[^>]+>", string.Empty);
            text = WebUtility.HtmlDecode(text);
            text = Regex.Replace(text, @"(\s*\n\s*)+", "\n");
            text = text.Trim();

            return text;
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

        private static string AbbreviateGenre(string genre)
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

        private static string FormatFirstGenre(IEnumerable<Genre> genres)
        {
            if (genres == null) return null;
            var genreList = genres.ToList();
            if (!genreList.Any()) return null;

            var bestGenre = genreList.FirstOrDefault(g => g.Name != "2D" && g.Name != "3D");
            var genreToDisplay = bestGenre ?? genreList.FirstOrDefault();
            return AbbreviateGenre(genreToDisplay?.Name);
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
            return $"{(int)(playtimeInSeconds / SecondsInHour)}h";
        }

        private static string FormatInstallSize(ulong? installSizeInBytes)
        {
            if (installSizeInBytes == null || installSizeInBytes <= 0) return null;
            if (installSizeInBytes >= BytesInGigabyte)
            {
                return string.Format("{0:0.##} GB", (double)installSizeInBytes / BytesInGigabyte);
            }
            return string.Format("{0:0} MB", (double)installSizeInBytes / BytesInMegabyte);
        }

        private static string FormatShortDate(DateTime? date)
        {
            return date?.ToString("d", CultureInfo.CurrentCulture);
        }

        private string ObfuscateJson(string json)
        {
            // 1. Convert the JSON string to a byte array
            var bytes = Encoding.UTF8.GetBytes(json);

            // 2. Compress the byte array using an in-memory Gzip stream
            using (var outputStream = new MemoryStream())
            {
                using (var gzipStream = new GZipStream(outputStream, CompressionMode.Compress))
                {
                    gzipStream.Write(bytes, 0, bytes.Length);
                }
                var compressedBytes = outputStream.ToArray();

                // 3. Convert the compressed binary data to a Base64 string and return it
                return Convert.ToBase64String(compressedBytes);
            }
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
            sb.AppendLine("## Installed Plugins ##");
            try
            {
                sb.AppendLine("## Installed Plugins ##");
                sb.AppendLine("\n### Enabled Plugins ###");
                foreach (var plugin in PlayniteApi.Addons.Plugins)
                {
                    sb.AppendLine($"- Type: {plugin.GetType().FullName}, ID: {plugin.Id}");
                }
                if (PlayniteApi.Addons.DisabledAddons is System.Collections.Generic.IEnumerable<string> disabledAddonIds && disabledAddonIds.Any())
                {
                    sb.AppendLine("\n### Disabled Plugins ###");
                    foreach (var idString in disabledAddonIds)
                    {
                        sb.AppendLine($"- ID: {idString} (DISABLED)");
                    }
                }
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

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }
        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new PlayniteGoSettingsView();
        }
    }
}