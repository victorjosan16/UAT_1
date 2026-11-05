using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VoxelBloom.Core;

namespace VoxelBloom
{
    /// <summary>
    /// Drives one conveyor level: fixed 60 Hz logic ticks on ConveyorSession (deterministic, frame-rate independent),
    /// smooth interpolated belt rendering, visible cube transfers, tray placement flights, boosters, win/loss and saving.
    /// </summary>
    public sealed class ConveyorController : MonoBehaviour
    {
        public static ConveyorController Active { get; private set; }

        ConveyorScreen view;
        ConveyorLevel level;
        ConveyorSession session;
        Color[] colors;
        float acc;
        bool paused, finished, failShown;
        int shownCoins = -1;
        int hintTray = -2;
        int[] hint;

        sealed class Flight { public Image Img; public Vector2 From; public int Station, Compartment; public float T, Dur; }
        readonly List<Flight> flights = new List<Flight>();
        readonly Stack<Image> flightPool = new Stack<Image>();
        readonly Dictionary<int, float> feedPop = new Dictionary<int, float>();
        /// <summary>Released cubes: lifted from their compartment to the station's transfer point, waiting there for an empty slot.</summary>
        sealed class OutCube { public Image Img; public int Station; public Vector2 From; public float T; }
        readonly List<OutCube> outs = new List<OutCube>();

        public ConveyorSession Session => session;
        public ConveyorLevel Level => level;
        static float SpeedFactor => new[] { 1f, 1.5f, 2f }[Mathf.Clamp(Services.Save.Settings.FireSpeed, 0, 2)];

        public void Bind(ConveyorScreen v) { view = v; }

        // ------------------------------------------------------------------ entry
        public static int TotalLevels => Services.Data.ConveyorLevelCount;

        public static void StartLevel(int id)
        {
            var router = Router.Instance;
            if (!Services.Progress.IsConveyorUnlocked(id, TotalLevels)) { router.Toast("Nivelul " + id + " este blocat", Icon.Lock); return; }
            Services.Lives.Update(Services.Now);
            if (Services.Lives.Count <= 0) { router.Open<NoLivesDialog>(); return; }
            var def = Services.Data.GetConveyorLevel(id);
            if (def == null) { router.Toast("Lipsesc datele nivelului", Icon.Close); return; }
            var screen = router.Show<ConveyorScreen>();
            screen.GetComponent<ConveyorController>().Begin(def, Services.Save.ConveyorActive);
        }

        public void Begin(ConveyorLevel def, Dictionary<string, object> snapshot)
        {
            Active = this;
            level = def;
            session = (snapshot != null ? ConveyorSession.FromSnapshot(def, snapshot) : null) ?? new ConveyorSession(def, true, true);
            colors = new Color[def.Colors.Length];
            for (int i = 0; i < colors.Length; i++) colors[i] = Theme.Hex(def.Colors[i]);
            acc = 0; paused = finished = failShown = false; hintTray = -2;
            foreach (var f in flights) if (f.Img != null) Destroy(f.Img.gameObject);
            flights.Clear(); flightPool.Clear(); feedPop.Clear();
            foreach (var o in outs) if (o.Img != null) Destroy(o.Img.gameObject);
            outs.Clear();
            Time.timeScale = 1f;
            Services.Save.LastPlayedLevel = def.Id;
            view.BuildBoard(def);
            view.LevelText.text = "Nivel <color=#FFD84A>" + def.Id + "</color>";
            RefreshAll();
            App.State.ForceReset(GameState.Ready);
            if (def.Tutorial && !Services.Save.HowToPlaySeen)
            {
                paused = true; // "Cum se joacă?" before the first level
                Router.Instance.Open<HowToPlayDialog>(d => d.Closed = () => paused = false);
            }
        }

        // ------------------------------------------------------------------ input
        public void OnTrayTapped(int qi)
        {
            if (session == null || finished || paused) return;
            if (qi >= session.VisibleCount) return;
            if (!session.CanPlace(qi))
            {
                Services.Audio.Play(Sfx.Error);
                Router.Instance.Toast(session.HasLockedPlusSlot() ? "Nicio stație liberă - deschide o stație cu +" : "Nicio stație liberă momentan", Icon.Lock);
                return;
            }
            var tray = session.Queue[qi];
            var from = view.CardInSpace(qi);
            int st = session.Place(qi);
            hintTray = -2;
            session.ClearEvents();
            if (st < 0) return;
            Services.Audio.Play(Sfx.Whoosh, 1f + st * 0.03f);
            FlyTray(from, st, tray);
            RefreshQueue(); RefreshStations(); RefreshHud();
            SaveProgress();
        }

