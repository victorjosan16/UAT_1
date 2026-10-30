using System;
using TollRoadTycoon.Core;
using TollRoadTycoon.Economy;
using TollRoadTycoon.Localization;
using TollRoadTycoon.TollSystem;
using TollRoadTycoon.Upgrades;
using UnityEngine;
using UnityEngine.UI;

namespace TollRoadTycoon.Presentation.UI
{
    /// <summary>Reusable content blocks shared by tabs and dialogs.</summary>
    public static class Panels
    {
        /// <summary>A card row: icon, title, value line and an action button. Values refresh via the updater.</summary>
        public static Button Row(Transform parent, Color iconColor, string iconText, Func<string> title, Func<string> value,
            Func<string> buttonLabel, Func<bool> buttonEnabled, Action onClick, Action<Action> register, float height = 140, Color? buttonColor = null, float textRightInset = 300)
        {
            var card = UIFactory.Panel(parent, "Row", Color.white);
            UIFactory.Layout(card, height);

            var icon = UIFactory.Panel(card.transform, "Icon", iconColor);
            UIFactory.Place(icon.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(18, 0), new Vector2(height - 36, height - 36));
            var iconLabel = UIFactory.Label(icon.transform, iconText, 40, Color.white);
            UIFactory.AddOutline(iconLabel, new Color(0, 0, 0, 0.3f), 2);
            UIFactory.Stretch(iconLabel.rectTransform);

            var titleText = UIFactory.Label(card.transform, "", 34, Palette.DarkBlue, TextAnchor.LowerLeft);
            var t = titleText.rectTransform;
            t.anchorMin = new Vector2(0, 0.5f);
            t.anchorMax = new Vector2(1, 1);
            t.offsetMin = new Vector2(height, 0);
            t.offsetMax = new Vector2(-textRightInset, -10);

            var valueText = UIFactory.Label(card.transform, "", 32, Palette.Ink, TextAnchor.UpperLeft, FontStyle.Normal);
            var v = valueText.rectTransform;
            v.anchorMin = new Vector2(0, 0);
            v.anchorMax = new Vector2(1, 0.5f);
            v.offsetMin = new Vector2(height, 10);
            v.offsetMax = new Vector2(-textRightInset, -4);

            var button = UIFactory.Button(card.transform, "", buttonColor ?? Palette.Green, onClick, 36);
            UIFactory.Place((RectTransform)button.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-18, 0), new Vector2(260, Mathf.Min(100, height - 30)));
            var label = UIFactory.ButtonText(button);

            register(() =>
            {
                titleText.text = title();
                valueText.text = value();
                label.text = buttonLabel();
                button.interactable = buttonEnabled();
            });
            return button;
        }

        public static Text Header(Transform parent, string text, int size = 40)
        {
            var label = UIFactory.Label(parent, text, size, Palette.DarkBlue);
            UIFactory.Layout(label, size + 30);
            return label;
        }

        public static void BuildBoothUpgrade(UIManager ui, Transform content, TollBooth booth, Action<Action> register, float rowHeight)
        {
            var root = ui.Root;
            var sim = root.Sim;
            var header = Header(content, "", 40);
            register(() => header.text = Loc.F("booth.name", booth.SlotIndex + 1) + "  ·  " + Loc.F("common.level", booth.Level) + "  ·  " + BoothStatus(booth));

            UpgradeRow(ui, content, booth, BoothUpgradeType.Speed, "upgrade.speed", "»", Palette.Blue, register, rowHeight,
                p => double.IsInfinity(p.Current) ? "—" : $"{p.Current:0.0}s → {p.Next:0.0}s");
            long baseToll = sim.Config.FindVehicleType("compact").TollCents;
            UpgradeRow(ui, content, booth, BoothUpgradeType.Toll, "upgrade.toll", "$", Palette.Green, register, rowHeight,
                p => Money.Format(Money.ApplyBasisPoints(baseToll, (long)Math.Round(p.Current * 10000))) + " → " +
                     Money.Format(Money.ApplyBasisPoints(baseToll, (long)Math.Round(p.Next * 10000))));
            UpgradeRow(ui, content, booth, BoothUpgradeType.Capacity, "upgrade.capacity", "#", Palette.DarkBlue, register, rowHeight,
                p => $"{p.Current:0} → {p.Next:0}");
            UpgradeRow(ui, content, booth, BoothUpgradeType.Barrier, "upgrade.barrier", "|", Palette.Red, register, rowHeight,
                p => $"{p.Current:0.00}s → {p.Next:0.00}s");
            UpgradeRow(ui, content, booth, BoothUpgradeType.Automation, "upgrade.automation", "A", Palette.Gold, register, rowHeight,
                p => p.IsLocked ? Loc.T(p.LockReasonKey)
                    : p.IsMaxed ? Loc.T("booth.auto")
                    : (double.IsInfinity(p.Current) ? "—" : $"{p.Current:0.0}s") + $" → {p.Next:0.0}s");
        }

