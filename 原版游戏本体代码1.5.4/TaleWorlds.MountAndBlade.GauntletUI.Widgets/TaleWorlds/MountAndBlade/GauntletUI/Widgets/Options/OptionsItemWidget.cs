using System.Collections.Generic;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Options;

public class OptionsItemWidget : Widget
{
	private ButtonWidget _booleanToggleButtonWidget;

	private AnimatedDropdownWidget _dropdownWidget;

	private OptionsScreenWidget _screenWidget;

	private Widget _dropdownExtensionParentWidget;

	private bool _eventsRegistered;

	private bool _initialized;

	private List<Sprite> _graphicsSprites;

	private bool _isEnabledStateDirty = true;

	private int _optionTypeID;

	private string _optionDescription;

	private string _optionTitle;

	private string[] _imageIDs;

	private bool _isOptionEnabled;

	public Widget BooleanOption { get; set; }

	public Widget NumericOption { get; set; }

	public Widget StringOption { get; set; }

	public Widget GameKeyOption { get; set; }

	public Widget ActionOption { get; set; }

	public Widget InputOption { get; set; }

	public AnimatedDropdownWidget DropdownWidget
	{
		get
		{
			return _dropdownWidget;
		}
		set
		{
			if (value != _dropdownWidget)
			{
				_dropdownWidget = value;
			}
		}
	}

	public ButtonWidget BooleanToggleButtonWidget
	{
		get
		{
			return _booleanToggleButtonWidget;
		}
		set
		{
			if (value != _booleanToggleButtonWidget)
			{
				_booleanToggleButtonWidget = value;
			}
		}
	}

	public int OptionTypeID
	{
		get
		{
			return _optionTypeID;
		}
		set
		{
			if (_optionTypeID != value)
			{
				_optionTypeID = value;
				OnPropertyChanged(value, "OptionTypeID");
			}
		}
	}

	public bool IsOptionEnabled
	{
		get
		{
			return _isOptionEnabled;
		}
		set
		{
			if (_isOptionEnabled != value)
			{
				_isOptionEnabled = value;
				OnPropertyChanged(value, "IsOptionEnabled");
				_isEnabledStateDirty = true;
			}
		}
	}

	public string OptionTitle
	{
		get
		{
			return _optionTitle;
		}
		set
		{
			if (_optionTitle != value)
			{
				_optionTitle = value;
			}
		}
	}

	public string[] ImageIDs
	{
		get
		{
			return _imageIDs;
		}
		set
		{
			if (_imageIDs != value)
			{
				_imageIDs = value;
			}
		}
	}

	public string OptionDescription
	{
		get
		{
			return _optionDescription;
		}
		set
		{
			if (_optionDescription != value)
			{
				_optionDescription = value;
			}
		}
	}

	public OptionsItemWidget(UIContext context)
		: base(context)
	{
		_optionTypeID = -1;
		_graphicsSprites = new List<Sprite>();
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (!_initialized)
		{
			SetCurrentScreenWidget(FindScreenWidget(base.ParentWidget));
			if (ImageIDs != null)
			{
				for (int i = 0; i < ImageIDs.Length; i++)
				{
					if (ImageIDs[i] != string.Empty)
					{
						Sprite sprite = base.Context.SpriteData.GetSprite(ImageIDs[i]);
						_graphicsSprites.Add(sprite);
					}
				}
			}
			RefreshVisibilityOfSubItems();
			ResetNavigationIndices();
			_initialized = true;
		}
		if (!_eventsRegistered)
		{
			RegisterHoverEvents();
			_eventsRegistered = true;
		}
		if (_isEnabledStateDirty)
		{
			GetCurrentOptionWidget()?.ApplyActionToAllChildrenRecursive(delegate(Widget child)
			{
				child.IsEnabled = IsOptionEnabled;
			});
			_isEnabledStateDirty = false;
		}
	}

	protected override void OnHoverBegin()
	{
		base.OnHoverBegin();
		SetCurrentOption(fromHoverOverDropdown: false, fromBooleanSelection: false);
	}

	protected override void OnHoverEnd()
	{
		base.OnHoverEnd();
		ResetCurrentOption();
	}

	private OptionsScreenWidget FindScreenWidget(Widget parent)
	{
		if (parent is OptionsScreenWidget result)
		{
			return result;
		}
		if (parent == null)
		{
			return null;
		}
		return FindScreenWidget(parent.ParentWidget);
	}

	private void SetCurrentOption(bool fromHoverOverDropdown, bool fromBooleanSelection, int hoverDropdownItemIndex = -1)
	{
		if (_optionTypeID == 3)
		{
			Sprite sprite = null;
			sprite = ((!fromHoverOverDropdown) ? ((_graphicsSprites.Count > DropdownWidget.CurrentSelectedIndex && DropdownWidget.CurrentSelectedIndex >= 0) ? _graphicsSprites[DropdownWidget.CurrentSelectedIndex] : null) : ((_graphicsSprites.Count > hoverDropdownItemIndex) ? _graphicsSprites[hoverDropdownItemIndex] : null));
			_screenWidget?.SetCurrentOption(this, sprite);
		}
		else if (_optionTypeID == 0)
		{
			int num = ((!BooleanToggleButtonWidget.IsSelected) ? 1 : 0);
			Sprite newgraphicsSprite = ((_graphicsSprites.Count > num) ? _graphicsSprites[num] : null);
			_screenWidget?.SetCurrentOption(this, newgraphicsSprite);
		}
		else
		{
			_screenWidget?.SetCurrentOption(this, null);
		}
	}

