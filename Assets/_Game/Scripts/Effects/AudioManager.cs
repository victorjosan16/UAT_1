using System.Collections.Generic;
using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Redă efecte sonore pe câteva surse audio refolosite (fără să creeze obiecte noi).
    /// Respectă setarea „Sunet” din salvare.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField, Range(1, 24)] private int voices = 10;
        [SerializeField, Range(0f, 1f)] private float masterVolume = 0.8f;

        private AudioSource[] _sources;
        private int _next;
        private readonly Dictionary<AudioClip, float> _lastPlayTime = new();

        public bool Muted => SaveSystem.Instance != null && !SaveSystem.Instance.Data.soundOn;

        private void Awake()
        {
            Instance = this;
            _sources = new AudioSource[voices];
            for (int i = 0; i < voices; i++)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f; // sunet 2D
                _sources[i] = source;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <param name="minInterval">Nu repetăm același sunet mai des de atât (evită „zgomotul de mitralieră”).</param>
        public void Play(AudioClip clip, float volume = 1f, float pitchJitter = 0.06f, float minInterval = 0.03f)
        {
            if (clip == null || Muted) return;

            float now = Time.unscaledTime;
            if (_lastPlayTime.TryGetValue(clip, out float last) && now - last < minInterval) return;
            _lastPlayTime[clip] = now;

            AudioSource source = _sources[_next];
            _next = (_next + 1) % _sources.Length;
            source.clip = clip;
            source.volume = volume * masterVolume;
            source.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            source.Play();
        }
    }
}
