using System;
using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Sunete generate din cod (sinusoide + zgomot), ca jocul să aibă audio fără fișiere externe.
    /// Pot fi înlocuite oricând cu fișiere .wav/.ogg proprii.
    /// </summary>
    public static class ProceduralSfx
    {
        private const int SampleRate = 44100;
        private static readonly System.Random Random = new(1234);

        private static AudioClip _hit, _break, _pop, _coin, _pay, _purchase, _upgrade, _error, _layerClear;

        public static AudioClip Hit => _hit ??= Create("Hit", 0.12f,
            t => Noise() * Decay(t, 60f) * 0.45f + Sine(140f, t) * Decay(t, 35f) * 0.7f);

        public static AudioClip Break => _break ??= Create("Break", 0.35f,
            t => Noise() * Decay(t, 16f) * 0.55f + Sine(80f, t) * Decay(t, 18f) * 0.6f);

        public static AudioClip Pop => _pop ??= Create("Pop", 0.08f,
            t => Mathf.Sin(2f * Mathf.PI * (700f * t + 3000f * t * t)) * Decay(t, 30f) * 0.35f);

        public static AudioClip Coin => _coin ??= Create("Coin", 0.25f,
            t => (Sine(1318f, t) + (t > 0.05f ? Sine(1760f, t) : 0f)) * Decay(t, 18f) * 0.3f);

        public static AudioClip Pay => _pay ??= Create("Pay", 0.04f,
            t => Sine(1000f, t) * Decay(t, 80f) * 0.25f);

        public static AudioClip Purchase => _purchase ??= Create("Purchase", 0.42f, t =>
        {
            float[] notes = { 523f, 659f, 784f, 1046f };
            int index = Mathf.Min(notes.Length - 1, (int)(t / 0.09f));
            return Sine(notes[index], t) * Decay(t % 0.09f, 12f) * 0.35f;
        });

        public static AudioClip Upgrade => _upgrade ??= Create("Upgrade", 0.45f,
            t => Mathf.Sin(2f * Mathf.PI * (300f * t + 900f * t * t)) * (1f - t / 0.45f) * 0.35f);

        public static AudioClip Error => _error ??= Create("Error", 0.2f,
            t => Mathf.Sign(Sine(150f, t)) * Decay(t, 8f) * 0.15f);

        public static AudioClip LayerClear => _layerClear ??= Create("LayerClear", 1f,
            t => (Sine(523f, t) + Sine(659f, t) + Sine(784f, t)) / 3f * Decay(t, 3f) * 0.45f);

        private static float Sine(float frequency, float t) => Mathf.Sin(2f * Mathf.PI * frequency * t);
        private static float Decay(float t, float speed) => Mathf.Exp(-t * speed);
        private static float Noise() => (float)(Random.NextDouble() * 2.0 - 1.0);

        private static AudioClip Create(string name, float duration, Func<float, float> generator)
        {
            int length = Mathf.Max(1, (int)(SampleRate * duration));
            var data = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)SampleRate;
                // Atenuăm ultimele 5 ms ca să nu se audă un „clic” la final.
                float fadeOut = Mathf.Clamp01((duration - t) / 0.005f);
                data[i] = Mathf.Clamp(generator(t) * fadeOut, -1f, 1f);
            }
            AudioClip clip = AudioClip.Create(name, length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
