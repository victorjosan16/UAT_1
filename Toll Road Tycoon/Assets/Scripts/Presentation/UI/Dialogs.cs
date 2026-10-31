using System;
using TollRoadTycoon.Core;
using TollRoadTycoon.Economy;
using TollRoadTycoon.Localization;
using TollRoadTycoon.Missions;
using TollRoadTycoon.Monetization;
using TollRoadTycoon.Services;
using UnityEngine;
using UnityEngine.UI;

namespace TollRoadTycoon.Presentation.UI
{
    /// <summary>Phase 2 dialogs: road map, shop, missions, statistics, offline income, buildings, test ads.</summary>
    public sealed partial class UIManager
    {
        private TycoonGame Game => _root.Game;

        // ---- road map ----------------------------------------------------------------------------

        public void ShowRoadMap()
        {
            var content = OpenModal(Loc.T("map.title"), 1500, ShowRoadMap);
            var list = ScrollBody(content, 1130);
            for (int i = 0; i < Game.Sections.Count; i++)
            {
                int index = i;
                var s = Game.Sections[i];
                Panels.Row(list, s.Unlocked ? Palette.Green : Palette.Disabled, (index + 1).ToString(),
                    () => Loc.F("hud.section_n", index + 1) + " · " + Loc.T(s.Config.NameKey),
                    () => SectionSummary(s),
                    () => index == Game.ActiveIndex ? Loc.T("map.current") : s.Unlocked ? Loc.T("map.enter")
                        : Game.CanUnlock(index) == SectionBlock.PreviousLocked ? Loc.T("map.locked") : Loc.T("map.unlock"),
                    () => index != Game.ActiveIndex && (s.Unlocked || Game.CanUnlock(index) != SectionBlock.PreviousLocked),
                    () =>
                    {
                        if (s.Unlocked)
                        {
                            CloseModal();
                            _root.EnterSection(index);
                        }
                        else
                        {
                            ShowUnlockSection(index);
                        }
                    },
                    AddModalUpdater, 170, s.Unlocked ? Palette.Blue : Palette.Green);
            }
            var note = UIFactory.Label(content, Loc.T("map.estimate_note"), 28, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Italic);
            UIFactory.Layout(note, 80);
        }

        private string SectionSummary(SectionState s)
        {
            string km = Loc.F("hud.km_range", s.Config.KmFrom, s.Config.KmTo);
            if (!s.Unlocked) return km + "  ·  " + Money.Format(s.Config.UnlockPriceCents);
            long perMinute = (long)(Game.EstimateIncomePerSecond(s) * 60);
            return km + "  ·  " + Loc.F("map.booths", Game.BoothCount(s), s.RoadConfig.MaxBooths) + "\n" + Loc.F("map.income", Money.Format(perMinute));
        }

        private void ShowUnlockSection(int index)
        {
            var s = Game.Sections[index];
            var previous = Game.Sections[index - 1];
            var content = OpenModal(Loc.T("map.unlock_title"), 1150, () => ShowUnlockSection(index));
            var title = UIFactory.Label(content, Loc.F("hud.section_n", index + 1) + " · " + Loc.T(s.Config.NameKey), 46, Palette.DarkBlue);
            UIFactory.Layout(title, 70);
            var km = UIFactory.Label(content, Loc.F("hud.km_range", s.Config.KmFrom, s.Config.KmTo), 36, Palette.Ink);
            UIFactory.Layout(km, 50);

            Requirement(content, Loc.T("map.req_prev"), () => previous.Unlocked);
            Requirement(content, Loc.F("map.req_booths", s.Config.RequiredPreviousBooths), () => Game.BoothCount(previous) >= s.Config.RequiredPreviousBooths);
            if (s.Config.RequiredIncomePerMinuteCents > 0)
            {
                Requirement(content, Loc.F("map.req_income", Money.Format(s.Config.RequiredIncomePerMinuteCents)),
                    () => Game.Economy.GetIncomePerMinute() >= s.Config.RequiredIncomePerMinuteCents);
            }
            Requirement(content, Loc.F("map.req_price", Money.Format(s.Config.UnlockPriceCents)), () => Game.Economy.CanAfford(s.Config.UnlockPriceCents));

            var buy = UIFactory.Button(content, Loc.T("map.unlock") + "  " + Money.Format(s.Config.UnlockPriceCents), Palette.Green, () =>
            {
                if (Game.Unlock(index))
                {
                    _root.Audio.Play(Sfx.Purchase);
                    Vibrate();
                    Toast(Loc.T("toast.section_unlocked"));
                    CloseModal();
                    _root.EnterSection(index);
                }
                else
                {
                    _root.Audio.Play(Sfx.Error);
                }
            }, 44);
            UIFactory.Layout(buy, 130);
            AddModalUpdater(() => buy.interactable = Game.CanUnlock(index) == SectionBlock.None);
        }

