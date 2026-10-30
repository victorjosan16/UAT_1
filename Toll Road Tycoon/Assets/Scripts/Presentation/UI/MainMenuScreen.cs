using TollRoadTycoon.Economy;
using TollRoadTycoon.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace TollRoadTycoon.Presentation.UI
{
    /// <summary>
    /// Main menu over the live road. Only working features are active; DRUMURI, MAGAZIN and MISIUNI
    /// are clearly marked as coming in Phase 2 instead of pretending to work.
    /// </summary>
    public sealed class MainMenuScreen : MonoBehaviour
    {
        private Text _balance;

        public void Build(UIManager ui)
        {
            UIFactory.Clear(transform);
            var root = ui.Root;

            var shade = UIFactory.Panel(transform, "Shade", new Color(0.02f, 0.25f, 0.45f, 0.35f), SpriteFactory.Square);
            UIFactory.Stretch(shade.rectTransform);
            shade.raycastTarget = false;

            var balance = UIFactory.Panel(transform, "Balance", Palette.DarkBlue, SpriteFactory.Pill);
            UIFactory.Place(balance.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -24), new Vector2(560, 100));
            _balance = UIFactory.Label(balance.transform, "", 50, Color.white);
            UIFactory.Stretch(_balance.rectTransform);

            // Logo badge.
            var logo = UIFactory.Panel(transform, "Logo", Palette.Blue);
            UIFactory.Place(logo.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -170), new Vector2(820, 470));
            UIFactory.AddDropShadow(logo, 14);
            var inner = UIFactory.Panel(logo.transform, "Inner", Palette.DarkBlue);
            UIFactory.Stretch(inner.rectTransform, 16, 16, 16, 16);
            var title = UIFactory.Label(inner.transform, "TOLL ROAD", 120, Palette.Gold);
            UIFactory.AddOutline(title, Palette.Ink, 5);
            UIFactory.Band(title.rectTransform, true, 40, 160, 10);
            var subtitle = UIFactory.Label(inner.transform, "TYCOON", 120, Color.white);
            UIFactory.AddOutline(subtitle, Palette.Ink, 5);
            UIFactory.Band(subtitle.rectTransform, true, 200, 160, 10);
            var stripe = UIFactory.Panel(inner.transform, "Stripe", Palette.Gold, SpriteFactory.Pill);
            UIFactory.Band(stripe.rectTransform, false, 34, 18, 120);

            float y = -720;
            MenuButton(ui, "menu.play", Palette.Green, y, 160, 64, root.StartGameplay);
            y -= 190;
            MenuButton(ui, "menu.roads", Palette.Blue, y, 130, 48, () => ui.ShowInfo(Loc.T("menu.roads"), Loc.T("menu.soon.body")), true);
            y -= 155;
            MenuButton(ui, "menu.shop", Palette.Blue, y, 130, 48, () => ui.ShowInfo(Loc.T("menu.shop"), Loc.T("menu.soon.body")), true);
            y -= 155;
            MenuButton(ui, "menu.missions", Palette.Blue, y, 130, 48, () => ui.ShowInfo(Loc.T("menu.missions"), Loc.T("menu.soon.body")), true);
            y -= 155;
            MenuButton(ui, "menu.settings", Palette.Blue, y, 130, 48, ui.ShowSettings);
        }

        private void MenuButton(UIManager ui, string key, Color color, float y, float height, int fontSize, System.Action onClick, bool comingSoon = false)
        {
            var button = UIFactory.Button(transform, Loc.T(key), color, () =>
            {
                ui.Root.Audio.Play(Sfx.Click);
                onClick();
            }, fontSize);
            UIFactory.Place((RectTransform)button.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, y), new Vector2(640, height));
            if (!comingSoon) return;
            button.GetComponent<Image>().color = Color.Lerp(color, Palette.Disabled, 0.45f);
            var badge = UIFactory.Panel(button.transform, "Soon", Palette.Gold, SpriteFactory.Pill);
            UIFactory.Place(badge.rectTransform, new Vector2(1, 1), new Vector2(1, 0.5f), new Vector2(-10, -6), new Vector2(230, 56));
            var text = UIFactory.Label(badge.transform, Loc.T("menu.soon"), 28, Palette.DarkBlue);
            UIFactory.Stretch(text.rectTransform);
        }

        private void Update()
        {
            var root = GameRoot.Instance;
            if (_balance != null && root != null && root.Sim != null) _balance.text = Money.Format(root.Sim.Economy.Balance);
        }
    }
}
