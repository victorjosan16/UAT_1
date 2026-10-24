using System.Collections.Generic;
using UnityEngine;

namespace LastBlock.Gameplay
{
    /// <summary>
    /// Watches tower blocks and decides when the tower has collapsed.
    ///
    /// A block counts as fallen when, for at least <see cref="fallenConfirmTime"/> seconds, it has
    /// dropped more than half a block height below its settled position, tilted more than
    /// <see cref="tiltThresholdDegrees"/>, or left the play area. Extracted and currently dragged blocks
    /// are ignored. Horizontal sliding alone is not a fall (blocks are legitimately pushed and pulled).
    /// The tower collapses when more blocks have fallen than the level allows.
    /// </summary>
    public class TowerStabilityChecker : MonoBehaviour
    {
        [SerializeField, Range(0.1f, 2f)] float dropThresholdFraction = 0.5f;
        [SerializeField] float tiltThresholdDegrees = 30f;
        [SerializeField] float fallenConfirmTime = 0.25f;
        [SerializeField] float killHeight = -1.5f;
        [Header("Settled check")]
        [SerializeField] float settledLinearSpeed = 0.05f;
        [SerializeField] float settledAngularSpeed = 0.2f;

        readonly List<BlockController> blocks = new List<BlockController>();
        readonly Dictionary<BlockController, Vector3> snapshot = new Dictionary<BlockController, Vector3>();
        float blockHeight = 0.3f;
        int allowedFallen;

        /// <summary>True once a baseline has been captured; collapse is only evaluated while monitoring.</summary>
        public bool IsMonitoring { get; private set; }
        public bool IsCollapsed { get; private set; }
        public int FallenCount { get; private set; }

        /// <summary>Starts tracking a new tower. Monitoring begins after <see cref="CaptureBaseline"/>.</summary>
        public void Begin(IReadOnlyList<BlockController> towerBlocks, float towerBlockHeight, int allowedFallenBlocks)
        {
            blocks.Clear();
            snapshot.Clear();
            if (towerBlocks != null) blocks.AddRange(towerBlocks);
            blockHeight = Mathf.Max(0.01f, towerBlockHeight);
            allowedFallen = Mathf.Max(0, allowedFallenBlocks);
            IsMonitoring = false;
            IsCollapsed = false;
            FallenCount = 0;
        }

        /// <summary>Records the settled pose of every block. Call once the initial physics has settled.</summary>
        public void CaptureBaseline()
        {
            foreach (BlockController block in blocks)
            {
                if (block == null) continue;
                block.BaselinePosition = block.transform.position;
                block.FallenTimer = 0f;
            }
            IsMonitoring = true;
        }

        public void Stop()
        {
            IsMonitoring = false;
        }

        void FixedUpdate()
        {
            if (!IsMonitoring || IsCollapsed) return;
            Evaluate(Time.fixedDeltaTime);
        }

        /// <summary>Updates fallen timers and the collapse flag. Public so tests can drive it directly.</summary>
        public void Evaluate(float deltaTime)
        {
            int fallen = 0;
            foreach (BlockController block in blocks)
            {
                // Destroyed blocks are extracted blocks that finished their animation.
                if (block == null) continue;
                switch (block.State)
                {
                    case BlockState.Extracted:
                        continue;
                    case BlockState.OutOfBounds:
                        fallen++;
                        continue;
                    case BlockState.Dragging:
                        block.FallenTimer = 0f;
                        continue;
                }

                bool displaced = IsBlockDisplaced(block.BaselinePosition, block.transform.position,
                    block.transform.rotation, blockHeight, dropThresholdFraction, tiltThresholdDegrees, killHeight);
                block.FallenTimer = displaced ? block.FallenTimer + deltaTime : 0f;
                if (block.FallenTimer >= fallenConfirmTime) fallen++;
            }

            FallenCount = fallen;
            if (fallen > allowedFallen) IsCollapsed = true;
        }

        /// <summary>Pure threshold test, kept static so it can be unit tested.</summary>
        public static bool IsBlockDisplaced(Vector3 baseline, Vector3 position, Quaternion rotation,
            float blockHeight, float dropFraction, float tiltDegrees, float killHeight)
        {
            if (position.y < killHeight) return true;
            if (baseline.y - position.y > blockHeight * dropFraction) return true;
            // Angle between the block's up axis and world up; yaw alone does not count as tilt.
            float tilt = Vector3.Angle(rotation * Vector3.up, Vector3.up);
            return tilt > tiltDegrees;
        }

        /// <summary>True when every tracked block is (almost) motionless.</summary>
        public bool IsSettled
        {
            get
            {
                foreach (BlockController block in blocks)
                {
                    if (block == null || block.State == BlockState.Extracted || block.State == BlockState.OutOfBounds) continue;
                    Rigidbody body = block.Body;
                    if (body == null || body.isKinematic || body.IsSleeping()) continue;
                    if (body.linearVelocity.magnitude > settledLinearSpeed) return false;
                    if (body.angularVelocity.magnitude > settledAngularSpeed) return false;
                }
                return true;
            }
        }

        /// <summary>Remembers current block positions (used to rate an extraction as "perfect").</summary>
        public void TakeSnapshot()
        {
            snapshot.Clear();
            foreach (BlockController block in blocks)
            {
                if (block != null) snapshot[block] = block.transform.position;
            }
        }

        /// <summary>Largest movement of any other block since <see cref="TakeSnapshot"/>.</summary>
        public float MaxDisplacementSinceSnapshot(BlockController exclude = null)
        {
            float max = 0f;
            foreach (KeyValuePair<BlockController, Vector3> pair in snapshot)
            {
                BlockController block = pair.Key;
                if (block == null || block == exclude || block.State == BlockState.Extracted) continue;
                if (block.State == BlockState.OutOfBounds) return float.MaxValue;
                max = Mathf.Max(max, Vector3.Distance(block.transform.position, pair.Value));
            }
            return max;
        }
    }
}
