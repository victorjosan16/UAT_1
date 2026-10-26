using UndergroundMetroTycoon.Simulation;
using UnityEngine;

namespace UndergroundMetroTycoon.Game
{
    /// <summary>
    /// Sound effects synthesised at startup (no audio assets needed): shovel thud, soil break, coin, fanfare,
    /// level-up and UI click. Uses a small round-robin pool of AudioSources.
    /// </summary>
    public sealed class SfxPlayer : MonoBehaviour
    {
        private const int SampleRate = 22050;
        private const int Voices = 6;

        private readonly AudioSource[] _sources = new AudioSource[Voices];
        private int _next;
        private float _lastCoinTime;
        private AudioClip _thud, _rockHit, _break, _coin, _fanfare, _levelUp, _click;

        public bool Enabled { get; set; } = true;

        public void Init()
        {
            for (int i = 0; i < Voices; i++)
            {
                _sources[i] = gameObject.AddComponent<AudioSource>();
                _sources[i].playOnAwake = false;
                _sources[i].spatialBlend = 0f;
            }
            var rng = new System.Random(7);
            _thud = Thud(rng, 0.16f, 95f, 900f);
            _rockHit = Thud(rng, 0.12f, 180f, 3200f);
            _break = Crunch(rng, 0.28f);
            _coin = Tones("Coin", new[] { 1318.5f, 1760f }, 0.07f, 0.35f);
            _fanfare = Tones("Fanfare", new[] { 523.3f, 659.3f, 784f, 1046.5f }, 0.11f, 0.3f);
            _levelUp = Tones("LevelUp", new[] { 784f, 988f, 1175f, 1568f }, 0.09f, 0.3f);
            _click = Tones("Click", new[] { 1900f }, 0.03f, 0.25f);
        }

        public void PlayHit(TerrainMaterial material) => Play(material >= TerrainMaterial.Stone ? _rockHit : _thud, 0.8f, Random.Range(0.9f, 1.1f));
        public void PlayBreak() => Play(_break, 0.9f, Random.Range(0.92f, 1.08f));
        public void PlayFanfare() => Play(_fanfare, 0.8f, 1f);
        public void PlayLevelUp() => Play(_levelUp, 0.8f, 1f);
        public void PlayClick() => Play(_click, 0.5f, 1f);

        public void PlayCoin()
        {
            if (Time.unscaledTime - _lastCoinTime < 0.05f) return;
            _lastCoinTime = Time.unscaledTime;
            Play(_coin, 0.45f, Random.Range(0.97f, 1.06f));
        }

        private void Play(AudioClip clip, float volume, float pitch)
        {
            if (!Enabled || clip == null) return;
            AudioSource source = _sources[_next];
            _next = (_next + 1) % Voices;
            source.pitch = pitch;
            source.PlayOneShot(clip, volume);
        }

        private static AudioClip Thud(System.Random rng, float seconds, float bodyHz, float cutoffHz)
        {
            int n = (int)(SampleRate * seconds);
            var data = new float[n];
            float alpha = Mathf.Clamp01(2f * Mathf.PI * cutoffHz / SampleRate);
            float low = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-t * 28f);
                low += alpha * ((float)rng.NextDouble() * 2f - 1f - low);
                data[i] = (low * 0.9f + Mathf.Sin(2f * Mathf.PI * bodyHz * t) * 0.6f) * env;
            }
            return Clip("Thud", data);
        }

        private static AudioClip Crunch(System.Random rng, float seconds)
        {
            int n = (int)(SampleRate * seconds);
            var data = new float[n];
            float low = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-t * 14f);
                low += 0.25f * ((float)rng.NextDouble() * 2f - 1f - low);
                float crackle = rng.NextDouble() < 0.02 ? ((float)rng.NextDouble() - 0.5f) * 1.4f : 0f;
                data[i] = (low * 1.2f + crackle + Mathf.Sin(2f * Mathf.PI * 70f * t) * 0.4f) * env;
            }
            return Clip("Break", data);
        }

        private static AudioClip Tones(string name, float[] frequencies, float noteSeconds, float gain)
        {
            int perNote = (int)(SampleRate * noteSeconds);
            int tail = (int)(SampleRate * 0.12f);
            var data = new float[perNote * frequencies.Length + tail];
            for (int note = 0; note < frequencies.Length; note++)
            {
                int start = note * perNote;
                int length = note == frequencies.Length - 1 ? perNote + tail : perNote;
                for (int i = 0; i < length; i++)
                {
                    float t = (float)i / SampleRate;
                    float env = Mathf.Min(1f, i / 80f) * Mathf.Exp(-t * 9f);
                    float f = frequencies[note];
                    data[start + i] += (Mathf.Sin(2f * Mathf.PI * f * t) + 0.3f * Mathf.Sin(4f * Mathf.PI * f * t)) * env * gain;
                }
            }
            return Clip(name, data);
        }

        private static AudioClip Clip(string name, float[] data)
        {
            float peak = 0.0001f;
            foreach (float s in data) peak = Mathf.Max(peak, Mathf.Abs(s));
            float scale = Mathf.Min(1f, 0.95f / peak);
            for (int i = 0; i < data.Length; i++) data[i] *= scale;
            AudioClip clip = AudioClip.Create("UMT_" + name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
