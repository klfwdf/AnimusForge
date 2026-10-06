using System;
using System.Linq;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Clans;

public class KingdomClanVM : KingdomCategoryVM
{
	private Action<KingdomDecision> _forceDecide;

	private bool _isThereAPendingDecisionToExpelThisClan;

	private MBBindingList<KingdomClanItemVM> _clans;

	private HintViewModel _expelHint;

	private HintViewModel _supportHint;

	private string _bannerText;

	private string _nameText;

	private string _influenceText;

	private string _membersText;

	private string _fiefsText;

	private string _typeText;

	private string _expelActionText;

	private string _expelActionExplanationText;

	private string _supportActionExplanationText;

	private int _expelCost;

	private string _supportText;

	private int _supportCost;

	private bool _canSupportCurrentClan;

	private bool _canExpelCurrentClan;

	private KingdomClanItemVM _currentSelectedClan;

	private KingdomClanSortControllerVM _clanSortController;

	[DataSourceProperty]
	public KingdomClanSortControllerVM ClanSortController
	{
		get
		{
			return _clanSortController;
		}
		set
		{
			if (value != _clanSortController)
			{
				_clanSortController = value;
				OnPropertyChangedWithValue(value, "ClanSortController");
			}
		}
	}

	[DataSourceProperty]
	public KingdomClanItemVM CurrentSelectedClan
	{
		get
		{
			return _currentSelectedClan;
		}
		set
		{
			if (value != _currentSelectedClan)
			{
				_currentSelectedClan = value;
				OnPropertyChangedWithValue(value, "CurrentSelectedClan");
			}
		}
	}

	[DataSourceProperty]
	public string ExpelActionExplanationText
	{
		get
		{
			return _expelActionExplanationText;
		}
		set
		{
			if (value != _expelActionExplanationText)
			{
				_expelActionExplanationText = value;
				OnPropertyChangedWithValue(value, "ExpelActionExplanationText");
			}
		}
	}

	[DataSourceProperty]
	public string SupportActionExplanationText
	{
		get
		{
			return _supportActionExplanationText;
		}
		set
		{
			if (value != _supportActionExplanationText)
			{
				_supportActionExplanationText = value;
				OnPropertyChangedWithValue(value, "SupportActionExplanationText");
			}
		}
	}

	[DataSourceProperty]
	public string BannerText
	{
		get
		{
			return _bannerText;
		}
		set
		{
			if (value != _bannerText)
			{
				_bannerText = value;
				OnPropertyChangedWithValue(value, "BannerText");
			}
		}
	}

	[DataSourceProperty]
	public string TypeText
	{
		get
		{
			return _typeText;
		}
		set
		{
			if (value != _typeText)
			{
				_typeText = value;
				OnPropertyChangedWithValue(value, "TypeText");
			}
		}
	}

	[DataSourceProperty]
	public string NameText
	{
		get
		{
			return _nameText;
		}
		set
		{
			if (value != _nameText)
			{
				_nameText = value;
				OnPropertyChangedWithValue(value, "NameText");
			}
		}
	}

	[DataSourceProperty]
	public string InfluenceText
	{
		get
		{
			return _influenceText;
		}
		set
		{
			if (value != _influenceText)
			{
				_influenceText = value;
				OnPropertyChangedWithValue(value, "InfluenceText");
			}
		}
	}

	[DataSourceProperty]
	public string FiefsText
	{
		get
		{
			return _fiefsText;
		}
		set
		{
			if (value != _fiefsText)
			{
				_fiefsText = value;
				OnPropertyChangedWithValue(value, "FiefsText");
			}
		}
	}

