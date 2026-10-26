using NUnit.Framework;
using UndergroundMetroTycoon.Simulation;
using Vector3 = System.Numerics.Vector3;

namespace UndergroundMetroTycoon.Tests
{
    public class KinematicMoverTests
    {
        private sealed class StepGround : IGroundHeightProvider
        {
            public float StepX = 1f;
            public float StepHeight;
            public float GetGroundHeight(float x, float z) => x >= StepX ? StepHeight : 0f;
        }

        private static void Walk(KinematicMover mover, float dx, float dz, int steps)
        {
            for (int i = 0; i < steps; i++) mover.Step(dx, dz, 0.02f);
        }

        [Test]
        public void ObstacleBlocksMovement()
        {
            var mover = new KinematicMover();
            mover.SetObstacles(new[] { ObstacleBox.FromCenter(2f, 0f, 1f, 4f) });
            mover.Teleport(Vector3.Zero);
            Walk(mover, 0.05f, 0f, 100);
            Assert.Less(mover.Position.X, 1.5f - mover.Radius + 0.06f);
        }

        [Test]
        public void SlidesAlongObstacle()
        {
            var mover = new KinematicMover();
            mover.SetObstacles(new[] { ObstacleBox.FromCenter(2f, 0f, 1f, 10f) });
            mover.Teleport(Vector3.Zero);
            Walk(mover, 0.05f, 0.05f, 50);
            Assert.Greater(mover.Position.Z, 2f);
        }

        [Test]
        public void ClimbsHalfMeterStep_ButNotOneMeterWall()
        {
            var low = new KinematicMover { Ground = new StepGround { StepHeight = 0.5f } };
            low.Teleport(Vector3.Zero);
            Walk(low, 0.05f, 0f, 60);
            Assert.Greater(low.Position.X, 1.5f);
            Assert.AreEqual(0.5f, low.Position.Y, 1e-4f);

            var high = new KinematicMover { Ground = new StepGround { StepHeight = 1.0f } };
            high.Teleport(Vector3.Zero);
            Walk(high, 0.05f, 0f, 60);
            Assert.Less(high.Position.X, 1f);
            Assert.AreEqual(0f, high.Position.Y, 1e-4f);
        }

        [Test]
        public void FallsIntoExcavatedHole()
        {
            var mover = new KinematicMover { Ground = new StepGround { StepX = -100f, StepHeight = -0.5f } };
            mover.Teleport(new Vector3(0f, 0f, 0f));
            Assert.AreEqual(-0.5f, mover.Position.Y, 1e-4f);

            var ground = new StepGround { StepX = 100f, StepHeight = 0f };
            var falling = new KinematicMover { Ground = ground };
            falling.Teleport(Vector3.Zero);
            ground.StepX = -100f;
            ground.StepHeight = -0.5f;
            Walk(falling, 0f, 0f, 30);
            Assert.AreEqual(-0.5f, falling.Position.Y, 1e-4f);
        }

        [Test]
        public void StaysInsideBounds()
        {
            var mover = new KinematicMover { MinX = -2, MaxX = 2, MinZ = -2, MaxZ = 2 };
            mover.Teleport(Vector3.Zero);
            Walk(mover, 0.5f, -0.5f, 50);
            Assert.AreEqual(2f, mover.Position.X, 1e-4f);
            Assert.AreEqual(-2f, mover.Position.Z, 1e-4f);
        }
    }
}
