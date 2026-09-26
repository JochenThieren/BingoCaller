// BackdropPanel.cs
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace BingoCaller
{
    /// <summary>
    /// Container that paints a cross-fading image slideshow (dimmed) as its background,
    /// plus an optional banner strip and two corner logos. Child controls with a
    /// transparent BackColor draw on top of it.
    /// </summary>
    internal sealed class BackdropPanel : Panel
    {
        private static readonly Color BaseColor = Color.FromArgb(14, 16, 24);

        private DisplaySettings _s = new DisplaySettings();

        private readonly System.Windows.Forms.Timer _slideTimer = new System.Windows.Forms.Timer();
        private readonly System.Windows.Forms.Timer _fadeTimer = new System.Windows.Forms.Timer { Interval = 30 };
        private readonly ImageAttributes _fadeAttr = new ImageAttributes();
        private readonly Random _rng = new Random();

        private List<string> _order = new List<string>();
        private int _pos = -1;
        private string _slideSig = "";
        private Bitmap? _curSrc, _curScaled, _prevScaled;
        private Bitmap? _frame;   // slideshow (incl. cross-fade) composed once, so each paint is just a clipped copy
        private float _fade = 1f;

        private Bitmap? _banner;
        private string _bannerPath = "";
        private readonly Bitmap?[] _logos = new Bitmap?[DisplaySettings.LogoSlots];
        private readonly string[] _logoPaths = new string[DisplaySettings.LogoSlots] { "", "", "", "", "", "" };
        private readonly ImageAttributes _logoAttr = new ImageAttributes();
        private Color _bannerBack = BaseColor;

        public BackdropPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            BackColor = BaseColor;

            _slideTimer.Tick += (s, e) => { if (_order.Count > 1) ShowNext(false); };
            _tickerTimer.Tick += (s, e) => TickerTick();
            _fadeTimer.Tick += (s, e) =>
            {
                _fade += 0.08f;
                if (_fade >= 1f)
                {
                    _fade = 1f;
                    _fadeTimer.Stop();
                    _prevScaled?.Dispose();
                    _prevScaled = null;
                }
                ComposeFrame();
                Invalidate(true);
            };
        }

        // ------------------------------------------------------------------
        //  Settings
        // ------------------------------------------------------------------

        public void Apply(DisplaySettings s)
        {
            _s = s;

            ApplySlideshow();

            // banner + logos; a changed file drops just its own pre-scaled copy
            string bannerBefore = _bannerPath;
            LoadInto(ref _banner, ref _bannerPath, s.BannerEnabled ? s.BannerPath : "");
            if (_bannerPath != bannerBefore)
            {
                _bannerScaled?.Dispose(); _bannerScaled = null;
                _bannerBack = BaseColor;
                if (_banner != null)
                {
                    Color px = _banner.GetPixel(0, 0);
                    if (px.A == 255) _bannerBack = px;
                }
            }

            if (s.BannerOpacityPercent != _appliedBannerOpacity)
            {
                _bannerAttr.SetColorMatrix(new ColorMatrix { Matrix33 = s.BannerOpacityPercent / 100f });
                _bannerScaled?.Dispose(); _bannerScaled = null;
                _appliedBannerOpacity = s.BannerOpacityPercent;
            }

            for (int i = 0; i < _logos.Length; i++)
            {
                string before = _logoPaths[i];
                LoadInto(ref _logos[i], ref _logoPaths[i], s.GetLogoPath(i));
                if (_logoPaths[i] != before) { _logoScaled[i]?.Dispose(); _logoScaled[i] = null; }
            }

            if (s.LogoOpacityPercent != _appliedLogoOpacity)
            {
                _logoAttr.SetColorMatrix(new ColorMatrix { Matrix33 = s.LogoOpacityPercent / 100f });
                for (int i = 0; i < _logoScaled.Length; i++) { _logoScaled[i]?.Dispose(); _logoScaled[i] = null; }
                _appliedLogoOpacity = s.LogoOpacityPercent;
            }

            ApplyTicker();
            UpdatePadding();
            Invalidate(true);
        }

        // ------------------------------------------------------------------
        //  Pause: the slideshow alone, full screen and fitted
        // ------------------------------------------------------------------

        private bool _pause;

        /// <summary>True: show only the slideshow (opacity 100 %, whole picture visible), hiding banner, logos and numbers.</summary>
        public bool PauseMode
        {
            get => _pause;
            set
            {
                if (_pause == value) return;
                _pause = value;
                ApplySlideshow();
                ApplyTicker();
                UpdatePadding();
                Invalidate(true);
            }
        }

        // Pause shows the slideshow even when the background slideshow option is off.
        private bool SlidesWanted => _s.SlideFiles.Count > 0 && (_s.SlideshowEnabled || _pause);
        private int EffOpacity => _pause ? 100 : _s.SlideOpacityPercent;
        private bool EffCrop => !_pause && _s.SlideCrop;

        private void ApplySlideshow()
        {
            string sig = string.Join("|", _s.SlideFiles) + "#" + _s.SlideShuffle;
            bool listChanged = sig != _slideSig;
            _slideSig = sig;

            if (!SlidesWanted)
            {
                StopSlides();
                return;
            }

            // Only redo the (expensive) scaling when something that affects the picture changed;
            // sliders elsewhere in the options call Apply on every tick.
            bool lookChanged = EffOpacity != _appliedSlideOpacity || EffCrop != _appliedSlideCrop;
            if (listChanged || _curSrc == null) StartSlides();
            else if (lookChanged) RenderCurrent();
            _appliedSlideOpacity = EffOpacity;
            _appliedSlideCrop = EffCrop;

            int interval = Math.Max(2, _s.SlideSeconds) * 1000;
            if (_slideTimer.Interval != interval) _slideTimer.Interval = interval;
            _slideTimer.Start();
        }

        // ------------------------------------------------------------------
        //  Scrolling banner: the slideshow images travel right to left through the banner strip
        // ------------------------------------------------------------------

        private readonly System.Windows.Forms.Timer _tickerTimer = new System.Windows.Forms.Timer { Interval = 33 };
        private readonly List<Bitmap> _tickerSources = new List<Bitmap>();
        private string _tickerFilesSig = "";
        private Bitmap? _tickerStrip;
        private IntPtr _tickerHandle;
        private int _tickerTotal, _tickerStripHeight;
        private Color _tickerStripBack;
        private float _tickerOffset;
        private long _tickerLastStamp;

        private bool TickerActive => _s.BannerEnabled && _s.BannerScroll && !_pause && _tickerSources.Count > 0;

        private void ApplyTicker()
        {
            bool want = _s.BannerEnabled && _s.BannerScroll && _s.SlideFiles.Count > 0;
            string sig = want ? string.Join("|", _s.SlideFiles) : "";

            if (sig != _tickerFilesSig)
            {
                foreach (Bitmap b in _tickerSources) b.Dispose();
                _tickerSources.Clear();
                _tickerFilesSig = sig;
                ReleaseTickerStrip();

                if (want)
                    foreach (string file in _s.SlideFiles.Take(40))
                    {
                        Bitmap? b = LoadSafe(file, 1200);
                        if (b != null) _tickerSources.Add(b);
                    }
            }

            if (TickerActive)
            {
                if (!_tickerTimer.Enabled)
                {
                    _tickerLastStamp = System.Diagnostics.Stopwatch.GetTimestamp();
                    _tickerTimer.Start();
                }
            }
            else
            {
                _tickerTimer.Stop();
            }
        }

        private void TickerTick()
        {
            long now = System.Diagnostics.Stopwatch.GetTimestamp();
            double dt = (now - _tickerLastStamp) / (double)System.Diagnostics.Stopwatch.Frequency;
            _tickerLastStamp = now;

            _tickerOffset += (float)(dt * _s.BannerScrollSpeed * 15);   // speed 1..20 -> 15..300 px per second
            if (_tickerTotal > 0 && _tickerOffset >= _tickerTotal) _tickerOffset %= _tickerTotal;

            int bh = BannerPixels;
            if (bh > 0) Invalidate(new Rectangle(0, _s.BannerAtTop ? 0 : Height - bh, Width, bh));
        }

        private void ReleaseTickerStrip()
        {
            if (_tickerHandle != IntPtr.Zero) { DeleteObject(_tickerHandle); _tickerHandle = IntPtr.Zero; }
            _tickerStrip?.Dispose();
            _tickerStrip = null;
            _tickerTotal = 0;
        }

        /// <summary>Lays all images out in one long strip at the banner height (built once; painting is a plain BitBlt).</summary>
        private void EnsureTickerStrip(int stripHeight)
        {
            Color back = _banner != null ? _bannerBack : Color.FromArgb(22, 25, 36);
            if (_tickerStrip != null && _tickerStripHeight == stripHeight && _tickerStripBack == back) return;

            ReleaseTickerStrip();
            if (_tickerSources.Count == 0) return;

            int th = Math.Max(4, stripHeight - 12);
            int gap = Math.Max(24, th / 2);
            var widths = _tickerSources.Select(b => Math.Max(1, (int)Math.Round(b.Width * (double)th / b.Height))).ToList();
            int total = widths.Sum() + gap * widths.Count;

            var strip = new Bitmap(total, stripHeight, PixelFormat.Format32bppRgb);
            using (Graphics g = Graphics.FromImage(strip))
            {
                g.Clear(back);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                int x = gap / 2;
                for (int i = 0; i < _tickerSources.Count; i++)
                {
                    g.DrawImage(_tickerSources[i], x, (stripHeight - th) / 2, widths[i], th);
                    x += widths[i] + gap;
                }
            }

            _tickerStrip = strip;
            _tickerHandle = strip.GetHbitmap();
            _tickerTotal = total;
            _tickerStripHeight = stripHeight;
            _tickerStripBack = back;
        }

        private void DrawTicker(Graphics g, Rectangle strip, Rectangle clip)
        {
            EnsureTickerStrip(strip.Height);
            if (_tickerHandle == IntPtr.Zero || _tickerTotal <= 0) return;

            Rectangle visible = Rectangle.Intersect(strip, clip);
            for (int x = -(int)_tickerOffset; x < strip.Right; x += _tickerTotal)
            {
                Rectangle dst = Rectangle.Intersect(new Rectangle(x, strip.Y, _tickerTotal, strip.Height), visible);
                if (dst.Width > 0 && dst.Height > 0)
                    BitBltRect(g, _tickerHandle, dst, dst.X - x, dst.Y - strip.Y, BannerAlpha);
            }
        }

        private bool _heroMode;
        private int _appliedSlideOpacity = -1, _appliedLogoOpacity = -1, _appliedBannerOpacity = -1;
        private readonly ImageAttributes _bannerAttr = new ImageAttributes();
        private bool _appliedSlideCrop;

        /// <summary>Bounds (in this panel's client coordinates) of the big-number area; used for the hero banner and middle logos.</summary>
        public Func<Rectangle>? NumberBounds { get; set; }

        /// <summary>Bounds (in this panel's client coordinates) of the called-numbers strip, which gets a tinted backing.</summary>
        public Func<Rectangle>? StripBounds { get; set; }

        public bool HasBanner => _banner != null;

        /// <summary>True: the banner is drawn large in <see cref="HeroBounds"/> instead of as a strip.</summary>
        public bool HeroMode
        {
            get => _heroMode;
            set
            {
                if (_heroMode == value) return;
                _heroMode = value;
                UpdatePadding();
                Invalidate(true);
            }
        }

        private bool DrawHero => _heroMode && _banner != null;

        private int BannerPixels =>
            _pause || DrawHero || (_banner == null && !TickerActive) ? 0 : Math.Max(1, Height * _s.BannerHeightPercent / 100);

        private const int LogoMargin = 12;

        private const int BottomRowPad = 8;

        private int LogoPixels => Math.Max(1, Height * _s.LogoHeightPercent / 100);
        private int BottomLogoPixels => Math.Max(1, Height * _s.BottomLogoHeightPercent / 100);

        /// <summary>Size a logo is drawn at: fixed height (bottom logos have their own), width capped so the screen centre stays free.</summary>
        private Size LogoSize(int slot)
        {
            Bitmap? logo = _logos[slot];
            if (logo == null) return Size.Empty;

            int h = slot >= 4 ? BottomLogoPixels : LogoPixels;
            int w = (int)Math.Round(logo.Width * (double)h / logo.Height);
            int maxW = (int)(Width * 0.28);
            if (w > maxW) { h = (int)Math.Round(h * (double)maxW / w); w = maxW; }
            return new Size(Math.Max(1, w), Math.Max(1, h));
        }

        // The bottom row is only as tall as the tallest bottom logo actually drawn, so a small logo costs little screen.
        private int BottomRowPixels =>
            _logos[4] == null && _logos[5] == null
                ? 0
                : Math.Max(LogoSize(4).Height, LogoSize(5).Height) + BottomRowPad * 2;

        /// <summary>Height the layout must reserve between the big number and the numbers strip (0 unless bottom logos are placed above it).</summary>
        public int SponsorRowAbovePixels => _s.BottomLogosAbove ? BottomRowPixels : 0;

        private void UpdatePadding()
        {
            if (_pause) { Padding = Padding.Empty; return; }

            int bh = BannerPixels;
            int belowStrip = _s.BottomLogosAbove ? 0 : BottomRowPixels;
            Padding = new Padding(0, _s.BannerAtTop ? bh : 0, 0, (_s.BannerAtTop ? 0 : bh) + belowStrip);
        }

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            UpdatePadding();
            if (_curSrc != null && ClientSize.Width > 0 && ClientSize.Height > 0)
            {
                _prevScaled?.Dispose();
                _prevScaled = null;
                _fade = 1f;
                RenderCurrent();
            }
        }

        // ------------------------------------------------------------------
        //  Slideshow
        // ------------------------------------------------------------------

        private void StartSlides()
        {
            _order = _s.SlideFiles.Where(File.Exists).ToList();
            if (_s.SlideShuffle)
                _order = _order.OrderBy(_ => _rng.Next()).ToList();

            _pos = -1;
            _curSrc?.Dispose(); _curSrc = null;
            _curScaled?.Dispose(); _curScaled = null;
            _prevScaled?.Dispose(); _prevScaled = null;

            ComposeFrame();
            if (_order.Count > 0) ShowNext(true);
        }

        private void StopSlides()
        {
            _slideTimer.Stop();
            _fadeTimer.Stop();
            _order.Clear();
            _curSrc?.Dispose(); _curSrc = null;
            _curScaled?.Dispose(); _curScaled = null;
            _prevScaled?.Dispose(); _prevScaled = null;
            _fade = 1f;
            ComposeFrame();
            Invalidate(true);
        }

        private void ShowNext(bool immediate)
        {
            Bitmap? src = null;
            for (int tries = 0; tries < _order.Count && src == null; tries++)
            {
                _pos = (_pos + 1) % _order.Count;
                src = LoadSafe(_order[_pos], 2560);
            }
            if (src == null) return;

            _curSrc?.Dispose();
            _curSrc = src;

            Bitmap? scaled = ClientSize.Width > 0 && ClientSize.Height > 0 ? Render(src) : null;

            if (immediate || _curScaled == null || scaled == null)
            {
                _curScaled?.Dispose();
                _prevScaled?.Dispose();
                _prevScaled = null;
                _curScaled = scaled;
                _fade = 1f;
            }
            else
            {
                _prevScaled?.Dispose();
                _prevScaled = _curScaled;
                _curScaled = scaled;
                _fade = 0f;
                _fadeTimer.Start();
            }
            ComposeFrame();
            Invalidate(true);
        }

        private void RenderCurrent()
        {
            if (_curSrc == null || ClientSize.Width <= 0 || ClientSize.Height <= 0) return;
            _curScaled?.Dispose();
            _curScaled = Render(_curSrc);
            ComposeFrame();
            Invalidate(true);
        }

        /// <summary>Blends the previous/current scaled pictures into <see cref="_frame"/> for the current fade step.</summary>
        private void ComposeFrame()
        {
            int w = ClientSize.Width, h = ClientSize.Height;
            if (_curScaled == null || w <= 0 || h <= 0)
            {
                ReleaseFrame();
                return;
            }

            if (_frame == null || _frame.Width != w || _frame.Height != h)
            {
                ReleaseFrame();
                _frame = new Bitmap(w, h, PixelFormat.Format32bppRgb);
            }

            using (Graphics g = Graphics.FromImage(_frame))
            {
                g.CompositingMode = CompositingMode.SourceCopy;
                if (_prevScaled != null && _fade < 1f)
                {
                    g.DrawImageUnscaled(_prevScaled, 0, 0);

                    g.CompositingMode = CompositingMode.SourceOver;
                    _fadeAttr.SetColorMatrix(new ColorMatrix { Matrix33 = _fade });
                    g.DrawImage(_curScaled, new Rectangle(0, 0, _curScaled.Width, _curScaled.Height),
                        0, 0, _curScaled.Width, _curScaled.Height, GraphicsUnit.Pixel, _fadeAttr);
                }
                else
                {
                    g.DrawImageUnscaled(_curScaled, 0, 0);
                }
            }

            if (_frameHandle != IntPtr.Zero) DeleteObject(_frameHandle);
            _frameHandle = _frame.GetHbitmap();
        }

        private void ReleaseFrame()
        {
            if (_frameHandle != IntPtr.Zero) { DeleteObject(_frameHandle); _frameHandle = IntPtr.Zero; }
            _frame?.Dispose();
            _frame = null;
        }

        /// <summary>Scales the source to the client size and bakes in the dimming overlay.</summary>
        private Bitmap Render(Bitmap src)
        {
            int w = ClientSize.Width, h = ClientSize.Height;
            var bmp = new Bitmap(w, h, PixelFormat.Format32bppPArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(BaseColor);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;

                double sx = (double)w / src.Width, sy = (double)h / src.Height;
                double scale = EffCrop ? Math.Max(sx, sy) : Math.Min(sx, sy);
                int dw = (int)Math.Round(src.Width * scale), dh = (int)Math.Round(src.Height * scale);

                // Opacity blends the picture into the dark base colour.
                using var attr = new ImageAttributes();
                attr.SetColorMatrix(new ColorMatrix { Matrix33 = EffOpacity / 100f });
                attr.SetWrapMode(WrapMode.TileFlipXY);
                g.DrawImage(src, new Rectangle((w - dw) / 2, (h - dh) / 2, dw, dh),
                    0, 0, src.Width, src.Height, GraphicsUnit.Pixel, attr);
            }
            return bmp;
        }

        // ------------------------------------------------------------------
        //  Painting
        // ------------------------------------------------------------------

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            // Transparent children repaint this background many times per frame with small clips,
            // so only the requested region is copied from the pre-composed frame.
            Rectangle clip = Rectangle.Intersect(e.ClipRectangle, ClientRectangle);
            if (clip.Width <= 0 || clip.Height <= 0) return;

            if (!BlitFrame(g, clip))
            {
                using var b = new SolidBrush(BaseColor);
                g.FillRectangle(b, clip);
            }

            // Tinted backing for the called-numbers strip; lower opacity lets the slideshow show through.
            if (!_pause && StripBounds != null && IsHandleCreated && _s.StripOpacityPercent > 0)
            {
                Rectangle strip = Rectangle.Intersect(StripBounds(), clip);
                if (strip.Width > 0 && strip.Height > 0)
                {
                    using var tint = new SolidBrush(Color.FromArgb(_s.StripOpacityPercent * 255 / 100, 22, 25, 36));
                    g.FillRectangle(tint, strip);
                }
            }
        }

        [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr hdc);
        [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);
        [DllImport("gdi32.dll")] private static extern bool BitBlt(IntPtr dest, int x, int y, int w, int h, IntPtr src, int sx, int sy, int rop);
        [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr hdc);
        [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr h);
        [DllImport("msimg32.dll")] private static extern bool AlphaBlend(IntPtr hdcDest, int xDest, int yDest, int wDest, int hDest,
            IntPtr hdcSrc, int xSrc, int ySrc, int wSrc, int hSrc, BLENDFUNCTION blend);

        [StructLayout(LayoutKind.Sequential)]
        private struct BLENDFUNCTION
        {
            public byte BlendOp, BlendFlags, SourceConstantAlpha, AlphaFormat;
        }

        /// <summary>
        /// Copies <paramref name="clip"/> of the composed frame with a plain GDI BitBlt. GDI+ DrawImage
        /// takes ~60 ms for a full-screen copy here (several copies per frame because transparent
        /// children repaint this background); BitBlt takes ~2 ms.
        /// </summary>
        private bool BlitFrame(Graphics g, Rectangle clip)
        {
            if (_frameHandle == IntPtr.Zero || _frame == null
                || _frame.Width != ClientSize.Width || _frame.Height != ClientSize.Height)
                return false;

            BitBltRect(g, _frameHandle, clip, clip.X, clip.Y);
            return true;
        }

        /// <summary>BitBlts part of an HBITMAP (from <paramref name="sx"/>,<paramref name="sy"/>) into <paramref name="dst"/> of this panel.</summary>
        private static void BitBltRect(Graphics g, IntPtr bitmap, Rectangle dst, int sx, int sy, int alpha = 255)
        {
            if (alpha <= 0) return;

            // Children are painted through a translated Graphics; BitBlt works in device coordinates.
            float ox, oy;
            using (var t = g.Transform) { ox = t.OffsetX; oy = t.OffsetY; }

            IntPtr hdc = g.GetHdc();
            try
            {
                IntPtr mem = CreateCompatibleDC(hdc);
                IntPtr old = SelectObject(mem, bitmap);
                if (alpha >= 255)
                {
                    BitBlt(hdc, dst.X + (int)ox, dst.Y + (int)oy, dst.Width, dst.Height, mem, sx, sy, 0x00CC0020 /* SRCCOPY */);
                }
                else
                {
                    // constant-alpha blend of an opaque source over what is already painted (the slideshow)
                    var blend = new BLENDFUNCTION { BlendOp = 0, BlendFlags = 0, SourceConstantAlpha = (byte)alpha, AlphaFormat = 0 };
                    AlphaBlend(hdc, dst.X + (int)ox, dst.Y + (int)oy, dst.Width, dst.Height, mem, sx, sy, dst.Width, dst.Height, blend);
                }
                SelectObject(mem, old);
                DeleteDC(mem);
            }
            finally
            {
                g.ReleaseHdc(hdc);
            }
        }

        private IntPtr _frameHandle;   // HBITMAP copy of _frame for BitBlt

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            Rectangle clip = e.ClipRectangle;

            if (_pause)
            {
                if (_curScaled == null) DrawPauseText(g);
                return;
            }

            // Transparent children repaint the parent many times per frame, so everything
            // below is drawn from pre-scaled copies and skipped when outside the clip.
            int bh = BannerPixels;
            if (DrawHero)
            {
                if (NumberBounds != null && IsHandleCreated)
                {
                    Rectangle box = NumberBounds();
                    box.Inflate(-20, -20);
                    if (box.Width > 0 && box.Height > 0)
                        DrawFit(g, clip, _banner!, box, ref _bannerScaled, BannerAttr);
                }
            }
            else if (bh > 0 && (_banner != null || TickerActive))
            {
                var strip = new Rectangle(0, _s.BannerAtTop ? 0 : Height - bh, Width, bh);
                if (strip.IntersectsWith(clip))
                {
                    if (TickerActive)
                    {
                        DrawTicker(g, strip, clip);
                    }
                    else
                    {
                        using (var b = new SolidBrush(Color.FromArgb(BannerAlpha, _bannerBack)))
                            g.FillRectangle(b, strip);
                        strip.Inflate(-8, -6);
                        DrawFit(g, clip, _banner!, strip, ref _bannerScaled, BannerAttr);
                    }
                }
            }

            // Slots: 0/1 top left/right, 2/3 middle left/right, 4/5 bottom left/right.
            int topY = (_s.BannerAtTop ? bh : 0) + LogoMargin;

            Rectangle nb = NumberBounds != null && IsHandleCreated ? NumberBounds() : new Rectangle(0, Height / 2, 0, 0);

            // Bottom row: directly above the numbers strip, or below it (above any bottom banner).
            int rowPx = BottomRowPixels;
            int rowTop = _s.BottomLogosAbove && StripBounds != null && IsHandleCreated
                ? StripBounds().Top - rowPx
                : Height - (_s.BannerAtTop ? 0 : bh) - rowPx;

            for (int i = 0; i < _logos.Length; i++)
            {
                Bitmap? logo = _logos[i];
                if (logo == null) continue;

                Size size = LogoSize(i);
                int w = size.Width, h = size.Height;

                int x = i % 2 == 0 ? LogoMargin : Width - LogoMargin - w;
                int y = i < 2 ? topY
                      : i < 4 ? nb.Top + (nb.Height - h) / 2
                      : rowTop + (rowPx - h) / 2;
                var rect = new Rectangle(x, y, w, h);
                if (!rect.IntersectsWith(clip)) continue;

                Bitmap scaled = GetScaled(ref _logoScaled[i], logo, w, h,
                    _s.LogoOpacityPercent >= 100 ? null : _logoAttr);
                g.DrawImageUnscaled(scaled, x, y);
            }
        }

        private void DrawPauseText(Graphics g)
        {
            using var font = new Font("Segoe UI", 72f, FontStyle.Bold, GraphicsUnit.Pixel);
            using var brush = new SolidBrush(Color.FromArgb(90, 100, 120));
            using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString("PAUSE", font, brush, ClientRectangle, format);
        }

        private Bitmap? _bannerScaled;
        private readonly Bitmap?[] _logoScaled = new Bitmap?[DisplaySettings.LogoSlots];

        private void ClearScaledCaches()
        {
            _bannerScaled?.Dispose(); _bannerScaled = null;
            for (int i = 0; i < _logoScaled.Length; i++) { _logoScaled[i]?.Dispose(); _logoScaled[i] = null; }
        }

        /// <summary>Returns the image scaled to w x h (opacity baked in via <paramref name="attr"/>), reusing the cache when the size is unchanged.</summary>
        private static Bitmap GetScaled(ref Bitmap? cache, Bitmap src, int w, int h, ImageAttributes? attr)
        {
            w = Math.Max(1, w); h = Math.Max(1, h);
            if (cache != null && cache.Width == w && cache.Height == h) return cache;

            cache?.Dispose();
            var bmp = new Bitmap(w, h, PixelFormat.Format32bppPArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                if (attr == null)
                    g.DrawImage(src, 0, 0, w, h);
                else
                    g.DrawImage(src, new Rectangle(0, 0, w, h), 0, 0, src.Width, src.Height, GraphicsUnit.Pixel, attr);
            }
            cache = bmp;
            return bmp;
        }

        private int BannerAlpha => _s.BannerOpacityPercent * 255 / 100;
        private ImageAttributes? BannerAttr => _s.BannerOpacityPercent >= 100 ? null : _bannerAttr;

        private static void DrawFit(Graphics g, Rectangle clip, Bitmap img, Rectangle box, ref Bitmap? cache, ImageAttributes? attr)
        {
            double scale = Math.Min((double)box.Width / img.Width, (double)box.Height / img.Height);
            int w = (int)Math.Round(img.Width * scale), h = (int)Math.Round(img.Height * scale);
            var target = new Rectangle(box.X + (box.Width - w) / 2, box.Y + (box.Height - h) / 2, w, h);
            if (!target.IntersectsWith(clip)) return;

            g.DrawImageUnscaled(GetScaled(ref cache, img, w, h, attr), target.X, target.Y);
        }

        // ------------------------------------------------------------------
        //  Image loading
        // ------------------------------------------------------------------

        private static void LoadInto(ref Bitmap? target, ref string currentPath, string wantedPath)
        {
            if (wantedPath == currentPath && (wantedPath == "" || target != null)) return;

            target?.Dispose();
            target = string.IsNullOrEmpty(wantedPath) ? null : LoadSafe(wantedPath, 1600);
            currentPath = wantedPath;
        }

        /// <summary>Loads an image without keeping the file locked, downscaled to at most <paramref name="maxDim"/>.</summary>
        private static Bitmap? LoadSafe(string path, int maxDim)
        {
            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var img = Image.FromStream(fs, false, false);

                double sc = Math.Min(1.0, (double)maxDim / Math.Max(img.Width, img.Height));
                int w = Math.Max(1, (int)(img.Width * sc)), h = Math.Max(1, (int)(img.Height * sc));

                var bmp = new Bitmap(w, h, PixelFormat.Format32bppPArgb);
                using var g = Graphics.FromImage(bmp);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(img, 0, 0, w, h);
                return bmp;
            }
            catch
            {
                return null;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _slideTimer.Dispose();
                _fadeTimer.Dispose();
                _fadeAttr.Dispose();
                _curSrc?.Dispose(); _curScaled?.Dispose(); _prevScaled?.Dispose(); ReleaseFrame();
                _tickerTimer.Dispose();
                foreach (Bitmap b in _tickerSources) b.Dispose();
                ReleaseTickerStrip();
                _banner?.Dispose();
                ClearScaledCaches();
                foreach (Bitmap? logo in _logos) logo?.Dispose();
                _logoAttr.Dispose();
                _bannerAttr.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
