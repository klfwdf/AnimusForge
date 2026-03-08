using System;
using Helpers;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.TownManagement
{
	// Token: 0x020000A9 RID: 169
	public class TownManagementReserveControlVM : ViewModel
	{
		// Token: 0x06001031 RID: 4145 RVA: 0x00042010 File Offset: 0x00040210
		public TownManagementReserveControlVM(Settlement settlement, Action onReserveUpdated)
		{
			this._settlement = settlement;
			this._onReserveUpdated = onReserveUpdated;
			if (((settlement != null) ? settlement.Town : null) != null)
			{
				this.CurrentReserveAmount = Settlement.CurrentSettlement.Town.BoostBuildingProcess;
				this.CurrentGivenAmount = 0;
				this.MaxReserveAmount = MathF.Min(Hero.MainHero.Gold, 10000);
			}
			this.RefreshValues();
		}

		// Token: 0x06001032 RID: 4146 RVA: 0x0004207C File Offset: 0x0004027C
		public override void RefreshValues()
		{
			base.RefreshValues();
			Settlement settlement = this._settlement;
			if (((settlement != null) ? settlement.Town : null) != null)
			{
				this.ReserveText = new TextObject("{=2ckyCKR7}Reserve", null).ToString();
				GameTexts.SetVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
				this.UpdateReserveText();
			}
		}

		// Token: 0x06001033 RID: 4147 RVA: 0x000420D0 File Offset: 0x000402D0
		private void UpdateReserveText()
		{
			TextObject textObject = GameTexts.FindText("str_town_management_reserve_explanation", null);
			textObject.SetTextVariable("BOOST", Campaign.Current.Models.BuildingConstructionModel.GetBoostAmount(this._settlement.Town));
			textObject.SetTextVariable("COST", Campaign.Current.Models.BuildingConstructionModel.GetBoostCost(this._settlement.Town));
			this.ReserveBonusText = textObject.ToString();
		}

		// Token: 0x06001034 RID: 4148 RVA: 0x0004214C File Offset: 0x0004034C
		public void ExecuteConfirm()
		{
			this.IsEnabled = false;
			BuildingHelper.BoostBuildingProcessWithGold(this.CurrentReserveAmount + this.CurrentGivenAmount, Settlement.CurrentSettlement.Town);
			this.CurrentGivenAmount = 0;
			GameTexts.SetVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
			this.UpdateReserveText();
			this.MaxReserveAmount = MathF.Min(Hero.MainHero.Gold, 10000);
			this.CurrentReserveAmount = Settlement.CurrentSettlement.Town.BoostBuildingProcess;
			Action onReserveUpdated = this._onReserveUpdated;
			if (onReserveUpdated == null)
			{
				return;
			}
			onReserveUpdated();
		}

		// Token: 0x06001035 RID: 4149 RVA: 0x000421D7 File Offset: 0x000403D7
		public void ExecuteCancel()
		{
			this.IsEnabled = false;
		}

		// Token: 0x1700053C RID: 1340
		// (get) Token: 0x06001036 RID: 4150 RVA: 0x000421E0 File Offset: 0x000403E0
		// (set) Token: 0x06001037 RID: 4151 RVA: 0x000421E8 File Offset: 0x000403E8
		[DataSourceProperty]
		public bool IsEnabled
		{
			get
			{
				return this._isEnabled;
			}
			set
			{
				if (value != this._isEnabled)
				{
					this._isEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsEnabled");
				}
			}
		}

		// Token: 0x1700053D RID: 1341
		// (get) Token: 0x06001038 RID: 4152 RVA: 0x00042206 File Offset: 0x00040406
		// (set) Token: 0x06001039 RID: 4153 RVA: 0x00042210 File Offset: 0x00040410
		[DataSourceProperty]
		public int CurrentReserveAmount
		{
			get
			{
				return this._currentReserveAmount;
			}
			set
			{
				if (value != this._currentReserveAmount)
				{
					this._currentReserveAmount = value;
					base.OnPropertyChangedWithValue(value, "CurrentReserveAmount");
					this.CurrentReserveText = (this.CurrentGivenAmount + value).ToString();
				}
			}
		}

		// Token: 0x1700053E RID: 1342
		// (get) Token: 0x0600103A RID: 4154 RVA: 0x0004224F File Offset: 0x0004044F
		// (set) Token: 0x0600103B RID: 4155 RVA: 0x00042257 File Offset: 0x00040457
		[DataSourceProperty]
		public int CurrentGivenAmount
		{
			get
			{
				return this._currentGivenAmount;
			}
			set
			{
				if (value != this._currentGivenAmount)
				{
					this._currentGivenAmount = value;
					base.OnPropertyChangedWithValue(value, "CurrentGivenAmount");
				}
			}
		}

		// Token: 0x1700053F RID: 1343
		// (get) Token: 0x0600103C RID: 4156 RVA: 0x00042275 File Offset: 0x00040475
		// (set) Token: 0x0600103D RID: 4157 RVA: 0x0004227D File Offset: 0x0004047D
		[DataSourceProperty]
		public int MaxReserveAmount
		{
			get
			{
				return this._maxReserveAmount;
			}
			set
			{
				if (value != this._maxReserveAmount)
				{
					this._maxReserveAmount = value;
					base.OnPropertyChangedWithValue(value, "MaxReserveAmount");
				}
			}
		}

		// Token: 0x17000540 RID: 1344
		// (get) Token: 0x0600103E RID: 4158 RVA: 0x0004229B File Offset: 0x0004049B
		// (set) Token: 0x0600103F RID: 4159 RVA: 0x000422A3 File Offset: 0x000404A3
		[DataSourceProperty]
		public string ReserveBonusText
		{
			get
			{
				return this._reserveBonusText;
			}
			set
			{
				if (value != this._reserveBonusText)
				{
					this._reserveBonusText = value;
					base.OnPropertyChangedWithValue<string>(value, "ReserveBonusText");
				}
			}
		}

		// Token: 0x17000541 RID: 1345
		// (get) Token: 0x06001040 RID: 4160 RVA: 0x000422C6 File Offset: 0x000404C6
		// (set) Token: 0x06001041 RID: 4161 RVA: 0x000422CE File Offset: 0x000404CE
		[DataSourceProperty]
		public string ReserveText
		{
			get
			{
				return this._reserveText;
			}
			set
			{
				if (value != this._reserveText)
				{
					this._reserveText = value;
					base.OnPropertyChangedWithValue<string>(value, "ReserveText");
				}
			}
		}

		// Token: 0x17000542 RID: 1346
		// (get) Token: 0x06001042 RID: 4162 RVA: 0x000422F1 File Offset: 0x000404F1
		// (set) Token: 0x06001043 RID: 4163 RVA: 0x000422F9 File Offset: 0x000404F9
		[DataSourceProperty]
		public string CurrentReserveText
		{
			get
			{
				return this._currentReserveText;
			}
			set
			{
				if (value != this._currentReserveText)
				{
					this._currentReserveText = value;
					base.OnPropertyChangedWithValue<string>(value, "CurrentReserveText");
				}
			}
		}

		// Token: 0x04000765 RID: 1893
		private readonly Action _onReserveUpdated;

		// Token: 0x04000766 RID: 1894
		private readonly Settlement _settlement;

		// Token: 0x04000767 RID: 1895
		private const int MaxOneTimeAmount = 10000;

		// Token: 0x04000768 RID: 1896
		private bool _isEnabled;

		// Token: 0x04000769 RID: 1897
		private string _reserveText;

		// Token: 0x0400076A RID: 1898
		private int _currentReserveAmount;

		// Token: 0x0400076B RID: 1899
		private int _currentGivenAmount;

		// Token: 0x0400076C RID: 1900
		private int _maxReserveAmount;

		// Token: 0x0400076D RID: 1901
		private string _reserveBonusText;

		// Token: 0x0400076E RID: 1902
		private string _currentReserveText;
	}
}
