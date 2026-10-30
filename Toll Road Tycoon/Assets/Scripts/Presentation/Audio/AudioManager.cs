using UnityEngine;

namespace TollRoadTycoon.Presentation
{
    public enum Sfx
    {
        Coin,
        Barrier,
        Purchase,
        Error,
        Click,
    }

    /// <summary>
    /// Short procedural sound effects (no imported audio yet). Coins are rate-limited so a busy
    /// plaza does not stack dozens of overlapping sounds. Music is not implemented in this phase.
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        private const int SampleRate = 44100;
        private AudioSource _source;
        private AudioClip[] _clips;
        private float _nextCoinTime;

        public bool SfxEnabled { get; set; } = true;
        public float SfxVolume { get; set; } = 0.8f;

        private void Awake()
        {
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _clips = new[]
            {
                Tone("coin", new[] { 1318f, 1760f }, 0.07f, 0.35f),
                Tone("barrier", new[] { 220f }, 0.06f, 0.25f),
                Tone("purchase", new[] { 784f, 988f, 1318f }, 0.07f, 0.4f),
                Tone("error", new[] { 180f, 150f }, 0.09f, 0.35f),
                Tone("click", new[] { 900f }, 0.025f, 0.25f),
            };
        }

        public void Play(Sfx sfx)
        {
            if (!SfxEnabled || SfxVolume <= 0) return;
            if (sfx == Sfx.Coin)
            {
                if (Time.unscaledTime < _nextCoinTime) return;
                _nextCoinTime = Time.unscaledTime + 0.12f;
            }
            _source.PlayOneShot(_clips[(int)sfx], SfxVolume);
        }

        /// <summary>Sequence of short sine notes with a soft attack/decay envelope.</summary>
        private static AudioClip Tone(string name, float[] notes, float noteSeconds, float gain)
        {
            int perNote = (int)(SampleRate * noteSeconds);
            var data = new float[perNote * notes.Length];
            for (int n = 0; n < notes.Length; n++)
            {
                for (int i = 0; i < perNote; i++)
                {
                    float t = (float)i / SampleRate;
                    float env = Mathf.Min(1f, i / (SampleRate * 0.004f)) * (1f - (float)i / perNote);
                    data[n * perNote + i] = Mathf.Sin(2 * Mathf.PI * notes[n] * t) * env * gain;
                }
            }
            var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
