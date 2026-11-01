using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelBloom.Core;

namespace VoxelBloom
{
    /// <summary>
    /// Drives one level: applies player input to the authoritative GameSession, sequences attack
    /// animations through the state machine, handles boosters, win/fail and progress saving.
    /// Game logic is applied immediately; visuals catch up (input can never desync the rules).
    /// </summary>
    public sealed class GameplayController : MonoBehaviour
    {
        public static GameplayController Active { get; private set; }
        static GameStateMachine SM => App.State;

        GameplayScreen view;
        LevelDefinition level;
        GameSession session;
        Color32[] palette;
        Coroutine resolveRoutine;
        bool paused, finished, failShown;
        BoosterType? targeting;
        int shownCoins = -1;

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
            Time.timeScale = 1f;
            Active = this;
            SM.ForceReset(GameState.LevelLoading);
            level = def;
            session = snapshot != null ? GameSession.FromSnapshot(def, snapshot) : null;
            if (session == null) session = new GameSession(def);
            palette = VoxelMeshBuilder.ToColors(def.Grid.Palette);
            paused = finished = failShown = false;
            targeting = null;
            Services.Save.LastPlayedLevel = def.Id;

            view.Stage.ShowModel(session.CellsSnapshot(), def.Grid.X, def.Grid.Y, def.Grid.Z, palette, 2.1f);
            view.Stage.TargetYaw = view.Stage.Yaw = -30f;
            view.Stage.TurntableSpeed = Services.Save.Settings.AutoRotate ? (Services.ReducedEffects ? 8f : 14f) : 0f; // 360° turntable while playing
            view.Stage.Pivot.localScale = Vector3.one;
            view.ModelInput.OnTapUv = OnModelTapped;
            view.LevelText.text = "Lv. " + def.Id;
            view.HardBadge.gameObject.SetActive(def.Hard || Services.Progress.IsHard(def.Id));
            view.HintText.text = "";
            view.Progress.Set(session.Progress, true);
            RefreshAll();
            SM.TryTransition(GameState.Ready);
            // a restored session may still have queued attacks to resolve
            if (session.Queue.Count > 0) StartResolve();
        }

        // ------------------------------------------------------------------ input
        public void OnBlockTapped(int slot)
        {
            if (session == null || finished || paused) return;
            if (targeting == BoosterType.ColorBomb)
            {
                var b = session.BoardAt(slot);
                if (b.HasValue) ApplyColorBomb(b.Value.Color);
                return;
            }
            if (!SM.AcceptsBoardInput) return;
            if (!session.CanTap(slot))
            {
                if (session.Queue.Count >= session.QueueSize) { Services.Audio.Play(Sfx.Error); view.FloatText("Queue full!", Theme.Red, new Vector2(0, -200)); }
                return;
            }
            var block = session.BoardAt(slot).Value;
            int targetSlot = session.Queue.Count;
            bool wasReady = SM.Current == GameState.Ready;
            if (wasReady) { SM.TryTransition(GameState.BlockSelected); SM.TryTransition(GameState.AttackQueued); }
            session.Tap(slot);
            Services.Audio.Play(Sfx.Launch, 1f + targetSlot * 0.05f);
            FlyBlock(view.Board[slot], targetSlot, block);
            view.Board[slot].Set(session.BoardAt(slot), palette);
            Tween.Scale(view.Board[slot].Root, Vector3.one * 0.4f, Vector3.one, 0.25f, Ease.OutBack);
            RefreshHud();
            StartResolve();
        }

        /// <summary>Press-and-hold on a board block: show on the model exactly which voxels it would remove.</summary>
        public void PreviewBlock(int slot)
        {
            if (session == null || finished || paused) return;
            var b = session.BoardAt(slot);
            if (!b.HasValue) return;
            var targets = session.PreviewTargets(b.Value.Color, b.Value.Power);
            if (targets.Count == 0) { view.HintText.text = "No reachable voxels of this colour yet"; return; }
            view.Stage.Model.SetHighlight(targets, Theme.Lighter(palette[b.Value.Color], 0.55f));
            view.HintText.text = targets.Count + " voxels will be removed";
        }

        public void ClearPreview()
        {
            if (view == null) return;
            view.Stage.Model.ClearHighlight();
            if (targeting == null) view.HintText.text = "";
        }

        public void OnQueueTapped(int index)
        {
            if (targeting == BoosterType.Eliminate) ApplyEliminate(index);
        }

        /// <summary>World point just below the model view, horizontally aligned with the queue slot.</summary>
        Vector3 QueueOriginWorld(int queueIndex)
        {
            var cam = view.Stage.Camera;
            float x = 0.5f + (Mathf.Clamp(queueIndex, 0, 4) - 2) * 0.17f;
            float dist = Vector3.Distance(cam.transform.position, view.Stage.Model.transform.position);
            return cam.ViewportToWorldPoint(new Vector3(x, -0.08f, dist * 0.8f));
        }

