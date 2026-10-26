using System.Linq;
using NUnit.Framework;
using UndergroundMetroTycoon.Simulation;

namespace UndergroundMetroTycoon.Tests
{
    public class ExcavationTests
    {
        private GameContent _content;

        [SetUp]
        public void SetUp()
        {
            _content = DefaultContent.Create();
        }

        private ExcavationService ServiceFor(string contractId)
        {
            var service = new ExcavationService();
            service.SetGrid(SiteGenerator.Build(_content.GetContract(contractId), _content));
            service.ActiveContractId = contractId;
            return service;
        }

        private ToolDefinition Shovel => _content.GetTool(DefaultContent.BasicShovelId);

        [Test]
        public void SoftSoil_TakesTwoShovelHits_AndHpNeverGoesNegative()
        {
            ExcavationService service = ServiceFor(DefaultContent.GardenTrenchId);
            Assert.AreEqual(DigOutcome.Damaged, service.Dig(0, Shovel, "a").Outcome);
            Assert.AreEqual(5, service.Grid.GetCell(0).CurrentHp);
            Assert.AreEqual(DigOutcome.Excavated, service.Dig(0, Shovel, "a").Outcome);
            Assert.AreEqual(0, service.Grid.GetCell(0).CurrentHp);
            Assert.IsTrue(service.Grid.GetCell(0).IsExcavated);
        }

        [Test]
        public void OverkillDamage_IsClamped()
        {
            ExcavationService service = ServiceFor(DefaultContent.GardenTrenchId);
            var bigTool = new ToolDefinition { Id = "big", Level = 10, Power = 999, SwingsPerSecond = 1, Reach = 1 };
            DigResult result = service.Dig(0, bigTool, "a");
            Assert.AreEqual(DigOutcome.Excavated, result.Outcome);
            Assert.AreEqual(10, result.Damage);
            Assert.AreEqual(0, service.Grid.GetCell(0).CurrentHp);
        }

        [Test]
        public void ExcavatedCell_CannotBeDugAgain()
        {
            ExcavationService service = ServiceFor(DefaultContent.GardenTrenchId);
            service.Dig(0, Shovel, "a");
            service.Dig(0, Shovel, "a");
            Assert.AreEqual(DigOutcome.AlreadyExcavated, service.Dig(0, Shovel, "a").Outcome);
            Assert.AreEqual(1, service.Grid.ExcavatedContractCellCount);
        }

        [Test]
        public void LowerLayer_OpensOnlyAfterTheWholeLayerAboveIsDug()
        {
            ExcavationService service = ServiceFor(DefaultContent.ResidentialFoundationId);
            ExcavationGrid grid = service.Grid;
            int above = grid.IndexOf(2, 0, 2);
            int below = grid.IndexOf(2, 1, 2);

            Assert.AreEqual(DigOutcome.NotAccessible, service.Dig(below, Shovel, "a").Outcome);
            service.Dig(above, Shovel, "a");
            service.Dig(above, Shovel, "a");
            Assert.AreEqual(DigOutcome.NotAccessible, service.Dig(below, Shovel, "a").Outcome, "rest of layer 0 still standing");

            for (int z = 0; z < grid.Length; z++)
                for (int x = 0; x < grid.Width; x++)
                {
                    int i = grid.IndexOf(x, 0, z);
                    while (!grid.GetCell(i).IsExcavated) service.Dig(i, Shovel, "a");
                }
            Assert.AreEqual(0, grid.RemainingContractCellsInLayer(0));
            Assert.AreEqual(DigOutcome.Damaged, service.Dig(below, Shovel, "a").Outcome);
        }

        [Test]
        public void NonContractCell_IsRejected()
        {
            ExcavationService service = ServiceFor(DefaultContent.ResidentialFoundationId);
            ExcavationGrid grid = service.Grid;
            int stepAbove = grid.IndexOf(0, 0, 0);
            int stepCell = grid.IndexOf(0, 1, 0);
            service.Dig(stepAbove, Shovel, "a");
            service.Dig(stepAbove, Shovel, "a");
            Assert.IsFalse(grid.GetCell(stepCell).IsContractCell);
            Assert.AreEqual(DigOutcome.OutsideContract, service.Dig(stepCell, Shovel, "a").Outcome);
        }

