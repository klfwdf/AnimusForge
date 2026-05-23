using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class FloatComparedStateChangerTextWidget : TextWidget
{
	public enum ComparisonTypes
	{
		Equals,
		NotEquals,
		GreaterThan,
		LessThan,
		GreaterThanOrEqual,
		LessThanOrEqual
	}

	private ComparisonTypes _comparisonType;

	private float _firstValue;

	private float _secondValue;

	private string _trueState;

	private string _falseState;

	public ComparisonTypes ComparisonType
	{
		get
		{
			return _comparisonType;
		}
		set
		{
			if (value != _comparisonType)
			{
				_comparisonType = value;
				UpdateState();
			}
		}
	}

	public float FirstValue
	{
		get
		{
			return _firstValue;
		}
		set
		{
			if (value != _firstValue)
			{
				_firstValue = value;
				UpdateState();
			}
		}
	}

	public float SecondValue
	{
		get
		{
			return _secondValue;
		}
		set
		{
			if (value != _secondValue)
			{
				_secondValue = value;
				UpdateState();
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
				UpdateState();
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
				UpdateState();
			}
		}
	}

	public FloatComparedStateChangerTextWidget(UIContext context)
		: base(context)
	{
	}

	private void UpdateState()
	{
		if (!string.IsNullOrEmpty(TrueState) && !string.IsNullOrEmpty(FalseState))
		{
			bool flag = false;
			if (ComparisonType == ComparisonTypes.Equals)
			{
				flag = FirstValue == SecondValue;
			}
			else if (ComparisonType == ComparisonTypes.NotEquals)
			{
				flag = FirstValue != SecondValue;
			}
			else if (ComparisonType == ComparisonTypes.LessThan)
			{
				flag = FirstValue < SecondValue;
			}
			else if (ComparisonType == ComparisonTypes.GreaterThan)
			{
				flag = FirstValue > SecondValue;
			}
			else if (ComparisonType == ComparisonTypes.GreaterThanOrEqual)
			{
				flag = FirstValue >= SecondValue;
			}
			else if (ComparisonType == ComparisonTypes.LessThanOrEqual)
			{
				flag = FirstValue <= SecondValue;
			}
			SetState(flag ? TrueState : FalseState);
		}
	}
}