        private static void UpgradeRow(UIManager ui, Transform content, TollBooth booth, BoothUpgradeType type, string titleKey,
            string icon, Color color, Action<Action> register, float height, Func<UpgradePreview, string> describe)
        {
            var sim = ui.Root.Sim;
            Row(content, color, icon,
                () => Loc.T(titleKey) + "  " + Loc.F("common.level", sim.Upgrades.GetPreview(booth, type).Level),
                () => describe(sim.Upgrades.GetPreview(booth, type)),
                () =>
                {
                    var p = sim.Upgrades.GetPreview(booth, type);
                    return p.IsMaxed ? Loc.T("upgrade.max") : Money.Format(p.CostCents);
                },
                () => sim.Upgrades.CanBuy(booth, type),
                () =>
                {
                    var p = sim.Upgrades.GetPreview(booth, type);
                    ui.TryPurchase(p.CostCents, () => sim.Upgrades.TryUpgrade(booth, type));
                },
                register, height);
        }

        public static string BoothStatus(TollBooth booth)
        {
            if (booth.Mode == BoothMode.Automatic) return Loc.T("booth.auto");
            if (booth.AssignedWorker == null) return Loc.T("booth.unstaffed");
            if (booth.PendingClose) return Loc.T("booth.closing");
            return Loc.T("booth.open");
        }

        public static void BuildVehicleUnlocks(UIManager ui, Transform content, Action<Action> register)
        {
            var sim = ui.Root.Sim;
            Header(content, Loc.T("upgrade.vehicles"));
            foreach (var type in sim.Config.VehicleTypes)
            {
                var t = type;
                Row(content, t.IsEmergency ? Palette.Red : Palette.Blue, Loc.T(t.NameKey).Substring(0, 1),
                    () => Loc.T(t.NameKey),
                    () => t.TollCents > 0 ? Loc.F("vehicle.toll", Money.Format(t.TollCents)) : Loc.T("vehicle.free"),
                    () => sim.Upgrades.IsVehicleUnlocked(t.Id) ? Loc.T("upgrade.unlocked") : Money.Format(t.UnlockPriceCents),
                    () => !sim.Upgrades.IsVehicleUnlocked(t.Id) && sim.Economy.CanAfford(t.UnlockPriceCents),
                    () => ui.TryPurchase(t.UnlockPriceCents, () => sim.Upgrades.TryUnlockVehicle(t.Id)),
                    register, 120);
            }
        }