        /// <summary>Tap on a BUS: it lets one wrong cube out onto the belt (only with free space).</summary>
        public void OnStationTapped(int k)
        {
            if (session == null || finished || paused) return;
            var st = session.Stations[k];
            if (!st.IsOpen) return;
            if (!session.CanRelease(k))
            {
                Services.Audio.Play(Sfx.Error);
                Router.Instance.Toast(st.Tray.WrongIndex() < 0 ? "Acest BUS are doar cuburi potrivite" : "Nu mai este spațiu pe bandă", Icon.Info);
                return;
            }
            session.Release(k);
            hintTray = -2;
            ProcessEvents();
            Services.Audio.Play(Sfx.Whoosh, 1.2f);
            RefreshHud();
            SaveProgress();
        }

        void FlyTray(Vector2 from, int station, ConveyorTray tray)
        {
            var sv = view.Stations[station];
            sv.HiddenUntil = Time.time + 0.28f;
            var ghost = UI.Image(view.FlyLayer, "TrayFlight", SpriteFactory.Rounded(24), ConveyorScreen.TrayRim);
            ghost.rectTransform.anchorMin = ghost.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            var to = sv.Tray.anchoredPosition;
            var size0 = new Vector2(110, 190); var size1 = sv.Tray.sizeDelta;
            Tween.Value(0.28f, k =>
            {
                if (ghost == null) return;
                ghost.rectTransform.anchoredPosition = Vector2.Lerp(from, to, k) + new Vector2(0, Mathf.Sin(k * Mathf.PI) * 60f);
                ghost.rectTransform.sizeDelta = Vector2.Lerp(size0, size1, k);
            }, Ease.OutCubic, () => { if (ghost != null) Destroy(ghost.gameObject); if (sv.Root != null) Tween.Pop(sv.Root, 0.06f, 0.2f); });
        }

        // ------------------------------------------------------------------ loop
        void Update()
        {
            if (session == null || view == null || view.SlotCubes == null) return;
            if (!paused && !finished)
            {
                acc += Time.deltaTime * ConveyorSession.TicksPerSecond * SpeedFactor;
                int steps = 0;
                while (acc >= 1f && steps < 12)
                {
                    session.Step();
                    acc -= 1f; steps++;
                    ProcessEvents();
                    if (session.IsWon) break;
                }
                if (steps >= 12) acc = 0f; // hitch: never spiral
                if (session.IsWon) { finished = true; Invoke(nameof(Win), 0.6f); }
                else if (!failShown && session.IsStuck()) Fail();
            }
            float alpha = paused || finished ? 0f : Mathf.Clamp01(acc);
            DrawBelt(alpha);
            UpdateFlights();
            UpdateOuts();
            UpdateStationsVisual();
            UpdateHint();
            int c = Services.Wallet.Coins;
            if (c != shownCoins) { shownCoins = c; RefreshHud(); }
        }

