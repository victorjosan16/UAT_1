using UnityEngine;
using Veliqora.Infrastructure;

namespace Veliqora.UI.Screens
{
    /// <summary>Full-screen Settings scene (the in-game Pause menu opens the same controls as an overlay).</summary>
    public sealed class SettingsScreen : ScreenBase
    {
        public override bool AllowsBanner => true;

        protected override void BuildBackground(RectTransform full)
        {
            FullBackground(full, Presentation.BackgroundArt.Valley(S.Themes.Theme));
            ReadabilityScrim(full, 0.4f, 0.4f, 0.25f);
        }

        protected override void Build(object args)
        {
            var bar = TopBar();
            AddBack(bar, () => S.Navigator.GoBackToPrevious());
            AddCoins(bar);
            var bg = UIFactory.Panel(Safe, "SettingsPanel", UIStyle.Ivory, UIStyle.RadiusLarge);
            var panel = (RectTransform)bg.transform.parent;
            panel.offsetMin = new Vector2(40, 60);
            panel.offsetMax = new Vector2(-40, -UIStyle.TopBarHeight - 110);
            Ribbon(Safe, Loc.T("Settings"), -UIStyle.TopBarHeight - 40, 560);
            var content = UIFactory.Rect("Content", panel);
            UIFactory.Stretch(content, UIStyle.Space3);
            content.offsetMax = new Vector2(-UIStyle.Space3, -90);
            CommonOverlays.BuildSettings(content, S, inOverlay: false);
        }

        public override bool HandleBack()
        {
            S.SaveNow();
            S.Navigator.GoBackToPrevious();
            return true;
        }
    }
}
