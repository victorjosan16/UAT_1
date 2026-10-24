using UnityEngine;
using UnityEngine.UI;

namespace QuarryEmpire
{
    /// <summary>Butonul „COBOARĂ”, vizibil doar cât jucătorul conduce un utilaj (stânga jos).</summary>
    public class VehicleHud : MonoBehaviour
    {
        private GameObject _button;

        private void Awake()
        {
            UiTheme theme = UiRoot.Theme;
            Button button = UiFactory.CreateButton("ExitVehicle", transform, Loc.T("COBOARĂ"), theme.NegativeColor, OnExitClicked, 40);
            UiFactory.Place((RectTransform)button.transform, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(30f, 250f), new Vector2(280f, 116f));
            _button = button.gameObject;
            _button.SetActive(false);
        }

        private void OnEnable()
        {
            GameEvents.VehicleEntered += OnEntered;
            GameEvents.VehicleExited += OnExited;
        }

        private void OnDisable()
        {
            GameEvents.VehicleEntered -= OnEntered;
            GameEvents.VehicleExited -= OnExited;
        }

        private void OnEntered(Vehicle vehicle)
        {
            _button.SetActive(true);
            GameEvents.RaiseToast(Loc.F("Conduci: {0}", Loc.T(vehicle.Definition.DisplayName)));
        }

        private void OnExited(Vehicle _) => _button.SetActive(false);

        private static void OnExitClicked()
        {
            if (PlayerRoot.Instance != null) PlayerRoot.Instance.Driver.Exit();
        }
    }
}
