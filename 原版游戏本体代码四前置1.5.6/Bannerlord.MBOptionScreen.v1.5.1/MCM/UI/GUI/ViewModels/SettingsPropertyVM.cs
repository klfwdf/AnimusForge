using System;
using System.ComponentModel;
using System.Linq;
using Bannerlord.ButterLib.Common.Helpers;
using MCM.Abstractions;
using MCM.Common;
using MCM.UI.Actions;
using MCM.UI.Dropdown;
using MCM.UI.HotKeys;
using MCM.UI.Utils;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace MCM.UI.GUI.ViewModels;

internal sealed class SettingsPropertyVM : ViewModel
{
	private string _buttonContent = string.Empty;

	private string _name = string.Empty;

	private MCMSelectorVM<MCMSelectorItemVM<TextObject>>? _selectorVMWrapper;

	[DataSourceProperty]
	public bool IsBool => SettingType == SettingType.Bool;

	[DataSourceProperty]
	public bool BoolValue
	{
		get
		{
			if (IsBool)
			{
				object value = PropertyReference.Value;
				if (value is bool)
				{
					return (bool)value;
				}
			}
			return false;
		}
		set
		{
			if (IsBool && BoolValue != value)
			{
				URS.Do(new SetValueTypeAction<bool>(PropertyReference, value));
				OnPropertyChanged("BoolValue");
			}
		}
	}

	[DataSourceProperty]
	public bool IsButton => SettingType == SettingType.Button;

	[DataSourceProperty]
	public string ButtonContent
	{
		get
		{
			return _buttonContent;
		}
		set
		{
			SetField(ref _buttonContent, value, "ButtonContent");
		}
	}

	public ModOptionsVM MainView => SettingsVM.MainView;

	public SettingsVM SettingsVM { get; }

	public SettingsPropertyGroupVM? Group { get; set; }

	public UndoRedoStack URS => SettingsVM.URS;

	public ISettingsPropertyDefinition SettingPropertyDefinition { get; }

	public IRef PropertyReference => SettingPropertyDefinition.PropertyReference;

	public SettingType SettingType => SettingPropertyDefinition.SettingType;

	public string HintText
	{
		get
		{
			if (SettingPropertyDefinition.HintText.Length <= 0)
			{
				return string.Empty;
			}
			return $"{Name}: {new TextObject(SettingPropertyDefinition.HintText)}";
		}
	}

	public string ValueFormat => SettingPropertyDefinition.ValueFormat;

	public IFormatProvider? ValueFormatProvider { get; }

	public bool SatisfiesSearch
	{
		get
		{
			if (!string.IsNullOrEmpty(MainView.SearchText))
			{
				return Name.IndexOf(MainView.SearchText, StringComparison.InvariantCultureIgnoreCase) >= 0;
			}
			return true;
		}
	}

	[DataSourceProperty]
	public string Name
	{
		get
		{
			return _name;
		}
		private set
		{
			SetField(ref _name, value, "Name");
		}
	}

	[DataSourceProperty]
	public bool IsEnabled => Group?.GroupToggle ?? false;

	[DataSourceProperty]
	public bool IsSettingVisible
	{
		get
		{
			if (SettingPropertyDefinition.IsToggle)
			{
				return false;
			}
			SettingsPropertyGroupVM? settingsPropertyGroupVM = Group;
			if (settingsPropertyGroupVM != null && !settingsPropertyGroupVM.GroupToggle)
			{
				return false;
			}
			SettingsPropertyGroupVM? settingsPropertyGroupVM2 = Group;
			if (settingsPropertyGroupVM2 != null && !settingsPropertyGroupVM2.IsExpanded)
			{
				return false;
			}
			if (!SatisfiesSearch)
			{
				return false;
			}
			return true;
		}
	}

	public bool IsSelected { get; set; }

	[DataSourceProperty]
	public bool IsDropdown
	{
		get
		{
			if (!IsDropdownDefault)
			{
				return IsDropdownCheckbox;
			}
			return true;
		}
	}

