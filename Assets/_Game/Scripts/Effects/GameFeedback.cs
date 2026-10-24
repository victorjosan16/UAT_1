using Unity.Cinemachine;
using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// „Senzația” jocului: tremurat de cameră și vibrații la momentele importante.
    /// </summary>
    public class GameFeedback : MonoBehaviour
    {
        [SerializeField] private CinemachineImpulseSource impulseSource;
        [Tooltip("Efectele pentru blocuri apar doar dacă blocul e aproape de jucător.")]
        [SerializeField] private float nearDistance = 4f;
        [SerializeField] private float blockShake = 0.15f;
        [SerializeField] private float purchaseShake = 0.35f;
        [SerializeField] private float layerShake = 1f;

        private void OnEnable()
        {
            GameEvents.BlockDestroyed += OnBlockDestroyed;
            GameEvents.Purchased += OnPurchased;
            GameEvents.Upgraded += OnUpgraded;
            GameEvents.LayerCleared += OnLayerCleared;
            GameEvents.OrderCompleted += OnOrderCompleted;
        }

        private void OnDisable()
        {
            GameEvents.BlockDestroyed -= OnBlockDestroyed;
            GameEvents.Purchased -= OnPurchased;
            GameEvents.Upgraded -= OnUpgraded;
            GameEvents.LayerCleared -= OnLayerCleared;
            GameEvents.OrderCompleted -= OnOrderCompleted;
        }

        private void Shake(float force)
        {
            if (impulseSource != null) impulseSource.GenerateImpulseWithForce(force);
        }

        private void OnBlockDestroyed(MineBlock block)
        {
            PlayerRoot player = PlayerRoot.Instance;
            if (player == null || (player.Position - block.transform.position).sqrMagnitude > nearDistance * nearDistance) return;
            Shake(blockShake);
            Haptics.Light();
        }

        private void OnPurchased(string _, string __)
        {
            Shake(purchaseShake);
            Haptics.Medium();
        }

        private void OnUpgraded(UpgradeDefinition _, int __) => Haptics.Medium();

        private void OnLayerCleared(int _)
        {
            Shake(layerShake);
            Haptics.Heavy();
        }

        private void OnOrderCompleted(long _) => Haptics.Medium();
    }
}
