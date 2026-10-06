using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Inventory;

public class InventoryItemPreviewWidget : Widget
{
	private ItemTableauWidget _itemTableau;

	private bool _isPreviewOpen;

	[Editor(false)]
	public bool IsPreviewOpen
	{
		get
		{
			return _isPreviewOpen;
		}
		set
		{
			if (_isPreviewOpen != value)
			{
				_isPreviewOpen = value;
				base.IsVisible = value;
				OnPropertyChanged(value, "IsPreviewOpen");
			}
		}
	}

	[Editor(false)]
	public ItemTableauWidget ItemTableau
	{
		get
		{
			return _itemTableau;
		}
		set
		{
			if (_itemTableau != value)
			{
				_itemTableau = value;
				OnPropertyChanged(value, "ItemTableau");
			}
		}
	}

	public InventoryItemPreviewWidget(UIContext context)
		: base(context)
	{
	}
}
