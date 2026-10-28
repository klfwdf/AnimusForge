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

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Decisions;

public class KingdomDecisionsVM : ViewModel
{
	private List<KingdomDecision> _examinedDecisionsSinceInit;

	private List<KingdomDecision> _solvedDecisionsSinceInit;

	private readonly Action _refreshKingdomManagement;

	private InquiryData _queryData;

	private InputKeyItemVM _doneInputKey;

	private bool _isRefreshed;

	private bool _isActive;

	private int _notificationCount;

	private string _titleText;

	private DecisionItemBaseVM _currentDecision;

	public bool IsCurrentDecisionActive => CurrentDecision?.IsActive ?? false;

	private bool _shouldCheckForDecision { get; set; } = true;

	[DataSourceProperty]
	public InputKeyItemVM DoneInputKey
	{
		get
		{
			return _doneInputKey;
		}
		set
		{
			if (value != _doneInputKey)
			{
				_doneInputKey = value;
				OnPropertyChangedWithValue(value, "DoneInputKey");
			}
		}
	}

	[DataSourceProperty]
	public DecisionItemBaseVM CurrentDecision
	{
		get
		{
			return _currentDecision;
		}
		set
		{
			if (value != _currentDecision)
			{
				_currentDecision = value;
				OnPropertyChangedWithValue(value, "CurrentDecision");
			}
		}
	}

	[DataSourceProperty]
	public int NotificationCount
	{
		get
		{
			return _notificationCount;
		}
		set
		{
			if (value != _notificationCount)
			{
				_notificationCount = value;
				OnPropertyChangedWithValue(value, "NotificationCount");
			}
		}
	}

	[DataSourceProperty]
	public bool IsRefreshed
	{
		get
		{
			return _isRefreshed;
		}
		set
		{
			if (value != _isRefreshed)
			{
				_isRefreshed = value;
				OnPropertyChangedWithValue(value, "IsRefreshed");
			}
		}
	}

	[DataSourceProperty]
	public bool IsActive
	{
		get
		{
			return _isActive;
		}
		set
		{
			if (value != _isActive)
			{
				_isActive = value;
				OnPropertyChangedWithValue(value, "IsActive");
			}
		}
	}

	[DataSourceProperty]
	public string TitleText
	{
		get
		{
			return _titleText;
		}
		set
		{
			if (value != _titleText)
			{
				_titleText = value;
				OnPropertyChangedWithValue(value, "TitleText");
			}
		}
	}

	public KingdomDecisionsVM(Action refreshKingdomManagement)
	{
		_refreshKingdomManagement = refreshKingdomManagement;
		_examinedDecisionsSinceInit = new List<KingdomDecision>();
		_examinedDecisionsSinceInit.AddRange(Clan.PlayerClan.Kingdom.UnresolvedDecisions.Where((KingdomDecision d) => d.ShouldBeCancelled()));
		_solvedDecisionsSinceInit = new List<KingdomDecision>();
		CampaignEvents.KingdomDecisionConcluded.AddNonSerializedListener(this, OnKingdomDecisionConcluded);
		IsRefreshed = true;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		TitleText = GameTexts.FindText("str_kingdom_decisions").ToString();
		CurrentDecision?.RefreshValues();
	}

	public void OnFrameTick()
	{
		IsActive = IsCurrentDecisionActive;
		IEnumerable<KingdomDecision> source = Clan.PlayerClan.Kingdom.UnresolvedDecisions.Except(_examinedDecisionsSinceInit);
		if (!_shouldCheckForDecision)
		{
			return;
		}
		if (CurrentDecision != null)
		{
			DecisionItemBaseVM currentDecision = CurrentDecision;
			if (currentDecision == null || currentDecision.IsActive)
			{
				return;
			}
		}
		if (source.Any())
		{
			KingdomDecision kingdomDecision = _solvedDecisionsSinceInit.LastOrDefault()?.GetFollowUpDecision();
			if (kingdomDecision != null)
			{
				HandleDecision(kingdomDecision);
			}
			else
			{
				HandleNextDecision();
			}
		}
	}

	public void HandleNextDecision()
	{
		HandleDecision(Clan.PlayerClan.Kingdom.UnresolvedDecisions.Except(_examinedDecisionsSinceInit).FirstOrDefault());
	}

