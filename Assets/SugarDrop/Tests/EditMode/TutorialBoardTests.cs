using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SugarDrop.Tests
{
    public class TutorialBoardTests
    {
        private static BlastEngine Engine()
        {
            var engine = new BlastEngine(LevelConfig.For(1), 5);
            engine.DebugFill(TutorialBoard.ColorAt);
            return engine;
        }

        [Test]
        public void TutorialBoard_StartsWithoutLines()
        {
            Assert.AreEqual(0, Engine().DebugMatchCount());
        }

        [Test]
        public void FirstMove_MakesThreeInARow()
        {
            BlastEngine engine = Engine();
            List<BoardStep> steps = engine.Swap(TutorialBoard.MatchFrom, TutorialBoard.MatchTo);
            Assert.IsNotNull(steps);
            BoardStep clear = steps.Find(s => s.Kind == StepKind.Clear);
            Assert.AreEqual(3, clear.Cleared.Count);
        }

        [Test]
        public void SecondMove_MakesAStripedPiece()
        {
            BlastEngine engine = Engine();
            engine.Swap(TutorialBoard.MatchFrom, TutorialBoard.MatchTo);
            List<BoardStep> steps = engine.Swap(TutorialBoard.RocketFrom, TutorialBoard.RocketTo);
            Assert.IsNotNull(steps, "mutarea pentru rachetă e încă validă după primul pas");
            bool rocket = false;
            foreach (BoardStep step in steps)
                foreach (PieceInfo piece in step.Changed)
                    rocket |= BlastRules.IsRocket(piece.Kind);
            Assert.IsTrue(rocket, "4 la rând fac o piesă dungată");
            Assert.IsTrue(engine.IsSpecialAt(TutorialBoard.RocketTo));
        }
    }
}
