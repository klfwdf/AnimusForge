using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.ClanFinance;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.Supporters;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.Categories
{
	// Token: 0x0200013B RID: 315
	public class ClanIncomeVM : ViewModel
	{
		// Token: 0x170009F5 RID: 2549
		// (get) Token: 0x06001D4E RID: 7502 RVA: 0x0006C13E File Offset: 0x0006A33E
		// (set) Token: 0x06001D4F RID: 7503 RVA: 0x0006C146 File Offset: 0x0006A346
		public int TotalIncome { get; private set; }

		// Token: 0x06001D50 RID: 7504 RVA: 0x0006C150 File Offset: 0x0006A350
		public ClanIncomeVM(Action onRefresh, Action<ClanCardSelectionInfo> openCardSelectionPopup)
		{
			this._onRefresh = onRefresh;
			this._openCardSelectionPopup = openCardSelectionPopup;
			this.Incomes = new MBBindingList<ClanFinanceWorkshopItemVM>();
			this.SupporterGroups = new MBBindingList<ClanSupporterGroupVM>();
			this.Alleys = new MBBindingList<ClanFinanceAlleyItemVM>();
			this.SortController = new ClanIncomeSortControllerVM(this._incomes, this._supporterGroups, this._alleys);
			this.RefreshList();
		}

		// Token: 0x06001D51 RID: 7505 RVA: 0x0006C1B8 File Offset: 0x0006A3B8
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.NameText = GameTexts.FindText("str_sort_by_name_label", null).ToString();
			this.IncomeText = GameTexts.FindText("str_income", null).ToString();
			this.LocationText = GameTexts.FindText("str_tooltip_label_location", null).ToString();
			this.NoAdditionalIncomesText = GameTexts.FindText("str_clan_no_additional_incomes", null).ToString();
			this.Incomes.ApplyActionOnAllItems(delegate(ClanFinanceWorkshopItemVM x)
			{
				x.RefreshValues();
			});
			ClanFinanceWorkshopItemVM currentSelectedIncome = this.CurrentSelectedIncome;
			if (currentSelectedIncome != null)
			{
				currentSelectedIncome.RefreshValues();
			}
			this.SortController.RefreshValues();
		}

		// Token: 0x06001D52 RID: 7506 RVA: 0x0006C26C File Offset: 0x0006A46C
		public void RefreshList()
		{
			this.Incomes.Clear();
			foreach (Settlement settlement in Settlement.All)
			{
				if (settlement.IsTown)
				{
					foreach (Workshop workshop in settlement.Town.Workshops)
					{
						if (workshop.Owner == Hero.MainHero)
						{
							this.Incomes.Add(new ClanFinanceWorkshopItemVM(workshop, new Action<ClanFinanceWorkshopItemVM>(this.OnIncomeSelection), new Action(this.OnRefresh), this._openCardSelectionPopup));
						}
					}
				}
			}
			this.RefreshSupporters();
			this.RefreshAlleys();
			this.SortController.ResetAllStates();
			GameTexts.SetVariable("STR1", GameTexts.FindText("str_clan_workshops", null));
			GameTexts.SetVariable("LEFT", Hero.MainHero.OwnedWorkshops.Count);
			GameTexts.SetVariable("RIGHT", Campaign.Current.Models.WorkshopModel.GetMaxWorkshopCountForClanTier(Clan.PlayerClan.Tier));
			GameTexts.SetVariable("STR2", GameTexts.FindText("str_LEFT_over_RIGHT_in_paranthesis", null));
			this.WorkshopText = GameTexts.FindText("str_STR1_space_STR2", null).ToString();
			int num = 0;
			foreach (ClanSupporterGroupVM clanSupporterGroupVM in this.SupporterGroups)
			{
				num += clanSupporterGroupVM.Supporters.Count;
			}
			GameTexts.SetVariable("RANK", new TextObject("{=RzFyGnWJ}Supporters", null).ToString());
			GameTexts.SetVariable("NUMBER", num);
			this.SupportersText = GameTexts.FindText("str_RANK_with_NUM_between_parenthesis", null).ToString();
			GameTexts.SetVariable("RANK", new TextObject("{=7tKjfMSb}Alleys", null).ToString());
			GameTexts.SetVariable("NUMBER", this.Alleys.Count);
			this.AlleysText = GameTexts.FindText("str_RANK_with_NUM_between_parenthesis", null).ToString();
			this.RefreshTotalIncome();
			this.OnIncomeSelection(this.GetDefaultIncome());
			this.RefreshValues();
		}

		// Token: 0x06001D53 RID: 7507 RVA: 0x0006C4A8 File Offset: 0x0006A6A8
		private void RefreshSupporters()
		{
			foreach (ClanSupporterGroupVM clanSupporterGroupVM in this.SupporterGroups)
			{
				clanSupporterGroupVM.Supporters.Clear();
			}
			this.SupporterGroups.Clear();
			Dictionary<float, List<Hero>> dictionary = new Dictionary<float, List<Hero>>();
			NotablePowerModel notablePowerModel = Campaign.Current.Models.NotablePowerModel;
			foreach (Hero hero in from x in Clan.PlayerClan.SupporterNotables
				orderby x.Power
				select x)
			{
				if (hero.CurrentSettlement != null)
				{
					float influenceBonusToClan = notablePowerModel.GetInfluenceBonusToClan(hero);
					List<Hero> list;
					if (dictionary.TryGetValue(influenceBonusToClan, out list))
					{
						list.Add(hero);
					}
					else
					{
						dictionary.Add(influenceBonusToClan, new List<Hero> { hero });
					}
				}
			}
			foreach (KeyValuePair<float, List<Hero>> keyValuePair in dictionary)
			{
				if (keyValuePair.Value.Count > 0)
				{
					ClanSupporterGroupVM clanSupporterGroupVM2 = new ClanSupporterGroupVM(notablePowerModel.GetPowerRankName(keyValuePair.Value.FirstOrDefault<Hero>()), keyValuePair.Key, new Action<ClanSupporterGroupVM>(this.OnSupporterSelection));
					foreach (Hero hero2 in keyValuePair.Value)
					{
						clanSupporterGroupVM2.AddSupporter(hero2);
					}
					this.SupporterGroups.Add(clanSupporterGroupVM2);
				}
			}
			foreach (ClanSupporterGroupVM clanSupporterGroupVM3 in this.SupporterGroups)
			{
				clanSupporterGroupVM3.Refresh();
			}
		}

		// Token: 0x06001D54 RID: 7508 RVA: 0x0006C6BC File Offset: 0x0006A8BC
		private void RefreshAlleys()
		{
			this.Alleys.Clear();
			foreach (Alley alley in Hero.MainHero.OwnedAlleys)
			{
				this.Alleys.Add(new ClanFinanceAlleyItemVM(alley, this._openCardSelectionPopup, new Action<ClanFinanceAlleyItemVM>(this.OnAlleySelection), new Action(this.OnRefresh)));
			}
		}

		// Token: 0x06001D55 RID: 7509 RVA: 0x0006C748 File Offset: 0x0006A948
		private ClanFinanceWorkshopItemVM GetDefaultIncome()
		{
			return this.Incomes.FirstOrDefault<ClanFinanceWorkshopItemVM>();
		}

		// Token: 0x06001D56 RID: 7510 RVA: 0x0006C758 File Offset: 0x0006A958
		public void SelectWorkshop(Workshop workshop)
		{
			foreach (ClanFinanceWorkshopItemVM clanFinanceWorkshopItemVM in this.Incomes)
			{
				if (clanFinanceWorkshopItemVM != null)
				{
					ClanFinanceWorkshopItemVM clanFinanceWorkshopItemVM2 = clanFinanceWorkshopItemVM;
					if (clanFinanceWorkshopItemVM2.Workshop == workshop)
					{
						this.OnIncomeSelection(clanFinanceWorkshopItemVM2);
						break;
					}
				}
			}
		}

		// Token: 0x06001D57 RID: 7511 RVA: 0x0006C7B8 File Offset: 0x0006A9B8
		public void SelectAlley(Alley alley)
		{
			for (int i = 0; i < this.Alleys.Count; i++)
			{
				if (this.Alleys[i].Alley == alley)
				{
					this.OnAlleySelection(this.Alleys[i]);
					return;
				}
			}
		}

		// Token: 0x06001D58 RID: 7512 RVA: 0x0006C804 File Offset: 0x0006AA04
		private void OnAlleySelection(ClanFinanceAlleyItemVM alley)
		{
			if (alley == null)
			{
				if (this.CurrentSelectedAlley != null)
				{
					this.CurrentSelectedAlley.IsSelected = false;
				}
				this.CurrentSelectedAlley = null;
				return;
			}
			this.OnIncomeSelection(null);
			this.OnSupporterSelection(null);
			if (this.CurrentSelectedAlley != null)
			{
				this.CurrentSelectedAlley.IsSelected = false;
			}
			this.CurrentSelectedAlley = alley;
			if (alley != null)
			{
				alley.IsSelected = true;
			}
		}

		// Token: 0x06001D59 RID: 7513 RVA: 0x0006C864 File Offset: 0x0006AA64
		private void OnIncomeSelection(ClanFinanceWorkshopItemVM income)
		{
			if (income == null)
			{
				if (this.CurrentSelectedIncome != null)
				{
					this.CurrentSelectedIncome.IsSelected = false;
				}
				this.CurrentSelectedIncome = null;
				return;
			}
			this.OnSupporterSelection(null);
			this.OnAlleySelection(null);
			if (this.CurrentSelectedIncome != null)
			{
				this.CurrentSelectedIncome.IsSelected = false;
			}
			this.CurrentSelectedIncome = income;
			if (income != null)
			{
				income.IsSelected = true;
			}
		}

		// Token: 0x06001D5A RID: 7514 RVA: 0x0006C8C4 File Offset: 0x0006AAC4
		private void OnSupporterSelection(ClanSupporterGroupVM supporter)
		{
			if (supporter == null)
			{
				if (this.CurrentSelectedSupporterGroup != null)
				{
					this.CurrentSelectedSupporterGroup.IsSelected = false;
				}
				this.CurrentSelectedSupporterGroup = null;
				return;
			}
			this.OnIncomeSelection(null);
			this.OnAlleySelection(null);
			if (this.CurrentSelectedSupporterGroup != null)
			{
				this.CurrentSelectedSupporterGroup.IsSelected = false;
			}
			this.CurrentSelectedSupporterGroup = supporter;
			if (this.CurrentSelectedSupporterGroup != null)
			{
				this.CurrentSelectedSupporterGroup.IsSelected = true;
			}
		}

		// Token: 0x06001D5B RID: 7515 RVA: 0x0006C92D File Offset: 0x0006AB2D
		public void RefreshTotalIncome()
		{
			this.TotalIncome = this.Incomes.Sum((ClanFinanceWorkshopItemVM i) => i.Income);
		}

		// Token: 0x06001D5C RID: 7516 RVA: 0x0006C95F File Offset: 0x0006AB5F
		public void OnRefresh()
		{
			Action onRefresh = this._onRefresh;
			if (onRefresh == null)
			{
				return;
			}
			onRefresh();
		}

		// Token: 0x170009F6 RID: 2550
		// (get) Token: 0x06001D5D RID: 7517 RVA: 0x0006C971 File Offset: 0x0006AB71
		// (set) Token: 0x06001D5E RID: 7518 RVA: 0x0006C979 File Offset: 0x0006AB79
		[DataSourceProperty]
		public ClanFinanceAlleyItemVM CurrentSelectedAlley
		{
			get
			{
				return this._currentSelectedAlley;
			}
			set
			{
				if (value != this._currentSelectedAlley)
				{
					this._currentSelectedAlley = value;
					base.OnPropertyChangedWithValue<ClanFinanceAlleyItemVM>(value, "CurrentSelectedAlley");
					this.IsAnyValidAlleySelected = value != null;
					this.IsAnyValidIncomeSelected = false;
					this.IsAnyValidSupporterSelected = false;
				}
			}
		}

		// Token: 0x170009F7 RID: 2551
		// (get) Token: 0x06001D5F RID: 7519 RVA: 0x0006C9AF File Offset: 0x0006ABAF
		// (set) Token: 0x06001D60 RID: 7520 RVA: 0x0006C9B7 File Offset: 0x0006ABB7
		[DataSourceProperty]
		public ClanFinanceWorkshopItemVM CurrentSelectedIncome
		{
			get
			{
				return this._currentSelectedIncome;
			}
			set
			{
				if (value != this._currentSelectedIncome)
				{
					this._currentSelectedIncome = value;
					base.OnPropertyChangedWithValue<ClanFinanceWorkshopItemVM>(value, "CurrentSelectedIncome");
					this.IsAnyValidIncomeSelected = value != null;
					this.IsAnyValidSupporterSelected = false;
					this.IsAnyValidAlleySelected = false;
				}
			}
		}

		// Token: 0x170009F8 RID: 2552
		// (get) Token: 0x06001D61 RID: 7521 RVA: 0x0006C9ED File Offset: 0x0006ABED
		// (set) Token: 0x06001D62 RID: 7522 RVA: 0x0006C9F5 File Offset: 0x0006ABF5
		[DataSourceProperty]
		public ClanSupporterGroupVM CurrentSelectedSupporterGroup
		{
			get
			{
				return this._currentSelectedSupporterGroup;
			}
			set
			{
				if (value != this._currentSelectedSupporterGroup)
				{
					this._currentSelectedSupporterGroup = value;
					base.OnPropertyChangedWithValue<ClanSupporterGroupVM>(value, "CurrentSelectedSupporterGroup");
					this.IsAnyValidSupporterSelected = value != null;
					this.IsAnyValidIncomeSelected = false;
					this.IsAnyValidAlleySelected = false;
				}
			}
		}

		// Token: 0x170009F9 RID: 2553
		// (get) Token: 0x06001D63 RID: 7523 RVA: 0x0006CA2B File Offset: 0x0006AC2B
		// (set) Token: 0x06001D64 RID: 7524 RVA: 0x0006CA33 File Offset: 0x0006AC33
		[DataSourceProperty]
		public bool IsAnyValidAlleySelected
		{
			get
			{
				return this._isAnyValidAlleySelected;
			}
			set
			{
				if (value != this._isAnyValidAlleySelected)
				{
					this._isAnyValidAlleySelected = value;
					base.OnPropertyChangedWithValue(value, "IsAnyValidAlleySelected");
				}
			}
		}

		// Token: 0x170009FA RID: 2554
		// (get) Token: 0x06001D65 RID: 7525 RVA: 0x0006CA51 File Offset: 0x0006AC51
		// (set) Token: 0x06001D66 RID: 7526 RVA: 0x0006CA59 File Offset: 0x0006AC59
		[DataSourceProperty]
		public bool IsAnyValidIncomeSelected
		{
			get
			{
				return this._isAnyValidIncomeSelected;
			}
			set
			{
				if (value != this._isAnyValidIncomeSelected)
				{
					this._isAnyValidIncomeSelected = value;
					base.OnPropertyChangedWithValue(value, "IsAnyValidIncomeSelected");
				}
			}
		}

		// Token: 0x170009FB RID: 2555
		// (get) Token: 0x06001D67 RID: 7527 RVA: 0x0006CA77 File Offset: 0x0006AC77
		// (set) Token: 0x06001D68 RID: 7528 RVA: 0x0006CA7F File Offset: 0x0006AC7F
		[DataSourceProperty]
		public bool IsAnyValidSupporterSelected
		{
			get
			{
				return this._isAnyValidSupporterSelected;
			}
			set
			{
				if (value != this._isAnyValidSupporterSelected)
				{
					this._isAnyValidSupporterSelected = value;
					base.OnPropertyChangedWithValue(value, "IsAnyValidSupporterSelected");
				}
			}
		}

		// Token: 0x170009FC RID: 2556
		// (get) Token: 0x06001D69 RID: 7529 RVA: 0x0006CA9D File Offset: 0x0006AC9D
		// (set) Token: 0x06001D6A RID: 7530 RVA: 0x0006CAA5 File Offset: 0x0006ACA5
		[DataSourceProperty]
		public string IncomeText
		{
			get
			{
				return this._incomeText;
			}
			set
			{
				if (value != this._incomeText)
				{
					this._incomeText = value;
					base.OnPropertyChangedWithValue<string>(value, "IncomeText");
				}
			}
		}

		// Token: 0x170009FD RID: 2557
		// (get) Token: 0x06001D6B RID: 7531 RVA: 0x0006CAC8 File Offset: 0x0006ACC8
		// (set) Token: 0x06001D6C RID: 7532 RVA: 0x0006CAD0 File Offset: 0x0006ACD0
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

		// Token: 0x170009FE RID: 2558
		// (get) Token: 0x06001D6D RID: 7533 RVA: 0x0006CAEE File Offset: 0x0006ACEE
		// (set) Token: 0x06001D6E RID: 7534 RVA: 0x0006CAF6 File Offset: 0x0006ACF6
		[DataSourceProperty]
		public string NameText
		{
			get
			{
				return this._nameText;
			}
			set
			{
				if (value != this._nameText)
				{
					this._nameText = value;
					base.OnPropertyChangedWithValue<string>(value, "NameText");
				}
			}
		}

		// Token: 0x170009FF RID: 2559
		// (get) Token: 0x06001D6F RID: 7535 RVA: 0x0006CB19 File Offset: 0x0006AD19
		// (set) Token: 0x06001D70 RID: 7536 RVA: 0x0006CB21 File Offset: 0x0006AD21
		[DataSourceProperty]
		public string LocationText
		{
			get
			{
				return this._locationText;
			}
			set
			{
				if (value != this._locationText)
				{
					this._locationText = value;
					base.OnPropertyChangedWithValue<string>(value, "LocationText");
				}
			}
		}

		// Token: 0x17000A00 RID: 2560
		// (get) Token: 0x06001D71 RID: 7537 RVA: 0x0006CB44 File Offset: 0x0006AD44
		// (set) Token: 0x06001D72 RID: 7538 RVA: 0x0006CB4C File Offset: 0x0006AD4C
		[DataSourceProperty]
		public string WorkshopText
		{
			get
			{
				return this._workshopsText;
			}
			set
			{
				if (value != this._workshopsText)
				{
					this._workshopsText = value;
					base.OnPropertyChangedWithValue<string>(value, "WorkshopText");
				}
			}
		}

		// Token: 0x17000A01 RID: 2561
		// (get) Token: 0x06001D73 RID: 7539 RVA: 0x0006CB6F File Offset: 0x0006AD6F
		// (set) Token: 0x06001D74 RID: 7540 RVA: 0x0006CB77 File Offset: 0x0006AD77
		[DataSourceProperty]
		public string SupportersText
		{
			get
			{
				return this._supportersText;
			}
			set
			{
				if (value != this._supportersText)
				{
					this._supportersText = value;
					base.OnPropertyChangedWithValue<string>(value, "SupportersText");
				}
			}
		}

		// Token: 0x17000A02 RID: 2562
		// (get) Token: 0x06001D75 RID: 7541 RVA: 0x0006CB9A File Offset: 0x0006AD9A
		// (set) Token: 0x06001D76 RID: 7542 RVA: 0x0006CBA2 File Offset: 0x0006ADA2
		[DataSourceProperty]
		public string AlleysText
		{
			get
			{
				return this._alleysText;
			}
			set
			{
				if (value != this._alleysText)
				{
					this._alleysText = value;
					base.OnPropertyChangedWithValue<string>(value, "AlleysText");
				}
			}
		}

		// Token: 0x17000A03 RID: 2563
		// (get) Token: 0x06001D77 RID: 7543 RVA: 0x0006CBC5 File Offset: 0x0006ADC5
		// (set) Token: 0x06001D78 RID: 7544 RVA: 0x0006CBCD File Offset: 0x0006ADCD
		[DataSourceProperty]
		public string NoAdditionalIncomesText
		{
			get
			{
				return this._noAdditionalIncomesText;
			}
			set
			{
				if (this._noAdditionalIncomesText != value)
				{
					this._noAdditionalIncomesText = value;
					base.OnPropertyChangedWithValue<string>(value, "NoAdditionalIncomesText");
				}
			}
		}

		// Token: 0x17000A04 RID: 2564
		// (get) Token: 0x06001D79 RID: 7545 RVA: 0x0006CBF0 File Offset: 0x0006ADF0
		// (set) Token: 0x06001D7A RID: 7546 RVA: 0x0006CBF8 File Offset: 0x0006ADF8
		[DataSourceProperty]
		public MBBindingList<ClanFinanceWorkshopItemVM> Incomes
		{
			get
			{
				return this._incomes;
			}
			set
			{
				if (value != this._incomes)
				{
					this._incomes = value;
					base.OnPropertyChangedWithValue<MBBindingList<ClanFinanceWorkshopItemVM>>(value, "Incomes");
				}
			}
		}

		// Token: 0x17000A05 RID: 2565
		// (get) Token: 0x06001D7B RID: 7547 RVA: 0x0006CC16 File Offset: 0x0006AE16
		// (set) Token: 0x06001D7C RID: 7548 RVA: 0x0006CC1E File Offset: 0x0006AE1E
		[DataSourceProperty]
		public MBBindingList<ClanSupporterGroupVM> SupporterGroups
		{
			get
			{
				return this._supporterGroups;
			}
			set
			{
				if (value != this._supporterGroups)
				{
					this._supporterGroups = value;
					base.OnPropertyChangedWithValue<MBBindingList<ClanSupporterGroupVM>>(value, "SupporterGroups");
				}
			}
		}

		// Token: 0x17000A06 RID: 2566
		// (get) Token: 0x06001D7D RID: 7549 RVA: 0x0006CC3C File Offset: 0x0006AE3C
		// (set) Token: 0x06001D7E RID: 7550 RVA: 0x0006CC44 File Offset: 0x0006AE44
		[DataSourceProperty]
		public MBBindingList<ClanFinanceAlleyItemVM> Alleys
		{
			get
			{
				return this._alleys;
			}
			set
			{
				if (value != this._alleys)
				{
					this._alleys = value;
					base.OnPropertyChangedWithValue<MBBindingList<ClanFinanceAlleyItemVM>>(value, "Alleys");
				}
			}
		}

		// Token: 0x17000A07 RID: 2567
		// (get) Token: 0x06001D7F RID: 7551 RVA: 0x0006CC62 File Offset: 0x0006AE62
		// (set) Token: 0x06001D80 RID: 7552 RVA: 0x0006CC6A File Offset: 0x0006AE6A
		[DataSourceProperty]
		public ClanIncomeSortControllerVM SortController
		{
			get
			{
				return this._sortController;
			}
			set
			{
				if (value != this._sortController)
				{
					this._sortController = value;
					base.OnPropertyChangedWithValue<ClanIncomeSortControllerVM>(value, "SortController");
				}
			}
		}

		// Token: 0x04000DB4 RID: 3508
		private readonly Action _onRefresh;

		// Token: 0x04000DB5 RID: 3509
		private readonly Action<ClanCardSelectionInfo> _openCardSelectionPopup;

		// Token: 0x04000DB7 RID: 3511
		private MBBindingList<ClanFinanceWorkshopItemVM> _incomes;

		// Token: 0x04000DB8 RID: 3512
		private MBBindingList<ClanSupporterGroupVM> _supporterGroups;

		// Token: 0x04000DB9 RID: 3513
		private MBBindingList<ClanFinanceAlleyItemVM> _alleys;

		// Token: 0x04000DBA RID: 3514
		private ClanFinanceAlleyItemVM _currentSelectedAlley;

		// Token: 0x04000DBB RID: 3515
		private ClanFinanceWorkshopItemVM _currentSelectedIncome;

		// Token: 0x04000DBC RID: 3516
		private ClanSupporterGroupVM _currentSelectedSupporterGroup;

		// Token: 0x04000DBD RID: 3517
		private bool _isSelected;

		// Token: 0x04000DBE RID: 3518
		private string _nameText;

		// Token: 0x04000DBF RID: 3519
		private string _incomeText;

		// Token: 0x04000DC0 RID: 3520
		private string _locationText;

		// Token: 0x04000DC1 RID: 3521
		private string _workshopsText;

		// Token: 0x04000DC2 RID: 3522
		private string _supportersText;

		// Token: 0x04000DC3 RID: 3523
		private string _alleysText;

		// Token: 0x04000DC4 RID: 3524
		private string _noAdditionalIncomesText;

		// Token: 0x04000DC5 RID: 3525
		private bool _isAnyValidAlleySelected;

		// Token: 0x04000DC6 RID: 3526
		private bool _isAnyValidIncomeSelected;

		// Token: 0x04000DC7 RID: 3527
		private bool _isAnyValidSupporterSelected;

		// Token: 0x04000DC8 RID: 3528
		private ClanIncomeSortControllerVM _sortController;
	}
}
