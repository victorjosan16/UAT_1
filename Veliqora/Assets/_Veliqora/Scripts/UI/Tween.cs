using System;
using System.Collections;
using UnityEngine;

namespace Veliqora.UI
{
    /// <summary>Global motion preferences, set from Settings (reduced motion + animation speed).</summary>
    public static class MotionPrefs
    {
        public static bool Reduced;
        public static float Speed = 1f;

        /// <summary>Effective duration. Decorative motion collapses to 0 in reduced-motion mode.</summary>
        public static float Dur(float seconds, bool decorative = false)
        {
            if (Reduced) return decorative ? 0f : Mathf.Min(seconds, 0.12f);
            return seconds / Mathf.Max(0.25f, Speed);
        }
    }

    public static class Ease
    {
        public static float Linear(float t) => t;
        public static float OutCubic(float t) { t = 1f - t; return 1f - t * t * t; }
        public static float InOutCubic(float t) => t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
        public static float InCubic(float t) => t * t * t;
        public static float OutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
        public static float OutElastic(float t)
        {
            if (t <= 0f || t >= 1f) return t;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * (2f * Mathf.PI / 3f)) + 1f;
        }
    }

    /// <summary>
    /// Minimal coroutine tweens. Coroutines stop automatically when the host is destroyed (scene unload),
    /// so callbacks never touch dead objects. Uses unscaled time so pause overlays don't freeze UI motion.
    /// </summary>
    public static class Tween
    {
        public static Coroutine Run(MonoBehaviour host, float duration, Action<float> step, Action done = null, Func<float, float> ease = null)
        {
            if (host == null || !host.isActiveAndEnabled)
            {
                step?.Invoke(1f);
                done?.Invoke();
                return null;
            }
            return host.StartCoroutine(Routine(duration, step, done, ease ?? Ease.OutCubic));
        }

        private static IEnumerator Routine(float duration, Action<float> step, Action done, Func<float, float> ease)
        {
            if (duration <= 0f)
            {
                step?.Invoke(1f);
                done?.Invoke();
                yield break;
            }
            float t = 0f;
            while (t < duration)
            {
                step?.Invoke(ease(Mathf.Clamp01(t / duration)));
                yield return null;
                t += Time.unscaledDeltaTime;
            }
            step?.Invoke(1f);
            done?.Invoke();
        }

        public static Coroutine Delay(MonoBehaviour host, float seconds, Action action)
        {
            if (host == null || !host.isActiveAndEnabled) { action?.Invoke(); return null; }
            return host.StartCoroutine(DelayRoutine(seconds, action));
        }

        private static IEnumerator DelayRoutine(float s, Action a)
        {
            float t = 0;
            while (t < s) { t += Time.unscaledDeltaTime; yield return null; }
            a?.Invoke();
        }

        public static Coroutine Scale(MonoBehaviour host, Transform target, Vector3 from, Vector3 to, float duration, Func<float, float> ease = null, Action done = null)
        {
            return Run(host, duration, k => { if (target != null) target.localScale = Vector3.LerpUnclamped(from, to, k); }, done, ease);
        }

        public static Coroutine Fade(MonoBehaviour host, CanvasGroup group, float from, float to, float duration, Action done = null)
        {
            return Run(host, duration, k => { if (group != null) group.alpha = Mathf.Lerp(from, to, k); }, done, Ease.Linear);
        }

        public static Coroutine Move(MonoBehaviour host, RectTransform target, Vector2 from, Vector2 to, float duration, Func<float, float> ease = null, Action done = null)
        {
            return Run(host, duration, k => { if (target != null) target.anchoredPosition = Vector2.LerpUnclamped(from, to, k); }, done, ease);
        }

        /// <summary>Horizontal shake used for invalid-move feedback.</summary>
        public static Coroutine Shake(MonoBehaviour host, RectTransform target, float amplitude = 18f, float duration = 0.3f)
        {
            if (target == null) return null;
            var origin = target.anchoredPosition;
            float d = MotionPrefs.Reduced ? 0.12f : duration;
            float amp = MotionPrefs.Reduced ? amplitude * 0.35f : amplitude;
            return Run(host, d, k =>
            {
                if (target == null) return;
                float s = Mathf.Sin(k * Mathf.PI * 6f) * (1f - k) * amp;
                target.anchoredPosition = origin + new Vector2(s, 0);
            }, () => { if (target != null) target.anchoredPosition = origin; }, Ease.Linear);
        }

        /// <summary>Soft looping pulse (scale) until the returned coroutine is stopped.</summary>
        public static Coroutine Pulse(MonoBehaviour host, Transform target, float amount = 0.06f, float period = 1.1f)
        {
            if (host == null || MotionPrefs.Reduced) return null;
            return host.StartCoroutine(PulseRoutine(target, amount, period));
        }

        private static IEnumerator PulseRoutine(Transform target, float amount, float period)
        {
            float t = 0;
            while (target != null)
            {
                t += Time.unscaledDeltaTime;
                float s = 1f + Mathf.Sin(t / period * Mathf.PI * 2f) * amount;
                target.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
        }
    }
}
