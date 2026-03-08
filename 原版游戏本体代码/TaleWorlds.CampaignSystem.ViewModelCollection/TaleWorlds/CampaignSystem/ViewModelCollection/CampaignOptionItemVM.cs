using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection
{
	// Token: 0x02000007 RID: 7
	public class CampaignOptionItemVM : ViewModel
	{
		// Token: 0x17000020 RID: 32
		// (get) Token: 0x06000070 RID: 112 RVA: 0x000032BB File Offset: 0x000014BB
		// (set) Token: 0x06000071 RID: 113 RVA: 0x000032C3 File Offset: 0x000014C3
		public ICampaignOptionData OptionData { get; private set; }

		// Token: 0x06000072 RID: 114 RVA: 0x000032CC File Offset: 0x000014CC
		public CampaignOptionItemVM(ICampaignOptionData optionData)
		{
			this.OptionData = optionData;
			this.OptionData.GetEnableState();
			this.Hint = new HintViewModel();
			this._dataType = this.OptionData.GetDataType();
			if (this._dataType == CampaignOptionDataType.Boolean)
			{
				this._optionDataAsBoolean = this.OptionData as BooleanCampaignOptionData;
				this.ValueAsBoolean = this._optionDataAsBoolean.GetValue() != 0f;
				this.OptionType = 0;
			}
			else if (this._dataType == CampaignOptionDataType.Numeric)
			{
				this._optionDataAsNumeric = this.OptionData as NumericCampaignOptionData;
				this.OptionType = 1;
				this.MinRange = this._optionDataAsNumeric.MinValue;
				this.MaxRange = this._optionDataAsNumeric.MaxValue;
				this.IsDiscrete = this._optionDataAsNumeric.IsDiscrete;
				this.ValueAsRange = this._optionDataAsNumeric.GetValue();
			}
			else if (this._dataType == CampaignOptionDataType.Selection)
			{
				this._optionDataAsSelection = this.OptionData as SelectionCampaignOptionData;
				List<TextObject> selections = this._optionDataAsSelection.Selections;
				int selectedIndex = (int)this._optionDataAsSelection.GetValue();
				this.SelectionSelector = new CampaignOptionSelectorVM(selections, selectedIndex, null);
				this.SelectionSelector.SetOnChangeAction(new Action<SelectorVM<SelectorItemVM>>(this.OnSelectionOptionValueChanged));
				this.OptionType = 2;
				this.SelectionSelector.SelectedIndex = (int)this._optionDataAsSelection.GetValue();
			}
			else if (this._dataType == CampaignOptionDataType.Action)
			{
				this._optionDataAsAction = this.OptionData as ActionCampaignOptionData;
				this.HideOptionName = true;
				this.OptionType = 3;
			}
			this.RefreshValues();
		}

		// Token: 0x06000073 RID: 115 RVA: 0x0000345E File Offset: 0x0000165E
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.Name = this.OptionData.GetName();
			this.RefreshDisabledStatus();
		}

		// Token: 0x06000074 RID: 116 RVA: 0x00003480 File Offset: 0x00001680
		public void RefreshDisabledStatus()
		{
			string description = this.OptionData.GetDescription();
			TextObject textObject = new TextObject("{=!}" + description, null);
			CampaignOptionDisableStatus isDisabledWithReason = this.OptionData.GetIsDisabledWithReason();
			this.IsDisabled = isDisabledWithReason.IsDisabled;
			if (!string.IsNullOrEmpty(isDisabledWithReason.DisabledReason))
			{
				string variable = textObject.ToString();
				string disabledReason = isDisabledWithReason.DisabledReason;
				textObject = GameTexts.FindText("str_string_newline_newline_string", null).CopyTextObject();
				textObject.SetTextVariable("STR1", variable);
				textObject.SetTextVariable("STR2", disabledReason);
			}
			if (this.IsDisabled && isDisabledWithReason.ValueIfDisabled != -1f)
			{
				this.SetValue(isDisabledWithReason.ValueIfDisabled);
			}
			this.Hint.HintText = textObject;
		}

		// Token: 0x06000075 RID: 117 RVA: 0x0000353D File Offset: 0x0000173D
		public void ExecuteAction()
		{
			ActionCampaignOptionData optionDataAsAction = this._optionDataAsAction;
			if (optionDataAsAction == null)
			{
				return;
			}
			optionDataAsAction.ExecuteAction();
		}

		// Token: 0x06000076 RID: 118 RVA: 0x0000354F File Offset: 0x0000174F
		public void OnSelectionOptionValueChanged(SelectorVM<SelectorItemVM> selector)
		{
			if (selector.SelectedIndex >= 0 && this._optionDataAsSelection != null)
			{
				this._optionDataAsSelection.SetValue((float)selector.SelectedIndex);
				Action<CampaignOptionItemVM> onValueChanged = this._onValueChanged;
				if (onValueChanged == null)
				{
					return;
				}
				onValueChanged(this);
			}
		}

		// Token: 0x06000077 RID: 119 RVA: 0x00003588 File Offset: 0x00001788
		public void SetValue(float value)
		{
			if (this._dataType == CampaignOptionDataType.Boolean)
			{
				this.ValueAsBoolean = value != 0f;
			}
			else if (this._dataType == CampaignOptionDataType.Numeric)
			{
				this.ValueAsRange = value;
			}
			else if (this._dataType == CampaignOptionDataType.Selection)
			{
				this.SelectionSelector.SelectedIndex = (int)value;
			}
			this.OptionData.SetValue(value);
		}

		// Token: 0x06000078 RID: 120 RVA: 0x000035E4 File Offset: 0x000017E4
		public void SetOnValueChangedCallback(Action<CampaignOptionItemVM> onValueChanged)
		{
			this._onValueChanged = onValueChanged;
		}

		// Token: 0x17000021 RID: 33
		// (get) Token: 0x06000079 RID: 121 RVA: 0x000035ED File Offset: 0x000017ED
		// (set) Token: 0x0600007A RID: 122 RVA: 0x000035F5 File Offset: 0x000017F5
		[DataSourceProperty]
		public bool HideOptionName
		{
			get
			{
				return this._hideOptionName;
			}
			set
			{
				if (value != this._hideOptionName)
				{
					this._hideOptionName = value;
					base.OnPropertyChangedWithValue(value, "HideOptionName");
				}
			}
		}

		// Token: 0x17000022 RID: 34
		// (get) Token: 0x0600007B RID: 123 RVA: 0x00003613 File Offset: 0x00001813
		// (set) Token: 0x0600007C RID: 124 RVA: 0x0000361B File Offset: 0x0000181B
		[DataSourceProperty]
		public string Name
		{
			get
			{
				return this._name;
			}
			set
			{
				if (value != this._name)
				{
					this._name = value;
					base.OnPropertyChangedWithValue<string>(value, "Name");
				}
			}
		}

		// Token: 0x17000023 RID: 35
		// (get) Token: 0x0600007D RID: 125 RVA: 0x0000363E File Offset: 0x0000183E
		// (set) Token: 0x0600007E RID: 126 RVA: 0x00003646 File Offset: 0x00001846
		[DataSourceProperty]
		public HintViewModel Hint
		{
			get
			{
				return this._hint;
			}
			set
			{
				if (value != this._hint)
				{
					this._hint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "Hint");
				}
			}
		}

		// Token: 0x17000024 RID: 36
		// (get) Token: 0x0600007F RID: 127 RVA: 0x00003664 File Offset: 0x00001864
		// (set) Token: 0x06000080 RID: 128 RVA: 0x0000366C File Offset: 0x0000186C
		[DataSourceProperty]
		public int OptionType
		{
			get
			{
				return this._optionType;
			}
			set
			{
				if (value != this._optionType)
				{
					this._optionType = value;
					base.OnPropertyChangedWithValue(value, "OptionType");
				}
			}
		}

		// Token: 0x17000025 RID: 37
		// (get) Token: 0x06000081 RID: 129 RVA: 0x0000368A File Offset: 0x0000188A
		// (set) Token: 0x06000082 RID: 130 RVA: 0x00003694 File Offset: 0x00001894
		[DataSourceProperty]
		public bool ValueAsBoolean
		{
			get
			{
				return this._valueAsBoolean;
			}
			set
			{
				if (value != this._valueAsBoolean)
				{
					this._valueAsBoolean = value;
					base.OnPropertyChangedWithValue(value, "ValueAsBoolean");
					this._optionDataAsBoolean.SetValue(value ? 1f : 0f);
					Action<CampaignOptionItemVM> onValueChanged = this._onValueChanged;
					if (onValueChanged == null)
					{
						return;
					}
					onValueChanged(this);
				}
			}
		}

		// Token: 0x17000026 RID: 38
		// (get) Token: 0x06000083 RID: 131 RVA: 0x000036E8 File Offset: 0x000018E8
		// (set) Token: 0x06000084 RID: 132 RVA: 0x000036F0 File Offset: 0x000018F0
		[DataSourceProperty]
		public bool IsDiscrete
		{
			get
			{
				return this._isDiscrete;
			}
			set
			{
				if (value != this._isDiscrete)
				{
					this._isDiscrete = value;
					base.OnPropertyChangedWithValue(value, "IsDiscrete");
				}
			}
		}

		// Token: 0x17000027 RID: 39
		// (get) Token: 0x06000085 RID: 133 RVA: 0x0000370E File Offset: 0x0000190E
		// (set) Token: 0x06000086 RID: 134 RVA: 0x00003716 File Offset: 0x00001916
		[DataSourceProperty]
		public bool IsDisabled
		{
			get
			{
				return this._isDisabled;
			}
			set
			{
				if (value != this._isDisabled)
				{
					this._isDisabled = value;
					base.OnPropertyChangedWithValue(value, "IsDisabled");
					if (this.SelectionSelector != null)
					{
						this.SelectionSelector.IsEnabled = !value;
					}
				}
			}
		}

		// Token: 0x17000028 RID: 40
		// (get) Token: 0x06000087 RID: 135 RVA: 0x0000374B File Offset: 0x0000194B
		// (set) Token: 0x06000088 RID: 136 RVA: 0x00003753 File Offset: 0x00001953
		[DataSourceProperty]
		public float MinRange
		{
			get
			{
				return this._minRange;
			}
			set
			{
				if (value != this._minRange)
				{
					this._minRange = value;
					base.OnPropertyChangedWithValue(value, "MinRange");
				}
			}
		}

		// Token: 0x17000029 RID: 41
		// (get) Token: 0x06000089 RID: 137 RVA: 0x00003771 File Offset: 0x00001971
		// (set) Token: 0x0600008A RID: 138 RVA: 0x00003779 File Offset: 0x00001979
		[DataSourceProperty]
		public float MaxRange
		{
			get
			{
				return this._maxRange;
			}
			set
			{
				if (value != this._maxRange)
				{
					this._maxRange = value;
					base.OnPropertyChangedWithValue(value, "MaxRange");
				}
			}
		}

		// Token: 0x1700002A RID: 42
		// (get) Token: 0x0600008B RID: 139 RVA: 0x00003797 File Offset: 0x00001997
		// (set) Token: 0x0600008C RID: 140 RVA: 0x000037A0 File Offset: 0x000019A0
		[DataSourceProperty]
		public float ValueAsRange
		{
			get
			{
				return this._valueAsRange;
			}
			set
			{
				if (value != this._valueAsRange)
				{
					this._valueAsRange = value;
					base.OnPropertyChangedWithValue(value, "ValueAsRange");
					this.ValueAsString = value.ToString("F1");
					this._optionDataAsNumeric.SetValue(value);
					Action<CampaignOptionItemVM> onValueChanged = this._onValueChanged;
					if (onValueChanged == null)
					{
						return;
					}
					onValueChanged(this);
				}
			}
		}

		// Token: 0x1700002B RID: 43
		// (get) Token: 0x0600008D RID: 141 RVA: 0x000037F8 File Offset: 0x000019F8
		// (set) Token: 0x0600008E RID: 142 RVA: 0x00003800 File Offset: 0x00001A00
		[DataSourceProperty]
		public string ValueAsString
		{
			get
			{
				return this._valueAsString;
			}
			set
			{
				if (value != this._valueAsString)
				{
					this._valueAsString = value;
					base.OnPropertyChangedWithValue<string>(value, "ValueAsString");
				}
			}
		}

		// Token: 0x1700002C RID: 44
		// (get) Token: 0x0600008F RID: 143 RVA: 0x00003823 File Offset: 0x00001A23
		// (set) Token: 0x06000090 RID: 144 RVA: 0x0000382B File Offset: 0x00001A2B
		[DataSourceProperty]
		public CampaignOptionSelectorVM SelectionSelector
		{
			get
			{
				return this._selectionSelector;
			}
			set
			{
				if (value != this._selectionSelector)
				{
					this._selectionSelector = value;
					base.OnPropertyChangedWithValue<CampaignOptionSelectorVM>(value, "SelectionSelector");
				}
			}
		}

		// Token: 0x04000036 RID: 54
		private ActionCampaignOptionData _optionDataAsAction;

		// Token: 0x04000037 RID: 55
		private BooleanCampaignOptionData _optionDataAsBoolean;

		// Token: 0x04000038 RID: 56
		private NumericCampaignOptionData _optionDataAsNumeric;

		// Token: 0x04000039 RID: 57
		private SelectionCampaignOptionData _optionDataAsSelection;

		// Token: 0x0400003A RID: 58
		private Action<CampaignOptionItemVM> _onValueChanged;

		// Token: 0x0400003B RID: 59
		private CampaignOptionDataType _dataType;

		// Token: 0x0400003C RID: 60
		private bool _hideOptionName;

		// Token: 0x0400003D RID: 61
		private int _optionType;

		// Token: 0x0400003E RID: 62
		private string _name;

		// Token: 0x0400003F RID: 63
		private HintViewModel _hint;

		// Token: 0x04000040 RID: 64
		private bool _isDiscrete;

		// Token: 0x04000041 RID: 65
		private bool _isDisabled;

		// Token: 0x04000042 RID: 66
		private float _minRange;

		// Token: 0x04000043 RID: 67
		private float _maxRange;

		// Token: 0x04000044 RID: 68
		private bool _valueAsBoolean;

		// Token: 0x04000045 RID: 69
		private float _valueAsRange;

		// Token: 0x04000046 RID: 70
		private string _valueAsString;

		// Token: 0x04000047 RID: 71
		private CampaignOptionSelectorVM _selectionSelector;
	}
}
