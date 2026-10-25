using NUnit.Framework;

namespace SugarDrop.Tests
{
    /// <summary>Echilibrul: primele niveluri sunt ușoare și niciun nivel nu e imposibil pentru jucătorul automat.</summary>
    public class BalanceTests
    {
        [Test]
        public void FirstLevels_AreEasy()
        {
            for (int level = 1; level <= 8; level++)
            {
                int wins = 0;
                for (int seed = 0; seed < 4; seed++)
                    if (BalanceSimulator.Play(level, seed * 97 + level, out _)) wins++;
                Assert.GreaterOrEqual(wins, 3, $"nivelul {level} e prea greu pentru început ({wins}/4)");
            }
        }

        [Test]
        public void Levels_AreWinnable()
        {
            int wins = 0, total = 0;
            for (int level = 1; level <= 40; level++)
            {
                int levelWins = 0;
                for (int seed = 0; seed < 6; seed++)
                    if (BalanceSimulator.Play(level, seed * 131 + level * 7, out _)) levelWins++;
                Assert.GreaterOrEqual(levelWins, 1, $"nivelul {level}: jucătorul automat n-a câștigat niciodată");
                wins += levelWins;
                total += 6;
            }
            Assert.GreaterOrEqual(wins * 100 / total, 55, "în medie, nivelurile 1–40 sunt prea grele");
        }
    }
}
