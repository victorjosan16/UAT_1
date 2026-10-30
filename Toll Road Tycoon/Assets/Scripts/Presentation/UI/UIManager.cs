using System;
using System.Collections.Generic;
using TollRoadTycoon.Economy;
using TollRoadTycoon.Localization;
using TollRoadTycoon.RoadExpansion;
using TollRoadTycoon.TollSystem;
using UnityEngine;
using UnityEngine.UI;

namespace TollRoadTycoon.Presentation.UI
{
    /// <summary>Owns the canvas, screens, modal dialogs and toasts.</summary>
    public sealed class UIManager : MonoBehaviour
    {
        private GameRoot _root;
        private CanvasScaler _scaler;
        private RectTransform _safe;
        private RectTransform _menuLayer;
        private RectTransform _gameLayer;
        private RectTransform _modalLayer;
        private RectTransform _toastLayer;
        private MainMenuScreen _menu;
        private GameplayScreen _gameplay;
        private readonly List<Action> _modalUpdaters = new List<Action>();
        private Action _modalRebuild;
        private Action _modalClosed;
        private float _refreshTimer;

        public GameRoot Root => _root;
        public bool ModalOpen => _modalLayer.childCount > 0;

        public void Initialize(GameRoot root)
        {
            _root = root;
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            _scaler = gameObject.AddComponent<CanvasScaler>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.referenceResolution = new Vector2(1080, 1920);
            _scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            gameObject.AddComponent<GraphicRaycaster>();

            _safe = UIFactory.Stretch(UIFactory.Rect(transform, "SafeArea"));
            _safe.gameObject.AddComponent<SafeAreaFitter>();
            _menuLayer = UIFactory.Stretch(UIFactory.Rect(_safe, "MenuLayer"));
            _gameLayer = UIFactory.Stretch(UIFactory.Rect(_safe, "GameLayer"));
            _modalLayer = UIFactory.Stretch(UIFactory.Rect(transform, "ModalLayer"));
            _toastLayer = UIFactory.Stretch(UIFactory.Rect(_safe, "ToastLayer"));

            _menu = _menuLayer.gameObject.AddComponent<MainMenuScreen>();
            _gameplay = _gameLayer.gameObject.AddComponent<GameplayScreen>();
            _menu.Build(this);
            _gameplay.Build(this);

            Loc.LanguageChanged += OnLanguageChanged;
            root.SimulationReplaced += () => _gameplay.Rebuild();
        }

        private void OnDestroy() => Loc.LanguageChanged -= OnLanguageChanged;

        private void OnLanguageChanged()
        {
            _menu.Build(this);
            _gameplay.Build(this);
            _modalRebuild?.Invoke();
        }

        private void Update()
        {
            // Wide screens (tablets) match height, tall phones match width.
            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            _scaler.matchWidthOrHeight = aspect > 0.6f ? 1f : 0f;

            _refreshTimer -= Time.unscaledDeltaTime;
            if (_refreshTimer > 0) return;
            _refreshTimer = 0.2f;
            foreach (var u in _modalUpdaters) u();
        }

        public void ShowMainMenu()
        {
            CloseModal();
            _menuLayer.gameObject.SetActive(true);
            _gameLayer.gameObject.SetActive(false);
        }

        public void ShowGameplay()
        {
            CloseModal();
            _menuLayer.gameObject.SetActive(false);
            _gameLayer.gameObject.SetActive(true);
            _gameplay.Rebuild();
        }

        // ---- Purchases --------------------------------------------------------------------

        /// <summary>Runs a purchase with feedback. Returns true when it succeeded.</summary>
        public bool TryPurchase(long cost, Func<bool> buy)
        {
            if (!_root.Sim.Economy.CanAfford(cost))
            {
                _root.Audio.Play(Sfx.Error);
                Toast(Loc.T("toast.no_money"));
                return false;
            }
            if (!buy())
            {
                _root.Audio.Play(Sfx.Error);
                return false;
            }
            _root.Audio.Play(Sfx.Purchase);
            Vibrate();
            _gameplay.Rebuild();
            return true;
        }

