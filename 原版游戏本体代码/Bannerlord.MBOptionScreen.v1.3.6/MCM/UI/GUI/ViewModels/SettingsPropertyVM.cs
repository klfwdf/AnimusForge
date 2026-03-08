using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Bannerlord.ButterLib.Common.Helpers;
using MCM.Abstractions;
using MCM.Common;
using MCM.UI.Actions;
using MCM.UI.Dropdown;
using MCM.UI.HotKeys;
using MCM.UI.Utils;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace MCM.UI.GUI.ViewModels
{
	// Token: 0x02000021 RID: 33
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class SettingsPropertyVM : ViewModel
	{
		// Token: 0x17000051 RID: 81
		// (get) Token: 0x06000117 RID: 279 RVA: 0x00005847 File Offset: 0x00003A47
		[DataSourceProperty]
		public bool IsBool
		{
			get
			{
				return this.SettingType == 0;
			}
		}

		// Token: 0x17000052 RID: 82
		// (get) Token: 0x06000118 RID: 280 RVA: 0x00005854 File Offset: 0x00003A54
		// (set) Token: 0x06000119 RID: 281 RVA: 0x00005889 File Offset: 0x00003A89
		[DataSourceProperty]
		public bool BoolValue
		{
			get
			{
				if (this.IsBool)
				{
					object value = this.PropertyReference.Value;
					if (value is bool)
					{
						return (bool)value;
					}
				}
				return false;
			}
			set
			{
				if (this.IsBool && this.BoolValue != value)
				{
					this.URS.Do(new SetValueTypeAction<bool>(this.PropertyReference, value));
					base.OnPropertyChanged("BoolValue");
				}
			}
		}

		// Token: 0x17000053 RID: 83
		// (get) Token: 0x0600011A RID: 282 RVA: 0x000058BE File Offset: 0x00003ABE
		[DataSourceProperty]
		public bool IsButton
		{
			get
			{
				return this.SettingType == 5;
			}
		}

		// Token: 0x17000054 RID: 84
		// (get) Token: 0x0600011B RID: 283 RVA: 0x000058C9 File Offset: 0x00003AC9
		// (set) Token: 0x0600011C RID: 284 RVA: 0x000058D1 File Offset: 0x00003AD1
		[DataSourceProperty]
		public string ButtonContent
		{
			get
			{
				return this._buttonContent;
			}
			set
			{
				base.SetField<string>(ref this._buttonContent, value, "ButtonContent");
			}
		}

		// Token: 0x17000055 RID: 85
		// (get) Token: 0x0600011D RID: 285 RVA: 0x000058E6 File Offset: 0x00003AE6
		public ModOptionsVM MainView
		{
			get
			{
				return this.SettingsVM.MainView;
			}
		}

		// Token: 0x17000056 RID: 86
		// (get) Token: 0x0600011E RID: 286 RVA: 0x000058F3 File Offset: 0x00003AF3
		public SettingsVM SettingsVM { get; }

		// Token: 0x17000057 RID: 87
		// (get) Token: 0x0600011F RID: 287 RVA: 0x000058FB File Offset: 0x00003AFB
		// (set) Token: 0x06000120 RID: 288 RVA: 0x00005903 File Offset: 0x00003B03
		[Nullable(2)]
		public SettingsPropertyGroupVM Group
		{
			[NullableContext(2)]
			get;
			[NullableContext(2)]
			set;
		}

		// Token: 0x17000058 RID: 88
		// (get) Token: 0x06000121 RID: 289 RVA: 0x0000590C File Offset: 0x00003B0C
		public UndoRedoStack URS
		{
			get
			{
				return this.SettingsVM.URS;
			}
		}

		// Token: 0x17000059 RID: 89
		// (get) Token: 0x06000122 RID: 290 RVA: 0x00005919 File Offset: 0x00003B19
		public ISettingsPropertyDefinition SettingPropertyDefinition { get; }

		// Token: 0x1700005A RID: 90
		// (get) Token: 0x06000123 RID: 291 RVA: 0x00005921 File Offset: 0x00003B21
		public IRef PropertyReference
		{
			get
			{
				return this.SettingPropertyDefinition.PropertyReference;
			}
		}

		// Token: 0x1700005B RID: 91
		// (get) Token: 0x06000124 RID: 292 RVA: 0x0000592E File Offset: 0x00003B2E
		public SettingType SettingType
		{
			get
			{
				return this.SettingPropertyDefinition.SettingType;
			}
		}

		// Token: 0x1700005C RID: 92
		// (get) Token: 0x06000125 RID: 293 RVA: 0x0000593B File Offset: 0x00003B3B
		public string HintText
		{
			get
			{
				if (this.SettingPropertyDefinition.HintText.Length <= 0)
				{
					return string.Empty;
				}
				return string.Format("{0}: {1}", this.Name, new TextObject(this.SettingPropertyDefinition.HintText, null));
			}
		}

		// Token: 0x1700005D RID: 93
		// (get) Token: 0x06000126 RID: 294 RVA: 0x00005977 File Offset: 0x00003B77
		public string ValueFormat
		{
			get
			{
				return this.SettingPropertyDefinition.ValueFormat;
			}
		}

		// Token: 0x1700005E RID: 94
		// (get) Token: 0x06000127 RID: 295 RVA: 0x00005984 File Offset: 0x00003B84
		[Nullable(2)]
		public IFormatProvider ValueFormatProvider
		{
			[NullableContext(2)]
			get;
		}

		// Token: 0x1700005F RID: 95
		// (get) Token: 0x06000128 RID: 296 RVA: 0x0000598C File Offset: 0x00003B8C
		public bool SatisfiesSearch
		{
			get
			{
				return string.IsNullOrEmpty(this.MainView.SearchText) || this.Name.IndexOf(this.MainView.SearchText, StringComparison.InvariantCultureIgnoreCase) >= 0;
			}
		}

		// Token: 0x17000060 RID: 96
		// (get) Token: 0x06000129 RID: 297 RVA: 0x000059BF File Offset: 0x00003BBF
		// (set) Token: 0x0600012A RID: 298 RVA: 0x000059C7 File Offset: 0x00003BC7
		[DataSourceProperty]
		public string Name
		{
			get
			{
				return this._name;
			}
			private set
			{
				base.SetField<string>(ref this._name, value, "Name");
			}
		}

		// Token: 0x17000061 RID: 97
		// (get) Token: 0x0600012B RID: 299 RVA: 0x000059DC File Offset: 0x00003BDC
		[DataSourceProperty]
		public bool IsEnabled
		{
			get
			{
				SettingsPropertyGroupVM group = this.Group;
				return group != null && group.GroupToggle;
			}
		}

		// Token: 0x17000062 RID: 98
		// (get) Token: 0x0600012C RID: 300 RVA: 0x000059F0 File Offset: 0x00003BF0
		[DataSourceProperty]
		public bool IsSettingVisible
		{
			get
			{
				if (this.SettingPropertyDefinition.IsToggle)
				{
					return false;
				}
				SettingsPropertyGroupVM group = this.Group;
				if (group != null && !group.GroupToggle)
				{
					return false;
				}
				SettingsPropertyGroupVM group2 = this.Group;
				return (group2 == null || group2.IsExpanded) && this.SatisfiesSearch;
			}
		}

		// Token: 0x17000063 RID: 99
		// (get) Token: 0x0600012D RID: 301 RVA: 0x00005A49 File Offset: 0x00003C49
		// (set) Token: 0x0600012E RID: 302 RVA: 0x00005A51 File Offset: 0x00003C51
		public bool IsSelected { get; set; }

		// Token: 0x0600012F RID: 303 RVA: 0x00005A5C File Offset: 0x00003C5C
		public SettingsPropertyVM(ISettingsPropertyDefinition definition, SettingsVM settingsVM)
		{
			this.SettingsVM = settingsVM;
			this.SettingPropertyDefinition = definition;
			this.ValueFormatProvider = SettingsPropertyVM.<.ctor>g__TryCreateCustomFormatter|50_0(this.SettingPropertyDefinition.CustomFormatter);
			this.NumericValueToggle = this.IsInt || this.IsFloat;
			this.PropertyReference.PropertyChanged += this.OnPropertyChanged;
			if (this.IsDropdown)
			{
				INotifyPropertyChanged notifyPropertyChanged = this.PropertyReference.Value as INotifyPropertyChanged;
				if (notifyPropertyChanged != null)
				{
					notifyPropertyChanged.PropertyChanged += this.OnPropertyChanged;
				}
				this.DropdownValue.PropertyChanged += this.DropdownValue_PropertyChanged;
				this.DropdownValue.PropertyChangedWithValue += this.DropdownValue_PropertyChangedWithValue;
			}
			INotifyPropertyChanged notifyPropertyChanged2 = this.SettingsVM.SettingsInstance;
			if (notifyPropertyChanged2 != null)
			{
				notifyPropertyChanged2.PropertyChanged += this.OnPropertyChanged;
			}
			this.RefreshValues();
			ResetValueToDefault key = MCMUISubModule.ResetValueToDefault;
			if (key != null)
			{
				key.IsDownAndReleasedEvent += this.ResetValueToDefaultOnReleasedEvent;
			}
		}

		// Token: 0x06000130 RID: 304 RVA: 0x00005B78 File Offset: 0x00003D78
		public override void OnFinalize()
		{
			ResetValueToDefault key = MCMUISubModule.ResetValueToDefault;
			if (key != null)
			{
				key.IsDownAndReleasedEvent -= this.ResetValueToDefaultOnReleasedEvent;
			}
			this.PropertyReference.PropertyChanged -= this.OnPropertyChanged;
			if (this.IsDropdown)
			{
				INotifyPropertyChanged notifyPropertyChanged = this.PropertyReference.Value as INotifyPropertyChanged;
				if (notifyPropertyChanged != null)
				{
					notifyPropertyChanged.PropertyChanged -= this.OnPropertyChanged;
				}
				this.DropdownValue.PropertyChanged -= this.DropdownValue_PropertyChanged;
				this.DropdownValue.PropertyChangedWithValue -= this.DropdownValue_PropertyChangedWithValue;
			}
			INotifyPropertyChanged notifyPropertyChanged2 = this.SettingsVM.SettingsInstance;
			if (notifyPropertyChanged2 != null)
			{
				notifyPropertyChanged2.PropertyChanged -= this.OnPropertyChanged;
			}
			base.OnFinalize();
		}

		// Token: 0x06000131 RID: 305 RVA: 0x00005C3C File Offset: 0x00003E3C
		private void OnPropertyChanged([Nullable(2)] object obj, PropertyChangedEventArgs args)
		{
			if (args.PropertyName == "SAVE_TRIGGERED")
			{
				return;
			}
			switch (this.SettingType)
			{
			case 0:
				base.OnPropertyChanged("BoolValue");
				break;
			case 1:
				base.OnPropertyChanged("IntValue");
				base.OnPropertyChanged("NumericValue");
				break;
			case 2:
				base.OnPropertyChanged("FloatValue");
				base.OnPropertyChanged("NumericValue");
				break;
			case 3:
				base.OnPropertyChanged("StringValue");
				break;
			case 4:
				this.DropdownValue.SelectedIndex = new SelectedIndexWrapper(this.PropertyReference.Value).SelectedIndex;
				break;
			case 5:
				this.ButtonContent = new TextObject(this.SettingPropertyDefinition.Content, null).ToString();
				break;
			}
			this.SettingsVM.RecalculatePresetIndex();
		}

		// Token: 0x06000132 RID: 306 RVA: 0x00005D1C File Offset: 0x00003F1C
		private void ResetValueToDefaultOnReleasedEvent()
		{
			if (this.IsSelected)
			{
				this.SettingsVM.ResetSettingsValue(this.SettingPropertyDefinition.Id);
				switch (this.SettingType)
				{
				case 0:
					base.OnPropertyChanged("BoolValue");
					return;
				case 1:
					base.OnPropertyChanged("IntValue");
					base.OnPropertyChanged("NumericValue");
					return;
				case 2:
					base.OnPropertyChanged("FloatValue");
					base.OnPropertyChanged("NumericValue");
					return;
				case 3:
					base.OnPropertyChanged("StringValue");
					return;
				case 4:
					this.DropdownValue.SelectedIndex = new SelectedIndexWrapper(this.PropertyReference.Value).SelectedIndex;
					break;
				case 5:
					break;
				default:
					return;
				}
			}
		}

		// Token: 0x06000133 RID: 307 RVA: 0x00005DDC File Offset: 0x00003FDC
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.Name = new TextObject(this.SettingPropertyDefinition.DisplayName, null).ToString();
			this.ButtonContent = new TextObject(this.SettingPropertyDefinition.Content, null).ToString();
			this.DropdownValue.RefreshValues();
		}

		// Token: 0x06000134 RID: 308 RVA: 0x00005E32 File Offset: 0x00004032
		public void OnHover()
		{
			this.IsSelected = true;
			this.MainView.HintText = this.HintText;
		}

		// Token: 0x06000135 RID: 309 RVA: 0x00005E4C File Offset: 0x0000404C
		public void OnHoverEnd()
		{
			this.IsSelected = false;
			this.MainView.HintText = string.Empty;
		}

		// Token: 0x06000136 RID: 310 RVA: 0x00005E68 File Offset: 0x00004068
		public void OnValueClick()
		{
			Action val = this.PropertyReference.Value as Action;
			if (val != null)
			{
				val();
			}
		}

		// Token: 0x06000137 RID: 311 RVA: 0x00005E8F File Offset: 0x0000408F
		public override string ToString()
		{
			return this.Name;
		}

		// Token: 0x06000138 RID: 312 RVA: 0x00005E97 File Offset: 0x00004097
		public override int GetHashCode()
		{
			return this.Name.GetHashCode();
		}

		// Token: 0x17000064 RID: 100
		// (get) Token: 0x06000139 RID: 313 RVA: 0x00005EA4 File Offset: 0x000040A4
		[DataSourceProperty]
		public bool IsDropdown
		{
			get
			{
				return this.IsDropdownDefault || this.IsDropdownCheckbox;
			}
		}

		// Token: 0x17000065 RID: 101
		// (get) Token: 0x0600013A RID: 314 RVA: 0x00005EB6 File Offset: 0x000040B6
		[DataSourceProperty]
		public bool IsDropdownDefault
		{
			get
			{
				return this.SettingType == 4 && SettingsUtils.IsForTextDropdown(this.PropertyReference.Value);
			}
		}

		// Token: 0x17000066 RID: 102
		// (get) Token: 0x0600013B RID: 315 RVA: 0x00005ED3 File Offset: 0x000040D3
		[DataSourceProperty]
		public bool IsDropdownCheckbox
		{
			get
			{
				return this.SettingType == 4 && SettingsUtils.IsForCheckboxDropdown(this.PropertyReference.Value);
			}
		}

		// Token: 0x17000067 RID: 103
		// (get) Token: 0x0600013C RID: 316 RVA: 0x00005EF0 File Offset: 0x000040F0
		[DataSourceProperty]
		public MCMSelectorVM<MCMSelectorItemVM<TextObject>> DropdownValue
		{
			get
			{
				MCMSelectorVM<MCMSelectorItemVM<TextObject>> result;
				if ((result = this._selectorVMWrapper) == null)
				{
					MCMSelectorVM<MCMSelectorItemVM<TextObject>> selectorVMWrapper;
					if (!this.IsDropdown)
					{
						selectorVMWrapper = MCMSelectorVM<MCMSelectorItemVM<TextObject>>.Empty;
					}
					else
					{
						selectorVMWrapper = new MCMSelectorVM<MCMSelectorItemVM<TextObject>, TextObject>(from x in UISettingsUtils.GetDropdownValues(this.PropertyReference)
							select new TextObject(x.ToString(), null), new SelectedIndexWrapper(this.PropertyReference.Value).SelectedIndex);
					}
					result = (this._selectorVMWrapper = selectorVMWrapper);
				}
				return result;
			}
		}

		// Token: 0x0600013D RID: 317 RVA: 0x00005F6B File Offset: 0x0000416B
		private void DropdownValue_PropertyChanged([Nullable(2)] object obj, PropertyChangedEventArgs args)
		{
			if (obj != null && args.PropertyName == "SelectedIndex")
			{
				this.URS.Do(new SetSelectedIndexAction(this.PropertyReference, obj));
			}
		}

		// Token: 0x0600013E RID: 318 RVA: 0x00005F99 File Offset: 0x00004199
		private void DropdownValue_PropertyChangedWithValue(object obj, PropertyChangedWithValueEventArgs args)
		{
			if (args.PropertyName == "SelectedIndex")
			{
				this.URS.Do(new SetSelectedIndexAction(this.PropertyReference, obj));
			}
		}

		// Token: 0x0600013F RID: 319 RVA: 0x00005FC5 File Offset: 0x000041C5
		private static TextObject SetNumeric(TextObject textObject, int value)
		{
			LocalizationHelper.SetNumericVariable(textObject, "NUMERIC", value, null);
			return textObject;
		}

		// Token: 0x06000140 RID: 320 RVA: 0x00005FD5 File Offset: 0x000041D5
		private static TextObject SetNumeric(TextObject textObject, float value)
		{
			LocalizationHelper.SetNumericVariable(textObject, "NUMERIC", value, null);
			return textObject;
		}

		// Token: 0x17000068 RID: 104
		// (get) Token: 0x06000141 RID: 321 RVA: 0x00005FE5 File Offset: 0x000041E5
		// (set) Token: 0x06000142 RID: 322 RVA: 0x00005FED File Offset: 0x000041ED
		[DataSourceProperty]
		public bool IsIntVisible { get; private set; }

		// Token: 0x17000069 RID: 105
		// (get) Token: 0x06000143 RID: 323 RVA: 0x00005FF6 File Offset: 0x000041F6
		// (set) Token: 0x06000144 RID: 324 RVA: 0x00005FFE File Offset: 0x000041FE
		[DataSourceProperty]
		public bool IsFloatVisible { get; private set; }

		// Token: 0x1700006A RID: 106
		// (get) Token: 0x06000145 RID: 325 RVA: 0x00006007 File Offset: 0x00004207
		[DataSourceProperty]
		public bool IsInt
		{
			get
			{
				return this.SettingType == 1;
			}
		}

		// Token: 0x1700006B RID: 107
		// (get) Token: 0x06000146 RID: 326 RVA: 0x00006012 File Offset: 0x00004212
		[DataSourceProperty]
		public bool IsFloat
		{
			get
			{
				return this.SettingType == 2;
			}
		}

		// Token: 0x1700006C RID: 108
		// (get) Token: 0x06000147 RID: 327 RVA: 0x00006020 File Offset: 0x00004220
		// (set) Token: 0x06000148 RID: 328 RVA: 0x00006060 File Offset: 0x00004260
		[DataSourceProperty]
		public float FloatValue
		{
			get
			{
				if (!this.IsFloat)
				{
					return 0f;
				}
				object value = this.PropertyReference.Value;
				if (value is float)
				{
					return (float)value;
				}
				return float.MinValue;
			}
			set
			{
				value = MathF.Max(MathF.Min(value, this.MaxFloat), this.MinFloat);
				if (this.IsFloat && MathF.Abs(this.FloatValue - value) >= 1E-06f)
				{
					this.URS.Do(new SetValueTypeAction<float>(this.PropertyReference, value));
					base.OnPropertyChanged("FloatValue");
					base.OnPropertyChanged("NumericValue");
				}
			}
		}

		// Token: 0x1700006D RID: 109
		// (get) Token: 0x06000149 RID: 329 RVA: 0x000060D0 File Offset: 0x000042D0
		// (set) Token: 0x0600014A RID: 330 RVA: 0x0000610C File Offset: 0x0000430C
		[DataSourceProperty]
		public int IntValue
		{
			get
			{
				if (!this.IsInt)
				{
					return 0;
				}
				object value = this.PropertyReference.Value;
				if (value is int)
				{
					return (int)value;
				}
				return int.MinValue;
			}
			set
			{
				value = MathF.Max(MathF.Min(value, this.MaxInt), this.MinInt);
				if (this.IsInt && this.IntValue != value)
				{
					this.URS.Do(new SetValueTypeAction<int>(this.PropertyReference, value));
					base.OnPropertyChanged("IntValue");
					base.OnPropertyChanged("NumericValue");
				}
			}
		}

		// Token: 0x1700006E RID: 110
		// (get) Token: 0x0600014B RID: 331 RVA: 0x00006170 File Offset: 0x00004370
		[DataSourceProperty]
		public int MaxInt
		{
			get
			{
				return (int)this.SettingPropertyDefinition.MaxValue;
			}
		}

		// Token: 0x1700006F RID: 111
		// (get) Token: 0x0600014C RID: 332 RVA: 0x00006182 File Offset: 0x00004382
		[DataSourceProperty]
		public int MinInt
		{
			get
			{
				return (int)this.SettingPropertyDefinition.MinValue;
			}
		}

		// Token: 0x17000070 RID: 112
		// (get) Token: 0x0600014D RID: 333 RVA: 0x00006194 File Offset: 0x00004394
		[DataSourceProperty]
		public float MaxFloat
		{
			get
			{
				return (float)this.SettingPropertyDefinition.MaxValue;
			}
		}

		// Token: 0x17000071 RID: 113
		// (get) Token: 0x0600014E RID: 334 RVA: 0x000061A7 File Offset: 0x000043A7
		[DataSourceProperty]
		public float MinFloat
		{
			get
			{
				return (float)this.SettingPropertyDefinition.MinValue;
			}
		}

		// Token: 0x17000072 RID: 114
		// (get) Token: 0x0600014F RID: 335 RVA: 0x000061BA File Offset: 0x000043BA
		[DataSourceProperty]
		public bool IsNotNumeric
		{
			get
			{
				return !this.IsInt && !this.IsFloat;
			}
		}

		// Token: 0x17000073 RID: 115
		// (get) Token: 0x06000150 RID: 336 RVA: 0x000061CF File Offset: 0x000043CF
		// (set) Token: 0x06000151 RID: 337 RVA: 0x000061D7 File Offset: 0x000043D7
		[DataSourceProperty]
		public bool NumericValueToggle { get; private set; }

		// Token: 0x17000074 RID: 116
		// (get) Token: 0x06000152 RID: 338 RVA: 0x000061E0 File Offset: 0x000043E0
		[DataSourceProperty]
		public string NumericValue
		{
			get
			{
				SettingType settingType = this.SettingType;
				if (settingType != 1)
				{
					if (settingType == 2)
					{
						object value = this.PropertyReference.Value;
						if (value is float)
						{
							float val = (float)value;
							return string.IsNullOrWhiteSpace(this.ValueFormat) ? string.Format(this.ValueFormatProvider, "{0}", val.ToString("0.00")) : string.Format(this.ValueFormatProvider, "{0}", val.ToString(SettingsPropertyVM.SetNumeric(new TextObject(this.ValueFormat, null), val).ToString()));
						}
					}
				}
				else
				{
					object value = this.PropertyReference.Value;
					if (value is int)
					{
						int val2 = (int)value;
						return string.IsNullOrWhiteSpace(this.ValueFormat) ? string.Format(this.ValueFormatProvider, "{0}", val2.ToString("0")) : string.Format(this.ValueFormatProvider, "{0}", val2.ToString(SettingsPropertyVM.SetNumeric(new TextObject(this.ValueFormat, null), val2).ToString()));
					}
				}
				return string.Empty;
			}
		}

		// Token: 0x06000153 RID: 339 RVA: 0x00006308 File Offset: 0x00004508
		public void OnEditBoxHover()
		{
			SettingType settingType = this.SettingType;
			if (settingType != 1)
			{
				if (settingType == 2)
				{
					this.IsFloatVisible = !this.IsFloatVisible;
					this.NumericValueToggle = !this.NumericValueToggle;
					base.OnPropertyChanged("IsFloatVisible");
					base.OnPropertyChanged("NumericValueToggle");
					return;
				}
			}
			else
			{
				this.IsIntVisible = !this.IsIntVisible;
				this.NumericValueToggle = !this.NumericValueToggle;
				base.OnPropertyChanged("IsIntVisible");
				base.OnPropertyChanged("NumericValueToggle");
			}
		}

		// Token: 0x06000154 RID: 340 RVA: 0x00006390 File Offset: 0x00004590
		public void OnEditBoxHoverEnd()
		{
			SettingType settingType = this.SettingType;
			if (settingType != 1)
			{
				if (settingType == 2)
				{
					this.IsFloatVisible = !this.IsFloatVisible;
					this.NumericValueToggle = !this.NumericValueToggle;
					base.OnPropertyChanged("IsFloatVisible");
					base.OnPropertyChanged("NumericValueToggle");
					return;
				}
			}
			else
			{
				this.IsIntVisible = !this.IsIntVisible;
				this.NumericValueToggle = !this.NumericValueToggle;
				base.OnPropertyChanged("IsIntVisible");
				base.OnPropertyChanged("NumericValueToggle");
			}
		}

		// Token: 0x17000075 RID: 117
		// (get) Token: 0x06000155 RID: 341 RVA: 0x00006415 File Offset: 0x00004615
		[DataSourceProperty]
		public bool IsString
		{
			get
			{
				return this.SettingType == 3;
			}
		}

		// Token: 0x17000076 RID: 118
		// (get) Token: 0x06000156 RID: 342 RVA: 0x00006420 File Offset: 0x00004620
		// (set) Token: 0x06000157 RID: 343 RVA: 0x00006469 File Offset: 0x00004669
		[DataSourceProperty]
		public string StringValue
		{
			get
			{
				if (!this.IsString)
				{
					return string.Empty;
				}
				if (this.PropertyReference.Value == null)
				{
					return string.Empty;
				}
				string val = this.PropertyReference.Value as string;
				if (val == null)
				{
					return "ERROR";
				}
				return val;
			}
			set
			{
				if (this.IsString && this.StringValue != value)
				{
					this.URS.Do(new SetStringAction(this.PropertyReference, value));
					base.OnPropertyChanged("StringValue");
				}
			}
		}

		// Token: 0x06000158 RID: 344 RVA: 0x000064A4 File Offset: 0x000046A4
		[NullableContext(2)]
		[CompilerGenerated]
		internal static IFormatProvider <.ctor>g__TryCreateCustomFormatter|50_0(Type customFormatter)
		{
			if (customFormatter == null)
			{
				return null;
			}
			IFormatProvider result;
			try
			{
				result = Activator.CreateInstance(customFormatter) as IFormatProvider;
			}
			catch (Exception)
			{
				result = null;
			}
			return result;
		}

		// Token: 0x04000048 RID: 72
		private string _buttonContent = string.Empty;

		// Token: 0x04000049 RID: 73
		private string _name = string.Empty;

		// Token: 0x0400004F RID: 79
		[Nullable(new byte[] { 2, 1, 1 })]
		private MCMSelectorVM<MCMSelectorItemVM<TextObject>> _selectorVMWrapper;
	}
}
