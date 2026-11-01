using System;
using System.Collections;
using UnityEngine;

namespace VoxelBloom
{
    public static class Ease
    {
        public static float OutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
        public static float InOutQuad(float t) => t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
        public static float OutBack(float t) { const float c1 = 1.70158f, c3 = c1 + 1f; return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f); }
        public static float OutElastic(float t) => t <= 0 ? 0 : t >= 1 ? 1 : Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * (2f * Mathf.PI / 3f)) + 1f;
    }

    /// <summary>Minimal coroutine tweens on unscaled time (UI keeps animating while gameplay is paused).</summary>
    public sealed class Tween : MonoBehaviour
    {
        static Tween runner;
        static Tween Runner
        {
            get
            {
                if (runner == null)
                {
                    var go = new GameObject("TweenRunner");
                    DontDestroyOnLoad(go);
                    runner = go.AddComponent<Tween>();
                }
                return runner;
            }
        }

        public static Coroutine Value(float duration, Action<float> step, Func<float, float> ease = null, Action done = null, MonoBehaviour host = null)
            => (host != null && host.isActiveAndEnabled ? host : Runner).StartCoroutine(Run(duration, step, ease ?? Ease.OutCubic, done));

        static IEnumerator Run(float duration, Action<float> step, Func<float, float> ease, Action done)
        {
            float t = 0;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                step(ease(Mathf.Clamp01(t / duration)));
                yield return null;
            }
            step(ease(1f));
            done?.Invoke();
        }

        public static Coroutine Scale(Transform tr, Vector3 from, Vector3 to, float duration, Func<float, float> ease = null, Action done = null)
            => Value(duration, k => { if (tr != null) tr.localScale = Vector3.LerpUnclamped(from, to, k); }, ease, done);

        public static Coroutine Pop(Transform tr, float amount = 0.15f, float duration = 0.25f)
            => Value(duration, k => { if (tr != null) tr.localScale = Vector3.one * (1f + Mathf.Sin(k * Mathf.PI) * amount); }, t => t);

        public static Coroutine Fade(CanvasGroup cg, float from, float to, float duration, Action done = null)
            => Value(duration, k => { if (cg != null) cg.alpha = Mathf.LerpUnclamped(from, to, k); }, Ease.OutCubic, done);

        public static Coroutine Move(RectTransform rt, Vector2 from, Vector2 to, float duration, Func<float, float> ease = null, Action done = null)
            => Value(duration, k => { if (rt != null) rt.anchoredPosition = Vector2.LerpUnclamped(from, to, k); }, ease, done);

        public static Coroutine Delay(float seconds, Action a) => Runner.StartCoroutine(DelayRun(seconds, a));

        static IEnumerator DelayRun(float s, Action a)
        {
            yield return new WaitForSecondsRealtime(s);
            a?.Invoke();
        }

        public static void StopAllOn(MonoBehaviour host) { if (host != null) host.StopAllCoroutines(); }
    }
}