        public void Vibrate()
        {
#if UNITY_ANDROID || UNITY_IOS
            if (_root.Settings.VibrationEnabled) Handheld.Vibrate();
#endif
        }

        public void ConfirmBuildBooth()
        {
            var sim = _root.Sim;
            var block = sim.Expansion.CanBuyBooth();
            if (block == ExpansionBlock.MaxReached)
            {
                Toast(Loc.T("booth.max"));
                return;
            }
            if (block == ExpansionBlock.NeedsMoreLanes)
            {
                Toast(Loc.F("booth.needs_lanes", sim.Expansion.LanesRequiredForNextBooth));
                return;
            }
            int number = sim.Booths.Count + 1;
            long cost = sim.Expansion.NextBoothCost;
            Confirm(Loc.F("booth.build", number), Loc.T("booth.build.body"), cost, () =>
            {
                TollBooth built = null;
                if (TryPurchase(cost, () => (built = sim.Expansion.TryBuyBooth()) != null))
                {
                    _root.SelectBooth(built);
                    if (built.AssignedWorker == null) Toast(Loc.T("toast.needs_worker"));
                    else Toast(Loc.T("toast.bought"));
                }
            });
        }

        public void ConfirmBuyLane()
        {
            var sim = _root.Sim;
            if (sim.Expansion.CanBuyLane() == ExpansionBlock.MaxReached)
            {
                Toast(Loc.T("lane.max"));
                return;
            }
            long cost = sim.Expansion.NextLaneCost;
            Confirm(Loc.F("lane.buy", sim.Expansion.LaneCount + 1), Loc.T("lane.buy.body"), cost, () =>
            {
                if (TryPurchase(cost, () => sim.Expansion.TryBuyLane())) Toast(Loc.T("toast.bought"));
            });
        }

        // ---- Modals -----------------------------------------------------------------------

        /// <summary>Opens a modal dialog and returns its body (a vertical list).</summary>
        public RectTransform OpenModal(string title, float height, Action rebuildOnLanguageChange = null, Action onClosed = null)
        {
            CloseModal();
            _modalRebuild = rebuildOnLanguageChange;
            _modalClosed = onClosed;
            var dim = UIFactory.Panel(_modalLayer, "Dim", new Color(0.02f, 0.1f, 0.2f, 0.55f), SpriteFactory.Square);
            UIFactory.Stretch(dim.rectTransform);
            var dimButton = dim.gameObject.AddComponent<Button>();
            dimButton.transition = Selectable.Transition.None;
            dimButton.onClick.AddListener(CloseModal);

            var frame = UIFactory.Panel(dim.transform, "Dialog", Palette.DarkBlue);
            UIFactory.Place(frame.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(980, height));
            frame.gameObject.AddComponent<Button>().transition = Selectable.Transition.None; // swallow clicks
            UIFactory.AddDropShadow(frame, 12);

            var header = UIFactory.Label(frame.transform, title, 50, Color.white);
            UIFactory.AddOutline(header, new Color(0, 0, 0, 0.4f), 2);
            UIFactory.Band(header.rectTransform, true, 14, 90, 110);

            var close = UIFactory.Button(frame.transform, "X", Palette.Red, CloseModal, 44);
            UIFactory.Place((RectTransform)close.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-18, -18), new Vector2(90, 90));

            var body = UIFactory.Panel(frame.transform, "Body", Palette.PanelLight);
            UIFactory.Stretch(body.rectTransform, 18, 18, 118, 18);
            var content = UIFactory.Stretch(UIFactory.Rect(body.transform, "Content"), 0, 0, 0, 0);
            UIFactory.VerticalList(content, 16, 24);
            return content;
        }

        public void AddModalUpdater(Action updater)
        {
            _modalUpdaters.Add(updater);
            updater();
        }

        public void CloseModal()
        {
            var closed = _modalClosed;
            _modalClosed = null;
            _modalUpdaters.Clear();
            _modalRebuild = null;
            UIFactory.Clear(_modalLayer);
            closed?.Invoke();
        }

