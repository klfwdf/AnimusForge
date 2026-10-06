using System;
using System.Linq;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Policies;

public class KingdomPoliciesVM : KingdomCategoryVM
{
	private readonly Action<KingdomDecision> _forceDecide;

	private readonly Kingdom _playerKingdom;

	private PolicyObject _currentSelectedPolicyObject;

	private KingdomDecision _currentItemsUnresolvedDecision;

	private MBBindingList<KingdomPolicyItemVM> _activePolicies;

	private MBBindingList<KingdomPolicyItemVM> _otherPolicies;

	private KingdomPolicyItemVM _currentSelectedPolicy;

	private bool _canProposeOrDisavowPolicy;

	private bool _isInProposeMode = true;

	private string _proposeOrDisavowText;

	private string _proposeActionExplanationText;

	private string _activePoliciesText;

	private string _otherPoliciesText;

	private string _currentActiveModeText;

	private string _currentActionText;

	private string _proposeNewPolicyText;

	private string _disavowPolicyText;

	private string _policiesText;

	private string _backText;

	private int _proposalAndDisavowalCost;

	private string _numOfActivePoliciesText;

	private string _numOfOtherPoliciesText;

	private HintViewModel _doneHint;

	private string _policyLikelihoodText;

	private HintViewModel _likelihoodHint;

	private int _policyLikelihood;