        private void Requirement(Transform parent, string text, Func<bool> met)
        {
            var label = UIFactory.Label(parent, "", 34, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Normal);
            UIFactory.Layout(label, 64);
            AddModalUpdater(() =>
            {
                bool ok = met();
                label.text = (ok ? "<color=#1F9A3C>•</color>  " : "<color=#F05B54>•</color>  ") + text;
            });
            label.supportRichText = true;
        }

        // ---- shop -------------------------------------------------------------------------------

        private int _shopTab;

        public void ShowShop() => ShowShop(_shopTab);

        public void ShowShop(int tab)
        {
            _shopTab = tab;
            var content = OpenModal(Loc.T("shop.title"), 1600, () => ShowShop(_shopTab));
            var tabs = UIFactory.Rect(content, "Tabs");
            UIFactory.HorizontalList(tabs, 10, 0);
            UIFactory.Layout(tabs, 100);
            string[] keys = { "shop.vehicles", "shop.decor", "shop.buildings", "shop.bonus" };
            for (int i = 0; i < keys.Length; i++)
            {
                int t = i;
                UIFactory.Button(tabs, Loc.T(keys[i]), t == tab ? Palette.Green : Palette.Blue, () => ShowShop(t), 30);
            }
            var list = ScrollBody(content, 1180);
            switch (tab)
            {
                case 0: Panels.BuildVehicleUnlocks(this, list, AddModalUpdater); break;
                case 1: BuildDecorationCatalog(list, AddModalUpdater, true); break;
                case 2: BuildServiceCatalog(list, AddModalUpdater, true); break;
                default: BuildBonuses(list); break;
            }
        }

        public void BuildServiceCatalog(Transform list, Action<Action> register, bool closeModal)
        {
            var section = Game.ActiveSection;
            Panels.Header(list, Loc.T("shop.buildings_hint"), 26);
            foreach (var config in Game.Progression.Services)
            {
                var c = config;
                bool allowed = section.Config.AllowsService(c.Id);
                long cost = section.Services.BuildCost(c);
                var spend = Money.Format((long)Math.Round(c.SpendCents * section.Config.TollMultiplier));
                Panels.Row(list, allowed ? Palette.Blue : Palette.Disabled, Loc.T(c.NameKey).Substring(0, 1),
                    () => Loc.T(c.NameKey),
                    () => allowed ? Loc.F("service.desc", c.Capacity, c.ServiceSeconds, spend) : Loc.T("service.not_here"),
                    () => allowed ? Money.Format(cost) : "—",
                    () => allowed && Game.Economy.CanAfford(cost),
                    () =>
                    {
                        if (closeModal) CloseModal();
                        _root.BeginPlacement(PlacementKind.BuildService, c.Id);
                        Toast(Loc.T("service.pick_slot"));
                    },
                    register, 140, null, 300, "Buildings/service_" + c.Id);
            }
        }

        public void BuildDecorationCatalog(Transform list, Action<Action> register, bool closeModal)
        {
            Panels.Header(list, Loc.T("decor.note"), 26);
            foreach (var config in Game.Progression.Decorations)
            {
                var d = config;
                Panels.Row(list, Palette.Gold, Loc.T(d.NameKey).Substring(0, 1),
                    () => Loc.T(d.NameKey),
                    () => Money.Format(d.PriceCents),
                    () => Loc.T("decor.buy"),
                    () => Game.Economy.CanAfford(d.PriceCents) && Game.ActiveSection.Decorations.FirstFreeSlot() >= 0,
                    () =>
                    {
                        if (closeModal) CloseModal();
                        _root.BeginPlacement(PlacementKind.BuildDecoration, d.Id);
                        Toast(Loc.T("service.pick_slot"));
                    },
                    register, 120, null, 300, "Decorations/" + d.Id);
            }
        }

