using System.Collections.Generic;
using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Zona SELL: orice stivă care intră aici (jucător, muncitor, camion) e vândută automat,
    /// bucată cu bucată. Resursele zboară spre zonă, iar banii spre UI.
    /// </summary>
    public class SellZone : MonoBehaviour
    {
        private static readonly List<SellZone> AllZones = new();

        /// <summary>Prima zonă de vânzare din scenă (folosită de muncitori și camion).</summary>
        public static SellZone Main => AllZones.Count > 0 ? AllZones[0] : null;

        [SerializeField] private ZoneArea area;
        [Tooltip("Unde zboară resursele vândute.")]
        [SerializeField] private Transform collectPoint;
        [Tooltip("Secunde între două bucăți vândute din aceeași stivă.")]
        [SerializeField] private float sellInterval = 0.05f;

        private readonly Dictionary<ResourceStack, float> _timers = new();
        private readonly List<ResourceStack> _buffer = new();

        public ZoneArea Area => area;
        public Vector3 Position => area != null ? area.Center : transform.position;

        private void OnEnable() => AllZones.Add(this);
        private void OnDisable() => AllZones.Remove(this);

        private void Update()
        {
            // Copiem lista: vânzarea poate modifica stivele active.
            _buffer.Clear();
            _buffer.AddRange(ResourceStack.Active);

            foreach (ResourceStack stack in _buffer)
            {
                if (stack == null || stack.IsEmpty || !area.Contains(stack.transform.position)) continue;

                _timers.TryGetValue(stack, out float timer);
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    timer = sellInterval;
                    SellOne(stack);
                }
                _timers[stack] = timer;
            }
        }

        private void SellOne(ResourceStack stack)
        {
            if (!stack.TryTakeLast(null, out ResourceData data, out Vector3 from)) return;

            Vector3 target = collectPoint != null ? collectPoint.position : Position;
            long price = data.SellPrice;
            bool animate = stack.AnimateMoneyOnSell;
            EffectsLibrary.FlyItemTo(data.PickupMaterial, from, target, () =>
            {
                if (MoneyManager.Instance != null) MoneyManager.Instance.AddIncome(price, target, animate);
            }, 0.3f, 0.8f);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => AllZones.Clear();
    }
}
