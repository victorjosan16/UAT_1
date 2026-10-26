using System;
using UndergroundMetroTycoon.Simulation;
using UnityEngine;

namespace UndergroundMetroTycoon.Game
{
    /// <summary>
    /// Automatic digging: when the player stands still next to a valid cell it reserves the best target,
    /// turns toward it, plays the swing and applies damage exactly at the blade-impact moment.
    /// </summary>
    [DefaultExecutionOrder(10)]
    public sealed class DigController : MonoBehaviour
    {
        private const float HintInterval = 5f;

        private GameSession _session;
        private PlayerController _player;
        private ExcavationSiteView _siteView;
        private int _target = -1;
        private float _phase;
        private bool _swinging;
        private bool _impactDone;
        private float _hintCooldown;

        /// <summary>Raised when the player is next to cells that cannot be dug yet, with the reason.</summary>
        public event Action<DigOutcome> BlockedHint;

        public void Init(GameSession session, PlayerController player, ExcavationSiteView siteView)
        {
            _session = session;
            _player = player;
            _siteView = siteView;
        }

        /// <summary>Drops the current target, e.g. after the site was rebuilt.</summary>
        public void ResetTarget()
        {
            ReleaseTarget();
            _swinging = false;
            _phase = 0f;
            if (_player != null && _player.Rig != null) _player.Rig.Digging = false;
        }

        private void Update()
        {
            if (_session == null || _player == null) return;
            float dt = Time.deltaTime;
            _hintCooldown -= dt;
            ToolDefinition tool = _session.EquippedTool;
            CharacterRig rig = _player.Rig;

            if (_player.HasMoveInput)
            {
                ResetTarget();
                _siteView.SetHighlight(-1);
                return;
            }

            if (_target >= 0 && _session.Excavation.Validate(_target, tool, GameSession.PlayerActorId) != DigOutcome.Ok) ReleaseTarget();
            if (_target < 0 && !_swinging) AcquireTarget(tool);

            if (_target < 0 && !_swinging)
            {
                rig.Digging = false;
                _siteView.SetHighlight(-1);
                return;
            }

            _siteView.SetHighlight(_target);
            bool facing = true;
            if (_target >= 0)
            {
                Vector3 top = _session.Grid.Layout.CellTopCenter(_session.Grid.GetCell(_target).Coord).ToUnity();
                facing = _player.FaceTowards(top, dt);
            }

            if (!_swinging)
            {
                if (!facing) return;
                _swinging = true;
                _impactDone = false;
                _phase = 0f;
            }

            rig.Digging = true;
            _phase += dt * tool.SwingsPerSecond;
            rig.DigPhase = Mathf.Min(_phase, 0.999f);

            if (!_impactDone && _phase >= CharacterRig.ImpactPhase)
            {
                _impactDone = true;
                if (_target >= 0)
                {
                    DigResult result = _session.Dig(_target, GameSession.PlayerActorId);
                    if (result.Outcome == DigOutcome.Excavated || !result.Succeeded) ReleaseTarget();
                }
            }

            if (_phase >= 1f)
            {
                _swinging = false;
                _phase = 0f;
                rig.DigPhase = 0f;
            }
        }

        private void AcquireTarget(ToolDefinition tool)
        {
            Vector3 feet = _player.transform.position;
            int best = _session.Excavation.FindBestTarget(feet.ToSim(), _player.transform.forward.ToSim(), tool.Reach, tool, GameSession.PlayerActorId);
            if (best >= 0 && _session.Excavation.TryReserve(best, GameSession.PlayerActorId))
            {
                _target = best;
                return;
            }

            if (_hintCooldown > 0f) return;
            int near = _session.Excavation.FindNearestExposedCell(feet.ToSim(), tool.Reach);
            if (near < 0) return;
            DigOutcome reason = _session.Excavation.Validate(near, tool, GameSession.PlayerActorId);
            if (reason == DigOutcome.Ok) return;
            _hintCooldown = HintInterval;
            BlockedHint?.Invoke(reason);
        }

        private void ReleaseTarget()
        {
            if (_target >= 0) _session.Excavation.Release(_target, GameSession.PlayerActorId);
            _target = -1;
        }
    }
}
