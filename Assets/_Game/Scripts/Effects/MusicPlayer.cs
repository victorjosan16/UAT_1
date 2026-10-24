using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Muzică de fundal generată din cod: o buclă liniștită (acorduri moi + arpegiu) care nu obosește.
    /// Respectă setarea „Muzică” și se aude mai încet decât efectele. Poate fi înlocuită cu un fișier propriu (Music Clip).
    /// </summary>
    public class MusicPlayer : MonoBehaviour
    {
        [Tooltip("Opțional: piesa ta (.ogg/.mp3). Gol = muzica generată din cod.")]
        [SerializeField] private AudioClip musicClip;
        [SerializeField, Range(0f, 1f)] private float volume = 0.28f;
        [SerializeField] private float fadeSpeed = 0.6f;

        private AudioSource _source;

        private bool Enabled => SaveSystem.Instance == null || SaveSystem.Instance.Data.musicOn;

        private void Start()
        {
            _source = gameObject.AddComponent<AudioSource>();
            _source.clip = musicClip != null ? musicClip : ProceduralMusic.CreateLoop();
            _source.loop = true;
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
            _source.volume = 0f;
            _source.ignoreListenerPause = true;
            _source.Play();
        }

        private void Update()
        {
            if (_source == null) return;
            float target = Enabled ? volume : 0f;
            _source.volume = Mathf.MoveTowards(_source.volume, target, fadeSpeed * Time.unscaledDeltaTime);
        }
    }

    /// <summary>Generează bucla muzicală: Do – Sol – La minor – Fa, 4 măsuri, ~9 secunde.</summary>
    public static class ProceduralMusic
    {
        private const int SampleRate = 22050; // suficient pentru muzică de fundal, la jumătate din memorie

        public static AudioClip CreateLoop()
        {
            // Acorduri (frecvențe în Hz) pentru fiecare măsură.
            float[][] chords =
            {
                new[] { 261.63f, 329.63f, 392.00f }, // Do major
                new[] { 196.00f, 246.94f, 293.66f }, // Sol major
                new[] { 220.00f, 261.63f, 329.63f }, // La minor
                new[] { 174.61f, 220.00f, 261.63f }, // Fa major
            };
            const float bpm = 104f;
            float beat = 60f / bpm;
            float bar = beat * 4f;
            int length = Mathf.RoundToInt(bar * chords.Length * SampleRate);
            var data = new float[length];

            for (int i = 0; i < length; i++)
            {
                float t = i / (float)SampleRate;
                int barIndex = Mathf.Min(chords.Length - 1, (int)(t / bar));
                float inBar = t - barIndex * bar;
                float[] chord = chords[barIndex];

                // Pad: acordul, cu atac și stingere lină în fiecare măsură.
                float padEnvelope = Mathf.Clamp01(inBar / 0.4f) * Mathf.Clamp01((bar - inBar) / 0.4f);
                float pad = 0f;
                foreach (float f in chord) pad += Mathf.Sin(2f * Mathf.PI * f * 0.5f * t) + 0.3f * Mathf.Sin(2f * Mathf.PI * f * t);
                pad *= 0.06f * padEnvelope;

                // Arpegiu pe optimi: notele acordului, o octavă mai sus, scurte și moi.
                float eighth = beat * 0.5f;
                int step = (int)(t / eighth);
                float inStep = t - step * eighth;
                float note = chord[step % 3] * 2f;
                float pluck = Mathf.Sin(2f * Mathf.PI * note * t) * Mathf.Exp(-inStep * 9f) * 0.09f;

                // Bas pe fiecare timp.
                float inBeat = t - (int)(t / beat) * beat;
                float bass = Mathf.Sin(2f * Mathf.PI * chord[0] * 0.25f * t) * Mathf.Exp(-inBeat * 4f) * 0.12f;

                data[i] = Mathf.Clamp(pad + pluck + bass, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("ProceduralMusic", length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
