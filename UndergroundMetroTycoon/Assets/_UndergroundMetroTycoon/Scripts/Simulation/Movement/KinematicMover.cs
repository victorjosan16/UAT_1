using System;
using System.Collections.Generic;
using System.Numerics;

namespace UndergroundMetroTycoon.Simulation
{
    public interface IGroundHeightProvider
    {
        float GetGroundHeight(float worldX, float worldZ);
    }

    /// <summary>Axis-aligned footprint of a static prop on the XZ plane.</summary>
    public readonly struct ObstacleBox
    {
        public readonly float MinX, MinZ, MaxX, MaxZ;

        public ObstacleBox(float minX, float minZ, float maxX, float maxZ)
        {
            MinX = Math.Min(minX, maxX);
            MaxX = Math.Max(minX, maxX);
            MinZ = Math.Min(minZ, maxZ);
            MaxZ = Math.Max(minZ, maxZ);
        }

        public static ObstacleBox FromCenter(float x, float z, float sizeX, float sizeZ) =>
            new ObstacleBox(x - sizeX * 0.5f, z - sizeZ * 0.5f, x + sizeX * 0.5f, z + sizeZ * 0.5f);

        public bool OverlapsCircle(float x, float z, float radius)
        {
            float cx = Math.Max(MinX, Math.Min(x, MaxX));
            float cz = Math.Max(MinZ, Math.Min(z, MaxZ));
            float dx = x - cx, dz = z - cz;
            return dx * dx + dz * dz < radius * radius;
        }
    }

    /// <summary>
    /// Deterministic character movement over the excavation height field: blocks walls taller than
    /// <see cref="MaxStepUp"/>, collides with static props, slides along obstacles and falls into dug holes.
    /// </summary>
    public sealed class KinematicMover
    {
        /// <summary>Highest ledge a character can step onto. Content validation relies on this value.</summary>
        public const float DefaultMaxStepUp = 0.55f;

        private readonly List<ObstacleBox> _obstacles = new List<ObstacleBox>();
        private float _verticalVelocity;

        public float Radius = 0.35f;
        public float MaxStepUp = DefaultMaxStepUp;
        public float Gravity = 20f;
        public float StepUpSpeed = 4f;
        public float MinX = -10f, MaxX = 10f, MinZ = -10f, MaxZ = 10f;

        public IGroundHeightProvider Ground { get; set; }
        public Vector3 Position { get; private set; }
        public bool IsGrounded => Math.Abs(Position.Y - GroundAt(Position.X, Position.Z)) < 0.01f;

        public void SetObstacles(IEnumerable<ObstacleBox> obstacles)
        {
            _obstacles.Clear();
            if (obstacles != null) _obstacles.AddRange(obstacles);
        }

        public void Teleport(Vector3 position)
        {
            float x = Clamp(position.X, MinX, MaxX);
            float z = Clamp(position.Z, MinZ, MaxZ);
            Position = new Vector3(x, GroundAt(x, z), z);
            _verticalVelocity = 0f;
        }

        public Vector3 Step(float deltaX, float deltaZ, float deltaTime)
        {
            Vector3 p = Position;
            float foot = p.Y;
            float tx = Clamp(p.X + deltaX, MinX, MaxX);
            float tz = Clamp(p.Z + deltaZ, MinZ, MaxZ);

            if (!IsBlocked(tx, tz, foot))
            {
                p.X = tx;
                p.Z = tz;
            }
            else if (!IsBlocked(tx, p.Z, foot))
            {
                p.X = tx;
            }
            else if (!IsBlocked(p.X, tz, foot))
            {
                p.Z = tz;
            }

            float ground = GroundAt(p.X, p.Z);
            if (ground > p.Y)
            {
                p.Y = Math.Min(ground, p.Y + StepUpSpeed * deltaTime);
                _verticalVelocity = 0f;
            }
            else if (ground < p.Y)
            {
                _verticalVelocity -= Gravity * deltaTime;
                p.Y = Math.Max(ground, p.Y + _verticalVelocity * deltaTime);
                if (p.Y <= ground) _verticalVelocity = 0f;
            }
            else
            {
                _verticalVelocity = 0f;
            }

            Position = p;
            return p;
        }

        public bool IsBlocked(float x, float z, float footY)
        {
            foreach (ObstacleBox box in _obstacles)
            {
                if (box.OverlapsCircle(x, z, Radius)) return true;
            }
            float r = Radius * 0.9f;
            return GroundAt(x, z) - footY > MaxStepUp
                || GroundAt(x + r, z) - footY > MaxStepUp
                || GroundAt(x - r, z) - footY > MaxStepUp
                || GroundAt(x, z + r) - footY > MaxStepUp
                || GroundAt(x, z - r) - footY > MaxStepUp;
        }

        private float GroundAt(float x, float z) => Ground?.GetGroundHeight(x, z) ?? 0f;

        private static float Clamp(float v, float min, float max) => v < min ? min : v > max ? max : v;
    }
}
