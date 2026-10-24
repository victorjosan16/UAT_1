using System;
using QuarryEmpire;
using UnityEngine;

namespace SugarDrop
{
    public enum SugarSound
    {
        Swap, Invalid, Striped, Wrapped, Rainbow, SpecialCreated, Ice, Biscuit, Candy,
        Win, Lose, Click, Coin, Shuffle, Star, Bomb,
    }

    /// <summary>
    /// Sunetele Sugar Drop, generate din cod. Fiecare aromă are nota ei (gamă pentatonică), iar într-o mutare
    /// cu multe alinieri notele urcă pe rând — lanțurile sună ca o melodie. Muzica de fundal e generată și ea.
    /// Respectă setările din salvare (sunet, muzică, vibrații).
    /// </summary>
    public class SugarAudio : MonoBehaviour
    {
        public static SugarAudio Instance { get; private set; }

        private const int SampleRate = 44100;
        private const int Voices = 10;

        // Do, Re, Mi, Sol, La (octava 5): câte o notă pentru fiecare aromă.
        private static readonly float[] Pentatonic = { 523.25f, 587.33f, 659.25f, 783.99f, 880f };

        private readonly AudioSource[] _voices = new AudioSource[Voices];
        private AudioSource _music;
        private int _nextVoice;
        private AudioClip[] _pops;
        private AudioClip[] _clips;
        private readonly float[] _lastPlayed = new float[32];
        private readonly System.Random _random = new(77);

        private void Awake()
        {
            Instance = this;
            for (int i = 0; i < Voices; i++)
            {
                _voices[i] = gameObject.AddComponent<AudioSource>();
                _voices[i].playOnAwake = false;
            }
            _music = gameObject.AddComponent<AudioSource>();
            _music.loop = true;
            _music.playOnAwake = false;
            _music.volume = 0.28f;
            BuildClips();
        }

