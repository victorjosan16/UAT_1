using NUnit.Framework;

namespace SugarDrop.Tests
{
    /// <summary>Luptele cu Boss: cine apare, viața, fazele, atacurile anunțate și efectul lor pe tablă.</summary>
    public class BossBattleTests
    {
        private static BossBattle BattleFor(int level) => new(BossDefinition.For(level, LevelConfig.For(level).Moves));

        [Test]
        public void Bosses_AppearInOrder_WithMegaBossesEveryFifty()
        {
            Assert.AreEqual("Regele Ciocolată", BossDefinition.For(10, 25).Name);
            Assert.AreEqual("Regina Gheții", BossDefinition.For(20, 25).Name);
            Assert.AreEqual("Dragonul de Bomboane", BossDefinition.For(30, 25).Name);
            Assert.AreEqual("Monstrul de Jeleu", BossDefinition.For(40, 25).Name);
            BossDefinition emperor = BossDefinition.For(50, 30);
            Assert.AreEqual("Împăratul Zahărului", emperor.Name);
            Assert.IsTrue(emperor.Mega);
            Assert.AreEqual(3, emperor.Phases.Length);
            foreach (int level in new[] { 100, 150, 200 })
            {
                BossDefinition mega = BossDefinition.For(level, 30);
                Assert.IsTrue(mega.Mega, $"nivel {level}");
                Assert.AreEqual(3, mega.Phases.Length, $"nivel {level}");
            }
            Assert.AreNotEqual(emperor.Name, BossDefinition.For(100, 30).Name);
            Assert.AreEqual(1, BossDefinition.For(60, 25).Phases.Length);
        }

        [Test]
        public void Health_GrowsWithMoves_AndIsPositive()
        {
            for (int level = 10; level <= 200; level += 10)
                Assert.Greater(BossDefinition.For(level, LevelConfig.For(level).Moves).Health, 0, $"nivel {level}");
            Assert.Greater(BossDefinition.For(10, 40).Health, BossDefinition.For(10, 20).Health);
        }

        [Test]
        public void Attack_IsWarnedOneMoveBefore_AndComesOnlyAtEndOfMove()
        {
            BossBattle battle = BattleFor(10);
            int every = battle.Phase.AttackEvery;
            for (int move = 1; move < every; move++)
            {
                Assert.IsNull(battle.EndOfMove(), $"mutarea {move}");
                Assert.AreEqual(move == every - 1, battle.AttackWarning, $"mutarea {move}");
            }
            Assert.AreEqual(BossAttack.ChocolateSpread, battle.EndOfMove());
            Assert.AreEqual(every, battle.MovesUntilAttack);
            Assert.IsFalse(battle.AttackWarning);
        }

        [Test]
        public void Damage_LowersHealth_AndDefeatedBossStopsAttacking()
        {
            BossBattle battle = BattleFor(20);
            int start = battle.Health;
            battle.Damage(100);
            Assert.AreEqual(start - 100, battle.Health);
            battle.Damage(0);
            battle.Damage(-30);
            Assert.AreEqual(start - 100, battle.Health);
            battle.Damage(start * 2);
            Assert.AreEqual(0, battle.Health);
            Assert.IsTrue(battle.Defeated);
            for (int i = 0; i < 10; i++) Assert.IsNull(battle.EndOfMove());
            Assert.IsFalse(battle.AttackWarning);
        }

        [Test]
        public void MegaBoss_ChangesPhase_AtTwoThirds_AndOneThird()
        {
            BossBattle battle = BattleFor(50);
            int health = battle.Definition.Health;
            Assert.AreEqual(0, battle.PhaseIndex);
            Assert.IsFalse(battle.Damage(health / 4));
            Assert.IsTrue(battle.Damage(health / 8 + 1)); // sub 66%
            Assert.AreEqual(1, battle.PhaseIndex);
            Assert.IsTrue(battle.Damage(health / 3 + 1)); // sub 33%
            Assert.AreEqual(2, battle.PhaseIndex);
            Assert.LessOrEqual(battle.MovesUntilAttack, battle.Phase.AttackEvery);
            Assert.AreEqual(5, battle.Phase.Attacks.Length, "faza finală are toate atacurile, inclusiv bombele cu ceas");
        }

