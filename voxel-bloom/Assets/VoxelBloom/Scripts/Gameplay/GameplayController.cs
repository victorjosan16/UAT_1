using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelBloom.Core;

namespace VoxelBloom
{
    /// <summary>
    /// Drives one level (rules v2): tapping a column moves its top block into a free launcher; launchers fire
    /// glowing projectiles every tick at their colour; a launcher whose colour is not visible yet waits.
    /// Logic is applied immediately on the GameSession; visuals (projectile flight, voxel pop) follow.
    /// </summary>
    public sealed class GameplayController : MonoBehaviour
    {
        public static GameplayController Active { get; private set; }
        static GameStateMachine SM => App.State;
        const float Tick = 0.07f, Flight = 0.24f;

        GameplayScreen view;
        LevelDefinition level;
        GameSession session;
        Color32[] palette;
        Coroutine fireRoutine;
        bool paused, finished, failShown;
        BoosterType? targeting;
        int shownCoins = -1;
        float lastComboFx;
        readonly List<(float at, int voxel, Color32 color)> pendingPops = new List<(float, int, Color32)>();

        public LevelDefinition Level => level;
        public GameSession Session => session;

        public void Bind(GameplayScreen v) { view = v; }

        // ------------------------------------------------------------------ entry
        public static void StartLevel(int id)
        {
            var router = Router.Instance;
            if (!Services.Progress.IsLevelUnlocked(id)) { router.Toast("Level " + id + " is locked", Icon.Lock); return; }
            Services.Lives.Update(Services.Now);
            if (Services.Lives.Count <= 0) { router.Open<NoLivesDialog>(); return; }
            var def = Services.Data.GetLevel(id);
            if (def == null) { router.Toast("Level data missing", Icon.Close); return; }
            var screen = router.Show<GameplayScreen>();
            var c = screen.GetComponent<GameplayController>();
            var snap = Services.Save.ActiveLevel;
            c.Begin(def, snap != null && MiniJson.GetInt(snap, "level") == id ? snap : null);
        }

        public void Begin(LevelDefinition def, Dictionary<string, object> snapshot)
        {
            StopAllCoroutines();
            fireRoutine = null;
            pendingPops.Clear();
            Time.timeScale = 1f;
            Active = this;
            SM.ForceReset(GameState.LevelLoading);
            level = def;
            session = (snapshot != null ? GameSession.FromSnapshot(def, snapshot) : null) ?? new GameSession(def);
            palette = VoxelMeshBuilder.ToColors(def.Grid.Palette);
            paused = finished = failShown = false;
            targeting = null;
            Services.Save.LastPlayedLevel = def.Id;

            view.Stage.ShowModel(session.CellsSnapshot(), def.Grid.X, def.Grid.Y, def.Grid.Z, palette, 2.1f);
            view.Stage.TargetYaw = view.Stage.Yaw = -30f;
            view.Stage.TurntableSpeed = Services.Save.Settings.AutoRotate ? (Services.ReducedEffects ? 8f : 14f) : 0f;
            view.Stage.Pivot.localScale = Vector3.one;
            view.ModelInput.OnTapUv = OnModelTapped;
            view.LevelText.text = "Lv. " + def.Id;
            view.HardBadge.gameObject.SetActive(def.Hard || Services.Progress.IsHard(def.Id));
            view.HintText.text = "";
            view.Progress.Set(session.Progress, true);
            RefreshAll();
            SM.TryTransition(GameState.Ready);
            StartFiring();
        }

        // ------------------------------------------------------------------ input
        public void OnColumnTapped(int col)
        {
            if (session == null || finished || paused) return;
            var top = session.Top(col);
            if (targeting == BoosterType.ColorBomb) { if (top.HasValue) ApplyColorBomb(top.Value.Color); return; }
            if (!SM.AcceptsBoardInput) return;
            if (!session.CanTap(col))
            {
                if (session.FreeLauncher() < 0) { Services.Audio.Play(Sfx.Error); view.FloatText("All launchers busy!", Theme.Red, new Vector2(0, -200)); }
                return;
            }
            var block = top.Value;
            int li = session.Tap(col);
            Services.Audio.Play(Sfx.Launch, 1f + li * 0.05f);
            FlyBlock(view.Board[col * GameplayScreen.VisibleRows], li, block);
            RefreshBoard();
            RefreshHud();
            if (SM.Current == GameState.Ready) SM.TryTransition(GameState.Attacking);
            StartFiring();
        }

