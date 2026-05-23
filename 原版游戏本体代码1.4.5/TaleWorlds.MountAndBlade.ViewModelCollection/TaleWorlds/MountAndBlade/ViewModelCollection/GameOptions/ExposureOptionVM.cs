using System;
using TaleWorlds.Engine.Options;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.ViewModelCollection.Input;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions;

public class ExposureOptionVM : ViewModel
{
	private readonly Action<bool> _onClose;

	private string _titleText;

	private string _explanationText;

	private string _cancelText;

	private string _acceptText;

	private float _initialValue;

	private float _value;

	private bool _visible;

	private InputKeyItemVM _cancelInputKey;

	private InputKeyItemVM _confirmInputKey;

	[DataSourceProperty]
	public string TitleText
	{
		get
		{
			return _titleText;
		}
		set
		{
			if (value != _titleText)
			{
				_titleText = value;
				OnPropertyChangedWithValue(value, "TitleText");
			}
		}
	}

	[DataSourceProperty]
	public string ExplanationText
	{
		get
		{
			return _explanationText;
		}
		set
		{
			if (value != _explanationText)
			{
				_explanationText = value;
				OnPropertyChangedWithValue(value, "ExplanationText");
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
			if (value != _cancelText)
			{
				_cancelText = value;
				OnPropertyChangedWithValue(value, "CancelText");
			}
		}
	}

	[DataSourceProperty]
	public string AcceptText
	{
		get
		{
			return _acceptText;
		}
		set
		{
			if (value != _acceptText)
			{
				_acceptText = value;
				OnPropertyChangedWithValue(value, "AcceptText");
			}
		}
	}

	public float Value
	{
		get
		{
			return _value;
		}
		set
		{
			if (_value != value)
			{
				_value = value;
				OnPropertyChangedWithValue(value, "Value");
				NativeOptions.SetConfig(NativeOptions.NativeOptionsType.ExposureCompensation, Value);
			}
		}
	}

	public float InitialValue
	{
		get
		{
			return _initialValue;
		}
		set
		{
			if (_initialValue != value)
			{
				_initialValue = value;
				OnPropertyChangedWithValue(value, "InitialValue");
			}
		}
	}

	public bool Visible
	{
		get
		{
			return _visible;
		}
		set
		{
			if (_visible != value)
			{
				_visible = value;
				OnPropertyChangedWithValue(value, "Visible");
				if (value)
				{
					Value = NativeOptions.GetConfig(NativeOptions.NativeOptionsType.ExposureCompensation);
				}
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM CancelInputKey
	{
		get
		{
			return _cancelInputKey;
		}
		set
		{
			if (value != _cancelInputKey)
			{
				_cancelInputKey = value;
				OnPropertyChangedWithValue(value, "CancelInputKey");
			}
		}
	}

	[DataSourceProperty]
	public InputKeyItemVM ConfirmInputKey
	{
		get
		{
			return _confirmInputKey;
		}
		set
		{
			if (value != _confirmInputKey)
			{
				_confirmInputKey = value;
				OnPropertyChangedWithValue(value, "ConfirmInputKey");
			}
		}
	}

	public ExposureOptionVM(Action<bool> onClose = null)
	{
		_onClose = onClose;
		InitialValue = 0f;
		Value = InitialValue;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		TitleText = Module.CurrentModule.GlobalTextManager.FindText("str_exposure_option_title").ToString();
		TextObject textObject = Module.CurrentModule.GlobalTextManager.FindText("str_exposure_option_explainer");
		textObject.SetTextVariable("newline", "\n");
		ExplanationText = textObject.ToString();
		CancelText = new TextObject("{=3CpNUnVl}Cancel").ToString();
		AcceptText = new TextObject("{=Y94H6XnK}Accept").ToString();
	}

	public void ExecuteConfirm()
	{
		InitialValue = Value;
		NativeOptions.SetConfig(NativeOptions.NativeOptionsType.ExposureCompensation, Value);
		_onClose?.Invoke(obj: true);
		Visible = false;
	}

	public void ExecuteCancel()
	{
		Value = InitialValue;
		NativeOptions.SetConfig(NativeOptions.NativeOptionsType.ExposureCompensation, InitialValue);
		Visible = false;
		_onClose?.Invoke(obj: false);
	}

	public void SetCancelInputKey(HotKey hotkey)
	{
		CancelInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, isConsoleOnly: true);
	}

	public void SetConfirmInputKey(HotKey hotkey)
	{
		ConfirmInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, isConsoleOnly: true);
	}
}
