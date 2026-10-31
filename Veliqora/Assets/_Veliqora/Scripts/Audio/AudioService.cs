using UnityEngine;
using UnityEngine.Audio;
using Veliqora.Core.Save;
using Veliqora.UI;

namespace Veliqora.Audio
{
    /// <summary>
    /// Four routed channels (Music, UI, Pour, Reward). If an AudioMixer exists at
    /// Resources/Veliqora/Audio/VeliqoraMixer with groups named Music/UI/Pour/Reward, each source is routed to
    /// its group; otherwise sources play directly. Volumes and mute toggles come from SettingsData.
    /// </summary>
    public sealed class AudioService : MonoBehaviour
    {
        private AudioSource _music, _ui, _pour, _reward;
        private AudioClip _tap, _select, _invalid, _pourClip, _complete, _win, _coin, _musicClip;
        private SettingsData _settings;

        public void Init(SettingsData settings)
        {
            _settings = settings;
            var mixer = Resources.Load<AudioMixer>("Veliqora/Audio/VeliqoraMixer");
            _music = Source("Music", mixer, true);
            _ui = Source("UI", mixer, false);
            _pour = Source("Pour", mixer, false);
            _reward = Source("Reward", mixer, false);

            _tap = ProceduralAudio.Load("tap", ProceduralAudio.Tap);
            _select = ProceduralAudio.Load("select", ProceduralAudio.Select);
            _invalid = ProceduralAudio.Load("invalid", ProceduralAudio.Invalid);
            _pourClip = ProceduralAudio.Load("pour", ProceduralAudio.Pour);
            _complete = ProceduralAudio.Load("complete", ProceduralAudio.Complete);
            _win = ProceduralAudio.Load("win", ProceduralAudio.Win);
            _coin = ProceduralAudio.Load("coin", ProceduralAudio.Coin);
            _musicClip = ProceduralAudio.Load("music", ProceduralAudio.Music);
            _music.clip = _musicClip;

            UIFeedback.ButtonPressed += PlayTap;
            ApplySettings();
        }

        private void OnDestroy() { UIFeedback.ButtonPressed -= PlayTap; }

        private AudioSource Source(string group, AudioMixer mixer, bool loop)
        {
            var go = new GameObject("Audio_" + group);
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = loop;
            src.spatialBlend = 0f;
            if (mixer != null)
            {
                var groups = mixer.FindMatchingGroups(group);
                if (groups != null && groups.Length > 0) src.outputAudioMixerGroup = groups[0];
            }
            return src;
        }

        public void ApplySettings()
        {
            if (_settings == null) return;
            float sfx = _settings.Sound ? _settings.SoundVolume : 0f;
            _ui.volume = sfx;
            _pour.volume = sfx;
            _reward.volume = sfx;
            _music.volume = _settings.Music ? _settings.MusicVolume * 0.6f : 0f;
            if (_settings.Music && !_music.isPlaying) _music.Play();
            if (!_settings.Music && _music.isPlaying) _music.Pause();
        }

        public void PauseAll(bool paused)
        {
            if (paused) _music.Pause();
            else if (_settings != null && _settings.Music) _music.UnPause();
        }

        private void One(AudioSource s, AudioClip c, float pitch = 1f)
        {
            if (_settings == null || !_settings.Sound || c == null) return;
            s.pitch = pitch;
            s.PlayOneShot(c);
        }

        public void PlayTap() => One(_ui, _tap);
        public void PlaySelect() => One(_ui, _select, Random.Range(0.97f, 1.04f));
        public void PlayInvalid() => One(_ui, _invalid);
        public void PlayPour(int units) => One(_pour, _pourClip, Mathf.Lerp(1.08f, 0.9f, (units - 1) / 3f));
        public void PlayComplete() => One(_reward, _complete);
        public void PlayWin() => One(_reward, _win);
        public void PlayCoin() => One(_reward, _coin);
    }
}
