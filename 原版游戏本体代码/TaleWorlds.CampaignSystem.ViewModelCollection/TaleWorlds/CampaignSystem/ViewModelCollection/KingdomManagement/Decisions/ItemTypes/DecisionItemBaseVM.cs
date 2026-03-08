using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Core.ViewModelCollection.Tutorial;
using TaleWorlds.Library;
using TaleWorlds.Library.EventSystem;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Decisions.ItemTypes
{
	// Token: 0x0200007A RID: 122
	public class DecisionItemBaseVM : ViewModel
	{
		// Token: 0x170002FE RID: 766
		// (get) Token: 0x060009F4 RID: 2548 RVA: 0x0002B36B File Offset: 0x0002956B
		// (set) Token: 0x060009F5 RID: 2549 RVA: 0x0002B373 File Offset: 0x00029573
		public KingdomElection KingdomDecisionMaker { get; private set; }

		// Token: 0x170002FF RID: 767
		// (get) Token: 0x060009F6 RID: 2550 RVA: 0x0002B37C File Offset: 0x0002957C
		private float _currentInfluenceCost
		{
			get
			{
				if (this._currentSelectedOption != null && !this._currentSelectedOption.IsOptionForAbstain)
				{
					if (!this.IsPlayerSupporter)
					{
						return (float)Campaign.Current.Models.ClanPoliticsModel.GetInfluenceRequiredToOverrideKingdomDecision(this.KingdomDecisionMaker.PossibleOutcomes.MaxBy((DecisionOutcome o) => o.WinChance), this._currentSelectedOption.Option, this._decision);
					}
					if (this._currentSelectedOption.CurrentSupportWeight != Supporter.SupportWeights.Choose)
					{
						return (float)this.KingdomDecisionMaker.GetInfluenceCostOfOutcome(this._currentSelectedOption.Option, Clan.PlayerClan, this._currentSelectedOption.CurrentSupportWeight);
					}
				}
				return 0f;
			}
		}

		// Token: 0x060009F7 RID: 2551 RVA: 0x0002B43C File Offset: 0x0002963C
		public DecisionItemBaseVM(KingdomDecision decision, Action onDecisionOver)
		{
			this._decision = decision;
			this._onDecisionOver = onDecisionOver;
			this.DecisionType = 0;
			this.DecisionOptionsList = new MBBindingList<DecisionOptionVM>();
			this.EndDecisionHint = new HintViewModel();
			CampaignEvents.KingdomDecisionConcluded.AddNonSerializedListener(this, new Action<KingdomDecision, DecisionOutcome, bool>(this.OnKingdomDecisionConcluded));
			this.RefreshValues();
			this.InitValues();
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
			eventManager.RegisterEvent<TutorialNotificationElementChangeEvent>(new Action<TutorialNotificationElementChangeEvent>(this.OnTutorialNotificationElementIDChange));
		}

		// Token: 0x060009F8 RID: 2552 RVA: 0x0002B4D4 File Offset: 0x000296D4
		private void OnKingdomDecisionConcluded(KingdomDecision decision, DecisionOutcome outcome, bool isPlayerInvolved)
		{
			if (decision == this._decision)
			{
				this.IsKingsDecisionOver = true;
				this.CurrentStageIndex = 1;
				foreach (DecisionOptionVM decisionOptionVM in this.DecisionOptionsList)
				{
					if (decisionOptionVM.Option == outcome)
					{
						decisionOptionVM.IsKingsOutcome = true;
					}
					decisionOptionVM.AfterKingChooseOutcome();
				}
			}
		}

		// Token: 0x060009F9 RID: 2553 RVA: 0x0002B548 File Offset: 0x00029748
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.DoneText = GameTexts.FindText("str_done", null).ToString();
			GameTexts.SetVariable("TOTAL_INFLUENCE", MathF.Round(Hero.MainHero.Clan.Influence));
			this.TotalInfluenceText = GameTexts.FindText("str_total_influence", null).ToString();
			this.RefreshInfluenceCost();
			MBBindingList<DecisionOptionVM> decisionOptionsList = this.DecisionOptionsList;
			if (decisionOptionsList == null)
			{
				return;
			}
			decisionOptionsList.ApplyActionOnAllItems(delegate(DecisionOptionVM x)
			{
				x.RefreshValues();
			});
		}

		// Token: 0x060009FA RID: 2554 RVA: 0x0002B5DC File Offset: 0x000297DC
		protected virtual void InitValues()
		{
			this.DecisionOptionsList.Clear();
			this.KingdomDecisionMaker = new KingdomElection(this._decision);
			this.KingdomDecisionMaker.StartElection();
			this.CurrentStageIndex = ((!this.KingdomDecisionMaker.IsPlayerChooser) ? 0 : 1);
			this.IsPlayerSupporter = !this.KingdomDecisionMaker.IsPlayerChooser;
			this.KingdomDecisionMaker.DetermineOfficialSupport();
			foreach (DecisionOutcome decisionOutcome in this.KingdomDecisionMaker.PossibleOutcomes)
			{
				DecisionOptionVM item = new DecisionOptionVM(decisionOutcome, this._decision, this.KingdomDecisionMaker, new Action<DecisionOptionVM>(this.OnChangeVote), new Action<DecisionOptionVM>(this.OnSupportStrengthChange))
				{
					WinPercentage = MathF.Round(decisionOutcome.WinChance * 100f),
					InitialPercentage = MathF.Round(decisionOutcome.WinChance * 100f)
				};
				this.DecisionOptionsList.Add(item);
			}
			if (this.IsPlayerSupporter)
			{
				DecisionOptionVM item2 = new DecisionOptionVM(null, null, this.KingdomDecisionMaker, new Action<DecisionOptionVM>(this.OnAbstain), new Action<DecisionOptionVM>(this.OnSupportStrengthChange));
				this.DecisionOptionsList.Add(item2);
			}
			this.TitleText = this.KingdomDecisionMaker.GetTitle().ToString();
			this.DescriptionText = this.KingdomDecisionMaker.GetDescription().ToString();
			this.RefreshInfluenceCost();
			this.RefreshCanEndDecision();
			this.RefreshRelationChangeText();
			this.IsActive = true;
		}

		// Token: 0x060009FB RID: 2555 RVA: 0x0002B770 File Offset: 0x00029970
		private void OnChangeVote(DecisionOptionVM target)
		{
			if (this._currentSelectedOption != target)
			{
				if (this._currentSelectedOption != null)
				{
					this._currentSelectedOption.IsSelected = false;
				}
				this._currentSelectedOption = target;
				this._currentSelectedOption.IsSelected = true;
				this.KingdomDecisionMaker.OnPlayerSupport((!this._currentSelectedOption.IsOptionForAbstain) ? this._currentSelectedOption.Option : null, this._currentSelectedOption.CurrentSupportWeight);
				this.RefreshWinPercentages();
				this.RefreshInfluenceCost();
				this.RefreshCanEndDecision();
				this.RefreshRelationChangeText();
			}
		}

		// Token: 0x060009FC RID: 2556 RVA: 0x0002B7F8 File Offset: 0x000299F8
		private void OnAbstain(DecisionOptionVM target)
		{
			if (this._currentSelectedOption != target)
			{
				if (this._currentSelectedOption != null)
				{
					this._currentSelectedOption.IsSelected = false;
				}
				this._currentSelectedOption = target;
				this._currentSelectedOption.IsSelected = true;
				this.KingdomDecisionMaker.OnPlayerSupport((!this._currentSelectedOption.IsOptionForAbstain) ? this._currentSelectedOption.Option : null, this._currentSelectedOption.CurrentSupportWeight);
				this.RefreshWinPercentages();
				this.RefreshInfluenceCost();
				this.RefreshCanEndDecision();
				this.RefreshRelationChangeText();
			}
		}

		// Token: 0x060009FD RID: 2557 RVA: 0x0002B87E File Offset: 0x00029A7E
		private void OnSupportStrengthChange(DecisionOptionVM option)
		{
			this.RefreshWinPercentages();
			this.RefreshCanEndDecision();
			this.RefreshRelationChangeText();
			this.RefreshInfluenceCost();
		}

		// Token: 0x060009FE RID: 2558 RVA: 0x0002B898 File Offset: 0x00029A98
		private void RefreshWinPercentages()
		{
			this.KingdomDecisionMaker.DetermineOfficialSupport();
			using (List<DecisionOutcome>.Enumerator enumerator = this.KingdomDecisionMaker.PossibleOutcomes.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					DecisionOutcome option = enumerator.Current;
					DecisionOptionVM decisionOptionVM = this.DecisionOptionsList.FirstOrDefault((DecisionOptionVM c) => c.Option == option);
					if (decisionOptionVM == null)
					{
						Debug.FailedAssert("Couldn't find option to update win chance for!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\KingdomManagement\\Decisions\\ItemTypes\\DecisionItemBaseVM.cs", "RefreshWinPercentages", 213);
					}
					else
					{
						decisionOptionVM.WinPercentage = (int)MathF.Round(option.WinChance * 100f, 2);
					}
				}
			}
			int num = (from d in this.DecisionOptionsList
				where !d.IsOptionForAbstain
				select d).Sum((DecisionOptionVM d) => d.WinPercentage);
			if (num != 100)
			{
				int num2 = 100 - num;
				List<DecisionOptionVM> list = (from opt in this.DecisionOptionsList
					where opt.Sponsor != null
					select opt).ToList<DecisionOptionVM>();
				int num3 = (from opt in list
					select opt.WinPercentage).Sum();
				if (num3 == 0)
				{
					int num4 = num2 / list.Count;
					foreach (DecisionOptionVM decisionOptionVM2 in list)
					{
						decisionOptionVM2.WinPercentage += num4;
					}
					list[0].WinPercentage += num2 - num4 * list.Count;
					return;
				}
				int num5 = 0;
				foreach (DecisionOptionVM decisionOptionVM3 in (from opt in list
					where opt.WinPercentage > 0
					select opt).ToList<DecisionOptionVM>())
				{
					int num6 = MathF.Floor((float)num2 * ((float)decisionOptionVM3.WinPercentage / (float)num3));
					decisionOptionVM3.WinPercentage += num6;
					num5 += num6;
				}
				list[0].WinPercentage += num2 - num5;
			}
		}

		// Token: 0x060009FF RID: 2559 RVA: 0x0002BB34 File Offset: 0x00029D34
		private void RefreshInfluenceCost()
		{
			if (this._currentInfluenceCost > 0f)
			{
				GameTexts.SetVariable("AMOUNT", this._currentInfluenceCost);
				GameTexts.SetVariable("INFLUENCE_ICON", "{=!}<img src=\"General\\Icons\\Influence@2x\" extend=\"7\">");
				this.InfluenceCostText = GameTexts.FindText("str_decision_influence_cost", null).ToString();
				return;
			}
			this.InfluenceCostText = "";
		}

		// Token: 0x06000A00 RID: 2560 RVA: 0x0002BB90 File Offset: 0x00029D90
		private void RefreshRelationChangeText()
		{
			this.RelationChangeText = "";
			DecisionOptionVM currentSelectedOption = this._currentSelectedOption;
			if (currentSelectedOption != null && !currentSelectedOption.IsOptionForAbstain)
			{
				DecisionOptionVM currentSelectedOption2 = this._currentSelectedOption;
				if (currentSelectedOption2 == null || currentSelectedOption2.CurrentSupportWeight > Supporter.SupportWeights.Choose)
				{
					foreach (DecisionOptionVM decisionOptionVM in this.DecisionOptionsList)
					{
						DecisionOutcome option = decisionOptionVM.Option;
						if (((option != null) ? option.SponsorClan : null) != null && decisionOptionVM.Option.SponsorClan != Clan.PlayerClan)
						{
							bool flag = this._currentSelectedOption == decisionOptionVM;
							GameTexts.SetVariable("HERO_NAME", decisionOptionVM.Option.SponsorClan.Leader.EncyclopediaLinkWithName);
							string text = (flag ? GameTexts.FindText("str_decision_relation_increase", null).ToString() : GameTexts.FindText("str_decision_relation_decrease", null).ToString());
							if (string.IsNullOrEmpty(this.RelationChangeText))
							{
								this.RelationChangeText = text;
							}
							else
							{
								GameTexts.SetVariable("newline", "\n");
								GameTexts.SetVariable("STR1", this.RelationChangeText);
								GameTexts.SetVariable("STR2", text);
								this.RelationChangeText = GameTexts.FindText("str_string_newline_string", null).ToString();
							}
						}
					}
				}
			}
		}

		// Token: 0x06000A01 RID: 2561 RVA: 0x0002BCEC File Offset: 0x00029EEC
		private void RefreshCanEndDecision()
		{
			bool flag = this._currentSelectedOption != null && (!this.IsPlayerSupporter || this._currentSelectedOption.CurrentSupportWeight > Supporter.SupportWeights.Choose);
			bool flag2 = this._currentInfluenceCost <= Clan.PlayerClan.Influence || this._currentInfluenceCost == 0f;
			DecisionOptionVM currentSelectedOption = this._currentSelectedOption;
			bool flag3 = currentSelectedOption != null && currentSelectedOption.IsOptionForAbstain;
			this.CanEndDecision = !this._finalSelectionDone && (flag3 || (flag && flag2));
			if (this.CanEndDecision)
			{
				this.EndDecisionHint.HintText = TextObject.GetEmpty();
				return;
			}
			if (flag)
			{
				if (!flag2)
				{
					this.EndDecisionHint.HintText = GameTexts.FindText("str_decision_not_enough_influence", null);
				}
				return;
			}
			if (this.IsPlayerSupporter)
			{
				this.EndDecisionHint.HintText = GameTexts.FindText("str_decision_need_to_select_an_option_and_support", null);
				return;
			}
			this.EndDecisionHint.HintText = GameTexts.FindText("str_decision_need_to_select_an_outcome", null);
		}

		// Token: 0x06000A02 RID: 2562 RVA: 0x0002BDD9 File Offset: 0x00029FD9
		protected void ExecuteLink(string link)
		{
			Campaign.Current.EncyclopediaManager.GoToLink(link);
		}

		// Token: 0x06000A03 RID: 2563 RVA: 0x0002BDEC File Offset: 0x00029FEC
		protected void ExecuteShowStageTooltip()
		{
			if (!this.IsPlayerSupporter)
			{
				MBInformationManager.ShowHint(GameTexts.FindText("str_decision_second_stage_player_decider", null).ToString());
				return;
			}
			if (this.CurrentStageIndex == 0)
			{
				MBInformationManager.ShowHint(GameTexts.FindText("str_decision_first_stage_player_supporter", null).ToString());
				return;
			}
			MBInformationManager.ShowHint(GameTexts.FindText("str_decision_second_stage_player_supporter", null).ToString());
		}

		// Token: 0x06000A04 RID: 2564 RVA: 0x0002BE4A File Offset: 0x0002A04A
		protected void ExecuteHideStageTooltip()
		{
			MBInformationManager.HideInformations();
		}

		// Token: 0x06000A05 RID: 2565 RVA: 0x0002BE51 File Offset: 0x0002A051
		public void ExecuteFinalSelection()
		{
			if (this.CanEndDecision)
			{
				this.KingdomDecisionMaker.ApplySelection();
				this._finalSelectionDone = true;
				this.RefreshCanEndDecision();
			}
		}

		// Token: 0x06000A06 RID: 2566 RVA: 0x0002BE74 File Offset: 0x0002A074
		protected void ExecuteDone()
		{
			TextObject chosenOutcomeText = this.KingdomDecisionMaker.GetChosenOutcomeText();
			this.IsActive = false;
			InformationManager.ShowInquiry(new InquiryData(GameTexts.FindText("str_decision_outcome", null).ToString(), chosenOutcomeText.ToString(), true, false, GameTexts.FindText("str_ok", null).ToString(), "", delegate()
			{
				this._onDecisionOver();
			}, null, "", 0f, null, null, null), false, false);
			CampaignEvents.KingdomDecisionConcluded.ClearListeners(this);
			this._currentSelectedOption = null;
		}

		// Token: 0x06000A07 RID: 2567 RVA: 0x0002BEF9 File Offset: 0x0002A0F9
		public override void OnFinalize()
		{
			base.OnFinalize();
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
			eventManager.UnregisterEvent<TutorialNotificationElementChangeEvent>(new Action<TutorialNotificationElementChangeEvent>(this.OnTutorialNotificationElementIDChange));
		}

		// Token: 0x06000A08 RID: 2568 RVA: 0x0002BF28 File Offset: 0x0002A128
		private void OnTutorialNotificationElementIDChange(TutorialNotificationElementChangeEvent obj)
		{
			if (this._latestTutorialElementID != obj.NewNotificationElementID)
			{
				this._latestTutorialElementID = obj.NewNotificationElementID;
				if (this._isDecisionOptionsHighlightEnabled && this._latestTutorialElementID != this._decisionOptionsHighlightID)
				{
					this.SetOptionsHighlight(false);
					this._isDecisionOptionsHighlightEnabled = false;
					return;
				}
				if (!this._isDecisionOptionsHighlightEnabled && this._latestTutorialElementID == this._decisionOptionsHighlightID)
				{
					this.SetOptionsHighlight(true);
					this._isDecisionOptionsHighlightEnabled = true;
				}
			}
		}

		// Token: 0x06000A09 RID: 2569 RVA: 0x0002BFA8 File Offset: 0x0002A1A8
		private void SetOptionsHighlight(bool state)
		{
			for (int i = 0; i < this.DecisionOptionsList.Count; i++)
			{
				DecisionOptionVM decisionOptionVM = this.DecisionOptionsList[i];
				if (decisionOptionVM.CanBeChosen)
				{
					decisionOptionVM.IsHighlightEnabled = state;
				}
			}
		}

		// Token: 0x06000A0A RID: 2570 RVA: 0x0002BFE7 File Offset: 0x0002A1E7
		public void SetDoneInputKey(InputKeyItemVM inputKeyItemVM)
		{
			this.DoneInputKey = inputKeyItemVM;
		}

		// Token: 0x17000300 RID: 768
		// (get) Token: 0x06000A0B RID: 2571 RVA: 0x0002BFF0 File Offset: 0x0002A1F0
		// (set) Token: 0x06000A0C RID: 2572 RVA: 0x0002BFF8 File Offset: 0x0002A1F8
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

		// Token: 0x17000301 RID: 769
		// (get) Token: 0x06000A0D RID: 2573 RVA: 0x0002C016 File Offset: 0x0002A216
		// (set) Token: 0x06000A0E RID: 2574 RVA: 0x0002C01E File Offset: 0x0002A21E
		[DataSourceProperty]
		public HintViewModel EndDecisionHint
		{
			get
			{
				return this._endDecisionHint;
			}
			set
			{
				if (value != this._endDecisionHint)
				{
					this._endDecisionHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "EndDecisionHint");
				}
			}
		}

		// Token: 0x17000302 RID: 770
		// (get) Token: 0x06000A0F RID: 2575 RVA: 0x0002C03C File Offset: 0x0002A23C
		// (set) Token: 0x06000A10 RID: 2576 RVA: 0x0002C044 File Offset: 0x0002A244
		[DataSourceProperty]
		public int DecisionType
		{
			get
			{
				return this._decisionType;
			}
			set
			{
				if (value != this._decisionType)
				{
					this._decisionType = value;
					base.OnPropertyChangedWithValue(value, "DecisionType");
				}
			}
		}

		// Token: 0x17000303 RID: 771
		// (get) Token: 0x06000A11 RID: 2577 RVA: 0x0002C062 File Offset: 0x0002A262
		// (set) Token: 0x06000A12 RID: 2578 RVA: 0x0002C06A File Offset: 0x0002A26A
		[DataSourceProperty]
		public string TotalInfluenceText
		{
			get
			{
				return this._totalInfluenceText;
			}
			set
			{
				if (value != this._totalInfluenceText)
				{
					this._totalInfluenceText = value;
					base.OnPropertyChangedWithValue<string>(value, "TotalInfluenceText");
				}
			}
		}

		// Token: 0x17000304 RID: 772
		// (get) Token: 0x06000A13 RID: 2579 RVA: 0x0002C08D File Offset: 0x0002A28D
		// (set) Token: 0x06000A14 RID: 2580 RVA: 0x0002C095 File Offset: 0x0002A295
		[DataSourceProperty]
		public bool IsActive
		{
			get
			{
				return this._isActive;
			}
			set
			{
				if (value != this._isActive)
				{
					this._isActive = value;
					base.OnPropertyChangedWithValue(value, "IsActive");
				}
			}
		}

		// Token: 0x17000305 RID: 773
		// (get) Token: 0x06000A15 RID: 2581 RVA: 0x0002C0B3 File Offset: 0x0002A2B3
		// (set) Token: 0x06000A16 RID: 2582 RVA: 0x0002C0BB File Offset: 0x0002A2BB
		[DataSourceProperty]
		public int CurrentStageIndex
		{
			get
			{
				return this._currentStageIndex;
			}
			set
			{
				if (value != this._currentStageIndex)
				{
					this._currentStageIndex = value;
					base.OnPropertyChangedWithValue(value, "CurrentStageIndex");
				}
			}
		}

		// Token: 0x17000306 RID: 774
		// (get) Token: 0x06000A17 RID: 2583 RVA: 0x0002C0D9 File Offset: 0x0002A2D9
		// (set) Token: 0x06000A18 RID: 2584 RVA: 0x0002C0E1 File Offset: 0x0002A2E1
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

		// Token: 0x17000307 RID: 775
		// (get) Token: 0x06000A19 RID: 2585 RVA: 0x0002C0FF File Offset: 0x0002A2FF
		// (set) Token: 0x06000A1A RID: 2586 RVA: 0x0002C107 File Offset: 0x0002A307
		[DataSourceProperty]
		public bool CanEndDecision
		{
			get
			{
				return this._canEndDecision;
			}
			set
			{
				if (value != this._canEndDecision)
				{
					this._canEndDecision = value;
					base.OnPropertyChangedWithValue(value, "CanEndDecision");
				}
			}
		}

		// Token: 0x17000308 RID: 776
		// (get) Token: 0x06000A1B RID: 2587 RVA: 0x0002C125 File Offset: 0x0002A325
		// (set) Token: 0x06000A1C RID: 2588 RVA: 0x0002C12D File Offset: 0x0002A32D
		[DataSourceProperty]
		public bool IsKingsDecisionOver
		{
			get
			{
				return this._isKingsDecisionOver;
			}
			set
			{
				if (value != this._isKingsDecisionOver)
				{
					this._isKingsDecisionOver = value;
					base.OnPropertyChangedWithValue(value, "IsKingsDecisionOver");
				}
			}
		}

		// Token: 0x17000309 RID: 777
		// (get) Token: 0x06000A1D RID: 2589 RVA: 0x0002C14B File Offset: 0x0002A34B
		// (set) Token: 0x06000A1E RID: 2590 RVA: 0x0002C153 File Offset: 0x0002A353
		[DataSourceProperty]
		public string RelationChangeText
		{
			get
			{
				return this._increaseRelationText;
			}
			set
			{
				if (value != this._increaseRelationText)
				{
					this._increaseRelationText = value;
					base.OnPropertyChangedWithValue<string>(value, "RelationChangeText");
				}
			}
		}

		// Token: 0x1700030A RID: 778
		// (get) Token: 0x06000A1F RID: 2591 RVA: 0x0002C176 File Offset: 0x0002A376
		// (set) Token: 0x06000A20 RID: 2592 RVA: 0x0002C17E File Offset: 0x0002A37E
		[DataSourceProperty]
		public string DescriptionText
		{
			get
			{
				return this._descriptionText;
			}
			set
			{
				if (value != this._descriptionText)
				{
					this._descriptionText = value;
					base.OnPropertyChangedWithValue<string>(value, "DescriptionText");
				}
			}
		}

		// Token: 0x1700030B RID: 779
		// (get) Token: 0x06000A21 RID: 2593 RVA: 0x0002C1A1 File Offset: 0x0002A3A1
		// (set) Token: 0x06000A22 RID: 2594 RVA: 0x0002C1A9 File Offset: 0x0002A3A9
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

		// Token: 0x1700030C RID: 780
		// (get) Token: 0x06000A23 RID: 2595 RVA: 0x0002C1CC File Offset: 0x0002A3CC
		// (set) Token: 0x06000A24 RID: 2596 RVA: 0x0002C1D4 File Offset: 0x0002A3D4
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

		// Token: 0x1700030D RID: 781
		// (get) Token: 0x06000A25 RID: 2597 RVA: 0x0002C1F7 File Offset: 0x0002A3F7
		// (set) Token: 0x06000A26 RID: 2598 RVA: 0x0002C1FF File Offset: 0x0002A3FF
		[DataSourceProperty]
		public string InfluenceCostText
		{
			get
			{
				return this._influenceCostText;
			}
			set
			{
				if (value != this._influenceCostText)
				{
					this._influenceCostText = value;
					base.OnPropertyChangedWithValue<string>(value, "InfluenceCostText");
				}
			}
		}

		// Token: 0x1700030E RID: 782
		// (get) Token: 0x06000A27 RID: 2599 RVA: 0x0002C222 File Offset: 0x0002A422
		// (set) Token: 0x06000A28 RID: 2600 RVA: 0x0002C22A File Offset: 0x0002A42A
		[DataSourceProperty]
		public MBBindingList<DecisionOptionVM> DecisionOptionsList
		{
			get
			{
				return this._decisionOptionsList;
			}
			set
			{
				if (value != this._decisionOptionsList)
				{
					this._decisionOptionsList = value;
					base.OnPropertyChangedWithValue<MBBindingList<DecisionOptionVM>>(value, "DecisionOptionsList");
				}
			}
		}

		// Token: 0x04000468 RID: 1128
		protected readonly KingdomDecision _decision;

		// Token: 0x04000469 RID: 1129
		private readonly Action _onDecisionOver;

		// Token: 0x0400046A RID: 1130
		private DecisionOptionVM _currentSelectedOption;

		// Token: 0x0400046B RID: 1131
		private bool _finalSelectionDone;

		// Token: 0x0400046C RID: 1132
		private bool _isDecisionOptionsHighlightEnabled;

		// Token: 0x0400046D RID: 1133
		private string _decisionOptionsHighlightID = "DecisionOptions";

		// Token: 0x0400046E RID: 1134
		private string _latestTutorialElementID;

		// Token: 0x0400046F RID: 1135
		private InputKeyItemVM _doneInputKey;

		// Token: 0x04000470 RID: 1136
		private int _decisionType;

		// Token: 0x04000471 RID: 1137
		private bool _isActive;

		// Token: 0x04000472 RID: 1138
		private bool _isPlayerSupporter;

		// Token: 0x04000473 RID: 1139
		private bool _canEndDecision;

		// Token: 0x04000474 RID: 1140
		private bool _isKingsDecisionOver;

		// Token: 0x04000475 RID: 1141
		private int _currentStageIndex = -1;

		// Token: 0x04000476 RID: 1142
		private string _titleText;

		// Token: 0x04000477 RID: 1143
		private string _doneText;

		// Token: 0x04000478 RID: 1144
		private string _descriptionText;

		// Token: 0x04000479 RID: 1145
		private string _influenceCostText;

		// Token: 0x0400047A RID: 1146
		private string _totalInfluenceText;

		// Token: 0x0400047B RID: 1147
		private string _increaseRelationText;

		// Token: 0x0400047C RID: 1148
		private HintViewModel _endDecisionHint;

		// Token: 0x0400047D RID: 1149
		private MBBindingList<DecisionOptionVM> _decisionOptionsList;

		// Token: 0x020001DC RID: 476
		protected enum DecisionTypes
		{
			// Token: 0x04001113 RID: 4371
			Default,
			// Token: 0x04001114 RID: 4372
			Settlement,
			// Token: 0x04001115 RID: 4373
			ExpelClan,
			// Token: 0x04001116 RID: 4374
			Policy,
			// Token: 0x04001117 RID: 4375
			DeclareWar,
			// Token: 0x04001118 RID: 4376
			MakePeace,
			// Token: 0x04001119 RID: 4377
			KingSelection,
			// Token: 0x0400111A RID: 4378
			StartAlliance,
			// Token: 0x0400111B RID: 4379
			AcceptCallToWarAgreement,
			// Token: 0x0400111C RID: 4380
			ProposeCallToWarAgreement,
			// Token: 0x0400111D RID: 4381
			Trade
		}
	}
}
