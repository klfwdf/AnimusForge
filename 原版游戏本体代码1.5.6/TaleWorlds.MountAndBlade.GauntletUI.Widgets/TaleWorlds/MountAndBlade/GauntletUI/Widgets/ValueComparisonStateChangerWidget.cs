using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class ValueComparisonStateChangerWidget : BrushWidget
{
	public enum WatchTypes
	{
		Equals,
		NotEquals,
		GreaterThan,
		LessThan
	}

	private bool _isScheduledForUpdate;

	private Widget _targetWidget;

	private WatchTypes _watchType;

	private float _firstValueFloat;

	private float _secondValueFloat;

	private string _trueState;

	private string _falseState;

	public Widget TargetWidget
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
				SetDirty();
			}
		}
	}

	public WatchTypes WatchType
	{
		get
		{
			return _watchType;
		}
		set
		{
			if (value != _watchType)
			{
				_watchType = value;
				SetDirty();
			}
		}
	}

	public int FirstValueInt
	{
		get
		{
			return (int)_firstValueFloat;
		}
		set
		{
			if (value != (int)_firstValueFloat)
			{
				_firstValueFloat = value;
				OnPropertyChanged(value, "FirstValueInt");
				SetDirty();
			}
		}
	}

	public int SecondValueInt
	{
		get
		{
			return (int)_secondValueFloat;
		}
		set
		{
			if (value != (int)_secondValueFloat)
			{
				_secondValueFloat = value;
				OnPropertyChanged(value, "SecondValueInt");
				SetDirty();
			}
		}
	}

	public float FirstValueFloat
	{
		get
		{
			return _firstValueFloat;
		}
		set
		{
			if (value != _firstValueFloat)
			{
				_firstValueFloat = value;
				OnPropertyChanged(value, "FirstValueFloat");
				SetDirty();
			}
		}
	}

	public float SecondValueFloat
	{
		get
		{
			return _secondValueFloat;
		}
		set
		{
			if (value != _secondValueFloat)
			{
				_secondValueFloat = value;
				OnPropertyChanged(value, "SecondValueFloat");
				SetDirty();
			}
		}
	}

	public string TrueState
	{
		get
		{
			return _trueState;
		}
		set
		{
			if (value != _trueState)
			{
				_trueState = value;
				OnPropertyChanged(value, "TrueState");
				SetDirty();
			}
		}
	}

	public string FalseState
	{
		get
		{
			return _falseState;
		}
		set
		{
			if (value != _falseState)
			{
				_falseState = value;
				OnPropertyChanged(value, "FalseState");
				SetDirty();
			}
		}
	}

	public ValueComparisonStateChangerWidget(UIContext context)
		: base(context)
	{
	}

	private void UpdateState(float dt)
	{
		bool flag = false;
		switch (WatchType)
		{
		case WatchTypes.Equals:
			flag = FirstValueFloat == SecondValueFloat;
			break;
		case WatchTypes.NotEquals:
			flag = FirstValueFloat != SecondValueFloat;
			break;
		case WatchTypes.GreaterThan:
			flag = FirstValueFloat > SecondValueFloat;
			break;
		case WatchTypes.LessThan:
			flag = FirstValueFloat < SecondValueFloat;
			break;
		}
		(TargetWidget ?? this).SetState(flag ? TrueState : FalseState);
		_isScheduledForUpdate = false;
	}

	private void SetDirty()
	{
		if (!_isScheduledForUpdate)
		{
			base.EventManager.AddLateUpdateAction(this, UpdateState, 1);
			_isScheduledForUpdate = true;
		}
	}
}