	[DataSourceProperty]
	public string MembersText
	{
		get
		{
			return _membersText;
		}
		set
		{
			if (value != _membersText)
			{
				_membersText = value;
				OnPropertyChangedWithValue(value, "MembersText");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<KingdomClanItemVM> Clans
	{
		get
		{
			return _clans;
		}
		set
		{
			if (value != _clans)
			{
				_clans = value;
				OnPropertyChangedWithValue(value, "Clans");
			}
		}
	}

	[DataSourceProperty]
	public bool CanSupportCurrentClan
	{
		get
		{
			return _canSupportCurrentClan;
		}
		set
		{
			if (value != _canSupportCurrentClan)
			{
				_canSupportCurrentClan = value;
				OnPropertyChangedWithValue(value, "CanSupportCurrentClan");
			}
		}
	}

	[DataSourceProperty]
	public bool CanExpelCurrentClan
	{
		get
		{
			return _canExpelCurrentClan;
		}
		set
		{
			if (value != _canExpelCurrentClan)
			{
				_canExpelCurrentClan = value;
				OnPropertyChangedWithValue(value, "CanExpelCurrentClan");
			}
		}
	}

	[DataSourceProperty]
	public string SupportText
	{
		get
		{
			return _supportText;
		}
		set
		{
			if (value != _supportText)
			{
				_supportText = value;
				OnPropertyChangedWithValue(value, "SupportText");
			}
		}
	}

	[DataSourceProperty]
	public string ExpelActionText
	{
		get
		{
			return _expelActionText;
		}
		set
		{
			if (value != _expelActionText)
			{
				_expelActionText = value;
				OnPropertyChangedWithValue(value, "ExpelActionText");
			}
		}
	}

	[DataSourceProperty]
	public int SupportCost
	{
		get
		{
			return _supportCost;
		}
		set
		{
			if (value != _supportCost)
			{
				_supportCost = value;
				OnPropertyChangedWithValue(value, "SupportCost");
			}
		}
	}

	[DataSourceProperty]
	public int ExpelCost
	{
		get
		{
			return _expelCost;
		}
		set
		{
			if (value != _expelCost)
			{
				_expelCost = value;
				OnPropertyChangedWithValue(value, "ExpelCost");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel ExpelHint
	{
		get
		{
			return _expelHint;
		}
		set
		{
			if (value != _expelHint)
			{
				_expelHint = value;
				OnPropertyChangedWithValue(value, "ExpelHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel SupportHint
	{
		get
		{
			return _supportHint;
		}
		set
		{
			if (value != _supportHint)
			{
				_supportHint = value;
				OnPropertyChangedWithValue(value, "SupportHint");
			}
		}
	}

	public KingdomClanVM(Action<KingdomDecision> forceDecide)
	{
		_forceDecide = forceDecide;
		SupportHint = new HintViewModel();
		ExpelHint = new HintViewModel();
		_clans = new MBBindingList<KingdomClanItemVM>();
		base.IsAcceptableItemSelected = false;
		RefreshClanList();
		base.NotificationCount = 0;
		SupportCost = Campaign.Current.Models.DiplomacyModel.GetInfluenceCostOfSupportingClan();
		ExpelCost = Campaign.Current.Models.DiplomacyModel.GetInfluenceCostOfExpellingClan(Clan.PlayerClan);
		CanSupportCurrentClan = GetCanSupportCurrentClanWithReason(SupportCost, out var disabledReason);
		SupportHint.HintText = disabledReason;
		CanExpelCurrentClan = GetCanExpelCurrentClanWithReason(_isThereAPendingDecisionToExpelThisClan, ExpelCost, out var disabledReason2);
		ExpelHint.HintText = disabledReason2;
		ClanSortController = new KingdomClanSortControllerVM(ref _clans);
		CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		SupportText = new TextObject("{=N63XYX2r}Support").ToString();
		NameText = GameTexts.FindText("str_scoreboard_header", "name").ToString();
		InfluenceText = GameTexts.FindText("str_influence").ToString();
		FiefsText = GameTexts.FindText("str_fiefs").ToString();
		MembersText = GameTexts.FindText("str_members").ToString();
		BannerText = GameTexts.FindText("str_banner").ToString();
		TypeText = GameTexts.FindText("str_sort_by_type_label").ToString();
		base.CategoryNameText = new TextObject("{=j4F7tTzy}Clan").ToString();
		base.NoItemSelectedText = GameTexts.FindText("str_kingdom_no_clan_selected").ToString();
		SupportActionExplanationText = GameTexts.FindText("str_support_clan_action_explanation").ToString();
		ExpelActionExplanationText = GameTexts.FindText("str_expel_clan_action_explanation").SetTextVariable("SUPPORT", GameTexts.FindText("str_decision_outcome_support_status", KingdomElection.ElectionOutcomeSupport.LowSupport.ToString())).ToString();
	}

	private void SetCurrentSelectedClan(KingdomClanItemVM clan)
	{
		if (clan != CurrentSelectedClan)
		{
			if (CurrentSelectedClan != null)
			{
				CurrentSelectedClan.IsSelected = false;
			}
			CurrentSelectedClan = clan;
			CurrentSelectedClan.IsSelected = true;
			SupportCost = Campaign.Current.Models.DiplomacyModel.GetInfluenceCostOfSupportingClan();
			_isThereAPendingDecisionToExpelThisClan = Clan.PlayerClan.Kingdom.UnresolvedDecisions.Any((KingdomDecision x) => x is ExpelClanFromKingdomDecision expelClanFromKingdomDecision && expelClanFromKingdomDecision.ClanToExpel == CurrentSelectedClan.Clan && !x.ShouldBeCancelled());
			CanExpelCurrentClan = GetCanExpelCurrentClanWithReason(_isThereAPendingDecisionToExpelThisClan, ExpelCost, out var disabledReason);
			ExpelHint.HintText = disabledReason;
			if (_isThereAPendingDecisionToExpelThisClan)
			{
				ExpelActionText = GameTexts.FindText("str_resolve").ToString();
				ExpelActionExplanationText = GameTexts.FindText("str_resolve_explanation").ToString();
				ExpelCost = 0;
				return;
			}
			ExpelActionText = GameTexts.FindText("str_policy_propose").ToString();
			ExpelCost = Campaign.Current.Models.DiplomacyModel.GetInfluenceCostOfExpellingClan(Clan.PlayerClan);
			CanSupportCurrentClan = GetCanSupportCurrentClanWithReason(SupportCost, out var disabledReason2);
			SupportHint.HintText = disabledReason2;
			ExpelActionExplanationText = GameTexts.FindText("str_expel_clan_action_explanation").SetTextVariable("SUPPORT", GetExpelLikelihoodText(CurrentSelectedClan)).ToString();
			base.IsAcceptableItemSelected = CurrentSelectedClan != null;
		}
	}

	private TextObject GetExpelLikelihoodText(KingdomClanItemVM clan)
	{
		ExpelClanFromKingdomDecision decision = new ExpelClanFromKingdomDecision(Clan.PlayerClan, clan.Clan);
		return GameTexts.FindText("str_decision_outcome_support_status", KingdomElection.GetElectionOutcomeSupport(decision, Clan.PlayerClan).ToString());
	}

	private bool GetCanSupportCurrentClanWithReason(int supportCost, out TextObject disabledReason)
	{
		if (!CampaignUIHelper.GetMapScreenActionIsEnabledWithReason(out var disabledReason2))
		{
			disabledReason = disabledReason2;
			return false;
		}
		if (Hero.MainHero.Clan.Influence < (float)supportCost)
		{
			disabledReason = GameTexts.FindText("str_warning_you_dont_have_enough_influence");
			return false;
		}
		if (CurrentSelectedClan.Clan == Clan.PlayerClan)
		{
			disabledReason = GameTexts.FindText("str_cannot_support_your_clan");
			return false;
		}
		if (Hero.MainHero.Clan.IsUnderMercenaryService)
		{
			disabledReason = GameTexts.FindText("str_mercenaries_cannot_support_clans");
			return false;
		}
		disabledReason = TextObject.GetEmpty();
		return true;
	}

	private bool GetCanExpelCurrentClanWithReason(bool isThereAPendingDecision, int expelCost, out TextObject disabledReason)
	{
		if (!CampaignUIHelper.GetMapScreenActionIsEnabledWithReason(out var disabledReason2))
		{
			disabledReason = disabledReason2;
			return false;
		}
		if (Hero.MainHero.Clan.IsUnderMercenaryService)
		{
			disabledReason = GameTexts.FindText("str_mercenaries_cannot_expel_clans");
			return false;
		}
		if (!isThereAPendingDecision)
		{
			if (Hero.MainHero.Clan.Influence < (float)expelCost)
			{
				disabledReason = GameTexts.FindText("str_warning_you_dont_have_enough_influence");
				return false;
			}
			if (CurrentSelectedClan.Clan == Clan.PlayerClan)
			{
				disabledReason = GameTexts.FindText("str_cannot_expel_your_clan");
				return false;
			}
			if (CurrentSelectedClan.Clan == CurrentSelectedClan.Clan.Kingdom?.RulingClan)
			{
				disabledReason = GameTexts.FindText("str_cannot_expel_ruling_clan");
				return false;
			}
		}
		disabledReason = TextObject.GetEmpty();
		return true;
	}

	public void RefreshClan()
	{
		RefreshClanList();
		foreach (KingdomClanItemVM clan in Clans)
		{
			clan.Refresh();
		}
	}

	public void SelectClan(Clan clan)
	{
		foreach (KingdomClanItemVM clan2 in Clans)
		{
			if (clan2.Clan == clan)
			{
				OnClanSelection(clan2);
				break;
			}
		}
	}

	private void OnClanSelection(KingdomClanItemVM clan)
	{
		if (_currentSelectedClan != clan)
		{
			SetCurrentSelectedClan(clan);
		}
	}

	private void ExecuteExpelCurrentClan()
	{
		if (Hero.MainHero.Clan.Influence >= (float)ExpelCost)
		{
			KingdomDecision kingdomDecision = new ExpelClanFromKingdomDecision(Clan.PlayerClan, _currentSelectedClan.Clan);
			Clan.PlayerClan.Kingdom.AddDecision(kingdomDecision);
			_forceDecide(kingdomDecision);
		}
	}

	private void ExecuteSupport()
	{
		if (Hero.MainHero.Clan.Influence >= (float)SupportCost)
		{
			_currentSelectedClan.Clan.OnSupportedByClan(Hero.MainHero.Clan);
			Clan clan = _currentSelectedClan.Clan;
			RefreshClan();
			SelectClan(clan);
		}
	}

	private int CalculateExpelLikelihood(KingdomClanItemVM clan)
	{
		return TaleWorlds.Library.MathF.Round(new KingdomElection(new ExpelClanFromKingdomDecision(Clan.PlayerClan, clan.Clan)).GetLikelihoodForSponsor(Clan.PlayerClan) * 100f);
	}

	private void RefreshClanList()
	{
		Clans.Clear();
		if (Clan.PlayerClan.Kingdom != null)
		{
			foreach (Clan clan in Clan.PlayerClan.Kingdom.Clans)
			{
				Clans.Add(new KingdomClanItemVM(clan, OnClanSelection));
			}
		}
		if (Clans.Count > 0)
		{
			SetCurrentSelectedClan(Clans.FirstOrDefault());
		}
		if (ClanSortController != null)
		{
			ClanSortController.SortByCurrentState();
		}
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		CampaignEvents.OnClanChangedKingdomEvent.ClearListeners(this);
	}

	private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification)
	{
		if (clan != Clan.PlayerClan && (oldKingdom == Clan.PlayerClan.Kingdom || newKingdom == Clan.PlayerClan.Kingdom))
		{
			RefreshClanList();
		}
	}
}
