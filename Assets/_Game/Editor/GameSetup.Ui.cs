using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace QuarryEmpire.EditorTools
{
    /// <summary>
    /// Canvas-ul principal. Fiecare element de UI e un obiect cu propriul script, care își
    /// construiește singur vizualul la pornire. Ordinea copiilor = ordinea de desenare (ultimul e deasupra).
    /// </summary>
    public static partial class GameSetup
    {
        private static void BuildUi(UiTheme theme)
        {
            var canvasGo = new GameObject("Canvas", typeof(RectTransform)) { layer = UiLayer };
            canvasGo.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0f; // portret: lățimea e fixă (1080), înălțimea se adaptează la telefon
            canvasGo.AddComponent<GraphicRaycaster>();
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            var inputModule = eventSystem.AddComponent<InputSystemUIInputModule>();

            var uiRoot = canvasGo.AddComponent<UiRoot>();
            Set(uiRoot, "theme", theme);
            Set(uiRoot, "inputModule", inputModule);

            Transform canvas = canvasGo.transform;
            UiChild(canvas, "LayerHud", typeof(LayerHud));
            UiChild(canvas, "MoneyHud", typeof(MoneyHud));
            UiChild(canvas, "OrderHud", typeof(OrderHud));
            UiChild(canvas, "RewardedOffers", typeof(RewardedOffersHud));
            UiChild(canvas, "VehicleHud", typeof(VehicleHud));
            UiChild(canvas, "CoinFly", typeof(CoinFlyUI));
            BuildJoystick(canvas);
            UiChild(canvas, "Toast", typeof(ToastHud));
            UiChild(canvas, "Settings", typeof(SettingsPanel));
            UiChild(canvas, "OfflinePopup", typeof(OfflinePopup));
            UiChild(canvas, "MainMenu", typeof(MainMenu));
            UiChild(canvas, "AdService", typeof(AdService));
        }

        private static void UiChild(Transform parent, string name, System.Type component)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = UiLayer };
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.AddComponent(component);
        }

        private static void BuildJoystick(Transform canvas)
        {
            // Joystick: fundal + mâner. OnScreenStick simulează stick-ul stâng al unui gamepad.
            var knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            RectTransform background = UiImage("JoystickBG", canvas, knob, new Color(1f, 1f, 1f, 0.3f), 300f);
            background.anchorMin = background.anchorMax = new Vector2(0.5f, 0f);
            background.anchoredPosition = new Vector2(0f, 330f);

            RectTransform handle = UiImage("JoystickHandle", background, knob, Color.white, 130f);
            var stick = handle.gameObject.AddComponent<OnScreenStick>();
            Edit(stick, so =>
            {
                P(so, "m_ControlPath").stringValue = "<Gamepad>/leftStick";
                P(so, "m_MovementRange").floatValue = 100f;
            });
        }

        private static RectTransform UiImage(string name, Transform parent, Sprite sprite, Color color, float size)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = UiLayer };
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = new Vector2(size, size);
            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            return rect;
        }
    }
}
