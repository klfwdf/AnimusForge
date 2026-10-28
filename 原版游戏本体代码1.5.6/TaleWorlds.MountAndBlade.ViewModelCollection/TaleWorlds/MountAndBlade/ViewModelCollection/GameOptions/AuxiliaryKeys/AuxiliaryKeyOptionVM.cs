using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions.AuxiliaryKeys;

public class AuxiliaryKeyOptionVM : KeyOptionVM
{
	private readonly Action<AuxiliaryKeyOptionVM, InputKey> _onKeySet;

	private readonly Func<AuxiliaryKeyOptionVM, string> _getExtraInformation;

	public HotKey CurrentHotKey { get; private set; }

	public AuxiliaryKeyOptionVM(HotKey hotKey, Action<KeyOptionVM> onKeybindRequest, Action<AuxiliaryKeyOptionVM, InputKey> onKeySet, Func<AuxiliaryKeyOptionVM, string> getExtraInformation)
		: base(hotKey.GroupId, hotKey.Id, onKeybindRequest)
	{
		_onKeySet = onKeySet;
		_getExtraInformation = getExtraInformation;
		CurrentHotKey = hotKey;
		base.Key = (TaleWorlds.InputSystem.Input.IsGamepadActive ? CurrentHotKey.Keys.FirstOrDefault((Key x) => x.IsControllerInput) : CurrentHotKey.Keys.FirstOrDefault((Key x) => !x.IsControllerInput));
		if (base.Key == null)
		{
			base.Key = new Key(InputKey.Invalid);
		}
		base.CurrentKey = new Key(base.Key.InputKey);
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		string name = CurrentHotKey.Id;
		if (Module.CurrentModule.GlobalTextManager.TryGetText("str_hotkey_name", _groupId + "_" + _id, out var text))
		{
			name = text.ToString();
		}
		base.Name = name;
		string variable = "";
		if (Module.CurrentModule.GlobalTextManager.TryGetText("str_hotkey_description", _groupId + "_" + _id, out var text2))
		{
			variable = text2.ToString();
		}
		GameTextManager globalTextManager = Module.CurrentModule.GlobalTextManager;
		base.OptionValueText = globalTextManager.GetHotKeyGameTextFromKeyID(base.CurrentKey.ToString().ToLower()).ToString();
		string text3 = base.OptionValueText;
		foreach (HotKey.Modifiers item in new List<HotKey.Modifiers>
		{
			HotKey.Modifiers.Alt,
			HotKey.Modifiers.Shift,
			HotKey.Modifiers.Control
		})
		{
			if (CurrentHotKey.HasModifier(item))
			{
				MBTextManager.SetTextVariable("KEY", text3);
				MBTextManager.SetTextVariable("MODIFIER", globalTextManager.GetHotKeyGameTextFromKeyID("any" + item.ToString().ToLower()).ToString());
				text3 = globalTextManager.FindText("str_hot_key_with_modifier").ToString();
			}
		}
		TextObject textObject = new TextObject("{=ol0rBSrb}{STR1}{newline}{STR2}");
		textObject.SetTextVariable("STR1", text3);
		textObject.SetTextVariable("STR2", variable);
		textObject.SetTextVariable("newline", "\n \n");
		base.Description = textObject.ToString();
		base.ExtraInformationText = _getExtraInformation?.Invoke(this);
	}

	private void ExecuteKeybindRequest()
	{
		_onKeybindRequest(this);
	}

	public override void Set(InputKey newKey)
	{
		_onKeySet(this, newKey);
		RefreshValues();
	}

	public override void Update()
	{
		base.Key = (TaleWorlds.InputSystem.Input.IsGamepadActive ? CurrentHotKey.Keys.FirstOrDefault((Key x) => x.IsControllerInput) : CurrentHotKey.Keys.FirstOrDefault((Key x) => !x.IsControllerInput));
		if (base.Key == null)
		{
			base.Key = new Key(InputKey.Invalid);
		}
		base.CurrentKey = new Key(base.Key.InputKey);
		UpdateIsChanged();
		RefreshValues();
	}

	public override void OnDone()
	{
		base.Key.ChangeKey(base.CurrentKey.InputKey);
	}

	internal override void UpdateIsChanged()
	{
		base.IsChanged = base.CurrentKey != base.Key;
	}

	public override void ExecuteRevert()
	{
		Set(base.Key.InputKey);
	}
}
