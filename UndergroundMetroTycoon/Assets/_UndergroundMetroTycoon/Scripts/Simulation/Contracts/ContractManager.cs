using System;
using System.Collections.Generic;

namespace UndergroundMetroTycoon.Simulation
{
    /// <summary>Contract lifecycle: Locked → Available → Accepted → InProgress → ReadyForCompletion → Completed → RewardClaimed.</summary>
    public enum ContractState
    {
        Locked = 0,
        Available = 1,
        Accepted = 2,
        InProgress = 3,
        ReadyForCompletion = 4,
        Completed = 5,
        RewardClaimed = 6
    }

    public enum ContractActionResult
    {
        Ok,
        UnknownContract,
        Locked,
        AnotherContractActive,
        InvalidState,
        AlreadyClaimed
    }

    public sealed class ContractProgress
    {
        public string ContractId;
        public ContractState State;
        public int CompletionCount;

        /// <summary>Incremented every time the contract is accepted.</summary>
        public int Instance;

        /// <summary>Highest instance whose reward was paid. Guarantees one payout per accepted instance.</summary>
        public int LastClaimedInstance;
    }

    public sealed class ContractManager
    {
        private readonly List<ContractDefinition> _definitions = new List<ContractDefinition>();
        private readonly Dictionary<string, ContractDefinition> _byId = new Dictionary<string, ContractDefinition>();
        private readonly Dictionary<string, ContractProgress> _progress = new Dictionary<string, ContractProgress>();

        public ContractManager(IEnumerable<ContractDefinition> definitions)
        {
            foreach (ContractDefinition def in definitions)
            {
                if (def == null || string.IsNullOrEmpty(def.Id) || _byId.ContainsKey(def.Id)) continue;
                _definitions.Add(def);
                _byId.Add(def.Id, def);
                _progress.Add(def.Id, new ContractProgress { ContractId = def.Id, State = ContractState.Locked });
            }
        }

        public event Action<string, ContractState> StateChanged;

        public IReadOnlyList<ContractDefinition> Definitions => _definitions;

        public ContractDefinition GetDefinition(string id) => id != null && _byId.TryGetValue(id, out ContractDefinition def) ? def : null;

        public ContractProgress GetProgress(string id) => id != null && _progress.TryGetValue(id, out ContractProgress p) ? p : null;

        public ContractState GetState(string id) => GetProgress(id)?.State ?? ContractState.Locked;

        public static bool IsActiveState(ContractState state) =>
            state == ContractState.Accepted || state == ContractState.InProgress || state == ContractState.ReadyForCompletion;

        public string ActiveContractId
        {
            get
            {
                foreach (ContractDefinition def in _definitions)
                {
                    if (IsActiveState(_progress[def.Id].State)) return def.Id;
                }
                return null;
            }
        }

        public bool IsUnlocked(ContractDefinition def, int playerLevel)
        {
            if (def == null || playerLevel < def.RequiredLevel) return false;
            if (string.IsNullOrEmpty(def.PrerequisiteContractId)) return true;
            ContractProgress prerequisite = GetProgress(def.PrerequisiteContractId);
            return prerequisite != null && prerequisite.CompletionCount > 0;
        }

        public void RefreshAvailability(int playerLevel)
        {
            foreach (ContractDefinition def in _definitions)
            {
                ContractProgress p = _progress[def.Id];
                bool unlocked = IsUnlocked(def, playerLevel);
                if (p.State == ContractState.Locked && unlocked) SetState(p, ContractState.Available);
                else if (p.State == ContractState.Available && !unlocked) SetState(p, ContractState.Locked);
            }
        }

        public ContractActionResult CanAccept(string id)
        {
            ContractProgress p = GetProgress(id);
            if (p == null) return ContractActionResult.UnknownContract;
            if (ActiveContractId != null) return ContractActionResult.AnotherContractActive;
            if (p.State == ContractState.Locked) return ContractActionResult.Locked;
            return p.State == ContractState.Available ? ContractActionResult.Ok : ContractActionResult.InvalidState;
        }

