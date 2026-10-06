using System;
using System.Collections.Generic;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Options;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions.GamepadOptions;

public class GamepadOptionCategoryVM : GroupedOptionCategoryVM
{
	private enum GamepadType
	{
		Xbox,
		Playstation4,
		Playstation5
	}

	private SelectorVM<SelectorItemVM> _categories;

	private int _currentGamepadType = -1;

	private MBBindingList<GamepadOptionKeyItemVM> _leftAnalogKeys;

	private MBBindingList<GamepadOptionKeyItemVM> _rightAnalogKeys;

	private MBBindingList<GamepadOptionKeyItemVM> _dpadKeys;

	private MBBindingList<GamepadOptionKeyItemVM> _rightTriggerAndBumperKeys;

	private MBBindingList<GamepadOptionKeyItemVM> _leftTriggerAndBumperKeys;

	private MBBindingList<GamepadOptionKeyItemVM> _otherKeys;

	private MBBindingList<GamepadOptionKeyItemVM> _faceKeys;

	private MBBindingList<SelectorVM<SelectorItemVM>> _actions;

	[DataSourceProperty]
	public int CurrentGamepadType
	{
		get
		{
			return _currentGamepadType;
		}
		set
		{
			if (value != _currentGamepadType)
			{
				_currentGamepadType = value;
				OnPropertyChangedWithValue(value, "CurrentGamepadType");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<GamepadOptionKeyItemVM> OtherKeys
	{
		get
		{
			return _otherKeys;
		}
		set
		{
			if (value != _otherKeys)
			{
				_otherKeys = value;
				OnPropertyChangedWithValue(value, "OtherKeys");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<GamepadOptionKeyItemVM> DpadKeys
	{
		get
		{
			return _dpadKeys;
		}
		set
		{
			if (value != _dpadKeys)
			{
				_dpadKeys = value;
				OnPropertyChangedWithValue(value, "DpadKeys");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<GamepadOptionKeyItemVM> LeftTriggerAndBumperKeys
	{
		get
		{
			return _leftTriggerAndBumperKeys;
		}
		set
		{
			if (value != _leftTriggerAndBumperKeys)
			{
				_leftTriggerAndBumperKeys = value;
				OnPropertyChangedWithValue(value, "LeftTriggerAndBumperKeys");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<GamepadOptionKeyItemVM> RightTriggerAndBumperKeys
	{
		get
		{
			return _rightTriggerAndBumperKeys;
		}
		set
		{
			if (value != _rightTriggerAndBumperKeys)
			{
				_rightTriggerAndBumperKeys = value;
				OnPropertyChangedWithValue(value, "RightTriggerAndBumperKeys");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<GamepadOptionKeyItemVM> RightAnalogKeys
	{
		get
		{
			return _rightAnalogKeys;
		}
		set
		{
			if (value != _rightAnalogKeys)
			{
				_rightAnalogKeys = value;
				OnPropertyChangedWithValue(value, "RightAnalogKeys");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<GamepadOptionKeyItemVM> LeftAnalogKeys
	{
		get
		{
			return _leftAnalogKeys;
		}
		set
		{
			if (value != _leftAnalogKeys)
			{
				_leftAnalogKeys = value;
				OnPropertyChangedWithValue(value, "LeftAnalogKeys");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<GamepadOptionKeyItemVM> FaceKeys
	{
		get
		{
			return _faceKeys;
		}
		set
		{
			if (value != _faceKeys)
			{
				_faceKeys = value;
				OnPropertyChangedWithValue(value, "FaceKeys");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<SelectorVM<SelectorItemVM>> Actions
	{
		get
		{
			return _actions;
		}
		set
		{
			if (value != _actions)
			{
				_actions = value;
				OnPropertyChangedWithValue(value, "Actions");
			}
		}
	}

	public GamepadOptionCategoryVM(OptionsVM options, TextObject name, OptionCategory category, bool isEnabled, bool isResetSupported = false)
		: base(options, name, category, isEnabled, isResetSupported)
	{
		OtherKeys = new MBBindingList<GamepadOptionKeyItemVM>();
		DpadKeys = new MBBindingList<GamepadOptionKeyItemVM>();
		LeftAnalogKeys = new MBBindingList<GamepadOptionKeyItemVM>();
		RightAnalogKeys = new MBBindingList<GamepadOptionKeyItemVM>();
		FaceKeys = new MBBindingList<GamepadOptionKeyItemVM>();
		LeftTriggerAndBumperKeys = new MBBindingList<GamepadOptionKeyItemVM>();
		RightTriggerAndBumperKeys = new MBBindingList<GamepadOptionKeyItemVM>();
		if (TaleWorlds.InputSystem.Input.ControllerType == TaleWorlds.InputSystem.Input.ControllerTypes.PlayStationDualSense)
		{
			SetCurrentGamepadType(GamepadType.Playstation5);
		}
		else if (TaleWorlds.InputSystem.Input.ControllerType == TaleWorlds.InputSystem.Input.ControllerTypes.PlayStationDualShock)
		{
			SetCurrentGamepadType(GamepadType.Playstation4);
		}
		else
		{
			SetCurrentGamepadType(GamepadType.Xbox);
		}
		Actions = new MBBindingList<SelectorVM<SelectorItemVM>>();
		_categories = new SelectorVM<SelectorItemVM>(0, null);
		_categories.AddItem(new SelectorItemVM(new TextObject("{=gamepadActionKeybind}Action")));
		_categories.AddItem(new SelectorItemVM(new TextObject("{=gamepadMapKeybind}Map")));
		_categories.SetOnChangeAction(OnCategoryChange);
		_categories.SelectedIndex = 0;
		Actions.Add(_categories);
		RefreshValues();
		TaleWorlds.InputSystem.Input.OnGamepadActiveStateChanged = (Action)Delegate.Combine(TaleWorlds.InputSystem.Input.OnGamepadActiveStateChanged, new Action(OnGamepadActiveStateChanged));
		base.IsEnabled = TaleWorlds.InputSystem.Input.IsGamepadActive;
	}

	private void OnCategoryChange(SelectorVM<SelectorItemVM> obj)
	{
		if (obj.SelectedIndex < 0)
		{
			return;
		}
		OtherKeys.Clear();
		DpadKeys.Clear();
		LeftAnalogKeys.Clear();
		RightAnalogKeys.Clear();
		FaceKeys.Clear();
		LeftTriggerAndBumperKeys.Clear();
		RightTriggerAndBumperKeys.Clear();
		IEnumerable<GamepadOptionKeyItemVM> enumerable = null;
		if (obj.SelectedIndex == 0)
		{
			enumerable = GetActionKeys();
		}
		else if (obj.SelectedIndex == 1)
		{
			enumerable = GetMapKeys();
		}
		foreach (GamepadOptionKeyItemVM item in enumerable)
		{
			InputKey key = item.Key ?? InputKey.Invalid;
			if (Key.IsLeftAnalogInput(key))
			{
				LeftAnalogKeys.Add(item);
			}
			else if (Key.IsRightAnalogInput(key))
			{
				RightAnalogKeys.Add(item);
			}
			else if (Key.IsDpadInput(key))
			{
				DpadKeys.Add(item);
			}
			else if (Key.IsFaceKeyInput(key))
			{
				FaceKeys.Add(item);
			}
			else
			{
				OtherKeys.Add(item);
			}
			if (Key.IsLeftBumperOrTriggerInput(key))
			{
				LeftTriggerAndBumperKeys.Add(item);
			}
			else if (Key.IsRightBumperOrTriggerInput(key))
			{
				RightTriggerAndBumperKeys.Add(item);
			}
		}
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		OtherKeys?.ApplyActionOnAllItems(delegate(GamepadOptionKeyItemVM x)
		{
			x.RefreshValues();
		});
		LeftAnalogKeys?.ApplyActionOnAllItems(delegate(GamepadOptionKeyItemVM x)
		{
			x.RefreshValues();
		});
		RightAnalogKeys?.ApplyActionOnAllItems(delegate(GamepadOptionKeyItemVM x)
		{
			x.RefreshValues();
		});
		FaceKeys?.ApplyActionOnAllItems(delegate(GamepadOptionKeyItemVM x)
		{
			x.RefreshValues();
		});
		DpadKeys?.ApplyActionOnAllItems(delegate(GamepadOptionKeyItemVM x)
		{
			x.RefreshValues();
		});
		LeftTriggerAndBumperKeys?.ApplyActionOnAllItems(delegate(GamepadOptionKeyItemVM x)
		{
			x.RefreshValues();
		});
		RightTriggerAndBumperKeys?.ApplyActionOnAllItems(delegate(GamepadOptionKeyItemVM x)
		{
			x.RefreshValues();
		});
		Actions?.ApplyActionOnAllItems(delegate(SelectorVM<SelectorItemVM> x)
		{
			x.RefreshValues();
		});
	}

	private void SetCurrentGamepadType(GamepadType type)
	{
		CurrentGamepadType = (int)type;
	}

	private void OnGamepadActiveStateChanged()
	{
		base.IsEnabled = TaleWorlds.InputSystem.Input.IsGamepadActive;
		Debug.Print("GAMEPAD TAB ENABLED: " + base.IsEnabled);
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		TaleWorlds.InputSystem.Input.OnGamepadActiveStateChanged = (Action)Delegate.Remove(TaleWorlds.InputSystem.Input.OnGamepadActiveStateChanged, new Action(OnGamepadActiveStateChanged));
	}

	private static IEnumerable<GamepadOptionKeyItemVM> GetActionKeys()
	{
		yield return new GamepadOptionKeyItemVM(InputKey.ControllerLStick, new TextObject("{=i28Kjuay}Move Character"));
		yield return new GamepadOptionKeyItemVM(InputKey.ControllerRStick, new TextObject("{=1hlaGzGI}Look"));
		yield return new GamepadOptionKeyItemVM(InputKey.ControllerLOption, new TextObject("{=9pgOGq7X}Log"));
		yield return new GamepadOptionKeyItemVM(HotKeyManager.GetCategory("CombatHotKeyCategory").GetGameKey(31));
		yield return new GamepadOptionKeyItemVM(HotKeyManager.GetCategory("CombatHotKeyCategory").GetGameKey(33));
		yield return new GamepadOptionKeyItemVM(HotKeyManager.GetCategory("CombatHotKeyCategory").GetGameKey(25));
		yield return new GamepadOptionKeyItemVM(HotKeyManager.GetCategory("CombatHotKeyCategory").GetGameKey(15));
		yield return new GamepadOptionKeyItemVM(HotKeyManager.GetCategory("CombatHotKeyCategory").GetGameKey(13));
		yield return new GamepadOptionKeyItemVM(HotKeyManager.GetCategory("ScoreboardHotKeyCategory").GetHotKey("HoldShow"));
		yield return new GamepadOptionKeyItemVM(HotKeyManager.GetCategory("CombatHotKeyCategory").GetGameKey(16));
		yield return new GamepadOptionKeyItemVM(HotKeyManager.GetCategory("CombatHotKeyCategory").GetGameKey(14));
		yield return new GamepadOptionKeyItemVM(HotKeyManager.GetCategory("CombatHotKeyCategory").GetGameKey(9));
		yield return new GamepadOptionKeyItemVM(HotKeyManager.GetCategory("CombatHotKeyCategory").GetGameKey(10));
		yield return new GamepadOptionKeyItemVM(HotKeyManager.GetCategory("CombatHotKeyCategory").GetGameKey(26));
		yield return new GamepadOptionKeyItemVM(HotKeyManager.GetCategory("CombatHotKeyCategory").GetGameKey(27));
		yield return new GamepadOptionKeyItemVM(HotKeyManager.GetCategory("Generic").GetGameKey(5));
		yield return new GamepadOptionKeyItemVM(HotKeyManager.GetCategory("CombatHotKeyCategory").GetGameKey(34));
		yield return new GamepadOptionKeyItemVM(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("ToggleEscapeMenu"));
	}

	private static IEnumerable<GamepadOptionKeyItemVM> GetMapKeys()
	{
		yield return new GamepadOptionKeyItemVM(InputKey.ControllerLStick, new TextObject("{=hdGay8xc}Map Cursor Move"));
		yield return new GamepadOptionKeyItemVM(InputKey.ControllerRStick, new TextObject("{=atUHbDeM}Map Camera Rotate"));
		yield return new GamepadOptionKeyItemVM(InputKey.ControllerLUp, new TextObject("{=u78WUP9W}Fast Cursor Move Up"));
		yield return new GamepadOptionKeyItemVM(InputKey.ControllerLRight, new TextObject("{=bLPSaLNv}Fast Cursor Move Right"));
		yield return new GamepadOptionKeyItemVM(InputKey.ControllerLLeft, new TextObject("{=82LuSDnd}Fast Cursor Move Left"));
		yield return new GamepadOptionKeyItemVM(InputKey.ControllerLDown, new TextObject("{=nEpZvaEl}Fast Cursor Move Down"));
		yield return new GamepadOptionKeyItemVM(HotKeyManager.GetCategory("MapHotKeyCategory").GetHotKey("MapChangeCursorMode"));
		yield return new GamepadOptionKeyItemVM(HotKeyManager.GetCategory("GenericCampaignPanelsGameKeyCategory").GetGameKey(39));
		yield return new GamepadOptionKeyItemVM(HotKeyManager.GetCategory("MapHotKeyCategory").GetGameKey(63));
		yield return new GamepadOptionKeyItemVM(HotKeyManager.GetCategory("MapHotKeyCategory").GetGameKey(56));
		yield return new GamepadOptionKeyItemVM(HotKeyManager.GetCategory("MapHotKeyCategory").GetGameKey(65));
		yield return new GamepadOptionKeyItemVM(HotKeyManager.GetCategory("MapHotKeyCategory").GetGameKey(57));
		yield return new GamepadOptionKeyItemVM(InputKey.ControllerLBumper, new TextObject("{=mueocuFG}Show Indicators"));
		yield return new GamepadOptionKeyItemVM(HotKeyManager.GetCategory("MapHotKeyCategory").GetGameKey(64));
		yield return new GamepadOptionKeyItemVM(HotKeyManager.GetCategory("MapHotKeyCategory").GetGameKey(66));
		yield return new GamepadOptionKeyItemVM(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("ToggleEscapeMenu"));
		yield return new GamepadOptionKeyItemVM(HotKeyManager.GetCategory("MapHotKeyCategory").GetHotKey("MapClick"));
	}
}
