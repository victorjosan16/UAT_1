using UndergroundMetroTycoon.Simulation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UndergroundMetroTycoon.Game
{
    /// <summary>Builds the portrait UGUI canvas: safe area, joystick zone, HUD, modal panels and feedback layer.</summary>
    public sealed class GameUI : MonoBehaviour
    {
        public const float ReferenceWidth = 1080f;
        public const float ReferenceHeight = 1920f;

        public HudView Hud { get; private set; }
        public ContractsPanel Contracts { get; private set; }
        public SettingsPanel Settings { get; private set; }
        public FeedbackLayer Feedback { get; private set; }
        public VirtualJoystick Joystick { get; private set; }

        public static GameUI Build(Transform parent, GameSession session, IGameUIActions actions, SfxPlayer sfx)
        {
            var go = new GameObject("GameUI");
            go.transform.SetParent(parent, false);
            go.layer = 5;
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            // Portrait: match width so the HUD keeps its horizontal layout on tall phones.
            scaler.matchWidthOrHeight = 0f;
            scaler.referencePixelsPerUnit = 100f;
            go.AddComponent<GraphicRaycaster>();

            EnsureEventSystem(parent);

            GameUI ui = go.AddComponent<GameUI>();
            ui.Construct((RectTransform)go.transform, session, actions, sfx);
            return ui;
        }

        private void Construct(RectTransform canvasRoot, GameSession session, IGameUIActions actions, SfxPlayer sfx)
        {
            RectTransform safe = UIFactory.Rect("SafeArea", canvasRoot);
            UIFactory.Stretch(safe);
            safe.gameObject.AddComponent<SafeAreaFitter>();

            BuildJoystick(safe);

            RectTransform hudRoot = UIFactory.Stretch(UIFactory.Rect("HUD", safe));
            Hud = hudRoot.gameObject.AddComponent<HudView>();
            Hud.Build(hudRoot, session, actions);

            RectTransform feedbackRoot = UIFactory.Stretch(UIFactory.Rect("Feedback", safe));
            Feedback = feedbackRoot.gameObject.AddComponent<FeedbackLayer>();
            Feedback.Build(feedbackRoot, Hud.CoinIconTarget, () =>
            {
                Hud.PunchCoins();
                sfx.PlayCoin();
            });

            RectTransform contractsRoot = UIFactory.Rect("ContractsPanel", safe);
            Contracts = contractsRoot.gameObject.AddComponent<ContractsPanel>();
            Contracts.Build(contractsRoot, session, actions, sfx);

            RectTransform settingsRoot = UIFactory.Rect("SettingsPanel", safe);
            Settings = settingsRoot.gameObject.AddComponent<SettingsPanel>();
            Settings.Build(settingsRoot, session.Settings, actions, sfx);
        }

        private void BuildJoystick(RectTransform safe)
        {
            // Invisible touch zone over the lower part of the screen; HUD buttons sit above it in the hierarchy.
            Image zone = UIFactory.Image("JoystickZone", safe, new Color(0f, 0f, 0f, 0f));
            zone.raycastTarget = true;
            RectTransform zoneRect = zone.rectTransform;
            zoneRect.anchorMin = Vector2.zero;
            zoneRect.anchorMax = new Vector2(1f, 0.58f);
            zoneRect.offsetMin = Vector2.zero;
            zoneRect.offsetMax = Vector2.zero;

            Image ring = UIFactory.Image("Base", zoneRect, new Color(1f, 1f, 1f, 0.22f), UISprites.Circle);
            UIFactory.Place(ring.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-250f, -230f), new Vector2(300f, 300f));
            Image inner = UIFactory.Image("Inner", ring.transform, new Color(0.03f, 0.07f, 0.13f, 0.35f), UISprites.Circle);
            UIFactory.Stretch(inner.rectTransform, 14f, 14f, 14f, 14f);
            Image knob = UIFactory.Image("Knob", ring.transform, new Color(1f, 1f, 1f, 0.9f), UISprites.Circle);
            UIFactory.Place(knob.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(130f, 130f));
            UIFactory.AddShadow(knob, 5f, 0.35f);

            Joystick = zone.gameObject.AddComponent<VirtualJoystick>();
            Joystick.Init(zoneRect, ring.rectTransform, knob.rectTransform, 120f);
        }

        private static void EnsureEventSystem(Transform parent)
        {
            if (EventSystem.current != null || Object.FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.transform.SetParent(parent, false);
            go.AddComponent<EventSystem>();
#if ENABLE_LEGACY_INPUT_MANAGER
            go.AddComponent<StandaloneInputModule>();
#else
            Debug.LogError("[UMT] Touch input needs 'Active Input Handling' set to 'Input Manager (Old)' or 'Both' " +
                           "(Project Settings > Player > Other Settings). Run 'Underground Metro Tycoon > Setup Project'.");
#endif
        }
    }
}
