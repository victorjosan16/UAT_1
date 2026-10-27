using System;
using QuarryEmpire;
using UnityEngine;
using UnityEngine.UI;

namespace SugarDrop
{
    /// <summary>
    /// Intro-ul de la pornire: imaginea Drop Quest (Resources/Art/intro.png) apare cu o săritură peste fundalul ei
    /// estompat (introbg.png), plutește ușor printre sclipiri, apoi dispare singură după câteva secunde sau la
    /// atingere. Fără imagine, intro-ul se sare.
    /// </summary>
    public class IntroPanel : MonoBehaviour
    {
        private const float PopDuration = 0.65f;
        private const float AutoClose = 2.8f;
        private const float MinShow = 0.8f;
        private const float FadeDuration = 0.35f;

        private GameObject _root;
        private CanvasGroup _group;
        private RectTransform _card;
        private RectTransform _shine;
        private UIParticleController _sparkles;
        private Action _onDone;
        private float _time = -1f;
        private float _closing = -1f;

        public bool IsOpen => _root != null && _root.activeSelf;

        private void Awake()
        {
            Sprite art = ArtLibrary.Find("intro");
            if (art == null) return;

            Sprite back = ArtLibrary.Find("introbg");
            Image background = UiFactory.CreateImage("Background", transform, back != null ? back : SugarSprites.BackgroundGradient, Color.white);
            background.preserveAspect = false;
            background.raycastTarget = true;
            UiFactory.Stretch(background.rectTransform, -300f);
            _root = background.gameObject;
            _group = _root.AddComponent<CanvasGroup>();
            var tap = _root.AddComponent<Button>();
            tap.transition = Selectable.Transition.None;
            tap.onClick.AddListener(() =>
            {
                if (_time >= MinShow) Close();
            });

            RectTransform sparkleLayer = UiFactory.CreateRect("Sparkles", _root.transform);
            UiFactory.Stretch(sparkleLayer, 300f);
            _sparkles = sparkleLayer.gameObject.AddComponent<UIParticleController>();
            _sparkles.Initialize(50, SugarSprites.Sparkle, new[] { Color.white, new Color(1f, 0.92f, 0.55f), new Color(0.7f, 0.9f, 1f) }, 6f);
            _sparkles.SetAmbientSize(18f, 44f);

            Image glow = UiFactory.CreateImage("Glow", _root.transform, SugarSprites.SoftDot, new Color(1f, 1f, 0.9f, 0.55f));
            glow.preserveAspect = false;
            UiFactory.Place(glow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(1300f, 1300f));

            Image card = UiFactory.CreateImage("Card", _root.transform, art, Color.white);
            card.preserveAspect = true;
            card.raycastTarget = false;
            _card = UiFactory.Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(940f, 940f));

            // O dungă de lumină care trece o dată peste imagine (decupată la marginile cardului).
            RectTransform mask = UiFactory.CreateRect("ShineMask", _card);
            UiFactory.Stretch(mask, 40f);
            mask.gameObject.AddComponent<RectMask2D>();
            Image shine = UiFactory.CreateImage("Shine", mask, UiFactory.WhiteSprite, new Color(1f, 1f, 1f, 0.32f));
            shine.preserveAspect = false;
            shine.raycastTarget = false;
            _shine = UiFactory.Place(shine.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-900f, 0f), new Vector2(120f, 1600f));
            _shine.localRotation = Quaternion.Euler(0f, 0f, -24f);

            _root.SetActive(false);
        }

        /// <summary>Arată intro-ul; onDone se cheamă după ce dispare (imediat, dacă nu există imaginea).</summary>
        public void Show(Action onDone)
        {
            if (_root == null)
            {
                onDone?.Invoke();
                return;
            }
            _onDone = onDone;
            _time = 0f;
            _closing = -1f;
            _group.alpha = 1f;
            _root.transform.SetAsLastSibling();
            _root.SetActive(true);
            Step();
        }

        /// <summary>Butonul Înapoi al telefonului sau atingerea: intro-ul se închide.</summary>
        public void Close()
        {
            if (_closing < 0f && IsOpen) _closing = 0f;
        }

        private void Update()
        {
            if (!IsOpen) return;
            float dt = Time.unscaledDeltaTime;
            _time += dt;
            if (_time >= AutoClose) Close();
            if (_closing >= 0f)
            {
                _closing += dt;
                _group.alpha = 1f - Mathf.Clamp01(_closing / FadeDuration);
                if (_closing >= FadeDuration)
                {
                    _root.SetActive(false);
                    Action done = _onDone;
                    _onDone = null;
                    done?.Invoke();
                    return;
                }
            }
            Step();
        }

        private void Step()
        {
            float pop = Mathf.Clamp01(_time / PopDuration);
            float scale = Mathf.LerpUnclamped(0.3f, 1f, UiEase.OutBack(pop, 1.8f));
            float breathe = 1f + Mathf.Sin(_time * 2.2f) * 0.015f;
            _card.localScale = Vector3.one * scale * breathe;
            _card.anchoredPosition = new Vector2(0f, 40f + Mathf.Sin(_time * 1.6f) * 10f);
            _card.localRotation = Quaternion.Euler(0f, 0f, (1f - pop) * -8f);
            float sweep = Mathf.Clamp01((_time - 0.7f) / 0.9f);
            _shine.anchoredPosition = new Vector2(Mathf.Lerp(-900f, 900f, sweep), 0f);
        }
    }
}
