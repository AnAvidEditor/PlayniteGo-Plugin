using Playnite.SDK.Data;
using System.Collections.Generic;

namespace PlayniteGo
{
    public class PlayniteGoSettings : ObservableObject
    {
        // Existing settings
        public string ExtraMetadataFolderPath { get; set; } = string.Empty;
        public string HowLongToBeatFolderPath { get; set; } = string.Empty;

        // --- NEW: Flexible Export Settings ---
        public string ImageExportFormat { get; set; } = "WebP";
        public int ImageQuality { get; set; } = 75;
        public int CoverWidth { get; set; } = 600;
        public int BackgroundWidth { get; set; } = 1280;

        public string DebugMessage { get; set; } = "Defaults applied via property initializer.";

        // Parameterless constructor for default values
        public PlayniteGoSettings()
        {
            ImageExportFormat = "WebP";
            ImageQuality = 75;
            CoverWidth = 600;
            BackgroundWidth = 1280;
            DebugMessage = "Defaults applied via constructor.";
        }
    }
}