	[DataSourceProperty]
	public HintViewModel DoneHint
	{
		get
		{
			return _doneHint;
		}
		set
		{
			if (value != _doneHint)
			{
				_doneHint = value;
				OnPropertyChangedWithValue(value, "DoneHint");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<KingdomPolicyItemVM> ActivePolicies
	{
		get
		{
			return _activePolicies;
		}
		set
		{
			if (value != _activePolicies)
			{
				_activePolicies = value;
				OnPropertyChangedWithValue(value, "ActivePolicies");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<KingdomPolicyItemVM> OtherPolicies
	{
		get
		{
			return _otherPolicies;
		}
		set
		{
			if (value != _otherPolicies)
			{
				_otherPolicies = value;
				OnPropertyChangedWithValue(value, "OtherPolicies");
			}
		}
	}

	[DataSourceProperty]
	public KingdomPolicyItemVM CurrentSelectedPolicy
	{
		get
		{
			return _currentSelectedPolicy;
		}
		set
		{
			if (value != _currentSelectedPolicy)
			{
				_currentSelectedPolicy = value;
				OnPropertyChangedWithValue(value, "CurrentSelectedPolicy");
			}
		}
	}

	[DataSourceProperty]
	public bool CanProposeOrDisavowPolicy
	{
		get
		{
			return _canProposeOrDisavowPolicy;
		}
		set
		{
			if (value != _canProposeOrDisavowPolicy)
			{
				_canProposeOrDisavowPolicy = value;
				OnPropertyChangedWithValue(value, "CanProposeOrDisavowPolicy");
			}
		}
	}

	[DataSourceProperty]
	public int ProposalAndDisavowalCost
	{
		get
		{
			return _proposalAndDisavowalCost;
		}
		set
		{
			if (value != _proposalAndDisavowalCost)
			{
				_proposalAndDisavowalCost = value;
				OnPropertyChangedWithValue(value, "ProposalAndDisavowalCost");
			}
		}
	}

	[DataSourceProperty]
	public string NumOfActivePoliciesText
	{
		get
		{
			return _numOfActivePoliciesText;
		}
		set
		{
			if (value != _numOfActivePoliciesText)
			{
				_numOfActivePoliciesText = value;
				OnPropertyChangedWithValue(value, "NumOfActivePoliciesText");
			}
		}
	}

	[DataSourceProperty]
	public string NumOfOtherPoliciesText
	{
		get
		{
			return _numOfOtherPoliciesText;
		}
		set
		{
			if (value != _numOfOtherPoliciesText)
			{
				_numOfOtherPoliciesText = value;
				OnPropertyChangedWithValue(value, "NumOfOtherPoliciesText");
			}
		}
	}

	[DataSourceProperty]
	public bool IsInProposeMode
	{
		get
		{
			return _isInProposeMode;
		}
		set
		{
			if (value != _isInProposeMode)
			{
				_isInProposeMode = value;
				OnPropertyChangedWithValue(value, "IsInProposeMode");
			}
		}
	}

	[DataSourceProperty]
	public string DisavowPolicyText
	{
		get
		{
			return _disavowPolicyText;
		}
		set
		{
			if (value != _disavowPolicyText)
			{
				_disavowPolicyText = value;
				OnPropertyChangedWithValue(value, "DisavowPolicyText");
			}
		}
	}

	[DataSourceProperty]
	public string CurrentActiveModeText
	{
		get
		{
			return _currentActiveModeText;
		}
		set
		{
			if (value != _currentActiveModeText)
			{
				_currentActiveModeText = value;
				OnPropertyChangedWithValue(value, "CurrentActiveModeText");
			}
		}
	}

	[DataSourceProperty]
	public string CurrentActionText
	{
		get
		{
			return _currentActionText;
		}
		set
		{
			if (value != _currentActionText)
			{
				_currentActionText = value;
				OnPropertyChangedWithValue(value, "CurrentActionText");
			}
		}
	}

	[DataSourceProperty]
	public string ProposeNewPolicyText
	{
		get
		{
			return _proposeNewPolicyText;
		}
		set
		{
			if (value != _proposeNewPolicyText)
			{
				_proposeNewPolicyText = value;
				OnPropertyChangedWithValue(value, "ProposeNewPolicyText");
			}
		}
	}

	[DataSourceProperty]
	public string BackText
	{
		get
		{
			return _backText;
		}
		set
		{
			if (value != _backText)
			{
				_backText = value;
				OnPropertyChangedWithValue(value, "BackText");
			}
		}
	}

	[DataSourceProperty]
	public string PoliciesText
	{
		get
		{
			return _policiesText;
		}
		set
		{
			if (value != _policiesText)
			{
				_policiesText = value;
				OnPropertyChangedWithValue(value, "PoliciesText");
			}
		}
	}

	[DataSourceProperty]
	public string ActivePoliciesText
	{
		get
		{
			return _activePoliciesText;
		}
		set
		{
			if (value != _activePoliciesText)
			{
				_activePoliciesText = value;
				OnPropertyChangedWithValue(value, "ActivePoliciesText");
			}
		}
	}

	[DataSourceProperty]
	public string PolicyLikelihoodText
	{
		get
		{
			return _policyLikelihoodText;
		}
		set
		{
			if (value != _policyLikelihoodText)
			{
				_policyLikelihoodText = value;
				OnPropertyChangedWithValue(value, "PolicyLikelihoodText");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel LikelihoodHint
	{
		get
		{
			return _likelihoodHint;
		}
		set
		{
			if (value != _likelihoodHint)
			{
				_likelihoodHint = value;
				OnPropertyChangedWithValue(value, "LikelihoodHint");
			}
		}
	}

	[DataSourceProperty]
	public int PolicyLikelihood
	{
		get
		{
			return _policyLikelihood;
		}
		set
		{
			if (value != _policyLikelihood)
			{
				_policyLikelihood = value;
				OnPropertyChangedWithValue(value, "PolicyLikelihood");
			}
		}
	}

	[DataSourceProperty]
	public string OtherPoliciesText
	{
		get
		{
			return _otherPoliciesText;
		}
		set
		{
			if (value != _otherPoliciesText)
			{
				_otherPoliciesText = value;
				OnPropertyChangedWithValue(value, "OtherPoliciesText");
			}
		}
	}

	[DataSourceProperty]
	public string ProposeOrDisavowText
	{
		get
		{
			return _proposeOrDisavowText;
		}
		set
		{
			if (value != _proposeOrDisavowText)
			{
				_proposeOrDisavowText = value;
				OnPropertyChangedWithValue(value, "ProposeOrDisavowText");
			}
		}
	}

	[DataSourceProperty]
	public string ProposeActionExplanationText
	{
		get
		{
			return _proposeActionExplanationText;
		}
		set
		{
			if (value != _proposeActionExplanationText)
			{
				_proposeActionExplanationText = value;
				OnPropertyChangedWithValue(value, "ProposeActionExplanationText");
			}
		}
	}

	public KingdomPoliciesVM(Action<KingdomDecision> forceDecide)
	{
		_forceDecide = forceDecide;
		ActivePolicies = new MBBindingList<KingdomPolicyItemVM>();
		OtherPolicies = new MBBindingList<KingdomPolicyItemVM>();
		DoneHint = new HintViewModel();
		_playerKingdom = Hero.MainHero.MapFaction as Kingdom;
		ProposalAndDisavowalCost = Campaign.Current.Models.DiplomacyModel.GetInfluenceCostOfPolicyProposalAndDisavowal(Clan.PlayerClan);
		base.IsAcceptableItemSelected = false;
		RefreshValues();
		ExecuteSwitchMode();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		PoliciesText = GameTexts.FindText("str_policies").ToString();
		ActivePoliciesText = GameTexts.FindText("str_active_policies").ToString();
		OtherPoliciesText = GameTexts.FindText("str_other_policies").ToString();
		ProposeNewPolicyText = GameTexts.FindText("str_propose_new_policy").ToString();
		DisavowPolicyText = GameTexts.FindText("str_disavow_a_policy").ToString();
		base.NoItemSelectedText = GameTexts.FindText("str_kingdom_no_policy_selected").ToString();
		base.CategoryNameText = new TextObject("{=Sls0KQVn}Elections").ToString();
		RefreshPolicyList();
	}

	public void SelectPolicy(PolicyObject policy)
	{
		bool flag = false;
		foreach (KingdomPolicyItemVM activePolicy in ActivePolicies)
		{
			if (activePolicy.Policy == policy)
			{
				OnPolicySelect(activePolicy);
				flag = true;
				break;
			}
		}
		if (flag)
		{
			return;
		}
		foreach (KingdomPolicyItemVM otherPolicy in OtherPolicies)
		{
			if (otherPolicy.Policy == policy)
			{
				OnPolicySelect(otherPolicy);
				flag = true;
				break;
			}
		}
	}

	private void OnPolicySelect(KingdomPolicyItemVM policy)
	{
		if (CurrentSelectedPolicy == policy)
		{
			return;
		}
		if (CurrentSelectedPolicy != null)
		{
			CurrentSelectedPolicy.IsSelected = false;
		}
		CurrentSelectedPolicy = policy;
		if (CurrentSelectedPolicy != null)
		{
			CurrentSelectedPolicy.IsSelected = true;
			_currentSelectedPolicyObject = policy.Policy;
			_currentItemsUnresolvedDecision = Clan.PlayerClan.Kingdom.UnresolvedDecisions.FirstOrDefault((KingdomDecision d) => d is KingdomPolicyDecision kingdomPolicyDecision && kingdomPolicyDecision.Policy == _currentSelectedPolicyObject && !d.ShouldBeCancelled());
			if (_currentItemsUnresolvedDecision != null)
			{
				CanProposeOrDisavowPolicy = GetCanProposeOrDisavowPolicyWithReason(hasUnresolvedDecision: true, out var disabledReason);
				DoneHint.HintText = disabledReason;
				ProposeOrDisavowText = GameTexts.FindText("str_resolve").ToString();
				ProposeActionExplanationText = GameTexts.FindText("str_resolve_explanation").ToString();
				PolicyLikelihood = CalculateLikelihood(policy.Policy);
			}
			else
			{
				_ = Clan.PlayerClan.Influence;
				_ = ProposalAndDisavowalCost;
				_ = Clan.PlayerClan.IsUnderMercenaryService;
				CanProposeOrDisavowPolicy = GetCanProposeOrDisavowPolicyWithReason(hasUnresolvedDecision: false, out var disabledReason2);
				DoneHint.HintText = disabledReason2;
				if (IsPolicyActive(policy.Policy))
				{
					ProposeActionExplanationText = GameTexts.FindText("str_policy_propose_again_action_explanation").SetTextVariable("SUPPORT", GetSupportText(policy.Policy)).ToString();
				}
				else
				{
					ProposeActionExplanationText = GameTexts.FindText("str_policy_propose_action_explanation").SetTextVariable("SUPPORT", GetSupportText(policy.Policy)).ToString();
				}
				ProposeOrDisavowText = ((_playerKingdom.Clans.Count > 1) ? GameTexts.FindText("str_policy_propose").ToString() : GameTexts.FindText("str_policy_enact").ToString());
				base.NotificationCount = Clan.PlayerClan.Kingdom.UnresolvedDecisions.Count((KingdomDecision d) => !d.ShouldBeCancelled());
				PolicyLikelihood = CalculateLikelihood(policy.Policy);
			}
			GameTexts.SetVariable("NUMBER", PolicyLikelihood);
			PolicyLikelihoodText = GameTexts.FindText("str_NUMBER_percent").ToString();
		}
		base.IsAcceptableItemSelected = CurrentSelectedPolicy != null;
	}

	private bool GetCanProposeOrDisavowPolicyWithReason(bool hasUnresolvedDecision, out TextObject disabledReason)
	{
		if (!CampaignUIHelper.GetMapScreenActionIsEnabledWithReason(out var disabledReason2))
		{
			disabledReason = disabledReason2;
			return false;
		}
		if (Clan.PlayerClan.IsUnderMercenaryService)
		{
			disabledReason = GameTexts.FindText("str_mercenaries_cannot_propose_policies");
			return false;
		}
		if (!hasUnresolvedDecision && Clan.PlayerClan.Influence < (float)ProposalAndDisavowalCost)
		{
			disabledReason = GameTexts.FindText("str_warning_you_dont_have_enough_influence");
			return false;
		}
		disabledReason = TextObject.GetEmpty();
		return true;
	}

	public void RefreshPolicyList()
	{
		ActivePolicies.Clear();
		OtherPolicies.Clear();
		if (_playerKingdom != null)
		{
			foreach (PolicyObject activePolicy in _playerKingdom.ActivePolicies)
			{
				ActivePolicies.Add(new KingdomPolicyItemVM(activePolicy, OnPolicySelect, IsPolicyActive));
			}
			foreach (PolicyObject item in PolicyObject.All.Where((PolicyObject p) => !IsPolicyActive(p)))
			{
				OtherPolicies.Add(new KingdomPolicyItemVM(item, OnPolicySelect, IsPolicyActive));
			}
		}
		GameTexts.SetVariable("STR", ActivePolicies.Count);
		NumOfActivePoliciesText = GameTexts.FindText("str_STR_in_parentheses").ToString();
		GameTexts.SetVariable("STR", OtherPolicies.Count);
		NumOfOtherPoliciesText = GameTexts.FindText("str_STR_in_parentheses").ToString();
		SetDefaultSelectedPolicy();
	}

	private bool IsPolicyActive(PolicyObject policy)
	{
		return _playerKingdom.ActivePolicies.Contains(policy);
	}

	private void SetDefaultSelectedPolicy()
	{
		KingdomPolicyItemVM policy = (IsInProposeMode ? OtherPolicies.FirstOrDefault() : ActivePolicies.FirstOrDefault());
		OnPolicySelect(policy);
	}

	private void ExecuteSwitchMode()
	{
		IsInProposeMode = !IsInProposeMode;
		CurrentActiveModeText = (IsInProposeMode ? OtherPoliciesText : ActivePoliciesText);
		CurrentActionText = (IsInProposeMode ? DisavowPolicyText : ProposeNewPolicyText);
		SetDefaultSelectedPolicy();
	}

	private void ExecuteProposeOrDisavow()
	{
		if (_currentItemsUnresolvedDecision != null)
		{
			_forceDecide(_currentItemsUnresolvedDecision);
		}
		else if (CanProposeOrDisavowPolicy)
		{
			KingdomDecision kingdomDecision = new KingdomPolicyDecision(Clan.PlayerClan, _currentSelectedPolicyObject, IsPolicyActive(_currentSelectedPolicyObject));
			Clan.PlayerClan.Kingdom.AddDecision(kingdomDecision);
			_forceDecide(kingdomDecision);
		}
	}

	private static int CalculateLikelihood(PolicyObject policy)
	{
		KingdomElection kingdomElection = new KingdomElection(new KingdomPolicyDecision(Clan.PlayerClan, policy, Clan.PlayerClan.Kingdom.ActivePolicies.Contains(policy)));
		kingdomElection.SetupResultWithoutPlayerSupport();
		return TaleWorlds.Library.MathF.Round(kingdomElection.GetWinChanceForSponsor(Clan.PlayerClan) * 100f);
	}

	private static TextObject GetSupportText(PolicyObject policy)
	{
		KingdomPolicyDecision decision = new KingdomPolicyDecision(Clan.PlayerClan, policy, Clan.PlayerClan.Kingdom.ActivePolicies.Contains(policy));
		return GameTexts.FindText("str_decision_outcome_support_status", KingdomElection.GetElectionOutcomeSupport(decision, Clan.PlayerClan).ToString());
	}
}
