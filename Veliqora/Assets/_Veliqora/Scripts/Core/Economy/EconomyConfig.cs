using System;
using System.Collections.Generic;
using Veliqora.Core.Json;

namespace Veliqora.Core.Economy
{
    public enum BoosterType { Undo = 0, ExtraBottle = 1, Shuffle = 2, Hint = 3 }

    public enum CosmeticKind { Theme, BottleSkin }

    public sealed class BoosterOffer
    {
        public string Id;
        public BoosterType Booster;
        public int Quantity = 1;
        public int Price;
        public string Title;
        public string Description;
    }

    public sealed class CosmeticItem
    {
        public string Id;
        public CosmeticKind Kind;
        public string Title;
        public int Price;            // 0 = free / owned by default
        public bool DefaultOwned;
        /// <summary>Free-form presentation data (hex colors, shape params). Interpreted by the Unity layer.</summary>
        public Dictionary<string, object> Visual = new Dictionary<string, object>();
    }

    /// <summary>
    /// All prices and rewards come from data (Resources/Veliqora/Config/economy.json); nothing is hard-coded in UI.
    /// </summary>
    public sealed class EconomyConfig
    {
        public int StartingCoins = 200;
        public Dictionary<BoosterType, int> StartingBoosters = new Dictionary<BoosterType, int>();
        public int LevelBaseReward = 20;
        public int PerStarReward = 10;
        public int ReplayBaseReward = 5;
        public int StarImprovementReward = 10;
        public int MilestoneEvery = 10;
        public int MilestoneCoins = 100;
        public Dictionary<BoosterType, int> MilestoneBoosters = new Dictionary<BoosterType, int>();
        public int RewardedBonusBoosterQuantity = 1;
        public BoosterType RewardedBonusBooster = BoosterType.Undo;
        public int RewardedCooldownSeconds = 30;
        public List<BoosterOffer> Offers = new List<BoosterOffer>();
        public List<CosmeticItem> Cosmetics = new List<CosmeticItem>();

        public BoosterOffer FindOffer(string id) => Offers.Find(o => o.Id == id);
        public BoosterOffer FindOffer(BoosterType type) => Offers.Find(o => o.Booster == type);
        public CosmeticItem FindCosmetic(string id) => Cosmetics.Find(c => c.Id == id);

        public static EconomyConfig FromJson(string json)
        {
            var root = MiniJson.AsObject(MiniJson.Parse(json));
            var cfg = new EconomyConfig
            {
                StartingCoins = MiniJson.GetInt(root, "startingCoins", 200),
                LevelBaseReward = MiniJson.GetInt(root, "levelBaseReward", 20),
                PerStarReward = MiniJson.GetInt(root, "perStarReward", 10),
                ReplayBaseReward = MiniJson.GetInt(root, "replayBaseReward", 5),
                StarImprovementReward = MiniJson.GetInt(root, "starImprovementReward", 10),
                MilestoneEvery = MiniJson.GetInt(root, "milestoneEvery", 10),
                MilestoneCoins = MiniJson.GetInt(root, "milestoneCoins", 100),
                RewardedBonusBoosterQuantity = MiniJson.GetInt(root, "rewardedBonusBoosterQuantity", 1),
                RewardedCooldownSeconds = MiniJson.GetInt(root, "rewardedCooldownSeconds", 30),
            };
            cfg.RewardedBonusBooster = ParseBooster(MiniJson.GetString(root, "rewardedBonusBooster", "Undo"));
            cfg.StartingBoosters = ParseBoosterMap(MiniJson.GetObject(root, "startingBoosters"));
            cfg.MilestoneBoosters = ParseBoosterMap(MiniJson.GetObject(root, "milestoneBoosters"));

            foreach (var o in MiniJson.GetList(root, "offers"))
            {
                var d = MiniJson.AsObject(o);
                var offer = new BoosterOffer
                {
                    Id = MiniJson.GetString(d, "id"),
                    Booster = ParseBooster(MiniJson.GetString(d, "booster")),
                    Quantity = MiniJson.GetInt(d, "quantity", 1),
                    Price = MiniJson.GetInt(d, "price"),
                    Title = MiniJson.GetString(d, "title", ""),
                    Description = MiniJson.GetString(d, "description", ""),
                };
                if (offer.Price < 0 || offer.Quantity <= 0) throw new FormatException("Invalid offer " + offer.Id);
                cfg.Offers.Add(offer);
            }
            foreach (var o in MiniJson.GetList(root, "cosmetics"))
            {
                var d = MiniJson.AsObject(o);
                var item = new CosmeticItem
                {
                    Id = MiniJson.GetString(d, "id"),
                    Kind = MiniJson.GetString(d, "kind", "theme") == "bottle" ? CosmeticKind.BottleSkin : CosmeticKind.Theme,
                    Title = MiniJson.GetString(d, "title", ""),
                    Price = MiniJson.GetInt(d, "price"),
                    DefaultOwned = MiniJson.GetBool(d, "defaultOwned"),
                    Visual = MiniJson.GetObject(d, "visual"),
                };
                if (item.Price < 0) throw new FormatException("Invalid cosmetic " + item.Id);
                cfg.Cosmetics.Add(item);
            }
            return cfg;
        }

        public static BoosterType ParseBooster(string s)
        {
            if (Enum.TryParse(s, true, out BoosterType t)) return t;
            throw new FormatException("Unknown booster '" + s + "'");
        }

        private static Dictionary<BoosterType, int> ParseBoosterMap(Dictionary<string, object> d)
        {
            var map = new Dictionary<BoosterType, int>();
            foreach (var kv in d) map[ParseBooster(kv.Key)] = checked((int)MiniJson.ToLong(kv.Value));
            return map;
        }
    }
}
