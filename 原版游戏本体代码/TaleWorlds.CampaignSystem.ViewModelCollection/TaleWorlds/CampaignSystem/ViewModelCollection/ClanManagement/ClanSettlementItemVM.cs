using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement
{
	// Token: 0x0200012F RID: 303
	public class ClanSettlementItemVM : ViewModel
	{
		// Token: 0x06001C32 RID: 7218 RVA: 0x00068034 File Offset: 0x00066234
		public ClanSettlementItemVM(Settlement settlement, Action<ClanSettlementItemVM> onSelection, Action onShowSendMembers, ITeleportationCampaignBehavior teleportationBehavior)
		{
			this.Settlement = settlement;
			this._onSelection = onSelection;
			this._onShowSendMembers = onShowSendMembers;
			this._teleportationBehavior = teleportationBehavior;
			this.IsFortification = settlement.IsFortification;
			SettlementComponent settlementComponent = settlement.SettlementComponent;
			this.FileName = ((settlementComponent == null) ? "placeholder" : (settlementComponent.BackgroundMeshName + "_t"));
			this.ItemProperties = new MBBindingList<SelectableFiefItemPropertyVM>();
			this.ProfitItemProperties = new MBBindingList<ProfitItemPropertyVM>();
			this.TotalProfit = new ProfitItemPropertyVM(GameTexts.FindText("str_profit", null).ToString(), 0, ProfitItemPropertyVM.PropertyType.None, null, null);
			this.ImageName = ((settlementComponent != null) ? settlementComponent.WaitMeshName : "");
			this.VillagesOwned = new MBBindingList<ClanSettlementItemVM>();
			this.Notables = new MBBindingList<HeroVM>();
			this.Members = new MBBindingList<HeroVM>();
			this._patrolsBehavior = Campaign.Current.GetCampaignBehavior<IPatrolPartiesCampaignBehavior>();
			this.RefreshValues();
		}

		// Token: 0x06001C33 RID: 7219 RVA: 0x0006811C File Offset: 0x0006631C
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.VillagesText = GameTexts.FindText("str_villages", null).ToString();
			this.NotablesText = GameTexts.FindText("str_center_notables", null).ToString();
			this.MembersText = GameTexts.FindText("str_members", null).ToString();
			this.Name = this.Settlement.Name.ToString();
			this.UpdateProperties();
		}

		// Token: 0x06001C34 RID: 7220 RVA: 0x0006818D File Offset: 0x0006638D
		protected virtual ClanSettlementItemVM CreateSettlementItem(Settlement settlement, Action<ClanSettlementItemVM> onSelection, Action onShowSendMembers, ITeleportationCampaignBehavior teleportationBehavior)
		{
			return new ClanSettlementItemVM(settlement, onSelection, onShowSendMembers, teleportationBehavior);
		}

		// Token: 0x06001C35 RID: 7221 RVA: 0x00068199 File Offset: 0x00066399
		public void OnSettlementSelection()
		{
			this._onSelection(this);
		}

		// Token: 0x06001C36 RID: 7222 RVA: 0x000681A7 File Offset: 0x000663A7
		public void ExecuteLink()
		{
			MBInformationManager.HideInformations();
			Campaign.Current.EncyclopediaManager.GoToLink(this.Settlement.EncyclopediaLink);
		}

		// Token: 0x06001C37 RID: 7223 RVA: 0x000681C8 File Offset: 0x000663C8
		public void ExecuteCloseTooltip()
		{
			MBInformationManager.HideInformations();
		}

		// Token: 0x06001C38 RID: 7224 RVA: 0x000681CF File Offset: 0x000663CF
		public void ExecuteOpenTooltip()
		{
			InformationManager.ShowTooltip(typeof(Settlement), new object[] { this.Settlement });
		}

		// Token: 0x06001C39 RID: 7225 RVA: 0x000681EF File Offset: 0x000663EF
		public void ExecuteSendMembers()
		{
			Action onShowSendMembers = this._onShowSendMembers;
			if (onShowSendMembers == null)
			{
				return;
			}
			onShowSendMembers();
		}

		// Token: 0x06001C3A RID: 7226 RVA: 0x00068201 File Offset: 0x00066401
		private void OnGovernorChanged(Hero oldHero, Hero newHero)
		{
			ChangeGovernorAction.Apply(this.Settlement.Town, newHero);
		}

		// Token: 0x06001C3B RID: 7227 RVA: 0x00068214 File Offset: 0x00066414
		private bool IsGovernorAssignable(Hero oldHero, Hero newHero)
		{
			return newHero.IsActive && newHero.GovernorOf == null;
		}

		// Token: 0x06001C3C RID: 7228 RVA: 0x0006822C File Offset: 0x0006642C
		protected virtual void UpdateProperties()
		{
			this.ItemProperties.Clear();
			this.VillagesOwned.Clear();
			this.Notables.Clear();
			this.Members.Clear();
			foreach (Village village in this.Settlement.BoundVillages)
			{
				this.VillagesOwned.Add(this.CreateSettlementItem(village.Settlement, null, null, null));
			}
			this.HasNotables = !this.Settlement.Notables.IsEmpty<Hero>();
			foreach (Hero hero in this.Settlement.Notables)
			{
				this.Notables.Add(new HeroVM(hero, false));
			}
			foreach (Hero hero2 in from h in this.Settlement.HeroesWithoutParty
				where h.Clan == Clan.PlayerClan
				select h)
			{
				this.Members.Add(new HeroVM(hero2, false));
			}
			this.HasGovernor = false;
			if (!this.Settlement.IsVillage)
			{
				Town town = this.Settlement.Town;
				Hero hero3 = ((((town != null) ? town.Governor : null) != null) ? this.Settlement.Town.Governor : CampaignUIHelper.GetTeleportingGovernor(this.Settlement, this._teleportationBehavior));
				this.HasGovernor = hero3 != null;
				this.Governor = (this.HasGovernor ? new HeroVM(hero3, false) : null);
			}
			this.IsFortification = this.Settlement.IsFortification;
			if (this.Settlement.Town != null)
			{
				BasicTooltipViewModel hint = new BasicTooltipViewModel(() => CampaignUIHelper.GetTownWallsTooltip(this.Settlement.Town));
				this.ItemProperties.Add(new SelectableFiefItemPropertyVM(GameTexts.FindText("str_walls", null).ToString(), this.Settlement.Town.GetWallLevel().ToString(), 0, SelectableItemPropertyVM.PropertyType.Wall, hint, false));
			}
			if (this.Settlement.Town != null)
			{
				BasicTooltipViewModel hint2 = new BasicTooltipViewModel(() => CampaignUIHelper.GetTownGarrisonTooltip(this.Settlement.Town));
				int changeAmount = (int)SettlementHelper.GetGarrisonChangeExplainedNumber(this.Settlement.Town).ResultNumber;
				Collection<SelectableFiefItemPropertyVM> itemProperties = this.ItemProperties;
				string name = GameTexts.FindText("str_garrison", null).ToString();
				MobileParty garrisonParty = this.Settlement.Town.GarrisonParty;
				itemProperties.Add(new SelectableFiefItemPropertyVM(name, ((garrisonParty != null) ? garrisonParty.Party.NumberOfAllMembers.ToString() : null) ?? "0", changeAmount, SelectableItemPropertyVM.PropertyType.Garrison, hint2, false));
			}
			int num = (int)this.Settlement.Militia;
			List<TooltipProperty> militiaHint = (this.Settlement.IsVillage ? CampaignUIHelper.GetVillageMilitiaTooltip(this.Settlement.Village) : CampaignUIHelper.GetTownMilitiaTooltip(this.Settlement.Town));
			int changeAmount2 = ((this.Settlement.Town != null) ? ((int)this.Settlement.Town.MilitiaChange) : ((int)this.Settlement.Village.MilitiaChange));
			this.ItemProperties.Add(new SelectableFiefItemPropertyVM(GameTexts.FindText("str_militia", null).ToString(), num.ToString(), changeAmount2, SelectableItemPropertyVM.PropertyType.Militia, new BasicTooltipViewModel(() => militiaHint), false));
			if (this.Settlement.Town != null)
			{
				BasicTooltipViewModel hint3 = new BasicTooltipViewModel(() => CampaignUIHelper.GetTownFoodTooltip(this.Settlement.Town));
				int changeAmount3 = (int)this.Settlement.Town.FoodChange;
				this.ItemProperties.Add(new SelectableFiefItemPropertyVM(GameTexts.FindText("str_food_stocks", null).ToString(), ((int)this.Settlement.Town.FoodStocks).ToString(), changeAmount3, SelectableItemPropertyVM.PropertyType.Food, hint3, false));
			}
			if (this.Settlement.IsFortification)
			{
				int changeAmount4 = ((this.Settlement.Town != null) ? ((int)this.Settlement.Town.ProsperityChange) : ((int)this.Settlement.Village.HearthChange));
				BasicTooltipViewModel hint4;
				if (this.Settlement.Town != null)
				{
					hint4 = new BasicTooltipViewModel(() => CampaignUIHelper.GetTownProsperityTooltip(this.Settlement.Town));
				}
				else
				{
					hint4 = new BasicTooltipViewModel(() => CampaignUIHelper.GetVillageProsperityTooltip(this.Settlement.Village));
				}
				this.ItemProperties.Add(new SelectableFiefItemPropertyVM(GameTexts.FindText("str_prosperity", null).ToString(), string.Format("{0:0.##}", this.Settlement.Town.Prosperity), changeAmount4, SelectableItemPropertyVM.PropertyType.Prosperity, hint4, false));
			}
			if (this.Settlement.Town != null)
			{
				BasicTooltipViewModel hint5 = new BasicTooltipViewModel(() => CampaignUIHelper.GetTownLoyaltyTooltip(this.Settlement.Town));
				int changeAmount5 = (int)this.Settlement.Town.LoyaltyChange;
				bool isWarning = this.Settlement.IsTown && this.Settlement.Town.Loyalty < (float)Campaign.Current.Models.SettlementLoyaltyModel.RebelliousStateStartLoyaltyThreshold;
				this.ItemProperties.Add(new SelectableFiefItemPropertyVM(GameTexts.FindText("str_loyalty", null).ToString(), string.Format("{0:0.#}", this.Settlement.Town.Loyalty), changeAmount5, SelectableItemPropertyVM.PropertyType.Loyalty, hint5, isWarning));
			}
			if (this.Settlement.Town != null)
			{
				BasicTooltipViewModel hint6 = new BasicTooltipViewModel(() => CampaignUIHelper.GetTownSecurityTooltip(this.Settlement.Town));
				int changeAmount6 = (int)this.Settlement.Town.SecurityChange;
				this.ItemProperties.Add(new SelectableFiefItemPropertyVM(GameTexts.FindText("str_security", null).ToString(), string.Format("{0:0.#}", this.Settlement.Town.Security), changeAmount6, SelectableItemPropertyVM.PropertyType.Security, hint6, false));
			}
			if (this.Settlement.IsTown)
			{
				BasicTooltipViewModel hint7 = new BasicTooltipViewModel(() => CampaignUIHelper.GetTownPatrolTooltip(this.Settlement.Town));
				this.ItemProperties.Add(new SelectableFiefItemPropertyVM(GameTexts.FindText("str_patrol", null).ToString(), this._patrolsBehavior.GetSettlementPatrolStatus(this.Settlement).ToString(), 0, SelectableItemPropertyVM.PropertyType.Patrol, hint7, false));
			}
			TextObject textObject;
			this.IsSendMembersEnabled = CampaignUIHelper.GetMapScreenActionIsEnabledWithReason(out textObject);
			TextObject textObject2 = new TextObject("{=uGMGjUZy}Send your clan members to {SETTLEMENT_NAME}", null);
			textObject2.SetTextVariable("SETTLEMENT_NAME", this.Settlement.Name.ToString());
			this.SendMembersHint = new HintViewModel(this.IsSendMembersEnabled ? textObject2 : textObject, null);
			this.UpdateProfitProperties();
		}

		// Token: 0x06001C3D RID: 7229 RVA: 0x000688F4 File Offset: 0x00066AF4
		protected virtual void UpdateProfitProperties()
		{
			this.ProfitItemProperties.Clear();
			if (this.Settlement.Town != null)
			{
				Town town = this.Settlement.Town;
				ClanFinanceModel clanFinanceModel = Campaign.Current.Models.ClanFinanceModel;
				int num = 0;
				int num2 = (int)Campaign.Current.Models.SettlementTaxModel.CalculateTownTax(town, false).ResultNumber;
				int num3 = (int)clanFinanceModel.CalculateTownIncomeFromTariffs(Clan.PlayerClan, town, false).ResultNumber;
				int num4 = clanFinanceModel.CalculateTownIncomeFromProjects(town);
				if (num2 != 0)
				{
					this.ProfitItemProperties.Add(new ProfitItemPropertyVM(new TextObject("{=qeclv74c}Taxes", null).ToString(), num2, ProfitItemPropertyVM.PropertyType.Tax, null, null));
					num += num2;
				}
				if (num3 != 0)
				{
					this.ProfitItemProperties.Add(new ProfitItemPropertyVM(new TextObject("{=eIgC6YGp}Tariffs", null).ToString(), num3, ProfitItemPropertyVM.PropertyType.Tariff, null, null));
					num += num3;
				}
				if (town.GarrisonParty != null && town.GarrisonParty.IsActive)
				{
					int totalWage = town.GarrisonParty.TotalWage;
					if (totalWage != 0)
					{
						this.ProfitItemProperties.Add(new ProfitItemPropertyVM(new TextObject("{=5dkPxmZG}Garrison Wages", null).ToString(), -totalWage, ProfitItemPropertyVM.PropertyType.Garrison, null, null));
						num -= totalWage;
					}
				}
				foreach (Village village in town.Villages)
				{
					int num5 = clanFinanceModel.CalculateVillageIncome(Clan.PlayerClan, village, false);
					if (num5 != 0)
					{
						this.ProfitItemProperties.Add(new ProfitItemPropertyVM(village.Name.ToString(), num5, ProfitItemPropertyVM.PropertyType.Village, null, null));
						num += num5;
					}
				}
				if (num4 != 0)
				{
					Collection<ProfitItemPropertyVM> profitItemProperties = this.ProfitItemProperties;
					string name = new TextObject("{=J8ddrAOf}Governor Effects", null).ToString();
					int value = num4;
					ProfitItemPropertyVM.PropertyType type = ProfitItemPropertyVM.PropertyType.Governor;
					HeroVM governor = this.Governor;
					profitItemProperties.Add(new ProfitItemPropertyVM(name, value, type, (governor != null) ? governor.ImageIdentifier : null, null));
					num += num4;
				}
				this.TotalProfit.Value = num;
			}
		}

		// Token: 0x06001C3E RID: 7230 RVA: 0x00068AF0 File Offset: 0x00066CF0
		private bool IsSettlementSlotAssignable(Hero oldHero, Hero newHero)
		{
			return (oldHero == null || !oldHero.IsHumanPlayerCharacter) && !newHero.IsHumanPlayerCharacter && newHero.IsActive && (newHero.PartyBelongedTo == null || newHero.PartyBelongedTo.LeaderHero != newHero) && newHero.PartyBelongedToAsPrisoner == null;
		}

		// Token: 0x06001C3F RID: 7231 RVA: 0x00068B3F File Offset: 0x00066D3F
		private void ExecuteOpenSettlementPage()
		{
			Campaign.Current.EncyclopediaManager.GoToLink(this.Settlement.EncyclopediaLink);
		}

		// Token: 0x17000998 RID: 2456
		// (get) Token: 0x06001C40 RID: 7232 RVA: 0x00068B5B File Offset: 0x00066D5B
		// (set) Token: 0x06001C41 RID: 7233 RVA: 0x00068B63 File Offset: 0x00066D63
		[DataSourceProperty]
		public HeroVM Governor
		{
			get
			{
				return this._governor;
			}
			set
			{
				if (value != this._governor)
				{
					this._governor = value;
					base.OnPropertyChangedWithValue<HeroVM>(value, "Governor");
				}
			}
		}

		// Token: 0x17000999 RID: 2457
		// (get) Token: 0x06001C42 RID: 7234 RVA: 0x00068B81 File Offset: 0x00066D81
		// (set) Token: 0x06001C43 RID: 7235 RVA: 0x00068B89 File Offset: 0x00066D89
		[DataSourceProperty]
		public MBBindingList<SelectableFiefItemPropertyVM> ItemProperties
		{
			get
			{
				return this._itemProperties;
			}
			set
			{
				if (value != this._itemProperties)
				{
					this._itemProperties = value;
					base.OnPropertyChangedWithValue<MBBindingList<SelectableFiefItemPropertyVM>>(value, "ItemProperties");
				}
			}
		}

		// Token: 0x1700099A RID: 2458
		// (get) Token: 0x06001C44 RID: 7236 RVA: 0x00068BA7 File Offset: 0x00066DA7
		// (set) Token: 0x06001C45 RID: 7237 RVA: 0x00068BAF File Offset: 0x00066DAF
		[DataSourceProperty]
		public MBBindingList<ProfitItemPropertyVM> ProfitItemProperties
		{
			get
			{
				return this._profitItemProperties;
			}
			set
			{
				if (value != this._profitItemProperties)
				{
					this._profitItemProperties = value;
					base.OnPropertyChangedWithValue<MBBindingList<ProfitItemPropertyVM>>(value, "ProfitItemProperties");
				}
			}
		}

		// Token: 0x1700099B RID: 2459
		// (get) Token: 0x06001C46 RID: 7238 RVA: 0x00068BCD File Offset: 0x00066DCD
		// (set) Token: 0x06001C47 RID: 7239 RVA: 0x00068BD5 File Offset: 0x00066DD5
		[DataSourceProperty]
		public ProfitItemPropertyVM TotalProfit
		{
			get
			{
				return this._totalProfit;
			}
			set
			{
				if (value != this._totalProfit)
				{
					this._totalProfit = value;
					base.OnPropertyChangedWithValue<ProfitItemPropertyVM>(value, "TotalProfit");
				}
			}
		}

		// Token: 0x1700099C RID: 2460
		// (get) Token: 0x06001C48 RID: 7240 RVA: 0x00068BF3 File Offset: 0x00066DF3
		// (set) Token: 0x06001C49 RID: 7241 RVA: 0x00068BFB File Offset: 0x00066DFB
		[DataSourceProperty]
		public string FileName
		{
			get
			{
				return this._fileName;
			}
			set
			{
				if (value != this._fileName)
				{
					this._fileName = value;
					base.OnPropertyChangedWithValue<string>(value, "FileName");
				}
			}
		}

		// Token: 0x1700099D RID: 2461
		// (get) Token: 0x06001C4A RID: 7242 RVA: 0x00068C1E File Offset: 0x00066E1E
		// (set) Token: 0x06001C4B RID: 7243 RVA: 0x00068C26 File Offset: 0x00066E26
		[DataSourceProperty]
		public string ImageName
		{
			get
			{
				return this._imageName;
			}
			set
			{
				if (value != this._imageName)
				{
					this._imageName = value;
					base.OnPropertyChangedWithValue<string>(value, "ImageName");
				}
			}
		}

		// Token: 0x1700099E RID: 2462
		// (get) Token: 0x06001C4C RID: 7244 RVA: 0x00068C49 File Offset: 0x00066E49
		// (set) Token: 0x06001C4D RID: 7245 RVA: 0x00068C51 File Offset: 0x00066E51
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

		// Token: 0x1700099F RID: 2463
		// (get) Token: 0x06001C4E RID: 7246 RVA: 0x00068C74 File Offset: 0x00066E74
		// (set) Token: 0x06001C4F RID: 7247 RVA: 0x00068C7C File Offset: 0x00066E7C
		[DataSourceProperty]
		public string NotablesText
		{
			get
			{
				return this._notablesText;
			}
			set
			{
				if (value != this._notablesText)
				{
					this._notablesText = value;
					base.OnPropertyChangedWithValue<string>(value, "NotablesText");
				}
			}
		}

		// Token: 0x170009A0 RID: 2464
		// (get) Token: 0x06001C50 RID: 7248 RVA: 0x00068C9F File Offset: 0x00066E9F
		// (set) Token: 0x06001C51 RID: 7249 RVA: 0x00068CA7 File Offset: 0x00066EA7
		[DataSourceProperty]
		public string MembersText
		{
			get
			{
				return this._membersText;
			}
			set
			{
				if (value != this._membersText)
				{
					this._membersText = value;
					base.OnPropertyChangedWithValue<string>(value, "MembersText");
				}
			}
		}

		// Token: 0x170009A1 RID: 2465
		// (get) Token: 0x06001C52 RID: 7250 RVA: 0x00068CCA File Offset: 0x00066ECA
		// (set) Token: 0x06001C53 RID: 7251 RVA: 0x00068CD2 File Offset: 0x00066ED2
		[DataSourceProperty]
		public bool IsFortification
		{
			get
			{
				return this._isFortification;
			}
			set
			{
				if (value != this._isFortification)
				{
					this._isFortification = value;
					base.OnPropertyChangedWithValue(value, "IsFortification");
				}
			}
		}

		// Token: 0x170009A2 RID: 2466
		// (get) Token: 0x06001C54 RID: 7252 RVA: 0x00068CF0 File Offset: 0x00066EF0
		// (set) Token: 0x06001C55 RID: 7253 RVA: 0x00068CF8 File Offset: 0x00066EF8
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

		// Token: 0x170009A3 RID: 2467
		// (get) Token: 0x06001C56 RID: 7254 RVA: 0x00068D16 File Offset: 0x00066F16
		// (set) Token: 0x06001C57 RID: 7255 RVA: 0x00068D1E File Offset: 0x00066F1E
		[DataSourceProperty]
		public bool HasNotables
		{
			get
			{
				return this._hasNotables;
			}
			set
			{
				if (value != this._hasNotables)
				{
					this._hasNotables = value;
					base.OnPropertyChangedWithValue(value, "HasNotables");
				}
			}
		}

		// Token: 0x170009A4 RID: 2468
		// (get) Token: 0x06001C58 RID: 7256 RVA: 0x00068D3C File Offset: 0x00066F3C
		// (set) Token: 0x06001C59 RID: 7257 RVA: 0x00068D44 File Offset: 0x00066F44
		[DataSourceProperty]
		public bool IsSendMembersEnabled
		{
			get
			{
				return this._isSendMembersEnabled;
			}
			set
			{
				if (value != this._isSendMembersEnabled)
				{
					this._isSendMembersEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsSendMembersEnabled");
				}
			}
		}

		// Token: 0x170009A5 RID: 2469
		// (get) Token: 0x06001C5A RID: 7258 RVA: 0x00068D62 File Offset: 0x00066F62
		// (set) Token: 0x06001C5B RID: 7259 RVA: 0x00068D6A File Offset: 0x00066F6A
		[DataSourceProperty]
		public bool IsSelected
		{
			get
			{
				return this._isSelected;
			}
			set
			{
				if (value != this._isSelected)
				{
					this._isSelected = value;
					base.OnPropertyChangedWithValue(value, "IsSelected");
				}
			}
		}

		// Token: 0x170009A6 RID: 2470
		// (get) Token: 0x06001C5C RID: 7260 RVA: 0x00068D88 File Offset: 0x00066F88
		// (set) Token: 0x06001C5D RID: 7261 RVA: 0x00068D90 File Offset: 0x00066F90
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

		// Token: 0x170009A7 RID: 2471
		// (get) Token: 0x06001C5E RID: 7262 RVA: 0x00068DB3 File Offset: 0x00066FB3
		// (set) Token: 0x06001C5F RID: 7263 RVA: 0x00068DBB File Offset: 0x00066FBB
		[DataSourceProperty]
		public MBBindingList<ClanSettlementItemVM> VillagesOwned
		{
			get
			{
				return this._villagesOwned;
			}
			set
			{
				if (value != this._villagesOwned)
				{
					this._villagesOwned = value;
					base.OnPropertyChangedWithValue<MBBindingList<ClanSettlementItemVM>>(value, "VillagesOwned");
				}
			}
		}

		// Token: 0x170009A8 RID: 2472
		// (get) Token: 0x06001C60 RID: 7264 RVA: 0x00068DD9 File Offset: 0x00066FD9
		// (set) Token: 0x06001C61 RID: 7265 RVA: 0x00068DE1 File Offset: 0x00066FE1
		[DataSourceProperty]
		public MBBindingList<HeroVM> Notables
		{
			get
			{
				return this._notables;
			}
			set
			{
				if (value != this._notables)
				{
					this._notables = value;
					base.OnPropertyChangedWithValue<MBBindingList<HeroVM>>(value, "Notables");
				}
			}
		}

		// Token: 0x170009A9 RID: 2473
		// (get) Token: 0x06001C62 RID: 7266 RVA: 0x00068DFF File Offset: 0x00066FFF
		// (set) Token: 0x06001C63 RID: 7267 RVA: 0x00068E07 File Offset: 0x00067007
		[DataSourceProperty]
		public MBBindingList<HeroVM> Members
		{
			get
			{
				return this._members;
			}
			set
			{
				if (value != this._members)
				{
					this._members = value;
					base.OnPropertyChangedWithValue<MBBindingList<HeroVM>>(value, "Members");
				}
			}
		}

		// Token: 0x170009AA RID: 2474
		// (get) Token: 0x06001C64 RID: 7268 RVA: 0x00068E25 File Offset: 0x00067025
		// (set) Token: 0x06001C65 RID: 7269 RVA: 0x00068E2D File Offset: 0x0006702D
		[DataSourceProperty]
		public HintViewModel SendMembersHint
		{
			get
			{
				return this._sendMembersHint;
			}
			set
			{
				if (value != this._sendMembersHint)
				{
					this._sendMembersHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "SendMembersHint");
				}
			}
		}

		// Token: 0x04000D28 RID: 3368
		private readonly Action<ClanSettlementItemVM> _onSelection;

		// Token: 0x04000D29 RID: 3369
		private readonly Action _onShowSendMembers;

		// Token: 0x04000D2A RID: 3370
		private readonly ITeleportationCampaignBehavior _teleportationBehavior;

		// Token: 0x04000D2B RID: 3371
		private readonly IPatrolPartiesCampaignBehavior _patrolsBehavior;

		// Token: 0x04000D2C RID: 3372
		public readonly Settlement Settlement;

		// Token: 0x04000D2D RID: 3373
		private string _name;

		// Token: 0x04000D2E RID: 3374
		private HeroVM _governor;

		// Token: 0x04000D2F RID: 3375
		private string _fileName;

		// Token: 0x04000D30 RID: 3376
		private string _imageName;

		// Token: 0x04000D31 RID: 3377
		private string _villagesText;

		// Token: 0x04000D32 RID: 3378
		private string _notablesText;

		// Token: 0x04000D33 RID: 3379
		private string _membersText;

		// Token: 0x04000D34 RID: 3380
		private bool _isFortification;

		// Token: 0x04000D35 RID: 3381
		private bool _isSelected;

		// Token: 0x04000D36 RID: 3382
		private bool _hasGovernor;

		// Token: 0x04000D37 RID: 3383
		private bool _hasNotables;

		// Token: 0x04000D38 RID: 3384
		private bool _isSendMembersEnabled;

		// Token: 0x04000D39 RID: 3385
		private MBBindingList<SelectableFiefItemPropertyVM> _itemProperties;

		// Token: 0x04000D3A RID: 3386
		private MBBindingList<ProfitItemPropertyVM> _profitItemProperties;

		// Token: 0x04000D3B RID: 3387
		private ProfitItemPropertyVM _totalProfit;

		// Token: 0x04000D3C RID: 3388
		private MBBindingList<ClanSettlementItemVM> _villagesOwned;

		// Token: 0x04000D3D RID: 3389
		private MBBindingList<HeroVM> _notables;

		// Token: 0x04000D3E RID: 3390
		private MBBindingList<HeroVM> _members;

		// Token: 0x04000D3F RID: 3391
		private HintViewModel _sendMembersHint;
	}
}
