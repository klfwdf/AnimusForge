using TaleWorlds.GauntletUI;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Inventory;

public class InventoryImageIdentifierWidget : ImageIdentifierWidget
{
	public InventoryImageIdentifierWidget(UIContext context)
		: base(context)
	{
	}

	public void SetRenderRequestedPreviousFrame(bool isRequested)
	{
		_isRenderRequestedPreviousFrame = isRequested && IsRecursivelyVisible() && base.EventManager.AreaRectangle.IsCollide(in AreaRect);
	}
}
