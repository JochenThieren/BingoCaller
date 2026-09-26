// InkText.cs
using System.Drawing.Drawing2D;

namespace BingoCaller
{
    /// <summary>
    /// Draws its text centred by the actual outline (ink) of the glyphs, not by the font's line box.
    /// A Label centres the line box, so digits sit off-centre and get clipped once the font is larger
    /// than the control. This control also has no Font resources to dispose: size and family are plain values.
    /// </summary>
    internal sealed class InkText : Control
    {
        private string _fontName = "Segoe UI";
        private bool _bold = true;
        private float _emPixels = 10f;

        public InkText()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            SetStyle(ControlStyles.Selectable, false);
            TabStop = false;
        }

        private bool _marker;

        /// <summary>Draws a small dot in the top-right corner (used for numbers that came from the random draw).</summary>
        public bool Marker
        {
            get => _marker;
            set { if (_marker != value) { _marker = value; Invalidate(); } }
        }

        public void SetFont(string fontName, bool bold, float emPixels)
        {
            if (_fontName == fontName && _bold == bold && Math.Abs(_emPixels - emPixels) < 0.25f) return;
            _fontName = fontName;
            _bold = bold;
            _emPixels = emPixels;
            Invalidate();
        }

        protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); Invalidate(); }
        protected override void OnForeColorChanged(EventArgs e) { base.OnForeColorChanged(e); Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (_marker)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                float r = Math.Max(4f, Width * 0.11f);
                float x = Width - r * 2.6f, y = r * 0.6f;
                using var dot = new SolidBrush(Color.FromArgb(240, 255, 255, 255));
                using var rim = new Pen(Color.FromArgb(120, 0, 0, 0), 1f);
                e.Graphics.FillEllipse(dot, x, y, r * 2, r * 2);
                e.Graphics.DrawEllipse(rim, x, y, r * 2, r * 2);
            }

            if (string.IsNullOrEmpty(Text) || _emPixels < 1f) return;

            using FontFamily family = ResolveFamily(_fontName, _bold, out FontStyle style);
            using var path = new GraphicsPath();
            path.AddString(Text, family, (int)style, _emPixels, PointF.Empty, StringFormat.GenericTypographic);

            RectangleF ink = path.GetBounds();
            if (ink.Width <= 0 || ink.Height <= 0) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TranslateTransform((Width - ink.Width) / 2f - ink.X, (Height - ink.Height) / 2f - ink.Y);
            using var brush = new SolidBrush(ForeColor);
            g.FillPath(brush, path);
        }

        /// <summary>The family plus a style it really has (some fonts have no Bold).</summary>
        public static FontFamily ResolveFamily(string? name, bool bold, out FontStyle style)
        {
            FontFamily ff;
            try { ff = new FontFamily(string.IsNullOrWhiteSpace(name) ? "Segoe UI" : name); }
            catch { ff = new FontFamily("Segoe UI"); }

            style = bold ? FontStyle.Bold : FontStyle.Regular;
            if (!ff.IsStyleAvailable(style))
            {
                style = FontStyle.Regular;
                foreach (FontStyle candidate in new[] { FontStyle.Regular, FontStyle.Bold, FontStyle.Italic, FontStyle.Bold | FontStyle.Italic })
                    if (ff.IsStyleAvailable(candidate)) { style = candidate; break; }
            }
            return ff;
        }

        /// <summary>Width and height of the text's outline per 100 px of em size.</summary>
        private static SizeF InkAt100(string fontName, bool bold, string text)
        {
            using FontFamily family = ResolveFamily(fontName, bold, out FontStyle style);
            using var path = new GraphicsPath();
            path.AddString(text, family, (int)style, 100f, PointF.Empty, StringFormat.GenericTypographic);
            RectangleF b = path.GetBounds();
            return new SizeF(b.Width, b.Height);
        }

        /// <summary>Largest em size (pixels) at which the outline of <paramref name="text"/> still fits in w x h.</summary>
        public static float FitEm(string fontName, bool bold, string text, float w, float h)
        {
            SizeF ink = InkAt100(fontName, bold, text);
            if (ink.Width <= 0 || ink.Height <= 0 || w <= 0 || h <= 0) return 1f;
            return Math.Min(w / ink.Width, h / ink.Height) * 100f;
        }

        /// <summary>
        /// Slider mapping shared by all sizes: 0..100 % scales the default size, 100..200 % grows it linearly
        /// up to <paramref name="maxEm"/>, the largest size that still fits (so 200 % is exactly "as big as fits").
        /// </summary>
        public static float MapEm(int percent, float defaultEm, float maxEm)
        {
            float d = Math.Min(defaultEm, maxEm);
            if (percent <= 100) return d * percent / 100f;
            return d + (maxEm - d) * (Math.Min(percent, 200) - 100) / 100f;
        }
    }
}
