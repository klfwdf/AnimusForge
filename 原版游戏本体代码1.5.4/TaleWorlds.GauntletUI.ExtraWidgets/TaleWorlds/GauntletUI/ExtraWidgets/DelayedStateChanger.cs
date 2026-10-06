using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.GauntletUI.ExtraWidgets;

public class DelayedStateChanger : BrushWidget
{
	private bool _isStarted;

	private bool _isFinished;

	private float _timePassed;

	private Widget _widget;

	private string _defaultState;

	private bool _autoStart;

	private bool _trigger;

	private bool _stateResetable;

	private bool _includeChildren;

	private float _delay;

	private string _state;

	private Widget _targetWidget;

	[Editor(false)]
	public bool AutoStart
	{
		get
		{
			return _autoStart;
		}
		set
		{
			if (_autoStart != value)
			{
				_autoStart = value;
				OnPropertyChanged(value, "AutoStart");
			}
		}
	}

	[Editor(false)]
	public bool Trigger
	{
		get
		{
			return _trigger;
		}
		set
		{
			if (_trigger != value)
			{
				_trigger = value;
				OnPropertyChanged(value, "Trigger");
				TriggerUpdated();
			}
		}
	}

	[Editor(false)]
	public bool StateResetable
	{
		get
		{
			return _stateResetable;
		}
		set
		{
			if (_stateResetable != value)
			{
				_stateResetable = value;
				OnPropertyChanged(value, "StateResetable");
			}
		}
	}

	[Editor(false)]
	public bool IncludeChildren
	{
		get
		{
			return _includeChildren;
		}
		set
		{
			if (_includeChildren != value)
			{
				_includeChildren = value;
				OnPropertyChanged(value, "IncludeChildren");
			}
		}
	}

	[Editor(false)]
	public float Delay
	{
		get
		{
			return _delay;
		}
		set
		{
			if (_delay != value)
			{
				_delay = value;
				OnPropertyChanged(value, "Delay");
			}
		}
	}

	[Editor(false)]
	public string State
	{
		get
		{
			return _state;
		}
		set
		{
			if (_state != value)
			{
				_state = value;
				OnPropertyChanged(value, "State");
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
				TargetWidgetUpdated();
			}
		}
	}

	public DelayedStateChanger(UIContext context)
		: base(context)
	{
		_isStarted = false;
		_isFinished = false;
		_timePassed = 0f;
	}

	protected override void OnConnectedToRoot()
	{
		_defaultState = base.CurrentState;
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		if (_isFinished || string.IsNullOrEmpty(State))
		{
			return;
		}
		if (!_isStarted)
		{
			if (AutoStart)
			{
				Start();
			}
			return;
		}
		_timePassed += dt;
		if (_timePassed >= Delay)
		{
			_isFinished = true;
			SetState(_widget, State, IncludeChildren);
		}
	}

	public void Start()
	{
		_isStarted = true;
		_isFinished = false;
		_timePassed = 0f;
		_widget = TargetWidget ?? this;
		AddState(_widget, State, IncludeChildren);
	}

	private void Reset()
	{
		_isStarted = false;
		_isFinished = true;
		_widget = TargetWidget ?? this;
		SetState(_widget, _defaultState, IncludeChildren);
	}

	private void AddState(Widget widget, string state, bool includeChildren)
	{
		widget.AddState(state);
		if (includeChildren)
		{
			for (int i = 0; i < widget.ChildCount; i++)
			{
				AddState(widget.GetChild(i), state, includeChildren: true);
			}
		}
	}

	private void SetState(Widget widget, string state, bool includeChildren)
	{
		widget.SetState(state);
		if (includeChildren)
		{
			for (int i = 0; i < widget.ChildCount; i++)
			{
				SetState(widget.GetChild(i), state, includeChildren: true);
			}
		}
	}

	private void TriggerUpdated()
	{
		if (Trigger)
		{
			Start();
		}
		else if (StateResetable)
		{
			Reset();
		}
	}

	private void TargetWidgetUpdated()
	{
		_defaultState = ((TargetWidget == null) ? base.CurrentState : TargetWidget.CurrentState);
	}
}
