using System;
using TaleWorlds.InputSystem;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions.GameKeys;

public class GameKeyOptionVM : KeyOptionVM
{
	private InputKey _initalKey;

	private readonly Action<GameKeyOptionVM, InputKey> _onKeySet;

	private readonly Func<GameKeyOptionVM, string> _getExtraInformation;

	public GameKey CurrentGameKey { get; private set; }

	public GameKeyOptionVM(GameKey gameKey, Action<KeyOptionVM> onKeybindRequest, Action<GameKeyOptionVM, InputKey> onKeySet, Func<GameKeyOptionVM, string> getExtraInformation)
		: base(gameKey.GroupId, ((GameKeyDefinition)gameKey.Id/*cast due to .constrained prefix*/).ToString(), onKeybindRequest)
	{
		_onKeySet = onKeySet;
		_getExtraInformation = getExtraInformation;
		CurrentGameKey = gameKey;
		base.Key = (TaleWorlds.InputSystem.Input.IsGamepadActive ? CurrentGameKey.ControllerKey : CurrentGameKey.KeyboardKey);
		if (base.Key == null)
		{
			base.Key = new Key(InputKey.Invalid);
		}
		_initalKey = base.Key.InputKey;
		base.CurrentKey = new Key(base.Key.InputKey);
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		base.Name = Module.CurrentModule.GlobalTextManager.FindText("str_key_name", _groupId + "_" + _id).ToString();
		base.Description = Module.CurrentModule.GlobalTextManager.FindText("str_key_description", _groupId + "_" + _id).ToString();
		base.OptionValueText = Module.CurrentModule.GlobalTextManager.GetHotKeyGameTextFromKeyID(base.CurrentKey.ToString().ToLower()).ToString();
		base.ExtraInformationText = _getExtraInformation?.Invoke(this);
	}

	private void ExecuteKeybindRequest()
	{
		_onKeybindRequest(this);
	}

	public override void Set(InputKey newKey)
	{
		_onKeySet(this, newKey);
	}

	public override void Update()
	{
		base.Key = (TaleWorlds.InputSystem.Input.IsGamepadActive ? CurrentGameKey.ControllerKey : CurrentGameKey.KeyboardKey);
		if (base.Key == null)
		{
			base.Key = new Key(InputKey.Invalid);
		}
		base.CurrentKey = new Key(base.Key.InputKey);
		base.OptionValueText = Module.CurrentModule.GlobalTextManager.GetHotKeyGameTextFromKeyID(base.CurrentKey.ToString().ToLower()).ToString();
		UpdateIsChanged();
	}

	public override void OnDone()
	{
		base.Key?.ChangeKey(base.CurrentKey.InputKey);
		_initalKey = base.CurrentKey.InputKey;
	}

	internal override void UpdateIsChanged()
	{
		base.IsChanged = base.CurrentKey?.InputKey != _initalKey;
	}

	public override void ExecuteRevert()
	{
		Set(_initalKey);
		Update();
	}

	public void Apply()
	{
		OnDone();
		base.CurrentKey = base.Key;
	}
}
