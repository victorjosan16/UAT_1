using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace QuarryEmpire.EditorTools
{
    public static partial class VisualSetup
    {
        private const string HudRootName = VisualPrefix + "HUD";
        private const string PauseRootName = VisualPrefix + "PausePanel";

        private static readonly Color Navy = VisualAssetFactory.Hex("#22304F");
        private static readonly Color Gold = VisualAssetFactory.Hex("#FFC83D");
        private static readonly Color Orange = VisualAssetFactory.Hex("#FF8A3D");
        private static readonly Color Green = VisualAssetFactory.Hex("#3DBE6A");
        private static readonly Color Purple = VisualAssetFactory.Hex("#8A5CF6");

        /// <summary>
        /// HUD nou (în Safe Area) peste Canvas-ul existent. Joystick-ul rămâne exact cel din Etapa 1
        /// (OnScreenStick, poziție, mărime) — primește doar sprite-uri noi și decor fără raycast.
        /// Valori de referință: 1080×1920 portret (CanvasScaler-ul Etapei 1, neschimbat).
        /// </summary>
        private static void BuildHud(SceneRefs refs, Palette p)
        {
            if (refs.Canvas == null) throw new Exception("Lipsește Canvas-ul Etapei 1.");
            Transform canvas = refs.Canvas.transform;
            DestroyChild(canvas, HudRootName);
            DestroyChild(canvas, PauseRootName);

            // ---- Rădăcina HUD în Safe Area (prima în ierarhie: joystick-ul rămâne deasupra ei)
            RectTransform hudRoot = UiRect(HudRootName, canvas);
            Stretch(hudRoot);
            hudRoot.SetAsFirstSibling();
            hudRoot.gameObject.AddComponent<SafeAreaFitter>();
            var hud = hudRoot.gameObject.AddComponent<HudController>();
            var binder = hudRoot.gameObject.AddComponent<HudStage1Binder>();
            SetRef(binder, "hud", hud);

            // ---- Bara de sus: nivel, bani, pauză
            RectTransform top = UiRect("TopBar", hudRoot);
            TopStretch(top, -24f, 140f);

            RectTransform badge = UiRect("LevelBadge", top);
            Place(badge, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(140f, 140f));
            UiImage("Shadow", badge, p.IconStar, VisualAssetFactory.Hex("#C9761E"), stretch: true).rectTransform.anchoredPosition = new Vector2(0f, -6f);
            UiImage("Star", badge, p.IconStar, VisualAssetFactory.Hex("#FFB531"), stretch: true);
            TMP_Text levelLabel = UiLabel("Level", badge, "1", 58f, Color.white, TextAlignmentOptions.Center);
            Stretch(levelLabel.rectTransform);
            levelLabel.rectTransform.anchoredPosition = new Vector2(0f, -6f);
            var levelPunch = badge.gameObject.AddComponent<UiPunchScale>();

            RectTransform money = UiRect("MoneyPill", top);
            Place(money, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(160f, 0f), new Vector2(400f, 100f));
            Panel(money, p, new Color(Navy.r, Navy.g, Navy.b, 0.92f), 0.9f, 6f);
            Image coin = UiImage("Coin", money, p.IconCoin, Gold);
            Place(coin.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(76f, 76f));
            TMP_Text moneyLabel = UiLabel("Amount", money, "0", 54f, Color.white, TextAlignmentOptions.MidlineLeft);
            Stretch(moneyLabel.rectTransform, 104f, 20f, 0f, 0f);
            var moneyPunch = money.gameObject.AddComponent<UiPunchScale>();

            Button pause = UiButton("PauseButton", top, p, Color.white, new Vector2(116f, 116f), 1.2f);
            Place((RectTransform)pause.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(116f, 116f));
            Image pauseIcon = UiImage("Icon", pause.transform, p.IconPause, Navy);
            Place(pauseIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(56f, 56f));

            // ---- Progres
            RectTransform progress = UiRect("Progress", hudRoot);
            TopStretch(progress, -182f, 44f);
            UiProgressBar progressBar = ProgressBar(progress, p, new Color(Navy.r, Navy.g, Navy.b, 0.55f), VisualAssetFactory.Hex("#7BD34A"), 2f, 5f);
            TMP_Text progressLabel = UiLabel("Label", progress, "Carieră 0/0", 28f, Color.white, TextAlignmentOptions.Center);
            Stretch(progressLabel.rectTransform);

            // ---- Misiune compactă
            RectTransform mission = UiRect("MissionCard", hudRoot);
            TopStretch(mission, -246f, 128f);
            Panel(mission, p, new Color(1f, 1f, 1f, 0.96f), 1.4f, 8f);
            Image missionIconBg = UiImage("IconBg", mission, p.Circle, Orange);
            Place(missionIconBg.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(88f, 88f));
            Image missionIcon = UiImage("Icon", missionIconBg.transform, p.IconTarget, Color.white);
            Place(missionIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(56f, 56f));
            TMP_Text missionLabel = UiLabel("Description", mission, "Sparge 10 blocuri", 36f, Navy, TextAlignmentOptions.MidlineLeft);
            RectTransform missionLabelRect = missionLabel.rectTransform;
            missionLabelRect.anchorMin = new Vector2(0f, 0.42f);
            missionLabelRect.anchorMax = new Vector2(1f, 1f);
            missionLabelRect.offsetMin = new Vector2(128f, 0f);
            missionLabelRect.offsetMax = new Vector2(-150f, -10f);
            missionLabel.enableAutoSizing = true;
            missionLabel.fontSizeMin = 24f;
            missionLabel.fontSizeMax = 36f;
            RectTransform missionBarRect = UiRect("Bar", mission);
            missionBarRect.anchorMin = new Vector2(0f, 0f);
            missionBarRect.anchorMax = new Vector2(1f, 0f);
            missionBarRect.pivot = new Vector2(0.5f, 0f);
            missionBarRect.offsetMin = new Vector2(128f, 26f);
            missionBarRect.offsetMax = new Vector2(-150f, 46f);
            UiProgressBar missionBar = ProgressBar(missionBarRect, p, new Color(Navy.r, Navy.g, Navy.b, 0.14f), Orange, 4f, 0f);
            TMP_Text missionCount = UiLabel("Count", mission, "0/10", 40f, Navy, TextAlignmentOptions.MidlineRight);
            Place(missionCount.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(130f, 64f));
            var missionPunch = mission.gameObject.AddComponent<UiPunchScale>();
            SetFloat(missionPunch, "amount", 0.06f);

            // ---- Cipuri de resurse
            RectTransform chips = UiRect("ResourceChips", hudRoot);
            Place(chips, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -394f), new Vector2(980f, 64f));
            var layout = chips.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            RectTransform chip = UiRect("ChipTemplate", chips);
            chip.sizeDelta = new Vector2(150f, 60f);
            Image chipBg = chip.gameObject.AddComponent<Image>();
            ConfigureSliced(chipBg, p.RoundRect, new Color(Navy.r, Navy.g, Navy.b, 0.8f), 1.5f);
            chipBg.raycastTarget = false;
            Image dot = UiImage("Dot", chip, p.Circle, Color.gray);
            Place(dot.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(34f, 34f));
            TMP_Text chipCount = UiLabel("Count", chip, "0", 32f, Color.white, TextAlignmentOptions.MidlineLeft);
            Stretch(chipCount.rectTransform, 58f, 12f, 0f, 0f);
            chip.gameObject.AddComponent<UiPunchScale>();
            var chipsBar = chips.gameObject.AddComponent<ResourceChipsBar>();
            SetRef(chipsBar, "chipTemplate", chip);
            chip.gameObject.SetActive(false); // modelul nu apare în scenă; cipurile reale se clonează din el

            // ---- Butoane bonus (jos-dreapta, deasupra zonei joystick-ului, fără suprapunere)
            BuildBonusButton(hudRoot, p, "BonusTurbo", "TURBO", p.IconBolt, Orange, new Vector2(-32f, 260f));
            BuildBonusButton(hudRoot, p, "BonusMagnet", "MAGNET", p.IconMagnet, Purple, new Vector2(-32f, 450f));

            // ---- Joystick: doar aspect
            StyleJoystick(refs, p);

            // ---- Contorul de debug al Etapei 1: dezactivat (cipurile îl înlocuiesc), nu șters.
            if (refs.DebugCounter != null) refs.DebugCounter.SetActive(false);

            // ---- Pauză
            PausePanel pausePanel = BuildPausePanel(canvas, p);

            var so = new SerializedObject(hud);
            so.FindProperty("levelLabel").objectReferenceValue = levelLabel;
            so.FindProperty("levelPunch").objectReferenceValue = levelPunch;
            so.FindProperty("moneyLabel").objectReferenceValue = moneyLabel;
            so.FindProperty("moneyPunch").objectReferenceValue = moneyPunch;
            so.FindProperty("progressBar").objectReferenceValue = progressBar;
            so.FindProperty("progressLabel").objectReferenceValue = progressLabel;
            so.FindProperty("pauseButton").objectReferenceValue = pause;
            so.FindProperty("pausePanel").objectReferenceValue = pausePanel;
            so.FindProperty("missionLabel").objectReferenceValue = missionLabel;
            so.FindProperty("missionCountLabel").objectReferenceValue = missionCount;
            so.FindProperty("missionBar").objectReferenceValue = missionBar;
            so.FindProperty("missionPunch").objectReferenceValue = missionPunch;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildBonusButton(Transform parent, Palette p, string name, string caption, Sprite icon,
            Color color, Vector2 position)
        {
            var size = new Vector2(150f, 150f);
            RectTransform root = UiRect(name, parent);
            Place(root, new Vector2(1f, 0f), new Vector2(1f, 0f), position, size);

            RectTransform content = UiRect("Content", root);
            Stretch(content);
            var group = content.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = true;

            Image shadow = UiImage("Shadow", content, p.Circle, color * 0.7f, stretch: true);
            shadow.color = new Color(shadow.color.r, shadow.color.g, shadow.color.b, 1f);
            shadow.rectTransform.anchoredPosition = new Vector2(0f, -8f);
            Image face = UiImage("Face", content, p.Circle, color, stretch: true);
            face.raycastTarget = true;
            Image iconImage = UiImage("Icon", content, icon, Color.white);
            Place(iconImage.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 12f), new Vector2(72f, 72f));
            TMP_Text label = UiLabel("Caption", content, caption, 24f, Color.white, TextAlignmentOptions.Center);
            Place(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -44f), new Vector2(140f, 34f));

            Image cooldown = UiImage("Cooldown", root, p.Circle, new Color(0f, 0f, 0f, 0.45f), stretch: true);
            cooldown.type = Image.Type.Filled;
            cooldown.fillMethod = Image.FillMethod.Radial360;
            cooldown.fillOrigin = (int)Image.Origin360.Top;
            cooldown.fillClockwise = false;
            cooldown.fillAmount = 0f;
            cooldown.gameObject.SetActive(false);
            TMP_Text timer = UiLabel("Timer", root, string.Empty, 46f, Color.white, TextAlignmentOptions.Center);
            Stretch(timer.rectTransform);

            RectTransform badge = UiRect("ComingSoon", root);
            Place(badge, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), new Vector2(150f, 40f));
            Image badgeBg = badge.gameObject.AddComponent<Image>();
            ConfigureSliced(badgeBg, p.RoundRect, Navy, 2.2f);
            badgeBg.raycastTarget = false;
            TMP_Text badgeLabel = UiLabel("Label", badge, "ÎN CURÂND", 20f, Color.white, TextAlignmentOptions.Center);
            Stretch(badgeLabel.rectTransform);

            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            button.transition = Selectable.Transition.None; // apăsarea e animată de UiPressFeedback
            root.gameObject.AddComponent<UiPressFeedback>();
            var bonus = root.gameObject.AddComponent<BonusButton>();
            var so = new SerializedObject(bonus);
            so.FindProperty("cooldownOverlay").objectReferenceValue = cooldown;
            so.FindProperty("comingSoonBadge").objectReferenceValue = badge.gameObject;
            so.FindProperty("timerLabel").objectReferenceValue = timer;
            so.FindProperty("contentGroup").objectReferenceValue = group;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void StyleJoystick(SceneRefs refs, Palette p)
        {
            RectTransform background = refs.JoystickBackground;
            RectTransform handle = refs.JoystickHandle;
            if (background == null || handle == null)
            {
                Debug.LogWarning("[QuarryEmpire/Vizual] Joystick-ul Etapei 1 nu a fost găsit: rămâne neschimbat.");
                return;
            }

            DestroyChild(background, VisualPrefix + "Ring");
            DestroyChild(background, VisualPrefix + "Direction");
            DestroyChild(handle, VisualPrefix + "HandleInner");
            if (background.TryGetComponent(out JoystickVisual oldVisual)) Object.DestroyImmediate(oldVisual);

            // Fundal: disc bleumarin translucid (contrast bun pe iarbă și pe pământ) + inel alb.
            var backgroundImage = background.GetComponent<Image>();
            backgroundImage.sprite = p.Circle;
            backgroundImage.color = new Color(Navy.r, Navy.g, Navy.b, 0.35f);

            Image ring = UiImage(VisualPrefix + "Ring", background, p.Ring, new Color(1f, 1f, 1f, 0.6f), stretch: true);
            ring.transform.SetAsFirstSibling();
            RectTransform direction = UiRect(VisualPrefix + "Direction", background);
            Stretch(direction);
            direction.SetSiblingIndex(1);
            Image chevron = UiImage("Chevron", direction, p.IconChevron, Color.white);
            Place(chevron.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -30f), new Vector2(60f, 60f));
            handle.SetAsLastSibling(); // mânerul rămâne deasupra decorului

            // Mâner: cerc alb cu centru auriu; raycast-ul rămâne pe Image-ul mânerului (necesar pentru OnScreenStick).
            var handleImage = handle.GetComponent<Image>();
            handleImage.sprite = p.Circle;
            handleImage.color = Color.white;
            if (!handle.TryGetComponent(out Shadow shadow)) shadow = handle.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(Navy.r, Navy.g, Navy.b, 0.35f);
            shadow.effectDistance = new Vector2(0f, -6f);
            Image inner = UiImage(VisualPrefix + "HandleInner", handle, p.Circle, Gold);
            Place(inner.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, handle.sizeDelta * 0.62f);

            float range = 100f;
            var stick = handle.GetComponent<UnityEngine.InputSystem.OnScreen.OnScreenStick>();
            if (stick != null)
            {
                SerializedProperty rangeProperty = new SerializedObject(stick).FindProperty("m_MovementRange");
                if (rangeProperty != null) range = rangeProperty.floatValue;
            }

            var visual = background.gameObject.AddComponent<JoystickVisual>();
            var so = new SerializedObject(visual);
            so.FindProperty("handle").objectReferenceValue = handle;
            so.FindProperty("background").objectReferenceValue = backgroundImage;
            so.FindProperty("directionIndicator").objectReferenceValue = direction;
            so.FindProperty("directionImage").objectReferenceValue = chevron;
            so.FindProperty("movementRange").floatValue = range;
            so.FindProperty("idleAlpha").floatValue = 0.35f;
            so.FindProperty("activeAlpha").floatValue = 0.55f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static PausePanel BuildPausePanel(Transform canvas, Palette p)
        {
            RectTransform root = UiRect(PauseRootName, canvas);
            Stretch(root);
            root.SetAsLastSibling();
            Image dim = root.gameObject.AddComponent<Image>();
            dim.color = new Color(Navy.r, Navy.g, Navy.b, 0.65f);
            dim.raycastTarget = true; // blochează atingerile cât e pauza
            var group = root.gameObject.AddComponent<CanvasGroup>();

            RectTransform card = UiRect("Card", root);
            Place(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 640f));
            Panel(card, p, Color.white, 1.1f, 10f);
            TMP_Text title = UiLabel("Title", card, "PAUZĂ", 84f, Navy, TextAlignmentOptions.Center);
            Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 210f), new Vector2(680f, 120f));

            Button resume = UiButton("ResumeButton", card, p, Green, new Vector2(520f, 140f), 1f);
            Place((RectTransform)resume.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(520f, 140f));
            TMP_Text resumeLabel = UiLabel("Label", resume.transform, "CONTINUĂ", 52f, Color.white, TextAlignmentOptions.Center);
            Stretch(resumeLabel.rectTransform);

            Button quality = UiButton("QualityButton", card, p, VisualAssetFactory.Hex("#EEF2F8"), new Vector2(560f, 110f), 1.2f);
            Place((RectTransform)quality.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -170f), new Vector2(560f, 110f));
            TMP_Text qualityLabel = UiLabel("Label", quality.transform, "Grafică: Calitate (60 FPS)", 34f, Navy, TextAlignmentOptions.Center);
            Stretch(qualityLabel.rectTransform, 16f, 16f, 0f, 0f);
            qualityLabel.enableAutoSizing = true;
            qualityLabel.fontSizeMin = 22f;
            qualityLabel.fontSizeMax = 34f;

            var panel = root.gameObject.AddComponent<PausePanel>();
            var so = new SerializedObject(panel);
            so.FindProperty("group").objectReferenceValue = group;
            so.FindProperty("card").objectReferenceValue = card;
            so.FindProperty("resumeButton").objectReferenceValue = resume;
            so.FindProperty("qualityButton").objectReferenceValue = quality;
            so.FindProperty("qualityLabel").objectReferenceValue = qualityLabel;
            so.ApplyModifiedPropertiesWithoutUndo();
            // Închis în scenă (nu acoperă Game view-ul în Editor); PausePanel.Open() îl activează.
            group.alpha = 0f;
            root.gameObject.SetActive(false);
            return panel;
        }

        // ================================================================ Utilitare UI

        private static RectTransform UiRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = 5 };
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static void Stretch(RectTransform rect, float left = 0f, float right = 0f, float top = 0f, float bottom = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        /// <summary>Întins pe lățime (cu margini de 32), lipit sus la distanța y, de înălțime fixă.</summary>
        private static void TopStretch(RectTransform rect, float y, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(-64f, height);
            rect.anchoredPosition = new Vector2(0f, y);
        }

        private static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static Image UiImage(string name, Transform parent, Sprite sprite, Color color, bool stretch = false)
        {
            RectTransform rect = UiRect(name, parent);
            if (stretch) Stretch(rect);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = true;
            image.raycastTarget = false; // doar butoanele primesc atingeri (mai puține verificări, fără blocaje)
            return image;
        }

        private static void ConfigureSliced(Image image, Sprite sprite, Color color, float cornerMultiplier)
        {
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = cornerMultiplier; // mai mare = colțuri mai mici
            image.color = color;
            image.preserveAspect = false;
        }

        /// <summary>Panou rotunjit cu „umbră” plată dedesubt (stil casual, fără blur costisitor).</summary>
        private static Image Panel(RectTransform target, Palette p, Color color, float cornerMultiplier, float shadowOffset)
        {
            if (shadowOffset > 0f)
            {
                RectTransform shadowRect = UiRect("Shadow", target);
                Stretch(shadowRect);
                shadowRect.anchoredPosition = new Vector2(0f, -shadowOffset);
                var shadow = shadowRect.gameObject.AddComponent<Image>();
                ConfigureSliced(shadow, p.RoundRect, new Color(Navy.r, Navy.g, Navy.b, 0.25f), cornerMultiplier);
                shadow.raycastTarget = false;
                shadowRect.SetAsFirstSibling();
            }
            RectTransform faceRect = UiRect("Face", target);
            Stretch(faceRect);
            faceRect.SetSiblingIndex(shadowOffset > 0f ? 1 : 0);
            var face = faceRect.gameObject.AddComponent<Image>();
            ConfigureSliced(face, p.RoundRect, color, cornerMultiplier);
            face.raycastTarget = false;
            return face;
        }

        private static Button UiButton(string name, Transform parent, Palette p, Color color, Vector2 size, float cornerMultiplier)
        {
            RectTransform rect = UiRect(name, parent);
            rect.sizeDelta = size;
            Image face = Panel(rect, p, color, cornerMultiplier, 8f);
            face.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            ColorBlock colors = button.colors;
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.9f, 0.9f, 0.9f);
            colors.selectedColor = Color.white;
            button.colors = colors;
            rect.gameObject.AddComponent<UiPressFeedback>();
            return button;
        }

        private static TMP_Text UiLabel(string name, Transform parent, string text, float size, Color color, TextAlignmentOptions alignment)
        {
            RectTransform rect = UiRect(name, parent);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.fontStyle = FontStyles.Bold;
            label.alignment = alignment;
            label.raycastTarget = false;
            label.overflowMode = TextOverflowModes.Overflow;
            return label;
        }

        private static UiProgressBar ProgressBar(RectTransform target, Palette p, Color background, Color fillColor,
            float cornerMultiplier, float padding)
        {
            var bg = target.gameObject.AddComponent<Image>();
            ConfigureSliced(bg, p.RoundRect, background, cornerMultiplier);
            bg.raycastTarget = false;

            RectTransform area = UiRect("FillArea", target);
            Stretch(area, padding, padding, padding, padding);
            RectTransform fillRect = UiRect("Fill", area);
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fill = fillRect.gameObject.AddComponent<Image>();
            ConfigureSliced(fill, p.RoundRect, fillColor, cornerMultiplier * 1.2f);
            fill.raycastTarget = false;

            var bar = target.gameObject.AddComponent<UiProgressBar>();
            SetRef(bar, "fill", fill);
            return bar;
        }
    }
}
