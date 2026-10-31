using System;
using System.Collections.Generic;
using TollRoadTycoon.Economy;
using TollRoadTycoon.Localization;
using TollRoadTycoon.RoadExpansion;
using TollRoadTycoon.TollSystem;
using TollRoadTycoon.Workers;
using UnityEngine;
using UnityEngine.UI;

namespace TollRoadTycoon.Presentation.UI
{
    public enum GameplayTab
    {
        Booths,
        Lanes,
        Workers,
        Upgrade,
        Services,
        Decor,
    }

    /// <summary>Top HUD (balance, section, income/min) and the bottom CABINE/BENZI/ANGAJAȚI/UPGRADE panel.</summary>
    public sealed class GameplayScreen : MonoBehaviour
    {
        private const float BottomHeight = 680;

        private UIManager _ui;
        private GameRoot _root;
        private RectTransform _content;
        private GameplayTab _tab = GameplayTab.Booths;
        private readonly List<Action> _updaters = new List<Action>();
        private readonly List<Image> _tabImages = new List<Image>();
        private float _timer;
        private bool _dirty;
        private int _fixedUpdaterCount;

        public void Build(UIManager ui)
        {
            _ui = ui;
            _root = ui.Root;
            UIFactory.Clear(transform);
            _updaters.Clear();
            _tabImages.Clear();
            _root.SelectionChanged -= MarkDirty;
            _root.SelectionChanged += MarkDirty;
            _root.SimulationReplaced -= OnSimulationReplaced;
            _root.SimulationReplaced += OnSimulationReplaced;
            OnSimulationReplaced();

            BuildTopBar();
            BuildBottomPanel();
            _fixedUpdaterCount = _updaters.Count;
            _root.CameraController.BottomPanelFraction = 0.33f;
            Rebuild();
        }

        private void OnDestroy()
        {
            if (_root == null) return;
            _root.SelectionChanged -= MarkDirty;
            _root.SimulationReplaced -= OnSimulationReplaced;
        }

        private void OnSimulationReplaced()
        {
            _root.Sim.StateChanged -= MarkDirty;
            _root.Sim.StateChanged += MarkDirty;
            _root.Game.StateChanged -= MarkDirty;
            _root.Game.StateChanged += MarkDirty;
            _root.PlacementChanged -= MarkDirty;
            _root.PlacementChanged += MarkDirty;
            MarkDirty();
        }

        private void MarkDirty() => _dirty = true;

        private void Register(Action updater)
        {
            _updaters.Add(updater);
            updater();
        }

        private void Update()
        {
            if (_dirty)
            {
                _dirty = false;
                Rebuild();
            }
            _timer -= Time.unscaledDeltaTime;
            if (_timer > 0) return;
            _timer = 0.2f;
            foreach (var u in _updaters) u();
        }

        // ---- HUD -----------------------------------------------------------------------------

        private void BuildTopBar()
        {
            Func<Core.GameSimulation> sim = () => _root.Sim;

            var balance = UIFactory.Panel(transform, "Balance", Palette.DarkBlue, SpriteFactory.Pill);
            UIFactory.Place(balance.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -20), new Vector2(520, 100));
            var coinArt = UIFactory.Icon(balance.transform, "Icons/coin");
            if (coinArt != null)
            {
                UIFactory.Place(coinArt.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(6, 0), new Vector2(90, 90));
            }
            else
            {
                var coin = UIFactory.Panel(balance.transform, "Coin", Palette.Gold, SpriteFactory.Pill);
                UIFactory.Place(coin.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(10, 0), new Vector2(80, 80));
                var coinMark = UIFactory.Label(coin.transform, "$", 46, Palette.DarkBlue);
                UIFactory.Stretch(coinMark.rectTransform);
            }
            var balanceText = UIFactory.Label(balance.transform, "", 50, Color.white, TextAnchor.MiddleLeft);
            UIFactory.Stretch(balanceText.rectTransform, 110, 20, 0, 0);
            Register(() => balanceText.text = Money.Format(sim().Economy.Balance));