        [Test]
        public void DamageRules_RewardBigGroupsSpecialsAndCombos()
        {
            var small = new BoardStep(StepKind.Clear) { GroupSize = 3 };
            for (int i = 0; i < 3; i++) small.Cleared.Add(new PieceInfo { Kind = TileKind.Normal });
            var big = new BoardStep(StepKind.Clear) { GroupSize = 8 };
            for (int i = 0; i < 8; i++) big.Cleared.Add(new PieceInfo { Kind = TileKind.Normal });
            var crate = new BoardStep(StepKind.Clear);
            crate.Cleared.Add(new PieceInfo { Kind = TileKind.Crate });
            var combo = new BoardStep(StepKind.Clear) { Combo = true };
            combo.Activations.Add(new Activation { Kind = TileKind.Bomb });

            Assert.AreEqual(30, BossRules.DamageFor(small));
            Assert.Greater(BossRules.DamageFor(big) / 8f, BossRules.DamageFor(small) / 3f);
            Assert.AreEqual(0, BossRules.DamageFor(crate));
            Assert.AreEqual(BossRules.PerActivation + BossRules.Combo, BossRules.DamageFor(combo));
        }

        [Test]
        public void Attacks_ChangeTheBoard_WithoutBreakingIt()
        {
            foreach (BossAttack attack in new[] { BossAttack.ChocolateSpread, BossAttack.Freeze, BossAttack.Lock, BossAttack.Shuffle })
            {
                for (int seed = 0; seed < 5; seed++)
                {
                    var engine = new BlastEngine(LevelConfig.For(10), seed);
                    BoardStep step = null;
                    foreach (BoardStep s in engine.ApplyBossAttack(attack, 3))
                    {
                        step ??= s;
                        Assert.IsTrue(s.Kind == StepKind.BossAttack || s.Kind == StepKind.Shuffle, $"{attack}");
                    }
                    Assert.IsNotNull(step, $"{attack}");
                    if (attack != BossAttack.Shuffle) Assert.Greater(step.Changed.Count, 0, $"{attack}, seed {seed}");
                    Assert.IsTrue(engine.CheckIntegrity(out string error), $"{attack}: {error}");
                    Assert.IsTrue(engine.HasValidMove(), $"{attack}: după atac trebuie să existe o mutare");
                }
            }
        }

        [Test]
        public void RepeatedAttacks_StayCapped()
        {
            var engine = new BlastEngine(LevelConfig.For(10), 3);
            for (int i = 0; i < 30; i++) engine.ApplyBossAttack(BossAttack.ChocolateSpread, 5);
            int chocolate = 0;
            for (int x = 0; x < engine.Columns; x++)
                for (int y = 0; y < engine.Rows; y++)
                    if (engine.TryGetPiece(new UnityEngine.Vector2Int(x, y), out PieceInfo p) && p.Kind == TileKind.Chocolate) chocolate++;
            Assert.LessOrEqual(chocolate, BlastEngine.MaxBossChocolate);
            Assert.IsTrue(engine.HasValidMove());
        }

        [Test]
        public void BossLevels_AreWinnable_ByTheAutomaticPlayer()
        {
            foreach (int level in new[] { 10, 20, 30, 40, 50 })
            {
                int wins = 0;
                for (int seed = 0; seed < 6; seed++)
                    if (BalanceSimulator.Play(level, seed * 53 + level, out _)) wins++;
                Assert.GreaterOrEqual(wins, 2, $"Boss-ul de la nivelul {level} e prea greu ({wins}/6)");
            }
        }
    }
}
