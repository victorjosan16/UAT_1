using System;
using System.Collections.Generic;

namespace UndergroundMetroTycoon.Simulation
{
    public struct PlayerPose
    {
        public bool IsSet;
        public float X;
        public float Y;
        public float Z;
        public float Yaw;
    }

    /// <summary>
    /// Authoritative game state for one play session. Owns the economy, progression, contracts and the
    /// excavation site, and is the only place rewards are paid. Contains no Unity code.
    /// </summary>
    public sealed class GameSession
    {
        public const string PlayerActorId = "player";

        private GameSession(GameContent content, long coins, int totalXp, SettingsData settings)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
            Wallet = new Wallet(coins);
            Progression = new PlayerProgression(content.Progression, totalXp);
            Contracts = new ContractManager(content.Contracts);
            Excavation = new ExcavationService();
            Settings = settings ?? new SettingsData();
            EquippedTool = content.GetTool(content.StartingToolId);

            Wallet.Changed += (_, __) => MarkDirty();
            Contracts.StateChanged += OnContractStateChanged;
        }

        public GameContent Content { get; }
        public Wallet Wallet { get; }
        public PlayerProgression Progression { get; }
        public ContractManager Contracts { get; }
        public ExcavationService Excavation { get; }
        public SettingsData Settings { get; }
        public ToolDefinition EquippedTool { get; private set; }
        public ExcavationGrid Grid => Excavation.Grid;
        public PlayerPose PlayerPose { get; set; }
        public bool IsDirty { get; private set; }

        public event Action<ExcavationGrid> SiteRebuilt;
        public event Action<TerrainCell, int> CellDamaged;
        /// <summary>Cell removed; args are the cell, coins paid and XP granted.</summary>
        public event Action<TerrainCell, long, int> CellExcavated;
        public event Action<string, ContractState> ContractStateChanged;
        /// <summary>Contract reward paid; args are the contract, coins and XP.</summary>
        public event Action<ContractDefinition, long, int> ContractRewardClaimed;
        /// <summary>Level changed; args are previous and new level.</summary>
        public event Action<int, int> LevelChanged;

        public static GameSession CreateNew(GameContent content)
        {
            var session = new GameSession(content, content.StartingCoins, 0, new SettingsData());
            session.Contracts.RefreshAvailability(session.Progression.Level);
            session.BuildSite(session.FindPreviewContract());
            session.SyncActiveContract();
            session.IsDirty = true;
            return session;
        }

        /// <summary>Rebuilds a session from save data, sanitising anything inconsistent. Warnings describe repairs made.</summary>
        public static GameSession Restore(GameContent content, SaveData data, List<string> warnings)
        {
            if (data == null) return CreateNew(content);
            warnings = warnings ?? new List<string>();

            var session = new GameSession(content, Math.Max(0, data.Coins), Math.Max(0, data.TotalXp), data.Settings);
            if (data.Coins < 0) warnings.Add("Negative coin balance reset to 0.");

            ToolDefinition tool = content.GetTool(data.EquippedToolId);
            if (tool != null) session.EquippedTool = tool;

            session.Contracts.Restore(data.Contracts);
            session.Contracts.RefreshAvailability(session.Progression.Level);

            string active = session.Contracts.ActiveContractId;
            ContractDefinition siteContract = content.GetContract(data.SiteContractId);
            if (active != null && siteContract != null && siteContract.Id != active)
            {
                warnings.Add("Site layout did not match the active contract; site regenerated.");
                siteContract = null;
                session.Contracts.ResetToAccepted(active);
            }

            if (siteContract == null)
            {
                if (active != null) session.Contracts.ResetToAccepted(active);
                session.BuildSite(content.GetContract(active) ?? session.FindPreviewContract());
            }
            else
            {
                session.BuildSite(siteContract);
                ExcavationGrid grid = session.Grid;
                if (data.SiteCellHp != null && data.SiteCellHp.Count == grid.CellCount)
                {
                    for (int i = 0; i < grid.CellCount; i++) grid.GetCell(i).RestoreHp(data.SiteCellHp[i]);
                    grid.Recount();
                }
                else
                {
                    warnings.Add("Saved excavation data did not match the site; site regenerated.");
                    if (active != null) session.Contracts.ResetToAccepted(active);
                }
            }

            if (data.HasPlayerPose && IsFinite(data.PlayerX) && IsFinite(data.PlayerY) && IsFinite(data.PlayerZ) && IsFinite(data.PlayerYaw))
            {
                session.PlayerPose = new PlayerPose { IsSet = true, X = data.PlayerX, Y = data.PlayerY, Z = data.PlayerZ, Yaw = data.PlayerYaw };
            }

            session.SyncActiveContract();
            if (active != null) session.Contracts.ReportProgress(active, session.Grid.ExcavatedContractCellCount, session.Grid.ContractCellCount);
            session.IsDirty = false;
            return session;
        }

