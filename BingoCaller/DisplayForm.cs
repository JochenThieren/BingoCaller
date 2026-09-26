// DisplayForm.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace BingoCaller
{
    /// <summary>
    /// The "big screen" window. Shows the current call in huge letters
    /// plus a history strip of everything already called.
    /// Put it on a TV / projector, press F11 for fullscreen, Esc to leave.
    /// </summary>
    public partial class DisplayForm : Form
    {
        public static readonly Color[] ColumnColors =
        {
            Color.FromArgb(225, 60, 90),    // B
            Color.FromArgb(235, 150, 40),   // I
            Color.FromArgb(70, 170, 90),    // N
            Color.FromArgb(55, 125, 210),   // G
            Color.FromArgb(150, 85, 195)    // O
        };

        private readonly BackdropPanel _backdrop;
        private readonly TableLayoutPanel _layout;
        private readonly InkText _letterLabel;
        private readonly InkText _numberLabel;
        private readonly FlowLayoutPanel _history;

        private DisplaySettings _settings;
        private bool _isFullScreen;
        private bool _adjustingFonts;
        private bool _fontsPending;
        private bool _idle = true;
        private FormBorderStyle _prevBorder;
        private Rectangle _prevBounds;

        public DisplayForm(DisplaySettings settings)
        {
            _settings = settings;
            Text = "Bingo - Big Screen";
            BackColor = Color.FromArgb(14, 16, 24);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(700, 500);
            KeyPreview = true;

            _letterLabel = new InkText
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(120, 130, 150),
                BackColor = Color.Transparent,
                Text = "BINGO"
            };
            _letterLabel.SetFont(settings.LetterFont, settings.LetterBold, 80f);

            _numberLabel = new InkText
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Text = "-"
            };
            _numberLabel.SetFont(settings.NumberFont, settings.NumberBold, 160f);
            _letterLabel.SizeChanged += (s, e) => ScheduleFonts();
            _numberLabel.SizeChanged += (s, e) => ScheduleFonts();

            _history = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = false,
                WrapContents = true,
                BackColor = Color.Transparent,   // backdrop paints the tinted backing
                Padding = new Padding(6)
            };
            _history.SizeChanged += (s, e) => { _backdrop?.Invalidate(true); LayoutHistory(); };
            _history.LocationChanged += (s, e) => _backdrop?.Invalidate(true);

            _layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = Color.Transparent
            };
            _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 160f));  // letter
            _layout.RowStyles.Add(new RowStyle(SizeType.Percent, 66f));    // number
            _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 0f));    // bottom logos when placed above the strip (empty row)
            _layout.RowStyles.Add(new RowStyle(SizeType.Percent, 34f));    // history

            _layout.Controls.Add(_letterLabel, 0, 0);
            _layout.Controls.Add(_numberLabel, 0, 1);
            _layout.Controls.Add(_history, 0, 3);

            _backdrop = new BackdropPanel { Dock = DockStyle.Fill };
            _backdrop.NumberBounds = () =>
                _backdrop.RectangleToClient(_numberLabel.RectangleToScreen(_numberLabel.ClientRectangle));
            _backdrop.StripBounds = () =>
                _backdrop.RectangleToClient(_history.RectangleToScreen(_history.ClientRectangle));
            _backdrop.Controls.Add(_layout);
            Controls.Add(_backdrop);
            _backdrop.Resize += (s, e) => UpdateSponsorRow();
            _backdrop.Apply(settings);
            UpdateSponsorRow();
            UpdateLetterRow();
            UpdateHero();
        }

        // Hiding the letters gives the whole row back to the number.
        private void UpdateLetterRow()
        {
            bool show = _settings.ShowLetters;
            _letterLabel.Visible = show;
            float height = show ? 160f : 0f;
            if (Math.Abs(_layout.RowStyles[0].Height - height) > 0.5f)
                _layout.RowStyles[0].Height = height;
        }

        // Bottom logos placed above the numbers strip need their own row in the layout.
        private void UpdateSponsorRow()
        {
            float px = _backdrop.SponsorRowAbovePixels;
            if (Math.Abs(_layout.RowStyles[2].Height - px) > 0.5f)
                _layout.RowStyles[2].Height = px;
        }

        // Before the first call the banner fills the space of the big number;
        // once a number is called it moves to its normal strip.
        private void UpdateHero()
        {
            bool hero = _idle && _settings.BannerHeroWhenIdle && _backdrop.HasBanner;
            _backdrop.HeroMode = hero;
            _layout.PerformLayout();
            _backdrop.Invalidate(true);

            // No placeholder: with the banner off the slideshow shows through where the number will appear.
            if (_idle) _numberLabel.Text = "";
        }

        /// <summary>Pause: the big screen shows only the slideshow, full screen and fitted.</summary>
        public void SetPaused(bool paused)
        {
            _layout.Visible = !paused;
            _backdrop.PauseMode = paused;

            if (!paused)
            {
                _layout.PerformLayout();
                UpdateFonts();
                LayoutHistory();
            }
        }

        /// <summary>Re-applies fonts, slideshow, banner and logos after the options changed.</summary>
        public void ApplySettings(DisplaySettings settings)
        {
            _settings = settings;
            _backdrop.Apply(settings);
            UpdateSponsorRow();
            UpdateLetterRow();
            UpdateHero();
            UpdateFonts();
            LayoutHistory();
        }

        // ------------------------------------------------------------------
        //  Public API used by MainForm
        // ------------------------------------------------------------------

        /// <summary>Redraws the whole display from the list of called numbers.</summary>
        public void UpdateDisplay(IReadOnlyList<int> called)
        {
            _history.SuspendLayout();
            var oldChips = new Control[_history.Controls.Count];
            _history.Controls.CopyTo(oldChips, 0);
            _history.Controls.Clear();
            foreach (Control old in oldChips) old.Dispose();

            if (called == null || called.Count == 0)
            {
                _letterLabel.Text = "BINGO";
                _letterLabel.ForeColor = Color.FromArgb(120, 130, 150);
                _numberLabel.Text = "";
                _numberLabel.ForeColor = Color.White;
            }
            else
            {
                int last = called[called.Count - 1];
                int col = (last - 1) / 15;

                _letterLabel.Text = "BINGO"[col].ToString();
                _letterLabel.ForeColor = ColumnColors[col];
                _numberLabel.Text = last.ToString();
                _numberLabel.ForeColor = Color.White;

                for (int i = 0; i < called.Count; i++)
                {
                    int n = called[i];
                    _history.Controls.Add(MakeHistoryChip(n, (n - 1) / 15));
                }
            }

            _idle = called == null || called.Count == 0;
            UpdateHero();

            LayoutHistory();
            _history.ResumeLayout();
        }

        public void EnterFullScreen(Screen screen)
        {
            if (_isFullScreen) return;

            _prevBorder = FormBorderStyle;
            _prevBounds = Bounds;
            _isFullScreen = true;

            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Normal;
            StartPosition = FormStartPosition.Manual;
            Bounds = (screen ?? Screen.FromControl(this)).Bounds;
        }

        public void ExitFullScreen()
        {
            if (!_isFullScreen) return;

            _isFullScreen = false;
            WindowState = FormWindowState.Normal;
            FormBorderStyle = _prevBorder;
            Bounds = _prevBounds;
        }

        // ------------------------------------------------------------------
        //  Internals
        // ------------------------------------------------------------------

        private const int MaxNumbers = 75;

        private static InkText MakeHistoryChip(int number, int col)
        {
            return new InkText
            {
                Text = number.ToString(),
                ForeColor = Color.White,
                BackColor = ColumnColors[col]
            };
        }

        // Sizes the chips so that all 75 possible numbers fit in the history
        // panel without scrolling. Sized for the full 75 (not the current
        // count) so chips don't change size during the game.
        private void LayoutHistory()
        {
            if (_history == null) return;

            // Assigning sizes can raise _history.SizeChanged, which calls back in here;
            // nested calls only ask for a re-run once the current pass is done.
            if (_layingOutHistory) { _historyDirty = true; return; }
            _layingOutHistory = true;
            try
            {
                do
                {
                    _historyDirty = false;
                    LayoutHistoryPass();
                } while (_historyDirty);
            }
            finally
            {
                _layingOutHistory = false;
            }
        }

        private void LayoutHistoryPass()
        {
            int w = _history.ClientSize.Width - _history.Padding.Horizontal;
            int h = _history.ClientSize.Height - _history.Padding.Vertical;
            if (w <= 0 || h <= 0) return;

            int cell = 0;
            for (int rows = 1; rows <= MaxNumbers; rows++)
            {
                int cols = (MaxNumbers + rows - 1) / rows;
                cell = Math.Max(cell, Math.Min(w / cols, h / rows));
            }
            if (cell < 8) cell = 8;

            int margin = Math.Max(1, cell / 14);
            int chip = cell - margin * 2;
            // 100 % = digits at 54 % of the chip size; 200 % = the largest size at which the widest
            // number ("88") still fits inside the chip.
            float maxEm = InkText.FitEm(_settings.HistoryFont, _settings.HistoryBold, "88", chip * 0.86f, chip * 0.86f);
            float em = Math.Max(4f, InkText.MapEm(_settings.HistorySizePercent, chip * 0.54f, maxEm));

            _history.SuspendLayout();
            foreach (Control c in _history.Controls)
            {
                c.Size = new Size(chip, chip);
                c.Margin = new Padding(margin);
                if (c is InkText t) t.SetFont(_settings.HistoryFont, _settings.HistoryBold, em);
            }
            _history.ResumeLayout(true);
        }

        private bool _layingOutHistory;
        private bool _historyDirty;

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            UpdateFonts();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateFonts();
        }

        private void UpdateFonts()
        {
            if (_numberLabel == null || _letterLabel == null) return;
            if (!IsHandleCreated) return;

            if (_adjustingFonts) return;

            _adjustingFonts = true;
            try
            {
                // Reference text keeps the size stable when the actual call
                // changes (single vs. double digit, letter vs. "BINGO").
                FitInk(_numberLabel, "88", 8,
                    _settings.NumberFont, _settings.NumberBold, _settings.NumberSizePercent);
                FitInk(_letterLabel, "BINGO", 6,
                    _settings.LetterFont, _settings.LetterBold, _settings.LetterSizePercent);
            }
            finally
            {
                _adjustingFonts = false;
            }
        }

        // Label size changes come from inside a layout pass; fitting fonts right
        // there re-enters layout, so run the fit once the pass has finished.
        private void ScheduleFonts()
        {
            if (_fontsPending || !IsHandleCreated) return;
            _fontsPending = true;
            BeginInvoke(new Action(() =>
            {
                _fontsPending = false;
                UpdateFonts();
            }));
        }

        /// <summary>
        /// Sizes a big label. 100 % is the size at which the font's whole line height fits the area
        /// (the original look); 200 % is the largest size at which the outline of the reference text
        /// still fits, so the top of the slider is always "as big as it can be without clipping".
        /// </summary>
        private static void FitInk(InkText label, string referenceText, int padding,
            string family, bool bold, int percent)
        {
            int availW = label.ClientSize.Width - padding * 2;
            int availH = label.ClientSize.Height - padding * 2;
            if (availW <= 4 || availH <= 4) return;

            // Default: measure the line box once at a known size, then scale linearly.
            const float refSize = 100f;  // points
            Size measured;
            using (FontFamily ff = InkText.ResolveFamily(family, bold, out FontStyle style))
            using (Font probe = new Font(ff, refSize, style, GraphicsUnit.Point))
            {
                measured = TextRenderer.MeasureText(
                    referenceText, probe,
                    new Size(int.MaxValue, int.MaxValue),
                    TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
            }
            if (measured.Width <= 0 || measured.Height <= 0) return;

            float scale = Math.Min((float)availW / measured.Width, (float)availH / measured.Height);
            float defaultEm = refSize * scale * 0.98f * label.DeviceDpi / 72f;   // points -> pixels

            float maxEm = InkText.FitEm(family, bold, referenceText, availW, availH) * 0.98f;

            float em = InkText.MapEm(percent, defaultEm, Math.Max(maxEm, defaultEm));
            label.SetFont(family, bold, Math.Max(4f, Math.Min(3000f, em)));
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.F11)
            {
                if (_isFullScreen) ExitFullScreen();
                else EnterFullScreen(Screen.FromControl(this));
                return true;
            }

            if (keyData == Keys.Escape && _isFullScreen)
            {
                ExitFullScreen();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}