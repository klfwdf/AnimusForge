using System;
using Helpers;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.TownManagement
{
	// Token: 0x020000A7 RID: 167
	public abstract class SettlementProjectVM : ViewModel
	{
		// Token: 0x1700052B RID: 1323
		// (get) Token: 0x06001000 RID: 4096 RVA: 0x00041A27 File Offset: 0x0003FC27
		// (set) Token: 0x06001001 RID: 4097 RVA: 0x00041A2F File Offset: 0x0003FC2F
		public bool IsDaily { get; protected set; }

		// Token: 0x1700052C RID: 1324
		// (get) Token: 0x06001002 RID: 4098 RVA: 0x00041A38 File Offset: 0x0003FC38
		// (set) Token: 0x06001003 RID: 4099 RVA: 0x00041A40 File Offset: 0x0003FC40
		public Building Building
		{
			get
			{
				return this._building;
			}
			set
			{
				this._building = value;
				this.Name = ((value != null) ? value.Name.ToString() : "");
				this.Explanation = ((value != null) ? value.Explanation.ToString() : "");
				this.VisualCode = ((value != null) ? value.BuildingType.StringId.ToLower() : "");
				int constructionCost = this.Building.GetConstructionCost();
				TextObject textObject;
				if (constructionCost > 0)
				{
					textObject = new TextObject("{=tAwRIPiy}Construction Cost: {COST}", null);
					textObject.SetTextVariable("COST", constructionCost);
				}
				else
				{
					textObject = TextObject.GetEmpty();
				}
				this.ProductionCostText = ((value != null) ? textObject.ToString() : "");
				this.CurrentPositiveEffectText = ((value != null) ? value.GetBonusExplanation().ToString() : "");
			}
		}

		// Token: 0x06001004 RID: 4100 RVA: 0x00041B0C File Offset: 0x0003FD0C
		protected SettlementProjectVM(Action<SettlementProjectVM, bool> onSelection, Action<SettlementProjectVM> onSetAsCurrent, Action onResetCurrent, Building building, Settlement settlement)
		{
			this._onSelection = onSelection;
			this._onSetAsCurrent = onSetAsCurrent;
			this._onResetCurrent = onResetCurrent;
			this.Building = building;
			this._settlement = settlement;
			this.Progress = (int)(BuildingHelper.GetProgressOfBuilding(building, this._settlement.Town) * 100f);
			this.RefreshValues();
		}

		// Token: 0x06001005 RID: 4101 RVA: 0x00041B9C File Offset: 0x0003FD9C
		public override void RefreshValues()
		{
			base.RefreshValues();
			if (this.Building.BuildingType.IsDailyProject)
			{
				this.CurrentPositiveEffectText = this.Building.BuildingType.GetExplanationAtLevel(this.Building.CurrentLevel).ToString();
				this.NextPositiveEffectText = "";
				return;
			}
			this.CurrentPositiveEffectText = this.GetBonusText(this.Building, this.Building.CurrentLevel);
			this.NextPositiveEffectText = this.GetBonusText(this.Building, this.Building.CurrentLevel + 1);
		}

		// Token: 0x06001006 RID: 4102 RVA: 0x00041C30 File Offset: 0x0003FE30
		private string GetBonusText(Building building, int level)
		{
			if (level == 0 || level == 4)
			{
				return "";
			}
			object obj = ((level == 1) ? this.L1BonusText : ((level == 2) ? this.L2BonusText : this.L3BonusText));
			TextObject bonusExplanationOfLevel = this.GetBonusExplanationOfLevel(level);
			object obj2 = obj;
			obj2.SetTextVariable("BONUS", bonusExplanationOfLevel);
			return obj2.ToString();
		}

		// Token: 0x06001007 RID: 4103 RVA: 0x00041C82 File Offset: 0x0003FE82
		private void ExecuteShowTooltip()
		{
			InformationManager.ShowTooltip(typeof(Building), new object[] { this._building });
		}

		// Token: 0x06001008 RID: 4104 RVA: 0x00041CA2 File Offset: 0x0003FEA2
		private void ExecuteHideTooltip()
		{
			MBInformationManager.HideInformations();
		}

		// Token: 0x06001009 RID: 4105 RVA: 0x00041CA9 File Offset: 0x0003FEA9
		private TextObject GetBonusExplanationOfLevel(int level)
		{
			if (level >= 0 && level <= 3)
			{
				return this.Building.BuildingType.GetExplanationAtLevel(level);
			}
			return TextObject.GetEmpty();
		}

		// Token: 0x0600100A RID: 4106 RVA: 0x00041CCA File Offset: 0x0003FECA
		public virtual void RefreshProductionText()
		{
		}

		// Token: 0x0600100B RID: 4107
		public abstract void ExecuteAddToQueue();

		// Token: 0x0600100C RID: 4108
		public abstract void ExecuteSetAsActiveDevelopment();

		// Token: 0x0600100D RID: 4109
		public abstract void ExecuteSetAsCurrent();

		// Token: 0x0600100E RID: 4110
		public abstract void ExecuteResetCurrent();

		// Token: 0x0600100F RID: 4111
		public abstract void ExecuteToggleSelected();

		// Token: 0x1700052D RID: 1325
		// (get) Token: 0x06001010 RID: 4112 RVA: 0x00041CCC File Offset: 0x0003FECC
		// (set) Token: 0x06001011 RID: 4113 RVA: 0x00041CD4 File Offset: 0x0003FED4
		[DataSourceProperty]
		public string VisualCode
		{
			get
			{
				return this._visualCode;
			}
			set
			{
				if (value != this._visualCode)
				{
					this._visualCode = value;
					base.OnPropertyChangedWithValue<string>(value, "VisualCode");
				}
			}
		}

		// Token: 0x1700052E RID: 1326
		// (get) Token: 0x06001012 RID: 4114 RVA: 0x00041CF7 File Offset: 0x0003FEF7
		// (set) Token: 0x06001013 RID: 4115 RVA: 0x00041CFF File Offset: 0x0003FEFF
		[DataSourceProperty]
		public string ProductionText
		{
			get
			{
				return this._productionText;
			}
			set
			{
				if (value != this._productionText)
				{
					this._productionText = value;
					base.OnPropertyChangedWithValue<string>(value, "ProductionText");
				}
			}
		}

		// Token: 0x1700052F RID: 1327
		// (get) Token: 0x06001014 RID: 4116 RVA: 0x00041D22 File Offset: 0x0003FF22
		// (set) Token: 0x06001015 RID: 4117 RVA: 0x00041D2A File Offset: 0x0003FF2A
		[DataSourceProperty]
		public string CurrentPositiveEffectText
		{
			get
			{
				return this._currentPositiveEffectText;
			}
			set
			{
				if (value != this._currentPositiveEffectText)
				{
					this._currentPositiveEffectText = value;
					base.OnPropertyChangedWithValue<string>(value, "CurrentPositiveEffectText");
				}
			}
		}

		// Token: 0x17000530 RID: 1328
		// (get) Token: 0x06001016 RID: 4118 RVA: 0x00041D4D File Offset: 0x0003FF4D
		// (set) Token: 0x06001017 RID: 4119 RVA: 0x00041D55 File Offset: 0x0003FF55
		[DataSourceProperty]
		public string NextPositiveEffectText
		{
			get
			{
				return this._nextPositiveEffectText;
			}
			set
			{
				if (value != this._nextPositiveEffectText)
				{
					this._nextPositiveEffectText = value;
					base.OnPropertyChangedWithValue<string>(value, "NextPositiveEffectText");
				}
			}
		}

		// Token: 0x17000531 RID: 1329
		// (get) Token: 0x06001018 RID: 4120 RVA: 0x00041D78 File Offset: 0x0003FF78
		// (set) Token: 0x06001019 RID: 4121 RVA: 0x00041D80 File Offset: 0x0003FF80
		[DataSourceProperty]
		public string ProductionCostText
		{
			get
			{
				return this._productionCostText;
			}
			set
			{
				if (value != this._productionCostText)
				{
					this._productionCostText = value;
					base.OnPropertyChangedWithValue<string>(value, "ProductionCostText");
				}
			}
		}

		// Token: 0x17000532 RID: 1330
		// (get) Token: 0x0600101A RID: 4122 RVA: 0x00041DA3 File Offset: 0x0003FFA3
		// (set) Token: 0x0600101B RID: 4123 RVA: 0x00041DAB File Offset: 0x0003FFAB
		[DataSourceProperty]
		public bool IsCurrentActiveProject
		{
			get
			{
				return this._isCurrentActiveProject;
			}
			set
			{
				if (value != this._isCurrentActiveProject)
				{
					this._isCurrentActiveProject = value;
					base.OnPropertyChangedWithValue(value, "IsCurrentActiveProject");
				}
			}
		}

		// Token: 0x17000533 RID: 1331
		// (get) Token: 0x0600101C RID: 4124 RVA: 0x00041DC9 File Offset: 0x0003FFC9
		// (set) Token: 0x0600101D RID: 4125 RVA: 0x00041DD1 File Offset: 0x0003FFD1
		[DataSourceProperty]
		public int Progress
		{
			get
			{
				return this._progress;
			}
			set
			{
				if (value != this._progress)
				{
					this._progress = value;
					base.OnPropertyChangedWithValue(value, "Progress");
				}
			}
		}

		// Token: 0x17000534 RID: 1332
		// (get) Token: 0x0600101E RID: 4126 RVA: 0x00041DEF File Offset: 0x0003FFEF
		// (set) Token: 0x0600101F RID: 4127 RVA: 0x00041DF7 File Offset: 0x0003FFF7
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

		// Token: 0x17000535 RID: 1333
		// (get) Token: 0x06001020 RID: 4128 RVA: 0x00041E1A File Offset: 0x0004001A
		// (set) Token: 0x06001021 RID: 4129 RVA: 0x00041E22 File Offset: 0x00040022
		[DataSourceProperty]
		public string Explanation
		{
			get
			{
				return this._explanation;
			}
			set
			{
				if (value != this._explanation)
				{
					this._explanation = value;
					base.OnPropertyChangedWithValue<string>(value, "Explanation");
				}
			}
		}

		// Token: 0x0400074B RID: 1867
		public int Index;

		// Token: 0x0400074D RID: 1869
		private Building _building;

		// Token: 0x0400074E RID: 1870
		protected Action<SettlementProjectVM, bool> _onSelection;

		// Token: 0x0400074F RID: 1871
		protected Action<SettlementProjectVM> _onSetAsCurrent;

		// Token: 0x04000750 RID: 1872
		protected Action _onResetCurrent;

		// Token: 0x04000751 RID: 1873
		protected Settlement _settlement;

		// Token: 0x04000752 RID: 1874
		private readonly TextObject L1BonusText = new TextObject("{=PJZ8QYgA}L-I : {BONUS}", null);

		// Token: 0x04000753 RID: 1875
		private readonly TextObject L2BonusText = new TextObject("{=9i0wnjJK}L-II : {BONUS}", null);

		// Token: 0x04000754 RID: 1876
		private readonly TextObject L3BonusText = new TextObject("{=pRP2sOWP}L-III : {BONUS}", null);

		// Token: 0x04000755 RID: 1877
		private string _name;

		// Token: 0x04000756 RID: 1878
		private string _visualCode;

		// Token: 0x04000757 RID: 1879
		private string _explanation;

		// Token: 0x04000758 RID: 1880
		private string _currentPositiveEffectText;

		// Token: 0x04000759 RID: 1881
		private string _nextPositiveEffectText;

		// Token: 0x0400075A RID: 1882
		private string _productionCostText;

		// Token: 0x0400075B RID: 1883
		private int _progress;

		// Token: 0x0400075C RID: 1884
		private bool _isCurrentActiveProject;

		// Token: 0x0400075D RID: 1885
		private string _productionText;
	}
}
