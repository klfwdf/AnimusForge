using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class SortButtonWidget : ButtonWidget
{
	private int _sortState;

	private BrushWidget _sortVisualWidget;

	[Editor(false)]
	public int SortState
	{
		get
		{
			return _sortState;
		}
		set
		{
			if (_sortState != value)
			{
				_sortState = value;
				OnPropertyChanged(value, "SortState");
			}
		}
	}

	[Editor(false)]
	public BrushWidget SortVisualWidget
	{
		get
		{
			return _sortVisualWidget;
		}
		set
		{
			if (_sortVisualWidget != value)
			{
				_sortVisualWidget = value;
				OnPropertyChanged(value, "SortVisualWidget");
			}
		}
	}

	public SortButtonWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (SortVisualWidget == null)
		{
			return;
		}
		if (base.IsSelected)
		{
			switch (SortState)
			{
			case 0:
				SortVisualWidget.SetState("Default");
				break;
			case 1:
				SortVisualWidget.SetState("Ascending");
				break;
			case 2:
				SortVisualWidget.SetState("Descending");
				break;
			}
		}
		else
		{
			SortVisualWidget.SetState("Default");
		}
	}
}
