using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Encyclopedia;

public class EncyclopediaDividerButtonWidget : ButtonWidget
{
	private Widget _itemListWidget;

	private Widget _collapseIndicator;

	public Widget ItemListWidget
	{
		get
		{
			return _itemListWidget;
		}
		set
		{
			if (value != _itemListWidget)
			{
				_itemListWidget = value;
				OnPropertyChanged(value, "ItemListWidget");
			}
		}
	}

	public Widget CollapseIndicator
	{
		get
		{
			return _collapseIndicator;
		}
		set
		{
			if (value != _collapseIndicator)
			{
				_collapseIndicator = value;
				OnPropertyChanged(value, "CollapseIndicator");
				CollapseIndicatorUpdated();
			}
		}
	}

	public EncyclopediaDividerButtonWidget(UIContext context)
		: base(context)
	{
	}

	protected override void HandleClick()
	{
		base.HandleClick();
		UpdateItemListVisibility();
		UpdateCollapseIndicator();
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		base.IsVisible = ItemListWidget.ChildCount > 0;
		UpdateCollapseIndicator();
	}

	private void UpdateItemListVisibility()
	{
		if (ItemListWidget != null && ItemListWidget != null)
		{
			ItemListWidget.IsVisible = !ItemListWidget.IsVisible;
		}
	}

	private void UpdateCollapseIndicator()
	{
		if (ItemListWidget != null && ItemListWidget != null && CollapseIndicator != null)
		{
			if (ItemListWidget.IsVisible)
			{
				CollapseIndicator.SetState("Expanded");
			}
			else
			{
				CollapseIndicator.SetState("Collapsed");
			}
		}
	}

	private void CollapseIndicatorUpdated()
	{
		CollapseIndicator.AddState("Collapsed");
		CollapseIndicator.AddState("Expanded");
		UpdateCollapseIndicator();
	}
}
