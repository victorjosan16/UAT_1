using NUnit.Framework;
using UnityEngine;

namespace VoxelBloom.Tests
{
    /// <summary>The premium artwork referenced by the UI code must exist in Resources/VBUI (else the UI falls back to procedural art).</summary>
    public class UIArtTests
    {
        static readonly string[] Required =
        {
            "Logo/logo",
            "HUD/avatar", "HUD/pill_hearts_blank", "HUD/pill_coins_blank", "HUD/pill_gems_blank",
            "HUD/caption_pill_blank", "HUD/percent_pill_blank", "HUD/progress_track", "HUD/progress_fill",
            "Buttons/settings", "Buttons/back", "Buttons/play", "Buttons/level_badge_blank",
            "Buttons/nav_collection", "Buttons/nav_rewards", "Buttons/nav_shop", "Buttons/nav_events",
            "Icons/label_360", "Icons/rotate_left", "Icons/rotate_right", "Icons/cube", "Icons/star_gold", "Icons/star_empty", "Icons/lightning",
            "Boosters/hammer", "Boosters/shuffle", "Boosters/color_bomb", "Boosters/remove",
            "Blocks/block_face", "Blocks/block_gloss",
        };

        [Test]
        public void AllReferencedArtwork_IsInResources()
        {
            foreach (var p in Required)
                Assert.IsNotNull(Resources.Load<Texture2D>("VBUI/" + p), "missing Resources/VBUI/" + p);
        }
    }
}
