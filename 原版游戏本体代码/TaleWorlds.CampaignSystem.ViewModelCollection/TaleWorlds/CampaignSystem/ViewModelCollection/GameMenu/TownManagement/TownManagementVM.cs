using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.TownManagement
{
	// Token: 0x020000AC RID: 172
	public class TownManagementVM : ViewModel
	{
		// Token: 0x0600105B RID: 4187 RVA: 0x000426AC File Offset: 0x000408AC
		public TownManagementVM()
		{
			this._settlement = Settlement.CurrentSettlement;
			Settlement settlement = this._settlement;
			if (((settlement != null) ? settlement.Town : null) == null)
			{
				Debug.FailedAssert("Town management initialized with null settlement and/or town!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\GameMenu\\TownManagement\\TownManagementVM.cs", ".ctor", 27);
				Debug.Print("Town management initialized with null settlement and/or town!", 0, Debug.DebugColor.White, 17592186044416UL);
			}
			this.ProjectSelection = new SettlementProjectSelectionVM(this._settlement, new Action(this.OnChangeInBuildingQueue));
			this.GovernorSelection = new SettlementGovernorSelectionVM(this._settlement, new Action<Hero>(this.OnGovernorSelectionDone));
			this.ReserveControl = new TownManagementReserveControlVM(this._settlement, new Action(this.OnReserveUpdated));
			this.MiddleFirstTextList = new MBBindingList<TownManagementDescriptionItemVM>();
			this.MiddleSecondTextList = new MBBindingList<TownManagementDescriptionItemVM>();
			this.Shops = new MBBindingList<TownManagementShopItemVM>();
			this.Villages = new MBBindingList<TownManagementVillageItemVM>();
			this.Show = false;
			this.IsTown = this._settlement.IsTown;
			this.IsThereCurrentProject = this._settlement.Town.CurrentBuilding != null;
			this.CurrentGovernor = new HeroVM(this._settlement.Town.Governor ?? CampaignUIHelper.GetTeleportingGovernor(this._settlement, Campaign.Current.GetCampaignBehavior<ITeleportationCampaignBehavior>()), true);
			if (this.CurrentGovernor.Hero != null)
			{
				this.CurrentGovernorTooltip = new BasicTooltipViewModel(() => CampaignUIHelper.GetHeroGovernorEffectsTooltip(this.CurrentGovernor.Hero, this._settlement));
			}
			this.UpdateGovernorSelectionProperties();
			this.RefreshCurrentDevelopment();
			this.RefreshTownManagementStats();
			foreach (Workshop workshop in this._settlement.Town.Workshops)
			{
				WorkshopType workshopType = workshop.WorkshopType;
				if (workshopType != null && !workshopType.IsHidden)
				{
					this.Shops.Add(new TownManagementShopItemVM(workshop));
				}
			}
			foreach (Village village in this._settlement.BoundVillages)
			{
				this.Villages.Add(new TownManagementVillageItemVM(village));
			}
			this.ConsumptionTooltip = new BasicTooltipViewModel(() => CampaignUIHelper.GetSettlementConsumptionTooltip(this._settlement));
			this.RefreshValues();
		}

		// Token: 0x0600105C RID: 4188 RVA: 0x000428E8 File Offset: 0x00040AE8
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.CurrentProjectText = new TextObject("{=qBq70qDq}Current Project", null).ToString();
			this.CompletionText = new TextObject("{=Rkh2k1OA}Completion:", null).ToString();
			this.ManageText = new TextObject("{=XseYJYka}Manage", null).ToString();
			this.DoneText = new TextObject("{=WiNRdfsm}Done", null).ToString();
			this.WallsText = new TextObject("{=LsZEdD2z}Walls", null).ToString();
			this.VillagesText = GameTexts.FindText("str_bound_village", null).ToString();
			this.ShopsInSettlementText = GameTexts.FindText("str_shops_in_settlement", null).ToString();
			this.GovernorText = GameTexts.FindText("str_sort_by_governor_label", null).ToString();
			this.MiddleFirstTextList.ApplyActionOnAllItems(delegate(TownManagementDescriptionItemVM x)
			{
				x.RefreshValues();
			});
			this.MiddleSecondTextList.ApplyActionOnAllItems(delegate(TownManagementDescriptionItemVM x)
			{
				x.RefreshValues();
			});
			this.ProjectSelection.RefreshValues();
			this.GovernorSelection.RefreshValues();
			this.ReserveControl.RefreshValues();
			this.Shops.ApplyActionOnAllItems(delegate(TownManagementShopItemVM x)
			{
				x.RefreshValues();
			});
			this.Villages.ApplyActionOnAllItems(delegate(TownManagementVillageItemVM x)
			{
				x.RefreshValues();
			});
			this.CurrentGovernor.RefreshValues();
		}

		// Token: 0x0600105D RID: 4189 RVA: 0x00042A80 File Offset: 0x00040C80
		private void RefreshTownManagementStats()
		{
			this.MiddleFirstTextList.Clear();
			this.MiddleSecondTextList.Clear();
			ExplainedNumber taxExplanation = Campaign.Current.Models.SettlementTaxModel.CalculateTownTax(this._settlement.Town, true);
			int taxValue = (int)taxExplanation.ResultNumber;
			BasicTooltipViewModel hint = new BasicTooltipViewModel(() => CampaignUIHelper.GetTooltipForAccumulatingPropertyWithResult(GameTexts.FindText("str_town_management_population_tax", null).ToString(), (float)taxValue, ref taxExplanation));
			GameTexts.SetVariable("LEFT", GameTexts.FindText("str_town_management_population_tax", null));
			this.MiddleFirstTextList.Add(new TownManagementDescriptionItemVM(GameTexts.FindText("str_LEFT_colon", null), taxValue, 0, TownManagementDescriptionItemVM.DescriptionType.Gold, hint));
			BasicTooltipViewModel hint2 = new BasicTooltipViewModel(() => CampaignUIHelper.GetTownDailyProductionTooltip(this._settlement.Town));
			this.MiddleFirstTextList.Add(new TownManagementDescriptionItemVM(GameTexts.FindText("str_town_management_daily_production", null), (int)Campaign.Current.Models.BuildingConstructionModel.CalculateDailyConstructionPower(this._settlement.Town, false).ResultNumber, 0, TownManagementDescriptionItemVM.DescriptionType.Production, hint2));
			BasicTooltipViewModel hint3 = new BasicTooltipViewModel(() => CampaignUIHelper.GetTownProsperityTooltip(this._settlement.Town));
			this.MiddleFirstTextList.Add(new TownManagementDescriptionItemVM(GameTexts.FindText("str_town_management_prosperity", null), (int)this._settlement.Town.Prosperity, MathF.Round(Campaign.Current.Models.SettlementProsperityModel.CalculateProsperityChange(this._settlement.Town, false).ResultNumber), TownManagementDescriptionItemVM.DescriptionType.Prosperity, hint3));
			BasicTooltipViewModel hint4 = new BasicTooltipViewModel(() => CampaignUIHelper.GetTownFoodTooltip(this._settlement.Town));
			this.MiddleFirstTextList.Add(new TownManagementDescriptionItemVM(GameTexts.FindText("str_town_management_food", null), (int)this._settlement.Town.FoodStocks, MathF.Round(Campaign.Current.Models.SettlementFoodModel.CalculateTownFoodStocksChange(this._settlement.Town, true, false).ResultNumber), TownManagementDescriptionItemVM.DescriptionType.Food, hint4));
			BasicTooltipViewModel hint5 = new BasicTooltipViewModel(() => CampaignUIHelper.GetTownLoyaltyTooltip(this._settlement.Town));
			this.MiddleSecondTextList.Add(new TownManagementDescriptionItemVM(GameTexts.FindText("str_town_management_loyalty", null), (int)this._settlement.Town.Loyalty, MathF.Round(Campaign.Current.Models.SettlementLoyaltyModel.CalculateLoyaltyChange(this._settlement.Town, false).ResultNumber), TownManagementDescriptionItemVM.DescriptionType.Loyalty, hint5));
			BasicTooltipViewModel hint6 = new BasicTooltipViewModel(() => CampaignUIHelper.GetTownSecurityTooltip(this._settlement.Town));
			this.MiddleSecondTextList.Add(new TownManagementDescriptionItemVM(GameTexts.FindText("str_town_management_security", null), (int)this._settlement.Town.Security, MathF.Round(Campaign.Current.Models.SettlementSecurityModel.CalculateSecurityChange(this._settlement.Town, false).ResultNumber), TownManagementDescriptionItemVM.DescriptionType.Security, hint6));
			BasicTooltipViewModel hint7 = new BasicTooltipViewModel(() => CampaignUIHelper.GetTownMilitiaTooltip(this._settlement.Town));
			this.MiddleSecondTextList.Add(new TownManagementDescriptionItemVM(GameTexts.FindText("str_town_management_militia", null), (int)this._settlement.Militia, MathF.Round(Campaign.Current.Models.SettlementMilitiaModel.CalculateMilitiaChange(this._settlement, false).ResultNumber), TownManagementDescriptionItemVM.DescriptionType.Militia, hint7));
			BasicTooltipViewModel hint8 = new BasicTooltipViewModel(() => CampaignUIHelper.GetTownGarrisonTooltip(this._settlement.Town));
			Collection<TownManagementDescriptionItemVM> middleSecondTextList = this.MiddleSecondTextList;
			TextObject title = GameTexts.FindText("str_town_management_garrison", null);
			MobileParty garrisonParty = this._settlement.Town.GarrisonParty;
			middleSecondTextList.Add(new TownManagementDescriptionItemVM(title, (garrisonParty != null) ? garrisonParty.Party.NumberOfAllMembers : 0, MathF.Round(SettlementHelper.GetGarrisonChangeExplainedNumber(this._settlement.Town).ResultNumber), TownManagementDescriptionItemVM.DescriptionType.Garrison, hint8));
		}

		// Token: 0x0600105E RID: 4190 RVA: 0x00042E22 File Offset: 0x00041022
		private void OnChangeInBuildingQueue()
		{
			this.OnProjectSelectionDone();
			this.RefreshTownManagementStats();
		}

		// Token: 0x0600105F RID: 4191 RVA: 0x00042E30 File Offset: 0x00041030
		private void RefreshCurrentDevelopment()
		{
			if (this._settlement.Town.CurrentBuilding != null)
			{
				this.IsCurrentProjectDaily = this._settlement.Town.CurrentBuilding.BuildingType.IsDailyProject;
				if (!this.IsCurrentProjectDaily)
				{
					this.CurrentProjectProgress = (int)(BuildingHelper.GetProgressOfBuilding(this.ProjectSelection.CurrentSelectedProject.Building, this._settlement.Town) * 100f);
					this.ProjectSelection.CurrentSelectedProject.RefreshProductionText();
				}
			}
		}

		// Token: 0x06001060 RID: 4192 RVA: 0x00042EB4 File Offset: 0x000410B4
		private void OnProjectSelectionDone()
		{
			List<Building> localDevelopmentList = this.ProjectSelection.LocalDevelopmentList;
			Building building = this.ProjectSelection.CurrentDailyDefault.Building;
			if (localDevelopmentList != null)
			{
				BuildingHelper.ChangeCurrentBuildingQueue(localDevelopmentList, this._settlement.Town);
			}
			if (building != this._settlement.Town.Buildings.FirstOrDefault((Building k) => k.IsCurrentlyDefault) && building != null)
			{
				BuildingHelper.ChangeDefaultBuilding(building, this._settlement.Town);
			}
			this.RefreshCurrentDevelopment();
		}

		// Token: 0x06001061 RID: 4193 RVA: 0x00042F44 File Offset: 0x00041144
		private void OnGovernorSelectionDone(Hero selectedGovernor)
		{
			if (selectedGovernor != this.CurrentGovernor.Hero)
			{
				this.CurrentGovernor = new HeroVM(selectedGovernor, true);
				if (this.CurrentGovernor.Hero != null)
				{
					ChangeGovernorAction.Apply(this._settlement.Town, this.CurrentGovernor.Hero);
					this.CurrentGovernorTooltip = new BasicTooltipViewModel(() => CampaignUIHelper.GetHeroGovernorEffectsTooltip(selectedGovernor, this._settlement));
				}
				else
				{
					ChangeGovernorAction.RemoveGovernorOfIfExists(this._settlement.Town);
					this.CurrentGovernorTooltip = new BasicTooltipViewModel();
				}
			}
			this.UpdateGovernorSelectionProperties();
			this.RefreshTownManagementStats();
		}

		// Token: 0x06001062 RID: 4194 RVA: 0x00042FF4 File Offset: 0x000411F4
		private void UpdateGovernorSelectionProperties()
		{
			this.HasGovernor = this.CurrentGovernor.Hero != null;
			TextObject hintText;
			this.IsGovernorSelectionEnabled = this.GetCanChangeGovernor(out hintText);
			this.GovernorSelectionDisabledHint = new HintViewModel(hintText, null);
		}

		// Token: 0x06001063 RID: 4195 RVA: 0x00043030 File Offset: 0x00041230
		private bool GetCanChangeGovernor(out TextObject disabledReason)
		{
			HeroVM currentGovernor = this.CurrentGovernor;
			bool flag;
			if (currentGovernor == null)
			{
				flag = false;
			}
			else
			{
				Hero hero = currentGovernor.Hero;
				bool? flag2 = ((hero != null) ? new bool?(hero.IsTraveling) : null);
				bool flag3 = true;
				flag = (flag2.GetValueOrDefault() == flag3) & (flag2 != null);
			}
			if (flag)
			{
				disabledReason = new TextObject("{=qbqimqMb}{GOVERNOR.NAME} is on the way to be the new governor of {SETTLEMENT_NAME}", null);
				if (this.CurrentGovernor.Hero.CharacterObject != null)
				{
					StringHelpers.SetCharacterProperties("GOVERNOR", this.CurrentGovernor.Hero.CharacterObject, disabledReason, false);
				}
				TextObject textObject = disabledReason;
				string tag = "SETTLEMENT_NAME";
				TextObject name = this._settlement.Name;
				textObject.SetTextVariable(tag, ((name != null) ? name.ToString() : null) ?? string.Empty);
				return false;
			}
			disabledReason = TextObject.GetEmpty();
			return true;
		}

		// Token: 0x06001064 RID: 4196 RVA: 0x000430F3 File Offset: 0x000412F3
		private void OnReserveUpdated()
		{
			this.RefreshCurrentDevelopment();
			this.RefreshTownManagementStats();
		}

		// Token: 0x06001065 RID: 4197 RVA: 0x00043101 File Offset: 0x00041301
		public void ExecuteDone()
		{
			this.OnProjectSelectionDone();
			this.Show = false;
		}

		// Token: 0x06001066 RID: 4198 RVA: 0x00043110 File Offset: 0x00041310
		public override void OnFinalize()
		{
			base.OnFinalize();
			this.DoneInputKey.OnFinalize();
		}

		// Token: 0x06001067 RID: 4199 RVA: 0x00043123 File Offset: 0x00041323
		public void SetDoneInputKey(HotKey hotKey)
		{
			this.DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
		}

		// Token: 0x1700054A RID: 1354
		// (get) Token: 0x06001068 RID: 4200 RVA: 0x00043132 File Offset: 0x00041332
		// (set) Token: 0x06001069 RID: 4201 RVA: 0x0004313A File Offset: 0x0004133A
		[DataSourceProperty]
		public InputKeyItemVM DoneInputKey
		{
			get
			{
				return this._doneInputKey;
			}
			set
			{
				if (value != this._doneInputKey)
				{
					this._doneInputKey = value;
					base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "DoneInputKey");
				}
			}
		}

		// Token: 0x1700054B RID: 1355
		// (get) Token: 0x0600106A RID: 4202 RVA: 0x00043158 File Offset: 0x00041358
		// (set) Token: 0x0600106B RID: 4203 RVA: 0x00043160 File Offset: 0x00041360
		[DataSourceProperty]
		public string CompletionText
		{
			get
			{
				return this._completionText;
			}
			set
			{
				if (value != this._completionText)
				{
					this._completionText = value;
					base.OnPropertyChangedWithValue<string>(value, "CompletionText");
				}
			}
		}

		// Token: 0x1700054C RID: 1356
		// (get) Token: 0x0600106C RID: 4204 RVA: 0x00043183 File Offset: 0x00041383
		// (set) Token: 0x0600106D RID: 4205 RVA: 0x0004318B File Offset: 0x0004138B
		[DataSourceProperty]
		public string GovernorText
		{
			get
			{
				return this._governorText;
			}
			set
			{
				if (value != this._governorText)
				{
					this._governorText = value;
					base.OnPropertyChangedWithValue<string>(value, "GovernorText");
				}
			}
		}

		// Token: 0x1700054D RID: 1357
		// (get) Token: 0x0600106E RID: 4206 RVA: 0x000431AE File Offset: 0x000413AE
		// (set) Token: 0x0600106F RID: 4207 RVA: 0x000431B6 File Offset: 0x000413B6
		[DataSourceProperty]
		public string ManageText
		{
			get
			{
				return this._manageText;
			}
			set
			{
				if (value != this._manageText)
				{
					this._manageText = value;
					base.OnPropertyChangedWithValue<string>(value, "ManageText");
				}
			}
		}

		// Token: 0x1700054E RID: 1358
		// (get) Token: 0x06001070 RID: 4208 RVA: 0x000431D9 File Offset: 0x000413D9
		// (set) Token: 0x06001071 RID: 4209 RVA: 0x000431E1 File Offset: 0x000413E1
		[DataSourceProperty]
		public string DoneText
		{
			get
			{
				return this._doneText;
			}
			set
			{
				if (value != this._doneText)
				{
					this._doneText = value;
					base.OnPropertyChangedWithValue<string>(value, "DoneText");
				}
			}
		}

		// Token: 0x1700054F RID: 1359
		// (get) Token: 0x06001072 RID: 4210 RVA: 0x00043204 File Offset: 0x00041404
		// (set) Token: 0x06001073 RID: 4211 RVA: 0x0004320C File Offset: 0x0004140C
		[DataSourceProperty]
		public string WallsText
		{
			get
			{
				return this._wallsText;
			}
			set
			{
				if (value != this._wallsText)
				{
					this._wallsText = value;
					base.OnPropertyChangedWithValue<string>(value, "WallsText");
				}
			}
		}

		// Token: 0x17000550 RID: 1360
		// (get) Token: 0x06001074 RID: 4212 RVA: 0x0004322F File Offset: 0x0004142F
		// (set) Token: 0x06001075 RID: 4213 RVA: 0x00043237 File Offset: 0x00041437
		[DataSourceProperty]
		public string CurrentProjectText
		{
			get
			{
				return this._currentProjectText;
			}
			set
			{
				if (value != this._currentProjectText)
				{
					this._currentProjectText = value;
					base.OnPropertyChangedWithValue<string>(value, "CurrentProjectText");
				}
			}
		}

		// Token: 0x17000551 RID: 1361
		// (get) Token: 0x06001076 RID: 4214 RVA: 0x0004325A File Offset: 0x0004145A
		// (set) Token: 0x06001077 RID: 4215 RVA: 0x00043262 File Offset: 0x00041462
		[DataSourceProperty]
		public string TitleText
		{
			get
			{
				return this._titleText;
			}
			set
			{
				if (value != this._titleText)
				{
					this._titleText = value;
					base.OnPropertyChangedWithValue<string>(value, "TitleText");
				}
			}
		}

		// Token: 0x17000552 RID: 1362
		// (get) Token: 0x06001078 RID: 4216 RVA: 0x00043285 File Offset: 0x00041485
		// (set) Token: 0x06001079 RID: 4217 RVA: 0x0004328D File Offset: 0x0004148D
		[DataSourceProperty]
		public bool HasGovernor
		{
			get
			{
				return this._hasGovernor;
			}
			set
			{
				if (value != this._hasGovernor)
				{
					this._hasGovernor = value;
					base.OnPropertyChangedWithValue(value, "HasGovernor");
				}
			}
		}

		// Token: 0x17000553 RID: 1363
		// (get) Token: 0x0600107A RID: 4218 RVA: 0x000432AB File Offset: 0x000414AB
		// (set) Token: 0x0600107B RID: 4219 RVA: 0x000432B3 File Offset: 0x000414B3
		[DataSourceProperty]
		public bool IsGovernorSelectionEnabled
		{
			get
			{
				return this._isGovernorSelectionEnabled;
			}
			set
			{
				if (value != this._isGovernorSelectionEnabled)
				{
					this._isGovernorSelectionEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsGovernorSelectionEnabled");
				}
			}
		}

		// Token: 0x17000554 RID: 1364
		// (get) Token: 0x0600107C RID: 4220 RVA: 0x000432D1 File Offset: 0x000414D1
		// (set) Token: 0x0600107D RID: 4221 RVA: 0x000432D9 File Offset: 0x000414D9
		[DataSourceProperty]
		public bool IsTown
		{
			get
			{
				return this._isTown;
			}
			set
			{
				if (value != this._isTown)
				{
					this._isTown = value;
					base.OnPropertyChangedWithValue(value, "IsTown");
				}
			}
		}

		// Token: 0x17000555 RID: 1365
		// (get) Token: 0x0600107E RID: 4222 RVA: 0x000432F7 File Offset: 0x000414F7
		// (set) Token: 0x0600107F RID: 4223 RVA: 0x000432FF File Offset: 0x000414FF
		[DataSourceProperty]
		public bool Show
		{
			get
			{
				return this._show;
			}
			set
			{
				if (value != this._show)
				{
					this._show = value;
					base.OnPropertyChangedWithValue(value, "Show");
				}
			}
		}

		// Token: 0x17000556 RID: 1366
		// (get) Token: 0x06001080 RID: 4224 RVA: 0x0004331D File Offset: 0x0004151D
		// (set) Token: 0x06001081 RID: 4225 RVA: 0x00043325 File Offset: 0x00041525
		[DataSourceProperty]
		public bool IsThereCurrentProject
		{
			get
			{
				return this._isThereCurrentProject;
			}
			set
			{
				if (value != this._isThereCurrentProject)
				{
					this._isThereCurrentProject = value;
					base.OnPropertyChangedWithValue(value, "IsThereCurrentProject");
				}
			}
		}

		// Token: 0x17000557 RID: 1367
		// (get) Token: 0x06001082 RID: 4226 RVA: 0x00043343 File Offset: 0x00041543
		// (set) Token: 0x06001083 RID: 4227 RVA: 0x0004334B File Offset: 0x0004154B
		[DataSourceProperty]
		public bool IsSelectingGovernor
		{
			get
			{
				return this._isSelectingGovernor;
			}
			set
			{
				if (value != this._isSelectingGovernor)
				{
					this._isSelectingGovernor = value;
					base.OnPropertyChangedWithValue(value, "IsSelectingGovernor");
				}
			}
		}

		// Token: 0x17000558 RID: 1368
		// (get) Token: 0x06001084 RID: 4228 RVA: 0x00043369 File Offset: 0x00041569
		// (set) Token: 0x06001085 RID: 4229 RVA: 0x00043371 File Offset: 0x00041571
		[DataSourceProperty]
		public MBBindingList<TownManagementDescriptionItemVM> MiddleFirstTextList
		{
			get
			{
				return this._middleLeftTextList;
			}
			set
			{
				if (value != this._middleLeftTextList)
				{
					this._middleLeftTextList = value;
					base.OnPropertyChanged("MiddleLeftTextList");
				}
			}
		}

		// Token: 0x17000559 RID: 1369
		// (get) Token: 0x06001086 RID: 4230 RVA: 0x0004338E File Offset: 0x0004158E
		// (set) Token: 0x06001087 RID: 4231 RVA: 0x00043396 File Offset: 0x00041596
		[DataSourceProperty]
		public MBBindingList<TownManagementDescriptionItemVM> MiddleSecondTextList
		{
			get
			{
				return this._middleRightTextList;
			}
			set
			{
				if (value != this._middleRightTextList)
				{
					this._middleRightTextList = value;
					base.OnPropertyChanged("MiddleRightTextList");
				}
			}
		}

		// Token: 0x1700055A RID: 1370
		// (get) Token: 0x06001088 RID: 4232 RVA: 0x000433B3 File Offset: 0x000415B3
		// (set) Token: 0x06001089 RID: 4233 RVA: 0x000433BB File Offset: 0x000415BB
		[DataSourceProperty]
		public MBBindingList<TownManagementShopItemVM> Shops
		{
			get
			{
				return this._shops;
			}
			set
			{
				if (value != this._shops)
				{
					this._shops = value;
					base.OnPropertyChangedWithValue<MBBindingList<TownManagementShopItemVM>>(value, "Shops");
				}
			}
		}

		// Token: 0x1700055B RID: 1371
		// (get) Token: 0x0600108A RID: 4234 RVA: 0x000433D9 File Offset: 0x000415D9
		// (set) Token: 0x0600108B RID: 4235 RVA: 0x000433E1 File Offset: 0x000415E1
		[DataSourceProperty]
		public MBBindingList<TownManagementVillageItemVM> Villages
		{
			get
			{
				return this._villages;
			}
			set
			{
				if (value != this._villages)
				{
					this._villages = value;
					base.OnPropertyChangedWithValue<MBBindingList<TownManagementVillageItemVM>>(value, "Villages");
				}
			}
		}

		// Token: 0x1700055C RID: 1372
		// (get) Token: 0x0600108C RID: 4236 RVA: 0x000433FF File Offset: 0x000415FF
		// (set) Token: 0x0600108D RID: 4237 RVA: 0x00043407 File Offset: 0x00041607
		[DataSourceProperty]
		public HintViewModel GovernorSelectionDisabledHint
		{
			get
			{
				return this._governorSelectionDisabledHint;
			}
			set
			{
				if (value != this._governorSelectionDisabledHint)
				{
					this._governorSelectionDisabledHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "GovernorSelectionDisabledHint");
				}
			}
		}

		// Token: 0x1700055D RID: 1373
		// (get) Token: 0x0600108E RID: 4238 RVA: 0x00043425 File Offset: 0x00041625
		// (set) Token: 0x0600108F RID: 4239 RVA: 0x0004342D File Offset: 0x0004162D
		[DataSourceProperty]
		public string VillagesText
		{
			get
			{
				return this._villagesText;
			}
			set
			{
				if (value != this._villagesText)
				{
					this._villagesText = value;
					base.OnPropertyChangedWithValue<string>(value, "VillagesText");
				}
			}
		}

		// Token: 0x1700055E RID: 1374
		// (get) Token: 0x06001090 RID: 4240 RVA: 0x00043450 File Offset: 0x00041650
		// (set) Token: 0x06001091 RID: 4241 RVA: 0x00043458 File Offset: 0x00041658
		[DataSourceProperty]
		public string ShopsInSettlementText
		{
			get
			{
				return this._shopsInSettlementText;
			}
			set
			{
				if (value != this._shopsInSettlementText)
				{
					this._shopsInSettlementText = value;
					base.OnPropertyChangedWithValue<string>(value, "ShopsInSettlementText");
				}
			}
		}

		// Token: 0x1700055F RID: 1375
		// (get) Token: 0x06001092 RID: 4242 RVA: 0x0004347B File Offset: 0x0004167B
		// (set) Token: 0x06001093 RID: 4243 RVA: 0x00043483 File Offset: 0x00041683
		[DataSourceProperty]
		public bool IsCurrentProjectDaily
		{
			get
			{
				return this._isCurrentProjectDaily;
			}
			set
			{
				if (value != this._isCurrentProjectDaily)
				{
					this._isCurrentProjectDaily = value;
					base.OnPropertyChangedWithValue(value, "IsCurrentProjectDaily");
				}
			}
		}

		// Token: 0x17000560 RID: 1376
		// (get) Token: 0x06001094 RID: 4244 RVA: 0x000434A1 File Offset: 0x000416A1
		// (set) Token: 0x06001095 RID: 4245 RVA: 0x000434A9 File Offset: 0x000416A9
		[DataSourceProperty]
		public int CurrentProjectProgress
		{
			get
			{
				return this._currentProjectProgress;
			}
			set
			{
				if (value != this._currentProjectProgress)
				{
					this._currentProjectProgress = value;
					base.OnPropertyChangedWithValue(value, "CurrentProjectProgress");
				}
			}
		}

		// Token: 0x17000561 RID: 1377
		// (get) Token: 0x06001096 RID: 4246 RVA: 0x000434C7 File Offset: 0x000416C7
		// (set) Token: 0x06001097 RID: 4247 RVA: 0x000434CF File Offset: 0x000416CF
		[DataSourceProperty]
		public SettlementProjectSelectionVM ProjectSelection
		{
			get
			{
				return this._projectSelection;
			}
			set
			{
				if (value != this._projectSelection)
				{
					this._projectSelection = value;
					base.OnPropertyChangedWithValue<SettlementProjectSelectionVM>(value, "ProjectSelection");
				}
			}
		}

		// Token: 0x17000562 RID: 1378
		// (get) Token: 0x06001098 RID: 4248 RVA: 0x000434ED File Offset: 0x000416ED
		// (set) Token: 0x06001099 RID: 4249 RVA: 0x000434F5 File Offset: 0x000416F5
		[DataSourceProperty]
		public SettlementGovernorSelectionVM GovernorSelection
		{
			get
			{
				return this._governorSelection;
			}
			set
			{
				if (value != this._governorSelection)
				{
					this._governorSelection = value;
					base.OnPropertyChangedWithValue<SettlementGovernorSelectionVM>(value, "GovernorSelection");
				}
			}
		}

		// Token: 0x17000563 RID: 1379
		// (get) Token: 0x0600109A RID: 4250 RVA: 0x00043513 File Offset: 0x00041713
		// (set) Token: 0x0600109B RID: 4251 RVA: 0x0004351B File Offset: 0x0004171B
		[DataSourceProperty]
		public TownManagementReserveControlVM ReserveControl
		{
			get
			{
				return this._reserveControl;
			}
			set
			{
				if (value != this._reserveControl)
				{
					this._reserveControl = value;
					base.OnPropertyChangedWithValue<TownManagementReserveControlVM>(value, "ReserveControl");
				}
			}
		}

		// Token: 0x17000564 RID: 1380
		// (get) Token: 0x0600109C RID: 4252 RVA: 0x00043539 File Offset: 0x00041739
		// (set) Token: 0x0600109D RID: 4253 RVA: 0x00043541 File Offset: 0x00041741
		[DataSourceProperty]
		public BasicTooltipViewModel CurrentGovernorTooltip
		{
			get
			{
				return this._currentGovernorTooltip;
			}
			set
			{
				if (value != this._currentGovernorTooltip)
				{
					this._currentGovernorTooltip = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "CurrentGovernorTooltip");
				}
			}
		}

		// Token: 0x17000565 RID: 1381
		// (get) Token: 0x0600109E RID: 4254 RVA: 0x0004355F File Offset: 0x0004175F
		// (set) Token: 0x0600109F RID: 4255 RVA: 0x00043567 File Offset: 0x00041767
		[DataSourceProperty]
		public HeroVM CurrentGovernor
		{
			get
			{
				return this._currentGovernor;
			}
			set
			{
				if (value != this._currentGovernor)
				{
					this._currentGovernor = value;
					base.OnPropertyChangedWithValue<HeroVM>(value, "CurrentGovernor");
				}
			}
		}

		// Token: 0x17000566 RID: 1382
		// (get) Token: 0x060010A0 RID: 4256 RVA: 0x00043585 File Offset: 0x00041785
		// (set) Token: 0x060010A1 RID: 4257 RVA: 0x0004358D File Offset: 0x0004178D
		[DataSourceProperty]
		public BasicTooltipViewModel ConsumptionTooltip
		{
			get
			{
				return this._consumptionTooltip;
			}
			set
			{
				if (value != this._consumptionTooltip)
				{
					this._consumptionTooltip = value;
					base.OnPropertyChangedWithValue<BasicTooltipViewModel>(value, "ConsumptionTooltip");
				}
			}
		}

		// Token: 0x04000778 RID: 1912
		private readonly Settlement _settlement;

		// Token: 0x04000779 RID: 1913
		private InputKeyItemVM _doneInputKey;

		// Token: 0x0400077A RID: 1914
		private bool _isThereCurrentProject;

		// Token: 0x0400077B RID: 1915
		private bool _isSelectingGovernor;

		// Token: 0x0400077C RID: 1916
		private SettlementProjectSelectionVM _projectSelection;

		// Token: 0x0400077D RID: 1917
		private SettlementGovernorSelectionVM _governorSelection;

		// Token: 0x0400077E RID: 1918
		private TownManagementReserveControlVM _reserveControl;

		// Token: 0x0400077F RID: 1919
		private MBBindingList<TownManagementDescriptionItemVM> _middleLeftTextList;

		// Token: 0x04000780 RID: 1920
		private MBBindingList<TownManagementDescriptionItemVM> _middleRightTextList;

		// Token: 0x04000781 RID: 1921
		private MBBindingList<TownManagementShopItemVM> _shops;

		// Token: 0x04000782 RID: 1922
		private MBBindingList<TownManagementVillageItemVM> _villages;

		// Token: 0x04000783 RID: 1923
		private HintViewModel _governorSelectionDisabledHint;

		// Token: 0x04000784 RID: 1924
		private bool _show;

		// Token: 0x04000785 RID: 1925
		private bool _isTown;

		// Token: 0x04000786 RID: 1926
		private bool _hasGovernor;

		// Token: 0x04000787 RID: 1927
		private bool _isGovernorSelectionEnabled;

		// Token: 0x04000788 RID: 1928
		private string _titleText;

		// Token: 0x04000789 RID: 1929
		private bool _isCurrentProjectDaily;

		// Token: 0x0400078A RID: 1930
		private int _currentProjectProgress;

		// Token: 0x0400078B RID: 1931
		private string _currentProjectText;

		// Token: 0x0400078C RID: 1932
		private HeroVM _currentGovernor;

		// Token: 0x0400078D RID: 1933
		private BasicTooltipViewModel _currentGovernorTooltip;

		// Token: 0x0400078E RID: 1934
		private string _manageText;

		// Token: 0x0400078F RID: 1935
		private string _doneText;

		// Token: 0x04000790 RID: 1936
		private string _wallsText;

		// Token: 0x04000791 RID: 1937
		private string _completionText;

		// Token: 0x04000792 RID: 1938
		private string _villagesText;

		// Token: 0x04000793 RID: 1939
		private string _shopsInSettlementText;

		// Token: 0x04000794 RID: 1940
		private BasicTooltipViewModel _consumptionTooltip;

		// Token: 0x04000795 RID: 1941
		private string _governorText;
	}
}
