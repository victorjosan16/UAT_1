using UnityEngine;

namespace LastBlock.Effects
{
    /// <summary>
    /// Generates the small set of sound effects in code, so the prototype needs no audio assets.
    /// </summary>
    public static class ProceduralAudio
    {
        const int SampleRate = 44100;

        public static AudioClip CreateClick() =>
            Create("Click", 0.06f, t => Mathf.Sin(2f * Mathf.PI * 950f * t) * Envelope(t, 0.06f, 0.002f));

        public static AudioClip CreatePop() =>
            Create("Pop", 0.16f, t =>
            {
                float frequency = Mathf.Lerp(320f, 980f, t / 0.16f);
                return Mathf.Sin(2f * Mathf.PI * frequency * t) * Envelope(t, 0.16f, 0.005f);
            });

        public static AudioClip CreateThud()
        {
            // Low sine plus a little filtered noise.
            System.Random random = new System.Random(7);
            float smoothed = 0f;
            return Create("Thud", 0.18f, t =>
            {
                float noise = (float)(random.NextDouble() * 2.0 - 1.0);
                smoothed = Mathf.Lerp(smoothed, noise, 0.08f);
                float tone = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(140f, 70f, t / 0.18f) * t);
                return (tone * 0.7f + smoothed * 1.5f) * Envelope(t, 0.18f, 0.002f);
            });
        }

        public static AudioClip CreateDeny() =>
            Create("Deny", 0.22f, t =>
            {
                float gate = t < 0.09f || t > 0.13f ? 1f : 0f;
                return Square(180f, t) * 0.4f * gate * Envelope(t, 0.22f, 0.003f);
            });

        public static AudioClip CreateWin()
        {
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
            const float noteLength = 0.13f;
            return Create("Win", noteLength * notes.Length + 0.35f, t =>
            {
                int index = Mathf.Min(notes.Length - 1, (int)(t / noteLength));
                float local = t - index * noteLength;
                float length = index == notes.Length - 1 ? noteLength + 0.35f : noteLength;
                return (Mathf.Sin(2f * Mathf.PI * notes[index] * t) * 0.8f + Mathf.Sin(4f * Mathf.PI * notes[index] * t) * 0.15f)
                       * Envelope(local, length, 0.005f);
            });
        }

        public static AudioClip CreateLose() =>
            Create("Lose", 0.7f, t =>
            {
                float frequency = Mathf.Lerp(420f, 110f, t / 0.7f);
                return (Mathf.Sin(2f * Mathf.PI * frequency * t) * 0.7f + Square(frequency * 0.5f, t) * 0.15f) * Envelope(t, 0.7f, 0.01f);
            });

        static float Square(float frequency, float t) => Mathf.Sin(2f * Mathf.PI * frequency * t) >= 0f ? 1f : -1f;

        /// <summary>Short linear attack, exponential-ish decay to zero at the end of the clip.</summary>
        static float Envelope(float t, float length, float attack)
        {
            if (t < attack) return t / attack;
            float k = Mathf.Clamp01((t - attack) / Mathf.Max(0.0001f, length - attack));
            return (1f - k) * (1f - k);
        }

        static AudioClip Create(string name, float length, System.Func<float, float> generator)
        {
            int samples = Mathf.Max(1, Mathf.CeilToInt(length * SampleRate));
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                data[i] = Mathf.Clamp(generator(i / (float)SampleRate) * 0.6f, -1f, 1f);
            }
            AudioClip clip = AudioClip.Create(name, samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
