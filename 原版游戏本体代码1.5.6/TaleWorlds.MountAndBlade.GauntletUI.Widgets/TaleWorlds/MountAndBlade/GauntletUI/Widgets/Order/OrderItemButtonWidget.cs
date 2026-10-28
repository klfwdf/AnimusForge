using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Order;

public class OrderItemButtonWidget : ButtonWidget
{
	private string _selectionState;

	private ImageWidget _selectionVisualWidget;

	[Editor(false)]
	public string SelectionState
	{
		get
		{
			return _selectionState;
		}
		set
		{
			if (_selectionState != value)
			{
				_selectionState = value;
				OnPropertyChanged(value, "SelectionState");
				SelectionStateChanged();
			}
		}
	}

	[Editor(false)]
	public ImageWidget SelectionVisualWidget
	{
		get
		{
			return _selectionVisualWidget;
		}
		set
		{
			if (_selectionVisualWidget != value)
			{
				_selectionVisualWidget = value;
				OnPropertyChanged(value, "SelectionVisualWidget");
				if (value != null)
				{
					value.AddState("Disabled");
					value.AddState("PartiallyActive");
					value.AddState("Active");
				}
				SelectionStateChanged();
			}
		}
	}

	public OrderItemButtonWidget(UIContext context)
		: base(context)
	{
	}

	private void SelectionStateChanged()
	{
		if (!string.IsNullOrEmpty(SelectionState))
		{
			ImageWidget selectionVisualWidget = SelectionVisualWidget;
			if (selectionVisualWidget != null && selectionVisualWidget.ContainsState(SelectionState))
			{
				SelectionVisualWidget.SetState(SelectionState);
			}
		}
	}
}
