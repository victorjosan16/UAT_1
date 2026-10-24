using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Punct central de acces la jucător pentru zone și UI (în loc de Find()).
    /// Ține referințele la componentele principale ale jucătorului.
    /// </summary>
    [DefaultExecutionOrder(-300)]
    public class PlayerRoot : MonoBehaviour
    {
        public static PlayerRoot Instance { get; private set; }

        [SerializeField] private ResourceStack stack;
        [SerializeField] private PlayerVehicleDriver driver;
        [SerializeField] private PlayerStatsRuntime stats;

        public ResourceStack Stack => stack;
        public PlayerVehicleDriver Driver => driver;
        public PlayerStatsRuntime Stats => stats;
        public Vector3 Position => transform.position;

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
