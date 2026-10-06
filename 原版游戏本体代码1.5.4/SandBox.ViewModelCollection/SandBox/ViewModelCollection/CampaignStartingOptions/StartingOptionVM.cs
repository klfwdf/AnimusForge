using System;
using System.Collections.Generic;
using SandBox.AdvancedStartOptions;
using SandBox.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.ViewModelCollection.CampaignStartingOptions;

public class StartingOptionVM : ViewModel
{
	public enum CampaignStartingOptionTypes
	{
		Bool,
		Slider,
		Selection,
		Input
	}

	private readonly AdvancedStartOption _data;

	private readonly ListAdvancedStartOption _listData;

	private readonly SandBox.AdvancedStartOptions.AdvancedStartOptions _stagedOptions;

	private readonly IReadOnlyList<(string Identifier, ListAdvancedStartOption.ListItemCondition Condition)> _selectionItems;

	private readonly Func<bool> _getAdditionalIsDisabled;

	private TextObject _nameTextObj;

	private InputKeyItemVM _randomizeInputKey;

	private string _name;

	private bool _isDisabled;

	private bool _isHidden;

	private bool _isFocused;

	private int _optionType;

	private bool _valueAsBoolean;

	private float _valueAsFloat;

	private string _valueAsString;

	private int _valueAsInt;

	private int _minInt;

	private int _maxInt;

	private bool _allowRandomization;

	private float _minRange;

	private float _maxRange;

	private bool _isDiscrete;

	private SelectorVM<SelectorItemVM> _selector;

	private HintViewModel _descriptionHint;

	private HintViewModel _randomizeHint;

	public InputKeyItemVM RandomizeInputKey
	{
		get
		{
			return _randomizeInputKey;
		}
		set
		{
			if (value != _randomizeInputKey)
			{
				_randomizeInputKey = value;
				OnPropertyChangedWithValue(value, "RandomizeInputKey");
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
			}
		}
	}

	[DataSourceProperty]
	public bool IsHidden
	{
		get
		{
			return _isHidden;
		}
		set
		{
			if (value != _isHidden)
			{
				_isHidden = value;
				OnPropertyChangedWithValue(value, "IsHidden");
			}
		}
	}

	[DataSourceProperty]
	public bool IsFocused
	{
		get
		{
			return _isFocused;
		}
		set
		{
			if (value != _isFocused)
			{
				_isFocused = value;
				OnPropertyChangedWithValue(value, "IsFocused");
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
				if (_data is BooleanAdvancedStartOption booleanAdvancedStartOption)
				{
					booleanAdvancedStartOption.SetValue(value);
				}
				StartingOptionVM.OnOptionChanged?.Invoke();
			}
		}
	}

