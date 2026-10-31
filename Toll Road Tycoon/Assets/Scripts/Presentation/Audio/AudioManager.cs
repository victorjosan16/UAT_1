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
    /// Procedural audio (no imported files needed): short sound effects and a soft looping music
    /// track. Coins are rate-limited so a busy plaza does not stack dozens of overlapping sounds.
    /// Music and effects have independent switches and volumes.
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        private const int SampleRate = 44100;
        private AudioSource _source;
        private AudioClip[] _clips;
        private float _nextCoinTime;

        private AudioSource _music;

        public bool SfxEnabled { get; set; } = true;
        public float SfxVolume { get; set; } = 0.8f;

        public void SetMusic(bool enabled, float volume)
        {
            if (_music == null) return;
            _music.volume = Mathf.Clamp01(volume) * 0.35f;
            if (enabled && !_music.isPlaying) _music.Play();
            else if (!enabled && _music.isPlaying) _music.Stop();
        }

        private void Awake()
        {
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _music = gameObject.AddComponent<AudioSource>();
            _music.playOnAwake = false;
            _music.loop = true;
            _music.clip = BuildMusicLoop();
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

        /// <summary>
        /// Eight-bar cheerful loop: soft triangle-wave melody over a bass line (C - Am - F - G).
        /// Generated once at start-up (about 16 s of mono audio).
        /// </summary>
        private static AudioClip BuildMusicLoop()
        {
            const float bpm = 120f;
            float beat = 60f / bpm;
            int[] chordRoots = { 48, 45, 41, 43 };
            int[] melody =
            {
                72, 76, 79, 76, 74, 72, 74, 76, 69, 72, 76, 72, 71, 69, 71, 72,
                65, 69, 72, 69, 67, 65, 67, 69, 67, 71, 74, 71, 72, 74, 71, 67,
            };
            int beats = melody.Length;
            int total = (int)(SampleRate * beat * beats);
            var data = new float[total];
            for (int i = 0; i < total; i++)
            {
                float t = (float)i / SampleRate;
                int b = Mathf.Min(beats - 1, (int)(t / beat));
                float inBeat = t - b * beat;
                float mf = Midi(melody[b]);
                float envelope = Mathf.Exp(-inBeat * 3.2f) * Mathf.Min(1f, inBeat * 200f);
                float lead = Triangle(mf * t) * envelope * 0.22f;
                int bar = (b / 8) % chordRoots.Length;
                float bf = Midi(chordRoots[bar]);
                float bassEnvelope = Mathf.Exp(-(inBeat % (beat / 2)) * 4f);
                float bass = Mathf.Sin(2 * Mathf.PI * bf * t) * bassEnvelope * 0.18f;
                data[i] = lead + bass;
            }
            var clip = AudioClip.Create("music", total, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static float Midi(int note) => 440f * Mathf.Pow(2f, (note - 69) / 12f);

        private static float Triangle(float phase)
        {
            float p = phase - Mathf.Floor(phase);
            return 4f * Mathf.Abs(p - 0.5f) - 1f;
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