            var pause = UIFactory.Button(transform, "II", Palette.Blue, _ui.ShowPause, 44);
            UIFactory.Place((RectTransform)pause.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -20), new Vector2(100, 100));

            var cars = UIFactory.Panel(transform, "Cars", Palette.DarkBlue, SpriteFactory.Pill);
            UIFactory.Place(cars.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-140, -20), new Vector2(300, 100));
            var carsText = UIFactory.Label(cars.transform, "", 36, Color.white);
            UIFactory.Stretch(carsText.rectTransform, 10, 10, 0, 0);
            Register(() => carsText.text = Loc.F("hud.cars", sim().Stats.VehiclesProcessed));

            // Section card with progress (booths built out of the section's maximum).
            var section = UIFactory.Panel(transform, "Section", Palette.DarkBlue);
            UIFactory.Place(section.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -140), new Vector2(520, 190));
            var title = UIFactory.Label(section.transform, "", 44, Color.white, TextAnchor.UpperLeft);
            UIFactory.Stretch(title.rectTransform, 26, 20, 14, 100);
            var km = UIFactory.Label(section.transform, "", 30, Palette.Cream, TextAnchor.UpperLeft, FontStyle.Normal);
            UIFactory.Stretch(km.rectTransform, 26, 20, 66, 60);
            Register(() =>
            {
                var sc = _root.Game.ActiveSection.Config;
                title.text = Loc.F("hud.section_n", _root.Game.ActiveIndex + 1);
                km.text = Loc.F("hud.km_range", sc.KmFrom, sc.KmTo) + " · " + Loc.T(sc.NameKey);
            });
            var barBack = UIFactory.Panel(section.transform, "ProgressBack", new Color(1, 1, 1, 0.25f), SpriteFactory.Pill);
            UIFactory.Band(barBack.rectTransform, false, 22, 44, 26);
            var barFill = UIFactory.Panel(barBack.transform, "ProgressFill", Palette.Green, SpriteFactory.Pill);
            var progress = UIFactory.Label(barBack.transform, "", 30, Color.white);
            UIFactory.Stretch(progress.rectTransform);
            Register(() =>
            {
                var s = sim();
                float p = (s.Booths.Count + s.Lanes.ActiveLaneCount - 1f) / (s.Config.MaxBooths + s.Config.MaxLanes - 1f);
                var rt = barFill.rectTransform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = new Vector2(Mathf.Clamp01(p), 1);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                barFill.enabled = p > 0.04f;
                progress.text = Mathf.RoundToInt(p * 100) + "%";
            });

            var income = UIFactory.Panel(transform, "Income", Palette.DarkBlue);
            UIFactory.Place(income.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -140), new Vector2(330, 190));
            var incomeTitle = UIFactory.Label(income.transform, Loc.T("hud.income"), 32, Palette.Cream, TextAnchor.UpperCenter, FontStyle.Normal);
            UIFactory.Stretch(incomeTitle.rectTransform, 10, 10, 18, 110);
            var incomeValue = UIFactory.Label(income.transform, "", 52, Palette.Green);
            UIFactory.AddOutline(incomeValue, new Color(0, 0, 0, 0.45f), 2);
            UIFactory.Stretch(incomeValue.rectTransform, 10, 10, 70, 20);
            Register(() => incomeValue.text = Money.Format(sim().Economy.GetIncomePerMinute()));

            BuildQuickButtons();
            BuildGoalBanner();
            BuildPlacementBar();
        }

        /// <summary>Map, missions, shop, statistics and the x2 boost, stacked under the income card.</summary>
        private void BuildQuickButtons()
        {
            float y = -350;
            void Quick(string key, Color color, System.Action onClick, string icon)
            {
                var b = UIFactory.Button(transform, Loc.T(key), color, () =>
                {
                    _root.Audio.Play(Sfx.Click);
                    onClick();
                }, 28);
                UIFactory.Place((RectTransform)b.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, y), new Vector2(230, 84));
                var art = UIFactory.Icon(b.transform, icon);
                if (art != null)
                {
                    UIFactory.Place(art.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(8, 0), new Vector2(64, 64));
                    UIFactory.ButtonText(b).rectTransform.offsetMin = new Vector2(70, 4);
                }
                y -= 96;
            }
            Quick("hud.map", Palette.Blue, _ui.ShowRoadMap, "Icons/menu_map");
            Quick("hud.missions", Palette.Blue, _ui.ShowMissions, "Icons/menu_missions");
            Quick("hud.shop", Palette.Blue, () => _ui.ShowShop(), "Icons/star");
            Quick("hud.stats", Palette.Blue, _ui.ShowStats, "Icons/upgrade_capacity");

            var boost = UIFactory.Button(transform, "", Palette.Gold, () => _ui.ShowShop(3), 34);
            UIFactory.Place((RectTransform)boost.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, y), new Vector2(230, 84));
            var boostText = UIFactory.ButtonText(boost);
            var boostImage = boost.GetComponent<Image>();
            Register(() =>
            {
                var b = _root.Game.Boost;
                boostText.text = b.IsActive ? Loc.F("hud.boost_active", Mathf.CeilToInt((float)b.SecondsLeft)) : Loc.T("hud.boost");
                boostImage.color = b.IsActive ? Palette.Green : Palette.Gold;
            });
        }

        /// <summary>Current milestone (first-play tutorial): what to do next, progress, and claiming.</summary>
        private void BuildGoalBanner()
        {
            var banner = UIFactory.Panel(transform, "Goal", new Color(0.02f, 0.3f, 0.52f, 0.92f));
            UIFactory.Place(banner.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -345), new Vector2(560, 120));
            var button = banner.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            var text = UIFactory.Label(banner.transform, "", 28, Color.white, TextAnchor.MiddleLeft, FontStyle.Normal);
            UIFactory.Stretch(text.rectTransform, 20, 16, 8, 8);
            text.supportRichText = true;
            button.onClick.AddListener(() =>
            {
                var m = _root.Game.Missions.CurrentMilestone;
                if (m != null && _root.Game.Missions.Claim(m))
                {
                    _root.Audio.Play(Sfx.Purchase);
                    _ui.Toast(Loc.F("toast.reward", Money.Format(m.RewardCents)));
                }
                else
                {
                    _ui.ShowMissions();
                }
            });
            Register(() =>
            {
                var missions = _root.Game.Missions;
                var m = missions.CurrentMilestone;
                banner.gameObject.SetActive(m != null && _root.Placement == PlacementKind.None);
                if (m == null) return;
                bool done = missions.CanClaim(m);
                banner.color = done ? Palette.Green : new Color(0.02f, 0.3f, 0.52f, 0.92f);
                text.text = Loc.F("hud.goal", _ui.DescribeMission(m)) + "\n<b>" +
                            (done ? Loc.T("hud.claim") + " " + Money.Format(m.RewardCents) : _ui.FormatProgress(m)) + "</b>";
            });
        }

        /// <summary>Shown while choosing a spot for a building or decoration.</summary>
        private void BuildPlacementBar()
        {
            var bar = UIFactory.Panel(transform, "Placement", Palette.DarkBlue);
            UIFactory.Band(bar.rectTransform, false, BottomHeight + 16, 120, 24);
            var text = UIFactory.Label(bar.transform, Loc.T("service.pick_slot"), 32, Color.white, TextAnchor.MiddleLeft);
            UIFactory.Stretch(text.rectTransform, 24, 300, 0, 0);
            var cancel = UIFactory.Button(bar.transform, Loc.T("service.cancel"), Palette.Red, () => _root.CancelPlacement(), 34);
            UIFactory.Place((RectTransform)cancel.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-16, 0), new Vector2(260, 90));
            Register(() => bar.gameObject.SetActive(_root.Placement != PlacementKind.None));
        }

        // ---- Bottom panel ---------------------------------------------------------------------

        private void BuildBottomPanel()
        {
            var panel = UIFactory.Panel(transform, "BottomPanel", Palette.DarkBlue);
            UIFactory.Band(panel.rectTransform, false, 0, BottomHeight, 0);
            UIFactory.AddDropShadow(panel, -6);

            var tabs = UIFactory.Rect(panel.transform, "Tabs");
            UIFactory.Band(tabs, true, 12, 120, 12);
            UIFactory.HorizontalList(tabs, 10, 0);
            AddTab(tabs, GameplayTab.Booths, "tab.booths");
            AddTab(tabs, GameplayTab.Lanes, "tab.lanes");
            AddTab(tabs, GameplayTab.Workers, "tab.workers");
            AddTab(tabs, GameplayTab.Upgrade, "tab.upgrade");
            AddTab(tabs, GameplayTab.Services, "tab.services");
            AddTab(tabs, GameplayTab.Decor, "tab.decor");

            var body = UIFactory.Panel(panel.transform, "Body", Palette.PanelLight);
            UIFactory.Stretch(body.rectTransform, 12, 12, 142, 12);
            _content = UIFactory.ScrollList(body.transform, "List", 14, 16);
            UIFactory.Stretch((RectTransform)_content.parent);
        }

        private void AddTab(Transform parent, GameplayTab tab, string key)
        {
            var button = UIFactory.Button(parent, Loc.T(key), Palette.Blue, () =>
            {
                _root.Audio.Play(Sfx.Click);
                _tab = tab;
                Rebuild();
            }, 26);
            var image = button.GetComponent<Image>();
            _tabImages.Add(image);
            var icon = UIFactory.Icon(button.transform, "Icons/tab_" + tab.ToString().ToLowerInvariant());
            if (icon != null)
            {
                // Icon on top, label underneath, as in the mockup.
                UIFactory.Place(icon.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -6), new Vector2(70, 64));
                var label = UIFactory.ButtonText(button).rectTransform;
                label.offsetMax = new Vector2(label.offsetMax.x, -66);
                UIFactory.ButtonText(button).fontSize = 22;
            }
            Register(() => image.color = _tab == tab ? Palette.Green : Palette.Blue);
        }

        /// <summary>Rebuilds the active tab's list (structure changes only; values refresh via updaters).</summary>
        public void Rebuild()
        {
            if (_content == null) return;
            UIFactory.Clear(_content);
            // Keep HUD and tab updaters (registered before the list), drop the previous list's ones.
            _updaters.RemoveRange(_fixedUpdaterCount, _updaters.Count - _fixedUpdaterCount);
            for (int i = 0; i < _tabImages.Count; i++) _tabImages[i].color = (int)_tab == i ? Palette.Green : Palette.Blue;
            switch (_tab)
            {
                case GameplayTab.Booths: BuildBooths(); break;
                case GameplayTab.Lanes: BuildLanes(); break;
                case GameplayTab.Workers: BuildWorkers(); break;
                case GameplayTab.Upgrade: BuildUpgrade(); break;
                case GameplayTab.Services: BuildServices(); break;
                case GameplayTab.Decor: BuildDecor(); break;
            }
        }

        private void BuildBooths()
        {
            var sim = _root.Sim;
            foreach (var booth in sim.Booths)
            {
                var b = booth;
                Panels.Row(_content, _root.SelectedBooth == b ? Palette.Gold : Palette.Blue, (b.SlotIndex + 1).ToString(),
                    () => Loc.F("booth.name", b.SlotIndex + 1) + "  ·  " + Loc.F("common.level", b.Level),
                    () => Panels.BoothStatus(b) + "  ·  " + Loc.F("booth.queue", b.AssignedVehicles.Count, b.QueueCapacity) +
                          "  ·  " + (double.IsInfinity(b.ProcessingSeconds) ? "—" : $"{b.ProcessingSeconds:0.0}s"),
                    () => Loc.T(_root.SelectedBooth == b ? "booth.selected" : "booth.select"),
                    () => true,
                    () =>
                    {
                        _root.SelectBooth(b);
                        _ui.ShowBoothUpgrade(b);
                    },
                    Register, 130, Palette.Blue);
            }

            int next = sim.Booths.Count + 1;
            Panels.Row(_content, Palette.Green, "+",
                () => sim.Expansion.CanBuyBooth() == ExpansionBlock.MaxReached ? Loc.T("booth.max") : Loc.F("booth.build", next),
                () =>
                {
                    var block = sim.Expansion.CanBuyBooth();
                    if (block == ExpansionBlock.MaxReached) return "";
                    if (block == ExpansionBlock.NeedsMoreLanes) return Loc.F("booth.needs_lanes", sim.Expansion.LanesRequiredForNextBooth);
                    return Loc.T("booth.build.body");
                },
                () => sim.Expansion.CanBuyBooth() == ExpansionBlock.MaxReached ? Loc.T("upgrade.max") : Money.Format(sim.Expansion.NextBoothCost),
                () => sim.Expansion.CanBuyBooth() == ExpansionBlock.None,
                _ui.ConfirmBuildBooth,
                Register, 150);
        }

        private void BuildLanes()
        {
            var sim = _root.Sim;
            var header = Panels.Header(_content, "");
            Register(() => header.text = Loc.F("lane.count", sim.Lanes.ActiveLaneCount, sim.Config.MaxLanes) + "  ·  " +
                                         Loc.F("lane.demand", sim.Spawner.ArrivalsPerSecond));
            for (int i = 0; i < sim.Lanes.ActiveLaneCount; i++)
            {
                int lane = i;
                // Information card only: the action button is hidden so there is no dead control.
                var info = Panels.Row(_content, Palette.Asphalt, (lane + 1).ToString(),
                    () => Loc.F("lane.name", lane + 1),
                    () => Loc.F("hud.cars", sim.Lanes.Approach(lane).Vehicles.Count),
                    () => "", () => false, null, Register, 110, null, 30);
                info.gameObject.SetActive(false);
            }
            Panels.Row(_content, Palette.Green, "+",
                () => sim.Expansion.CanBuyLane() == ExpansionBlock.MaxReached ? Loc.T("lane.max") : Loc.F("lane.buy", sim.Expansion.LaneCount + 1),
                () => sim.Expansion.CanBuyLane() == ExpansionBlock.MaxReached ? "" : Loc.T("lane.buy.body"),
                () => sim.Expansion.CanBuyLane() == ExpansionBlock.MaxReached ? Loc.T("upgrade.max") : Money.Format(sim.Expansion.NextLaneCost),
                () => sim.Expansion.CanBuyLane() == ExpansionBlock.None,
                _ui.ConfirmBuyLane,
                Register, 160);
        }

        private void BuildWorkers()
        {
            var sim = _root.Sim;
            Panels.Row(_content, Palette.Green, "+",
                () => Loc.T("worker.hire"),
                () => Loc.F("worker.speed", sim.Workers.Config.ProcessingSecondsByLevel[0]),
                () => Money.Format(sim.Workers.HireCost),
                () => sim.Economy.CanAfford(sim.Workers.HireCost),
                () =>
                {
                    Worker hired = null;
                    if (_ui.TryPurchase(sim.Workers.HireCost, () => sim.Workers.TryHire(out hired)))
                    {
                        var target = _root.SelectedBooth;
                        if (target != null && target.Mode == BoothMode.Manual && target.AssignedWorker == null) sim.Workers.Assign(hired, target);
                        else
                        {
                            foreach (var b in sim.Booths)
                            {
                                if (b.Mode == BoothMode.Manual && b.AssignedWorker == null)
                                {
                                    sim.Workers.Assign(hired, b);
                                    break;
                                }
                            }
                        }
                    }
                },
                Register, 130);

            foreach (var worker in sim.Workers.Workers)
            {
                var w = worker;
                var upgrade = Panels.Row(_content, Palette.Gold, w.Id.ToString(),
                    () => Loc.F("worker.name", w.Id) + "  ·  " + Loc.F("worker.level", w.Level),
                    () => Loc.F("worker.speed", w.GetProcessingSeconds(sim.Workers.Config)) + "  ·  " + WorkerStatus(w),
                    () => sim.Workers.CanUpgrade(w) ? Money.Format(sim.Workers.UpgradeCost(w)) : Loc.T("worker.max"),
                    () => sim.Workers.CanUpgrade(w) && sim.Economy.CanAfford(sim.Workers.UpgradeCost(w)),
                    () => _ui.TryPurchase(sim.Workers.UpgradeCost(w), () => sim.Workers.TryUpgrade(w)),
                    Register, 150, null, 560);

                var assign = UIFactory.Button(upgrade.transform.parent, "", Palette.Blue, () => ToggleAssignment(w), 30);
                UIFactory.Place((RectTransform)assign.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-290, 0), new Vector2(250, 100));
                var assignText = UIFactory.ButtonText(assign);
                Register(() =>
                {
                    assignText.text = w.AssignedBooth != null ? Loc.T("worker.unassign") : Loc.T("worker.assign");
                    assign.interactable = w.AssignedBooth != null ? !w.AssignedBooth.PendingClose : CanAssignToSelected();
                });
            }
        }

        private bool CanAssignToSelected()
        {
            var b = _root.SelectedBooth;
            return b != null && b.Mode == BoothMode.Manual && b.AssignedWorker == null;
        }

        private void ToggleAssignment(Worker w)
        {
            var sim = _root.Sim;
            _root.Audio.Play(Sfx.Click);
            if (w.AssignedBooth != null)
            {
                sim.Workers.Unassign(w);
            }
            else if (_root.SelectedBooth == null)
            {
                _ui.Toast(Loc.T("toast.select_booth"));
            }
            else
            {
                var result = sim.Workers.Assign(w, _root.SelectedBooth);
                if (result != AssignResult.Assigned) _ui.Toast(Loc.T("toast.select_booth"));
            }
            MarkDirty();
        }

        private static string WorkerStatus(Worker w)
        {
            if (w.AssignedBooth == null) return Loc.T("worker.free");
            if (w.AssignedBooth.PendingClose) return Loc.T("worker.pending");
            return Loc.F("worker.at", w.AssignedBooth.SlotIndex + 1);
        }

        private void BuildServices()
        {
            var services = _root.Game.ActiveSection.Services;
            foreach (var placed in services.Placed)
            {
                var p = placed;
                Panels.Row(_content, Palette.Green, Loc.T(p.Config.NameKey).Substring(0, 1),
                    () => Loc.T(p.Config.NameKey) + "  ·  " + Loc.F("common.level", p.Level),
                    () => Loc.F("service.placed", p.Level, Money.Format(p.Earned)),
                    () => Loc.T("booth.select"),
                    () => true,
                    () => _ui.ShowServiceDetail(p.Slot),
                    Register, 130, Palette.Blue, 300, "Buildings/service_" + p.Config.Id);
            }
            _ui.BuildServiceCatalog(_content, Register, false);
        }

        private void BuildDecor()
        {
            var decorations = _root.Game.ActiveSection.Decorations;
            foreach (var placed in decorations.Placed)
            {
                var d = placed;
                Panels.Row(_content, Palette.Gold, Loc.T(d.Config.NameKey).Substring(0, 1),
                    () => Loc.T(d.Config.NameKey),
                    () => "",
                    () => Loc.T("booth.select"),
                    () => true,
                    () => _ui.ShowDecorationDetail(d.Slot),
                    Register, 110, Palette.Blue, 300, "Decorations/" + d.Config.Id);
            }
            _ui.BuildDecorationCatalog(_content, Register, false);
        }

        private void BuildUpgrade()
        {
            var booth = _root.SelectedBooth ?? _root.Sim.Booths[0];
            Panels.BuildBoothUpgrade(_ui, _content, booth, Register, 130);
            Panels.BuildVehicleUnlocks(_ui, _content, Register);
        }
    }
}