        /// <summary>Press-and-hold a column: highlight the voxels its top block will remove first.</summary>
        public void PreviewColumn(int col)
        {
            if (session == null || finished || paused) return;
            var b = session.Top(col);
            if (!b.HasValue) return;
            var targets = session.PreviewTargets(b.Value.Color, b.Value.Power);
            if (targets.Count == 0) { view.HintText.text = "This colour is not visible yet - it will wait"; return; }
            view.Stage.Model.SetHighlight(targets, Theme.Lighter(palette[b.Value.Color], 0.55f));
            view.HintText.text = b.Value.Power + " voxels of this colour";
        }

        public void ClearPreview()
        {
            if (view == null) return;
            view.Stage.Model.ClearHighlight();
            if (targeting == null) view.HintText.text = "";
        }

        public void OnLauncherTapped(int li)
        {
            if (targeting == BoosterType.Eliminate) ApplyEliminate(li);
        }

        void FlyBlock(BlockView from, int launcher, AttackBlock block)
        {
            var layer = view.Root;
            var ghost = BlockView.Create(layer, from.Root.rect.size, "Flying");
            ghost.Set(block, palette);
            ghost.Button.Interactable = false;
            var start = (Vector2)layer.InverseTransformPoint(from.Root.position);
            var end = (Vector2)layer.InverseTransformPoint(view.LauncherSlots[launcher].position);
            ghost.Root.anchorMin = ghost.Root.anchorMax = new Vector2(0.5f, 0.5f);
            ghost.Root.anchoredPosition = start;
            Tween.Value(0.2f, k =>
            {
                if (ghost.Root == null) return;
                ghost.Root.anchoredPosition = Vector2.Lerp(start, end, k) + new Vector2(0, Mathf.Sin(k * Mathf.PI) * 60f);
                ghost.Root.localScale = Vector3.one * Mathf.Lerp(1f, 0.85f, k);
            }, Ease.InOutQuad, () =>
            {
                if (ghost.Root != null) Destroy(ghost.Root.gameObject);
                RefreshLaunchers();
                Tween.Pop(view.LauncherSlots[launcher], 0.15f);
            });
        }

        // ------------------------------------------------------------------ firing loop
        void StartFiring()
        {
            if (fireRoutine == null && !finished) fireRoutine = StartCoroutine(FireLoop());
        }

        IEnumerator FireLoop()
        {
            yield return new WaitForSeconds(0.15f);
            while (!finished)
            {
                while (paused) yield return null;
                var r = session.Step();
                if (r == null) break;
                AnimateRound(r);
                if (session.IsWon) break;
                yield return new WaitForSeconds(Tick);
            }
            fireRoutine = null;
            yield return new WaitForSeconds(Flight + 0.05f);
            Settle();
        }

        void AnimateRound(RoundResult r)
        {
            var model = view.Stage.Model;
            foreach (var (li, voxel) in r.Shots)
            {
                var c = palette[level.Grid.Get(voxel)];
                view.Stage.Fragments.Shoot(LauncherOriginWorld(li), model.VoxelWorldPosition(voxel), c, model.VoxelSize * 1.1f, Flight);
                pendingPops.Add((Time.time + Flight, voxel, c));
                var g = view.LauncherGlow[li];
                g.color = new Color(c.r / 255f, c.g / 255f, c.b / 255f, 0.9f);
            }
            if (r.Shots.Count > 0) Services.Audio.Play(Sfx.Pop, 0.9f + r.Shots.Count * 0.08f, 0.6f);
            foreach (int idx in r.Dropped)
            {
                view.Stage.Debris.Spawn(model.VoxelWorldPosition(idx), model.ColorOf(level.Grid.Get(idx)), model.VoxelSize);
                model.RemoveVoxel(idx);
            }
            foreach (int li in r.Freed) Tween.Pop(view.LauncherSlots[li], 0.2f);
            if (r.Combo >= 3 && Time.time - lastComboFx > 1.2f)
            {
                lastComboFx = Time.time;
                view.ShowCombo(r.Combo);
                Services.Audio.Play(Sfx.Combo, 1f + (r.Combo - 3) * 0.1f);
                view.Stage.Shake(0.25f);
                if (r.Combo >= 4) Haptics.Light();
            }
            view.Progress.Set(session.Progress);
            RefreshLaunchers();
            if (r.Freed.Count > 0 || r.Dropped.Count > 0) RefreshBoard();
            RefreshHud();
        }

