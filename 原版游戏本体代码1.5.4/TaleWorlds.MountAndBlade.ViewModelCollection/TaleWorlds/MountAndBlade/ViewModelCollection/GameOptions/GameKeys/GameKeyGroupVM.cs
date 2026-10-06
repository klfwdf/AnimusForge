using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions.GameKeys;

public class GameKeyGroupVM : ViewModel
{
	private readonly Action<KeyOptionVM> _onKeybindRequest;

	private readonly Action<int, InputKey> _setAllKeysOfId;

	private readonly Func<KeyOptionVM, string> _getExtraInformation;

	private readonly string _categoryId;

	private IEnumerable<GameKey> _keys;

	private string _description;

	private MBBindingList<GameKeyOptionVM> _gameKeys;

	[DataSourceProperty]
	public MBBindingList<GameKeyOptionVM> GameKeys
	{
		get
		{
			return _gameKeys;
		}
		set
		{
			if (value != _gameKeys)
			{
				_gameKeys = value;
				OnPropertyChangedWithValue(value, "GameKeys");
			}
		}
	}

	[DataSourceProperty]
	public string Description
	{
		get
		{
			return _description;
		}
		set
		{
			if (value != _description)
			{
				_description = value;
				OnPropertyChangedWithValue(value, "Description");
			}
		}
	}

	public GameKeyGroupVM(string categoryId, IEnumerable<GameKey> keys, Action<KeyOptionVM> onKeybindRequest, Action<int, InputKey> setAllKeysOfId, Func<KeyOptionVM, string> getExtraInformation)
	{
		_onKeybindRequest = onKeybindRequest;
		_setAllKeysOfId = setAllKeysOfId;
		_getExtraInformation = getExtraInformation;
		_categoryId = categoryId;
		_gameKeys = new MBBindingList<GameKeyOptionVM>();
		_keys = keys;
		PopulateGameKeys();
		RefreshValues();
	}

	private void PopulateGameKeys()
	{
		GameKeys.Clear();
		foreach (GameKey key in _keys)
		{
			bool num;
			if (!TaleWorlds.InputSystem.Input.IsGamepadActive)
			{
				if (!(key?.DefaultKeyboardKey != null))
				{
					continue;
				}
				if (key != null)
				{
					num = key.DefaultKeyboardKey.InputKey != InputKey.Invalid;
					goto IL_0088;
				}
			}
			else
			{
				if (!(key?.DefaultControllerKey != null))
				{
					continue;
				}
				if (key != null)
				{
					num = key.DefaultControllerKey.InputKey != InputKey.Invalid;
					goto IL_0088;
				}
			}
			goto IL_008a;
			IL_008a:
			GameKeys.Add(new GameKeyOptionVM(key, _onKeybindRequest, SetGameKey, _getExtraInformation));
			continue;
			IL_0088:
			if (!num)
			{
				continue;
			}
			goto IL_008a;
		}
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Description = Module.CurrentModule.GlobalTextManager.FindText("str_key_category_name", _categoryId).ToString();
		GameKeys.ApplyActionOnAllItems(delegate(GameKeyOptionVM x)
		{
			x.RefreshValues();
		});
	}

	private void SetGameKey(GameKeyOptionVM option, InputKey newKey)
	{
		InputKey inputKey = option.CurrentKey.InputKey;
		if (newKey != inputKey)
		{
			option.CurrentKey.ChangeKey(newKey);
			option.OptionValueText = Module.CurrentModule.GlobalTextManager.GetHotKeyGameTextFromKeyID(option.CurrentKey.ToString().ToLower()).ToString();
			option.UpdateIsChanged();
			_setAllKeysOfId(option.CurrentGameKey.Id, newKey);
			GameKeyOptionVM gameKeyOptionVM = GameKeys.FirstOrDefault((GameKeyOptionVM k) => k != option && k.CurrentKey.InputKey == option.CurrentKey.InputKey);
			gameKeyOptionVM?.Set(inputKey);
			if (gameKeyOptionVM != null)
			{
				MBInformationManager.AddQuickInformation(new TextObject("{=gb2S2aRq}Swapped {FIRST_KEY} and {SECOND_KEY}").SetTextVariable("FIRST_KEY", option.Name).SetTextVariable("SECOND_KEY", gameKeyOptionVM.Name), -1000);
			}
		}
	}

	internal void Update()
	{
		foreach (GameKeyOptionVM gameKey in GameKeys)
		{
			gameKey.Update();
		}
	}

	public void OnDone()
	{
		foreach (GameKeyOptionVM gameKey in GameKeys)
		{
			gameKey.OnDone();
		}
	}

	internal bool IsChanged()
	{
		for (int i = 0; i < GameKeys.Count; i++)
		{
			if (GameKeys[i].IsChanged)
			{
				return true;
			}
		}
		return false;
	}

	public void OnGamepadActiveStateChanged()
	{
		PopulateGameKeys();
		Update();
		OnDone();
	}

	public void Cancel()
	{
		GameKeys.ApplyActionOnAllItems(delegate(GameKeyOptionVM g)
		{
			g.ExecuteRevert();
		});
	}

	public void ApplyValues()
	{
		GameKeys.ApplyActionOnAllItems(delegate(GameKeyOptionVM g)
		{
			g.Apply();
		});
	}
}
