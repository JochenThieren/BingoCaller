// ScreenPickerForm.cs
namespace BingoCaller
{
    /// <summary>Lets the caller tick every monitor that should show the big screen.</summary>
    internal sealed class ScreenPickerForm : Form
    {
        private readonly Screen[] _screens;
        private readonly CheckBox[] _boxes;
        private readonly Button _ok;

        public List<Screen> SelectedScreens =>
            _screens.Where((s, i) => _boxes[i].Checked).ToList();

        public ScreenPickerForm(Screen[] screens, IEnumerable<string> previous, Screen? callerScreen)
        {
            _screens = screens;

            Text = "Choose the big screens";
            Icon = AppIcon.Get();
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            BackColor = Color.FromArgb(30, 34, 48);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 10f);
            ClientSize = new Size(460, 120 + screens.Length * 36);

            Controls.Add(new Label
            {
                Text = "Tick every monitor that should show the big screen:",
                Left = 16, Top = 14, Width = 430, Height = 24
            });

            var saved = new HashSet<string>(previous ?? Enumerable.Empty<string>());
            bool anySaved = screens.Any(s => saved.Contains(s.DeviceName));

            _boxes = new CheckBox[screens.Length];
            for (int i = 0; i < screens.Length; i++)
            {
                Screen s = screens[i];
                bool isCaller = callerScreen != null && s.DeviceName == callerScreen.DeviceName;

                // Default: what was used last time; first time, every monitor except the one showing this caller window.
                bool initial = anySaved ? saved.Contains(s.DeviceName) : !isCaller;

                _boxes[i] = new CheckBox
                {
                    Text = $"Screen {i + 1}  -  {s.Bounds.Width}x{s.Bounds.Height}"
                           + (s.Primary ? "  (primary)" : "")
                           + (isCaller ? "  (this window)" : ""),
                    Left = 24, Top = 48 + i * 36, Width = 410, Height = 30,
                    Checked = initial
                };
                _boxes[i].CheckedChanged += (sender, e) => UpdateOk();
                Controls.Add(_boxes[i]);
            }

            int by = 64 + screens.Length * 36;

            var identify = MakeButton("Identify", 16, by, 100);
            identify.Click += (s, e) => IdentifyScreens();

            _ok = MakeButton("Show", 250, by, 90);
            _ok.DialogResult = DialogResult.OK;

            var cancel = MakeButton("Cancel", 350, by, 90);
            cancel.DialogResult = DialogResult.Cancel;

            Controls.AddRange(new Control[] { identify, _ok, cancel });
            AcceptButton = _ok;
            CancelButton = cancel;
            UpdateOk();
        }

        private void UpdateOk() => _ok.Enabled = _boxes.Any(b => b.Checked);

        private static Button MakeButton(string text, int left, int top, int width)
        {
            var b = new Button
            {
                Text = text, Left = left, Top = top, Width = width, Height = 34,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(52, 58, 80),
                ForeColor = Color.White
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        /// <summary>Flashes the screen number on every monitor for a moment, so the list can be matched to the real monitors.</summary>
        private void IdentifyScreens()
        {
            for (int i = 0; i < _screens.Length; i++)
            {
                Rectangle b = _screens[i].Bounds;
                var flash = new Form
                {
                    FormBorderStyle = FormBorderStyle.None,
                    StartPosition = FormStartPosition.Manual,
                    ShowInTaskbar = false,
                    TopMost = true,
                    BackColor = Color.FromArgb(30, 34, 48),
                    Size = new Size(260, 220),
                    Location = new Point(b.X + (b.Width - 260) / 2, b.Y + (b.Height - 220) / 2)
                };
                flash.Controls.Add(new Label
                {
                    Text = (i + 1).ToString(),
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 96f, FontStyle.Bold)
                });

                var timer = new System.Windows.Forms.Timer { Interval = 1800 };
                timer.Tick += (s, e) => { timer.Stop(); timer.Dispose(); flash.Close(); flash.Dispose(); };
                flash.Show();
                timer.Start();
            }
        }
    }
}