        void Update()
        {
            // voxels pop when their projectile arrives
            for (int i = pendingPops.Count - 1; i >= 0; i--)
            {
                if (Time.time < pendingPops[i].at) continue;
                var p = pendingPops[i];
                pendingPops.RemoveAt(i);
                if (view == null) continue;
                var model = view.Stage.Model;
                if (Random.value < 0.5f) view.Stage.Fragments.Burst(model.VoxelWorldPosition(p.voxel), p.color, model.VoxelSize, 2);
                model.RemoveVoxel(p.voxel);
            }
            // waiting launchers pulse red, firing ones fade their glow
            if (session != null && view != null)
            {
                for (int li = 0; li < GameSession.Launchers; li++)
                {
                    var g = view.LauncherGlow[li];
                    if (session.IsWaiting(li)) g.color = new Color(1f, 0.3f, 0.35f, 0.35f + 0.3f * Mathf.Sin(Time.unscaledTime * 6f));
                    else g.color = new Color(g.color.r, g.color.g, g.color.b, Mathf.MoveTowards(g.color.a, 0f, Time.deltaTime * 2.5f));
                }
                int c = Services.Wallet.Coins;
                if (c != shownCoins) { shownCoins = c; view.CoinsText.text = c.ToString("N0"); RefreshHud(); }
            }
        }

        /// <summary>World point just below the model view, aligned with the launcher on screen.</summary>
        Vector3 LauncherOriginWorld(int li)
        {
            var cam = view.Stage.Camera;
            float x = 0.5f + (Mathf.Clamp(li, 0, 3) - 1.5f) * 0.22f;
            float dist = Vector3.Distance(cam.transform.position, view.Stage.Model.transform.position);
            return cam.ViewportToWorldPoint(new Vector3(x, -0.06f, dist * 0.8f));
        }

        void Settle()
        {
            RefreshAll();
            if (session.IsWon) { StartCoroutine(Win()); return; }
            var fail = session.GetFailReason();
            if (fail != FailReason.None) { Fail(fail); return; }
            if (SM.Current == GameState.Attacking) { SM.TryTransition(GameState.Resolving); SM.TryTransition(GameState.Ready); }
            SaveProgress();
        }

        IEnumerator Win()
        {
            finished = true;
            SM.ForceReset(GameState.LevelComplete);
            var receipt = Services.Progress.CompleteLevel(level, session.Moves, session.Score, session.MaxCombo);
            Services.Commit();
            Services.Audio.Play(Sfx.Win);
            yield return new WaitForSecondsRealtime(0.35f);
            view.Stage.ShowModel(level.Grid.CopyCells(), level.Grid.X, level.Grid.Y, level.Grid.Z, palette, 2.1f);
            view.Stage.TurntableSpeed = 0f;
            view.Stage.AutoRotateSpeed = 90f;
            for (int i = 0; i < (Services.ReducedEffects ? 6 : 30); i++)
            {
                int idx = Random.Range(0, level.Grid.Length);
                if (level.Grid.Get(idx) < 0) continue;
                view.Stage.Fragments.Burst(view.Stage.Model.VoxelWorldPosition(idx), palette[level.Grid.Get(idx)], view.Stage.Model.VoxelSize, 3);
            }
            yield return new WaitForSecondsRealtime(1.1f);
            view.Stage.AutoRotateSpeed = 0f;
            Router.Instance.Open<LevelCompleteDialog>(d => { d.Receipt = receipt; d.Level = level; });
        }

        void Fail(FailReason reason)
        {
            if (failShown) return;
            failShown = true;
            SM.ForceReset(GameState.LevelFailed);
            Services.Audio.Play(Sfx.Fail);
            SaveProgress();
            Router.Instance.Open<LevelFailedDialog>(d => { d.Reason = reason; d.Controller = this; });
        }

        public void AcceptFailure()
        {
            if (finished) return;
            finished = true;
            Services.Lives.TryConsume(Services.Now);
            Services.Save.ActiveLevel = null;
            Services.Commit();
        }

        /// <summary>Continue after a jam by eliminating a waiting launcher (consumes one Eliminare).</summary>
        public bool ContinueWithEliminate()
        {
            if (finished || session.GetFailReason() != FailReason.Jammed) return false;
            int li = session.FirstWaitingLauncher();
            if (li < 0 || !Services.Boosters.TryConsume(BoosterType.Eliminate)) return false;
            session.ApplyEliminate(li);
            failShown = false;
            SM.ForceReset(GameState.Ready);
            RefreshAll();
            StartFiring();
            return true;
        }

