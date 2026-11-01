using System.Collections.Generic;
using UnityEngine;

namespace VoxelBloom
{
    /// <summary>Plays pooled one-shot SFX and the music loop, honouring the persisted settings.</summary>
    public sealed class AudioService : MonoBehaviour
    {
        readonly Dictionary<Sfx, AudioClip> clips = new Dictionary<Sfx, AudioClip>();
        readonly Dictionary<Sfx, float> lastPlayed = new Dictionary<Sfx, float>();
        AudioSource music;
        AudioSource[] voices;
        int nextVoice;

        void Awake()
        {
            foreach (Sfx s in System.Enum.GetValues(typeof(Sfx))) clips[s] = SfxSynth.Make(s);
            music = gameObject.AddComponent<AudioSource>();
            music.clip = SfxSynth.Music();
            music.loop = true;
            music.playOnAwake = false;
            voices = new AudioSource[8];
            for (int i = 0; i < voices.Length; i++)
            {
                voices[i] = gameObject.AddComponent<AudioSource>();
                voices[i].playOnAwake = false;
            }
            Services.SettingsChanged += Apply;
            Apply();
        }

        void OnDestroy() { Services.SettingsChanged -= Apply; }

        void Apply()
        {
            var st = Services.Save.Settings;
            music.volume = st.MusicVolume * 0.55f;
            if (st.Music && !music.isPlaying) music.Play();
            else if (!st.Music && music.isPlaying) music.Stop();
        }

        public void Play(Sfx s, float pitch = 1f, float volume = 1f)
        {
            var st = Services.Save.Settings;
            if (!st.Sound || !clips.TryGetValue(s, out var clip) || clip == null) return;
            // throttle identical sounds (e.g. many pops in one frame)
            if (lastPlayed.TryGetValue(s, out float t) && Time.unscaledTime - t < 0.03f) return;
            lastPlayed[s] = Time.unscaledTime;
            var v = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Length;
            v.pitch = pitch;
            v.volume = st.SoundVolume * volume;
            v.clip = clip;
            v.Play();
        }

        public void PauseMusic(bool paused)
        {
            if (paused) music.Pause();
            else if (Services.Save.Settings.Music) music.UnPause();
        }
    }

    public static class Haptics
    {
        public static void Light()
        {
#if UNITY_ANDROID || UNITY_IOS
            if (Services.Initialized && Services.Save.Settings.Vibration && !Services.ReducedEffects) Handheld.Vibrate();
#endif
        }
    }
}
