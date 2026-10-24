using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
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
            // Textele plutitoare stau sub HUD (în lume), restul HUD-ului în zona sigură (fără notch / bara de gesturi).
            UiChild(canvas, "FloatingText", typeof(FloatingTextUI));
            Transform safe = UiChild(canvas, "SafeArea", typeof(SafeAreaFitter));
            // Joystick-ul flotant e primul: zona lui mare de atingere stă sub butoane, care primesc atingerile primele.
            BuildJoystick(safe);
            UiChild(safe, "LayerHud", typeof(LayerHud));
            UiChild(safe, "MoneyHud", typeof(MoneyHud));
            UiChild(safe, "OrderHud", typeof(OrderHud));
            UiChild(safe, "RewardedOffers", typeof(RewardedOffersHud));
            UiChild(safe, "VehicleHud", typeof(VehicleHud));
            UiChild(safe, "Toast", typeof(ToastHud));
            UiChild(canvas, "CoinFly", typeof(CoinFlyUI));
            UiChild(canvas, "LayerTransition", typeof(LayerTransitionUI));
            UiChild(safe, "Settings", typeof(SettingsPanel));
            UiChild(safe, "OfflinePopup", typeof(OfflinePopup));
            UiChild(canvas, "MainMenu", typeof(MainMenu));
            UiChild(canvas, "AdService", typeof(AdService));
        }

        private static Transform UiChild(Transform parent, string name, System.Type component)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = UiLayer };
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.AddComponent(component);
            return rect;
        }

        /// <summary>
        /// Joystick flotant: o zonă transparentă pe jumătatea de jos a ecranului; inelul apare unde pui degetul.
        /// Trimite direcția ca stick-ul stâng al unui gamepad (la fel ca vechiul OnScreenStick).
        /// </summary>
        private static void BuildJoystick(Transform parent)
        {
            var areaGo = new GameObject("JoystickArea", typeof(RectTransform)) { layer = UiLayer };
            var area = (RectTransform)areaGo.transform;
            area.SetParent(parent, false);
            area.anchorMin = Vector2.zero;
            area.anchorMax = new Vector2(1f, 0.55f);
            area.offsetMin = Vector2.zero;
            area.offsetMax = Vector2.zero;
            var touchSurface = areaGo.AddComponent<Image>();
            touchSurface.color = new Color(1f, 1f, 1f, 0f); // invizibilă, dar primește atingerile
            touchSurface.raycastTarget = true;

            var knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            RectTransform background = UiImage("JoystickBG", area, knob, new Color(1f, 1f, 1f, 0.3f), 260f);
            background.anchorMin = background.anchorMax = new Vector2(0.5f, 0f);
            background.anchoredPosition = new Vector2(0f, 300f);
            background.GetComponent<Image>().raycastTarget = false;

            RectTransform handle = UiImage("JoystickHandle", background, knob, Color.white, 120f);
            handle.GetComponent<Image>().raycastTarget = false;

            var joystick = areaGo.AddComponent<FloatingJoystick>();
            Edit(joystick, so =>
            {
                P(so, "m_ControlPath").stringValue = "<Gamepad>/leftStick";
                P(so, "background").objectReferenceValue = background;
                P(so, "handle").objectReferenceValue = handle;
                P(so, "radius").floatValue = 110f;
            });

            // Aspectul (inel + buton cu umbră) se aplică la rulare.
            var skin = areaGo.AddComponent<JoystickSkin>();
            Set(skin, "background", background.GetComponent<Image>());
            Set(skin, "handle", handle.GetComponent<Image>());
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