        // ------------------------------------------------------------------ boosters
        public void UseBooster(BoosterType t)
        {
            if (session == null || finished || paused) return;
            if (!BoosterAllowed(t)) { Router.Instance.Toast("This booster unlocks in a later level", Icon.Lock); return; }
            if (targeting == t) { CancelTargeting(); return; }
            if (Services.Boosters.Count(t) <= 0) { Router.Instance.Open<BoostersDialog>(d => d.Focus = t); return; }
            switch (t)
            {
                case BoosterType.Hammer: BeginTargeting(t, "Tap the model to smash voxels"); break;
                case BoosterType.ColorBomb: BeginTargeting(t, "Tap a top block or a voxel to pick a colour"); break;
                case BoosterType.Eliminate:
                    int waiting = session.FirstWaitingLauncher();
                    if (waiting < 0) { Router.Instance.Toast("No launcher is waiting", Icon.Eliminate); return; }
                    BeginTargeting(t, "Tap a launcher to empty it");
                    break;
                case BoosterType.Shuffle:
                    if (session.ApplyShuffle(new DeterministicRandom(level.Seed + session.Moves * 31 + Services.Boosters.Count(t))))
                    {
                        Services.Boosters.TryConsume(t);
                        foreach (var b in view.Board) Tween.Pop(b.Root, 0.15f);
                        Services.Audio.Play(Sfx.Whoosh);
                        RefreshAll();
                        SaveProgress();
                    }
                    break;
            }
        }

        bool BoosterAllowed(BoosterType t)
        {
            var b = level.Boosters;
            return t == BoosterType.Hammer ? b.Hammer : t == BoosterType.Shuffle ? b.Shuffle : t == BoosterType.ColorBomb ? b.ColorBomb : b.Eliminate;
        }

        void BeginTargeting(BoosterType t, string hint)
        {
            targeting = t;
            view.Stage.NotifyInteraction(30f);
            view.HintText.text = hint;
            Tween.Pop(view.HintText.transform, 0.2f);
        }

        void CancelTargeting()
        {
            targeting = null;
            view.Stage.NotifyInteraction(1.5f);
            view.HintText.text = "";
        }

        void OnModelTapped(Vector2 uv)
        {
            if (targeting == null || finished || paused) return;
            var ray = view.Stage.Camera.ViewportPointToRay(new Vector3(uv.x, uv.y, 0));
            int voxel = view.Stage.Model.Raycast(ray);
            if (voxel < 0) return;
            if (targeting == BoosterType.Hammer)
            {
                int count = Mathf.Max(1, Services.Config.Booster(BoosterType.Hammer).Voxels);
                var destroyed = session.ApplyHammer(voxel, count);
                if (destroyed.Count > 0 && Services.Boosters.TryConsume(BoosterType.Hammer)) StartCoroutine(AnimateBooster(destroyed));
                CancelTargeting();
            }
            else if (targeting == BoosterType.ColorBomb) ApplyColorBomb(session.CellColor(voxel));
        }

        void ApplyColorBomb(int color)
        {
            if (color < 0) return;
            int count = Mathf.Max(1, Services.Config.Booster(BoosterType.ColorBomb).Voxels);
            var destroyed = session.ApplyColorBomb(color, count);
            if (destroyed.Count > 0 && Services.Boosters.TryConsume(BoosterType.ColorBomb)) StartCoroutine(AnimateBooster(destroyed));
            else if (destroyed.Count == 0) Router.Instance.Toast("That colour is not visible yet");
            CancelTargeting();
        }

        void ApplyEliminate(int li)
        {
            if (!session.LauncherAt(li).HasValue) return;
            if (!Services.Boosters.TryConsume(BoosterType.Eliminate)) return;
            session.ApplyEliminate(li);
            CancelTargeting();
            Services.Audio.Play(Sfx.Whoosh);
            Tween.Pop(view.LauncherSlots[li], 0.25f);
            RefreshAll();
            SaveProgress();
            StartFiring();
        }

