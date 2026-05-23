using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Library.EventSystem;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Decisions
{
	// Token: 0x02000075 RID: 117
	public class DecisionOptionVM : ViewModel
	{
		// Token: 0x170002CB RID: 715
		// (get) Token: 0x06000973 RID: 2419 RVA: 0x00029C4E File Offset: 0x00027E4E
		// (set) Token: 0x06000974 RID: 2420 RVA: 0x00029C56 File Offset: 0x00027E56
		public DecisionOutcome Option { get; private set; }

		// Token: 0x170002CC RID: 716
		// (get) Token: 0x06000975 RID: 2421 RVA: 0x00029C5F File Offset: 0x00027E5F
		// (set) Token: 0x06000976 RID: 2422 RVA: 0x00029C67 File Offset: 0x00027E67
		public KingdomDecision Decision { get; private set; }

		// Token: 0x06000977 RID: 2423 RVA: 0x00029C70 File Offset: 0x00027E70
		public DecisionOptionVM(DecisionOutcome option, KingdomDecision decision, KingdomElection kingdomDecisionMaker, Action<DecisionOptionVM> onSelect, Action<DecisionOptionVM> onSupportStrengthChange)
		{
			this._onSelect = onSelect;
			this._onSupportStrengthChange = onSupportStrengthChange;
			this._kingdomDecisionMaker = kingdomDecisionMaker;
			this.Decision = decision;
			this.CurrentSupportWeight = Supporter.SupportWeights.Choose;
			this.OptionHint = new HintViewModel();
			this.IsPlayerSupporter = !this._kingdomDecisionMaker.IsPlayerChooser;
			this.SupportersOfThisOption = new MBBindingList<DecisionSupporterVM>();
			this.Option = option;
			if (option != null)
			{
				Clan sponsorClan = option.SponsorClan;
				if (((sponsorClan != null) ? sponsorClan.Leader : null) != null)
				{
					this.Sponsor = new HeroVM(option.SponsorClan.Leader, false);
				}
				List<Supporter> supporterList = option.SupporterList;
				if (supporterList != null && supporterList.Count > 0)
				{
					foreach (Supporter supporter in option.SupporterList)
					{
						if (supporter.SupportWeight > Supporter.SupportWeights.StayNeutral)
						{
							if (supporter.Clan != option.SponsorClan)
							{
								this.SupportersOfThisOption.Add(new DecisionSupporterVM(supporter.Name, supporter.ImagePath, supporter.Clan, supporter.SupportWeight));
							}
							else
							{
								this.SponsorWeightImagePath = DecisionSupporterVM.GetSupporterWeightImagePath(supporter.SupportWeight);
							}
						}
					}
				}
				this.IsOptionForAbstain = false;
			}
			else
			{
				this.IsOptionForAbstain = true;
			}
			this.RefreshValues();
			this.RefreshSupportOptionEnabled();
			this.RefreshCanChooseOption();
		}

		// Token: 0x06000978 RID: 2424 RVA: 0x00029DE8 File Offset: 0x00027FE8
		public override void RefreshValues()
		{
			base.RefreshValues();
			if (this.Option != null)
			{
				this.Name = this.Option.GetDecisionTitle().ToString();
				this.Description = this.Option.GetDecisionDescription().ToString();
			}
			else
			{
				this.Name = GameTexts.FindText("str_abstain", null).ToString();
				this.Description = GameTexts.FindText("str_kingdom_decision_abstain_desc", null).ToString();
			}
			MBBindingList<DecisionSupporterVM> supportersOfThisOption = this.SupportersOfThisOption;
			if (supportersOfThisOption == null)
			{
				return;
			}
			supportersOfThisOption.ApplyActionOnAllItems(delegate(DecisionSupporterVM x)
			{
				x.RefreshValues();
			});
		}

		// Token: 0x06000979 RID: 2425 RVA: 0x00029E8C File Offset: 0x0002808C
		private void ExecuteShowSupporterTooltip()
		{
			DecisionOutcome option = this.Option;
			if (option != null && option.SupporterList.Count > 0)
			{
				List<TooltipProperty> list = new List<TooltipProperty>();
				this._kingdomDecisionMaker.DetermineOfficialSupport();
				foreach (Supporter supporter in this.Option.SupporterList)
				{
					if (supporter.SupportWeight > Supporter.SupportWeights.StayNeutral && !supporter.IsPlayer)
					{
						int influenceCost = this.Decision.GetInfluenceCost(this.Option, supporter.Clan, supporter.SupportWeight);
						GameTexts.SetVariable("AMOUNT", influenceCost);
						GameTexts.SetVariable("INFLUENCE_ICON", "{=!}<img src=\"General\\Icons\\Influence@2x\" extend=\"7\">");
						list.Add(new TooltipProperty(supporter.Name.ToString(), GameTexts.FindText("str_amount_with_influence_icon", null).ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
					}
				}
				InformationManager.ShowTooltip(typeof(List<TooltipProperty>), new object[] { list });
			}
		}

		// Token: 0x0600097A RID: 2426 RVA: 0x00029F9C File Offset: 0x0002819C
		private void ExecuteHideSupporterTooltip()
		{
			MBInformationManager.HideInformations();
		}

		// Token: 0x0600097B RID: 2427 RVA: 0x00029FA4 File Offset: 0x000281A4
		private void RefreshSupportOptionEnabled()
		{
			int influenceCostOfOutcome = this._kingdomDecisionMaker.GetInfluenceCostOfOutcome(this.Option, Clan.PlayerClan, Supporter.SupportWeights.SlightlyFavor);
			int influenceCostOfOutcome2 = this._kingdomDecisionMaker.GetInfluenceCostOfOutcome(this.Option, Clan.PlayerClan, Supporter.SupportWeights.StronglyFavor);
			int influenceCostOfOutcome3 = this._kingdomDecisionMaker.GetInfluenceCostOfOutcome(this.Option, Clan.PlayerClan, Supporter.SupportWeights.FullyPush);
			this.SupportOption1Text = influenceCostOfOutcome.ToString();
			this.SupportOption2Text = influenceCostOfOutcome2.ToString();
			this.SupportOption3Text = influenceCostOfOutcome3.ToString();
			this.IsSupportOption1Enabled = (float)influenceCostOfOutcome <= Clan.PlayerClan.Influence && !this.IsOptionForAbstain;
			this.IsSupportOption2Enabled = (float)influenceCostOfOutcome2 <= Clan.PlayerClan.Influence && !this.IsOptionForAbstain;
			this.IsSupportOption3Enabled = (float)influenceCostOfOutcome3 <= Clan.PlayerClan.Influence && !this.IsOptionForAbstain;
		}

		// Token: 0x0600097C RID: 2428 RVA: 0x0002A080 File Offset: 0x00028280
		private void OnSupportStrengthChange(int index)
		{
			if (!this.IsOptionForAbstain)
			{
				switch (index)
				{
				case 0:
					this.CurrentSupportWeight = Supporter.SupportWeights.SlightlyFavor;
					break;
				case 1:
					this.CurrentSupportWeight = Supporter.SupportWeights.StronglyFavor;
					break;
				case 2:
					this.CurrentSupportWeight = Supporter.SupportWeights.FullyPush;
					break;
				}
				this._kingdomDecisionMaker.OnPlayerSupport((!this.IsOptionForAbstain) ? this.Option : null, this.CurrentSupportWeight);
				this._onSupportStrengthChange(this);
			}
		}

		// Token: 0x0600097D RID: 2429 RVA: 0x0002A0F0 File Offset: 0x000282F0
		public void AfterKingChooseOutcome()
		{
			this._hasKingChoosen = true;
			this.RefreshCanChooseOption();
		}

		// Token: 0x0600097E RID: 2430 RVA: 0x0002A100 File Offset: 0x00028300
		private void RefreshCanChooseOption()
		{
			if (this._hasKingChoosen)
			{
				this.CanBeChosen = false;
				return;
			}
			if (this.IsOptionForAbstain)
			{
				this.CanBeChosen = true;
				return;
			}
			if (this.IsPlayerSupporter)
			{
				this.CanBeChosen = (float)this._kingdomDecisionMaker.GetInfluenceCostOfOutcome(this.Option, Clan.PlayerClan, Supporter.SupportWeights.SlightlyFavor) <= Clan.PlayerClan.Influence;
			}
			else
			{
				int influenceCostOfOutcome = this._kingdomDecisionMaker.GetInfluenceCostOfOutcome(this.Option, Clan.PlayerClan, Supporter.SupportWeights.Choose);
				this.CanBeChosen = (float)influenceCostOfOutcome <= Clan.PlayerClan.Influence || influenceCostOfOutcome == 0;
			}
			this.OptionHint.HintText = (this.CanBeChosen ? TextObject.GetEmpty() : new TextObject("{=Xmw93W6a}Not Enough Influence", null));
		}

		// Token: 0x0600097F RID: 2431 RVA: 0x0002A1BC File Offset: 0x000283BC
		private void ExecuteSelection()
		{
			this._onSelect(this);
			Game game = Game.Current;
			if (game == null)
			{
				return;
			}
			EventManager eventManager = game.EventManager;
			if (eventManager == null)
			{
				return;
			}
			eventManager.TriggerEvent<PlayerSelectedAKingdomDecisionOptionEvent>(new PlayerSelectedAKingdomDecisionOptionEvent(this.Option));
		}

		// Token: 0x170002CD RID: 717
		// (get) Token: 0x06000980 RID: 2432 RVA: 0x0002A1EE File Offset: 0x000283EE
		// (set) Token: 0x06000981 RID: 2433 RVA: 0x0002A1F6 File Offset: 0x000283F6
		[DataSourceProperty]
		public HintViewModel OptionHint
		{
			get
			{
				return this._optionHint;
			}
			set
			{
				if (value != this._optionHint)
				{
					this._optionHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "OptionHint");
				}
			}
		}

		// Token: 0x170002CE RID: 718
		// (get) Token: 0x06000982 RID: 2434 RVA: 0x0002A214 File Offset: 0x00028414
		// (set) Token: 0x06000983 RID: 2435 RVA: 0x0002A21C File Offset: 0x0002841C
		[DataSourceProperty]
		public MBBindingList<DecisionSupporterVM> SupportersOfThisOption
		{
			get
			{
				return this._supportersOfThisOption;
			}
			set
			{
				if (value != this._supportersOfThisOption)
				{
					this._supportersOfThisOption = value;
					base.OnPropertyChangedWithValue<MBBindingList<DecisionSupporterVM>>(value, "SupportersOfThisOption");
				}
			}
		}

		// Token: 0x170002CF RID: 719
		// (get) Token: 0x06000984 RID: 2436 RVA: 0x0002A23A File Offset: 0x0002843A
		// (set) Token: 0x06000985 RID: 2437 RVA: 0x0002A242 File Offset: 0x00028442
		[DataSourceProperty]
		public HeroVM Sponsor
		{
			get
			{
				return this._sponsor;
			}
			set
			{
				if (value != this._sponsor)
				{
					this._sponsor = value;
					base.OnPropertyChangedWithValue<HeroVM>(value, "Sponsor");
				}
			}
		}

		// Token: 0x170002D0 RID: 720
		// (get) Token: 0x06000986 RID: 2438 RVA: 0x0002A260 File Offset: 0x00028460
		// (set) Token: 0x06000987 RID: 2439 RVA: 0x0002A268 File Offset: 0x00028468
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

		// Token: 0x170002D1 RID: 721
		// (get) Token: 0x06000988 RID: 2440 RVA: 0x0002A28B File Offset: 0x0002848B
		// (set) Token: 0x06000989 RID: 2441 RVA: 0x0002A293 File Offset: 0x00028493
		[DataSourceProperty]
		public string SponsorWeightImagePath
		{
			get
			{
				return this._sponsorWeightImagePath;
			}
			set
			{
				if (value != this._sponsorWeightImagePath)
				{
					this._sponsorWeightImagePath = value;
					base.OnPropertyChangedWithValue<string>(value, "SponsorWeightImagePath");
				}
			}
		}

		// Token: 0x170002D2 RID: 722
		// (get) Token: 0x0600098A RID: 2442 RVA: 0x0002A2B6 File Offset: 0x000284B6
		// (set) Token: 0x0600098B RID: 2443 RVA: 0x0002A2BE File Offset: 0x000284BE
		[DataSourceProperty]
		public bool CanBeChosen
		{
			get
			{
				return this._canBeChosen;
			}
			set
			{
				if (value != this._canBeChosen)
				{
					this._canBeChosen = value;
					base.OnPropertyChangedWithValue(value, "CanBeChosen");
				}
			}
		}

		// Token: 0x170002D3 RID: 723
		// (get) Token: 0x0600098C RID: 2444 RVA: 0x0002A2DC File Offset: 0x000284DC
		// (set) Token: 0x0600098D RID: 2445 RVA: 0x0002A2E4 File Offset: 0x000284E4
		[DataSourceProperty]
		public bool IsKingsOutcome
		{
			get
			{
				return this._isKingsOutcome;
			}
			set
			{
				if (value != this._isKingsOutcome)
				{
					this._isKingsOutcome = value;
					base.OnPropertyChangedWithValue(value, "IsKingsOutcome");
				}
			}
		}

		// Token: 0x170002D4 RID: 724
		// (get) Token: 0x0600098E RID: 2446 RVA: 0x0002A302 File Offset: 0x00028502
		// (set) Token: 0x0600098F RID: 2447 RVA: 0x0002A30A File Offset: 0x0002850A
		[DataSourceProperty]
		public bool IsPlayerSupporter
		{
			get
			{
				return this._isPlayerSupporter;
			}
			set
			{
				if (value != this._isPlayerSupporter)
				{
					this._isPlayerSupporter = value;
					base.OnPropertyChangedWithValue(value, "IsPlayerSupporter");
				}
			}
		}

		// Token: 0x170002D5 RID: 725
		// (get) Token: 0x06000990 RID: 2448 RVA: 0x0002A328 File Offset: 0x00028528
		// (set) Token: 0x06000991 RID: 2449 RVA: 0x0002A330 File Offset: 0x00028530
		[DataSourceProperty]
		public bool IsHighlightEnabled
		{
			get
			{
				return this._isHighlightEnabled;
			}
			set
			{
				if (value != this._isHighlightEnabled)
				{
					this._isHighlightEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsHighlightEnabled");
				}
			}
		}

		// Token: 0x170002D6 RID: 726
		// (get) Token: 0x06000992 RID: 2450 RVA: 0x0002A34E File Offset: 0x0002854E
		// (set) Token: 0x06000993 RID: 2451 RVA: 0x0002A356 File Offset: 0x00028556
		[DataSourceProperty]
		public int WinPercentage
		{
			get
			{
				return this._winPercentage;
			}
			set
			{
				if (value != this._winPercentage)
				{
					this._winPercentage = value;
					base.OnPropertyChangedWithValue(value, "WinPercentage");
					GameTexts.SetVariable("NUMBER", value);
					this.WinPercentageStr = GameTexts.FindText("str_NUMBER_percent", null).ToString();
				}
			}
		}

		// Token: 0x170002D7 RID: 727
		// (get) Token: 0x06000994 RID: 2452 RVA: 0x0002A395 File Offset: 0x00028595
		// (set) Token: 0x06000995 RID: 2453 RVA: 0x0002A39D File Offset: 0x0002859D
		[DataSourceProperty]
		public string WinPercentageStr
		{
			get
			{
				return this._winPercentageStr;
			}
			set
			{
				if (value != this._winPercentageStr)
				{
					this._winPercentageStr = value;
					base.OnPropertyChangedWithValue<string>(value, "WinPercentageStr");
				}
			}
		}

		// Token: 0x170002D8 RID: 728
		// (get) Token: 0x06000996 RID: 2454 RVA: 0x0002A3C0 File Offset: 0x000285C0
		// (set) Token: 0x06000997 RID: 2455 RVA: 0x0002A3C8 File Offset: 0x000285C8
		[DataSourceProperty]
		public string Description
		{
			get
			{
				return this._description;
			}
			set
			{
				if (value != this._description)
				{
					this._description = value;
					base.OnPropertyChangedWithValue<string>(value, "Description");
				}
			}
		}

		// Token: 0x170002D9 RID: 729
		// (get) Token: 0x06000998 RID: 2456 RVA: 0x0002A3EB File Offset: 0x000285EB
		// (set) Token: 0x06000999 RID: 2457 RVA: 0x0002A3F3 File Offset: 0x000285F3
		[DataSourceProperty]
		public int InitialPercentage
		{
			get
			{
				return this._initialPercentage;
			}
			set
			{
				if (value != this._initialPercentage)
				{
					this._initialPercentage = value;
					base.OnPropertyChangedWithValue(value, "InitialPercentage");
				}
			}
		}

		// Token: 0x170002DA RID: 730
		// (get) Token: 0x0600099A RID: 2458 RVA: 0x0002A411 File Offset: 0x00028611
		// (set) Token: 0x0600099B RID: 2459 RVA: 0x0002A419 File Offset: 0x00028619
		[DataSourceProperty]
		public int InfluenceCost
		{
			get
			{
				return this._influenceCost;
			}
			set
			{
				if (value != this._influenceCost)
				{
					this._influenceCost = value;
					base.OnPropertyChangedWithValue(value, "InfluenceCost");
				}
			}
		}

		// Token: 0x170002DB RID: 731
		// (get) Token: 0x0600099C RID: 2460 RVA: 0x0002A437 File Offset: 0x00028637
		// (set) Token: 0x0600099D RID: 2461 RVA: 0x0002A43F File Offset: 0x0002863F
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

		// Token: 0x170002DC RID: 732
		// (get) Token: 0x0600099E RID: 2462 RVA: 0x0002A45D File Offset: 0x0002865D
		// (set) Token: 0x0600099F RID: 2463 RVA: 0x0002A465 File Offset: 0x00028665
		[DataSourceProperty]
		public bool IsOptionForAbstain
		{
			get
			{
				return this._isOptionForAbstain;
			}
			set
			{
				if (value != this._isOptionForAbstain)
				{
					this._isOptionForAbstain = value;
					base.OnPropertyChangedWithValue(value, "IsOptionForAbstain");
				}
			}
		}

		// Token: 0x170002DD RID: 733
		// (get) Token: 0x060009A0 RID: 2464 RVA: 0x0002A483 File Offset: 0x00028683
		// (set) Token: 0x060009A1 RID: 2465 RVA: 0x0002A48B File Offset: 0x0002868B
		[DataSourceProperty]
		public Supporter.SupportWeights CurrentSupportWeight
		{
			get
			{
				return this._currentSupportWeight;
			}
			set
			{
				if (value != this._currentSupportWeight)
				{
					this._currentSupportWeight = value;
					base.OnPropertyChanged("CurrentSupportWeight");
					this.CurrentSupportWeightIndex = (int)value;
				}
			}
		}

		// Token: 0x170002DE RID: 734
		// (get) Token: 0x060009A2 RID: 2466 RVA: 0x0002A4AF File Offset: 0x000286AF
		// (set) Token: 0x060009A3 RID: 2467 RVA: 0x0002A4B7 File Offset: 0x000286B7
		[DataSourceProperty]
		public int CurrentSupportWeightIndex
		{
			get
			{
				return this._currentSupportWeightIndex;
			}
			set
			{
				if (value != this._currentSupportWeightIndex)
				{
					this._currentSupportWeightIndex = value;
					base.OnPropertyChangedWithValue(value, "CurrentSupportWeightIndex");
				}
			}
		}

		// Token: 0x170002DF RID: 735
		// (get) Token: 0x060009A4 RID: 2468 RVA: 0x0002A4D5 File Offset: 0x000286D5
		// (set) Token: 0x060009A5 RID: 2469 RVA: 0x0002A4DD File Offset: 0x000286DD
		[DataSourceProperty]
		public string SupportOption1Text
		{
			get
			{
				return this._supportOption1Text;
			}
			set
			{
				if (value != this._supportOption1Text)
				{
					this._supportOption1Text = value;
					base.OnPropertyChangedWithValue<string>(value, "SupportOption1Text");
				}
			}
		}

		// Token: 0x170002E0 RID: 736
		// (get) Token: 0x060009A6 RID: 2470 RVA: 0x0002A500 File Offset: 0x00028700
		// (set) Token: 0x060009A7 RID: 2471 RVA: 0x0002A508 File Offset: 0x00028708
		[DataSourceProperty]
		public string SupportOption2Text
		{
			get
			{
				return this._supportOption2Text;
			}
			set
			{
				if (value != this._supportOption2Text)
				{
					this._supportOption2Text = value;
					base.OnPropertyChangedWithValue<string>(value, "SupportOption2Text");
				}
			}
		}

		// Token: 0x170002E1 RID: 737
		// (get) Token: 0x060009A8 RID: 2472 RVA: 0x0002A52B File Offset: 0x0002872B
		// (set) Token: 0x060009A9 RID: 2473 RVA: 0x0002A533 File Offset: 0x00028733
		[DataSourceProperty]
		public string SupportOption3Text
		{
			get
			{
				return this._supportOption3Text;
			}
			set
			{
				if (value != this._supportOption3Text)
				{
					this._supportOption3Text = value;
					base.OnPropertyChangedWithValue<string>(value, "SupportOption3Text");
				}
			}
		}

		// Token: 0x170002E2 RID: 738
		// (get) Token: 0x060009AA RID: 2474 RVA: 0x0002A556 File Offset: 0x00028756
		// (set) Token: 0x060009AB RID: 2475 RVA: 0x0002A55E File Offset: 0x0002875E
		[DataSourceProperty]
		public bool IsSupportOption1Enabled
		{
			get
			{
				return this._isSupportOption1Enabled;
			}
			set
			{
				if (value != this._isSupportOption1Enabled)
				{
					this._isSupportOption1Enabled = value;
					base.OnPropertyChangedWithValue(value, "IsSupportOption1Enabled");
				}
			}
		}

		// Token: 0x170002E3 RID: 739
		// (get) Token: 0x060009AC RID: 2476 RVA: 0x0002A57C File Offset: 0x0002877C
		// (set) Token: 0x060009AD RID: 2477 RVA: 0x0002A584 File Offset: 0x00028784
		[DataSourceProperty]
		public bool IsSupportOption2Enabled
		{
			get
			{
				return this._isSupportOption2Enabled;
			}
			set
			{
				if (value != this._isSupportOption2Enabled)
				{
					this._isSupportOption2Enabled = value;
					base.OnPropertyChangedWithValue(value, "IsSupportOption2Enabled");
				}
			}
		}

		// Token: 0x170002E4 RID: 740
		// (get) Token: 0x060009AE RID: 2478 RVA: 0x0002A5A2 File Offset: 0x000287A2
		// (set) Token: 0x060009AF RID: 2479 RVA: 0x0002A5AA File Offset: 0x000287AA
		[DataSourceProperty]
		public bool IsSupportOption3Enabled
		{
			get
			{
				return this._isSupportOption3Enabled;
			}
			set
			{
				if (value != this._isSupportOption3Enabled)
				{
					this._isSupportOption3Enabled = value;
					base.OnPropertyChangedWithValue(value, "IsSupportOption3Enabled");
				}
			}
		}

		// Token: 0x0400042C RID: 1068
		private readonly Action<DecisionOptionVM> _onSelect;

		// Token: 0x0400042D RID: 1069
		private readonly Action<DecisionOptionVM> _onSupportStrengthChange;

		// Token: 0x0400042E RID: 1070
		private readonly KingdomElection _kingdomDecisionMaker;

		// Token: 0x0400042F RID: 1071
		private bool _hasKingChoosen;

		// Token: 0x04000430 RID: 1072
		private MBBindingList<DecisionSupporterVM> _supportersOfThisOption;

		// Token: 0x04000431 RID: 1073
		private HeroVM _sponsor;

		// Token: 0x04000432 RID: 1074
		private bool _isOptionForAbstain;

		// Token: 0x04000433 RID: 1075
		private bool _isPlayerSupporter;

		// Token: 0x04000434 RID: 1076
		private bool _isSelected;

		// Token: 0x04000435 RID: 1077
		private bool _canBeChosen;

		// Token: 0x04000436 RID: 1078
		private bool _isKingsOutcome;

		// Token: 0x04000437 RID: 1079
		private bool _isHighlightEnabled;

		// Token: 0x04000438 RID: 1080
		private int _winPercentage = -1;

		// Token: 0x04000439 RID: 1081
		private int _influenceCost;

		// Token: 0x0400043A RID: 1082
		private int _initialPercentage = -99;

		// Token: 0x0400043B RID: 1083
		private int _currentSupportWeightIndex;

		// Token: 0x0400043C RID: 1084
		private string _name;

		// Token: 0x0400043D RID: 1085
		private string _description;

		// Token: 0x0400043E RID: 1086
		private string _winPercentageStr;

		// Token: 0x0400043F RID: 1087
		private string _sponsorWeightImagePath;

		// Token: 0x04000440 RID: 1088
		private Supporter.SupportWeights _currentSupportWeight;

		// Token: 0x04000441 RID: 1089
		private string _supportOption1Text;

		// Token: 0x04000442 RID: 1090
		private bool _isSupportOption1Enabled;

		// Token: 0x04000443 RID: 1091
		private string _supportOption2Text;

		// Token: 0x04000444 RID: 1092
		private bool _isSupportOption2Enabled;

		// Token: 0x04000445 RID: 1093
		private string _supportOption3Text;

		// Token: 0x04000446 RID: 1094
		private bool _isSupportOption3Enabled;

		// Token: 0x04000447 RID: 1095
		private HintViewModel _optionHint;
	}
}