        private void BuildBonuses(Transform list)
        {
            bool test = Game.Ads.Service is TestAdService;
            Panels.Header(list, Loc.T(test ? "shop.ads_test" : "shop.ads_off"), 26);
            Panels.Row(list, Palette.Green, "x2",
                () => Loc.T("shop.ad_boost"),
                () => Game.Boost.IsActive ? Loc.F("hud.boost_active", Mathf.CeilToInt((float)Game.Boost.SecondsLeft)) : "",
                () => Loc.T("shop.watch"),
                () => Game.Ads.IsAvailable(AdPlacements.IncomeBoost),
                () => Game.RequestAdBoost(r =>
                {
                    if (r == AdResult.Completed) Toast(Loc.T("toast.boost"));
                }),
                AddModalUpdater, 130, null, 300, "Icons/star");
            Panels.Row(list, Palette.Gold, "$",
                () => Loc.F("shop.ad_cash", Money.Format(Game.InstantCashReward)),
                () => "",
                () => Loc.T("shop.watch"),
                () => Game.Ads.IsAvailable(AdPlacements.InstantCash),
                () => Game.RequestAdInstantCash(),
                AddModalUpdater, 130, null, 300, "Icons/coin");
            Panels.Row(list, Palette.Blue, "x2",
                () => Loc.T("shop.cash_boost"),
                () => Game.Boost.IsActive ? Loc.F("hud.boost_active", Mathf.CeilToInt((float)Game.Boost.SecondsLeft)) : "",
                () => Money.Format(Game.Progression.BoostCashPriceCents),
                () => Game.Economy.CanAfford(Game.Progression.BoostCashPriceCents),
                () =>
                {
                    if (TryPurchase(Game.Progression.BoostCashPriceCents, Game.BuyBoostWithCash)) Toast(Loc.T("toast.boost"));
                },
                AddModalUpdater, 130, null, 300, "Icons/upgrade_speed");
        }

        // ---- missions ----------------------------------------------------------------------------

        public void ShowMissions()
        {
            Game.Missions.RefreshDaily(DateTime.UtcNow, Game.Economy.GetIncomePerMinute());
            var content = OpenModal(Loc.T("missions.title"), 1600, ShowMissions);
            var list = ScrollBody(content, 1300);
            Panels.Header(list, Loc.T("missions.daily"));
            foreach (var m in Game.Missions.Daily) MissionRow(list, m, AddModalUpdater);
            Panels.Header(list, Loc.T("missions.milestones"));
            var current = Game.Missions.CurrentMilestone;
            if (current == null)
            {
                Panels.Header(list, Loc.T("missions.done"), 30);
                return;
            }
            int start = Game.Missions.MilestoneIndex;
            for (int i = start; i < Math.Min(start + 4, Game.Missions.Milestones.Count); i++) MissionRow(list, Game.Missions.Milestones[i], AddModalUpdater);
        }

        public void MissionRow(Transform list, Mission m, Action<Action> register)
        {
            Panels.Row(list, m.Claimed ? Palette.Disabled : Palette.Gold, "!",
                () => DescribeMission(m),
                () => Loc.F("missions.reward", Money.Format(m.RewardCents)) + "   " + FormatProgress(m),
                () => m.Claimed ? Loc.T("missions.claimed") : Loc.T("missions.claim"),
                () => Game.Missions.CanClaim(m),
                () =>
                {
                    if (Game.Missions.Claim(m))
                    {
                        _root.Audio.Play(Sfx.Purchase);
                        Toast(Loc.F("toast.reward", Money.Format(m.RewardCents)));
                    }
                },
                register, 130);
        }

        public string DescribeMission(Mission m)
        {
            string target;
            switch (m.Type)
            {
                case MissionType.EarnMoney:
                case MissionType.ReachIncome:
                    target = Money.Format(m.Target);
                    break;
                case MissionType.UnlockVehicle:
                    target = Loc.T("vehicle." + m.Param);
                    break;
                default:
                    target = m.Target.ToString();
                    break;
            }
            return Loc.F("mission." + m.Type, target);
        }

