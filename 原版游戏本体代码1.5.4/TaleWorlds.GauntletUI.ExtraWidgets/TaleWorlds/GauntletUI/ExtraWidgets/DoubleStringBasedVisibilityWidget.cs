using System;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.GauntletUI.ExtraWidgets;

public class DoubleStringBasedVisibilityWidget : Widget
{
	public enum WatchTypes
	{
		Equal,
		NotEqual
	}

	public enum JoinTypes
	{
		And,
		Or
	}

	private WatchTypes _watchType;

	private JoinTypes _joinType;

	private string _firstString;

	private string _secondString;

	private string _comparisonString;

	public WatchTypes WatchType
	{
		get
		{
			return _watchType;
		}
		set
		{
			if (_watchType != value)
			{
				_watchType = value;
				UpdateVisibility();
			}
		}
	}

	public JoinTypes JoinType
	{
		get
		{
			return _joinType;
		}
		set
		{
			if (_joinType != value)
			{
				_joinType = value;
				UpdateVisibility();
			}
		}
	}

	[Editor(false)]
	public string FirstString
	{
		get
		{
			return _firstString;
		}
		set
		{
			if (_firstString != value)
			{
				_firstString = value;
				OnPropertyChanged(value, "FirstString");
				UpdateVisibility();
			}
		}
	}

	[Editor(false)]
	public string SecondString
	{
		get
		{
			return _secondString;
		}
		set
		{
			if (_secondString != value)
			{
				_secondString = value;
				OnPropertyChanged(value, "SecondString");
				UpdateVisibility();
			}
		}
	}

	[Editor(false)]
	public string ComparisonString
	{
		get
		{
			return _comparisonString;
		}
		set
		{
			if (_comparisonString != value)
			{
				_comparisonString = value;
				OnPropertyChanged(value, "ComparisonString");
				UpdateVisibility();
			}
		}
	}

	public DoubleStringBasedVisibilityWidget(UIContext context)
		: base(context)
	{
		UpdateVisibility();
	}

	private void UpdateVisibility()
	{
		bool flag = string.Equals(FirstString, ComparisonString, StringComparison.OrdinalIgnoreCase);
		bool flag2 = string.Equals(SecondString, ComparisonString, StringComparison.OrdinalIgnoreCase);
		if (WatchType == WatchTypes.NotEqual)
		{
			flag = !flag;
			flag2 = !flag2;
		}
		base.IsVisible = ((JoinType == JoinTypes.And) ? (flag && flag2) : (flag || flag2));
	}
}
