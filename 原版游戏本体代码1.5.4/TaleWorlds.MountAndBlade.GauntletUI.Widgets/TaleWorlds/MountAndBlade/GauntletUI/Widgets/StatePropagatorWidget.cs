using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class StatePropagatorWidget : Widget
{
	private Widget _targetWidget;

	[Editor(false)]
	public Widget TargetWidget
	{
		get
		{
			return _targetWidget;
		}
		set
		{
			if (_targetWidget != value)
			{
				_targetWidget = value;
				OnPropertyChanged(value, "TargetWidget");
				TargetWidget?.SetState(base.CurrentState);
			}
		}
	}

	public StatePropagatorWidget(UIContext context)
		: base(context)
	{
	}

	public override void SetState(string stateName)
	{
		base.SetState(stateName);
		TargetWidget?.SetState(stateName);
	}
}