        public ContractActionResult Accept(string id)
        {
            ContractActionResult result = CanAccept(id);
            if (result != ContractActionResult.Ok) return result;
            ContractProgress p = _progress[id];
            p.Instance++;
            SetState(p, ContractState.Accepted);
            return ContractActionResult.Ok;
        }

        public void MarkStarted(string id)
        {
            ContractProgress p = GetProgress(id);
            if (p != null && p.State == ContractState.Accepted) SetState(p, ContractState.InProgress);
        }

        public void ReportProgress(string id, int excavated, int required)
        {
            ContractProgress p = GetProgress(id);
            if (p == null || required <= 0 || excavated < required) return;
            if (p.State == ContractState.Accepted || p.State == ContractState.InProgress) SetState(p, ContractState.ReadyForCompletion);
        }

        /// <summary>Completes a finished contract. The caller pays the reward only when this returns Ok.</summary>
        public ContractActionResult Claim(string id)
        {
            ContractProgress p = GetProgress(id);
            if (p == null) return ContractActionResult.UnknownContract;
            if (p.State != ContractState.ReadyForCompletion) return ContractActionResult.InvalidState;
            if (p.Instance <= p.LastClaimedInstance) return ContractActionResult.AlreadyClaimed;

            p.LastClaimedInstance = p.Instance;
            p.CompletionCount++;
            SetState(p, ContractState.Completed);
            SetState(p, _byId[id].Repeatable ? ContractState.Available : ContractState.RewardClaimed);
            return ContractActionResult.Ok;
        }

        /// <summary>Puts an active contract back to Accepted (used when its site has to be regenerated on load).</summary>
        public void ResetToAccepted(string id)
        {
            ContractProgress p = GetProgress(id);
            if (p != null && IsActiveState(p.State)) SetState(p, ContractState.Accepted);
        }

        public List<ContractSaveData> CreateSaveData()
        {
            var list = new List<ContractSaveData>();
            foreach (ContractDefinition def in _definitions)
            {
                ContractProgress p = _progress[def.Id];
                list.Add(new ContractSaveData
                {
                    Id = p.ContractId,
                    State = (int)p.State,
                    CompletionCount = p.CompletionCount,
                    Instance = p.Instance,
                    LastClaimedInstance = p.LastClaimedInstance
                });
            }
            return list;
        }

        /// <summary>Restores saved progress, sanitising invalid values. Unknown ids are ignored.</summary>
        public void Restore(IEnumerable<ContractSaveData> saved)
        {
            if (saved == null) return;
            bool hasActive = false;
            foreach (ContractSaveData s in saved)
            {
                if (s == null || !_progress.TryGetValue(s.Id ?? string.Empty, out ContractProgress p)) continue;
                var state = (ContractState)Math.Max(0, Math.Min((int)ContractState.RewardClaimed, s.State));
                p.CompletionCount = Math.Max(0, s.CompletionCount);
                p.Instance = Math.Max(0, s.Instance);
                p.LastClaimedInstance = Math.Max(0, Math.Min(p.Instance, s.LastClaimedInstance));

                if (IsActiveState(state))
                {
                    if (hasActive || p.Instance <= p.LastClaimedInstance) state = ContractState.Available;
                    else hasActive = true;
                }
                if (state == ContractState.Completed) state = _byId[s.Id].Repeatable ? ContractState.Available : ContractState.RewardClaimed;
                if (state == ContractState.RewardClaimed && (_byId[s.Id].Repeatable || p.CompletionCount == 0)) state = ContractState.Available;
                p.State = state;
            }
        }

        private void SetState(ContractProgress p, ContractState state)
        {
            if (p.State == state) return;
            p.State = state;
            StateChanged?.Invoke(p.ContractId, state);
        }
    }
}
