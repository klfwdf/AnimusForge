using System;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.GauntletUI;

public class KeybindingPopupVM : ViewModel
{
	private readonly Action _onCancel;

	private string _pressKeyText;

	private string _cancelText;

	[DataSourceProperty]
	public string PressKeyText
	{
		get
		{
			return _pressKeyText;
		}
		set
		{
			if (_pressKeyText != value)
			{
				_pressKeyText = value;
				OnPropertyChangedWithValue(value, "PressKeyText");
			}
		}
	}

	[DataSourceProperty]
	public string CancelText
	{
		get
		{
			return _cancelText;
		}
		set
		{
			if (_cancelText != value)
			{
				_cancelText = value;
				OnPropertyChangedWithValue(value, "CancelText");
			}
		}
	}

	public KeybindingPopupVM(Action onCancel)
	{
		_onCancel = onCancel;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		PressKeyText = new TextObject("{=hvaDkG4w}Press any key.").ToString();
		TextObject textObject = new TextObject("{=5U8vXv4E}Press {KEY} to cancel");
		textObject.SetTextVariable("KEY", HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit").ToString());
		CancelText = textObject.ToString();
	}

	public void ExecuteCancel()
	{
		_onCancel?.Invoke();
	}
}
