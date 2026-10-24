using UnityEngine;
using UnityEngine.UI;

namespace QuarryEmpire
{
    /// <summary>Fereastra „Cât ai lipsit, muncitorii au câștigat $X” (cu opțiune x2 prin reclamă).</summary>
    public class OfflinePopup : MonoBehaviour
    {
        private GameObject _root;
        private UiLabel _message;
        private UiLabel _claimLabel;
        private UiLabel _doubleLabel;

        private void Awake()
        {
            UiTheme theme = UiRoot.Theme;
            Image dim = UiFactory.CreateImage("OfflineDim", transform, UiFactory.WhiteSprite, new Color(0f, 0f, 0f, 0.6f));
            dim.preserveAspect = false;
            dim.raycastTarget = true;
            UiFactory.Stretch(dim.rectTransform, -200f);
            _root = dim.gameObject;

            Image box = UiFactory.CreatePanel("OfflineBox", dim.transform, theme.PanelLightColor);
            var boxRect = UiFactory.Place((RectTransform)box.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(840f, 720f));
            box.gameObject.AddComponent<CanvasGroup>();
            box.gameObject.AddComponent<PopIn>();

            Image coin = UiFactory.CreateImage("Coin", boxRect, ProceduralSprites.Coin, Color.white);
            UiFactory.Place(coin.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(150f, 150f));

            _message = UiFactory.CreateText("Message", boxRect, "", 46, TextAnchor.MiddleCenter, theme.TextDarkColor);
            UiFactory.Place(_message.RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(760f, 260f));

            Button claim = UiFactory.CreateButton("Claim", boxRect, "", theme.PositiveColor, () => Claim(false), 40);
            UiFactory.Place((RectTransform)claim.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 190f), new Vector2(640f, 124f));
            _claimLabel = UiFactory.GetButtonLabel(claim);

            Button doubled = UiFactory.CreateButton("Double", boxRect, "", theme.AdColor, ClaimDouble, 40);
            UiFactory.Place((RectTransform)doubled.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 46f), new Vector2(640f, 124f));
            _doubleLabel = UiFactory.GetButtonLabel(doubled);

            _root.SetActive(false);
        }

        private void OnEnable() => GameEvents.OfflineEarningsReady += Show;
        private void OnDisable() => GameEvents.OfflineEarningsReady -= Show;

        private void Show(long amount)
        {
            _message.Text = Loc.F("Bine ai revenit!\nCât ai lipsit, muncitorii au câștigat\n{0}", NumberFormat.Money(amount));
            _claimLabel.Text = Loc.F("IA {0}", NumberFormat.Money(amount));
            _doubleLabel.Text = Loc.F("IA {0} (reclamă)", NumberFormat.Money(amount * 2));
            _root.SetActive(true);
        }

        private void Claim(bool doubled)
        {
            _root.SetActive(false);
            if (OfflineEarnings.Instance != null) OfflineEarnings.Instance.Claim(doubled);
        }

        private void ClaimDouble()
        {
            if (AdService.Instance == null)
            {
                Claim(false);
                return;
            }
            AdService.Instance.ShowRewarded("offline_x2", rewarded => Claim(rewarded));
        }
    }
}
