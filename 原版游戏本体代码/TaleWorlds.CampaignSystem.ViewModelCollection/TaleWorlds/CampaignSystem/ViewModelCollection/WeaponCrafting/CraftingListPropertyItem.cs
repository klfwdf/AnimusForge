using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting
{
	// Token: 0x020000FA RID: 250
	public class CraftingListPropertyItem : ViewModel
	{
		// Token: 0x0600166E RID: 5742 RVA: 0x00056F4C File Offset: 0x0005514C
		public CraftingListPropertyItem(TextObject description, float maxValue, float value, float targetValue, CraftingTemplate.CraftingStatTypes propertyType, bool isAlternativeUsageProperty = false)
		{
			this.Description = description;
			this.PropertyMaxValue = maxValue;
			this.PropertyValue = value;
			this.TargetValue = targetValue;
			this.IsAlternativeUsageProperty = isAlternativeUsageProperty;
			this.Type = propertyType;
			this.RefreshValues();
		}

		// Token: 0x0600166F RID: 5743 RVA: 0x00056FA0 File Offset: 0x000551A0
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.HasValidTarget = this.TargetValue > float.Epsilon;
			this.HasValidValue = this.PropertyValue > float.Epsilon;
			TextObject description = this.Description;
			this.PropertyLbl = ((description != null) ? description.ToString() : null);
			this.IsExceedingBeneficial = this.CheckIfExceedingIsBeneficial();
			this.SeparatorText = new TextObject("{=dB6cFDmz}/", null).ToString();
			this.PropertyValueText = CampaignUIHelper.GetFormattedItemPropertyText(this.PropertyValue, this.GetIsTypeRequireInteger(this.Type));
			if (this.HasValidTarget)
			{
				this.TargetValueText = CampaignUIHelper.GetFormattedItemPropertyText(this.TargetValue, this.GetIsTypeRequireInteger(this.Type));
			}
		}

		// Token: 0x06001670 RID: 5744 RVA: 0x00057055 File Offset: 0x00055255
		private bool CheckIfExceedingIsBeneficial()
		{
			return this.Type > CraftingTemplate.CraftingStatTypes.Weight;
		}

		// Token: 0x06001671 RID: 5745 RVA: 0x00057060 File Offset: 0x00055260
		private bool GetIsTypeRequireInteger(CraftingTemplate.CraftingStatTypes type)
		{
			return type == CraftingTemplate.CraftingStatTypes.StackAmount;
		}

		// Token: 0x17000772 RID: 1906
		// (get) Token: 0x06001672 RID: 5746 RVA: 0x00057067 File Offset: 0x00055267
		// (set) Token: 0x06001673 RID: 5747 RVA: 0x0005706F File Offset: 0x0005526F
		[DataSourceProperty]
		public bool IsValidForUsage
		{
			get
			{
				return this._showStats;
			}
			set
			{
				if (value != this._showStats)
				{
					this._showStats = value;
					base.OnPropertyChangedWithValue(value, "IsValidForUsage");
				}
			}
		}

		// Token: 0x17000773 RID: 1907
		// (get) Token: 0x06001674 RID: 5748 RVA: 0x0005708D File Offset: 0x0005528D
		// (set) Token: 0x06001675 RID: 5749 RVA: 0x00057095 File Offset: 0x00055295
		[DataSourceProperty]
		public bool IsExceedingBeneficial
		{
			get
			{
				return this._isExceedingBeneficial;
			}
			set
			{
				if (value != this._isExceedingBeneficial)
				{
					this._isExceedingBeneficial = value;
					base.OnPropertyChangedWithValue(value, "IsExceedingBeneficial");
				}
			}
		}

		// Token: 0x17000774 RID: 1908
		// (get) Token: 0x06001676 RID: 5750 RVA: 0x000570B3 File Offset: 0x000552B3
		// (set) Token: 0x06001677 RID: 5751 RVA: 0x000570BB File Offset: 0x000552BB
		[DataSourceProperty]
		public bool HasValidTarget
		{
			get
			{
				return this._hasValidTarget;
			}
			set
			{
				if (value != this._hasValidTarget)
				{
					this._hasValidTarget = value;
					base.OnPropertyChangedWithValue(value, "HasValidTarget");
				}
			}
		}

		// Token: 0x17000775 RID: 1909
		// (get) Token: 0x06001678 RID: 5752 RVA: 0x000570D9 File Offset: 0x000552D9
		// (set) Token: 0x06001679 RID: 5753 RVA: 0x000570E1 File Offset: 0x000552E1
		[DataSourceProperty]
		public bool HasValidValue
		{
			get
			{
				return this._hasValidValue;
			}
			set
			{
				if (value != this._hasValidValue)
				{
					this._hasValidValue = value;
					base.OnPropertyChangedWithValue(value, "HasValidValue");
				}
			}
		}

		// Token: 0x17000776 RID: 1910
		// (get) Token: 0x0600167A RID: 5754 RVA: 0x000570FF File Offset: 0x000552FF
		// (set) Token: 0x0600167B RID: 5755 RVA: 0x00057107 File Offset: 0x00055307
		[DataSourceProperty]
		public float TargetValue
		{
			get
			{
				return this._targetValue;
			}
			set
			{
				if (value != this._targetValue)
				{
					this._targetValue = value;
					base.OnPropertyChangedWithValue(value, "TargetValue");
				}
			}
		}

		// Token: 0x17000777 RID: 1911
		// (get) Token: 0x0600167C RID: 5756 RVA: 0x00057125 File Offset: 0x00055325
		// (set) Token: 0x0600167D RID: 5757 RVA: 0x0005712D File Offset: 0x0005532D
		[DataSourceProperty]
		public string TargetValueText
		{
			get
			{
				return this._targetValueText;
			}
			set
			{
				if (value != this._targetValueText)
				{
					this._targetValueText = value;
					base.OnPropertyChangedWithValue<string>(value, "TargetValueText");
				}
			}
		}

		// Token: 0x17000778 RID: 1912
		// (get) Token: 0x0600167E RID: 5758 RVA: 0x00057150 File Offset: 0x00055350
		// (set) Token: 0x0600167F RID: 5759 RVA: 0x00057158 File Offset: 0x00055358
		[DataSourceProperty]
		public bool IsAlternativeUsageProperty
		{
			get
			{
				return this._isAlternativeUsageProperty;
			}
			set
			{
				if (this._isAlternativeUsageProperty != value)
				{
					this._isAlternativeUsageProperty = value;
					base.OnPropertyChangedWithValue(value, "IsAlternativeUsageProperty");
				}
			}
		}

		// Token: 0x17000779 RID: 1913
		// (get) Token: 0x06001680 RID: 5760 RVA: 0x00057176 File Offset: 0x00055376
		// (set) Token: 0x06001681 RID: 5761 RVA: 0x0005717E File Offset: 0x0005537E
		[DataSourceProperty]
		public string PropertyLbl
		{
			get
			{
				return this._propertyLbl;
			}
			set
			{
				if (value != this._propertyLbl)
				{
					this._propertyLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "PropertyLbl");
				}
			}
		}

		// Token: 0x1700077A RID: 1914
		// (get) Token: 0x06001682 RID: 5762 RVA: 0x000571A1 File Offset: 0x000553A1
		// (set) Token: 0x06001683 RID: 5763 RVA: 0x000571A9 File Offset: 0x000553A9
		[DataSourceProperty]
		public float PropertyValue
		{
			get
			{
				return this._propertyValue;
			}
			set
			{
				if (value == 0f || value != this._propertyValue)
				{
					this._propertyValue = value;
					base.OnPropertyChangedWithValue(value, "PropertyValue");
				}
			}
		}

		// Token: 0x1700077B RID: 1915
		// (get) Token: 0x06001684 RID: 5764 RVA: 0x000571CF File Offset: 0x000553CF
		// (set) Token: 0x06001685 RID: 5765 RVA: 0x000571D7 File Offset: 0x000553D7
		[DataSourceProperty]
		public float PropertyMaxValue
		{
			get
			{
				return this._propertyMaxValue;
			}
			set
			{
				if (value != this._propertyMaxValue)
				{
					this._propertyMaxValue = value;
					base.OnPropertyChangedWithValue(value, "PropertyMaxValue");
				}
			}
		}

		// Token: 0x1700077C RID: 1916
		// (get) Token: 0x06001686 RID: 5766 RVA: 0x000571F5 File Offset: 0x000553F5
		// (set) Token: 0x06001687 RID: 5767 RVA: 0x000571FD File Offset: 0x000553FD
		[DataSourceProperty]
		public string PropertyValueText
		{
			get
			{
				return this._propertyValueText;
			}
			set
			{
				if (this._propertyValueText != value)
				{
					this._propertyValueText = value;
					base.OnPropertyChangedWithValue<string>(value, "PropertyValueText");
				}
			}
		}

		// Token: 0x1700077D RID: 1917
		// (get) Token: 0x06001688 RID: 5768 RVA: 0x00057220 File Offset: 0x00055420
		// (set) Token: 0x06001689 RID: 5769 RVA: 0x00057228 File Offset: 0x00055428
		[DataSourceProperty]
		public string SeparatorText
		{
			get
			{
				return this._separatorText;
			}
			set
			{
				if (value != this._separatorText)
				{
					this._separatorText = value;
					base.OnPropertyChangedWithValue<string>(value, "SeparatorText");
				}
			}
		}

		// Token: 0x04000A43 RID: 2627
		public readonly TextObject Description;

		// Token: 0x04000A44 RID: 2628
		public readonly CraftingTemplate.CraftingStatTypes Type;

		// Token: 0x04000A45 RID: 2629
		private bool _showStats;

		// Token: 0x04000A46 RID: 2630
		private bool _isExceedingBeneficial;

		// Token: 0x04000A47 RID: 2631
		private bool _hasValidTarget;

		// Token: 0x04000A48 RID: 2632
		private bool _hasValidValue;

		// Token: 0x04000A49 RID: 2633
		private float _targetValue;

		// Token: 0x04000A4A RID: 2634
		private string _targetValueText;

		// Token: 0x04000A4B RID: 2635
		private string _propertyLbl;

		// Token: 0x04000A4C RID: 2636
		private float _propertyValue;

		// Token: 0x04000A4D RID: 2637
		private float _propertyMaxValue = -1f;

		// Token: 0x04000A4E RID: 2638
		private string _propertyValueText;

		// Token: 0x04000A4F RID: 2639
		public bool _isAlternativeUsageProperty;

		// Token: 0x04000A50 RID: 2640
		private string _separatorText;
	}
}