        public string FormatProgress(Mission m)
        {
            long p = Game.Missions.Progress(m);
            if (m.Type == MissionType.EarnMoney || m.Type == MissionType.ReachIncome) return Money.Format(p) + " / " + Money.Format(m.Target);
            return p + " / " + m.Target;
        }

        // ---- statistics --------------------------------------------------------------------------

        public void ShowStats()
        {
            var content = OpenModal(Loc.T("stats.title"), 1500, ShowStats);
            var st = Game.Stats;
            var sim = _root.Sim;
            StatRow(content, "stats.income", () => Money.Format(Game.Economy.GetIncomePerMinute()));
            StatRow(content, "stats.cars", () => st.VehiclesProcessed.ToString("N0"));
            StatRow(content, "stats.trucks", () => st.HeavyVehiclesProcessed.ToString("N0"));
            StatRow(content, "stats.wait", () => st.AverageQueueWaitSeconds.ToString("0.0") + " s");
            StatRow(content, "stats.utilization", () =>
            {
                double busy = 0;
                foreach (var b in sim.Booths) busy += b.BusySeconds;
                double total = Math.Max(1, sim.Time * Math.Max(1, sim.Booths.Count));
                return Mathf.RoundToInt((float)(busy / total * 100)) + "%";
            });
            StatRow(content, "stats.booths", () =>
            {
                int active = 0;
                foreach (var b in sim.Booths)
                {
                    if (b.IsAcceptingVehicles) active++;
                }
                return active + " / " + sim.Booths.Count;
            });
            StatRow(content, "stats.sections", () =>
            {
                int n = 0;
                foreach (var s in Game.Sections)
                {
                    if (s.Unlocked) n++;
                }
                return n + " / " + Game.Sections.Count;
            });
            StatRow(content, "stats.services", () => Money.Format(st.ServiceIncomeCents));
            StatRow(content, "stats.passive", () => Money.Format(st.PassiveIncomeCents));
            StatRow(content, "stats.offline", () => Money.Format(st.OfflineIncomeCents));
            StatRow(content, "stats.earnings", () => Money.Format(Game.Economy.TotalEarnings));
            StatRow(content, "stats.expenses", () => Money.Format(Game.Economy.TotalExpenses));
            StatRow(content, "stats.playtime", () =>
            {
                var t = TimeSpan.FromSeconds(st.PlayTimeSeconds);
                return (int)t.TotalHours + "h " + t.Minutes.ToString("00") + "m";
            });
        }

        private void StatRow(Transform parent, string key, Func<string> value)
        {
            var row = UIFactory.Panel(parent, "Stat", Color.white);
            UIFactory.Layout(row, 78);
            var label = UIFactory.Label(row.transform, Loc.T(key), 32, Palette.DarkBlue, TextAnchor.MiddleLeft, FontStyle.Normal);
            UIFactory.Stretch(label.rectTransform, 26, 300, 0, 0);
            var v = UIFactory.Label(row.transform, "", 36, Palette.DarkGreen, TextAnchor.MiddleRight);
            UIFactory.Stretch(v.rectTransform, 300, 26, 0, 0);
            AddModalUpdater(() => v.text = value());
        }

        // ---- offline income ------------------------------------------------------------------------

        public void ShowOffline()
        {
            if (Game.PendingOfflineCents <= 0) return;
            var content = OpenModal(Loc.T("offline.title"), 1000, ShowOffline);
            var away = TimeSpan.FromSeconds(Game.PendingOfflineSeconds);
            string duration = away.TotalSeconds > 0 ? (int)away.TotalHours + "h " + away.Minutes.ToString("00") + "m" : "—";
            var body = UIFactory.Label(content, Loc.F("offline.body", duration), 36, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Normal);
            UIFactory.Layout(body, 110);
            var amount = UIFactory.Label(content, Money.Format(Game.PendingOfflineCents), 80, Palette.Gold);
            UIFactory.AddOutline(amount, Palette.DarkBlue, 3);
            UIFactory.Layout(amount, 130);
            var note = UIFactory.Label(content, Loc.T("offline.note"), 28, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Italic);
            UIFactory.Layout(note, 90);
            var collect = UIFactory.Button(content, Loc.T("offline.collect"), Palette.Green, () =>
            {
                long got = Game.CollectOffline();
                if (got > 0) _root.Audio.Play(Sfx.Purchase);
                CloseModal();
            }, 46);
            UIFactory.Layout(collect, 130);
            if (Game.Ads.IsAvailable(AdPlacements.OfflineDouble))
            {
                var dbl = UIFactory.Button(content, Loc.T("offline.double"), Palette.Blue, () =>
                {
                    Game.RequestOfflineDouble(result =>
                    {
                        if (result == AdResult.Completed)
                        {
                            _root.Audio.Play(Sfx.Purchase);
                            CloseModal();
                        }
                    });
                }, 40);
                UIFactory.Layout(dbl, 120);
            }
        }