	public void HandleDecision(KingdomDecision curDecision)
	{
		if (!CampaignUIHelper.GetMapScreenActionIsEnabledWithReason(out var _))
		{
			_shouldCheckForDecision = false;
			return;
		}
		KingdomDecision kingdomDecision = curDecision;
		if (kingdomDecision != null && !kingdomDecision.ShouldBeCancelled())
		{
			_shouldCheckForDecision = false;
			_examinedDecisionsSinceInit.Add(curDecision);
			if (curDecision.IsPlayerParticipant)
			{
				TextObject generalTitle = new KingdomElection(curDecision).GetGeneralTitle();
				GameTexts.SetVariable("DECISION_NAME", generalTitle.ToString());
				string text = (curDecision.NeedsPlayerResolution ? GameTexts.FindText("str_you_need_to_resolve_decision").ToString() : GameTexts.FindText("str_do_you_want_to_resolve_decision").ToString());
				if (!curDecision.NeedsPlayerResolution && curDecision.TriggerTime.IsFuture)
				{
					GameTexts.SetVariable("HOUR", ((int)curDecision.TriggerTime.RemainingHoursFromNow).ToString());
					GameTexts.SetVariable("newline", "\n");
					GameTexts.SetVariable("STR1", text);
					GameTexts.SetVariable("STR2", GameTexts.FindText("str_decision_will_be_resolved_in_hours"));
					text = GameTexts.FindText("str_string_newline_string").ToString();
				}
				_queryData = new InquiryData(GameTexts.FindText("str_decision").ToString(), text, isAffirmativeOptionShown: true, !curDecision.NeedsPlayerResolution, GameTexts.FindText("str_ok").ToString(), GameTexts.FindText("str_cancel").ToString(), delegate
				{
					RefreshWith(curDecision);
				}, delegate
				{
					_shouldCheckForDecision = true;
				});
				_shouldCheckForDecision = false;
				InformationManager.ShowInquiry(_queryData);
			}
		}
		else
		{
			_shouldCheckForDecision = false;
			_queryData = null;
		}
	}

	public void RefreshWith(KingdomDecision decision)
	{
		if (decision.IsSingleClanDecision())
		{
			KingdomElection kingdomElection = new KingdomElection(decision);
			kingdomElection.StartElection();
			kingdomElection.ApplySelection();
			InformationManager.ShowInquiry(new InquiryData(GameTexts.FindText("str_decision_outcome").ToString(), kingdomElection.GetChosenOutcomeText().ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, GameTexts.FindText("str_ok").ToString(), "", delegate
			{
				OnSingleDecisionOver();
			}, null));
		}
		else
		{
			_shouldCheckForDecision = false;
			CurrentDecision = GetDecisionItemBasedOnType(decision);
			CurrentDecision.SetDoneInputKey(DoneInputKey);
		}
	}

	private void OnSingleDecisionOver()
	{
		_refreshKingdomManagement();
		_shouldCheckForDecision = true;
	}

	private void OnDecisionOver()
	{
		_refreshKingdomManagement();
		CurrentDecision?.OnFinalize();
		CurrentDecision = null;
		_shouldCheckForDecision = true;
	}

	private void OnKingdomDecisionConcluded(KingdomDecision decision, DecisionOutcome outcome, bool isPlayerInvolved)
	{
		if (isPlayerInvolved)
		{
			_solvedDecisionsSinceInit.Add(decision);
		}
	}

	private DecisionItemBaseVM GetDecisionItemBasedOnType(KingdomDecision decision)
	{
		if (decision is SettlementClaimantDecision settlementClaimantDecision)
		{
			return new SettlementDecisionItemVM(settlementClaimantDecision.Settlement, decision, OnDecisionOver);
		}
		if (decision is SettlementClaimantPreliminaryDecision settlementClaimantPreliminaryDecision)
		{
			return new SettlementDecisionItemVM(settlementClaimantPreliminaryDecision.Settlement, decision, OnDecisionOver);
		}
		if (decision is ExpelClanFromKingdomDecision decision2)
		{
			return new ExpelClanDecisionItemVM(decision2, OnDecisionOver);
		}
		if (decision is KingdomPolicyDecision decision3)
		{
			return new PolicyDecisionItemVM(decision3, OnDecisionOver);
		}
		if (decision is DeclareWarDecision decision4)
		{
			return new DeclareWarDecisionItemVM(decision4, OnDecisionOver);
		}
		if (decision is MakePeaceKingdomDecision decision5)
		{
			return new MakePeaceDecisionItemVM(decision5, OnDecisionOver);
		}
		if (decision is KingSelectionKingdomDecision decision6)
		{
			return new KingSelectionDecisionItemVM(decision6, OnDecisionOver);
		}
		if (decision is StartAllianceDecision decision7)
		{
			return new StartAllianceDecisionItemVM(decision7, OnDecisionOver);
		}
		if (decision is ProposeCallToWarAgreementDecision decision8)
		{
			return new ProposeCallToWarAgreementDecisionItemVM(decision8, OnDecisionOver);
		}
		if (decision is AcceptCallToWarAgreementDecision decision9)
		{
			return new AcceptingCallToWarAgreementDecisionItemVM(decision9, OnDecisionOver);
		}
		if (decision is TradeAgreementDecision decision10)
		{
			return new TradeAgreementDecisionItemVM(decision10, OnDecisionOver);
		}
		Debug.FailedAssert("No defined decision type for this decision! This shouldn't happen", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\KingdomManagement\\Decisions\\KingdomDecisionsVM.cs", "GetDecisionItemBasedOnType", 215);
		return new DecisionItemBaseVM(decision, OnDecisionOver);
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		DoneInputKey.OnFinalize();
		CurrentDecision?.OnFinalize();
		CurrentDecision = null;
		CampaignEvents.KingdomDecisionConcluded.ClearListeners(this);
	}

	public void SetDoneInputKey(HotKey hotKey)
	{
		DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, isConsoleOnly: true);
	}
}