        void FlyBlock(BlockView from, int queueIndex, AttackBlock block)
        {
            var layer = view.Root;
            var ghost = BlockView.Create(layer, from.Root.rect.size, "Flying");
            ghost.Set(block, palette);
            ghost.Button.Interactable = false;
            var start = (Vector2)layer.InverseTransformPoint(from.Root.position);
            var end = (Vector2)layer.InverseTransformPoint(view.QueueSlots[Mathf.Clamp(queueIndex, 0, 4)].position);
            ghost.Root.anchorMin = ghost.Root.anchorMax = new Vector2(0.5f, 0.5f);
            ghost.Root.anchoredPosition = start;
            var mid = (start + end) * 0.5f + new Vector2(0, 120);
            Tween.Value(0.22f, k =>
            {
                if (ghost.Root == null) return;
                var a = Vector2.Lerp(start, mid, k); var b2 = Vector2.Lerp(mid, end, k);
                ghost.Root.anchoredPosition = Vector2.Lerp(a, b2, k);
                ghost.Root.localScale = Vector3.one * Mathf.Lerp(1f, 0.85f, k);
            }, Ease.InOutQuad, () =>
            {
                if (ghost.Root != null) Destroy(ghost.Root.gameObject);
                RefreshQueue();
            });
        }

        // ------------------------------------------------------------------ resolution
        void StartResolve()
        {
            if (resolveRoutine == null && !finished) resolveRoutine = StartCoroutine(ResolveLoop());
        }

        IEnumerator ResolveLoop()
        {
            yield return new WaitForSeconds(0.2f); // let the block land in the queue
            while (true)
            {
                while (paused) yield return null;
                var r = session.ResolveNext();
                if (r == null) break;
                if (SM.Current != GameState.Attacking) SM.TryTransition(GameState.Attacking);
                yield return AnimateAttack(r);
                if (session.IsWon) break;
            }
            while (paused) yield return null;
            SM.TryTransition(GameState.Resolving);
            resolveRoutine = null;
            Settle();
        }

        IEnumerator AnimateAttack(AttackResult r)
        {
            RefreshQueue();
            if (r.Wasted)
            {
                view.FloatText("Wasted", Theme.InkSoft, new Vector2(0, -160));
                yield return new WaitForSeconds(0.12f);
                yield break;
            }
            var color = palette[r.Attack.Color];
            var model = view.Stage.Model;
            int n = r.Destroyed.Count;
            // 1) show where the colour goes: targets pulse and coloured cubes fly from the queue slot to them
            model.SetHighlight(r.Destroyed, Theme.Lighter(color, 0.6f));
            Vector3 origin = QueueOriginWorld(r.QueueIndex);
            int shotsEvery = Mathf.Max(1, n / (Services.ReducedEffects ? 6 : 24));
            const float flight = 0.28f;
            for (int k = 0; k < n; k += shotsEvery)
                view.Stage.Fragments.Shoot(origin, model.VoxelWorldPosition(r.Destroyed[k]), color, model.VoxelSize, flight, k * 0.004f);
            Services.Audio.Play(Sfx.Launch, 1.1f);
            yield return new WaitForSeconds(flight);
            model.ClearHighlight();
            // 2) pop the voxels
            Services.Audio.Play(Sfx.Pop, 1f + Mathf.Min(r.Combo, 5) * 0.06f);
            int bursts = view.Stage.Fragments.MaxPerBurst;
            float duration = Mathf.Lerp(0.15f, 0.35f, Mathf.Clamp01(n / 64f));
            int perStep = Mathf.Max(1, Mathf.CeilToInt(n / (duration / 0.03f)));
            int every = Mathf.Max(1, n / Mathf.Max(1, bursts));
            for (int k = 0; k < n; k++)
            {
                int idx = r.Destroyed[k];
                if (k % every == 0) view.Stage.Fragments.Burst(model.VoxelWorldPosition(idx), color, model.VoxelSize, 2);
                model.RemoveVoxel(idx);
                if ((k + 1) % perStep == 0) yield return new WaitForSeconds(0.03f);
            }
            // 3) detached pieces fall (physics debris) - no part stays floating
            if (r.Dropped.Count > 0)
            {
                foreach (int idx in r.Dropped)
                {
                    view.Stage.Debris.Spawn(model.VoxelWorldPosition(idx), model.ColorOf(level.Grid.Get(idx)), model.VoxelSize);
                    model.RemoveVoxel(idx);
                }
                Services.Audio.Play(Sfx.Whoosh, 0.8f);
            }
            view.Progress.Set(session.Progress);
            RefreshHud();
            if (r.Combo >= 2)
            {
                view.ShowCombo(r.Combo);
                Services.Audio.Play(Sfx.Combo, 1f + (r.Combo - 2) * 0.08f);
                view.Stage.Shake(0.2f + r.Combo * 0.08f);
                if (r.Combo >= 3) Haptics.Light();
            }
            if (r.ColorCleared)
            {
                view.FloatText("Colour cleared!", Color.white, new Vector2(0, -100));
                foreach (int s in r.RefilledSlots) Tween.Pop(view.Board[s].Root, 0.2f);
            }
            RefreshAll();
            yield return new WaitForSeconds(0.08f);
        }

