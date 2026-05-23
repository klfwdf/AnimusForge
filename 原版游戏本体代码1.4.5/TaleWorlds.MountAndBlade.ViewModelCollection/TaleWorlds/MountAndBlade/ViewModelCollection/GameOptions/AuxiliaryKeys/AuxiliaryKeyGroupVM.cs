using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions.AuxiliaryKeys;

public class AuxiliaryKeyGroupVM : ViewModel
{
	private readonly Action<KeyOptionVM> _onKeybindRequest;

	private readonly Func<KeyOptionVM, string> _getExtraInformation;

	private readonly string _categoryId;

	private IEnumerable<HotKey> _keys;

	private string _description;

	private MBBindingList<AuxiliaryKeyOptionVM> _hotKeys;

	[DataSourceProperty]
	public MBBindingList<AuxiliaryKeyOptionVM> HotKeys
	{
		get
		{
			return _hotKeys;
		}
		set
		{
			if (value != _hotKeys)
			{
				_hotKeys = value;
				OnPropertyChangedWithValue(value, "HotKeys");
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

	public AuxiliaryKeyGroupVM(string categoryId, IEnumerable<HotKey> keys, Action<KeyOptionVM> onKeybindRequest, Func<KeyOptionVM, string> getExtraInformation)
	{
		_onKeybindRequest = onKeybindRequest;
		_getExtraInformation = getExtraInformation;
		_categoryId = categoryId;
		_hotKeys = new MBBindingList<AuxiliaryKeyOptionVM>();
		_keys = keys;
		PopulateHotKeys();
		RefreshValues();
	}

	private void PopulateHotKeys()
	{
		HotKeys.Clear();
		foreach (HotKey key in _keys)
		{
			bool num;
			if (!TaleWorlds.InputSystem.Input.IsGamepadActive)
			{
				if (key == null)
				{
					continue;
				}
				num = key.DefaultKeys.Any((Key x) => x != null && x.IsKeyboardInput && x.InputKey != InputKey.Invalid);
			}
			else
			{
				if (key == null)
				{
					continue;
				}
				num = key.DefaultKeys.Any((Key x) => x != null && x.IsControllerInput && x.InputKey != InputKey.Invalid);
			}
			if (num)
			{
				HotKeys.Add(new AuxiliaryKeyOptionVM(key, _onKeybindRequest, SetHotKey, _getExtraInformation));
			}
		}
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		string description = _categoryId;
		if (Module.CurrentModule.GlobalTextManager.TryGetText("str_hotkey_category_name", _categoryId, out var text))
		{
			description = text.ToString();
		}
		Description = description;
		HotKeys.ApplyActionOnAllItems(delegate(AuxiliaryKeyOptionVM x)
		{
			x.RefreshValues();
		});
	}

	private void SetHotKey(AuxiliaryKeyOptionVM option, InputKey newKey)
	{
		InputKey inputKey = option.CurrentKey.InputKey;
		if (newKey != inputKey)
		{
			option.CurrentKey.ChangeKey(newKey);
			option.OptionValueText = Module.CurrentModule.GlobalTextManager.GetHotKeyGameTextFromKeyID(option.CurrentKey.ToString().ToLower()).ToString();
			option.UpdateIsChanged();
			AuxiliaryKeyOptionVM auxiliaryKeyOptionVM = HotKeys.FirstOrDefault((AuxiliaryKeyOptionVM k) => k != option && k.CurrentKey.InputKey == option.CurrentKey.InputKey && k.CurrentHotKey.HasSameModifiers(option.CurrentHotKey));
			auxiliaryKeyOptionVM?.Set(inputKey);
			if (auxiliaryKeyOptionVM != null)
			{
				MBInformationManager.AddQuickInformation(new TextObject("{=gb2S2aRq}Swapped {FIRST_KEY} and {SECOND_KEY}").SetTextVariable("FIRST_KEY", option.Name).SetTextVariable("SECOND_KEY", auxiliaryKeyOptionVM.Name), -1000);
			}
		}
	}

	internal void Update()
	{
		foreach (AuxiliaryKeyOptionVM hotKey in HotKeys)
		{
			hotKey.Update();
		}
	}

	public void OnDone()
	{
		foreach (AuxiliaryKeyOptionVM hotKey in HotKeys)
		{
			hotKey.OnDone();
		}
	}

	internal bool IsChanged()
	{
		for (int i = 0; i < HotKeys.Count; i++)
		{
			if (HotKeys[i].IsChanged)
			{
				return true;
			}
		}
		return false;
	}

	public void OnGamepadActiveStateChanged()
	{
		Update();
		OnDone();
	}
}
