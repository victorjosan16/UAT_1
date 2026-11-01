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

        sealed class Flight { public Image Img; public Vector2 From; public int Station, Compartment; public float T, Dur; }
        readonly List<Flight> flights = new List<Flight>();
        readonly Stack<Image> flightPool = new Stack<Image>();
        readonly Dictionary<int, float> feedPop = new Dictionary<int, float>();

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
            acc = 0; paused = finished = failShown = false;
            foreach (var f in flights) if (f.Img != null) Destroy(f.Img.gameObject);
            flights.Clear(); flightPool.Clear(); feedPop.Clear();
            Time.timeScale = 1f;
            Services.Save.LastPlayedLevel = def.Id;
            view.BuildBoard(def);
            view.LevelText.text = "Nivel <color=#FFD84A>" + def.Id + "</color>";
            RefreshAll();
            App.State.ForceReset(GameState.Ready);
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
            session.ClearEvents();
            if (st < 0) return;
            Services.Audio.Play(Sfx.Whoosh, 1f + st * 0.03f);
            FlyTray(from, st, tray);
            RefreshQueue(); RefreshStations(); RefreshHud();
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
                        var img = flightPool.Count > 0 ? flightPool.Pop() : NewCubeImage(view.FlyLayer);
                        img.gameObject.SetActive(true);
                        img.color = colors[e.Color];
                        flights.Add(new Flight
                        {
                            Img = img, From = view.ToUi(view.Board.At(session.SlotPos(e.Slot))), Station = e.Station, Compartment = e.Compartment,
                            T = 0f, Dur = ConveyorSession.Transfer / (ConveyorSession.TicksPerSecond * SpeedFactor),
                        });
                        break;
                    case ConveyorEventType.Land:
                        Services.Audio.Play(Sfx.Pop, 1f + e.Compartment * 0.08f, 0.7f);
                        RefreshStation(e.Station);
                        var cell = view.Stations[e.Station].Cells[Mathf.Clamp(e.Compartment, 0, view.Stations[e.Station].Cells.Length - 1)];
                        Tween.Pop(cell.transform, 0.25f, 0.18f);
                        break;
                    case ConveyorEventType.Complete:
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

        Image NewCubeImage(Transform parent)
        {
            var c = UI.Image(parent, "Cube", SpriteFactory.Glossy(18), Color.white, true);
            c.pixelsPerUnitMultiplier = 18f / (0.2f * view.K);
            c.rectTransform.anchorMin = c.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            c.rectTransform.sizeDelta = Vector2.one * 0.8f * view.K;
            return c;
        }

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

        void Burst(int station)
        {
            if (Services.ReducedEffects) return;
            var centre = view.Stations[station].Tray.anchoredPosition;
            var col = colors[session.Stations[station].Tray?.Colors[0] ?? 0];
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
                    var cell = v.Cells[c];
                    if (hidden || st.Tray == null || c >= st.Tray.Colors.Length) { cell.color = Color.clear; continue; }
                    var col = colors[st.Tray.Colors[c]];
                    if (c >= st.Tray.Filled) col = Color.Lerp(col, Color.white, 0.25f); // ghost = still required (lighter, half transparent)
                    cell.color = new Color(col.r, col.g, col.b, (c < st.Tray.Filled ? 1f : 0.5f) * a);
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
                    if (!has || r >= session.Queue[i].Colors.Length) { cell.color = Color.clear; continue; }
                    cell.color = colors[session.Queue[i].Colors[r]];
                }
            }
            int hidden = session.Queue.Count - session.VisibleCount;
            view.QueueMore.text = hidden > 0 ? "+" + hidden + " tăvi în așteptare" : "";
        }

        void RefreshHud()
        {
            view.ProgressText.text = session.Completed + "/" + session.Total;
            view.Progress.Set(session.Total == 0 ? 1f : (float)session.Completed / session.Total);
            view.FeederText.text = session.HopperLeft > 0 ? "+" + session.HopperLeft : "";
            for (int i = 0; i < 3; i++)
            {
                int n = Services.Boosters.Count(ConveyorScreen.BoosterOrder[i]);
                view.BoosterCounts[i].text = n > 0 ? n.ToString() : "+";
            }
        }

        void UpdateHint()
        {
            // tutorial (level 1): point at the best tray until two trays are placed
            bool show = level.Tutorial && !finished && session.Placed < 2;
            int best = show ? ConveyorSolver.Hint(session) : -1;
            view.HintArrow.gameObject.SetActive(best >= 0);
            view.HintText.text = show ? (session.Placed == 0 ? "Atinge o tavă de jos.\nCuburile de aceeași culoare\no umplu de pe bandă." : "Bravo! Mai trimite una.") : "";
            if (best >= 0)
            {
                var card = view.Cards[best].Root;
                view.HintArrow.anchoredPosition = card.anchoredPosition + new Vector2(0, 150 + Mathf.Sin(Time.time * 6f) * 14f);
            }
        }

        // ------------------------------------------------------------------ boosters
        public void UseBooster(BoosterType t)
        {
            if (session == null || finished || paused) return;
            if (Services.Boosters.Count(t) <= 0) { OfferBooster(t); return; }
            bool ok;
            switch (t)
            {
                case BoosterType.Undo:
                    ok = session.UndoLast();
                    if (!ok) Router.Instance.Toast("Nu ai ce anula încă", Icon.Restart);
                    break;
                case BoosterType.Shuffle:
                    ok = session.Shuffle(new DeterministicRandom(level.Id * 7919 + session.Tick));
                    if (!ok) Router.Instance.Toast("Nu există o altă ordine bună acum", Icon.Shuffle);
                    break;
                case BoosterType.ExtraStation:
                    ok = session.ExtraStation();
                    if (!ok) Router.Instance.Toast("Nu există stații + pe acest nivel", Icon.Plus);
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
            int price = Services.Config.Booster(t).Price;
            Router.Instance.Open<ConfirmDialog>(d =>
            {
                d.Message = BoosterName(t) + "\nCumperi 1 pentru " + price + " monede?";
                d.YesLabel = "Cumpără";
                d.OnYes = () =>
                {
                    if (Services.Boosters.Buy(t, Services.Config, Services.Wallet) != PurchaseResult.Success) { Router.Instance.Toast("Nu ai destule monede", Icon.Coin); return; }
                    Services.Commit();
                    RefreshHud();
                };
            });
        }

        public static string BoosterName(BoosterType t) => t == BoosterType.Undo ? "Anulează mutarea" : t == BoosterType.Shuffle ? "Amestecă tăvile" : t == BoosterType.ExtraStation ? "Stație extra" : t.ToString();

        // ------------------------------------------------------------------ win / loss
        void Win()
        {
            var receipt = Services.Progress.CompleteConveyor(level, session.BoostersUsed, TotalLevels);
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
            Router.Instance.Show<ConveyorLevelsScreen>();
        }

        public void NextLevel()
        {
            int next = level.Id + 1;
            if (next <= TotalLevels && Services.Progress.IsConveyorUnlocked(next, TotalLevels)) StartLevel(next);
            else Router.Instance.Show<ConveyorLevelsScreen>();
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