        void Settle()
        {
            RefreshAll();
            if (session.IsWon) { StartCoroutine(Win()); return; }
            var fail = session.GetFailReason();
            if (fail != FailReason.None) { Fail(fail); return; }
            SM.TryTransition(GameState.Ready);
            SaveProgress();
        }

        IEnumerator Win()
        {
            finished = true;
            SM.TryTransition(GameState.LevelComplete);
            var receipt = Services.Progress.CompleteLevel(level, session.Moves, session.Score, session.MaxCombo);
            Services.Commit();
            Services.Audio.Play(Sfx.Win);
            // celebration: show the intact model spinning with confetti
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
            SM.TryTransition(GameState.LevelFailed);
            Services.Audio.Play(Sfx.Fail);
            SaveProgress();
            Router.Instance.Open<LevelFailedDialog>(d => { d.Reason = reason; d.Controller = this; });
        }

        /// <summary>Called by the fail dialog when the player gives up (retry/home): costs one life, once.</summary>
        public void AcceptFailure()
        {
            if (finished) return;
            finished = true;
            Services.Lives.TryConsume(Services.Now);
            Services.Save.ActiveLevel = null;
            Services.Commit();
        }

        /// <summary>Continue after a queue-full failure by eliminating a stuck attack.</summary>
        public bool ContinueWithEliminate()
        {
            if (finished || session.GetFailReason() != FailReason.QueueFull) return false;
            int idx = session.FirstStuckQueueIndex();
            if (idx < 0) idx = 0;
            if (idx >= session.Queue.Count || !Services.Boosters.TryConsume(BoosterType.Eliminate)) return false;
            session.ApplyEliminate(idx);
            failShown = false;
            SM.ForceReset(GameState.Ready);
            RefreshAll();
            StartResolve();
            return true;
        }

