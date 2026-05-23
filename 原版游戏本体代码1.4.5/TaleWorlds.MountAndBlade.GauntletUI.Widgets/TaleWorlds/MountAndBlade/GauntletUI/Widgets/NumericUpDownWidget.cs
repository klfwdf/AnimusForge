using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class NumericUpDownWidget : Widget
{
	private bool _showOneAdded;

	private float _minValue;

	private float _maxValue;

	private int _intValue = int.MinValue;

	private float _value = float.MinValue;

	private TextWidget _textWidget;

	private ButtonWidget _upButton;

	private ButtonWidget _downButton;

	[Editor(false)]
	public bool ShowOneAdded
	{
		get
		{
			return _showOneAdded;
		}
		set
		{
			if (_showOneAdded != value)
			{
				_showOneAdded = value;
				OnPropertyChanged(value, "ShowOneAdded");
			}
		}
	}

	[Editor(false)]
	public int IntValue
	{
		get
		{
			return _intValue;
		}
		set
		{
			if (_intValue != value)
			{
				_intValue = value;
				Value = _intValue;
				OnPropertyChanged(value, "IntValue");
				_textWidget.IntText = (ShowOneAdded ? (IntValue + 1) : IntValue);
				UpdateControlButtonsEnabled();
			}
		}
	}

	[Editor(false)]
	public float Value
	{
		get
		{
			return _value;
		}
		set
		{
			if (_value != value)
			{
				_value = value;
				IntValue = (int)_value;
				OnPropertyChanged(value, "Value");
			}
		}
	}

	[Editor(false)]
	public float MinValue
	{
		get
		{
			return _minValue;
		}
		set
		{
			if (value != _minValue)
			{
				_minValue = value;
				OnPropertyChanged(value, "MinValue");
				UpdateControlButtonsEnabled();
			}
		}
	}

	[Editor(false)]
	public float MaxValue
	{
		get
		{
			return _maxValue;
		}
		set
		{
			if (value != _maxValue)
			{
				_maxValue = value;
				OnPropertyChanged(value, "MaxValue");
				UpdateControlButtonsEnabled();
			}
		}
	}

	[Editor(false)]
	public TextWidget TextWidget
	{
		get
		{
			return _textWidget;
		}
		set
		{
			if (_textWidget != value)
			{
				_textWidget = value;
				OnPropertyChanged(value, "TextWidget");
			}
		}
	}

	[Editor(false)]
	public ButtonWidget UpButton
	{
		get
		{
			return _upButton;
		}
		set
		{
			if (_upButton != value)
			{
				_upButton = value;
				OnPropertyChanged(value, "UpButton");
				if (value != null && !_upButton.ClickEventHandlers.Contains(OnUpButtonClicked))
				{
					_upButton.ClickEventHandlers.Add(OnUpButtonClicked);
				}
			}
		}
	}

	[Editor(false)]
	public ButtonWidget DownButton
	{
		get
		{
			return _downButton;
		}
		set
		{
			if (_downButton != value)
			{
				_downButton = value;
				OnPropertyChanged(value, "DownButton");
				if (value != null && !_downButton.ClickEventHandlers.Contains(OnDownButtonClicked))
				{
					_downButton.ClickEventHandlers.Add(OnDownButtonClicked);
				}
			}
		}
	}

	public NumericUpDownWidget(UIContext context)
		: base(context)
	{
	}

	private void OnUpButtonClicked(Widget widget)
	{
		ChangeValue(1);
	}

	private void OnDownButtonClicked(Widget widget)
	{
		ChangeValue(-1);
	}

	private void ChangeValue(int changeAmount)
	{
		int num = IntValue + changeAmount;
		if ((float)num <= MaxValue && (float)num >= MinValue)
		{
			IntValue = num;
		}
	}

	private void UpdateControlButtonsEnabled()
	{
		if (UpButton != null)
		{
			UpButton.IsEnabled = (float)(_intValue + 1) <= MaxValue;
		}
		if (DownButton != null)
		{
			DownButton.IsEnabled = (float)(_intValue - 1) >= MinValue;
		}
	}
}
