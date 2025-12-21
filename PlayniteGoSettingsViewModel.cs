using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PlayniteGo
{
    public class PlayniteGoSettingsViewModel : ObservableObject, ISettings
    {
        private readonly PlayniteGo plugin;
        private PlayniteGoSettings settings;
        public PlayniteGoSettings Settings { get => settings; set => SetValue(ref settings, value); }

        private PlayniteGoSettings editingClone;

        // UI-related properties
        public List<string> ImageExportFormatOptions { get; } = new List<string> { "WebP", "JPEG", "Copy Original" };

        public PlayniteGoSettingsViewModel(PlayniteGo plugin)
        {
            this.plugin = plugin;
            this.settings = new PlayniteGoSettings(plugin);
            // Path detection logic is now moved to BeginEdit()
        }

        // ISettings implementation
        public void BeginEdit()
        {
            editingClone = Serialization.GetClone(settings);
        }

        public void CancelEdit()
        {
            Settings = editingClone;
        }

        public void EndEdit()
        {
            plugin.SavePluginSettings(Settings);
        }

        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();

            if (Settings.ImageQuality < 1 || Settings.ImageQuality > 100)
            {
                errors.Add("Image Quality must be between 1 and 100.");
            }

            if (Settings.CoverWidth < 0)
            {
                errors.Add("Max Cover Image Width cannot be negative.");
            }

            if (Settings.BackgroundWidth < 0)
            {
                errors.Add("Max Background Image Width cannot be negative.");
            }

            return errors.Count == 0;
        }
    }
}