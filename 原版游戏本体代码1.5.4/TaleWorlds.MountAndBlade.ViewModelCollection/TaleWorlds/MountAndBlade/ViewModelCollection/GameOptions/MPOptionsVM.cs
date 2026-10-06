using System;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions;

public class MPOptionsVM : OptionsVM
{
	private TextObject _changesAppliedTextObject = new TextObject("{=SfsnlbyK}Changes applied.");

	private TextObject _noChangesMadeTextObject = new TextObject("{=jS5rrX8M}There are no changes to apply.");

	private bool _areHotkeysEnabled;

	private bool _isEnabled;

	private string _applyText;

	private string _revertText;

	[DataSourceProperty]
	public bool AreHotkeysEnabled
	{
		get
		{
			return _areHotkeysEnabled;
		}
		set
		{
			if (value != _areHotkeysEnabled)
			{
				_areHotkeysEnabled = value;
				OnPropertyChangedWithValue(value, "AreHotkeysEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsEnabled
	{
		get
		{
			return _isEnabled;
		}
		set
		{
			if (value != _isEnabled)
			{
				_isEnabled = value;
				OnPropertyChangedWithValue(value, "IsEnabled");
			}
		}
	}

	[DataSourceProperty]
	public string ApplyText
	{
		get
		{
			return _applyText;
		}
		set
		{
			if (value != _applyText)
			{
				_applyText = value;
				OnPropertyChangedWithValue(value, "ApplyText");
			}
		}
	}

	[DataSourceProperty]
	public string RevertText
	{
		get
		{
			return _revertText;
		}
		set
		{
			if (value != _revertText)
			{
				_revertText = value;
				OnPropertyChangedWithValue(value, "RevertText");
			}
		}
	}

	public MPOptionsVM(bool autoHandleClose, Action onChangeBrightnessRequest, Action onChangeExposureRequest, Action<KeyOptionVM> onKeybindRequest)
		: base(autoHandleClose, OptionsMode.Multiplayer, onKeybindRequest, onChangeBrightnessRequest, onChangeExposureRequest)
	{
		RefreshValues();
	}

	public MPOptionsVM(Action onClose, Action<KeyOptionVM> onKeybindRequest)
		: base(OptionsMode.Multiplayer, onClose, onKeybindRequest)
	{
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		ApplyText = new TextObject("{=BAaS5Dkc}Apply").ToString();
		RevertText = new TextObject("{=Npqlj5Ln}Revert Changes").ToString();
	}

	public new void ExecuteCancel()
	{
		base.ExecuteCancel();
	}

	public void ExecuteApply()
	{
		bool num = IsOptionsChanged();
		OnDone();
		InformationManager.DisplayMessage(new InformationMessage(num ? _changesAppliedTextObject.ToString() : _noChangesMadeTextObject.ToString()));
	}

	public void ForceCancel()
	{
		HandleCancel(autoHandleClose: false);
	}
}
