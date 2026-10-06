using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.CharacterCreation.Options;

public class CharacterCreationOptionsItemWidget : Widget
{
	private bool _isDirty = true;

	private int _type;

	private Widget _actionOptionWidget;

	private Widget _numericOptionWidget;

	private Widget _selectionOptionWidget;

	private Widget _booleanOptionWidget;

	[Editor(false)]
	public int Type
	{
		get
		{
			return _type;
		}
		set
		{
			if (_type != value)
			{
				_type = value;
				OnPropertyChanged(value, "Type");
				_isDirty = true;
			}
		}
	}

	[Editor(false)]
	public Widget ActionOptionWidget
	{
		get
		{
			return _actionOptionWidget;
		}
		set
		{
			if (_actionOptionWidget != value)
			{
				_actionOptionWidget = value;
				OnPropertyChanged(value, "ActionOptionWidget");
			}
		}
	}

	[Editor(false)]
	public Widget NumericOptionWidget
	{
		get
		{
			return _numericOptionWidget;
		}
		set
		{
			if (_numericOptionWidget != value)
			{
				_numericOptionWidget = value;
				OnPropertyChanged(value, "NumericOptionWidget");
			}
		}
	}

	[Editor(false)]
	public Widget SelectionOptionWidget
	{
		get
		{
			return _selectionOptionWidget;
		}
		set
		{
			if (_selectionOptionWidget != value)
			{
				_selectionOptionWidget = value;
				OnPropertyChanged(value, "SelectionOptionWidget");
			}
		}
	}

	[Editor(false)]
	public Widget BooleanOptionWidget
	{
		get
		{
			return _booleanOptionWidget;
		}
		set
		{
			if (_booleanOptionWidget != value)
			{
				_booleanOptionWidget = value;
				OnPropertyChanged(value, "BooleanOptionWidget");
			}
		}
	}

	public CharacterCreationOptionsItemWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (_isDirty)
		{
			if (Type == 0)
			{
				ActionOptionWidget.IsVisible = false;
				BooleanOptionWidget.IsVisible = true;
				SelectionOptionWidget.IsVisible = false;
				NumericOptionWidget.IsVisible = false;
			}
			else if (Type == 1)
			{
				ActionOptionWidget.IsVisible = false;
				BooleanOptionWidget.IsVisible = false;
				SelectionOptionWidget.IsVisible = false;
				NumericOptionWidget.IsVisible = true;
			}
			else if (Type == 2)
			{
				ActionOptionWidget.IsVisible = false;
				BooleanOptionWidget.IsVisible = false;
				SelectionOptionWidget.IsVisible = true;
				NumericOptionWidget.IsVisible = false;
			}
			else if (Type == 3)
			{
				ActionOptionWidget.IsVisible = true;
				BooleanOptionWidget.IsVisible = false;
				SelectionOptionWidget.IsVisible = false;
				NumericOptionWidget.IsVisible = false;
			}
			ResetNavigationIndices();
			_isDirty = false;
		}
	}

	private void ResetNavigationIndices()
	{
		if (base.GamepadNavigationIndex == -1)
		{
			return;
		}
		bool flag = false;
		Widget booleanOptionWidget = BooleanOptionWidget;
		if (booleanOptionWidget != null && booleanOptionWidget.IsVisible)
		{
			BooleanOptionWidget.GamepadNavigationIndex = base.GamepadNavigationIndex;
			flag = true;
		}
		else
		{
			Widget numericOptionWidget = NumericOptionWidget;
			if (numericOptionWidget != null && numericOptionWidget.IsVisible)
			{
				NumericOptionWidget.GamepadNavigationIndex = base.GamepadNavigationIndex;
				flag = true;
			}
			else
			{
				Widget selectionOptionWidget = SelectionOptionWidget;
				if (selectionOptionWidget != null && selectionOptionWidget.IsVisible)
				{
					SelectionOptionWidget.GamepadNavigationIndex = base.GamepadNavigationIndex;
					flag = true;
				}
				else
				{
					Widget actionOptionWidget = ActionOptionWidget;
					if (actionOptionWidget != null && actionOptionWidget.IsVisible)
					{
						ActionOptionWidget.GamepadNavigationIndex = base.GamepadNavigationIndex;
						flag = true;
					}
				}
			}
		}
		if (flag)
		{
			base.GamepadNavigationIndex = -1;
		}
	}

	protected override void OnGamepadNavigationIndexUpdated(int newIndex)
	{
		base.OnGamepadNavigationIndexUpdated(newIndex);
		ResetNavigationIndices();
	}
}
