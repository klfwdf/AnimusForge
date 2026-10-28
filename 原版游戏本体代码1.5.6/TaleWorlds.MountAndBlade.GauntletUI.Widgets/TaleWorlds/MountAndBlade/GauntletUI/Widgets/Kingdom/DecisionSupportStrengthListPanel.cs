using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Kingdom;

public class DecisionSupportStrengthListPanel : ListPanel
{
	private ButtonWidget _strengthButton0;

	private RichTextWidget _strengthButton0Text;

	private ButtonWidget _strengthButton1;

	private RichTextWidget _strengthButton1Text;

	private ButtonWidget _strengthButton2;

	private RichTextWidget _strengthButton2Text;

	private int _currentIndex;

	public bool IsAbstain { get; set; }

	public bool IsPlayerSupporter { get; set; }

	public bool IsOptionSelected { get; set; }

	public bool IsKingsOutcome { get; set; }

	[Editor(false)]
	public int CurrentIndex
	{
		get
		{
			return _currentIndex;
		}
		set
		{
			if (_currentIndex != value)
			{
				_currentIndex = value;
				OnPropertyChanged(value, "CurrentIndex");
			}
		}
	}

	[Editor(false)]
	public ButtonWidget StrengthButton0
	{
		get
		{
			return _strengthButton0;
		}
		set
		{
			if (_strengthButton0 != value)
			{
				_strengthButton0 = value;
				OnPropertyChanged(value, "StrengthButton0");
			}
		}
	}

	[Editor(false)]
	public ButtonWidget StrengthButton1
	{
		get
		{
			return _strengthButton1;
		}
		set
		{
			if (_strengthButton1 != value)
			{
				_strengthButton1 = value;
				OnPropertyChanged(value, "StrengthButton1");
			}
		}
	}

	[Editor(false)]
	public ButtonWidget StrengthButton2
	{
		get
		{
			return _strengthButton2;
		}
		set
		{
			if (_strengthButton2 != value)
			{
				_strengthButton2 = value;
				OnPropertyChanged(value, "StrengthButton2");
			}
		}
	}

	[Editor(false)]
	public RichTextWidget StrengthButton0Text
	{
		get
		{
			return _strengthButton0Text;
		}
		set
		{
			if (_strengthButton0Text != value)
			{
				_strengthButton0Text = value;
				OnPropertyChanged(value, "StrengthButton0Text");
			}
		}
	}

	[Editor(false)]
	public RichTextWidget StrengthButton1Text
	{
		get
		{
			return _strengthButton1Text;
		}
		set
		{
			if (_strengthButton1Text != value)
			{
				_strengthButton1Text = value;
				OnPropertyChanged(value, "StrengthButton1Text");
			}
		}
	}

	[Editor(false)]
	public RichTextWidget StrengthButton2Text
	{
		get
		{
			return _strengthButton2Text;
		}
		set
		{
			if (_strengthButton2Text != value)
			{
				_strengthButton2Text = value;
				OnPropertyChanged(value, "StrengthButton2Text");
			}
		}
	}

	public DecisionSupportStrengthListPanel(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		switch (CurrentIndex)
		{
		case 2:
			StrengthButton0.IsSelected = true;
			StrengthButton1.IsSelected = false;
			StrengthButton2.IsSelected = false;
			break;
		case 3:
			StrengthButton0.IsSelected = false;
			StrengthButton1.IsSelected = true;
			StrengthButton2.IsSelected = false;
			break;
		case 4:
			StrengthButton0.IsSelected = false;
			StrengthButton1.IsSelected = false;
			StrengthButton2.IsSelected = true;
			break;
		}
		base.GamepadNavigationIndex = (IsOptionSelected ? (-1) : 0);
	}

	private void SetButtonsEnabled(bool isEnabled)
	{
		StrengthButton0.IsEnabled = isEnabled;
		StrengthButton1.IsEnabled = isEnabled;
		StrengthButton2.IsEnabled = isEnabled;
	}
}