        private void Start()
        {
            _music.clip = ProceduralMusic.CreateLoop();
            ApplySettings();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Pornește/oprește muzica după setări (apelat și după schimbarea lor).</summary>
        public void ApplySettings()
        {
            bool musicOn = SugarSave.Data.music;
            if (musicOn && !_music.isPlaying && _music.clip != null) _music.Play();
            else if (!musicOn && _music.isPlaying) _music.Stop();
        }

        // ------------------------------------------------------------ Redare

        /// <summary>Pocnetul unui capac: nota aromei, urcată cu „chain” semitonuri (lanțuri melodice).</summary>
        public void PlayPop(int color, int chain)
        {
            if (!SugarSave.Data.sound || _pops == null) return;
            if (Time.unscaledTime - _lastPlayed[16 + Mathf.Clamp(color, 0, 4)] < 0.035f) return;
            _lastPlayed[16 + Mathf.Clamp(color, 0, 4)] = Time.unscaledTime;
            float pitch = Mathf.Pow(2f, Mathf.Min(chain, 12) / 12f);
            PlayClip(_pops[Mathf.Clamp(color, 0, _pops.Length - 1)], 0.55f, pitch);
        }

        public void Play(SugarSound sound, float volume = 1f, float pitch = 1f)
        {
            if (!SugarSave.Data.sound || _clips == null) return;
            int index = (int)sound;
            float minInterval = sound == SugarSound.Candy ? 0.045f : 0.03f;
            if (Time.unscaledTime - _lastPlayed[index] < minInterval) return;
            _lastPlayed[index] = Time.unscaledTime;
            if (sound == SugarSound.Candy) pitch *= 1f + (float)(_random.NextDouble() - 0.5) * 0.3f;
            PlayClip(_clips[index], volume, pitch);
        }

        private void PlayClip(AudioClip clip, float volume, float pitch)
        {
            AudioSource voice = _voices[_nextVoice];
            _nextVoice = (_nextVoice + 1) % Voices;
            voice.pitch = pitch;
            voice.PlayOneShot(clip, volume);
        }

        // ------------------------------------------------------------ Vibrații

        public static void Buzz(int strength)
        {
            if (!SugarSave.Data.vibration) return;
            if (strength <= 0) Haptics.Light();
            else if (strength == 1) Haptics.Medium();
            else Haptics.Heavy();
        }

        // ------------------------------------------------------------ Sinteză

        private void BuildClips()
        {
            _pops = new AudioClip[Pentatonic.Length];
            for (int i = 0; i < Pentatonic.Length; i++)
            {
                float f = Pentatonic[i];
                // „Pluc” dulce: sinus cu armonica a doua și o mică alunecare în jos a frecvenței.
                _pops[i] = Create("Pop" + i, 0.22f, t =>
                {
                    float freq = f * (1f + 0.25f * Mathf.Exp(-t * 60f));
                    return (Sine(freq, t) + 0.35f * Sine(freq * 2f, t) + 0.1f * Sine(freq * 3f, t)) * Decay(t, 18f) * 0.45f;
                });
            }

            _clips = new AudioClip[Enum.GetValues(typeof(SugarSound)).Length];
            _clips[(int)SugarSound.Swap] = Create("Swap", 0.12f, t => Noise() * Mathf.Sin(t / 0.12f * Mathf.PI) * 0.12f + Sweep(900f, 1400f, 0.12f, t) * Decay(t, 25f) * 0.12f);
            _clips[(int)SugarSound.Invalid] = Create("Invalid", 0.28f, t => Sweep(420f, 220f, 0.28f, t) * Decay(t, 9f) * 0.35f * (1f + 0.4f * Mathf.Sin(t * 90f)));
            _clips[(int)SugarSound.Striped] = Create("Striped", 0.4f, t => (Sweep(2200f, 300f, 0.4f, t) * 0.4f + Noise() * 0.12f) * Decay(t, 7f));
            _clips[(int)SugarSound.Wrapped] = Create("Wrapped", 0.6f, t => (Sweep(160f, 45f, 0.6f, t) * 0.8f + Noise() * Decay(t, 14f) * 0.5f) * Decay(t, 5f) * 0.7f);
            _clips[(int)SugarSound.Rainbow] = Create("Rainbow", 0.9f, t =>
            {
                // Arpegiu „sclipitor” rapid în sus.
                int step = Mathf.Min(7, (int)(t / 0.07f));
                float f = Pentatonic[step % 5] * (step >= 5 ? 2f : 1f);
                return (Sine(f, t) + 0.4f * Sine(f * 2.01f, t)) * Decay(t % 0.07f, 10f) * Decay(t, 2.5f) * 0.35f;
            });
            _clips[(int)SugarSound.SpecialCreated] = Create("Special", 0.45f, t =>
                (Sine(1318.5f, t) + Sine(1975.5f, t) * (t > 0.08f ? 1f : 0f)) * Decay(t, 8f) * 0.22f);
            _clips[(int)SugarSound.Ice] = Create("Ice", 0.3f, t =>
                (Noise() * Decay(t, 30f) * 0.35f + Sine(3100f, t) * Decay(t, 20f) * 0.15f + Sine(4200f, t) * Decay(t, 26f) * 0.1f));
            _clips[(int)SugarSound.Biscuit] = Create("Biscuit", 0.22f, t =>
            {
                // Crănțănit: rafale scurte de zgomot.
                float bursts = Mathf.Repeat(t, 0.045f) < 0.012f ? 1f : 0.2f;
                return Noise() * bursts * Decay(t, 12f) * 0.5f + Sine(110f, t) * Decay(t, 30f) * 0.3f;
            });
            _clips[(int)SugarSound.Candy] = Create("Candy", 0.06f, t => Sine(2400f, t) * Decay(t, 70f) * 0.25f);
            _clips[(int)SugarSound.Win] = Create("Win", 1.3f, t =>
            {
                float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f, 783.99f, 1046.5f };
                float[] starts = { 0f, 0.12f, 0.24f, 0.36f, 0.6f, 0.72f };
                float sum = 0f;
                for (int i = 0; i < notes.Length; i++)
                {
                    float local = t - starts[i];
                    if (local < 0f) continue;
                    sum += (Sine(notes[i], local) + 0.3f * Sine(notes[i] * 2f, local)) * Decay(local, i == notes.Length - 1 ? 3f : 9f);
                }
                return sum * 0.25f;
            });
            _clips[(int)SugarSound.Lose] = Create("Lose", 1.2f, t =>
            {
                // „Uaa-uaa-uaaa” în jos, ca o trompetă tristă.
                int step = Mathf.Min(2, (int)(t / 0.3f));
                float[] notes = { 392f, 370f, 349f };
                float local = t - step * 0.3f;
                float vibrato = step == 2 ? 1f + 0.015f * Mathf.Sin(local * 35f) : 1f;
                float f = notes[step] * vibrato;
                float env = step == 2 ? Decay(local, 2.2f) : Decay(local, 6f);
                return (Mathf.Sign(Sine(f, t)) * 0.25f + Sine(f, t) * 0.5f) * env * 0.3f;
            });
            _clips[(int)SugarSound.Click] = Create("Click", 0.07f, t => Sweep(600f, 1000f, 0.07f, t) * Decay(t, 45f) * 0.35f);
            _clips[(int)SugarSound.Coin] = ProceduralSfx.Coin;
            _clips[(int)SugarSound.Shuffle] = Create("Shuffle", 0.5f, t => (Noise() * 0.15f + Sweep(300f, 1200f, 0.5f, t) * 0.25f) * Mathf.Sin(t / 0.5f * Mathf.PI));
            _clips[(int)SugarSound.Star] = Create("Star", 0.5f, t => (Sine(1567.98f, t) + 0.5f * Sine(2349.3f, t)) * Decay(t, 7f) * 0.3f);
            _clips[(int)SugarSound.Bomb] = Create("Bomb", 0.8f, t => (Noise() * 0.6f + Sweep(120f, 35f, 0.8f, t) * 0.8f) * Decay(t, 4.5f) * 0.6f);
        }

        private static float Sine(float frequency, float t) => Mathf.Sin(2f * Mathf.PI * frequency * t);
        private static float Decay(float t, float speed) => Mathf.Exp(-t * speed);
        private float Noise() => (float)(_random.NextDouble() * 2.0 - 1.0);

        /// <summary>Glissando liniar de la „from” la „to” Hz pe durata dată (fază integrată, fără pocnituri).</summary>
        private static float Sweep(float from, float to, float duration, float t)
        {
            float k = (to - from) / duration;
            return Mathf.Sin(2f * Mathf.PI * (from * t + 0.5f * k * t * t));
        }

        private static AudioClip Create(string name, float duration, Func<float, float> generator)
        {
            int length = Mathf.Max(1, (int)(SampleRate * duration));
            var data = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)SampleRate;
                float fadeIn = Mathf.Clamp01(t / 0.002f);
                float fadeOut = Mathf.Clamp01((duration - t) / 0.01f);
                data[i] = Mathf.Clamp(generator(t) * fadeIn * fadeOut, -1f, 1f);
            }
            AudioClip clip = AudioClip.Create(name, length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
