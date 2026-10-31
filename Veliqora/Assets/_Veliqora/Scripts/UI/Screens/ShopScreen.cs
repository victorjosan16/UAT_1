using UnityEngine;
using UnityEngine.UI;
using Veliqora.Core.Ads;
using Veliqora.Core.Economy;
using Veliqora.Infrastructure;
using Veliqora.Presentation;

namespace Veliqora.UI.Screens
{
    /// <summary>Booster shop: virtual-coin purchases only (no real-money checkout), clear prices and feedback.</summary>
    public sealed class ShopScreen : ScreenBase
    {
        public override bool AllowsBanner => true;

        protected override void BuildBackground(RectTransform full)
        {
            // Dark-fantasy themes carry the whole UI; illustrated themes use the authored shop scene.
            var theme = S.Themes.Theme;
            Texture tex = theme.IsDark ? null : BackgroundArt.ScreenArt("shop");
            if (tex == null) tex = BackgroundArt.Valley(theme);
            FullBackground(full, tex);
            ReadabilityScrim(full, 0.4f, 0.4f, 0.15f);
        }

        protected override void Build(object args)
        {
            var bar = TopBar();
            AddBack(bar, () => S.Navigator.GoBackToPrevious());
            AddCoins(bar);

            var bg = UIFactory.Panel(Safe, "ShopPanel", UIStyle.Ivory, UIStyle.RadiusLarge);
            var panel = (RectTransform)bg.transform.parent;
            panel.offsetMin = new Vector2(40, 220);
            panel.offsetMax = new Vector2(-40, -UIStyle.TopBarHeight - 120);
            Ribbon(Safe, "Boosters", -UIStyle.TopBarHeight - 50, 560);

            var list = UIFactory.Rect("List", panel);
            UIFactory.Stretch(list, 36);
            list.offsetMax = new Vector2(-36, -90);
            float y = 0;
            const float cardH = 210;
            foreach (var offer in S.EconomyConfig.Offers)
            {
                BuildCard(list, offer, y, cardH);
                y -= cardH + 20;
            }
            BuildAdCard(list, y, cardH);

            var themes = UIFactory.Button(Safe, "Themes & bottles", UIStyle.ButtonKind.Accent, () => S.Navigator.Go(ScreenId.Themes), new Vector2(640, 140), Icon.Palette);
            UIFactory.Place(themes.Rect, new Vector2(0.5f, 0f), themes.Rect.sizeDelta, new Vector2(0, 50), new Vector2(0.5f, 0f));
        }

        private static Icon IconFor(BoosterType t)
        {
            switch (t)
            {
                case BoosterType.Undo: return Icon.Undo;
                case BoosterType.ExtraBottle: return Icon.BottlePlus;
                case BoosterType.Shuffle: return Icon.Shuffle;
                default: return Icon.Bulb;
            }
        }

        private static Color ColorFor(BoosterType t)
        {
            switch (t)
            {
                case BoosterType.Undo: return UIStyle.Hex("#3D8BFF");
                case BoosterType.ExtraBottle: return UIStyle.Hex("#36B0C9");
                case BoosterType.Shuffle: return UIStyle.Hex("#8E5CF7");
                default: return UIStyle.Hex("#F5A623");
            }
        }

        private RectTransform Card(RectTransform list, float y, float h, out Image bg)
        {
            bg = UIFactory.Panel(list, "Card", UIStyle.Card, UIStyle.RadiusMedium, false);
            var card = (RectTransform)bg.transform.parent;
            UIFactory.Band(card, 1f, h, y, 0, 1f);
            var edge = UIFactory.Image(card, "Edge", SpriteLibrary.Rounded(UIStyle.RadiusMedium), UIStyle.IvoryDark);
            edge.transform.SetAsFirstSibling();
            edge.rectTransform.offsetMin = new Vector2(0, -6);
            return card;
        }

