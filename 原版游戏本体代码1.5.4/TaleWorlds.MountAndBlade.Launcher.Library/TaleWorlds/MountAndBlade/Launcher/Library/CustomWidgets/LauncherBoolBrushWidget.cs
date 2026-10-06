using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Launcher.Library.CustomWidgets;

public class LauncherBoolBrushWidget : BrushWidget
{
	private bool _boolVariable;

	private BrushWidget _targetWidget;

	private Brush _onTrueBrush;

	private Brush _onFalseBrush;

	[DataSourceProperty]
	public bool BoolVariable
	{
		get
		{
			return _boolVariable;
		}
		set
		{
			if (value != _boolVariable)
			{
				_boolVariable = value;
				OnPropertyChanged(value, "BoolVariable");
				BoolVariableUpdated();
			}
		}
	}

	[DataSourceProperty]
	public BrushWidget TargetWidget
	{
		get
		{
			return _targetWidget;
		}
		set
		{
			if (value != _targetWidget)
			{
				_targetWidget = value;
				OnPropertyChanged(value, "TargetWidget");
			}
		}
	}

	[DataSourceProperty]
	public Brush OnTrueBrush
	{
		get
		{
			return _onTrueBrush;
		}
		set
		{
			if (value != _onTrueBrush)
			{
				_onTrueBrush = value;
				OnPropertyChanged(value, "OnTrueBrush");
			}
		}
	}

	[DataSourceProperty]
	public Brush OnFalseBrush
	{
		get
		{
			return _onFalseBrush;
		}
		set
		{
			if (value != _onFalseBrush)
			{
				_onFalseBrush = value;
				OnPropertyChanged(value, "OnFalseBrush");
			}
		}
	}

	public LauncherBoolBrushWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnConnectedToRoot()
	{
		base.OnConnectedToRoot();
		BoolVariableUpdated();
	}

	private void BoolVariableUpdated()
	{
		(TargetWidget ?? this).Brush = (BoolVariable ? OnTrueBrush : OnFalseBrush);
	}
}