        public ContractActionResult AcceptContract(string contractId)
        {
            ContractActionResult result = Contracts.Accept(contractId);
            if (result != ContractActionResult.Ok) return result;

            ContractDefinition def = Contracts.GetDefinition(contractId);
            if (Grid == null || Grid.SiteContractId != contractId || Grid.ExcavatedContractCellCount > 0) BuildSite(def);
            SyncActiveContract();
            MarkDirty();
            return ContractActionResult.Ok;
        }

        /// <summary>One tool impact on a cell. Pays the cell's coins and XP exactly once, when it is removed.</summary>
        public DigResult Dig(int cellIndex, string actorId)
        {
            SyncActiveContract();
            DigResult result = Excavation.Dig(cellIndex, EquippedTool, actorId);
            if (!result.Succeeded) return result;

            string active = Excavation.ActiveContractId;
            TerrainCell cell = Grid.GetCell(cellIndex);
            Contracts.MarkStarted(active);
            CellDamaged?.Invoke(cell, result.Damage);

            if (result.Outcome == DigOutcome.Excavated)
            {
                Wallet.Credit(cell.CoinValue);
                AddXp(cell.XpValue);
                CellExcavated?.Invoke(cell, cell.CoinValue, cell.XpValue);
                Contracts.ReportProgress(active, Grid.ExcavatedContractCellCount, Grid.ContractCellCount);
            }
            MarkDirty();
            return result;
        }

        public ContractActionResult ClaimContract(string contractId)
        {
            ContractDefinition def = Contracts.GetDefinition(contractId);
            ContractActionResult result = Contracts.Claim(contractId);
            if (result != ContractActionResult.Ok) return result;

            Wallet.Credit(def.CoinReward);
            AddXp(def.XpReward);
            // Completing a contract can unlock contracts that list it as a prerequisite.
            Contracts.RefreshAvailability(Progression.Level);
            SyncActiveContract();
            ContractRewardClaimed?.Invoke(def, def.CoinReward, def.XpReward);
            MarkDirty();
            return ContractActionResult.Ok;
        }

        public SaveData CreateSaveData(long utcTicks)
        {
            var data = new SaveData
            {
                SavedAtUtcTicks = utcTicks,
                Coins = Wallet.Coins,
                TotalXp = Progression.TotalXp,
                EquippedToolId = EquippedTool?.Id,
                Contracts = Contracts.CreateSaveData(),
                SiteContractId = Grid?.SiteContractId,
                Settings = new SettingsData { SoundEnabled = Settings.SoundEnabled, GraphicsQuality = Settings.GraphicsQuality }
            };
            PlayerPose pose = PlayerPose;
            if (pose.IsSet)
            {
                data.HasPlayerPose = true;
                data.PlayerX = pose.X;
                data.PlayerY = pose.Y;
                data.PlayerZ = pose.Z;
                data.PlayerYaw = pose.Yaw;
            }
            if (Grid != null)
            {
                for (int i = 0; i < Grid.CellCount; i++) data.SiteCellHp.Add(Grid.GetCell(i).CurrentHp);
            }
            return data;
        }

        public void MarkDirty() => IsDirty = true;
        public void ClearDirty() => IsDirty = false;

        private void AddXp(int amount)
        {
            int before = Progression.Level;
            if (Progression.AddXp(amount) <= 0) return;
            Contracts.RefreshAvailability(Progression.Level);
            LevelChanged?.Invoke(before, Progression.Level);
        }

        private void BuildSite(ContractDefinition def)
        {
            if (def == null) return;
            Excavation.SetGrid(SiteGenerator.Build(def, Content));
            SiteRebuilt?.Invoke(Grid);
        }

        private void SyncActiveContract() => Excavation.ActiveContractId = Contracts.ActiveContractId;

        /// <summary>Site shown when no contract is active: the first contract the player can accept.</summary>
        private ContractDefinition FindPreviewContract()
        {
            foreach (ContractDefinition def in Contracts.Definitions)
            {
                if (Contracts.GetState(def.Id) == ContractState.Available) return def;
            }
            return Contracts.Definitions.Count > 0 ? Contracts.Definitions[0] : null;
        }

        private void OnContractStateChanged(string id, ContractState state)
        {
            MarkDirty();
            ContractStateChanged?.Invoke(id, state);
        }

        private static bool IsFinite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
    }
}