        void ProcessEvents()
        {
            if (session.Events == null) return;
            foreach (var e in session.Events)
            {
                switch (e.Type)
                {
                    case ConveyorEventType.Take:
                        RefreshHud();
                        var img = flightPool.Count > 0 ? flightPool.Pop() : NewCubeImage(view.FlyLayer);
                        img.gameObject.SetActive(true);
                        img.color = colors[e.Color];
                        flights.Add(new Flight
                        {
                            Img = img, From = view.ToUi(view.Board.At(session.SlotPos(e.Slot))), Station = e.Station, Compartment = e.Compartment,
                            T = 0f, Dur = ConveyorSession.Transfer / (ConveyorSession.TicksPerSecond * SpeedFactor),
                        });
                        break;
                    case ConveyorEventType.Lift:
                    {
                        var oimg = flightPool.Count > 0 ? flightPool.Pop() : NewCubeImage(view.FlyLayer);
                        oimg.gameObject.SetActive(true);
                        oimg.color = colors[e.Color];
                        var def = level.Stations[e.Station];
                        outs.Add(new OutCube { Img = oimg, Station = e.Station, From = view.ToUi(view.Board.Compartment(def, Mathf.Min(e.Compartment, def.Capacity - 1))), T = 0f });
                        break;
                    }
                    case ConveyorEventType.Drop:
                    {
                        // the waiting cube enters the belt: the belt slot now draws it (it grows in like a fed cube)
                        int idx = outs.FindIndex(o => o.Station == e.Station);
                        if (idx >= 0) { outs[idx].Img.gameObject.SetActive(false); flightPool.Push(outs[idx].Img); outs.RemoveAt(idx); }
                        feedPop[e.Slot] = Time.time - 0.12f;
                        RefreshHud();
                        break;
                    }
                    case ConveyorEventType.Land:
                        Services.Audio.Play(Sfx.Pop, 1f + e.Compartment * 0.08f, 0.7f);
                        Services.Events.OnCubesDelivered(1); // "Misiune Specială"
                        Services.Save.CubesDelivered++;
                        RefreshStation(e.Station);
                        var cell = view.Stations[e.Station].Cells[Mathf.Clamp(e.Compartment, 0, view.Stations[e.Station].Cells.Length - 1)];
                        Tween.Pop(cell.transform, 0.25f, 0.18f);
                        break;
                    case ConveyorEventType.Complete:
                        ShowCheck(e.Station);
                        Services.Audio.Play(Sfx.Star, 1.1f);
                        Haptics.Light();
                        Burst(e.Station);
                        RefreshHud();
                        RefreshStations();
                        break;
                    case ConveyorEventType.Leave:
                        RefreshStation(e.Station);
                        RefreshQueue();
                        break;
                    case ConveyorEventType.Feed:
                        feedPop[e.Slot] = Time.time;
                        RefreshHud();
                        break;
                }
            }
            session.ClearEvents();
        }

        Image NewCubeImage(Transform parent) => ConveyorScreen.NewCube(parent, view.K);

        void DrawBelt(float alpha)
        {
            float shift = alpha * session.V;
            for (int k = 0; k < session.C; k++)
            {
                var img = view.SlotCubes[k];
                int col = session.Slot[k];
                if (col < 0) { if (img.enabled) img.enabled = false; continue; }
                if (!img.enabled) img.enabled = true;
                img.color = colors[col];
                img.rectTransform.anchoredPosition = view.ToUi(view.Board.At(session.SlotPos(k) + shift));
                if (feedPop.TryGetValue(k, out var t0))
                {
                    float f = (Time.time - t0) / 0.25f;
                    if (f >= 1f) { feedPop.Remove(k); img.rectTransform.localScale = Vector3.one; }
                    else img.rectTransform.localScale = Vector3.one * Ease.OutBack(Mathf.Clamp01(f));
                }
            }
        }

        void UpdateFlights()
        {
            for (int i = flights.Count - 1; i >= 0; i--)
            {
                var f = flights[i];
                if (!paused) f.T += Time.deltaTime;
                float k = Mathf.Clamp01(f.T / f.Dur);
                var def = level.Stations[f.Station];
                var to = view.ToUi(view.Board.Compartment(def, Mathf.Min(f.Compartment, def.Capacity - 1)));
                float e = Ease.InOutQuad(k);
                f.Img.rectTransform.anchoredPosition = Vector2.Lerp(f.From, to, e) + new Vector2(0, Mathf.Sin(k * Mathf.PI) * 0.5f * view.K);
                f.Img.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(k * Mathf.PI) * 0.18f);
                if (k >= 1f) { f.Img.gameObject.SetActive(false); flightPool.Push(f.Img); flights.RemoveAt(i); }
            }
        }

        void UpdateOuts()
        {
            var perStation = new Dictionary<int, int>();
            foreach (var o in outs)
            {
                if (!paused) o.T += Time.deltaTime;
                int n = perStation.TryGetValue(o.Station, out var c) ? c : 0; perStation[o.Station] = n + 1;
                // the transfer point on the belt, nudged towards the inside; later cubes queue behind the first
                var s = level.Stations[o.Station].S;
                var at = view.Board.At(s); var tan = view.Board.Tangent(s);
                var inward = new Vector2(-tan.y, tan.x);
                var to = view.ToUi(at - tan * (0.9f * n) + inward * 0.15f);
                float k = Mathf.Clamp01(o.T / 0.22f), e = Ease.OutCubic(k);
                o.Img.rectTransform.anchoredPosition = Vector2.Lerp(o.From, to, e) + new Vector2(0, Mathf.Sin(k * Mathf.PI) * 0.4f * view.K);
                o.Img.rectTransform.localScale = Vector3.one * (1f + 0.08f * Mathf.Sin(Time.time * 10f) * (k >= 1f ? 1f : 0f));
            }
        }

