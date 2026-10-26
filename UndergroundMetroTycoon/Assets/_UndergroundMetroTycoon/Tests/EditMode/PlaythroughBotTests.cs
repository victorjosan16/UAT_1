using System;
using NUnit.Framework;
using UndergroundMetroTycoon.Simulation;
using Vector3 = System.Numerics.Vector3;

namespace UndergroundMetroTycoon.Tests
{
    /// <summary>
    /// Plays contracts the way the player does: walking with the real movement rules and digging only what the
    /// auto-dig targeting picks within reach. Proves every cell is reachable and the player never gets trapped.
    /// </summary>
    public class PlaythroughBotTests
    {
        private const float Dt = 1f / 30f;
        private const float Speed = 3.6f;

        [Test]
        public void Bot_CompletesAllPhase1Contracts_FromZeroCoins_AndWalksOutOfThePit()
        {
            GameSession session = GameSession.CreateNew(DefaultContent.Create());
            var mover = new KinematicMover { MinX = -7.4f, MaxX = 7.4f, MinZ = -7.4f, MaxZ = 7.4f };
            mover.Teleport(new Vector3(-2.2f, 0f, -3.6f));

            PlayContract(session, mover, DefaultContent.GardenTrenchId);
            PlayContract(session, mover, DefaultContent.SmallTrenchId);
            Assert.AreEqual(2, session.Progression.Level);
            PlayContract(session, mover, DefaultContent.ResidentialFoundationId);

            // Walk from the bottom of the foundation pit back to the spawn point.
            var exit = new Vector3(-2.2f, 0f, -6.5f);
            for (int i = 0; i < 600 && Distance2D(mover.Position, exit) > 0.2f; i++) StepToward(mover, exit);
            Assert.Less(Distance2D(mover.Position, exit), 0.25f, "player must be able to climb out of the pit");
            Assert.AreEqual(0f, mover.Position.Y, 1e-3f);

            Assert.AreEqual(5 * 20 + 500 + 5 * 12 + 120 + 30 * 5 + 24 * 8 + 1200, session.Wallet.Coins);
        }

        private static void PlayContract(GameSession session, KinematicMover mover, string contractId)
        {
            Assert.AreEqual(ContractActionResult.Ok, session.AcceptContract(contractId), contractId);
            mover.Ground = session.Grid;
            mover.Teleport(mover.Position);
            ToolDefinition tool = session.EquippedTool;
            Vector3 forward = Vector3.UnitZ;

            for (int tick = 0; tick < 20000 && !session.Grid.IsContractComplete; tick++)
            {
                int target = session.Excavation.FindBestTarget(mover.Position, forward, tool.Reach, tool, GameSession.PlayerActorId);
                if (target >= 0)
                {
                    session.Dig(target, GameSession.PlayerActorId);
                    continue;
                }

                int goal = NearestRemaining(session, mover.Position);
                Assert.GreaterOrEqual(goal, 0, "no reachable cell left but contract incomplete");
                Vector3 top = session.Grid.Layout.CellTopCenter(session.Grid.GetCell(goal).Coord);
                Vector3 before = mover.Position;
                StepToward(mover, top);
                var delta = new Vector3(top.X - before.X, 0f, top.Z - before.Z);
                if (delta.Length() > 0.001f) forward = Vector3.Normalize(delta);
            }

            Assert.IsTrue(session.Grid.IsContractComplete, contractId + " was not completed by walking and auto-digging");
            Assert.AreEqual(ContractActionResult.Ok, session.ClaimContract(contractId));
        }

        private static int NearestRemaining(GameSession session, Vector3 from)
        {
            int best = -1;
            float bestDistance = float.MaxValue;
            ExcavationGrid grid = session.Grid;
            for (int i = 0; i < grid.CellCount; i++)
            {
                TerrainCell c = grid.GetCell(i);
                if (!c.IsContractCell || !grid.IsAccessible(c)) continue;
                float d = Distance2D(from, grid.Layout.CellTopCenter(c.Coord));
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = i;
                }
            }
            return best;
        }

        private static void StepToward(KinematicMover mover, Vector3 goal)
        {
            var delta = new Vector3(goal.X - mover.Position.X, 0f, goal.Z - mover.Position.Z);
            float length = delta.Length();
            if (length < 1e-4f)
            {
                mover.Step(0f, 0f, Dt);
                return;
            }
            float step = Math.Min(length, Speed * Dt);
            mover.Step(delta.X / length * step, delta.Z / length * step, Dt);
        }

        private static float Distance2D(Vector3 a, Vector3 b)
        {
            float dx = a.X - b.X, dz = a.Z - b.Z;
            return (float)Math.Sqrt(dx * dx + dz * dz);
        }
    }
}
