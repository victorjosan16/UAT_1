using UnityEngine;
using UnityEngine.UI;

namespace QuarryEmpire
{
    /// <summary>Butonul „COBOARĂ”, vizibil doar cât jucătorul conduce un utilaj.</summary>
    public class VehicleHud : MonoBehaviour
    {
        private GameObject _button;

        private void Awake()
        {
            UiTheme theme = UiRoot.Theme;
            Button button = UiFactory.CreateButton("ExitVehicle", transform, "COBOARĂ", theme.NegativeColor, OnExitClicked, 40);
            UiFactory.Place((RectTransform)button.transform, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-40f, 300f), new Vector2(300f, 120f));
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
            GameEvents.RaiseToast($"Conduci: {vehicle.Definition.DisplayName}");
        }

        private void OnExited(Vehicle _) => _button.SetActive(false);

        private static void OnExitClicked()
        {
            if (PlayerRoot.Instance != null) PlayerRoot.Instance.Driver.Exit();
        }
    }
}