        void ShowCheck(int station)
        {
            var sv = view.Stations[station];
            var ok = UI.Icon(view.FxLayer, Icon.Check2, 1.4f * view.K);
            ok.rectTransform.anchorMin = ok.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            ok.rectTransform.anchoredPosition = sv.Tray.anchoredPosition;
            Tween.Scale(ok.transform, Vector3.zero, Vector3.one, 0.3f, Ease.OutBack);
            Tween.Delay(0.55f, () => { if (ok != null) Destroy(ok.gameObject); });
        }

        void Burst(int station)
        {
            if (Services.ReducedEffects) return;
            var centre = view.Stations[station].Tray.anchoredPosition;
            var col = colors[session.Stations[station].Tray?.Need[0] ?? 0];
            for (int i = 0; i < 10; i++)
            {
                var sp = UI.Image(view.FxLayer, "Spark", SpriteFactory.Glow(), Color.Lerp(col, Color.white, 0.4f), false);
                sp.rectTransform.anchorMin = sp.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                sp.rectTransform.sizeDelta = Vector2.one * 0.5f * view.K;
                float ang = i * Mathf.PI * 2f / 10f;
                var dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 1.6f * view.K;
                Tween.Value(0.45f, k =>
                {
                    if (sp == null) return;
                    sp.rectTransform.anchoredPosition = centre + dir * k;
                    sp.color = new Color(sp.color.r, sp.color.g, sp.color.b, 1f - k);
                }, Ease.OutCubic, () => { if (sp != null) Destroy(sp.gameObject); });
            }
        }

        // ------------------------------------------------------------------ view refresh
        void RefreshAll() { RefreshStations(); RefreshQueue(); RefreshHud(); }

        void RefreshStations() { for (int i = 0; i < session.Stations.Length; i++) RefreshStation(i); }

        void RefreshStation(int i)
        {
            var st = session.Stations[i]; var v = view.Stations[i];
            if (v.LockTile != null) v.LockTile.gameObject.SetActive(st.Lock == StationLock.Completed);
            if (v.LockText != null) v.LockText.text = Mathf.Max(1, st.LockN - session.Completed).ToString();
            if (v.PlusTile != null) v.PlusTile.gameObject.SetActive(st.Lock == StationLock.Booster);
        }

        void UpdateStationsVisual()
        {
            for (int i = 0; i < session.Stations.Length; i++)
            {
                var st = session.Stations[i]; var v = view.Stations[i];
                float a = 1f;
                if (st.Leave > 0) a = Mathf.Clamp01((st.Leave - acc) / ConveyorSession.LeaveTicks); // completed tray fades out
                bool hidden = Time.time < v.HiddenUntil;
                for (int c = 0; c < v.Cells.Length; c++)
                {
                    var cell = v.Cells[c]; var plate = v.Plates[c];
                    if (hidden || st.Tray == null || c >= st.Tray.Need.Length) { cell.color = Color.clear; plate.color = Color.clear; continue; }
                    var need = Color.Lerp(colors[st.Tray.Need[c]], Color.white, 0.2f);
                    plate.color = new Color(need.r, need.g, need.b, 0.55f * a);          // required colour
                    int have = st.Tray.Have[c];
                    if (have >= 0) { var col = colors[have]; cell.color = new Color(col.r, col.g, col.b, a); } // cube in the compartment
                    else cell.color = Color.clear;                                       // empty or a cube on its way
                    cell.rectTransform.localScale = Vector3.one * (have >= 0 && have != st.Tray.Need[c] ? 0.92f : 1.12f); // wrong cubes show the required rim
                }
                bool open = st.Lock == StationLock.None;
                v.Rim.color = open ? ConveyorScreen.TrayRim : new Color(1, 1, 1, 0);
                v.Inner.color = open ? ConveyorScreen.TrayInner : new Color(1, 1, 1, 0);
            }
        }

