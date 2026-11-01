using UnityEngine;

namespace VoxelBloom
{
    public enum Sfx { Button, Select, Launch, Pop, Combo, Coin, Star, Win, Fail, Booster, Error, Whoosh }

    /// <summary>
    /// Synthesizes all sound effects and the music loop at startup (original, license-free audio).
    /// Replace with authored clips by assigning AudioService overrides if desired.
    /// </summary>
    public static class SfxSynth
    {
        const int Rate = 22050;

        public static AudioClip Make(Sfx s)
        {
            switch (s)
            {
                case Sfx.Button: return Tone("btn", 0.07f, t => Sine(t, 880) * Env(t, 0.005f, 0.06f) * 0.5f);
                case Sfx.Select: return Tone("select", 0.12f, t => (Sine(t, 660 + 900 * t)) * Env(t, 0.005f, 0.1f) * 0.45f);
                case Sfx.Launch: return Tone("launch", 0.18f, t => (Square(t, 300 + 1400 * t) * 0.25f + Noise() * 0.1f) * Env(t, 0.005f, 0.17f) * 0.5f);
                case Sfx.Pop: return Tone("pop", 0.09f, t => (Sine(t, 520 - 2400 * t) + Noise() * 0.25f) * Env(t, 0.002f, 0.08f) * 0.55f);
                case Sfx.Combo: return Tone("combo", 0.45f, t => (Sine(t, Arp(t, 0.07f, 523, 659, 784, 1047)) + 0.3f * Sine(t, 2 * Arp(t, 0.07f, 523, 659, 784, 1047))) * Env(t, 0.005f, 0.42f) * 0.4f);
                case Sfx.Coin: return Tone("coin", 0.25f, t => Square(t, t < 0.07f ? 988 : 1319) * Env(t, 0.003f, 0.24f) * 0.22f);
                case Sfx.Star: return Tone("star", 0.5f, t => (Sine(t, 1200) * 0.6f + Sine(t, 1800) * 0.3f + Sine(t, 2400) * 0.15f) * Env(t, 0.005f, 0.48f) * 0.4f);
                case Sfx.Win: return Tone("win", 1.1f, t => (Sine(t, Arp(t, 0.13f, 523, 659, 784, 1047, 1319, 1568, 2093)) * 0.6f + Tri(t, Arp(t, 0.13f, 262, 330, 392, 523, 659, 784, 1047)) * 0.3f) * Env(t, 0.01f, 1.05f) * 0.45f);
                case Sfx.Fail: return Tone("fail", 0.8f, t => Tri(t, 392 - 180 * t) * Env(t, 0.01f, 0.78f) * 0.45f);
                case Sfx.Booster: return Tone("booster", 0.5f, t => (Sine(t, 200 + 1600 * t * t) * 0.5f + Noise() * 0.25f * (1 - t * 2)) * Env(t, 0.005f, 0.48f) * 0.5f);
                case Sfx.Error: return Tone("error", 0.2f, t => Square(t, 160) * Env(t, 0.005f, 0.19f) * 0.2f);
                case Sfx.Whoosh: return Tone("whoosh", 0.25f, t => Noise() * Mathf.Sin(t / 0.25f * Mathf.PI) * 0.25f);
            }
            return null;
        }

        /// <summary>Gentle 16-bar pentatonic loop (~19 s).</summary>
        public static AudioClip Music()
        {
            int[] melody = { 0, 2, 4, 7, 4, 2, 9, 7, 4, 7, 9, 12, 9, 7, 4, 2, 0, 4, 7, 9, 7, 4, 2, 4, 2, 0, -3, 0, 2, 4, 2, 0 };
            int[] bass = { 0, 0, -5, -5, -3, -3, -7, -7 };
            float beat = 0.3f;
            float len = melody.Length * beat * 2f;
            int n = (int)(len * Rate);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                int step = (int)(t / beat);
                float tl = t - step * beat;
                int note = melody[step % melody.Length];
                float f = 523.25f * Mathf.Pow(2f, note / 12f);
                float m = (Sine(tl, f) * 0.6f + Sine(tl, f * 2) * 0.12f) * Env(tl, 0.01f, beat * 0.95f) * (step % 2 == 0 ? 1f : 0.7f);
                int bstep = (int)(t / (beat * 4));
                float tb = t - bstep * beat * 4;
                float fb = 130.81f * Mathf.Pow(2f, bass[bstep % bass.Length] / 12f);
                float b = Tri(tb, fb) * Env(tb, 0.02f, beat * 3.8f) * 0.35f;
                float pad = Sine(t, fb * 2) * 0.05f;
                data[i] = (m * 0.32f + b * 0.5f + pad) * 0.6f;
            }
            var clip = AudioClip.Create("music_loop", n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float noiseState = 1;
        static float Noise() { noiseState = (noiseState * 16807f) % 2147483647f; return noiseState / 1073741823.5f - 1f; }
        static float Sine(float t, float f) => Mathf.Sin(2 * Mathf.PI * f * t);
        static float Square(float t, float f) => Mathf.Sign(Mathf.Sin(2 * Mathf.PI * f * t));
        static float Tri(float t, float f) => 2f * Mathf.Abs(2f * (t * f - Mathf.Floor(t * f + 0.5f))) - 1f;
        static float Env(float t, float a, float d) => t < a ? t / a : Mathf.Clamp01(1f - (t - a) / Mathf.Max(0.0001f, d - a)) * Mathf.Clamp01(1f - (t - a) / Mathf.Max(0.0001f, d - a));
        static float Arp(float t, float step, params float[] notes) => notes[Mathf.Min(notes.Length - 1, (int)(t / step))];

        static AudioClip Tone(string name, float seconds, System.Func<float, float> f)
        {
            int n = Mathf.CeilToInt(seconds * Rate);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(f(i / (float)Rate), -1f, 1f);
            var clip = AudioClip.Create("sfx_" + name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