        // ---- test ads --------------------------------------------------------------------------------

        public void ShowTestAd(TestAdService service, TestAdService.Request request)
        {
            UIFactory.Clear(_adLayer);
            var dim = UIFactory.Panel(_adLayer, "TestAd", new Color(0.02f, 0.08f, 0.16f, 0.95f), SpriteFactory.Square);
            UIFactory.Stretch(dim.rectTransform);
            var title = UIFactory.Label(dim.transform, Loc.T("ad.title"), 70, Palette.Gold);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 300), new Vector2(980, 120));
            var body = UIFactory.Label(dim.transform, Loc.T("ad.body") + "\n(" + request.Placement + ")", 36, Color.white, TextAnchor.MiddleCenter, FontStyle.Normal);
            UIFactory.Place(body.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 120), new Vector2(900, 220));
            var countdown = UIFactory.Label(dim.transform, "", 48, Palette.Green);
            UIFactory.Place(countdown.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(900, 100));
            var skip = UIFactory.Button(dim.transform, Loc.T("ad.skip"), Palette.Red, () =>
            {
                UIFactory.Clear(_adLayer);
                service.Finish(AdResult.Skipped);
            }, 36);
            UIFactory.Place((RectTransform)skip.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -260), new Vector2(640, 120));
            var timer = dim.gameObject.AddComponent<TestAdCountdown>();
            timer.Begin(3f, countdown, () =>
            {
                UIFactory.Clear(_adLayer);
                service.Finish(AdResult.Completed);
                Toast(Loc.T("ad.done"));
            });
        }

        // ---- buildings ---------------------------------------------------------------------------------

        public void ConfirmBuildService(string id, int slot)
        {
            var section = Game.ActiveSection;
            var config = Game.Progression.FindService(id);
            long cost = section.Services.BuildCost(config);
            var spend = Money.Format((long)Math.Round(config.SpendCents * section.Config.TollMultiplier));
            Confirm(Loc.T(config.NameKey), Loc.F("service.desc", config.Capacity, config.ServiceSeconds, spend), cost, () =>
            {
                if (TryPurchase(cost, () => section.Services.Build(id, slot) == PlacementResult.Ok))
                {
                    _root.CancelPlacement();
                    Toast(Loc.T("toast.bought"));
                }
            });
        }

        public void ConfirmBuyDecoration(string id, int slot)
        {
            var config = Game.Progression.FindDecoration(id);
            Confirm(Loc.T(config.NameKey), Loc.T("decor.note"), config.PriceCents, () =>
            {
                if (TryPurchase(config.PriceCents, () => Game.ActiveSection.Decorations.Buy(id, slot) == PlacementResult.Ok))
                {
                    _root.CancelPlacement();
                }
            });
        }

        public void ShowServiceDetail(int slot)
        {
            var services = Game.ActiveSection.Services;
            var s = services.At(slot);
            if (s == null) return;
            var content = OpenModal(Loc.T("service.title"), 1050, () => ShowServiceDetail(slot));
            var title = UIFactory.Label(content, Loc.T(s.Config.NameKey), 46, Palette.DarkBlue);
            UIFactory.Layout(title, 70);
            var info = UIFactory.Label(content, "", 34, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Normal);
            UIFactory.Layout(info, 130);
            AddModalUpdater(() => info.text = Loc.F("service.placed", s.Level, Money.Format(s.Earned)) + "\n" +
                                              Loc.F("service.desc", services.CapacityOf(s), s.Config.ServiceSeconds, Money.Format(services.SpendPerCustomer(s))));
            var upgrade = Panels.Row(content, Palette.Green, "+",
                () => Loc.T("service.upgrade"),
                () => s.Level >= s.Config.MaxLevel ? Loc.T("upgrade.max") : Loc.F("common.level", s.Level + 1),
                () => s.Level >= s.Config.MaxLevel ? Loc.T("upgrade.max") : Money.Format(services.UpgradeCost(s)),
                () => s.Level < s.Config.MaxLevel && Game.Economy.CanAfford(services.UpgradeCost(s)),
                () => TryPurchase(services.UpgradeCost(s), () => services.Upgrade(s.Slot) == PlacementResult.Ok),
                AddModalUpdater, 130);
            var row = UIFactory.Rect(content, "Buttons");
            UIFactory.HorizontalList(row, 24, 0);
            UIFactory.Layout(row, 120);
            UIFactory.Button(row, Loc.T("service.move"), Palette.Blue, () =>
            {
                CloseModal();
                _root.BeginPlacement(PlacementKind.MoveService, s.Config.Id, s.Slot);
                Toast(Loc.T("service.pick_slot"));
            }, 38);
            UIFactory.Button(row, Loc.T("service.remove"), Palette.Red, () =>
            {
                long refund = services.RemoveRefund(s);
                var confirm = OpenModal(Loc.T("service.remove"), 640);
                var text = UIFactory.Label(confirm, Loc.F("service.remove_body", Money.Format(refund)), 38, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Normal);
                UIFactory.Layout(text, 200);
                var buttons = UIFactory.Rect(confirm, "Buttons");
                UIFactory.HorizontalList(buttons, 24, 0);
                UIFactory.Layout(buttons, 120);
                UIFactory.Button(buttons, Loc.T("confirm.cancel"), Palette.Green, CloseModal, 38);
                UIFactory.Button(buttons, Loc.T("service.remove"), Palette.Red, () =>
                {
                    services.Remove(s.Slot, _root.Sim.Time);
                    CloseModal();
                }, 36);
            }, 38);
        }

        public void ShowDecorationDetail(int slot)
        {
            var decorations = Game.ActiveSection.Decorations;
            var d = decorations.At(slot);
            if (d == null) return;
            var content = OpenModal(Loc.T(d.Config.NameKey), 640);
            var note = UIFactory.Label(content, Loc.T("decor.note"), 34, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Normal);
            UIFactory.Layout(note, 150);
            var row = UIFactory.Rect(content, "Buttons");
            UIFactory.HorizontalList(row, 24, 0);
            UIFactory.Layout(row, 120);
            UIFactory.Button(row, Loc.T("service.move"), Palette.Blue, () =>
            {
                CloseModal();
                _root.BeginPlacement(PlacementKind.MoveDecoration, d.Config.Id, d.Slot);
                Toast(Loc.T("service.pick_slot"));
            }, 38);
            UIFactory.Button(row, Loc.T("decor.remove"), Palette.Red, () =>
            {
                decorations.Remove(d.Slot);
                CloseModal();
            }, 38);
        }

        // ---- helpers ---------------------------------------------------------------------------------

        /// <summary>Scrollable list inside a modal body.</summary>
        private static RectTransform ScrollBody(Transform content, float height)
        {
            var holder = UIFactory.Rect(content, "ScrollHolder");
            UIFactory.Layout(holder, height);
            var list = UIFactory.ScrollList(holder, "List", 12, 4);
            UIFactory.Stretch((RectTransform)list.parent);
            return list;
        }
    }

    /// <summary>Counts down the test ad and completes it when time is up.</summary>
    public sealed class TestAdCountdown : MonoBehaviour
    {
        private float _left;
        private Text _label;
        private Action _done;

        public void Begin(float seconds, Text label, Action done)
        {
            _left = seconds;
            _label = label;
            _done = done;
        }

        private void Update()
        {
            if (_done == null) return;
            _left -= Time.unscaledDeltaTime;
            _label.text = Localization.Loc.F("ad.wait", Mathf.Max(0, Mathf.CeilToInt(_left)));
            if (_left > 0) return;
            var done = _done;
            _done = null;
            done();
        }
    }
}
