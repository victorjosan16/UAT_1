using System.Collections.Generic;
using NUnit.Framework;

namespace SugarDrop.Tests
{
    public class LevelStoryTests
    {
        [Test]
        public void EveryStoryHasTranslatedTitleAndLine()
        {
            var keys = new HashSet<string>();
            foreach (string[] row in SugarLoc.Rows) keys.Add(row[0]);
            foreach (LevelStoryEntry entry in LevelStory.All)
            {
                Assert.IsTrue(keys.Contains(entry.TitleKey), entry.TitleKey);
                Assert.IsTrue(keys.Contains(entry.LineKey), entry.LineKey);
            }
        }

        [Test]
        public void BubblesLieInsideThePicture()
        {
            foreach (LevelStoryEntry e in LevelStory.All)
            {
                Assert.That(e.BubbleLeft, Is.InRange(0f, 1f), "level " + e.Level);
                Assert.That(e.BubbleTop, Is.InRange(0f, 1f), "level " + e.Level);
                Assert.Less(e.BubbleLeft, e.BubbleRight, "level " + e.Level);
                Assert.Less(e.BubbleTop, e.BubbleBottom, "level " + e.Level);
                Assert.LessOrEqual(e.BubbleRight, 1f, "level " + e.Level);
                Assert.LessOrEqual(e.BubbleBottom, 1f, "level " + e.Level);
            }
        }

        [Test]
        public void SceneShowsOnlyOnceAndOnlyForTheNewLevel()
        {
            Assert.IsTrue(LevelStory.ShouldShow(4, 4, 3));
            Assert.IsFalse(LevelStory.ShouldShow(4, 4, 4), "already seen");
            Assert.IsFalse(LevelStory.ShouldShow(4, 9, 3), "replaying an old level");
            Assert.IsFalse(LevelStory.ShouldShow(12, 12, 11), "no scene for this level");
        }
    }
}