        IEnumerator AnimateBooster(List<int> destroyed)
        {
            Services.Audio.Play(Sfx.Booster);
            view.Stage.Shake(0.4f);
            for (int k = 0; k < destroyed.Count; k++)
            {
                int idx = destroyed[k];
                var c = view.Stage.Model.ColorOf(level.Grid.Get(idx));
                if (k % 2 == 0) view.Stage.Fragments.Burst(view.Stage.Model.VoxelWorldPosition(idx), c, view.Stage.Model.VoxelSize, 2);
                view.Stage.Model.RemoveVoxel(idx);
                if (k % 4 == 3) yield return new WaitForSeconds(0.02f);
            }
            view.Progress.Set(session.Progress);
            RefreshAll();
            if (session.IsWon) StartCoroutine(Win());
            else { StartFiring(); SaveProgress(); }
        }

        // ------------------------------------------------------------------ pause / restart / quit
        public void Pause()
        {
            if (session == null || finished || paused || Router.Instance.HasDialog) return;
            if (!SM.TryTransition(GameState.Paused)) SM.ForceReset(GameState.Paused);
            paused = true;
            Time.timeScale = 0f;
            Services.Audio.PauseMusic(true);
            Router.Instance.Open<PauseDialog>(d => d.Controller = this);
        }

        public void Resume()
        {
            if (!paused) return;
            paused = false;
            Time.timeScale = 1f;
            Services.Audio.PauseMusic(false);
            if (!SM.Resume()) SM.ForceReset(fireRoutine != null ? GameState.Attacking : GameState.Ready);
        }

        public void Restart()
        {
            paused = false;
            Time.timeScale = 1f;
            Services.Audio.PauseMusic(false);
            if (!finished && session != null && session.Moves > 0) Services.Lives.TryConsume(Services.Now);
            Services.Save.ActiveLevel = null;
            Services.Commit();
            Services.Lives.Update(Services.Now);
            if (Services.Lives.Count <= 0) { QuitToMap(); Router.Instance.Open<NoLivesDialog>(); return; }
            Begin(level, null);
        }

        public void QuitToMap()
        {
            paused = false;
            Time.timeScale = 1f;
            Services.Audio.PauseMusic(false);
            StopAllCoroutines();
            fireRoutine = null;
            if (!finished) SaveProgress();
            SM.ForceReset(GameState.Home);
            var grid = Router.Instance.Show<LevelGridScreen>();
            var ch = Services.Config.ChapterOf(level.Id);
            if (ch != null) grid.SetChapter(ch);
        }

        public void OnScreenHidden()
        {
            StopAllCoroutines();
            fireRoutine = null;
            Time.timeScale = 1f;
            if (Active == this) Active = null;
        }

        public void SaveProgress()
        {
            if (session == null || finished || session.IsWon) return;
            Services.Save.ActiveLevel = session.ToSnapshot();
            Services.Commit();
        }

        // ------------------------------------------------------------------ view refresh
        void RefreshAll() { RefreshBoard(); RefreshLaunchers(); RefreshHud(); }

        void RefreshBoard()
        {
            for (int col = 0; col < GameSession.Columns; col++)
                for (int row = 0; row < GameplayScreen.VisibleRows; row++)
                {
                    var bv = view.Board[col * GameplayScreen.VisibleRows + row];
                    var b = session.BlockAt(col, row);
                    bv.Set(b, palette);
                    if (b.HasValue) bv.SetPlayable(session.ExposedCount(b.Value.Color) > 0, row);
                }
        }

        void RefreshLaunchers()
        {
            for (int li = 0; li < GameSession.Launchers; li++) view.LauncherBlocks[li].Set(session.LauncherAt(li), palette);
        }

        void RefreshHud()
        {
            view.MovesText.text = session.BlocksLeft.ToString();
            view.ProgressText.text = Mathf.FloorToInt(session.Progress * 100f) + "%";
            view.RemainingText.text = session.TotalRemaining + " voxels left  ·  " + session.BlocksLeft + " blocks";
            int projected = level.StarsForScore(session.Score);
            for (int i = 0; i < 3; i++) view.Stars[i].sprite = SpriteFactory.Get(i < projected ? Icon.Star : Icon.StarEmpty);
            for (int i = 0; i < 4; i++)
            {
                var t = GameplayScreen.BoosterOrder[i];
                int n = Services.Boosters.Count(t);
                view.BoosterCounts[i].text = n > 0 ? n.ToString() : "+";
                view.BoosterButtons[i].SetInteractable(BoosterAllowed(t));
            }
        }
    }
}
