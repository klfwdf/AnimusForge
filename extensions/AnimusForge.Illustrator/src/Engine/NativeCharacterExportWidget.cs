using TaleWorlds.GauntletUI;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets;
using TaleWorlds.TwoDimension;

namespace AnimusForge.Illustrator.Engine
{
    // Constructed only for a requested portrait export. Vanilla still owns the
    // tableau, provider updates and deferred cleanup; only the screen blit is omitted.
    internal sealed class NativeCharacterExportWidget : CharacterTableauWidget
    {
        public NativeCharacterExportWidget(UIContext context) : base(context) { }

        protected override void OnRender(TwoDimensionContext context, TwoDimensionDrawContext drawContext)
        {
            // TextureWidget.OnUpdate requires this flag to create and tick its
            // provider. Keep that lifecycle without calling the vanilla OnRender,
            // which submits a screen draw even for this temporary export widget.
            _isRenderRequestedPreviousFrame = true;
        }
    }
}