	[DataSourceProperty]
	public bool IsDropdownDefault
	{
		get
		{
			if (SettingType == SettingType.Dropdown)
			{
				return SettingsUtils.IsForTextDropdown(PropertyReference.Value);
			}
			return false;
		}
	}

	[DataSourceProperty]
	public bool IsDropdownCheckbox
	{
		get
		{
			if (SettingType == SettingType.Dropdown)
			{
				return SettingsUtils.IsForCheckboxDropdown(PropertyReference.Value);
			}
			return false;
		}
	}

	[DataSourceProperty]
	public MCMSelectorVM<MCMSelectorItemVM<TextObject>> DropdownValue => _selectorVMWrapper ?? (_selectorVMWrapper = (IsDropdown ? new MCMSelectorVM<MCMSelectorItemVM<TextObject>, TextObject>(from x in UISettingsUtils.GetDropdownValues(PropertyReference)
		select new TextObject(x.ToString()), new SelectedIndexWrapper(PropertyReference.Value).SelectedIndex) : MCMSelectorVM<MCMSelectorItemVM<TextObject>>.Empty));

	[DataSourceProperty]
	public bool IsIntVisible { get; private set; }

	[DataSourceProperty]
	public bool IsFloatVisible { get; private set; }

	[DataSourceProperty]
	public bool IsInt => SettingType == SettingType.Int;

	[DataSourceProperty]
	public bool IsFloat => SettingType == SettingType.Float;

	[DataSourceProperty]
	public float FloatValue
	{
		get
		{
			if (!IsFloat)
			{
				return 0f;
			}
			object value = PropertyReference.Value;
			if (value is float)
			{
				return (float)value;
			}
			return float.MinValue;
		}
		set
		{
			value = TaleWorlds.Library.MathF.Max(TaleWorlds.Library.MathF.Min(value, MaxFloat), MinFloat);
			if (IsFloat && TaleWorlds.Library.MathF.Abs(FloatValue - value) >= 1E-06f)
			{
				URS.Do(new SetValueTypeAction<float>(PropertyReference, value));
				OnPropertyChanged("FloatValue");
				OnPropertyChanged("NumericValue");
			}
		}
	}

	[DataSourceProperty]
	public int IntValue
	{
		get
		{
			if (!IsInt)
			{
				return 0;
			}
			object value = PropertyReference.Value;
			if (value is int)
			{
				return (int)value;
			}
			return int.MinValue;
		}
		set
		{
			value = TaleWorlds.Library.MathF.Max(TaleWorlds.Library.MathF.Min(value, MaxInt), MinInt);
			if (IsInt && IntValue != value)
			{
				URS.Do(new SetValueTypeAction<int>(PropertyReference, value));
				OnPropertyChanged("IntValue");
				OnPropertyChanged("NumericValue");
			}
		}
	}

	[DataSourceProperty]
	public int MaxInt => (int)SettingPropertyDefinition.MaxValue;

	[DataSourceProperty]
	public int MinInt => (int)SettingPropertyDefinition.MinValue;

	[DataSourceProperty]
	public float MaxFloat => (float)SettingPropertyDefinition.MaxValue;

	[DataSourceProperty]
	public float MinFloat => (float)SettingPropertyDefinition.MinValue;

	[DataSourceProperty]
	public bool IsNotNumeric
	{
		get
		{
			if (!IsInt)
			{
				return !IsFloat;
			}
			return false;
		}
	}

	[DataSourceProperty]
	public bool NumericValueToggle { get; private set; }

	[DataSourceProperty]
	public string NumericValue
	{
		get
		{
			switch (SettingType)
			{
			case SettingType.Int:
				if (PropertyReference.Value is int value2)
				{
					return string.IsNullOrWhiteSpace(ValueFormat) ? string.Format(ValueFormatProvider, "{0}", value2.ToString("0")) : string.Format(ValueFormatProvider, "{0}", value2.ToString(SetNumeric(new TextObject(ValueFormat), value2).ToString()));
				}
				break;
			case SettingType.Float:
				if (PropertyReference.Value is float value)
				{
					return string.IsNullOrWhiteSpace(ValueFormat) ? string.Format(ValueFormatProvider, "{0}", value.ToString("0.00")) : string.Format(ValueFormatProvider, "{0}", value.ToString(SetNumeric(new TextObject(ValueFormat), value).ToString()));
				}
				break;
			}
			return string.Empty;
		}
	}