	public void SetCurrentScreenWidget(OptionsScreenWidget screenWidget)
	{
		_screenWidget = screenWidget;
	}

	private void ResetCurrentOption()
	{
		_screenWidget?.SetCurrentOption(null, null);
	}

	private void RegisterHoverEvents()
	{
		ApplyActionToAllChildrenRecursive(delegate(Widget child)
		{
			child.boolPropertyChanged += Child_PropertyChanged;
		});
		if (OptionTypeID == 0)
		{
			BooleanToggleButtonWidget.boolPropertyChanged += BooleanOption_PropertyChanged;
		}
		else if (OptionTypeID == 3)
		{
			_dropdownExtensionParentWidget = DropdownWidget.DropdownClipWidget;
			_dropdownExtensionParentWidget.ApplyActionToAllChildrenRecursive(delegate(Widget child)
			{
				child.boolPropertyChanged += DropdownItem_PropertyChanged1;
			});
		}
	}

	private void BooleanOption_PropertyChanged(PropertyOwnerObject childWidget, string propertyName, bool propertyValue)
	{
		if (propertyName == "IsSelected")
		{
			SetCurrentOption(fromHoverOverDropdown: false, fromBooleanSelection: true);
		}
	}

	private void Child_PropertyChanged(PropertyOwnerObject childWidget, string propertyName, bool propertyValue)
	{
		if (propertyName == "IsHovered")
		{
			if (propertyValue)
			{
				SetCurrentOption(fromHoverOverDropdown: false, fromBooleanSelection: false);
			}
			else
			{
				ResetCurrentOption();
			}
		}
	}

	private void DropdownItem_PropertyChanged1(PropertyOwnerObject childWidget, string propertyName, bool propertyValue)
	{
		if (propertyName == "IsHovered")
		{
			if (propertyValue)
			{
				Widget widget = childWidget as Widget;
				SetCurrentOption(fromHoverOverDropdown: true, fromBooleanSelection: false, widget.ParentWidget.GetChildIndex(widget));
			}
			else
			{
				ResetCurrentOption();
			}
		}
	}

	private void RefreshVisibilityOfSubItems()
	{
		BooleanOption.IsVisible = OptionTypeID == 0;
		NumericOption.IsVisible = OptionTypeID == 1;
		StringOption.IsVisible = OptionTypeID == 3;
		GameKeyOption.IsVisible = OptionTypeID == 2;
		InputOption.IsVisible = OptionTypeID == 4;
		if (ActionOption != null)
		{
			ActionOption.IsVisible = OptionTypeID == 5;
		}
	}

	private Widget GetCurrentOptionWidget()
	{
		return OptionTypeID switch
		{
			0 => BooleanOption, 
			1 => NumericOption, 
			2 => StringOption, 
			3 => GameKeyOption, 
			4 => InputOption, 
			5 => ActionOption, 
			_ => null, 
		};
	}

	private void ResetNavigationIndices()
	{
		if (base.GamepadNavigationIndex == -1)
		{
			return;
		}
		bool flag = false;
		Widget booleanOption = BooleanOption;
		if (booleanOption != null && booleanOption.IsVisible)
		{
			BooleanOption.GamepadNavigationIndex = base.GamepadNavigationIndex;
			flag = true;
		}
		else
		{
			Widget numericOption = NumericOption;
			if (numericOption != null && numericOption.IsVisible)
			{
				NumericOption.GamepadNavigationIndex = base.GamepadNavigationIndex;
				flag = true;
			}
			else
			{
				Widget stringOption = StringOption;
				if (stringOption != null && stringOption.IsVisible)
				{
					StringOption.GamepadNavigationIndex = base.GamepadNavigationIndex;
					flag = true;
				}
				else
				{
					Widget gameKeyOption = GameKeyOption;
					if (gameKeyOption != null && gameKeyOption.IsVisible)
					{
						GameKeyOption.GamepadNavigationIndex = base.GamepadNavigationIndex;
						flag = true;
					}
					else
					{
						Widget inputOption = InputOption;
						if (inputOption != null && inputOption.IsVisible)
						{
							InputOption.GamepadNavigationIndex = base.GamepadNavigationIndex;
							flag = true;
						}
						else
						{
							Widget actionOption = ActionOption;
							if (actionOption != null && actionOption.IsVisible)
							{
								ActionOption.GamepadNavigationIndex = base.GamepadNavigationIndex;
								flag = true;
							}
						}
					}
				}
			}
		}
		if (flag)
		{
			base.GamepadNavigationIndex = -1;
		}
		else
		{
			Debug.FailedAssert("No option type is visible for: " + GetType().Name, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.GauntletUI.Widgets\\Options\\OptionsItemWidget.cs", "ResetNavigationIndices", 310);
		}
	}

	protected override void OnGamepadNavigationIndexUpdated(int newIndex)
	{
		if (_initialized)
		{
			ResetNavigationIndices();
		}
	}
}