        [Test]
        public void NoActiveContract_IsRejected()
        {
            ExcavationService service = ServiceFor(DefaultContent.GardenTrenchId);
            service.ActiveContractId = null;
            Assert.AreEqual(DigOutcome.NoActiveContract, service.Dig(0, Shovel, "a").Outcome);
        }

        [Test]
        public void ToolTooWeak_ForClay()
        {
            var contract = new ContractDefinition
            {
                Id = "clay_test",
                SiteWidth = 1,
                SiteLength = 1,
                Layers = { new ContractLayerSpec { Material = TerrainMaterial.Clay } }
            };
            var service = new ExcavationService();
            service.SetGrid(SiteGenerator.Build(contract, _content));
            service.ActiveContractId = "clay_test";
            Assert.AreEqual(DigOutcome.ToolTooWeak, service.Dig(0, Shovel, "a").Outcome);
        }

        [Test]
        public void ReservedCell_CannotBeDugByAnotherActor()
        {
            ExcavationService service = ServiceFor(DefaultContent.GardenTrenchId);
            Assert.IsTrue(service.TryReserve(0, "worker1"));
            Assert.IsFalse(service.TryReserve(0, "worker2"));
            Assert.AreEqual(DigOutcome.ReservedByOther, service.Dig(0, Shovel, "worker2").Outcome);
            Assert.AreEqual(DigOutcome.Damaged, service.Dig(0, Shovel, "worker1").Outcome);
        }

        [Test]
        public void Reservation_IsReleasedWhenCellIsExcavated()
        {
            ExcavationService service = ServiceFor(DefaultContent.GardenTrenchId);
            service.TryReserve(0, "worker1");
            service.Dig(0, Shovel, "worker1");
            service.Dig(0, Shovel, "worker1");
            Assert.IsNull(service.GetReservation(0));
        }

        [Test]
        public void FindBestTarget_RespectsReachAndSkipsOthersReservations()
        {
            ExcavationService service = ServiceFor(DefaultContent.GardenTrenchId);
            ExcavationGrid grid = service.Grid;
            System.Numerics.Vector3 top = grid.Layout.CellTopCenter(grid.GetCell(0).Coord);
            var feet = new System.Numerics.Vector3(top.X - 1.0f, 0f, top.Z);

            Assert.AreEqual(0, service.FindBestTarget(feet, System.Numerics.Vector3.UnitX, 1.6f, Shovel, "a"));
            service.TryReserve(0, "b");
            int other = service.FindBestTarget(feet, System.Numerics.Vector3.UnitX, 1.6f, Shovel, "a");
            Assert.AreNotEqual(0, other);

            var farAway = new System.Numerics.Vector3(top.X - 20f, 0f, top.Z);
            Assert.AreEqual(-1, service.FindBestTarget(farAway, System.Numerics.Vector3.UnitX, 1.6f, Shovel, "a"));
        }

        [Test]
        public void GroundHeight_DropsWhenColumnIsExcavated()
        {
            ExcavationService service = ServiceFor(DefaultContent.GardenTrenchId);
            ExcavationGrid grid = service.Grid;
            System.Numerics.Vector3 top = grid.Layout.CellTopCenter(grid.GetCell(0).Coord);
            Assert.AreEqual(0f, grid.GetGroundHeight(top.X, top.Z), 1e-5f);
            service.Dig(0, Shovel, "a");
            service.Dig(0, Shovel, "a");
            Assert.AreEqual(-0.5f, grid.GetGroundHeight(top.X, top.Z), 1e-5f);
        }

        [Test]
        public void GeneratedSite_IsDeterministic()
        {
            ExcavationGrid a = SiteGenerator.Build(_content.GetContract(DefaultContent.ResidentialFoundationId), _content);
            ExcavationGrid b = SiteGenerator.Build(_content.GetContract(DefaultContent.ResidentialFoundationId), _content);
            Assert.AreEqual(a.CellCount, b.CellCount);
            Assert.IsTrue(a.Cells.Select(c => c.Id).SequenceEqual(b.Cells.Select(c => c.Id)));
            Assert.AreEqual(54, a.ContractCellCount);
        }
    }
}