	[DataSourceProperty]
	public bool IsString => SettingType == SettingType.String;

	[DataSourceProperty]
	public string StringValue
	{
		get
		{
			if (!IsString)
			{
				return string.Empty;
			}
			if (PropertyReference.Value != null)
			{
				if (!(PropertyReference.Value is string result))
				{
					return "ERROR";
				}
				return result;
			}
			return string.Empty;
		}
		set
		{
			if (IsString && StringValue != value)
			{
				URS.Do(new SetStringAction(PropertyReference, value));
				OnPropertyChanged("StringValue");
			}
		}
	}

	public SettingsPropertyVM(ISettingsPropertyDefinition definition, SettingsVM settingsVM)
	{
		SettingsVM = settingsVM;
		SettingPropertyDefinition = definition;
		ValueFormatProvider = TryCreateCustomFormatter(SettingPropertyDefinition.CustomFormatter);
		NumericValueToggle = IsInt || IsFloat;
		PropertyReference.PropertyChanged += OnPropertyChanged;
		if (IsDropdown)
		{
			if (PropertyReference.Value is INotifyPropertyChanged notifyPropertyChanged)
			{
				notifyPropertyChanged.PropertyChanged += OnPropertyChanged;
			}
			DropdownValue.PropertyChanged += DropdownValue_PropertyChanged;
			DropdownValue.PropertyChangedWithValue += DropdownValue_PropertyChangedWithValue;
		}
		INotifyPropertyChanged settingsInstance = SettingsVM.SettingsInstance;
		if (settingsInstance != null)
		{
			settingsInstance.PropertyChanged += OnPropertyChanged;
		}
		RefreshValues();
		ResetValueToDefault resetValueToDefault = MCMUISubModule.ResetValueToDefault;
		if (resetValueToDefault != null)
		{
			resetValueToDefault.IsDownAndReleasedEvent += ResetValueToDefaultOnReleasedEvent;
		}
		static IFormatProvider? TryCreateCustomFormatter(Type? customFormatter)
		{
			if ((object)customFormatter == null)
			{
				return null;
			}
			try
			{
				return Activator.CreateInstance(customFormatter) as IFormatProvider;
			}
			catch (Exception)
			{
				return null;
			}
		}
	}

	public override void OnFinalize()
	{
		ResetValueToDefault resetValueToDefault = MCMUISubModule.ResetValueToDefault;
		if (resetValueToDefault != null)
		{
			resetValueToDefault.IsDownAndReleasedEvent -= ResetValueToDefaultOnReleasedEvent;
		}
		PropertyReference.PropertyChanged -= OnPropertyChanged;
		if (IsDropdown)
		{
			if (PropertyReference.Value is INotifyPropertyChanged notifyPropertyChanged)
			{
				notifyPropertyChanged.PropertyChanged -= OnPropertyChanged;
			}
			DropdownValue.PropertyChanged -= DropdownValue_PropertyChanged;
			DropdownValue.PropertyChangedWithValue -= DropdownValue_PropertyChangedWithValue;
		}
		INotifyPropertyChanged settingsInstance = SettingsVM.SettingsInstance;
		if (settingsInstance != null)
		{
			settingsInstance.PropertyChanged -= OnPropertyChanged;
		}
		base.OnFinalize();
	}

	private void OnPropertyChanged(object? obj, PropertyChangedEventArgs args)
	{
		if (!(args.PropertyName == "SAVE_TRIGGERED"))
		{
			switch (SettingType)
			{
			case SettingType.Bool:
				OnPropertyChanged("BoolValue");
				break;
			case SettingType.Int:
				OnPropertyChanged("IntValue");
				OnPropertyChanged("NumericValue");
				break;
			case SettingType.Float:
				OnPropertyChanged("FloatValue");
				OnPropertyChanged("NumericValue");
				break;
			case SettingType.String:
				OnPropertyChanged("StringValue");
				break;
			case SettingType.Dropdown:
				DropdownValue.SelectedIndex = new SelectedIndexWrapper(PropertyReference.Value).SelectedIndex;
				break;
			case SettingType.Button:
				ButtonContent = new TextObject(SettingPropertyDefinition.Content).ToString();
				break;
			}
			SettingsVM.RecalculatePresetIndex();
		}
	}

