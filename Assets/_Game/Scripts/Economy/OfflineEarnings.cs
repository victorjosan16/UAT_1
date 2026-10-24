using System;
using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Progres idle: calculează cât au „muncit” muncitorii cât timp jocul a fost închis
    /// și oferă suma la pornire (cu opțiune de dublare prin reclamă).
    /// </summary>
    public class OfflineEarnings : MonoBehaviour
    {
        public static OfflineEarnings Instance { get; private set; }

        [SerializeField] private float maxOfflineHours = 2f;
        [SerializeField] private long minimumToShow = 10;

        public long Pending { get; private set; }

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable() => GameEvents.GameStarted += OnGameStarted;
        private void OnDisable() => GameEvents.GameStarted -= OnGameStarted;

        private void Start()
        {
            // Start rulează după ce zonele de cumpărare au activat muncitorii deblocați (în Awake).
            long previous = SaveSystem.Instance != null ? SaveSystem.Instance.PreviousSaveUnixSeconds : 0;
            if (previous <= 0) return;

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            double seconds = Math.Clamp(now - previous, 0, maxOfflineHours * 3600.0);
            float incomePerMinute = 0f;
            foreach (WorkerAI worker in WorkerAI.Active) incomePerMinute += worker.OfflineIncomePerMinute;
            Pending = (long)(incomePerMinute * seconds / 60.0);
        }

        private void OnGameStarted()
        {
            if (Pending >= minimumToShow) GameEvents.RaiseOfflineEarningsReady(Pending);
        }

        public void Claim(bool doubled)
        {
            if (Pending <= 0 || MoneyManager.Instance == null) return;
            MoneyManager.Instance.Add(doubled ? Pending * 2 : Pending);
            GameEvents.RaiseMoneyEarned(Pending, PlayerRoot.Instance != null ? PlayerRoot.Instance.Position : Vector3.zero, true);
            Pending = 0;
        }
    }
}
