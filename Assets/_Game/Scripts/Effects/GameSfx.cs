using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>Leagă evenimentele jocului de sunete. Nu conține logică de joc.</summary>
    public class GameSfx : MonoBehaviour
    {
        [Tooltip("Sunetele blocurilor se aud doar dacă sunt la această distanță de jucător.")]
        [SerializeField] private float hearingDistance = 9f;

        private void OnEnable()
        {
            GameEvents.BlockHit += OnBlockHit;
            GameEvents.BlockDestroyed += OnBlockDestroyed;
            GameEvents.ResourceCollected += OnResourceCollected;
            GameEvents.MoneyEarned += OnMoneyEarned;
            GameEvents.CoinPaid += OnCoinPaid;
            GameEvents.Purchased += OnPurchased;
            GameEvents.Upgraded += OnUpgraded;
            GameEvents.LayerCleared += OnLayerCleared;
            GameEvents.OrderCompleted += OnOrderCompleted;
            GameEvents.OrderFailed += OnOrderFailed;
            GameEvents.StackFull += OnStackFull;
        }

        private void OnDisable()
        {
            GameEvents.BlockHit -= OnBlockHit;
            GameEvents.BlockDestroyed -= OnBlockDestroyed;
            GameEvents.ResourceCollected -= OnResourceCollected;
            GameEvents.MoneyEarned -= OnMoneyEarned;
            GameEvents.CoinPaid -= OnCoinPaid;
            GameEvents.Purchased -= OnPurchased;
            GameEvents.Upgraded -= OnUpgraded;
            GameEvents.LayerCleared -= OnLayerCleared;
            GameEvents.OrderCompleted -= OnOrderCompleted;
            GameEvents.OrderFailed -= OnOrderFailed;
            GameEvents.StackFull -= OnStackFull;
        }

        private static void Play(AudioClip clip, float volume = 1f, float minInterval = 0.03f)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.Play(clip, volume, 0.06f, minInterval);
        }

        private bool IsNearPlayer(Vector3 position)
        {
            PlayerRoot player = PlayerRoot.Instance;
            return player == null || (player.Position - position).sqrMagnitude <= hearingDistance * hearingDistance;
        }

        private void OnBlockHit(MineBlock block)
        {
            if (IsNearPlayer(block.transform.position)) Play(ProceduralSfx.Hit, 0.8f, 0.04f);
        }

        private void OnBlockDestroyed(MineBlock block)
        {
            if (IsNearPlayer(block.transform.position)) Play(ProceduralSfx.Break, 0.9f, 0.05f);
        }

        private void OnResourceCollected(ResourceData _, Vector3 position)
        {
            if (IsNearPlayer(position)) Play(ProceduralSfx.Pop, 0.5f, 0.04f);
        }

        private void OnMoneyEarned(long _, Vector3 __, bool animate)
        {
            if (animate) Play(ProceduralSfx.Coin, 0.6f, 0.06f);
        }

        private void OnCoinPaid(Vector3 _) => Play(ProceduralSfx.Pay, 0.5f, 0.05f);
        private void OnPurchased(string _, string __) => Play(ProceduralSfx.Purchase);
        private void OnUpgraded(UpgradeDefinition _, int __) => Play(ProceduralSfx.Upgrade);
        private void OnLayerCleared(int _) => Play(ProceduralSfx.LayerClear);
        private void OnOrderCompleted(long _) => Play(ProceduralSfx.Purchase);
        private void OnOrderFailed() => Play(ProceduralSfx.Error);

        private void OnStackFull(ResourceStack stack)
        {
            if (PlayerRoot.Instance != null && stack == PlayerRoot.Instance.Stack) Play(ProceduralSfx.Error, 0.6f, 1.5f);
        }
    }
}