	private void ResetValueToDefaultOnReleasedEvent()
	{
		if (IsSelected)
		{
			SettingsVM.ResetSettingsValue(SettingPropertyDefinition.Id);
			switch (SettingType)
			{
			case SettingType.Bool:
				OnPropertyChanged("BoolValue");
				break;
			case SettingType.Int:
				OnPropertyChanged("IntValue");
				OnPropertyChanged("NumericValue");
				break;
			case SettingType.Float:
				OnPropertyChanged("FloatValue");
				OnPropertyChanged("NumericValue");
				break;
			case SettingType.String:
				OnPropertyChanged("StringValue");
				break;
			case SettingType.Dropdown:
				DropdownValue.SelectedIndex = new SelectedIndexWrapper(PropertyReference.Value).SelectedIndex;
				break;
			case SettingType.Button:
				break;
			}
		}
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Name = new TextObject(SettingPropertyDefinition.DisplayName).ToString();
		ButtonContent = new TextObject(SettingPropertyDefinition.Content).ToString();
		DropdownValue.RefreshValues();
	}

	public void OnHover()
	{
		IsSelected = true;
		MainView.HintText = HintText;
	}

	public void OnHoverEnd()
	{
		IsSelected = false;
		MainView.HintText = string.Empty;
	}

	public void OnValueClick()
	{
		if (PropertyReference.Value is Action action)
		{
			action();
		}
	}

	public override string ToString()
	{
		return Name;
	}

	public override int GetHashCode()
	{
		return Name.GetHashCode();
	}

	private void DropdownValue_PropertyChanged(object? obj, PropertyChangedEventArgs args)
	{
		if (obj != null && args.PropertyName == "SelectedIndex")
		{
			URS.Do(new SetSelectedIndexAction(PropertyReference, obj));
		}
	}

	private void DropdownValue_PropertyChangedWithValue(object obj, PropertyChangedWithValueEventArgs args)
	{
		if (args.PropertyName == "SelectedIndex")
		{
			URS.Do(new SetSelectedIndexAction(PropertyReference, obj));
		}
	}

	private static TextObject SetNumeric(TextObject textObject, int value)
	{
		LocalizationHelper.SetNumericVariable(textObject, "NUMERIC", value);
		return textObject;
	}

	private static TextObject SetNumeric(TextObject textObject, float value)
	{
		LocalizationHelper.SetNumericVariable(textObject, "NUMERIC", value);
		return textObject;
	}

	public void OnEditBoxHover()
	{
		switch (SettingType)
		{
		case SettingType.Float:
			IsFloatVisible = !IsFloatVisible;
			NumericValueToggle = !NumericValueToggle;
			OnPropertyChanged("IsFloatVisible");
			OnPropertyChanged("NumericValueToggle");
			break;
		case SettingType.Int:
			IsIntVisible = !IsIntVisible;
			NumericValueToggle = !NumericValueToggle;
			OnPropertyChanged("IsIntVisible");
			OnPropertyChanged("NumericValueToggle");
			break;
		}
	}

	public void OnEditBoxHoverEnd()
	{
		switch (SettingType)
		{
		case SettingType.Float:
			IsFloatVisible = !IsFloatVisible;
			NumericValueToggle = !NumericValueToggle;
			OnPropertyChanged("IsFloatVisible");
			OnPropertyChanged("NumericValueToggle");
			break;
		case SettingType.Int:
			IsIntVisible = !IsIntVisible;
			NumericValueToggle = !NumericValueToggle;
			OnPropertyChanged("IsIntVisible");
			OnPropertyChanged("NumericValueToggle");
			break;
		}
	}
}
