using UnityEngine;
using UnityEngine.UI;

namespace QuarryEmpire
{
    /// <summary>Fereastra „Cât ai lipsit, muncitorii au câștigat $X” (cu opțiune x2 prin reclamă).</summary>
    public class OfflinePopup : MonoBehaviour
    {
        private GameObject _root;
        private Text _message;
        private Text _claimLabel;
        private Text _doubleLabel;

        private void Awake()
        {
            UiTheme theme = UiRoot.Theme;
            Image dim = UiFactory.CreatePanel("OfflineDim", transform, new Color(0f, 0f, 0f, 0.7f));
            UiFactory.Stretch((RectTransform)dim.transform);
            _root = dim.gameObject;

            Image box = UiFactory.CreatePanel("OfflineBox", dim.transform, theme.PanelColor);
            var boxRect = UiFactory.Place((RectTransform)box.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820f, 640f));

            _message = UiFactory.CreateText("Message", boxRect, "", 46);
            UiFactory.Place((RectTransform)_message.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(740f, 260f));

            Button claim = UiFactory.CreateButton("Claim", boxRect, "", theme.PositiveColor, () => Claim(false), 40);
            UiFactory.Place((RectTransform)claim.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 190f), new Vector2(620f, 120f));
            _claimLabel = UiFactory.GetButtonLabel(claim);

            Button doubled = UiFactory.CreateButton("Double", boxRect, "", theme.AdColor, ClaimDouble, 40);
            UiFactory.Place((RectTransform)doubled.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(620f, 120f));
            _doubleLabel = UiFactory.GetButtonLabel(doubled);

            _root.SetActive(false);
        }

        private void OnEnable() => GameEvents.OfflineEarningsReady += Show;
        private void OnDisable() => GameEvents.OfflineEarningsReady -= Show;

        private void Show(long amount)
        {
            _message.text = $"Bine ai revenit!\nCât ai lipsit, muncitorii au câștigat\n{NumberFormat.Money(amount)}";
            _claimLabel.text = $"IA {NumberFormat.Money(amount)}";
            _doubleLabel.text = $"IA {NumberFormat.Money(amount * 2)} (reclamă)";
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
