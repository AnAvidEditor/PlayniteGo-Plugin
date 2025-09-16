using Playnite.SDK.Data;
using Playnite.SDK.Plugins;
using System.Collections.Generic;

namespace PlayniteGo
{
    public class PlayniteGoSettings : ObservableObject
    {
        private readonly PlayniteGo plugin;

        // Settings Properties
        private string imageExportFormat = "WebP";
        public string ImageExportFormat { get => imageExportFormat; set => SetValue(ref imageExportFormat, value); }

        private int imageQuality = 75;
        public int ImageQuality { get => imageQuality; set => SetValue(ref imageQuality, value); }

        private int coverWidth = 600;
        public int CoverWidth { get => coverWidth; set => SetValue(ref coverWidth, value); }

        private int backgroundWidth = 1280;
        public int BackgroundWidth { get => backgroundWidth; set => SetValue(ref backgroundWidth, value); }

        // Parameterless constructor is required for serialization.
        public PlayniteGoSettings()
        {
        }

        public PlayniteGoSettings(PlayniteGo plugin)
        {
            this.plugin = plugin;

            // Load saved settings.
            var savedSettings = plugin.LoadPluginSettings<PlayniteGoSettings>();

            // Load properties from saved settings.
            if (savedSettings != null)
            {
                ImageExportFormat = savedSettings.ImageExportFormat;
                ImageQuality = savedSettings.ImageQuality;
                CoverWidth = savedSettings.CoverWidth;
                BackgroundWidth = savedSettings.BackgroundWidth;
            }
        }
    }
}