        public void Confirm(string title, string body, long cost, Action onConfirm)
        {
            var content = OpenModal(Loc.T("confirm.title"), 760);
            var heading = UIFactory.Label(content, title, 46, Palette.DarkBlue);
            UIFactory.Layout(heading, 80);
            var text = UIFactory.Label(content, body, 36, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Normal);
            UIFactory.Layout(text, 170);
            var price = UIFactory.Label(content, Money.Format(cost), 60, Palette.Gold);
            UIFactory.AddOutline(price, Palette.DarkBlue, 3);
            UIFactory.Layout(price, 90);
            var row = UIFactory.Rect(content, "Buttons");
            UIFactory.HorizontalList(row, 24, 0);
            UIFactory.Layout(row, 120);
            UIFactory.Button(row, Loc.T("confirm.cancel"), Palette.Disabled, CloseModal, 40);
            var buy = UIFactory.Button(row, Loc.T("confirm.buy"), Palette.Green, () =>
            {
                CloseModal();
                onConfirm();
            }, 44);
            AddModalUpdater(() => buy.interactable = _root.Sim.Economy.CanAfford(cost));
        }

        public void ShowInfo(string title, string body)
        {
            var content = OpenModal(title, 620);
            var text = UIFactory.Label(content, body, 38, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Normal);
            UIFactory.Layout(text, 300);
            var ok = UIFactory.Button(content, Loc.T("common.ok"), Palette.Green, CloseModal, 44);
            UIFactory.Layout(ok, 120);
        }

        public void ShowBoothUpgrade(TollBooth booth)
        {
            var content = OpenModal(Loc.T("upgrade.title"), 1300, () => ShowBoothUpgrade(booth));
            Panels.BuildBoothUpgrade(this, content, booth, AddModalUpdater, 140);
        }

        public void ShowPause()
        {
            var content = OpenModal(Loc.T("pause.title"), 760, ShowPause, Unpause);
            _root.Paused = true;
            var resume = UIFactory.Button(content, Loc.T("pause.resume"), Palette.Green, CloseModal, 46);
            UIFactory.Layout(resume, 130);
            var settings = UIFactory.Button(content, Loc.T("menu.settings"), Palette.Blue, ShowSettings, 46);
            UIFactory.Layout(settings, 130);
            var menu = UIFactory.Button(content, Loc.T("pause.menu"), Palette.DarkBlue, () =>
            {
                CloseModal();
                _root.ExitToMenu();
                Toast(Loc.T("toast.saved"));
            }, 40);
            UIFactory.Layout(menu, 130);
        }

        public void ShowSettings()
        {
            bool fromPause = _root.Paused;
            var content = OpenModal(Loc.T("settings.title"), 1350, ShowSettings, fromPause ? (Action)Unpause : null);
            if (fromPause) _root.Paused = true;
            Panels.BuildSettings(this, content, AddModalUpdater);
        }

        private void Unpause() => _root.Paused = false;

        // ---- Toasts -----------------------------------------------------------------------

        public void Toast(string message)
        {
            var panel = UIFactory.Panel(_toastLayer, "Toast", new Color(0.02f, 0.18f, 0.32f, 0.92f), SpriteFactory.Pill);
            UIFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 260), new Vector2(900, 110));
            panel.raycastTarget = false;
            var text = UIFactory.Label(panel.transform, message, 36, Color.white);
            UIFactory.Stretch(text.rectTransform, 30, 30, 6, 6);
            var toast = panel.gameObject.AddComponent<ToastFade>();
            toast.Group = panel.gameObject.AddComponent<CanvasGroup>();
            // Push older toasts up so they never overlap.
            for (int i = 0; i < _toastLayer.childCount - 1; i++)
            {
                var rt = (RectTransform)_toastLayer.GetChild(i);
                rt.anchoredPosition += new Vector2(0, 125);
            }
        }
    }

    public sealed class ToastFade : MonoBehaviour
    {
        public CanvasGroup Group;
        private float _age;

        private void Update()
        {
            _age += Time.unscaledDeltaTime;
            Group.alpha = 1f - Mathf.Clamp01((_age - 2.0f) / 0.4f);
            if (_age > 2.4f) Destroy(gameObject);
        }
    }
}
