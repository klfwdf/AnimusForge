using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Decisions.ItemTypes;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Decisions
{
	// Token: 0x02000078 RID: 120
	public class KingdomDecisionsVM : ViewModel
	{
		// Token: 0x170002EA RID: 746
		// (get) Token: 0x060009C0 RID: 2496 RVA: 0x0002A7A5 File Offset: 0x000289A5
		public bool IsCurrentDecisionActive
		{
			get
			{
				DecisionItemBaseVM currentDecision = this.CurrentDecision;
				return currentDecision != null && currentDecision.IsActive;
			}
		}

		// Token: 0x170002EB RID: 747
		// (get) Token: 0x060009C1 RID: 2497 RVA: 0x0002A7B8 File Offset: 0x000289B8
		// (set) Token: 0x060009C2 RID: 2498 RVA: 0x0002A7C0 File Offset: 0x000289C0
		private bool _shouldCheckForDecision { get; set; } = true;

		// Token: 0x060009C3 RID: 2499 RVA: 0x0002A7CC File Offset: 0x000289CC
		public KingdomDecisionsVM(Action refreshKingdomManagement)
		{
			this._refreshKingdomManagement = refreshKingdomManagement;
			this._examinedDecisionsSinceInit = new List<KingdomDecision>();
			this._examinedDecisionsSinceInit.AddRange(from d in Clan.PlayerClan.Kingdom.UnresolvedDecisions
				where d.ShouldBeCancelled()
				select d);
			this._solvedDecisionsSinceInit = new List<KingdomDecision>();
			CampaignEvents.KingdomDecisionConcluded.AddNonSerializedListener(this, new Action<KingdomDecision, DecisionOutcome, bool>(this.OnKingdomDecisionConcluded));
			this.IsRefreshed = true;
			this.RefreshValues();
		}

		// Token: 0x060009C4 RID: 2500 RVA: 0x0002A865 File Offset: 0x00028A65
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.TitleText = GameTexts.FindText("str_kingdom_decisions", null).ToString();
			DecisionItemBaseVM currentDecision = this.CurrentDecision;
			if (currentDecision == null)
			{
				return;
			}
			currentDecision.RefreshValues();
		}

		// Token: 0x060009C5 RID: 2501 RVA: 0x0002A894 File Offset: 0x00028A94
		public void OnFrameTick()
		{
			this.IsActive = this.IsCurrentDecisionActive;
			IEnumerable<KingdomDecision> source = Clan.PlayerClan.Kingdom.UnresolvedDecisions.Except(this._examinedDecisionsSinceInit);
			if (this._shouldCheckForDecision)
			{
				if (this.CurrentDecision != null)
				{
					DecisionItemBaseVM currentDecision = this.CurrentDecision;
					if (currentDecision == null || currentDecision.IsActive)
					{
						return;
					}
				}
				if (source.Any<KingdomDecision>())
				{
					KingdomDecision kingdomDecision = this._solvedDecisionsSinceInit.LastOrDefault<KingdomDecision>();
					KingdomDecision kingdomDecision2 = ((kingdomDecision != null) ? kingdomDecision.GetFollowUpDecision() : null);
					if (kingdomDecision2 != null)
					{
						this.HandleDecision(kingdomDecision2);
						return;
					}
					this.HandleNextDecision();
				}
			}
		}

		// Token: 0x060009C6 RID: 2502 RVA: 0x0002A920 File Offset: 0x00028B20
		public void HandleNextDecision()
		{
			this.HandleDecision(Clan.PlayerClan.Kingdom.UnresolvedDecisions.Except(this._examinedDecisionsSinceInit).FirstOrDefault<KingdomDecision>());
		}

		// Token: 0x060009C7 RID: 2503 RVA: 0x0002A948 File Offset: 0x00028B48
		public void HandleDecision(KingdomDecision curDecision)
		{
			TextObject textObject;
			if (!CampaignUIHelper.GetMapScreenActionIsEnabledWithReason(out textObject))
			{
				this._shouldCheckForDecision = false;
				return;
			}
			KingdomDecision curDecision2 = curDecision;
			if (curDecision2 != null && !curDecision2.ShouldBeCancelled())
			{
				this._shouldCheckForDecision = false;
				this._examinedDecisionsSinceInit.Add(curDecision);
				if (curDecision.IsPlayerParticipant)
				{
					TextObject generalTitle = new KingdomElection(curDecision).GetGeneralTitle();
					GameTexts.SetVariable("DECISION_NAME", generalTitle.ToString());
					string text = (curDecision.NeedsPlayerResolution ? GameTexts.FindText("str_you_need_to_resolve_decision", null).ToString() : GameTexts.FindText("str_do_you_want_to_resolve_decision", null).ToString());
					if (!curDecision.NeedsPlayerResolution && curDecision.TriggerTime.IsFuture)
					{
						GameTexts.SetVariable("HOUR", ((int)curDecision.TriggerTime.RemainingHoursFromNow).ToString());
						GameTexts.SetVariable("newline", "\n");
						GameTexts.SetVariable("STR1", text);
						GameTexts.SetVariable("STR2", GameTexts.FindText("str_decision_will_be_resolved_in_hours", null));
						text = GameTexts.FindText("str_string_newline_string", null).ToString();
					}
					this._queryData = new InquiryData(GameTexts.FindText("str_decision", null).ToString(), text, true, !curDecision.NeedsPlayerResolution, GameTexts.FindText("str_ok", null).ToString(), GameTexts.FindText("str_cancel", null).ToString(), delegate()
					{
						this.RefreshWith(curDecision);
					}, delegate()
					{
						this._shouldCheckForDecision = true;
					}, "", 0f, null, null, null);
					this._shouldCheckForDecision = false;
					InformationManager.ShowInquiry(this._queryData, false, false);
					return;
				}
			}
			else
			{
				this._shouldCheckForDecision = false;
				this._queryData = null;
			}
		}

		// Token: 0x060009C8 RID: 2504 RVA: 0x0002AB2C File Offset: 0x00028D2C
		public void RefreshWith(KingdomDecision decision)
		{
			if (decision.IsSingleClanDecision())
			{
				KingdomElection kingdomElection = new KingdomElection(decision);
				kingdomElection.StartElection();
				kingdomElection.ApplySelection();
				InformationManager.ShowInquiry(new InquiryData(GameTexts.FindText("str_decision_outcome", null).ToString(), kingdomElection.GetChosenOutcomeText().ToString(), true, false, GameTexts.FindText("str_ok", null).ToString(), "", delegate()
				{
					this.OnSingleDecisionOver();
				}, null, "", 0f, null, null, null), false, false);
				return;
			}
			this._shouldCheckForDecision = false;
			this.CurrentDecision = this.GetDecisionItemBasedOnType(decision);
			this.CurrentDecision.SetDoneInputKey(this.DoneInputKey);
		}

		// Token: 0x060009C9 RID: 2505 RVA: 0x0002ABD2 File Offset: 0x00028DD2
		private void OnSingleDecisionOver()
		{
			this._refreshKingdomManagement();
			this._shouldCheckForDecision = true;
		}

		// Token: 0x060009CA RID: 2506 RVA: 0x0002ABE6 File Offset: 0x00028DE6
		private void OnDecisionOver()
		{
			this._refreshKingdomManagement();
			DecisionItemBaseVM currentDecision = this.CurrentDecision;
			if (currentDecision != null)
			{
				currentDecision.OnFinalize();
			}
			this.CurrentDecision = null;
			this._shouldCheckForDecision = true;
		}

		// Token: 0x060009CB RID: 2507 RVA: 0x0002AC12 File Offset: 0x00028E12
		private void OnKingdomDecisionConcluded(KingdomDecision decision, DecisionOutcome outcome, bool isPlayerInvolved)
		{
			if (isPlayerInvolved)
			{
				this._solvedDecisionsSinceInit.Add(decision);
			}
		}

		// Token: 0x060009CC RID: 2508 RVA: 0x0002AC24 File Offset: 0x00028E24
		private DecisionItemBaseVM GetDecisionItemBasedOnType(KingdomDecision decision)
		{
			SettlementClaimantDecision settlementClaimantDecision;
			if ((settlementClaimantDecision = decision as SettlementClaimantDecision) != null)
			{
				return new SettlementDecisionItemVM(settlementClaimantDecision.Settlement, decision, new Action(this.OnDecisionOver));
			}
			SettlementClaimantPreliminaryDecision settlementClaimantPreliminaryDecision;
			if ((settlementClaimantPreliminaryDecision = decision as SettlementClaimantPreliminaryDecision) != null)
			{
				return new SettlementDecisionItemVM(settlementClaimantPreliminaryDecision.Settlement, decision, new Action(this.OnDecisionOver));
			}
			ExpelClanFromKingdomDecision decision2;
			if ((decision2 = decision as ExpelClanFromKingdomDecision) != null)
			{
				return new ExpelClanDecisionItemVM(decision2, new Action(this.OnDecisionOver));
			}
			KingdomPolicyDecision decision3;
			if ((decision3 = decision as KingdomPolicyDecision) != null)
			{
				return new PolicyDecisionItemVM(decision3, new Action(this.OnDecisionOver));
			}
			DeclareWarDecision decision4;
			if ((decision4 = decision as DeclareWarDecision) != null)
			{
				return new DeclareWarDecisionItemVM(decision4, new Action(this.OnDecisionOver));
			}
			MakePeaceKingdomDecision decision5;
			if ((decision5 = decision as MakePeaceKingdomDecision) != null)
			{
				return new MakePeaceDecisionItemVM(decision5, new Action(this.OnDecisionOver));
			}
			KingSelectionKingdomDecision decision6;
			if ((decision6 = decision as KingSelectionKingdomDecision) != null)
			{
				return new KingSelectionDecisionItemVM(decision6, new Action(this.OnDecisionOver));
			}
			StartAllianceDecision decision7;
			if ((decision7 = decision as StartAllianceDecision) != null)
			{
				return new StartAllianceDecisionItemVM(decision7, new Action(this.OnDecisionOver));
			}
			ProposeCallToWarAgreementDecision decision8;
			if ((decision8 = decision as ProposeCallToWarAgreementDecision) != null)
			{
				return new ProposeCallToWarAgreementDecisionItemVM(decision8, new Action(this.OnDecisionOver));
			}
			AcceptCallToWarAgreementDecision decision9;
			if ((decision9 = decision as AcceptCallToWarAgreementDecision) != null)
			{
				return new AcceptingCallToWarAgreementDecisionItemVM(decision9, new Action(this.OnDecisionOver));
			}
			TradeAgreementDecision decision10;
			if ((decision10 = decision as TradeAgreementDecision) != null)
			{
				return new TradeAgreementDecisionItemVM(decision10, new Action(this.OnDecisionOver));
			}
			Debug.FailedAssert("No defined decision type for this decision! This shouldn't happen", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\KingdomManagement\\Decisions\\KingdomDecisionsVM.cs", "GetDecisionItemBasedOnType", 215);
			return new DecisionItemBaseVM(decision, new Action(this.OnDecisionOver));
		}

		// Token: 0x060009CD RID: 2509 RVA: 0x0002ADB5 File Offset: 0x00028FB5
		public override void OnFinalize()
		{
			base.OnFinalize();
			this.DoneInputKey.OnFinalize();
			DecisionItemBaseVM currentDecision = this.CurrentDecision;
			if (currentDecision != null)
			{
				currentDecision.OnFinalize();
			}
			this.CurrentDecision = null;
			CampaignEvents.KingdomDecisionConcluded.ClearListeners(this);
		}

		// Token: 0x060009CE RID: 2510 RVA: 0x0002ADEB File Offset: 0x00028FEB
		public void SetDoneInputKey(HotKey hotKey)
		{
			this.DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
		}

		// Token: 0x170002EC RID: 748
		// (get) Token: 0x060009CF RID: 2511 RVA: 0x0002ADFA File Offset: 0x00028FFA
		// (set) Token: 0x060009D0 RID: 2512 RVA: 0x0002AE02 File Offset: 0x00029002
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

		// Token: 0x170002ED RID: 749
		// (get) Token: 0x060009D1 RID: 2513 RVA: 0x0002AE20 File Offset: 0x00029020
		// (set) Token: 0x060009D2 RID: 2514 RVA: 0x0002AE28 File Offset: 0x00029028
		[DataSourceProperty]
		public DecisionItemBaseVM CurrentDecision
		{
			get
			{
				return this._currentDecision;
			}
			set
			{
				if (value != this._currentDecision)
				{
					this._currentDecision = value;
					base.OnPropertyChangedWithValue<DecisionItemBaseVM>(value, "CurrentDecision");
				}
			}
		}

		// Token: 0x170002EE RID: 750
		// (get) Token: 0x060009D3 RID: 2515 RVA: 0x0002AE46 File Offset: 0x00029046
		// (set) Token: 0x060009D4 RID: 2516 RVA: 0x0002AE4E File Offset: 0x0002904E
		[DataSourceProperty]
		public int NotificationCount
		{
			get
			{
				return this._notificationCount;
			}
			set
			{
				if (value != this._notificationCount)
				{
					this._notificationCount = value;
					base.OnPropertyChangedWithValue(value, "NotificationCount");
				}
			}
		}

		// Token: 0x170002EF RID: 751
		// (get) Token: 0x060009D5 RID: 2517 RVA: 0x0002AE6C File Offset: 0x0002906C
		// (set) Token: 0x060009D6 RID: 2518 RVA: 0x0002AE74 File Offset: 0x00029074
		[DataSourceProperty]
		public bool IsRefreshed
		{
			get
			{
				return this._isRefreshed;
			}
			set
			{
				if (value != this._isRefreshed)
				{
					this._isRefreshed = value;
					base.OnPropertyChangedWithValue(value, "IsRefreshed");
				}
			}
		}

		// Token: 0x170002F0 RID: 752
		// (get) Token: 0x060009D7 RID: 2519 RVA: 0x0002AE92 File Offset: 0x00029092
		// (set) Token: 0x060009D8 RID: 2520 RVA: 0x0002AE9A File Offset: 0x0002909A
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

		// Token: 0x170002F1 RID: 753
		// (get) Token: 0x060009D9 RID: 2521 RVA: 0x0002AEB8 File Offset: 0x000290B8
		// (set) Token: 0x060009DA RID: 2522 RVA: 0x0002AEC0 File Offset: 0x000290C0
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

		// Token: 0x04000451 RID: 1105
		private List<KingdomDecision> _examinedDecisionsSinceInit;

		// Token: 0x04000452 RID: 1106
		private List<KingdomDecision> _solvedDecisionsSinceInit;

		// Token: 0x04000453 RID: 1107
		private readonly Action _refreshKingdomManagement;

		// Token: 0x04000455 RID: 1109
		private InquiryData _queryData;

		// Token: 0x04000456 RID: 1110
		private InputKeyItemVM _doneInputKey;

		// Token: 0x04000457 RID: 1111
		private bool _isRefreshed;

		// Token: 0x04000458 RID: 1112
		private bool _isActive;

		// Token: 0x04000459 RID: 1113
		private int _notificationCount;

		// Token: 0x0400045A RID: 1114
		private string _titleText;

		// Token: 0x0400045B RID: 1115
		private DecisionItemBaseVM _currentDecision;
	}
}
