// OptionsForm.cs
using System.Drawing.Text;

namespace BingoCaller
{
    /// <summary>Modeless options window. Every change is applied to the big screen immediately.</summary>
    internal sealed class OptionsForm : Form
    {
        private const string ImageFilter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All files|*.*";

        private readonly DisplaySettings _s;
        private readonly Action _changed;
        private ListBox _slideList = new ListBox();
        private CheckBox _slideEnabled = new CheckBox();
        private TabControl? _tabs;

        public OptionsForm(DisplaySettings settings, Action changed)
        {
            _s = settings;
            _changed = changed;

            Text = "Bingo options";
            Icon = AppIcon.Get();
            Font = new Font("Segoe UI", 9.5f);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(580, 590);

            var bottom = new Panel { Dock = DockStyle.Bottom, Height = 46 };
            var reset = new Button { Text = "Reset all to defaults...", Left = 12, Top = 8, Width = 190, Height = 30 };
            reset.Click += (s, e) => ResetAll();
            bottom.Controls.Add(reset);
            Controls.Add(bottom);

            BuildTabs(0);
        }

        private void BuildTabs(int selectedIndex)
        {
            if (_tabs != null)
            {
                Controls.Remove(_tabs);
                _tabs.Dispose();
            }

            _slideList = new ListBox();
            _slideEnabled = new CheckBox();

            _tabs = new TabControl { Dock = DockStyle.Fill };
            _tabs.TabPages.Add(BuildFontsTab());
            _tabs.TabPages.Add(BuildSlideshowTab());
            _tabs.TabPages.Add(BuildBannerTab());
            _tabs.TabPages.Add(BuildLogosTab());
            _tabs.TabPages.Add(BuildDrawTab());
            _tabs.SelectedIndex = Math.Clamp(selectedIndex, 0, _tabs.TabPages.Count - 1);
            Controls.Add(_tabs);
            _tabs.BringToFront();   // Fill must be laid out after the bottom panel
        }

