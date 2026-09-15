using TaleWorlds.GauntletUI;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets;
using TaleWorlds.TwoDimension;

namespace AnimusForge.Illustrator.Engine
{
    // Constructed in code; no prefab registration. Gauntlet drives the native provider.
    internal sealed class NativeBannerExportWidget : BannerTableauWidget
    {
        public NativeBannerExportWidget(UIContext context) : base(context) { }

        protected override void OnRender(TwoDimensionContext context, TwoDimensionDrawContext drawContext)
        {
            _isRenderRequestedPreviousFrame = true;
            // Never call base.OnRender or drawContext.Draw. Vanilla banner drawing
            // ignores the widget's AlphaFactor; skipping the blit prevents flashing.
        }
    }
}
