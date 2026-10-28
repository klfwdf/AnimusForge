using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.GauntletUI.ExtraWidgets;

public class StateSyncWidget : BrushWidget
{
	private Widget _sourceWidget;

	private Widget _targetWidget;

	[Editor(false)]
	public Widget SourceWidget
	{
		get
		{
			return _sourceWidget;
		}
		set
		{
			if (_sourceWidget != value)
			{
				_sourceWidget = value;
				OnPropertyChanged(value, "SourceWidget");
			}
		}
	}

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
			}
		}
	}

	public StateSyncWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		(TargetWidget ?? this).SetState(SourceWidget?.CurrentState ?? "Default");
	}
}