        // ------------------------------------------------------------------ boosters
        public void UseBooster(BoosterType t)
        {
            if (session == null || finished || paused) return;
            if (!BoosterAllowed(t)) { Router.Instance.Toast("This booster unlocks in a later level", Icon.Lock); return; }
            if (targeting == t) { CancelTargeting(); return; }
            if (Services.Boosters.Count(t) <= 0)
            {
                Router.Instance.Open<BoostersDialog>(d => d.Focus = t);
                return;
            }
            if (resolveRoutine != null) { Router.Instance.Toast("Wait for the attacks to finish"); return; }
            switch (t)
            {
                case BoosterType.Hammer: BeginTargeting(t, "Tap the model to smash voxels"); break;
                case BoosterType.ColorBomb: BeginTargeting(t, "Tap a block or a voxel to pick a colour"); break;
                case BoosterType.Eliminate:
                    if (session.Queue.Count == 0) { Router.Instance.Toast("The queue is empty", Icon.Eliminate); return; }
                    int stuck = session.FirstStuckQueueIndex();
                    if (stuck >= 0 && session.Queue.Count == 1) ApplyEliminate(stuck);
                    else BeginTargeting(t, "Tap a queued attack to eliminate it");
                    break;
                case BoosterType.Shuffle:
                    if (session.ApplyShuffle(new DeterministicRandom(level.Seed + session.Moves * 31 + Services.Boosters.Count(t))))
                    {
                        Services.Boosters.TryConsume(t);
                        for (int s = 0; s < GameSession.BoardSize; s++) Tween.Pop(view.Board[s].Root, 0.15f);
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
            view.Stage.NotifyInteraction(30f); // hold still while the player aims
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
                if (destroyed.Count > 0 && Services.Boosters.TryConsume(BoosterType.Hammer)) StartCoroutine(AnimateBooster(destroyed, Sfx.Booster));
                CancelTargeting();
            }
            else if (targeting == BoosterType.ColorBomb)
            {
                ApplyColorBomb(session.CellColor(voxel));
            }
        }

        void ApplyColorBomb(int color)
        {
            if (color < 0) return;
            int count = Mathf.Max(1, Services.Config.Booster(BoosterType.ColorBomb).Voxels);
            var destroyed = session.ApplyColorBomb(color, count);
            if (destroyed.Count > 0 && Services.Boosters.TryConsume(BoosterType.ColorBomb)) StartCoroutine(AnimateBooster(destroyed, Sfx.Booster));
            else if (destroyed.Count == 0) Router.Instance.Toast("No reachable voxels of that colour yet");
            CancelTargeting();
        }

        void ApplyEliminate(int index)
        {
            if (index < 0 || index >= session.Queue.Count) return;
            if (!Services.Boosters.TryConsume(BoosterType.Eliminate)) return;
            session.ApplyEliminate(index);
            CancelTargeting();
            Services.Audio.Play(Sfx.Whoosh);
            Tween.Pop(view.QueueSlots[Mathf.Min(index, 4)], 0.25f);
            RefreshAll();
            SaveProgress();
            StartResolve();
        }

        IEnumerator AnimateBooster(List<int> destroyed, Sfx sfx)
        {
            Services.Audio.Play(sfx);
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
            if (session.IsWon) { SM.ForceReset(GameState.Resolving); StartCoroutine(Win()); }
            else { StartResolve(); SaveProgress(); }
        }

        // ------------------------------------------------------------------ pause / restart / quit
        public void Pause()
        {
            if (session == null || finished || paused || Router.Instance.HasDialog) return;
            if (!SM.TryTransition(GameState.Paused)) return;
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
            if (!SM.Resume()) SM.ForceReset(resolveRoutine != null ? GameState.Attacking : GameState.Ready);
        }

        public void Restart()
        {
            paused = false;
            Time.timeScale = 1f;
            Services.Audio.PauseMusic(false);
            if (resolveRoutine != null) { StopCoroutine(resolveRoutine); resolveRoutine = null; }
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
            if (resolveRoutine != null) { StopCoroutine(resolveRoutine); resolveRoutine = null; }
            if (!finished) SaveProgress();
            SM.ForceReset(GameState.Home);
            var grid = Router.Instance.Show<LevelGridScreen>();
            var ch = Services.Config.ChapterOf(level.Id);
            if (ch != null) grid.SetChapter(ch);
        }

        public void OnScreenHidden()
        {
            StopAllCoroutines();
            resolveRoutine = null;
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
        void RefreshAll()
        {
            for (int s = 0; s < GameSession.BoardSize; s++)
            {
                var b = session.BoardAt(s);
                view.Board[s].Set(b, palette);
                if (b.HasValue) view.Board[s].SetPlayable(session.ExposedCount(b.Value.Color) > 0);
            }
            RefreshQueue();
            RefreshHud();
        }

        void RefreshQueue()
        {
            int stuck = -1;
            for (int i = 0; i < 5; i++)
            {
                bool has = i < session.Queue.Count && i < session.QueueSize;
                view.QueueBlocks[i].Set(has ? session.Queue[i] : (AttackBlock?)null, palette);
                view.QueueSlots[i].gameObject.SetActive(i < session.QueueSize);
                if (has && session.ExposedCount(session.Queue[i].Color) == 0 && stuck < 0) stuck = i;
            }
            for (int i = 0; i < 5; i++)
            {
                var img = view.QueueSlots[i].GetComponent<UnityEngine.UI.Image>();
                img.color = i == stuck ? Theme.Lighter(Theme.Red, 0.55f) : Theme.SlotEmpty;
            }
        }

        void RefreshHud()
        {
            view.MovesText.text = session.MovesLeft.ToString();
            view.ProgressText.text = Mathf.FloorToInt(session.Progress * 100f) + "%";
            view.RemainingText.text = session.TotalRemaining + " voxels left  ·  " + session.DeckRemaining + " blocks in deck";
            int projected = session.Moves <= level.StarsThree ? 3 : session.Moves <= level.StarsTwo ? 2 : 1;
            for (int i = 0; i < 3; i++) view.Stars[i].sprite = SpriteFactory.Get(i < projected ? Icon.Star : Icon.StarEmpty);
            for (int i = 0; i < 4; i++)
            {
                var t = GameplayScreen.BoosterOrder[i];
                int n = Services.Boosters.Count(t);
                view.BoosterCounts[i].text = n > 0 ? n.ToString() : "+";
                view.BoosterButtons[i].SetInteractable(BoosterAllowed(t));
            }
        }

        void Update()
        {
            if (view == null || session == null) return;
            int c = Services.Wallet.Coins;
            if (c != shownCoins) { shownCoins = c; view.CoinsText.text = c.ToString("N0"); RefreshHud(); }
        }
    }
}
