using System.Linq;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions.GamepadOptions;

public class GamepadOptionKeyItemVM : ViewModel
{
	private TextObject _nameObject;

	private string _action;

	private string _keyIdAsString;

	private int _keyId;

	public GameKey GamepadKey { get; }

	public HotKey GamepadHotKey { get; }

	public InputKey? Key { get; }

	[DataSourceProperty]
	public string Action
	{
		get
		{
			return _action;
		}
		set
		{
			if (value != _action)
			{
				_action = value;
				OnPropertyChangedWithValue(value, "Action");
			}
		}
	}

	[DataSourceProperty]
	public int KeyId
	{
		get
		{
			return _keyId;
		}
		set
		{
			if (value != _keyId)
			{
				_keyId = value;
				OnPropertyChangedWithValue(value, "KeyId");
			}
		}
	}

	[DataSourceProperty]
	public string KeyIdAsString
	{
		get
		{
			return _keyIdAsString;
		}
		set
		{
			if (value != _keyIdAsString)
			{
				_keyIdAsString = value;
				OnPropertyChangedWithValue(value, "KeyIdAsString");
			}
		}
	}

	public GamepadOptionKeyItemVM(GameKey gamepadGameKey)
	{
		GamepadKey = gamepadGameKey;
		Action = Module.CurrentModule.GlobalTextManager.FindText("str_key_name", GamepadKey.GroupId + "_" + GamepadKey.StringId).ToString();
		Key = gamepadGameKey.ControllerKey.InputKey;
		KeyId = (int)Key.Value;
		KeyIdAsString = gamepadGameKey?.ControllerKey?.InputKey.ToString() ?? string.Empty;
	}

	public GamepadOptionKeyItemVM(HotKey gamepadHotKey)
	{
		GamepadHotKey = gamepadHotKey;
		Action = Module.CurrentModule.GlobalTextManager.FindText("str_key_name", GamepadHotKey.GroupId + "_" + GamepadHotKey.Id).ToString();
		Key = gamepadHotKey.Keys.FirstOrDefault((Key k) => k.IsControllerInput)?.InputKey;
		KeyId = (int)Key.Value;
		KeyIdAsString = Key?.ToString() ?? string.Empty;
	}

	public GamepadOptionKeyItemVM(InputKey key, TextObject name)
	{
		Key = key;
		KeyId = (int)Key.Value;
		KeyIdAsString = Key?.ToString() ?? string.Empty;
		_nameObject = name;
		Action = _nameObject.ToString();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		if (GamepadKey != null)
		{
			Action = Module.CurrentModule.GlobalTextManager.FindText("str_key_name", GamepadKey.GroupId + "_" + GamepadKey.StringId).ToString();
		}
		else if (GamepadHotKey != null)
		{
			Action = Module.CurrentModule.GlobalTextManager.FindText("str_key_name", GamepadHotKey.GroupId + "_" + GamepadHotKey.Id).ToString();
		}
		else if (_nameObject != null)
		{
			Action = _nameObject.ToString();
		}
	}
}
