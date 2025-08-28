// START: D:\Visual Studio Projects\PlayniteGo\PlayniteGoSettings.cs 
using Playnite.SDK.Data;
using System.Collections.Generic;

namespace PlayniteGo
{
    public class PlayniteGoSettings : ObservableObject
    {
        // --- NEW: Flexible Export Settings ---
        public string ImageExportFormat { get; set; } = "WebP";
        public int ImageQuality { get; set; } = 75;
        public int CoverWidth { get; set; } = 600;
        public int BackgroundWidth { get; set; } = 1280;

        public string DebugMessage { get; set; } = "Defaults applied via property initializer.";
    }
}
// END: D:\Visual Studio Projects\PlayniteGo\PlayniteGoSettings.cs
