using System;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.Input;

public class InputKeyItemVM : ViewModel
{
	private bool _isVisibleToConsoleOnly;

	private TextObject _forcedName;

	private string _forcedID;

	private bool? _forcedVisibility;

	private string _keyID;

	private string _keyName;

	private bool _isVisible;

	public GameKey GameKey { get; private set; }

	public HotKey HotKey { get; private set; }

	[DataSourceProperty]
	public string KeyID
	{
		get
		{
			return _keyID;
		}
		set
		{
			if (value != _keyID)
			{
				_keyID = value;
				OnPropertyChangedWithValue(value, "KeyID");
			}
		}
	}

	[DataSourceProperty]
	public string KeyName
	{
		get
		{
			return _keyName;
		}
		set
		{
			if (value != _keyName)
			{
				_keyName = value;
				OnPropertyChangedWithValue(value, "KeyName");
			}
		}
	}

	[DataSourceProperty]
	public bool IsVisible
	{
		get
		{
			return _isVisible;
		}
		set
		{
			if (value != _isVisible)
			{
				_isVisible = value;
				OnPropertyChangedWithValue(value, "IsVisible");
			}
		}
	}

	private InputKeyItemVM()
	{
		TaleWorlds.InputSystem.Input.OnGamepadActiveStateChanged = (Action)Delegate.Combine(TaleWorlds.InputSystem.Input.OnGamepadActiveStateChanged, new Action(OnGamepadActiveStateChanged));
		HotKeyManager.OnKeybindsChanged += OnKeybindsChanged;
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		TaleWorlds.InputSystem.Input.OnGamepadActiveStateChanged = (Action)Delegate.Remove(TaleWorlds.InputSystem.Input.OnGamepadActiveStateChanged, new Action(OnGamepadActiveStateChanged));
		HotKeyManager.OnKeybindsChanged -= OnKeybindsChanged;
	}

	private void OnGamepadActiveStateChanged()
	{
		ForceRefresh();
	}

	private void OnKeybindsChanged()
	{
		ForceRefresh();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		ForceRefresh();
	}

	public void SetForcedVisibility(bool? isVisible)
	{
		_forcedVisibility = isVisible;
		UpdateVisibility();
	}

	private void ForceRefresh()
	{
		UpdateVisibility();
		KeyID = string.Empty;
		KeyName = string.Empty;
		if (_forcedID != null)
		{
			KeyID = _forcedID;
			KeyName = _forcedName?.ToString() ?? string.Empty;
		}
		else
		{
			KeyID = GetKeyId();
			KeyName = GetKeyName().ToString();
		}
	}

	private string GetKeyId()
	{
		if (TaleWorlds.InputSystem.Input.IsGamepadActive)
		{
			if (GameKey != null)
			{
				return GameKey.ControllerKey?.InputKey.ToString();
			}
			if (HotKey != null)
			{
				return HotKey.Keys.Find((Key k) => k.IsControllerInput)?.InputKey.ToString();
			}
		}
		if (GameKey != null)
		{
			return GameKey.KeyboardKey?.InputKey.ToString();
		}
		if (HotKey != null)
		{
			return HotKey.Keys.Find((Key k) => !k.IsControllerInput)?.InputKey.ToString();
		}
		return string.Empty;
	}

	private TextObject GetKeyName()
	{
		if (_forcedName != null)
		{
			return _forcedName;
		}
		if (Game.Current != null)
		{
			if (HotKey != null)
			{
				return Game.Current.GameTextManager.FindText("str_key_name", HotKey.GroupId + "_" + HotKey.Id);
			}
			if (GameKey != null)
			{
				return Game.Current.GameTextManager.FindText("str_key_name", GameKey.GroupId + "_" + GameKey.StringId);
			}
		}
		return TextObject.GetEmpty();
	}

	private void UpdateVisibility()
	{
		IsVisible = _forcedVisibility ?? (!_isVisibleToConsoleOnly || TaleWorlds.InputSystem.Input.IsGamepadActive);
	}

	public static InputKeyItemVM CreateFromGameKey(GameKey gameKey, bool isConsoleOnly)
	{
		InputKeyItemVM inputKeyItemVM = new InputKeyItemVM();
		inputKeyItemVM.GameKey = gameKey;
		inputKeyItemVM._isVisibleToConsoleOnly = isConsoleOnly;
		inputKeyItemVM.ForceRefresh();
		return inputKeyItemVM;
	}

	public static InputKeyItemVM CreateFromHotKey(HotKey hotKey, bool isConsoleOnly)
	{
		InputKeyItemVM inputKeyItemVM = new InputKeyItemVM();
		inputKeyItemVM.HotKey = hotKey;
		inputKeyItemVM._isVisibleToConsoleOnly = isConsoleOnly;
		inputKeyItemVM.ForceRefresh();
		return inputKeyItemVM;
	}

	public static InputKeyItemVM CreateFromHotKeyWithForcedName(HotKey hotKey, TextObject forcedName, bool isConsoleOnly)
	{
		InputKeyItemVM inputKeyItemVM = new InputKeyItemVM();
		inputKeyItemVM.HotKey = hotKey;
		inputKeyItemVM._isVisibleToConsoleOnly = isConsoleOnly;
		inputKeyItemVM._forcedName = forcedName;
		inputKeyItemVM.ForceRefresh();
		return inputKeyItemVM;
	}

	public static InputKeyItemVM CreateFromGameKeyWithForcedName(GameKey gameKey, TextObject forcedName, bool isConsoleOnly)
	{
		InputKeyItemVM inputKeyItemVM = new InputKeyItemVM();
		inputKeyItemVM.GameKey = gameKey;
		inputKeyItemVM._isVisibleToConsoleOnly = isConsoleOnly;
		inputKeyItemVM._forcedName = forcedName;
		inputKeyItemVM.ForceRefresh();
		return inputKeyItemVM;
	}

	public static InputKeyItemVM CreateFromForcedID(string forcedID, TextObject forcedName, bool isConsoleOnly)
	{
		InputKeyItemVM inputKeyItemVM = new InputKeyItemVM();
		inputKeyItemVM._forcedID = forcedID;
		inputKeyItemVM._forcedName = forcedName;
		inputKeyItemVM._isVisibleToConsoleOnly = isConsoleOnly;
		inputKeyItemVM.ForceRefresh();
		return inputKeyItemVM;
	}
}