        public static void BuildSettings(UIManager ui, Transform content, Action<Action> register)
        {
            var root = ui.Root;
            var s = root.Settings;

            void Changed()
            {
                root.ApplySettings();
                root.SaveNow();
            }

            ToggleRow(ui, content, Loc.T("settings.music"), () => false, null, register, enabled: false);
            ToggleRow(ui, content, Loc.T("settings.sfx"), () => s.SfxEnabled, () => { s.SfxEnabled = !s.SfxEnabled; Changed(); }, register);
            ToggleRow(ui, content, Loc.T("settings.vibration"), () => s.VibrationEnabled, () => { s.VibrationEnabled = !s.VibrationEnabled; Changed(); }, register);
            CycleRow(content, Loc.T("settings.quality"), () => Loc.T("quality." + Mathf.Clamp(s.GraphicsQuality, 0, 2)),
                d => { s.GraphicsQuality = (s.GraphicsQuality + d + 3) % 3; Changed(); }, register);
            CycleRow(content, Loc.T("settings.language"), () => Loc.T("lang." + s.Language),
                d => { s.Language = s.Language == Loc.Romanian ? Loc.English : Loc.Romanian; Changed(); }, register);

            var privacy = UIFactory.Button(content, Loc.T("settings.privacy"), Palette.Blue,
                () => ui.ShowInfo(Loc.T("settings.privacy"), Loc.T("settings.privacy.body")), 38);
            UIFactory.Layout(privacy, 110);
            var reset = UIFactory.Button(content, Loc.T("settings.reset"), Palette.Red, () => ConfirmReset(ui), 38);
            UIFactory.Layout(reset, 110);
        }

        private static void ConfirmReset(UIManager ui)
        {
            var content = ui.OpenModal(Loc.T("settings.reset"), 640);
            var text = UIFactory.Label(content, Loc.T("settings.reset.body"), 40, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Normal);
            UIFactory.Layout(text, 200);
            var row = UIFactory.Rect(content, "Buttons");
            UIFactory.HorizontalList(row, 24, 0);
            UIFactory.Layout(row, 120);
            UIFactory.Button(row, Loc.T("confirm.cancel"), Palette.Green, ui.CloseModal, 40);
            UIFactory.Button(row, Loc.T("settings.reset"), Palette.Red, () =>
            {
                ui.CloseModal();
                ui.Root.ResetProgress();
            }, 36);
        }

        private static void ToggleRow(UIManager ui, Transform content, string title, Func<bool> get, Action toggle, Action<Action> register, bool enabled = true)
        {
            var row = UIFactory.Panel(content, "Toggle", Color.white);
            UIFactory.Layout(row, 110);
            var label = UIFactory.Label(row.transform, title, 36, enabled ? Palette.DarkBlue : Palette.Disabled, TextAnchor.MiddleLeft);
            UIFactory.Stretch(label.rectTransform, 30, 260, 0, 0);
            var button = UIFactory.Button(row.transform, "", Palette.Green, () => toggle?.Invoke(), 34);
            UIFactory.Place((RectTransform)button.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-18, 0), new Vector2(220, 80));
            var image = button.GetComponent<Image>();
            var text = UIFactory.ButtonText(button);
            register(() =>
            {
                bool on = get();
                button.interactable = enabled;
                image.color = on ? Palette.Green : Palette.Disabled;
                text.text = Loc.T(on ? "settings.on" : "settings.off");
            });
        }

        private static void CycleRow(Transform content, string title, Func<string> value, Action<int> change, Action<Action> register)
        {
            var row = UIFactory.Panel(content, "Cycle", Color.white);
            UIFactory.Layout(row, 110);
            var label = UIFactory.Label(row.transform, title, 36, Palette.DarkBlue, TextAnchor.MiddleLeft);
            UIFactory.Stretch(label.rectTransform, 30, 420, 0, 0);
            var prev = UIFactory.Button(row.transform, "<", Palette.Blue, () => change(-1), 36);
            UIFactory.Place((RectTransform)prev.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-330, 0), new Vector2(80, 80));
            var valueText = UIFactory.Label(row.transform, "", 32, Palette.Ink);
            UIFactory.Place(valueText.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-110, 0), new Vector2(210, 80));
            var next = UIFactory.Button(row.transform, ">", Palette.Blue, () => change(1), 36);
            UIFactory.Place((RectTransform)next.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-18, 0), new Vector2(80, 80));
            register(() => valueText.text = value());
        }
    }
}
