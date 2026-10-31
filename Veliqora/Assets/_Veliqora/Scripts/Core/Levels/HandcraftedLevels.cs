namespace Veliqora.Core.Levels
{
    /// <summary>
    /// Ten hand-authored onboarding levels. Each introduces one idea; solvability and par are still computed
    /// and replay-verified by the solver during catalog build (and re-checked by EditMode tests).
    /// </summary>
    public static class HandcraftedLevels
    {
        public sealed class Entry
        {
            public int[][] Bottles;
            public string TutorialKey;
            public Entry(string tutorialKey, params int[][] bottles) { TutorialKey = tutorialKey; Bottles = bottles; }
        }

        private static int[] B(params int[] units) => units;

        public static readonly Entry[] All =
        {
            // 1 — Tap a bottle, then tap where to pour. One color, one move.
            new Entry("tap_to_pour", B(1, 1, 1), B(1)),
            // 2 — Same colors stack; the whole matching top run moves at once.
            new Entry("same_color_stacks", B(1, 1, 2, 2), B(2, 2, 1, 1), B()),
            // 3 — Empty bottles accept any color.
            new Entry("empty_accepts_any", B(1, 2, 1, 2), B(2, 1, 2, 1), B(), B()),
            // 4 — Three colors: plan ahead.
            new Entry("plan_ahead", B(1, 2, 3, 1), B(2, 3, 1, 2), B(3, 1, 2, 3), B(), B()),
            // 5 — Undo is available if you make a mistake.
            new Entry("undo_intro", B(1, 1, 2, 3), B(2, 2, 3, 1), B(3, 3, 1, 2), B(), B()),
            // 6 — Partial pours: only the space available is filled.
            new Entry("partial_pour", B(1, 2, 2, 3), B(3, 1, 1, 2), B(2, 3, 3, 1), B(), B()),
            // 7 — Four colors and the booster bar.
            new Entry("boosters_intro", B(1, 2, 3, 4), B(2, 3, 4, 1), B(3, 4, 1, 2), B(4, 1, 2, 3), B(), B()),
            // 8 — Hints come from the solver.
            new Entry("hint_intro", B(1, 1, 2, 2), B(3, 3, 4, 4), B(2, 4, 1, 3), B(4, 3, 2, 1), B(), B()),
            // 9 — Five colors.
            new Entry("five_colors", B(1, 2, 3, 4), B(5, 1, 2, 3), B(4, 5, 1, 2), B(3, 4, 5, 1), B(2, 3, 4, 5), B(), B()),
            // 10 — Stars reward efficient solving.
            new Entry("stars_intro", B(1, 2, 2, 3), B(4, 4, 5, 1), B(3, 5, 2, 4), B(5, 1, 3, 2), B(4, 3, 1, 5), B(), B()),
        };
    }
}
