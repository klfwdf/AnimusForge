using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection;

public class CampaignOptionItemVM : ViewModel
{
	private ActionCampaignOptionData _optionDataAsAction;

	private BooleanCampaignOptionData _optionDataAsBoolean;

	private NumericCampaignOptionData _optionDataAsNumeric;

	private SelectionCampaignOptionData _optionDataAsSelection;

	private Action<CampaignOptionItemVM> _onValueChanged;

	private CampaignOptionDataType _dataType;

	private bool _hideOptionName;

	private int _optionType;

	private string _name;

	private HintViewModel _hint;

	private bool _isDiscrete;

	private bool _isDisabled;

	private float _minRange;

	private float _maxRange;

	private bool _valueAsBoolean;

	private float _valueAsRange;

	private string _valueAsString;

	private CampaignOptionSelectorVM _selectionSelector;

	public ICampaignOptionData OptionData { get; private set; }

	[DataSourceProperty]
	public bool HideOptionName
	{
		get
		{
			return _hideOptionName;
		}
		set
		{
			if (value != _hideOptionName)
			{
				_hideOptionName = value;
				OnPropertyChangedWithValue(value, "HideOptionName");
			}
		}
	}

	[DataSourceProperty]
	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			if (value != _name)
			{
				_name = value;
				OnPropertyChangedWithValue(value, "Name");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel Hint
	{
		get
		{
			return _hint;
		}
		set
		{
			if (value != _hint)
			{
				_hint = value;
				OnPropertyChangedWithValue(value, "Hint");
			}
		}
	}

	[DataSourceProperty]
	public int OptionType
	{
		get
		{
			return _optionType;
		}
		set
		{
			if (value != _optionType)
			{
				_optionType = value;
				OnPropertyChangedWithValue(value, "OptionType");
			}
		}
	}

	[DataSourceProperty]
	public bool ValueAsBoolean
	{
		get
		{
			return _valueAsBoolean;
		}
		set
		{
			if (value != _valueAsBoolean)
			{
				_valueAsBoolean = value;
				OnPropertyChangedWithValue(value, "ValueAsBoolean");
				_optionDataAsBoolean.SetValue(value ? 1f : 0f);
				_onValueChanged?.Invoke(this);
			}
		}
	}

	[DataSourceProperty]
	public bool IsDiscrete
	{
		get
		{
			return _isDiscrete;
		}
		set
		{
			if (value != _isDiscrete)
			{
				_isDiscrete = value;
				OnPropertyChangedWithValue(value, "IsDiscrete");
			}
		}
	}

	[DataSourceProperty]
	public bool IsDisabled
	{
		get
		{
			return _isDisabled;
		}
		set
		{
			if (value != _isDisabled)
			{
				_isDisabled = value;
				OnPropertyChangedWithValue(value, "IsDisabled");
				if (SelectionSelector != null)
				{
					SelectionSelector.IsEnabled = !value;
				}
			}
		}
	}

	[DataSourceProperty]
	public float MinRange
	{
		get
		{
			return _minRange;
		}
		set
		{
			if (value != _minRange)
			{
				_minRange = value;
				OnPropertyChangedWithValue(value, "MinRange");
			}
		}
	}

	[DataSourceProperty]
	public float MaxRange
	{
		get
		{
			return _maxRange;
		}
		set
		{
			if (value != _maxRange)
			{
				_maxRange = value;
				OnPropertyChangedWithValue(value, "MaxRange");
			}
		}
	}

	[DataSourceProperty]
	public float ValueAsRange
	{
		get
		{
			return _valueAsRange;
		}
		set
		{
			if (value != _valueAsRange)
			{
				_valueAsRange = value;
				OnPropertyChangedWithValue(value, "ValueAsRange");
				ValueAsString = value.ToString("F1");
				_optionDataAsNumeric.SetValue(value);
				_onValueChanged?.Invoke(this);
			}
		}
	}

	[DataSourceProperty]
	public string ValueAsString
	{
		get
		{
			return _valueAsString;
		}
		set
		{
			if (value != _valueAsString)
			{
				_valueAsString = value;
				OnPropertyChangedWithValue(value, "ValueAsString");
			}
		}
	}

	[DataSourceProperty]
	public CampaignOptionSelectorVM SelectionSelector
	{
		get
		{
			return _selectionSelector;
		}
		set
		{
			if (value != _selectionSelector)
			{
				_selectionSelector = value;
				OnPropertyChangedWithValue(value, "SelectionSelector");
			}
		}
	}

	public CampaignOptionItemVM(ICampaignOptionData optionData)
	{
		OptionData = optionData;
		OptionData.GetEnableState();
		Hint = new HintViewModel();
		_dataType = OptionData.GetDataType();
		if (_dataType == CampaignOptionDataType.Boolean)
		{
			_optionDataAsBoolean = OptionData as BooleanCampaignOptionData;
			ValueAsBoolean = _optionDataAsBoolean.GetValue() != 0f;
			OptionType = 0;
		}
		else if (_dataType == CampaignOptionDataType.Numeric)
		{
			_optionDataAsNumeric = OptionData as NumericCampaignOptionData;
			OptionType = 1;
			MinRange = _optionDataAsNumeric.MinValue;
			MaxRange = _optionDataAsNumeric.MaxValue;
			IsDiscrete = _optionDataAsNumeric.IsDiscrete;
			ValueAsRange = _optionDataAsNumeric.GetValue();
		}
		else if (_dataType == CampaignOptionDataType.Selection)
		{
			_optionDataAsSelection = OptionData as SelectionCampaignOptionData;
			List<TextObject> selections = _optionDataAsSelection.Selections;
			int selectedIndex = (int)_optionDataAsSelection.GetValue();
			SelectionSelector = new CampaignOptionSelectorVM(selections, selectedIndex, null);
			SelectionSelector.SetOnChangeAction(OnSelectionOptionValueChanged);
			OptionType = 2;
			SelectionSelector.SelectedIndex = (int)_optionDataAsSelection.GetValue();
		}
		else if (_dataType == CampaignOptionDataType.Action)
		{
			_optionDataAsAction = OptionData as ActionCampaignOptionData;
			HideOptionName = true;
			OptionType = 3;
		}
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Name = OptionData.GetName();
		RefreshDisabledStatus();
	}

	public void RefreshDisabledStatus()
	{
		string description = OptionData.GetDescription();
		TextObject textObject = new TextObject("{=!}" + description);
		CampaignOptionDisableStatus isDisabledWithReason = OptionData.GetIsDisabledWithReason();
		IsDisabled = isDisabledWithReason.IsDisabled;
		if (!string.IsNullOrEmpty(isDisabledWithReason.DisabledReason))
		{
			string variable = textObject.ToString();
			string disabledReason = isDisabledWithReason.DisabledReason;
			textObject = GameTexts.FindText("str_string_newline_newline_string").CopyTextObject();
			textObject.SetTextVariable("STR1", variable);
			textObject.SetTextVariable("STR2", disabledReason);
		}
		if (IsDisabled && isDisabledWithReason.ValueIfDisabled != -1f)
		{
			SetValue(isDisabledWithReason.ValueIfDisabled);
		}
		Hint.HintText = textObject;
	}

	public void ExecuteAction()
	{
		_optionDataAsAction?.ExecuteAction();
	}

	public void OnSelectionOptionValueChanged(SelectorVM<SelectorItemVM> selector)
	{
		if (selector.SelectedIndex >= 0 && _optionDataAsSelection != null)
		{
			_optionDataAsSelection.SetValue(selector.SelectedIndex);
			_onValueChanged?.Invoke(this);
		}
	}

	public void SetValue(float value)
	{
		if (_dataType == CampaignOptionDataType.Boolean)
		{
			ValueAsBoolean = value != 0f;
		}
		else if (_dataType == CampaignOptionDataType.Numeric)
		{
			ValueAsRange = value;
		}
		else if (_dataType == CampaignOptionDataType.Selection)
		{
			SelectionSelector.SelectedIndex = (int)value;
		}
		OptionData.SetValue(value);
	}

	public void SetOnValueChangedCallback(Action<CampaignOptionItemVM> onValueChanged)
	{
		_onValueChanged = onValueChanged;
	}
}
