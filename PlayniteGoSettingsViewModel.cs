using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Playnite.SDK.Plugins;

namespace PlayniteGo
{
    public class PlayniteGoSettingsViewModel : ObservableObject, ISettings
    {
        private readonly PlayniteGo plugin;
        private PlayniteGoSettings settings;
        public PlayniteGoSettings Settings
        {
            get => settings;
            set
            {
                settings = value;
                OnPropertyChanged();
            }
        }

        private PlayniteGoSettings editingClone;

        // Properties for Data Binding in the View
        public string AutoDetectedHltbPath { get; private set; }
        public List<string> ImageExportFormatOptions { get; } = new List<string> { "WebP", "JPEG", "Copy Original" };

        public string ImageExportFormat
        {
            get => Settings.ImageExportFormat;
            set
            {
                Settings.ImageExportFormat = value;
                OnPropertyChanged();
            }
        }

        public int ImageQuality
        {
            get => Settings.ImageQuality;
            set
            {
                Settings.ImageQuality = value;
                OnPropertyChanged();
            }
        }

        public int CoverWidth
        {
            get => Settings.CoverWidth;
            set
            {
                Settings.CoverWidth = value;
                OnPropertyChanged();
            }
        }

        public int BackgroundWidth
        {
            get => Settings.BackgroundWidth;
            set
            {
                Settings.BackgroundWidth = value;
                OnPropertyChanged();
            }
        }

        public string DebugMessage
        {
            get => Settings.DebugMessage;
            set
            {
                Settings.DebugMessage = value;
                OnPropertyChanged();
            }
        }


        public PlayniteGoSettingsViewModel(PlayniteGo plugin)
        {
            this.plugin = plugin;
            var savedSettings = plugin.LoadPluginSettings<PlayniteGoSettings>();
            Settings = savedSettings ?? new PlayniteGoSettings();

            string hltbBasePath = GetAutoDetectedPath("HowLongToBeat", PlayniteGo.hltbPluginId);
            if (!string.IsNullOrEmpty(hltbBasePath) && Directory.Exists(hltbBasePath))
            {
                string potentialSubfolderPath = Path.Combine(hltbBasePath, "HowLongToBeat");
                if (Directory.Exists(potentialSubfolderPath))
                {
                    AutoDetectedHltbPath = potentialSubfolderPath;
                }
                else
                {
                    AutoDetectedHltbPath = hltbBasePath;
                }
            }
            else
            {
                AutoDetectedHltbPath = "HowLongToBeat plugin not found or path not configured.";
            }
        }

        private string GetAutoDetectedPath(string pluginName, Guid pluginId)
        {
            var foundPlugin = plugin.PlayniteApi.Addons.Plugins.FirstOrDefault(p => p.Id == pluginId);
            if (foundPlugin == null)
            {
                return null;
            }

            var path = foundPlugin.GetPluginUserDataPath();
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
            {
                return path;
            }

            return null;
        }

        public void BeginEdit()
        {
            editingClone = Serialization.GetClone(Settings);
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

            if (!string.IsNullOrEmpty(Settings.HowLongToBeatFolderPath) && !Directory.Exists(Settings.HowLongToBeatFolderPath))
            {
                errors.Add("HowLongToBeat Folder Path is not a valid directory.");
            }

            return errors.Count == 0;
        }
    }
}