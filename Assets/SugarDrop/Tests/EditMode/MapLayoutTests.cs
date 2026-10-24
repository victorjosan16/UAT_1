using NUnit.Framework;
using UnityEngine;

namespace SugarDrop.Tests
{
    /// <summary>Geometria hărții: nodurile urcă, încap pe ecran și derularea centrează nivelul corect.</summary>
    public class MapLayoutTests
    {
        [Test]
        public void Nodes_GoUpAndStayInsideTheScreenWidth()
        {
            for (int level = 1; level < 200; level++)
            {
                Vector2 a = MapLayout.NodePosition(level), b = MapLayout.NodePosition(level + 1);
                Assert.Greater(b.y, a.y);
                // 1080 lățime: centrul ± jumătate din nodul Boss (105) trebuie să rămână în ecran.
                Assert.Less(Mathf.Abs(a.x) + 105f, 540f, $"nivel {level}");
            }
        }

        [Test]
        public void Path_PassesThroughNodes()
        {
            for (int level = 1; level < 50; level++)
            {
                Assert.That(Vector2.Distance(MapLayout.PathPoint(level, 0f), MapLayout.NodePosition(level)), Is.LessThan(0.01f));
                Assert.That(Vector2.Distance(MapLayout.PathPoint(level, 1f), MapLayout.NodePosition(level + 1)), Is.LessThan(0.01f));
            }
        }

        [Test]
        public void Focus_IsClampedAndIncreasing()
        {
            int levels = MapLayout.VisibleLevels(1);
            float previous = -1f;
            for (int level = 1; level <= levels; level++)
            {
                float focus = MapLayout.FocusNormalized(level, levels, 2300f);
                Assert.That(focus, Is.InRange(0f, 1f));
                Assert.That(focus, Is.GreaterThanOrEqualTo(previous));
                previous = focus;
            }
            Assert.AreEqual(0f, MapLayout.FocusNormalized(1, levels, 2300f));
        }

        [Test]
        public void VisibleLevels_AlwaysShowAheadAndEndOnBoss()
        {
            for (int current = 1; current < 500; current += 7)
            {
                int visible = MapLayout.VisibleLevels(current);
                Assert.GreaterOrEqual(visible, current + 15);
                Assert.IsTrue(LevelConfig.IsBossLevel(visible));
            }
        }
    }
}
