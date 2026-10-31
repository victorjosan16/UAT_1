using UnityEngine;

namespace Veliqora.Audio
{
    /// <summary>
    /// PLACEHOLDER audio synthesized at runtime (no external assets). Replace by placing clips with the same
    /// names under Resources/Veliqora/Audio/ (e.g. "pour", "win", "music").
    /// </summary>
    public static class ProceduralAudio
    {
        private const int Rate = 44100;

        public static AudioClip Load(string name, System.Func<AudioClip> fallback)
        {
            var authored = Resources.Load<AudioClip>("Veliqora/Audio/" + name);
            return authored != null ? authored : fallback();
        }

        private static AudioClip Make(string name, float seconds, System.Func<float, float> wave)
        {
            int n = Mathf.CeilToInt(seconds * Rate);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(wave(i / (float)Rate), -1f, 1f);
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static float Env(float t, float attack, float decay) => t < attack ? t / attack : Mathf.Exp(-(t - attack) / decay);
        private static float Sine(float f, float t) => Mathf.Sin(2f * Mathf.PI * f * t);

        public static AudioClip Tap() => Make("tap", 0.08f, t => Sine(880f, t) * Env(t, 0.003f, 0.025f) * 0.5f);

        public static AudioClip Select() => Make("select", 0.12f, t => Sine(Mathf.Lerp(620f, 990f, t / 0.12f), t) * Env(t, 0.004f, 0.04f) * 0.45f);

        public static AudioClip Invalid() => Make("invalid", 0.2f, t =>
            (Sine(190f, t) * 0.7f + Sine(380f, t) * 0.2f) * Env(t, 0.005f, 0.07f) * 0.5f);

        public static AudioClip Pour()
        {
            var rng = new System.Random(3);
            float lp = 0f;
            float[] bubbleT = new float[12];
            float[] bubbleF = new float[12];
            for (int i = 0; i < bubbleT.Length; i++) { bubbleT[i] = (float)rng.NextDouble() * 0.6f; bubbleF[i] = 500f + (float)rng.NextDouble() * 700f; }
            return Make("pour", 0.7f, t =>
            {
                float noise = (float)rng.NextDouble() * 2f - 1f;
                lp += (noise - lp) * 0.08f; // low-pass "water" noise
                float body = lp * 0.9f * (t < 0.05f ? t / 0.05f : (t > 0.55f ? Mathf.Max(0, (0.7f - t) / 0.15f) : 1f));
                float bubbles = 0f;
                for (int i = 0; i < bubbleT.Length; i++)
                {
                    float dt = t - bubbleT[i];
                    if (dt < 0f || dt > 0.06f) continue;
                    bubbles += Sine(bubbleF[i] * (1f + dt * 6f), dt) * Mathf.Exp(-dt * 60f) * 0.25f;
                }
                return body * 0.55f + bubbles;
            });
        }

        private static float Bell(float f, float t) => (Sine(f, t) + 0.4f * Sine(f * 2.01f, t) + 0.2f * Sine(f * 3.02f, t)) * Env(t, 0.004f, 0.25f);

        public static AudioClip Complete() => Make("complete", 0.6f, t =>
            (Bell(1046.5f, t) + (t > 0.08f ? Bell(1318.5f, t - 0.08f) : 0f) + (t > 0.16f ? Bell(1568f, t - 0.16f) : 0f)) * 0.22f);

        public static AudioClip Win() => Make("win", 1.6f, t =>
        {
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f, 1318.5f };
            float s = 0f;
            for (int i = 0; i < notes.Length; i++)
            {
                float st = i * 0.11f;
                if (t > st) s += Bell(notes[i], t - st);
            }
            return s * 0.16f;
        });

        public static AudioClip Coin() => Make("coin", 0.25f, t =>
            (t < 0.07f ? Sine(1320f, t) : Sine(1760f, t)) * Env(t, 0.002f, 0.08f) * 0.35f);

        /// <summary>Streaming ambient loop (generated on the audio thread as it plays; zero startup cost).</summary>
        public static AudioClip Music()
        {
            const int rate = 22050;
            const float bars = 4f, barSeconds = 4f;
            int length = (int)(rate * bars * barSeconds);
            // Cmaj7, Am7, Fmaj7, G6 (pads) — gentle and loopable.
            float[][] chords =
            {
                new[] { 261.63f, 329.63f, 392.00f, 493.88f },
                new[] { 220.00f, 261.63f, 329.63f, 392.00f },
                new[] { 174.61f, 220.00f, 261.63f, 329.63f },
                new[] { 196.00f, 246.94f, 293.66f, 329.63f },
            };
            int position = 0;
            AudioClip.PCMReaderCallback reader = data =>
            {
                for (int i = 0; i < data.Length; i++, position++)
                {
                    int p = position % length;
                    float t = p / (float)rate;
                    int bar = (int)(t / barSeconds) % 4;
                    float inBar = t - bar * barSeconds;
                    var chord = chords[bar];
                    float pad = 0f;
                    for (int k = 0; k < chord.Length; k++) pad += Mathf.Sin(2f * Mathf.PI * chord[k] * 0.5f * t) * (0.6f + 0.4f * Mathf.Sin(t * 0.7f + k));
                    float fade = Mathf.Clamp01(inBar / 0.4f) * Mathf.Clamp01((barSeconds - inBar) / 0.4f);
                    // soft plucked arpeggio on eighth notes
                    float step = 0.5f;
                    int idx = (int)(inBar / step);
                    float pt = inBar - idx * step;
                    float pluck = Mathf.Sin(2f * Mathf.PI * chord[idx % chord.Length] * 2f * pt) * Mathf.Exp(-pt * 7f);
                    data[i] = (pad * 0.05f * (0.55f + 0.45f * fade) + pluck * 0.07f);
                }
            };
            AudioClip.PCMSetPositionCallback setPos = newPos => position = newPos;
            return AudioClip.Create("music", length, 1, rate, true, reader, setPos);
        }
    }
}
