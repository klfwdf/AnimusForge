using System.Collections.Generic;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class BoolBrushChangerBrushWidget : BrushWidget
{
	private bool _initialUpdateHandled;

	private bool _booleanCheck;

	private string _trueBrush;

	private string _falseBrush;

	private BrushWidget _targetWidget;

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
				OnBooleanUpdated();
			}
		}
	}

	[Editor(false)]
	public string TrueBrush
	{
		get
		{
			return _trueBrush;
		}
		set
		{
			if (_trueBrush != value)
			{
				_trueBrush = value;
				OnPropertyChanged(value, "TrueBrush");
			}
		}
	}

	[Editor(false)]
	public string FalseBrush
	{
		get
		{
			return _falseBrush;
		}
		set
		{
			if (_falseBrush != value)
			{
				_falseBrush = value;
				OnPropertyChanged(value, "FalseBrush");
			}
		}
	}

	[Editor(false)]
	public BrushWidget TargetWidget
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

	public BoolBrushChangerBrushWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (!_initialUpdateHandled)
		{
			OnBooleanUpdated();
			_initialUpdateHandled = true;
		}
	}

	private void OnBooleanUpdated()
	{
		string name = (BooleanCheck ? TrueBrush : FalseBrush);
		Brush brush = base.Context.GetBrush(name);
		BrushWidget brushWidget = TargetWidget ?? this;
		brushWidget.Brush = brush;
		if (!IncludeChildren)
		{
			return;
		}
		List<Widget> allChildrenRecursive = brushWidget.GetAllChildrenRecursive();
		for (int i = 0; i < allChildrenRecursive.Count; i++)
		{
			if (allChildrenRecursive[i] is BrushWidget brushWidget2)
			{
				brushWidget2.Brush = brush;
			}
		}
	}
}