        private void ResetAll()
        {
            if (MessageBox.Show(this,
                    "Reset every setting (fonts, sizes, slideshow, banner, logos, chosen monitors) to its default?",
                    "Reset all", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            int tab = _tabs?.SelectedIndex ?? 0;
            _s.ResetToDefaults();
            BuildTabs(tab);
            Changed();
        }

        private void Changed()
        {
            _changed();
        }

        /// <summary>Label + trackbar + "NN %" value on one row; applies live.</summary>
        private TrackBar AddSlider(Control parent, string label, int top, int labelWidth,
            int min, int max, int value, Action<int> set, string unit = " %")
        {
            var lbl = new Label { Text = label, Left = 14, Top = top + 8, Width = labelWidth };
            var valueLabel = new Label { Left = 500, Top = top + 8, Width = 55 };
            var track = new TrackBar
            {
                Left = 14 + labelWidth, Top = top, Width = 470 - labelWidth,
                Minimum = min, Maximum = max, TickFrequency = Math.Max(1, (max - min) / 10),
                LargeChange = Math.Max(1, (max - min) / 10),
                Value = Math.Clamp(value, min, max)
            };
            valueLabel.Text = track.Value + unit;
            track.ValueChanged += (s, e) => { valueLabel.Text = track.Value + unit; set(track.Value); Changed(); };

            parent.Controls.AddRange(new Control[] { lbl, track, valueLabel });
            return track;
        }

        // ------------------------------------------------------------------
        //  Fonts & sizes
        // ------------------------------------------------------------------

        private TabPage BuildFontsTab()
        {
            var page = new TabPage("Fonts && sizes") { Padding = new Padding(8) };

            var note = new Label
            {
                Text = "Size: 100 % is the default; 200 % is the largest size that still fits without clipping.",
                Left = 12, Top = 10, Width = 540, Height = 20, ForeColor = Color.DimGray
            };
            page.Controls.Add(note);

            AddFontGroup(page, "Called number (big)", 38,
                () => _s.NumberFont, v => _s.NumberFont = v,
                () => _s.NumberBold, v => _s.NumberBold = v,
                () => _s.NumberSizePercent, v => _s.NumberSizePercent = v, 0, 200);

            GroupBox letterBox = AddFontGroup(page, "Letter (B / I / N / G / O)", 168,
                () => _s.LetterFont, v => _s.LetterFont = v,
                () => _s.LetterBold, v => _s.LetterBold = v,
                () => _s.LetterSizePercent, v => _s.LetterSizePercent = v, 0, 200);

            var showLetter = new CheckBox { Text = "Show the letter", Left = 420, Top = 30, Width = 115, Checked = _s.ShowLetters };
            showLetter.CheckedChanged += (s, e) => { _s.ShowLetters = showLetter.Checked; Changed(); };
            letterBox.Controls.Add(showLetter);

            AddFontGroup(page, "Called-numbers strip (bottom)", 298,
                () => _s.HistoryFont, v => _s.HistoryFont = v,
                () => _s.HistoryBold, v => _s.HistoryBold = v,
                () => _s.HistorySizePercent, v => _s.HistorySizePercent = v, 0, 200);

            return page;
        }

        private GroupBox AddFontGroup(TabPage page, string title, int top,
            Func<string> getName, Action<string> setName,
            Func<bool> getBold, Action<bool> setBold,
            Func<int> getPct, Action<int> setPct, int min, int max)
        {
            var box = new GroupBox { Text = title, Left = 12, Top = top, Width = 545, Height = 120 };

            var combo = new ComboBox { Left = 14, Top = 28, Width = 300, DropDownStyle = ComboBoxStyle.DropDownList };
            using (var installed = new InstalledFontCollection())
                combo.Items.AddRange(installed.Families.Select(f => (object)f.Name).ToArray());
            int idx = combo.Items.IndexOf(getName());
            combo.SelectedIndex = idx >= 0 ? idx : Math.Max(0, combo.Items.IndexOf("Segoe UI"));

            var bold = new CheckBox { Text = "Bold", Left = 330, Top = 30, Width = 80, Checked = getBold() };

            combo.SelectedIndexChanged += (s, e) => { setName((string)combo.SelectedItem!); Changed(); };
            bold.CheckedChanged += (s, e) => { setBold(bold.Checked); Changed(); };

            box.Controls.AddRange(new Control[] { combo, bold });
            AddSlider(box, "Size", 62, 42, min, max, getPct(), setPct);
            page.Controls.Add(box);
            return box;
        }

        // ------------------------------------------------------------------
        //  Slideshow
        // ------------------------------------------------------------------

        private TabPage BuildSlideshowTab()
        {
            var page = new TabPage("Background slideshow") { Padding = new Padding(8) };

            _slideEnabled.Text = "Show a slideshow behind the numbers";
            _slideEnabled.SetBounds(12, 12, 400, 24);
            _slideEnabled.Checked = _s.SlideshowEnabled;
            _slideEnabled.CheckedChanged += (s, e) => { _s.SlideshowEnabled = _slideEnabled.Checked; Changed(); };

            _slideList.SetBounds(12, 42, 548, 150);
            _slideList.HorizontalScrollbar = true;
            _slideList.SelectionMode = SelectionMode.MultiExtended;
            RefreshSlideList();

            var addImages = MakeButton("Add images...", 12, 200, 120);
            addImages.Click += (s, e) =>
            {
                using var dlg = new OpenFileDialog { Filter = ImageFilter, Multiselect = true, Title = "Choose slideshow images" };
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                AddSlides(dlg.FileNames);
            };

            var addFolder = MakeButton("Add folder...", 140, 200, 120);
            addFolder.Click += (s, e) =>
            {
                using var dlg = new FolderBrowserDialog { Description = "All images in this folder are added" };
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                string[] exts = { ".png", ".jpg", ".jpeg", ".bmp", ".gif" };
                AddSlides(Directory.EnumerateFiles(dlg.SelectedPath)
                    .Where(f => exts.Contains(Path.GetExtension(f).ToLowerInvariant()))
                    .OrderBy(f => f, StringComparer.OrdinalIgnoreCase));
            };

            var remove = MakeButton("Remove selected", 268, 200, 140);
            remove.Click += (s, e) =>
            {
                foreach (string item in _slideList.SelectedItems.Cast<string>().ToList())
                    _s.SlideFiles.Remove(item);
                RefreshSlideList();
                Changed();
            };

            var clear = MakeButton("Clear all", 416, 200, 100);
            clear.Click += (s, e) => { _s.SlideFiles.Clear(); RefreshSlideList(); Changed(); };

            var secLabel = new Label { Text = "Seconds per image", Left = 12, Top = 248, Width = 130 };
            var seconds = new NumericUpDown
            {
                Left = 150, Top = 245, Width = 70, Minimum = 2, Maximum = 300,
                Value = Math.Clamp(_s.SlideSeconds, 2, 300)
            };
            seconds.ValueChanged += (s, e) => { _s.SlideSeconds = (int)seconds.Value; Changed(); };

            var shuffle = new CheckBox { Text = "Shuffle order", Left = 12, Top = 280, Width = 200, Checked = _s.SlideShuffle };
            shuffle.CheckedChanged += (s, e) => { _s.SlideShuffle = shuffle.Checked; Changed(); };

            var crop = new CheckBox
            {
                Text = "Fill the screen (crop edges) - untick to fit the whole picture",
                Left = 12, Top = 308, Width = 520, Checked = _s.SlideCrop
            };
            crop.CheckedChanged += (s, e) => { _s.SlideCrop = crop.Checked; Changed(); };

            page.Controls.AddRange(new Control[]
            {
                _slideEnabled, _slideList, addImages, addFolder, remove, clear,
                secLabel, seconds, shuffle, crop
            });

            AddSlider(page, "Slideshow opacity", 348, 160, 0, 100, _s.SlideOpacityPercent, v => _s.SlideOpacityPercent = v);
            AddSlider(page, "Numbers strip opacity", 396, 160, 0, 100, _s.StripOpacityPercent, v => _s.StripOpacityPercent = v);

            page.Controls.Add(new Label
            {
                Text = "Numbers strip opacity is the dark backing behind the called numbers at the bottom:\r\n0 % lets the slideshow show through completely.",
                Left = 12, Top = 444, Width = 545, Height = 40, ForeColor = Color.DimGray
            });
            return page;
        }

        private void AddSlides(IEnumerable<string> files)
        {
            foreach (string f in files)
                if (!_s.SlideFiles.Contains(f)) _s.SlideFiles.Add(f);
            RefreshSlideList();

            // Adding pictures is a clear sign the slideshow is wanted.
            if (_s.SlideFiles.Count > 0 && !_slideEnabled.Checked)
                _slideEnabled.Checked = true;   // raises CheckedChanged -> applies

            Changed();
        }

        private void RefreshSlideList()
        {
            _slideList.Items.Clear();
            _slideList.Items.AddRange(_s.SlideFiles.Cast<object>().ToArray());
        }

        // ------------------------------------------------------------------
        //  Banner
        // ------------------------------------------------------------------

        private TabPage BuildBannerTab()
        {
            var page = new TabPage("Banner") { Padding = new Padding(8) };

            var bannerBox = new GroupBox { Text = "Banner strip", Left = 12, Top = 10, Width = 545, Height = 344 };

            var enabled = new CheckBox { Text = "Show banner", Left = 14, Top = 26, Width = 200, Checked = _s.BannerEnabled };
            enabled.CheckedChanged += (s, e) => { _s.BannerEnabled = enabled.Checked; Changed(); };

            PathRow(bannerBox, 58, "Image", () => _s.BannerPath, v => _s.BannerPath = v);

            var scroll = new CheckBox
            {
                Text = "Scroll the slideshow images through the banner, right to left (instead of the image)",
                Left = 14, Top = 96, Width = 520, Checked = _s.BannerScroll
            };
            scroll.CheckedChanged += (s, e) => { _s.BannerScroll = scroll.Checked; Changed(); };

            var posLabel = new Label { Text = "Position", Left = 14, Top = 176, Width = 80 };
            var pos = new ComboBox { Left = 100, Top = 172, Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
            pos.Items.AddRange(new object[] { "Top", "Bottom" });
            pos.SelectedIndex = _s.BannerAtTop ? 0 : 1;
            pos.SelectedIndexChanged += (s, e) => { _s.BannerAtTop = pos.SelectedIndex == 0; Changed(); };

            var hero = new CheckBox
            {
                Text = "Before the first number: show the banner image big in the number's place\r\n(untick: only the slideshow shows there)",
                Left = 14, Top = 288, Width = 520, Height = 44, Checked = _s.BannerHeroWhenIdle
            };
            hero.CheckedChanged += (s, e) => { _s.BannerHeroWhenIdle = hero.Checked; Changed(); };

            bannerBox.Controls.AddRange(new Control[] { enabled, scroll, posLabel, pos, hero });
            AddSlider(bannerBox, "Scroll speed", 120, 100, 1, 20, _s.BannerScrollSpeed, v => _s.BannerScrollSpeed = v, "");
            AddSlider(bannerBox, "Height", 204, 100, 5, 40, _s.BannerHeightPercent, v => _s.BannerHeightPercent = v);
            AddSlider(bannerBox, "Opacity", 246, 100, 0, 100, _s.BannerOpacityPercent, v => _s.BannerOpacityPercent = v);
            page.Controls.Add(bannerBox);

            page.Controls.Add(new Label
            {
                Text = "Height is a percentage of the big-screen height. The scrolling banner uses the images from the Background slideshow tab. The Pause button on the caller window shows the slideshow alone, full screen.",
                Left = 12, Top = 366, Width = 545, Height = 66, ForeColor = Color.DimGray
            });
            return page;
        }

        // ------------------------------------------------------------------
        //  Logos
        // ------------------------------------------------------------------

        private TabPage BuildLogosTab()
        {
            var page = new TabPage("Sponsor logos") { Padding = new Padding(8) };

            var box = new GroupBox { Text = "Six sponsor positions", Left = 12, Top = 10, Width = 545, Height = 432 };

            for (int i = 0; i < DisplaySettings.LogoSlots; i++)
            {
                int slot = i;
                PathRow(box, 26 + i * 36, DisplaySettings.LogoSlotNames[i],
                    () => _s.GetLogoPath(slot), v => _s.SetLogoPath(slot, v), 90);
            }

            AddSlider(box, "Top/middle height", 250, 120, 5, 25, _s.LogoHeightPercent, v => _s.LogoHeightPercent = v);
            AddSlider(box, "Bottom height", 294, 120, 3, 20, _s.BottomLogoHeightPercent, v => _s.BottomLogoHeightPercent = v);
            AddSlider(box, "Opacity (all)", 338, 120, 0, 100, _s.LogoOpacityPercent, v => _s.LogoOpacityPercent = v);

            var placeLabel = new Label { Text = "Bottom logos are", Left = 14, Top = 392, Width = 120 };
            var place = new ComboBox { Left = 140, Top = 388, Width = 230, DropDownStyle = ComboBoxStyle.DropDownList };
            place.Items.AddRange(new object[] { "Below the numbers strip", "Above the numbers strip" });
            place.SelectedIndex = _s.BottomLogosAbove ? 1 : 0;
            place.SelectedIndexChanged += (s, e) => { _s.BottomLogosAbove = place.SelectedIndex == 1; Changed(); };
            box.Controls.AddRange(new Control[] { placeLabel, place });
            page.Controls.Add(box);

            page.Controls.Add(new Label
            {
                Text = "Top and middle logos sit in the corners over the picture. The bottom row is only as tall as the\r\nbottom logos are drawn. Heights are a percentage of the big-screen height.",
                Left = 12, Top = 448, Width = 545, Height = 40, ForeColor = Color.DimGray
            });
            return page;
        }

        // ------------------------------------------------------------------
        //  Random draw
        // ------------------------------------------------------------------

        private TabPage BuildDrawTab()
        {
            var page = new TabPage("Random draw") { Padding = new Padding(8) };

            var enable = new CheckBox
            {
                Text = "Show a \"Random draw\" button on the caller window",
                Left = 14, Top = 18, Width = 520, Height = 28, Checked = _s.EnableRandomDraw
            };
            enable.CheckedChanged += (s, e) => { _s.EnableRandomDraw = enable.Checked; Changed(); };

            var what = new Label
            {
                Left = 14, Top = 58, Width = 540, Height = 190,
                Text =
                    "A backup for when a physical ball is missing.\r\n\r\n" +
                    "The button picks one of the numbers that have not been called yet, with the computer's " +
                    "cryptographic random number generator (every remaining number is equally likely). The big " +
                    "screens then reveal it with a short animation of about three seconds: the number rolls " +
                    "through the remaining numbers, slows down and lands. The number is then called " +
                    "automatically, and Undo works as usual.\r\n\r\n" +
                    "Numbers that came from the random draw get a small dot in the numbers strip, so everybody " +
                    "can see which ones were not drawn from the ball machine."
            };

            var sound = new CheckBox
            {
                Text = "Play a ticking sound and a chime during the draw",
                Left = 14, Top = 254, Width = 400, Height = 28, Checked = _s.DrawSound
            };
            sound.CheckedChanged += (s, e) => { _s.DrawSound = sound.Checked; Changed(); };

            var sample = MakeButton("Play sample", 424, 252, 110);
            sample.Click += (s, e) => DrawSound.Play();

            var note = new Label
            {
                Left = 14, Top = 306, Width = 540, Height = 70, ForeColor = Color.DimGray,
                Text =
                    "Not a certified gaming device. If your event involves money or prizes, check the local " +
                    "rules on lotteries and bingo before you use a software draw (see the Disclaimer in the README)."
            };

            page.Controls.AddRange(new Control[] { enable, what, sound, sample, note });
            return page;
        }

        private void PathRow(Control parent, int top, string label, Func<string> get, Action<string> set, int labelWidth = 80)
        {
            var lbl = new Label { Text = label, Left = 14, Top = top + 4, Width = labelWidth };
            var text = new TextBox { Left = 14 + labelWidth + 6, Top = top, Width = 300 - (labelWidth - 80), ReadOnly = true, Text = get() };
            var browse = MakeButton("Browse...", 408, top - 2, 70);
            var clear = MakeButton("Clear", 484, top - 2, 50);

            browse.Click += (s, e) =>
            {
                using var dlg = new OpenFileDialog { Filter = ImageFilter, Title = label };
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                set(dlg.FileName);
                text.Text = dlg.FileName;
                Changed();
            };
            clear.Click += (s, e) => { set(""); text.Text = ""; Changed(); };

            parent.Controls.AddRange(new Control[] { lbl, text, browse, clear });
        }

        private static Button MakeButton(string text, int left, int top, int width)
        {
            return new Button { Text = text, Left = left, Top = top, Width = width, Height = 28 };
        }
    }
}
