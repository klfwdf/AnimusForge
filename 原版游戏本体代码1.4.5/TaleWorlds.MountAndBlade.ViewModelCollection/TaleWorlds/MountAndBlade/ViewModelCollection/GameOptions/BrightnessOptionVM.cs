using System;
using TaleWorlds.Engine.Options;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.ViewModelCollection.Input;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions;

public class BrightnessOptionVM : ViewModel
{
	private readonly Action<bool> _onClose;

	private string _titleText;

	private string _explanationText;

	private string _cancelText;

	private string _acceptText;

	private int _initialValue;

	private float _initialValue1;

	private float _initialValue2;

	private int _value;

	private int _value1;

	private int _value2;

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

	public int Value
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
			}
		}
	}

	public int InitialValue
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

	public float InitialValue1
	{
		get
		{
			return _initialValue1;
		}
		set
		{
			if (_initialValue1 != value)
			{
				_initialValue1 = value;
				OnPropertyChangedWithValue(value, "InitialValue1");
			}
		}
	}

	public float InitialValue2
	{
		get
		{
			return _initialValue2;
		}
		set
		{
			if (_initialValue2 != value)
			{
				_initialValue2 = value;
				OnPropertyChangedWithValue(value, "InitialValue2");
			}
		}
	}

	public int Value1
	{
		get
		{
			return _value1;
		}
		set
		{
			if (_value1 != value)
			{
				float value2 = (float)(value + 2) * 0.003f + 1f;
				NativeOptions.SetConfig(NativeOptions.NativeOptionsType.BrightnessMax, value2);
				_value1 = value;
				OnPropertyChangedWithValue(value, "Value1");
			}
		}
	}

	public int Value2
	{
		get
		{
			return _value2;
		}
		set
		{
			if (_value2 != value)
			{
				float value2 = (float)(value - 2) * 0.003f;
				NativeOptions.SetConfig(NativeOptions.NativeOptionsType.BrightnessMin, value2);
				_value2 = value;
				OnPropertyChangedWithValue(value, "Value2");
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
					RefreshOptionValues();
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

	public BrightnessOptionVM(Action<bool> onClose = null)
	{
		_onClose = onClose;
		RefreshOptionValues();
		RefreshValues();
	}

	private void RefreshOptionValues()
	{
		InitialValue = 50;
		InitialValue1 = NativeOptions.GetConfig(NativeOptions.NativeOptionsType.BrightnessMax);
		InitialValue2 = NativeOptions.GetConfig(NativeOptions.NativeOptionsType.BrightnessMin);
		if (NativeOptions.GetConfig(NativeOptions.NativeOptionsType.BrightnessCalibrated) < 2f)
		{
			Value1 = 0;
			Value2 = 0;
		}
		else
		{
			Value1 = TaleWorlds.Library.MathF.Round((InitialValue1 - 1f) / 0.003f) - 2;
			Value2 = TaleWorlds.Library.MathF.Round(InitialValue2 / 0.003f) + 2;
		}
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		TitleText = Module.CurrentModule.GlobalTextManager.FindText("str_brightness_option_title").ToString();
		TextObject textObject = Module.CurrentModule.GlobalTextManager.FindText("str_brightness_option_explainer");
		textObject.SetTextVariable("newline", "\n");
		ExplanationText = textObject.ToString();
		CancelText = new TextObject("{=3CpNUnVl}Cancel").ToString();
		AcceptText = new TextObject("{=Y94H6XnK}Accept").ToString();
	}

	public void ExecuteConfirm()
	{
		InitialValue = Value;
		NativeOptions.SetConfig(NativeOptions.NativeOptionsType.Brightness, Value);
		float num = (float)(Value1 + 2) * 0.003f + 1f;
		float num2 = (float)(Value2 - 2) * 0.003f;
		InitialValue1 = num;
		InitialValue2 = num2;
		NativeOptions.SetConfig(NativeOptions.NativeOptionsType.BrightnessMax, num);
		NativeOptions.SetConfig(NativeOptions.NativeOptionsType.BrightnessMin, num2);
		NativeOptions.SetConfig(NativeOptions.NativeOptionsType.BrightnessCalibrated, 4f);
		_onClose?.Invoke(obj: true);
		Visible = false;
	}

	public void ExecuteCancel()
	{
		Value = InitialValue;
		NativeOptions.SetConfig(NativeOptions.NativeOptionsType.Brightness, InitialValue);
		NativeOptions.SetConfig(NativeOptions.NativeOptionsType.BrightnessMax, InitialValue1);
		NativeOptions.SetConfig(NativeOptions.NativeOptionsType.BrightnessMin, InitialValue2);
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