        void RefreshQueue()
        {
            for (int i = 0; i < view.Cards.Count; i++)
            {
                var card = view.Cards[i];
                bool has = i < session.VisibleCount;
                card.Group.alpha = has ? 1f : 0.35f;
                card.Button.Interactable = has;
                for (int r = 0; r < 4; r++)
                {
                    var cell = card.Cells[r];
                    if (!has || r >= session.Queue[i].Need.Length) { cell.color = Color.clear; continue; }
                    int h = session.Queue[i].Have[r];
                    cell.color = h >= 0 ? colors[h] : Color.Lerp(colors[session.Queue[i].Need[r]], new Color(1, 1, 1, 0), 0.55f);
                }
            }
            int hidden = session.Queue.Count - session.VisibleCount;
            view.QueueMore.text = hidden > 0 ? "+" + hidden + " BUS-uri în așteptare" : "";
        }

        void RefreshHud()
        {
            // HUD pill = space on the belt (X/8); the objective is shown separately (8/8 is never a win)
            int occ = session.Occupied, space = session.Space;
            view.ProgressText.text = occ + "/" + space;
            view.Progress.Set(space == 0 ? 0f : Mathf.Clamp01((float)occ / space));
            var fill = view.Progress.Fill.GetComponent<Image>();
            if (fill != null) fill.color = occ >= space ? Theme.Hex("#FF4D5E") : occ >= space - 1 ? Theme.Hex("#FFB13B") : Color.white;
            view.SpaceWarning.text = occ >= space ? "Nu mai este spațiu" : occ >= space - 1 ? "Aproape plin!" : "";
            view.SpaceWarning.color = occ >= space ? Theme.Hex("#FF5A6A") : Theme.Hex("#FFC24A");
            view.ObjectiveText.text = "BUS-uri completate: " + session.Completed + "/" + session.Total;
            view.FeederText.text = session.HopperLeft > 0 ? "+" + session.HopperLeft : "";
            for (int i = 0; i < 3; i++)
            {
                int n = Services.Boosters.Count(ConveyorScreen.BoosterOrder[i]);
                view.BoosterCounts[i].text = n > 0 ? n.ToString() : "+";
            }
        }

        void UpdateHint()
        {
            // tutorial (level 1): point at the best action (a BUS to release or a queued BUS) for the first moves
            bool show = level.Tutorial && !finished && session.Released + session.Placed < 3;
            if (!show) { if (view.HintArrow.gameObject.activeSelf) view.HintArrow.gameObject.SetActive(false); view.HintText.text = ""; return; }
            if (Time.frameCount % 15 == 0 || hintTray < -1) { hint = ConveyorSolver.Hint(session); hintTray = -1; } // allocates: not every frame
            view.HintArrow.gameObject.SetActive(hint != null);
            view.HintText.text = session.Released == 0
                ? "Atinge un BUS: cubul greșit iese pe bandă\nși ajunge la BUS-ul care cere culoarea lui."
                : "Bravo! Contorul de sus arată\nspațiul ocupat pe bandă.";
            if (hint == null) return;
            if (hint[1] == ConveyorSession.ActRelease)
            {
                var p = view.QueueRoot.InverseTransformPoint(view.Stations[hint[2]].Tray.position);
                view.HintArrow.anchoredPosition = (Vector2)p + new Vector2(0, 90 + Mathf.Sin(Time.time * 6f) * 14f);
            }
            else if (hint[2] < view.Cards.Count)
                view.HintArrow.anchoredPosition = view.Cards[hint[2]].Root.anchoredPosition + new Vector2(0, 150 + Mathf.Sin(Time.time * 6f) * 14f);
        }

        // ------------------------------------------------------------------ boosters
        public void UseBooster(BoosterType t)
        {
            if (session == null || finished || paused) return;
            if (Services.Boosters.Count(t) <= 0) { OfferBooster(t); return; }
            bool ok;
            switch (t)
            {
                case BoosterType.Shuffle:
                    ok = session.Shuffle(new DeterministicRandom(level.Id * 7919 + session.Tick));
                    if (!ok) Router.Instance.Toast("Nu există o altă ordine bună acum", Icon.Refresh);
                    break;
                case BoosterType.ColorShuffle:
                    ok = session.ShuffleColors(new DeterministicRandom(level.Id * 104729 + session.Tick));
                    if (!ok) Router.Instance.Toast("Nu există o altă aranjare bună acum", Icon.ColorSwap);
                    break;
                case BoosterType.ExtraStation:
                    ok = session.ExtraStation();
                    if (!ok) Router.Instance.Toast("Nu există sloturi + pe acest nivel", Icon.CubesPlus);
                    break;
                default: ok = false; break;
            }
            if (!ok) return; // never consume a booster on a failed action
            Services.Boosters.TryConsume(t);
            Services.Audio.Play(Sfx.Booster);
            session.ClearEvents();
            foreach (var f in flights) { f.Img.gameObject.SetActive(false); flightPool.Push(f.Img); }
            flights.Clear();
            failShown = false;
            RefreshAll();
            Services.Commit();
            SaveProgress();
        }

