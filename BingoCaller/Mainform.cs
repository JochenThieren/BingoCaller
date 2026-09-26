// MainForm.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Windows.Forms;

namespace BingoCaller
{
    /// <summary>
    /// The caller's control panel: 75 buttons (B I N G O columns).
    /// Click a number -> it is marked as called and pushed to the big screen.
    /// </summary>
    public partial class MainForm : Form
    {
        private const int NumbersPerColumn = 15;
        private const int TotalNumbers = 75;

        private readonly TableLayoutPanel _grid;
        private readonly Dictionary<int, Button> _buttons = new Dictionary<int, Button>();
        private readonly List<int> _called = new List<int>();

        private readonly Label _statusLabel;
        private readonly Button _undoButton;
        private readonly Button _resetButton;

        private readonly DisplaySettings _settings = DisplaySettings.Load();
        private OptionsForm _options;
        private readonly List<DisplayForm> _displays = new List<DisplayForm>();
        private Button _pauseButton;
        private bool _paused;
        private bool _drawing;                       // a random draw animation is running
        private int _drawPick;
        private Button _randomButton;
        private System.Windows.Forms.Timer? _drawTimer;
        private readonly HashSet<int> _randomDrawn = new HashSet<int>();   // numbers that came from the random draw
        private bool _displayClosedByUser;

        public MainForm()
        {
            Version? version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            Text = version == null ? "Bingo Caller" : $"Bingo Caller {version.Major}.{version.Minor}.{version.Build}";
            Icon = AppIcon.Get();
            BackColor = Color.FromArgb(240, 242, 248);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1040, 700);   // the status text needs room next to the six buttons
            ClientSize = new Size(1100, 800);