        private void BuildCard(RectTransform list, BoosterOffer offer, float y, float h)
        {
            var card = Card(list, y, h, out _);
            var icon = UIFactory.IconButton(card, IconFor(offer.Booster), ColorFor(offer.Booster), 150, null);
            UIFactory.Place(icon.Rect, new Vector2(0, 0.5f), icon.Rect.sizeDelta, new Vector2(30, 0), new Vector2(0, 0.5f));
            icon.Button.interactable = false;
            var title = UIFactory.Text(card, offer.Title + (offer.Quantity > 1 ? "  ×" + offer.Quantity : ""), UIStyle.FontBody, UIStyle.Ink, TextAnchor.UpperLeft);
            title.rectTransform.offsetMin = new Vector2(205, 0);
            title.rectTransform.offsetMax = new Vector2(-300, -26);
            var desc = UIFactory.Text(card, offer.Description, UIStyle.FontCaption - 2, UIStyle.InkSoft, TextAnchor.UpperLeft, FontStyle.Normal);
            desc.rectTransform.offsetMin = new Vector2(205, 20);
            desc.rectTransform.offsetMax = new Vector2(-300, -84);
            var owned = UIFactory.Text(card, "", UIStyle.FontCaption, UIStyle.MintDark, TextAnchor.LowerRight);
            owned.rectTransform.offsetMin = new Vector2(0, 18);
            owned.rectTransform.offsetMax = new Vector2(-34, 0);

            ButtonHandle buy = null;
            buy = UIFactory.Button(card, offer.Price.ToString(), UIStyle.ButtonKind.Gold, () => Buy(offer, buy), new Vector2(250, 112), Icon.Coin, UIStyle.FontBody);
            UIFactory.Place(buy.Rect, new Vector2(1, 0.5f), buy.Rect.sizeDelta, new Vector2(-30, 26), new Vector2(1, 0.5f));

            System.Action refresh = () =>
            {
                owned.text = "Owned: " + S.Economy.Count(offer.Booster);
                bool afford = S.Economy.Coins >= offer.Price;
                buy.Background.color = afford ? UIStyle.ButtonFill(UIStyle.ButtonKind.Gold) : UIStyle.Disabled;
            };
            refresh();
            S.Economy.Changed += refresh;
            card.gameObject.AddComponent<OnDestroyHook>().Action = () => S.Economy.Changed -= refresh;
        }

        private void Buy(BoosterOffer offer, ButtonHandle button)
        {
            button.Button.interactable = false; // one transaction per tap
            var r = S.Economy.BuyOffer(offer.Id, "shop:" + offer.Id + ":" + System.Guid.NewGuid().ToString("N"));
            button.Button.interactable = true;
            switch (r.Status)
            {
                case TxStatus.Ok:
                    S.Audio.PlayCoin();
                    S.Haptics.Light();
                    Overlays.Toast(offer.Title + " ×" + offer.Quantity + " added!");
                    break;
                case TxStatus.InsufficientFunds:
                    S.Audio.PlayInvalid();
                    Overlays.Toast("Not enough coins — you need " + (offer.Price - S.Economy.Coins) + " more. Complete levels to earn coins!", 2.6f);
                    break;
                default:
                    Overlays.Toast("Purchase couldn't be completed (" + r.Status + ").");
                    break;
            }
        }

        private void BuildAdCard(RectTransform list, float y, float h)
        {
            var card = Card(list, y, h, out _);
            var icon = UIFactory.IconButton(card, Icon.Ad, UIStyle.Violet, 150, null);
            UIFactory.Place(icon.Rect, new Vector2(0, 0.5f), icon.Rect.sizeDelta, new Vector2(30, 0), new Vector2(0, 0.5f));
            icon.Button.interactable = false;
            var bonus = S.EconomyConfig.RewardedBonusBooster;
            var title = UIFactory.Text(card, "Free " + bonus + " booster", UIStyle.FontBody, UIStyle.Ink, TextAnchor.UpperLeft);
            title.rectTransform.offsetMin = new Vector2(205, 0);
            title.rectTransform.offsetMax = new Vector2(-300, -26);
            bool ready = S.AdGate.CanRequest(AdPlacements.BonusBooster);
            string note = S.Ads.IsMock ? "Simulated rewarded ad (MOCK — test only)." : (ready ? "Watch a short rewarded ad." : "Ads are unavailable (offline or disabled).");
            var desc = UIFactory.Text(card, note, UIStyle.FontCaption - 2, UIStyle.InkSoft, TextAnchor.UpperLeft, FontStyle.Normal);
            desc.rectTransform.offsetMin = new Vector2(205, 20);
            desc.rectTransform.offsetMax = new Vector2(-300, -84);
            ButtonHandle watch = null;
            watch = UIFactory.Button(card, ready ? "Watch" : "N/A", UIStyle.ButtonKind.Accent, () =>
            {
                watch.SetInteractable(false);
                string tx = "shop-ad:" + System.Guid.NewGuid().ToString("N");
                S.AdGate.Request(AdPlacements.BonusBooster, () =>
                {
                    if (S.Economy.GrantBooster(bonus, S.EconomyConfig.RewardedBonusBoosterQuantity, tx).Success) Overlays.Toast("Free booster added!");
                }, outcome =>
                {
                    if (outcome != AdOutcome.Completed) Overlays.Toast("No reward — the ad wasn't completed.");
                    if (watch.Rect != null) watch.SetInteractable(S.AdGate.CanRequest(AdPlacements.BonusBooster));
                }, S.EconomyConfig.RewardedCooldownSeconds);
            }, new Vector2(250, 112), null, UIStyle.FontBody);
            UIFactory.Place(watch.Rect, new Vector2(1, 0.5f), watch.Rect.sizeDelta, new Vector2(-30, 0), new Vector2(1, 0.5f));
            watch.SetInteractable(ready);
        }
    }
}