        void OfferBooster(BoosterType t)
        {
            paused = true;
            Router.Instance.Open<BoostersInfoDialog>(d => { d.Changed = RefreshHud; d.Closed = () => paused = false; });
        }

        public static string BoosterName(BoosterType t) => t == BoosterType.Shuffle ? "Reîmprospătează" : t == BoosterType.ColorShuffle ? "Amestecă culorile" : t == BoosterType.ExtraStation ? "Adaugă o tavă" : t == BoosterType.Undo ? "Anulează mutarea" : t.ToString();

        // ------------------------------------------------------------------ win / loss
        void Win()
        {
            var receipt = Services.Progress.CompleteConveyor(level, session.BoostersUsed, TotalLevels);
            Services.Events.OnLevelCompleted(receipt.Stars, Services.Now); // "Provocarea Zilnică" and the season
            Services.Commit();
            Services.Audio.Play(Sfx.Win);
            Router.Instance.Open<ConveyorWinDialog>(d => { d.Receipt = receipt; d.Level = level; });
        }

        void Fail()
        {
            failShown = true;
            Services.Audio.Play(Sfx.Fail);
            SaveProgress();
            Router.Instance.Open<ConveyorFailDialog>(d => d.Controller = this);
        }

        /// <summary>Continue after a deadlock with a booster (Undo or Extra station); returns false if not possible.</summary>
        public bool ContinueWith(BoosterType t)
        {
            if (Services.Boosters.Count(t) <= 0)
            {
                if (Services.Boosters.Buy(t, Services.Config, Services.Wallet) != PurchaseResult.Success) { Router.Instance.Toast("Nu ai destule monede", Icon.Coin); return false; }
            }
            int before = Services.Boosters.Count(t);
            UseBooster(t);
            return Services.Boosters.Count(t) < before;
        }

        public bool CanUndo => session != null && session.UndoCount > 0;
        public bool HasPlusSlot => session != null && session.HasLockedPlusSlot();

        public void AcceptFailure()
        {
            if (finished) return;
            finished = true;
            Services.Lives.TryConsume(Services.Now);
            Services.Save.ConveyorActive = null;
            Services.Commit();
        }

        // ------------------------------------------------------------------ pause / restart / quit / save
        public void Pause()
        {
            if (session == null || finished || paused || Router.Instance.HasDialog) return;
            paused = true;
            Router.Instance.Open<ConveyorPauseDialog>(d => d.Controller = this);
        }

        public void Resume() { paused = false; }

        public void Restart()
        {
            if (!finished && session != null && session.Placed > 0) Services.Lives.TryConsume(Services.Now);
            Services.Save.ConveyorActive = null;
            Services.Commit();
            Services.Lives.Update(Services.Now);
            if (Services.Lives.Count <= 0) { QuitToMap(); Router.Instance.Open<NoLivesDialog>(); return; }
            Begin(level, null);
        }

        public void QuitToMap()
        {
            if (!finished) SaveProgress();
            paused = false;
            Router.Instance.Show<MapScreen>();
        }

        public void NextLevel()
        {
            int next = level.Id + 1;
            if (next <= TotalLevels && Services.Progress.IsConveyorUnlocked(next, TotalLevels)) StartLevel(next);
            else Router.Instance.Show<MapScreen>();
        }

        public void OnScreenHidden()
        {
            if (!finished && session != null) SaveProgress();
            if (Active == this) Active = null;
        }

        public void SaveProgress()
        {
            if (session == null || finished || session.IsWon) return;
            Services.Save.ConveyorActive = session.ToSnapshot();
            Services.Commit();
        }
    }
}
