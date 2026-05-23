using System;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign
{
	// Token: 0x0200010C RID: 268
	public class WeaponDesignResultPropertyItemVM : ViewModel
	{
		// Token: 0x060017CA RID: 6090 RVA: 0x0005A764 File Offset: 0x00058964
		public WeaponDesignResultPropertyItemVM(TextObject description, float value, float changeAmount, bool showFloatingPoint)
		{
			this._description = description;
			this.InitialValue = value;
			this.ChangeAmount = changeAmount;
			this.ShowFloatingPoint = showFloatingPoint;
			this.IsOrderResult = false;
			this.OrderRequirementTooltip = new HintViewModel();
			this.CraftedValueTooltip = new HintViewModel();
			this.BonusPenaltyTooltip = new HintViewModel();
			this.RefreshValues();
		}

		// Token: 0x060017CB RID: 6091 RVA: 0x0005A7C4 File Offset: 0x000589C4
		public WeaponDesignResultPropertyItemVM(TextObject description, float craftedValue, float requiredValue, float changeAmount, bool showFloatingPoint, bool isExceedingBeneficial, bool showTooltip = true)
		{
			this._showTooltip = showTooltip;
			this._description = description;
			this.TargetValue = requiredValue;
			this.InitialValue = craftedValue;
			this.ChangeAmount = changeAmount;
			this._isExceedingBeneficial = isExceedingBeneficial;
			this.IsOrderResult = true;
			this.ShowFloatingPoint = showFloatingPoint;
			this.OrderRequirementTooltip = new HintViewModel();
			this.CraftedValueTooltip = new HintViewModel();
			this.BonusPenaltyTooltip = new HintViewModel();
			this.RefreshValues();
		}

		// Token: 0x060017CC RID: 6092 RVA: 0x0005A83C File Offset: 0x00058A3C
		public override void RefreshValues()
		{
			base.RefreshValues();
			TextObject description = this._description;
			this.PropertyLbl = ((description != null) ? description.ToString() : null);
			TextObject textObject = GameTexts.FindText("str_STR_in_parentheses", null);
			textObject.SetTextVariable("STR", CampaignUIHelper.GetFormattedItemPropertyText(this.TargetValue, this.ShowFloatingPoint));
			this.RequiredValueText = ((this.TargetValue == 0f) ? string.Empty : textObject.ToString());
			this.HasBenefit = (this._isExceedingBeneficial ? (this.InitialValue + this.ChangeAmount >= this.TargetValue) : (this.InitialValue + this.ChangeAmount <= this.TargetValue));
			this.OrderRequirementTooltip.HintText = (this._showTooltip ? GameTexts.FindText("str_crafting_order_requirement_tooltip", null) : TextObject.GetEmpty());
			this.CraftedValueTooltip.HintText = (this._showTooltip ? GameTexts.FindText("str_crafting_crafted_value_tooltip", null) : TextObject.GetEmpty());
			this.BonusPenaltyTooltip.HintText = (this._showTooltip ? GameTexts.FindText("str_crafting_bonus_penalty_tooltip", null) : TextObject.GetEmpty());
		}

		// Token: 0x170007F4 RID: 2036
		// (get) Token: 0x060017CD RID: 6093 RVA: 0x0005A95F File Offset: 0x00058B5F
		// (set) Token: 0x060017CE RID: 6094 RVA: 0x0005A967 File Offset: 0x00058B67
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

		// Token: 0x170007F5 RID: 2037
		// (get) Token: 0x060017CF RID: 6095 RVA: 0x0005A98A File Offset: 0x00058B8A
		// (set) Token: 0x060017D0 RID: 6096 RVA: 0x0005A992 File Offset: 0x00058B92
		[DataSourceProperty]
		public float InitialValue
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
					base.OnPropertyChangedWithValue(value, "InitialValue");
				}
			}
		}

		// Token: 0x170007F6 RID: 2038
		// (get) Token: 0x060017D1 RID: 6097 RVA: 0x0005A9B8 File Offset: 0x00058BB8
		// (set) Token: 0x060017D2 RID: 6098 RVA: 0x0005A9C0 File Offset: 0x00058BC0
		[DataSourceProperty]
		public float TargetValue
		{
			get
			{
				return this._requiredValue;
			}
			set
			{
				if (value != this._requiredValue)
				{
					this._requiredValue = value;
					base.OnPropertyChangedWithValue(value, "TargetValue");
				}
			}
		}

		// Token: 0x170007F7 RID: 2039
		// (get) Token: 0x060017D3 RID: 6099 RVA: 0x0005A9DE File Offset: 0x00058BDE
		// (set) Token: 0x060017D4 RID: 6100 RVA: 0x0005A9E6 File Offset: 0x00058BE6
		[DataSourceProperty]
		public string RequiredValueText
		{
			get
			{
				return this._requiredValueText;
			}
			set
			{
				if (value != this._requiredValueText)
				{
					this._requiredValueText = value;
					base.OnPropertyChangedWithValue<string>(value, "RequiredValueText");
				}
			}
		}

		// Token: 0x170007F8 RID: 2040
		// (get) Token: 0x060017D5 RID: 6101 RVA: 0x0005AA09 File Offset: 0x00058C09
		// (set) Token: 0x060017D6 RID: 6102 RVA: 0x0005AA11 File Offset: 0x00058C11
		[DataSourceProperty]
		public float ChangeAmount
		{
			get
			{
				return this._changeAmount;
			}
			set
			{
				if (this._changeAmount != value)
				{
					this._changeAmount = value;
					base.OnPropertyChangedWithValue(value, "ChangeAmount");
				}
			}
		}

		// Token: 0x170007F9 RID: 2041
		// (get) Token: 0x060017D7 RID: 6103 RVA: 0x0005AA2F File Offset: 0x00058C2F
		// (set) Token: 0x060017D8 RID: 6104 RVA: 0x0005AA37 File Offset: 0x00058C37
		[DataSourceProperty]
		public bool ShowFloatingPoint
		{
			get
			{
				return this._showFloatingPoint;
			}
			set
			{
				if (this._showFloatingPoint != value)
				{
					this._showFloatingPoint = value;
					base.OnPropertyChangedWithValue(value, "ShowFloatingPoint");
				}
			}
		}

		// Token: 0x170007FA RID: 2042
		// (get) Token: 0x060017D9 RID: 6105 RVA: 0x0005AA55 File Offset: 0x00058C55
		// (set) Token: 0x060017DA RID: 6106 RVA: 0x0005AA5D File Offset: 0x00058C5D
		[DataSourceProperty]
		public bool IsOrderResult
		{
			get
			{
				return this._isOrderResult;
			}
			set
			{
				if (value != this._isOrderResult)
				{
					this._isOrderResult = value;
					base.OnPropertyChangedWithValue(value, "IsOrderResult");
				}
			}
		}

		// Token: 0x170007FB RID: 2043
		// (get) Token: 0x060017DB RID: 6107 RVA: 0x0005AA7B File Offset: 0x00058C7B
		// (set) Token: 0x060017DC RID: 6108 RVA: 0x0005AA83 File Offset: 0x00058C83
		[DataSourceProperty]
		public bool HasBenefit
		{
			get
			{
				return this._hasBenefit;
			}
			set
			{
				if (value != this._hasBenefit)
				{
					this._hasBenefit = value;
					base.OnPropertyChangedWithValue(value, "HasBenefit");
				}
			}
		}

		// Token: 0x170007FC RID: 2044
		// (get) Token: 0x060017DD RID: 6109 RVA: 0x0005AAA1 File Offset: 0x00058CA1
		// (set) Token: 0x060017DE RID: 6110 RVA: 0x0005AAA9 File Offset: 0x00058CA9
		[DataSourceProperty]
		public HintViewModel OrderRequirementTooltip
		{
			get
			{
				return this._orderRequirementTooltip;
			}
			set
			{
				if (value != this._orderRequirementTooltip)
				{
					this._orderRequirementTooltip = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "OrderRequirementTooltip");
				}
			}
		}

		// Token: 0x170007FD RID: 2045
		// (get) Token: 0x060017DF RID: 6111 RVA: 0x0005AAC7 File Offset: 0x00058CC7
		// (set) Token: 0x060017E0 RID: 6112 RVA: 0x0005AACF File Offset: 0x00058CCF
		[DataSourceProperty]
		public HintViewModel CraftedValueTooltip
		{
			get
			{
				return this._craftedValueTooltip;
			}
			set
			{
				if (value != this._craftedValueTooltip)
				{
					this._craftedValueTooltip = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "CraftedValueTooltip");
				}
			}
		}

		// Token: 0x170007FE RID: 2046
		// (get) Token: 0x060017E1 RID: 6113 RVA: 0x0005AAED File Offset: 0x00058CED
		// (set) Token: 0x060017E2 RID: 6114 RVA: 0x0005AAF5 File Offset: 0x00058CF5
		[DataSourceProperty]
		public HintViewModel BonusPenaltyTooltip
		{
			get
			{
				return this._bonusPenaltyTooltip;
			}
			set
			{
				if (value != this._bonusPenaltyTooltip)
				{
					this._bonusPenaltyTooltip = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "BonusPenaltyTooltip");
				}
			}
		}

		// Token: 0x04000AEB RID: 2795
		private readonly TextObject _description;

		// Token: 0x04000AEC RID: 2796
		private bool _isExceedingBeneficial;

		// Token: 0x04000AED RID: 2797
		private bool _showTooltip;

		// Token: 0x04000AEE RID: 2798
		private string _propertyLbl;

		// Token: 0x04000AEF RID: 2799
		private float _propertyValue;

		// Token: 0x04000AF0 RID: 2800
		private float _requiredValue;

		// Token: 0x04000AF1 RID: 2801
		private string _requiredValueText;

		// Token: 0x04000AF2 RID: 2802
		private float _changeAmount;

		// Token: 0x04000AF3 RID: 2803
		private bool _showFloatingPoint;

		// Token: 0x04000AF4 RID: 2804
		private bool _isOrderResult;

		// Token: 0x04000AF5 RID: 2805
		private bool _hasBenefit;

		// Token: 0x04000AF6 RID: 2806
		private HintViewModel _orderRequirementTooltip;

		// Token: 0x04000AF7 RID: 2807
		private HintViewModel _craftedValueTooltip;

		// Token: 0x04000AF8 RID: 2808
		private HintViewModel _bonusPenaltyTooltip;
	}
}
