using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class BoolStateChangerWidget : BrushWidget
{
	private bool _isStateDirty;

	private bool _booleanCheck;

	private string _trueState;

	private string _falseState;

	private Widget _targetWidget;

	private bool _includeChildren;

	[Editor(false)]
	public bool BooleanCheck
	{
		get
		{
			return _booleanCheck;
		}
		set
		{
			if (_booleanCheck != value)
			{
				_booleanCheck = value;
				OnPropertyChanged(value, "BooleanCheck");
				SetStateDirty();
			}
		}
	}

	[Editor(false)]
	public string TrueState
	{
		get
		{
			return _trueState;
		}
		set
		{
			if (_trueState != value)
			{
				_trueState = value;
				OnPropertyChanged(value, "TrueState");
				SetStateDirty();
			}
		}
	}

	[Editor(false)]
	public string FalseState
	{
		get
		{
			return _falseState;
		}
		set
		{
			if (_falseState != value)
			{
				_falseState = value;
				OnPropertyChanged(value, "FalseState");
				SetStateDirty();
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
				SetStateDirty();
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
				SetStateDirty();
			}
		}
	}

	public BoolStateChangerWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		if (_isStateDirty)
		{
			_isStateDirty = false;
			UpdateState();
		}
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

	private void UpdateState()
	{
		string text = (BooleanCheck ? TrueState : FalseState);
		if (text == null)
		{
			Debug.FailedAssert("State is null for BoolStateChangerWidget", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.GauntletUI.Widgets\\BoolStateChangerWidget.cs", "UpdateState", 59);
			return;
		}
		Widget widget = TargetWidget ?? this;
		AddState(widget, text, IncludeChildren);
		SetState(widget, text, IncludeChildren);
	}

	private void SetStateDirty()
	{
		_isStateDirty = true;
	}
}
