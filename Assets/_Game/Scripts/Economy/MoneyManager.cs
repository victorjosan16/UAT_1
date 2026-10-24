using System;
using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Banii jucătorului: câștiguri, cheltuieli și bonusul temporar „x2” (din reclame).
    /// Valoarea se păstrează în salvare.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class MoneyManager : MonoBehaviour
    {
        public static MoneyManager Instance { get; private set; }

        [Tooltip("Bani de pornire la un joc nou.")]
        [SerializeField] private long startingMoney = 0;

        public long Money { get; private set; }
        public float BoostTimeLeft { get; private set; }
        public float IncomeMultiplier => BoostTimeLeft > 0f ? 2f : 1f;

        public event Action<long> MoneyChanged;

        private void Awake()
        {
            Instance = this;
            SaveData data = SaveSystem.Instance != null ? SaveSystem.Instance.Data : null;
            bool newGame = data == null || (data.money == 0 && data.lastSaveUnixSeconds == 0);
            Money = newGame ? startingMoney : data.money;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (BoostTimeLeft > 0f) BoostTimeLeft = Mathf.Max(0f, BoostTimeLeft - Time.deltaTime);
        }

        /// <summary>Câștig din joc (vânzări, comenzi): se aplică bonusul x2 dacă e activ.</summary>
        public long AddIncome(long baseAmount, Vector3 worldPosition, bool animate)
        {
            if (baseAmount <= 0) return 0;
            long amount = (long)Math.Round(baseAmount * (double)IncomeMultiplier);
            Add(amount);
            GameEvents.RaiseMoneyEarned(amount, worldPosition, animate);
            return amount;
        }

        /// <summary>Adaugă bani fără bonus (recompense fixe, câștiguri offline).</summary>
        public void Add(long amount)
        {
            if (amount <= 0) return;
            Money += amount;
            Persist();
        }

        public bool CanAfford(long amount) => Money >= amount;

        public bool TrySpend(long amount)
        {
            if (amount < 0 || Money < amount) return false;
            Money -= amount;
            Persist();
            return true;
        }

        /// <summary>Cheltuie cât se poate, până la „amount”. Întoarce suma cheltuită.</summary>
        public long SpendUpTo(long amount)
        {
            long spent = Math.Min(Math.Max(0, amount), Money);
            if (spent <= 0) return 0;
            Money -= spent;
            Persist();
            return spent;
        }

        /// <summary>Pornește (sau prelungește) bonusul „bani x2”.</summary>
        public void StartBoost(float seconds) => BoostTimeLeft = Mathf.Max(BoostTimeLeft, 0f) + seconds;

        private void Persist()
        {
            if (SaveSystem.Instance != null)
            {
                SaveSystem.Instance.Data.money = Money;
                SaveSystem.Instance.MarkDirty();
            }
            MoneyChanged?.Invoke(Money);
        }
    }
}