            // ---------------- top bar ----------------
            var top = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 68,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.FromArgb(30, 34, 48),
                Padding = new Padding(10, 8, 10, 8)
            };
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            _statusLabel = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(150, 158, 178),
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                Text = "No call yet - click a number"
            };

            var tools = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };

            _undoButton = MakeToolButton("Undo");
            _undoButton.Click += (s, e) => UndoLast();

            _resetButton = MakeToolButton("Reset");
            _resetButton.Click += (s, e) => ResetAll();

            _randomButton = MakeToolButton("Random draw");
            _randomButton.Click += (s, e) => RandomDraw();
            _randomButton.Visible = false;   // enabled in Options

            Button screenButton = MakeToolButton("Big Screen");
            screenButton.Click += (s, e) => ShowBigScreen();

            Button optionsButton = MakeToolButton("Options");
            optionsButton.Click += (s, e) => ShowOptions();

            _pauseButton = MakeToolButton("Pause");
            _pauseButton.Click += (s, e) => TogglePause();
            new ToolTip().SetToolTip(_pauseButton,
                "Pause: the big screens show only the slideshow, full screen. Press again to go back to the numbers.");

            tools.Controls.Add(optionsButton);
            tools.Controls.Add(_undoButton);
            tools.Controls.Add(_resetButton);
            tools.Controls.Add(_randomButton);
            tools.Controls.Add(_pauseButton);
            tools.Controls.Add(screenButton);

            top.Controls.Add(_statusLabel, 0, 0);
            top.Controls.Add(tools, 1, 0);

            // ---------------- number grid ----------------
            _grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5,
                RowCount = NumbersPerColumn + 1,
                BackColor = Color.FromArgb(240, 242, 248),
                Padding = new Padding(6)
            };

            for (int c = 0; c < 5; c++)
                _grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));

            _grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f)); // header row
            for (int r = 0; r < NumbersPerColumn; r++)
                _grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / NumbersPerColumn));

            BuildHeader();
            BuildNumberButtons();

            Controls.Add(_grid);
            Controls.Add(top);

            UpdateStatus();
        }

        // ------------------------------------------------------------------
        //  UI construction
        // ------------------------------------------------------------------

        private void BuildHeader()
        {
            for (int c = 0; c < 5; c++)
            {
                var lbl = new Label
                {
                    Text = "BINGO"[c].ToString(),
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                    ForeColor = Color.White,
                    BackColor = DisplayForm.ColumnColors[c],
                    Margin = new Padding(3)
                };
                _grid.Controls.Add(lbl, c, 0);
            }
        }

        private void BuildNumberButtons()
        {
            for (int c = 0; c < 5; c++)
            {
                for (int r = 1; r <= NumbersPerColumn; r++)
                {
                    int number = c * NumbersPerColumn + r;

                    var btn = new Button
                    {
                        Text = number.ToString(),
                        Tag = number,
                        Dock = DockStyle.Fill,
                        Margin = new Padding(3),
                        FlatStyle = FlatStyle.Flat,
                        Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                        BackColor = Color.White,
                        ForeColor = Color.FromArgb(40, 44, 60),
                        Cursor = Cursors.Hand,
                        TabStop = false
                    };
                    btn.FlatAppearance.BorderColor = Color.FromArgb(210, 214, 224);
                    btn.FlatAppearance.BorderSize = 1;
                    btn.Click += NumberButton_Click;

                    _buttons[number] = btn;
                    _grid.Controls.Add(btn, c, r);
                }
            }
        }

        private static Button MakeToolButton(string text)
        {
            var b = new Button
            {
                Text = text,
                AutoSize = false,
                Size = new Size(104, 40),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(52, 58, 80),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Margin = new Padding(6, 4, 0, 4),
                Cursor = Cursors.Hand,
                TabStop = false
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        // ------------------------------------------------------------------
        //  Game logic
        // ------------------------------------------------------------------

        private void NumberButton_Click(object sender, EventArgs e)
        {
            if (_paused || _drawing) return;   // paused / drawing: nothing may change

            var btn = (Button)sender;
            int number = (int)btn.Tag;

            if (_called.Contains(number)) return;   // already called

            CallNumber(number);
        }

        private void CallNumber(int number, bool random = false)
        {
            if (_paused) return;

            _called.Add(number);
            if (random) _randomDrawn.Add(number);

            RefreshGrid();
            UpdateStatus();
            PushToDisplay();
        }

        private void UndoLast()
        {
            if (_paused || _drawing || _called.Count == 0) return;

            _randomDrawn.Remove(_called[_called.Count - 1]);
            _called.RemoveAt(_called.Count - 1);

            RefreshGrid();
            UpdateStatus();
            PushToDisplay();
        }

        private void ResetAll()
        {
            if (_drawing) return;

            if (_called.Count > 0 &&
                MessageBox.Show(this,
                    "Clear all called numbers and start a new game?",
                    "Reset",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            _called.Clear();
            _randomDrawn.Clear();

            RefreshGrid();
            UpdateStatus();
            PushToDisplay();
        }

        private void RefreshGrid()
        {
            int last = _called.Count > 0 ? _called[_called.Count - 1] : -1;

            foreach (KeyValuePair<int, Button> pair in _buttons)
            {
                int n = pair.Key;
                Button btn = pair.Value;
                int col = (n - 1) / NumbersPerColumn;

                if (_called.Contains(n))
                {
                    btn.BackColor = DisplayForm.ColumnColors[col];
                    btn.ForeColor = Color.White;
                    btn.FlatAppearance.BorderColor = Color.White;
                    btn.FlatAppearance.BorderSize = (n == last) ? 3 : 1;
                }
                else
                {
                    btn.BackColor = Color.White;
                    btn.ForeColor = Color.FromArgb(40, 44, 60);
                    btn.FlatAppearance.BorderColor = Color.FromArgb(210, 214, 224);
                    btn.FlatAppearance.BorderSize = 1;
                }
            }
        }

        private void UpdateStatus()
        {
            if (_drawing)
            {
                _statusLabel.Text = "Drawing a random number...";
                _statusLabel.ForeColor = Color.FromArgb(230, 190, 40);
            }
            else if (_paused)
            {
                _statusLabel.Text = $"PAUSED   ({_called.Count} of {TotalNumbers} called)";
                _statusLabel.ForeColor = Color.FromArgb(230, 150, 40);
            }
            else if (_called.Count == 0)
            {
                _statusLabel.Text = "No call yet - click a number";
                _statusLabel.ForeColor = Color.FromArgb(150, 158, 178);
            }
            else
            {
                int n = _called[_called.Count - 1];
                int col = (n - 1) / NumbersPerColumn;
                char letter = "BINGO"[col];

                _statusLabel.Text =
                    $"Last call:   {letter} {n}        ({_called.Count} of {TotalNumbers} called)";
                _statusLabel.ForeColor = DisplayForm.ColumnColors[col];
            }

            UpdateControls();
        }

        /// <summary>One place that decides which controls may be used: paused or drawing locks the game.</summary>
        private void UpdateControls()
        {
            bool locked = _paused || _drawing;

            foreach (Button b in _buttons.Values)
                b.Enabled = !locked;

            _undoButton.Enabled = _called.Count > 0 && !locked;
            _resetButton.Enabled = _called.Count > 0 && !_drawing;   // allowed while paused (new game during a break)
            _pauseButton.Enabled = !_drawing;

            _randomButton.Visible = _settings.EnableRandomDraw;
            _randomButton.Enabled = !locked && _called.Count < TotalNumbers;
        }

        // ------------------------------------------------------------------
        //  Random draw (optional backup for a missing ball)
        // ------------------------------------------------------------------

        /// <summary>
        /// Picks one of the numbers that have not been called, uniformly, from the operating system's
        /// cryptographic random generator (GetInt32 rejects skewed values, so there is no modulo bias).
        /// Returns -1 when every number has been called.
        /// </summary>
        internal static int PickRandom(IEnumerable<int> called)
        {
            var taken = new HashSet<int>(called);
            var remaining = new List<int>(TotalNumbers);
            for (int n = 1; n <= TotalNumbers; n++)
                if (!taken.Contains(n)) remaining.Add(n);

            return remaining.Count == 0 ? -1 : remaining[RandomNumberGenerator.GetInt32(remaining.Count)];
        }

        private void RandomDraw()
        {
            if (_paused || _drawing || !_settings.EnableRandomDraw) return;

            // The result is decided here, first; the animation on the big screens is only the reveal.
            int pick = PickRandom(_called);
            if (pick < 0) return;

            _drawing = true;
            _drawPick = pick;
            UpdateStatus();

            _displays.RemoveAll(d => d.IsDisposed);
            if (_displays.Count == 0 && !_displayClosedByUser)
                OpenDisplay(null);

            foreach (DisplayForm d in _displays.ToList())
                d.PlayDraw(pick, _called);

            if (_settings.DrawSound) DrawSound.Play();   // one clip, built from the same schedule as the animation

            // With no big screen open there is nothing to wait for.
            _drawTimer ??= new System.Windows.Forms.Timer();
            _drawTimer.Tick -= DrawFinished;
            _drawTimer.Tick += DrawFinished;
            _drawTimer.Interval = _displays.Count > 0 ? DisplayForm.DrawDurationMs + 150 : 50;
            _drawTimer.Start();
        }

        private void DrawFinished(object? sender, EventArgs e)
        {
            _drawTimer!.Stop();
            _drawing = false;
            CallNumber(_drawPick, random: true);
        }

        // ------------------------------------------------------------------
        //  Big screen handling
        // ------------------------------------------------------------------

        private void ShowOptions()
        {
            if (_options != null && !_options.IsDisposed)
            {
                _options.Activate();
                return;
            }

            _options = new OptionsForm(_settings, () =>
            {
                _settings.Save();
                foreach (DisplayForm d in _displays.ToList())
                    if (!d.IsDisposed) d.ApplySettings(_settings);
                UpdateControls();
            });
            _options.Show(this);
        }

        private void PushToDisplay()
        {
            _displays.RemoveAll(d => d.IsDisposed);

            // First call without a big screen yet: open a plain window (unless the user closed them all).
            if (_displays.Count == 0 && !_displayClosedByUser)
                OpenDisplay(null);

            foreach (DisplayForm d in _displays.ToList())
                d.UpdateDisplay(_called, _randomDrawn);
        }

        /// <summary>Opens one big-screen window, full-screen on <paramref name="screen"/> when given.</summary>
        private DisplayForm OpenDisplay(Screen? screen)
        {
            var display = new DisplayForm(_settings);
            display.FormClosed += (s, e) =>
            {
                _displays.Remove(display);
                if (_displays.Count == 0) _displayClosedByUser = true;
            };
            _displays.Add(display);

            display.Show();
            if (screen != null) display.EnterFullScreen(screen);
            display.UpdateDisplay(_called, _randomDrawn);
            if (_paused) display.SetPaused(true);
            return display;
        }

        private void TogglePause()
        {
            if (_drawing) return;

            _paused = !_paused;
            _pauseButton.Text = _paused ? "Resume" : "Pause";
            _pauseButton.BackColor = _paused ? Color.FromArgb(200, 120, 20) : Color.FromArgb(52, 58, 80);

            // Block picking while paused: the number buttons go grey, Undo is disabled, the status line says PAUSED.
            UpdateStatus();

            _displays.RemoveAll(d => d.IsDisposed);
            foreach (DisplayForm d in _displays)
                d.SetPaused(_paused);
        }

        private void ShowBigScreen()
        {
            Screen[] screens = Screen.AllScreens;
            List<Screen> chosen;

            if (screens.Length == 1)
            {
                chosen = new List<Screen> { screens[0] };   // nothing to choose
            }
            else
            {
                using (var dlg = new ScreenPickerForm(screens, _settings.BigScreens, Screen.FromControl(this)))
                {
                    if (dlg.ShowDialog(this) != DialogResult.OK) return;
                    chosen = dlg.SelectedScreens;
                }

                _settings.BigScreens = chosen.Select(s => s.DeviceName).ToList();
                _settings.Save();
            }

            // Re-assign: close what is open, then open one window per ticked monitor.
            foreach (DisplayForm d in _displays.ToList())
                d.Close();

            foreach (Screen screen in chosen)
                OpenDisplay(screen);

            _displayClosedByUser = false;

            // Keep the caller window usable if it is not on one of the chosen monitors.
            Activate();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            DrawSound.Stop();

            foreach (DisplayForm d in _displays.ToList())
                if (!d.IsDisposed) d.Close();
        }
    }
}
