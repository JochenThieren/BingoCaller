// DisplaySettings.cs
using System.Drawing;
using System.Text.Json;

namespace BingoCaller
{
    /// <summary>Everything the caller can tweak for the big screen. Saved next to the exe as BingoCaller.settings.json.</summary>
    public class DisplaySettings
    {
        /// <summary>Bumped when a setting changes meaning, so older files can be converted on load. 0 = file predates versioning.</summary>
        public int SettingsVersion { get; set; }

        /// <summary>Device names of the monitors that showed the big screen last time.</summary>
        public List<string> BigScreens { get; set; } = new List<string>();

        /// <summary>Shows the "Random draw" button on the caller window (a backup, e.g. when a physical ball is missing).</summary>
        public bool EnableRandomDraw { get; set; }
        /// <summary>Play the ticking and landing chime during the random draw.</summary>
        public bool DrawSound { get; set; } = true;

        // ---- fonts / sizes (percent: 100 = default, 200 = double) ----
        public string NumberFont { get; set; } = "Segoe UI";
        public bool NumberBold { get; set; } = true;
        public int NumberSizePercent { get; set; } = 100;

        public string LetterFont { get; set; } = "Segoe UI";
        public bool LetterBold { get; set; } = true;
        public int LetterSizePercent { get; set; } = 100;
        /// <summary>False hides the B/I/N/G/O letter row completely and gives its space to the number.</summary>
        public bool ShowLetters { get; set; } = true;

        public string HistoryFont { get; set; } = "Segoe UI";
        public bool HistoryBold { get; set; } = true;
        public int HistorySizePercent { get; set; } = 100;

        // ---- background slideshow ----
        public bool SlideshowEnabled { get; set; }
        public List<string> SlideFiles { get; set; } = new List<string>();
        public int SlideSeconds { get; set; } = 8;
        public bool SlideShuffle { get; set; }
        public bool SlideCrop { get; set; } = true;
        public int SlideOpacityPercent { get; set; } = 45;
        /// <summary>Opacity of the dark panel behind the called-numbers strip; 0 lets the slideshow show through fully.</summary>
        public int StripOpacityPercent { get; set; } = 100;

        // ---- banner & logos ----
        public bool BannerEnabled { get; set; }
        public string BannerPath { get; set; } = "";
        public bool BannerAtTop { get; set; } = true;
        public int BannerHeightPercent { get; set; } = 14;
        /// <summary>Before the first number: show the banner image big in the number's place. Off = only the slideshow shows there.</summary>
        public bool BannerHeroWhenIdle { get; set; } = true;
        /// <summary>The banner strip scrolls the slideshow images right to left instead of showing the single banner image.</summary>
        public bool BannerScroll { get; set; }
        /// <summary>Opacity of the banner (static image, big idle banner and scrolling strip); lower lets the slideshow show through.</summary>
        public int BannerOpacityPercent { get; set; } = 100;
        public int BannerScrollSpeed { get; set; } = 6;

        // Six sponsor slots. LogoLeftPath / LogoRightPath are the top corners (names kept for existing settings files).
        public string LogoLeftPath { get; set; } = "";
        public string LogoRightPath { get; set; } = "";
        public string LogoMidLeftPath { get; set; } = "";
        public string LogoMidRightPath { get; set; } = "";
        public string LogoBotLeftPath { get; set; } = "";
        public string LogoBotRightPath { get; set; } = "";
        public int LogoHeightPercent { get; set; } = 12;
        /// <summary>Height of the two bottom logos; separate so a small square logo doesn't claim a big row.</summary>
        public int BottomLogoHeightPercent { get; set; } = 8;
        /// <summary>True: bottom logos sit between the big number and the called-numbers strip; false: below the strip.</summary>
        public bool BottomLogosAbove { get; set; }
        public int LogoOpacityPercent { get; set; } = 100;

        public const int LogoSlots = 6;
        public static readonly string[] LogoSlotNames =
            { "Top left", "Top right", "Middle left", "Middle right", "Bottom left", "Bottom right" };

        public string GetLogoPath(int slot) => slot switch
        {
            0 => LogoLeftPath, 1 => LogoRightPath,
            2 => LogoMidLeftPath, 3 => LogoMidRightPath,
            4 => LogoBotLeftPath, _ => LogoBotRightPath
        };

        public void SetLogoPath(int slot, string path)
        {
            switch (slot)
            {
                case 0: LogoLeftPath = path; break;
                case 1: LogoRightPath = path; break;
                case 2: LogoMidLeftPath = path; break;
                case 3: LogoMidRightPath = path; break;
                case 4: LogoBotLeftPath = path; break;
                default: LogoBotRightPath = path; break;
            }
        }

        // ------------------------------------------------------------------

        private static string FilePath => Path.Combine(AppContext.BaseDirectory, "BingoCaller.settings.json");

        public static DisplaySettings Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var s = JsonSerializer.Deserialize<DisplaySettings>(File.ReadAllText(FilePath));
                    if (s != null) { s.Migrate(); s.Normalize(); return s; }
                }
            }
            catch { /* corrupt file -> defaults */ }
            return new DisplaySettings { SettingsVersion = CurrentVersion };
        }

        private const int CurrentVersion = 2;

        /// <summary>Puts every setting back to its built-in default, in place (other windows hold this same object).</summary>
        public void ResetToDefaults()
        {
            var fresh = new DisplaySettings { SettingsVersion = CurrentVersion };
            foreach (var p in typeof(DisplaySettings).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                if (p.CanRead && p.CanWrite)
                    p.SetValue(this, p.GetValue(fresh));
        }

        private void Migrate()
        {
            // v2: the strip font's 100 % became what used to be 150 %, so keep existing looks unchanged.
            if (SettingsVersion < 2)
                HistorySizePercent = (int)Math.Round(HistorySizePercent * 100.0 / 150.0);
            SettingsVersion = CurrentVersion;
        }

        public void Save()
        {
            try
            {
                File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { /* read-only install folder - settings just won't persist */ }
        }

        private void Normalize()
        {
            NumberSizePercent = Math.Clamp(NumberSizePercent, 0, 200);
            LetterSizePercent = Math.Clamp(LetterSizePercent, 0, 200);
            HistorySizePercent = Math.Clamp(HistorySizePercent, 0, 200);
            SlideSeconds = Math.Clamp(SlideSeconds, 2, 300);
            SlideOpacityPercent = Math.Clamp(SlideOpacityPercent, 0, 100);
            StripOpacityPercent = Math.Clamp(StripOpacityPercent, 0, 100);
            BannerHeightPercent = Math.Clamp(BannerHeightPercent, 5, 40);
            BannerScrollSpeed = Math.Clamp(BannerScrollSpeed, 1, 20);
            BannerOpacityPercent = Math.Clamp(BannerOpacityPercent, 0, 100);
            LogoHeightPercent = Math.Clamp(LogoHeightPercent, 5, 25);
            BottomLogoHeightPercent = Math.Clamp(BottomLogoHeightPercent, 3, 20);
            LogoOpacityPercent = Math.Clamp(LogoOpacityPercent, 0, 100);
            SlideFiles ??= new List<string>();
            BigScreens ??= new List<string>();
            NumberFont ??= "Segoe UI"; LetterFont ??= "Segoe UI"; HistoryFont ??= "Segoe UI";
            BannerPath ??= "";
            for (int i = 0; i < LogoSlots; i++) SetLogoPath(i, GetLogoPath(i) ?? "");
        }
    }
}
