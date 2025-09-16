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
                if (settings.ImageExportFormat != value)
                {
                    Settings.ImageExportFormat = value;
                    OnPropertyChanged();
                }
            }
        }

        public int ImageQuality
        {
            get => Settings.ImageQuality;
            set
            {
                if (settings.ImageQuality != value)
                {
                    Settings.ImageQuality = value;
                    OnPropertyChanged();
                }
            }
        }

        public int CoverWidth
        {
            get => Settings.CoverWidth;
            set
            {
                if (settings.CoverWidth != value)
                {
                    Settings.CoverWidth = value;
                    OnPropertyChanged();
                }
            }
        }

        public int BackgroundWidth
        {
            get => Settings.BackgroundWidth;
            set
            {
                if (settings.BackgroundWidth != value)
                {
                    Settings.BackgroundWidth = value;
                    OnPropertyChanged();
                }
            }
        }

        public string DebugMessage
        {
            get => Settings.DebugMessage;
            set
            {
                if (settings.DebugMessage != value)
                {
                    Settings.DebugMessage = value;
                    OnPropertyChanged();
                }
            }
        }


        public PlayniteGoSettingsViewModel(PlayniteGo plugin, PlayniteGoSettings settings)
        {
            this.plugin = plugin;
            this.Settings = settings;

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

        // --- THIS NEW METHOD IS THE KEY CHANGE ---
        public void BeginEdit(PlayniteGoSettings settingsToEdit)
        {
            Settings = settingsToEdit;
            editingClone = Serialization.GetClone(Settings);
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