using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Inventory;

public class InventoryCenterPanelWidget : Widget
{
	public InventoryCenterPanelWidget(UIContext context)
		: base(context)
	{
	}

	protected override bool OnPreviewDragBegin()
	{
		return false;
	}

	protected override bool OnPreviewDrop()
	{
		return true;
	}

	protected override bool OnPreviewDragHover()
	{
		return false;
	}

	protected override bool OnPreviewMouseMove()
	{
		return false;
	}

	protected override bool OnPreviewMousePressed()
	{
		return false;
	}

	protected override bool OnPreviewMouseReleased()
	{
		return false;
	}

	protected override bool OnPreviewMouseScroll()
	{
		return false;
	}
}