	[DataSourceProperty]
	public float ValueAsFloat
	{
		get
		{
			return _valueAsFloat;
		}
		set
		{
			if (value != _valueAsFloat)
			{
				_valueAsFloat = value;
				OnPropertyChangedWithValue(value, "ValueAsFloat");
				OnNumericValueUpdated();
				if (_data is FloatAdvancedStartOption floatAdvancedStartOption)
				{
					floatAdvancedStartOption.SetValue(value);
				}
				else if (_data is IntAdvancedStartOption intAdvancedStartOption)
				{
					intAdvancedStartOption.SetValue(TaleWorlds.Library.MathF.Round(value));
				}
				StartingOptionVM.OnOptionChanged?.Invoke();
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
	public int ValueAsInt
	{
		get
		{
			return _valueAsInt;
		}
		set
		{
			if (value != _valueAsInt)
			{
				_valueAsInt = value;
				OnPropertyChangedWithValue(value, "ValueAsInt");
				if (_data is UIntAdvancedStartOption uIntAdvancedStartOption)
				{
					uIntAdvancedStartOption.SetValue((uint)value);
				}
				StartingOptionVM.OnOptionChanged?.Invoke();
			}
		}
	}

	[DataSourceProperty]
	public int MinInt
	{
		get
		{
			return _minInt;
		}
		set
		{
			if (value != _minInt)
			{
				_minInt = value;
				OnPropertyChangedWithValue(value, "MinInt");
			}
		}
	}

	[DataSourceProperty]
	public int MaxInt
	{
		get
		{
			return _maxInt;
		}
		set
		{
			if (value != _maxInt)
			{
				_maxInt = value;
				OnPropertyChangedWithValue(value, "MaxInt");
			}
		}
	}

	[DataSourceProperty]
	public bool AllowRandomization
	{
		get
		{
			return _allowRandomization;
		}
		set
		{
			if (value != _allowRandomization)
			{
				_allowRandomization = value;
				OnPropertyChangedWithValue(value, "AllowRandomization");
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
	public SelectorVM<SelectorItemVM> Selector
	{
		get
		{
			return _selector;
		}
		set
		{
			if (value != _selector)
			{
				_selector = value;
				OnPropertyChangedWithValue(value, "Selector");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel DescriptionHint
	{
		get
		{
			return _descriptionHint;
		}
		set
		{
			if (value != _descriptionHint)
			{
				_descriptionHint = value;
				OnPropertyChangedWithValue(value, "DescriptionHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel RandomizeHint
	{
		get
		{
			return _randomizeHint;
		}
		set
		{
			if (value != _randomizeHint)
			{
				_randomizeHint = value;
				OnPropertyChangedWithValue(value, "RandomizeHint");
			}
		}
	}

	public static event Action<StartingOptionVM> OnOptionFocusBegin;

	public static event Action<StartingOptionVM> OnOptionFocusEnd;

	public static event Action OnOptionChanged;

	public StartingOptionVM(AdvancedStartOption data, SandBox.AdvancedStartOptions.AdvancedStartOptions stagedOptions, Func<bool> getAdditionalIsDisabled = null)
	{
		_data = data;
		_stagedOptions = stagedOptions;
		_getAdditionalIsDisabled = getAdditionalIsDisabled;
		_nameTextObj = stagedOptions.GetOptionName(data.StringId) ?? Module.CurrentModule.GlobalTextManager.FindText("str_campaign_starting_options_name", data.StringId);
		TextObject hintText = ((data is ListAdvancedStartOption) ? null : stagedOptions.GetOptionDescription(data.StringId)) ?? Module.CurrentModule.GlobalTextManager.FindText("str_campaign_starting_options_description", data.StringId);
		DescriptionHint = new HintViewModel(hintText);
		if (data is BooleanAdvancedStartOption booleanAdvancedStartOption)
		{
			OptionType = 0;
			ValueAsBoolean = booleanAdvancedStartOption.GetValue<bool>();
		}
		else if (data is FloatAdvancedStartOption floatAdvancedStartOption)
		{
			OptionType = 1;
			MinRange = floatAdvancedStartOption.MinValue;
			MaxRange = floatAdvancedStartOption.MaxValue;
			IsDiscrete = false;
			ValueAsFloat = floatAdvancedStartOption.GetValue<float>();
			OnNumericValueUpdated();
		}
		else if (data is IntAdvancedStartOption intAdvancedStartOption)
		{
			OptionType = 1;
			IsDiscrete = true;
			MinRange = intAdvancedStartOption.MinValue;
			MaxRange = intAdvancedStartOption.MaxValue;
			ValueAsFloat = intAdvancedStartOption.GetValue<int>();
			OnNumericValueUpdated();
		}
		else if (data is ListAdvancedStartOption listAdvancedStartOption)
		{
			OptionType = 2;
			_listData = listAdvancedStartOption;
			_selectionItems = listAdvancedStartOption.GetItems();
			Selector = new SelectorVM<SelectorItemVM>(GetSelectionItemNames(), GetSelectedIndex(), delegate(SelectorVM<SelectorItemVM> selector)
			{
				OnSelectionChanged(selector.SelectedIndex);
			});
		}
		else if (data is UIntAdvancedStartOption uIntAdvancedStartOption)
		{
			OptionType = 3;
			MinInt = (int)TaleWorlds.Library.MathF.Max(uIntAdvancedStartOption.MinValue, 0u);
			MaxInt = (int)TaleWorlds.Library.MathF.Min(uIntAdvancedStartOption.MaxValue, 2147483647u);
			ValueAsInt = (int)uIntAdvancedStartOption.GetValue<uint>();
		}
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Name = _nameTextObj.ToString();
		RandomizeHint = new HintViewModel(new TextObject("{=NSSsxBHV}Randomize"));
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		RandomizeInputKey?.OnFinalize();
	}

	public void ExecuteFocusBegin()
	{
		IsFocused = true;
		StartingOptionVM.OnOptionFocusBegin?.Invoke(this);
	}

	public void ExecuteFocusEnd()
	{
		IsFocused = false;
		StartingOptionVM.OnOptionFocusEnd?.Invoke(this);
	}

	public void UpdateOptionState()
	{
		IsDisabled = _getAdditionalIsDisabled?.Invoke() ?? false;
		if (_data != null)
		{
			bool isHidden = _data.GetIsHidden(_stagedOptions);
			IsHidden = isHidden;
			TextObject optionName = _stagedOptions.GetOptionName(_data.StringId);
			if (optionName != null)
			{
				_nameTextObj = optionName;
				Name = optionName.ToString();
			}
		}
		else
		{
			IsHidden = false;
		}
		if (_selectionItems != null)
		{
			for (int i = 0; i < Selector.ItemList.Count; i++)
			{
				TextObject disabledText;
				bool itemCondition = _listData.GetItemCondition(_selectionItems[i].Identifier, _stagedOptions, out disabledText);
				Selector.ItemList[i].CanBeSelected = !itemCondition;
				Selector.ItemList[i].Hint = ((itemCondition && disabledText != null) ? new HintViewModel(disabledText) : null);
			}
			if (!Selector.SelectedItem.CanBeSelected)
			{
				Selector.ExecuteSelectNextItem();
			}
		}
	}

	private IEnumerable<TextObject> GetSelectionItemNames()
	{
		List<TextObject> list = new List<TextObject>(_selectionItems.Count);
		for (int i = 0; i < _selectionItems.Count; i++)
		{
			list.Add(_stagedOptions.GetItemName(_data.StringId, _selectionItems[i].Identifier));
		}
		return list;
	}

	private int GetSelectedIndex()
	{
		string value = _listData.GetValue<string>();
		for (int i = 0; i < _selectionItems.Count; i++)
		{
			if (object.Equals(_selectionItems[i].Identifier, value))
			{
				return i;
			}
		}
		return 0;
	}

	private void OnSelectionChanged(int selectedIndex)
	{
		_listData.SetValue(_selectionItems[selectedIndex].Identifier);
		StartingOptionVM.OnOptionChanged?.Invoke();
	}

	public TextObject GetComposedDescription()
	{
		if (_data == null)
		{
			return null;
		}
		return _stagedOptions.GetItemDescription(_data.StringId);
	}

	public void ExecuteRandomize()
	{
		if (AllowRandomization)
		{
			int maxValue = ((MaxInt == int.MaxValue) ? int.MaxValue : (MaxInt + 1));
			ValueAsInt = MBRandom.RandomInt(MinInt, maxValue);
		}
	}

	private void OnNumericValueUpdated()
	{
		ValueAsString = _valueAsFloat.ToString(IsDiscrete ? "0" : "0.##");
	}

	public void SetRandomizeInputKey(HotKey hotkey)
	{
		RandomizeInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, isConsoleOnly: true);
	}
}
