using System;
using System.Collections.Generic;

namespace TollRoadTycoon.Economy
{
    public enum MoneySource
    {
        Toll,
        Reward,
        Offline,
        Passive,
        Service,
        Ad,
        Debug,
    }

    /// <summary>
    /// Single owner of the player's balance. All money flows through AddMoney / TrySpendMoney,
    /// the balance can never go negative and every purchase is all-or-nothing.
    /// </summary>
    public sealed class EconomyManager
    {
        private struct Earning
        {
            public double Time;
            public long Cents;
        }

        private readonly Queue<Earning> _window = new Queue<Earning>();
        private readonly double _windowSeconds;
        private long _windowSum;
        private double _now;
        private double _trackedSince;

        public long Balance { get; private set; }
        public long TotalEarnings { get; private set; }
        public long TotalExpenses { get; private set; }

        public event Action<long> BalanceChanged;
        public event Action<long, MoneySource> MoneyEarned;
        public event Action<long, string> MoneySpent;

        public EconomyManager(long startingBalance, double incomeWindowSeconds = 60)
        {
            if (startingBalance < 0) throw new ArgumentOutOfRangeException(nameof(startingBalance));
            Balance = startingBalance;
            _windowSeconds = incomeWindowSeconds;
        }

        public long GetBalance() => Balance;
        public long GetTotalEarnings() => TotalEarnings;
        public long GetTotalExpenses() => TotalExpenses;

        public bool CanAfford(long cents) => cents >= 0 && Balance >= cents;

        public void AddMoney(long cents, MoneySource source)
        {
            if (cents <= 0) return;
            Balance = SaturatingAdd(Balance, cents);
            TotalEarnings = SaturatingAdd(TotalEarnings, cents);
            if (source == MoneySource.Toll)
            {
                _window.Enqueue(new Earning { Time = _now, Cents = cents });
                _windowSum = SaturatingAdd(_windowSum, cents);
            }
            MoneyEarned?.Invoke(cents, source);
            BalanceChanged?.Invoke(Balance);
        }

        /// <summary>Spends only if the full amount is available. Returns false and changes nothing otherwise.</summary>
        public bool TrySpendMoney(long cents, string reason)
        {
            if (cents < 0) return false;
            if (Balance < cents) return false;
            if (cents == 0) return true;
            Balance -= cents;
            TotalExpenses = SaturatingAdd(TotalExpenses, cents);
            MoneySpent?.Invoke(cents, reason);
            BalanceChanged?.Invoke(Balance);
            return true;
        }

        /// <summary>Advances the clock used by the rolling income window.</summary>
        public void Tick(double now)
        {
            _now = now;
            while (_window.Count > 0 && _window.Peek().Time < _now - _windowSeconds)
            {
                _windowSum -= _window.Dequeue().Cents;
            }
        }

        /// <summary>
        /// Toll income per minute measured from real payments in the last window.
        /// During the first window it is extrapolated from the time actually observed.
        /// </summary>
        public long GetIncomePerMinute()
        {
            double observed = Math.Min(_windowSeconds, _now - _trackedSince);
            if (observed < 5) observed = 5;
            return (long)Math.Round(_windowSum * (60.0 / observed));
        }

        public void ResetIncomeWindow(double now)
        {
            _window.Clear();
            _windowSum = 0;
            _now = now;
            _trackedSince = now;
        }

        internal void RestoreTotals(long balance, long totalEarnings, long totalExpenses)
        {
            Balance = Math.Max(0, balance);
            TotalEarnings = Math.Max(0, totalEarnings);
            TotalExpenses = Math.Max(0, totalExpenses);
            BalanceChanged?.Invoke(Balance);
        }

        private static long SaturatingAdd(long a, long b)
        {
            long r = a + b;
            return (b > 0 && r < a) ? long.MaxValue : r;
        }
    }
}
