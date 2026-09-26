// DrawSound.cs
using System.Media;

namespace BingoCaller
{
    /// <summary>
    /// The sound of the random draw, synthesised in code (no audio files, nothing to license): a tick for every rolling
    /// step, slowing down together with the animation, then a rising chime when the number lands. It is built as ONE clip
    /// from the same schedule as the animation (<see cref="DisplayForm.DrawHolds"/>), so the ticks always match the numbers.
    /// </summary>
    internal static class DrawSound
    {
        private const int Rate = 44100;
        private static byte[]? _wav;
        private static SoundPlayer? _player;

        /// <summary>Length in seconds of the rolling part (when the number lands and the chime starts).</summary>
        public static double LandingSeconds => DisplayForm.DrawHolds().Sum() / 1000.0;

        public static byte[] Wav => _wav ??= BuildWav();

        /// <summary>Plays the clip once. A missing or broken audio device must never disturb the game, so errors are swallowed.</summary>
        public static void Play()
        {
            try
            {
                Stop();
                _player = new SoundPlayer(new MemoryStream(Wav));
                _player.Load();
                _player.Play();   // asynchronous
            }
            catch { /* no audio device: the game continues silently */ }
        }

        public static void Stop()
        {
            try { _player?.Stop(); _player?.Dispose(); } catch { }
            _player = null;
        }

        /// <summary>Builds the 16-bit mono PCM WAV file.</summary>
        public static byte[] BuildWav()
        {
            List<int> holds = DisplayForm.DrawHolds();
            double landing = holds.Sum() / 1000.0;
            double total = landing + 0.33 + 0.9;
            var buf = new float[(int)(total * Rate) + 1];

            // ticks: one per rolling frame, pitch drifting down as the roll slows
            double t = 0;
            for (int k = 0; k < holds.Count; k++)
            {
                double freq = 1900 - 900.0 * k / Math.Max(1, holds.Count - 1);
                Tone(buf, t, freq, 0.040, 0.55, 0.008, 0.30);
                t += holds[k] / 1000.0;
            }

            // landing chime: C5 - E5 - G5 - C6, bell-like (second harmonic), the last one rings longest
            double[] notes = { 523.25, 659.25, 783.99, 1046.50 };
            for (int i = 0; i < notes.Length; i++)
                Tone(buf, landing + i * 0.11, notes[i], i == notes.Length - 1 ? 0.9 : 0.6, 0.42, i == notes.Length - 1 ? 0.22 : 0.14, 0.35);

            // fade the very end out so the clip never stops with a step (a click)
            int fade = (int)(0.04 * Rate);
            for (int i = 0; i < fade && i < buf.Length; i++)
                buf[buf.Length - 1 - i] *= (float)i / fade;

            float peak = buf.Max(Math.Abs);
            float scale = peak > 0.92f ? 0.92f / peak : 1f;   // never clip

            using var ms = new MemoryStream();
            using var w = new BinaryWriter(ms);
            int dataBytes = buf.Length * 2;
            w.Write(new[] { 'R', 'I', 'F', 'F' }); w.Write(36 + dataBytes); w.Write(new[] { 'W', 'A', 'V', 'E' });
            w.Write(new[] { 'f', 'm', 't', ' ' }); w.Write(16); w.Write((short)1); w.Write((short)1);
            w.Write(Rate); w.Write(Rate * 2); w.Write((short)2); w.Write((short)16);
            w.Write(new[] { 'd', 'a', 't', 'a' }); w.Write(dataBytes);
            foreach (float s in buf) w.Write((short)Math.Round(s * scale * short.MaxValue));
            return ms.ToArray();
        }

        private static void Tone(float[] buf, double startSec, double freq, double durSec, double amp, double decaySec, double harmonic)
        {
            int s0 = (int)(startSec * Rate), n = (int)(durSec * Rate), attack = (int)(0.002 * Rate);
            for (int i = 0; i < n && s0 + i < buf.Length; i++)
            {
                double x = (double)i / Rate;
                double env = Math.Exp(-x / decaySec) * Math.Min(1.0, (double)i / attack);   // short attack: no click from the start
                double v = Math.Sin(2 * Math.PI * freq * x) + harmonic * Math.Sin(4 * Math.PI * freq * x);
                buf[s0 + i] += (float)(amp * env * v);
            }
        }
    }
}
