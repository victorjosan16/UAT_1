using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Bază pentru zonele care se plătesc stând pe ele: banii se scad treptat, cu monede care zboară
    /// de la jucător spre zonă. Progresul plății parțiale se salvează.
    /// Clasele derivate spun doar cât costă și ce se întâmplă la final.
    /// </summary>
    public abstract class PayZone : MonoBehaviour
    {
        [SerializeField] protected ZoneArea area;
        [SerializeField] protected WorldLabel label;
        [Tooltip("Câte secunde stai pe zonă până începe plata (ca să nu plătești din greșeală).")]
        [SerializeField] private float startDelay = 0.35f;
        [Tooltip("În câte secunde se plătește tot prețul, dacă ai destui bani.")]
        [SerializeField] private float payDuration = 1.6f;
        [SerializeField] private float coinInterval = 0.06f;

        protected long Paid { get; private set; }

        protected abstract string SaveKey { get; }
        protected abstract string Title { get; }
        protected abstract long Cost { get; }
        protected virtual bool IsAvailable => true;
        protected virtual string UnavailableText => "MAX";
        protected abstract void OnFullyPaid();

        private float _insideTime;
        private float _coinTimer;
        private bool _warnedNoMoney;

        protected virtual void Start()
        {
            if (SaveSystem.Instance != null) Paid = SaveData.GetInt(SaveSystem.Instance.Data.zonePaid, SaveKey);
            RefreshLabel();
        }

        protected virtual void Update()
        {
            if (!IsAvailable) return;

            PlayerRoot player = PlayerRoot.Instance;
            MoneyManager money = MoneyManager.Instance;
            bool inside = player != null && money != null && area != null && area.Contains(player.Position);
            if (!inside)
            {
                _insideTime = 0f;
                _warnedNoMoney = false;
                return;
            }

            float dt = Time.deltaTime;
            _insideTime += dt;
            if (_insideTime < startDelay) return;

            long cost = Cost;
            long remaining = cost - Paid;
            if (remaining <= 0)
            {
                Complete();
                return;
            }

            long step = (long)Mathf.Ceil(cost * dt / payDuration);
            step = System.Math.Min(System.Math.Max(1, step), remaining);
            long spent = money.SpendUpTo(step);
            if (spent <= 0)
            {
                if (!_warnedNoMoney)
                {
                    _warnedNoMoney = true;
                    GameEvents.RaiseToast("Nu ai destui bani");
                }
                return;
            }

            Paid += spent;
            SavePaid();
            SpawnCoin(player.Position, dt);

            if (Paid >= cost) Complete();
            else RefreshLabel();
        }

        private void SpawnCoin(Vector3 playerPosition, float dt)
        {
            _coinTimer -= dt;
            if (_coinTimer > 0f) return;
            _coinTimer = coinInterval;
            Vector3 target = area.Center + Vector3.up * 0.2f;
            EffectsLibrary.FlyCoin(playerPosition + Vector3.up * 1.2f, target);
            GameEvents.RaiseCoinPaid(target);
        }

        private void Complete()
        {
            Paid = 0;
            SavePaid();
            OnFullyPaid();
            RefreshLabel();
        }

        private void SavePaid()
        {
            if (SaveSystem.Instance == null) return;
            SaveData.SetInt(SaveSystem.Instance.Data.zonePaid, SaveKey, (int)System.Math.Min(Paid, int.MaxValue));
            SaveSystem.Instance.MarkDirty();
        }

        protected void RefreshLabel()
        {
            if (label == null) return;
            if (!IsAvailable)
            {
                label.SetText($"{Title}\n{UnavailableText}");
                label.SetFill(1f);
                return;
            }
            long cost = Cost;
            label.SetText($"{Title}\n{NumberFormat.Money(cost - Paid)}");
            label.SetFill(cost > 0 ? (float)Paid / cost : 0f);
        }
    }
}
