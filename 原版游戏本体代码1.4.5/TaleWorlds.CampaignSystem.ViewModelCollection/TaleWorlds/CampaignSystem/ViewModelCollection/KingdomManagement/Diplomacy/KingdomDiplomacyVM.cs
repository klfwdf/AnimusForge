using System;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Diplomacy;

public class KingdomDiplomacyVM : KingdomCategoryVM
{
	private enum DiplomacyItemType
	{
		None,
		War,
		Peace,
		Alliance,
		TradeAgreement
	}

	private readonly Action<KingdomDecision> _forceDecision;

	private readonly Kingdom _playerKingdom;

	private bool _isChangingDiplomacyItem;

	private MBBindingList<KingdomWarItemVM> _playerWars;

	private MBBindingList<KingdomTruceItemVM> _playerTruces;

	private KingdomWarSortControllerVM _warsSortController;

	private KingdomDiplomacyItemVM _currentSelectedItem;

	private SelectorVM<SelectorItemVM> _behaviorSelection;

	private HintViewModel _showStatBarsHint;

	private HintViewModel _showWarLogsHint;

	private string _playerWarsText;

	private string _numOfPlayerWarsText;

	private string _otherWarsText;

	private string _numOfOtherWarsText;

	private string _warsText;

	private string _behaviorSelectionTitle;

	private bool _isDisplayingWarLogs;

	private bool _isDisplayingStatComparisons;

	private bool _isWar;

	private MBBindingList<KingdomDiplomacyProposalActionItemVM> _actions;

	[DataSourceProperty]
	public MBBindingList<KingdomWarItemVM> PlayerWars
	{
		get
		{
			return _playerWars;
		}
		set
		{
			if (value != _playerWars)
			{
				_playerWars = value;
				OnPropertyChangedWithValue(value, "PlayerWars");
			}
		}
	}

	[DataSourceProperty]
	public bool IsDisplayingWarLogs
	{
		get
		{
			return _isDisplayingWarLogs;
		}
		set
		{
			if (value != _isDisplayingWarLogs)
			{
				_isDisplayingWarLogs = value;
				OnPropertyChangedWithValue(value, "IsDisplayingWarLogs");
			}
		}
	}

	[DataSourceProperty]
	public bool IsDisplayingStatComparisons
	{
		get
		{
			return _isDisplayingStatComparisons;
		}
		set
		{
			if (value != _isDisplayingStatComparisons)
			{
				_isDisplayingStatComparisons = value;
				OnPropertyChangedWithValue(value, "IsDisplayingStatComparisons");
			}
		}
	}

	[DataSourceProperty]
	public bool IsWar
	{
		get
		{
			return _isWar;
		}
		set
		{
			if (value != _isWar)
			{
				_isWar = value;
				if (!value)
				{
					ExecuteShowStatComparisons();
				}
				OnPropertyChangedWithValue(value, "IsWar");
			}
		}
	}

	[DataSourceProperty]
	public string BehaviorSelectionTitle
	{
		get
		{
			return _behaviorSelectionTitle;
		}
		set
		{
			if (value != _behaviorSelectionTitle)
			{
				_behaviorSelectionTitle = value;
				OnPropertyChangedWithValue(value, "BehaviorSelectionTitle");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<KingdomTruceItemVM> PlayerTruces
	{
		get
		{
			return _playerTruces;
		}
		set
		{
			if (value != _playerTruces)
			{
				_playerTruces = value;
				OnPropertyChangedWithValue(value, "PlayerTruces");
			}
		}
	}

	[DataSourceProperty]
	public KingdomDiplomacyItemVM CurrentSelectedDiplomacyItem
	{
		get
		{
			return _currentSelectedItem;
		}
		set
		{
			if (value != _currentSelectedItem)
			{
				_isChangingDiplomacyItem = true;
				_currentSelectedItem = value;
				IsWar = value is KingdomWarItemVM;
				OnPropertyChangedWithValue(value, "CurrentSelectedDiplomacyItem");
				_isChangingDiplomacyItem = false;
			}
		}
	}

	[DataSourceProperty]
	public KingdomWarSortControllerVM WarsSortController
	{
		get
		{
			return _warsSortController;
		}
		set
		{
			if (value != _warsSortController)
			{
				_warsSortController = value;
				OnPropertyChangedWithValue(value, "WarsSortController");
			}
		}
	}

	[DataSourceProperty]
	public string PlayerWarsText
	{
		get
		{
			return _playerWarsText;
		}
		set
		{
			if (value != _playerWarsText)
			{
				_playerWarsText = value;
				OnPropertyChangedWithValue(value, "PlayerWarsText");
			}
		}
	}

	[DataSourceProperty]
	public string WarsText
	{
		get
		{
			return _warsText;
		}
		set
		{
			if (value != _warsText)
			{
				_warsText = value;
				OnPropertyChangedWithValue(value, "WarsText");
			}
		}
	}

	[DataSourceProperty]
	public string NumOfPlayerWarsText
	{
		get
		{
			return _numOfPlayerWarsText;
		}
		set
		{
			if (value != _numOfPlayerWarsText)
			{
				_numOfPlayerWarsText = value;
				OnPropertyChangedWithValue(value, "NumOfPlayerWarsText");
			}
		}
	}

	[DataSourceProperty]
	public string PlayerTrucesText
	{
		get
		{
			return _otherWarsText;
		}
		set
		{
			if (value != _otherWarsText)
			{
				_otherWarsText = value;
				OnPropertyChangedWithValue(value, "PlayerTrucesText");
			}
		}
	}

	[DataSourceProperty]
	public string NumOfPlayerTrucesText
	{
		get
		{
			return _numOfOtherWarsText;
		}
		set
		{
			if (value != _numOfOtherWarsText)
			{
				_numOfOtherWarsText = value;
				OnPropertyChangedWithValue(value, "NumOfPlayerTrucesText");
			}
		}
	}

	[DataSourceProperty]
	public SelectorVM<SelectorItemVM> BehaviorSelection
	{
		get
		{
			return _behaviorSelection;
		}
		set
		{
			if (value != _behaviorSelection)
			{
				_behaviorSelection = value;
				OnPropertyChangedWithValue(value, "BehaviorSelection");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel ShowStatBarsHint
	{
		get
		{
			return _showStatBarsHint;
		}
		set
		{
			if (value != _showStatBarsHint)
			{
				_showStatBarsHint = value;
				OnPropertyChangedWithValue(value, "ShowStatBarsHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel ShowWarLogsHint
	{
		get
		{
			return _showWarLogsHint;
		}
		set
		{
			if (value != _showWarLogsHint)
			{
				_showWarLogsHint = value;
				OnPropertyChangedWithValue(value, "ShowWarLogsHint");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<KingdomDiplomacyProposalActionItemVM> Actions
	{
		get
		{
			return _actions;
		}
		set
		{
			if (value != _actions)
			{
				_actions = value;
				OnPropertyChangedWithValue(value, "Actions");
			}
		}
	}

	public KingdomDiplomacyVM(Action<KingdomDecision> forceDecision)
	{
		_forceDecision = forceDecision;
		_playerKingdom = Hero.MainHero.MapFaction as Kingdom;
		PlayerWars = new MBBindingList<KingdomWarItemVM>();
		PlayerTruces = new MBBindingList<KingdomTruceItemVM>();
		WarsSortController = new KingdomWarSortControllerVM(ref _playerWars);
		Actions = new MBBindingList<KingdomDiplomacyProposalActionItemVM>();
		ExecuteShowStatComparisons();
		RefreshValues();
		SetDefaultSelectedItem();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		BehaviorSelection = new SelectorVM<SelectorItemVM>(0, OnBehaviorSelectionChanged);
		BehaviorSelection.AddItem(new SelectorItemVM(GameTexts.FindText("str_kingdom_war_strategy_balanced"), GameTexts.FindText("str_kingdom_war_strategy_balanced_desc")));
		BehaviorSelection.AddItem(new SelectorItemVM(GameTexts.FindText("str_kingdom_war_strategy_defensive"), GameTexts.FindText("str_kingdom_war_strategy_defensive_desc")));
		BehaviorSelection.AddItem(new SelectorItemVM(GameTexts.FindText("str_kingdom_war_strategy_offensive"), GameTexts.FindText("str_kingdom_war_strategy_offensive_desc")));
		RefreshDiplomacyList();
		BehaviorSelectionTitle = GameTexts.FindText("str_kingdom_war_strategy").ToString();
		base.NoItemSelectedText = GameTexts.FindText("str_kingdom_no_war_selected").ToString();
		PlayerWarsText = GameTexts.FindText("str_kingdom_at_war").ToString();
		PlayerTrucesText = GameTexts.FindText("str_kingdom_at_peace").ToString();
		WarsText = GameTexts.FindText("str_diplomatic_group").ToString();
		ShowStatBarsHint = new HintViewModel(GameTexts.FindText("str_kingdom_war_show_comparison_bars"));
		ShowWarLogsHint = new HintViewModel(GameTexts.FindText("str_kingdom_war_show_war_logs"));
		PlayerWars.ApplyActionOnAllItems(delegate(KingdomWarItemVM x)
		{
			x.RefreshValues();
		});
		PlayerTruces.ApplyActionOnAllItems(delegate(KingdomTruceItemVM x)
		{
			x.RefreshValues();
		});
		CurrentSelectedDiplomacyItem?.RefreshValues();
		Actions.ApplyActionOnAllItems(delegate(KingdomDiplomacyProposalActionItemVM x)
		{
			x.RefreshValues();
		});
	}

	public void RefreshDiplomacyList()
	{
		base.NotificationCount = Clan.PlayerClan.Kingdom?.UnresolvedDecisions.Count((KingdomDecision d) => !d.ShouldBeCancelled()) ?? 0;
		PlayerWars.Clear();
		PlayerTruces.Clear();
		foreach (StanceLink item in from x in _playerKingdom.FactionsAtWarWith
			select _playerKingdom.GetStanceWith(x) into w
			orderby w.Faction1.Name.ToString() + w.Faction2.Name.ToString()
			select w)
		{
			if (item.Faction1.IsKingdomFaction && item.Faction2.IsKingdomFaction)
			{
				PlayerWars.Add(new KingdomWarItemVM(item, OnDiplomacyItemSelection));
			}
		}
		foreach (Kingdom item2 in Kingdom.All)
		{
			if (item2 != _playerKingdom && !item2.IsEliminated && (DiplomacyHelper.IsSameFactionAndNotEliminated(item2, _playerKingdom) || FactionManager.IsNeutralWithFaction(item2, _playerKingdom)))
			{
				PlayerTruces.Add(new KingdomTruceItemVM(_playerKingdom, item2, OnDiplomacyItemSelection));
			}
		}
		GameTexts.SetVariable("STR", PlayerWars.Count);
		NumOfPlayerWarsText = GameTexts.FindText("str_STR_in_parentheses").ToString();
		GameTexts.SetVariable("STR", PlayerTruces.Count);
		NumOfPlayerTrucesText = GameTexts.FindText("str_STR_in_parentheses").ToString();
		SetDefaultSelectedItem();
	}

	public void SelectKingdom(Kingdom kingdom)
	{
		bool flag = false;
		foreach (KingdomWarItemVM playerWar in PlayerWars)
		{
			if (playerWar.Faction1 == kingdom || playerWar.Faction2 == kingdom)
			{
				OnSetCurrentDiplomacyItem(playerWar);
				flag = true;
				break;
			}
		}
		if (flag)
		{
			return;
		}
		foreach (KingdomTruceItemVM playerTruce in PlayerTruces)
		{
			if (playerTruce.Faction1 == kingdom || playerTruce.Faction2 == kingdom)
			{
				OnSetCurrentDiplomacyItem(playerTruce);
				flag = true;
				break;
			}
		}
	}

	private void OnSetCurrentDiplomacyItem(KingdomDiplomacyItemVM item)
	{
		Actions.Clear();
		if (item is KingdomWarItemVM)
		{
			OnSetWarItem(item as KingdomWarItemVM);
		}
		else if (item is KingdomTruceItemVM)
		{
			OnSetPeaceItem(item as KingdomTruceItemVM);
		}
		RefreshCurrentWarVisuals(item);
		UpdateBehaviorSelection();
	}

	private void OnSetWarItem(KingdomWarItemVM item)
	{
		KingdomDecision unresolvedPeaceDecision = Clan.PlayerClan.Kingdom.UnresolvedDecisions.FirstOrDefault((KingdomDecision d) => d is MakePeaceKingdomDecision makePeaceKingdomDecision && makePeaceKingdomDecision.FactionToMakePeaceWith == item.Faction2 && !d.ShouldBeCancelled());
		if (unresolvedPeaceDecision != null)
		{
			Actions.Add(new KingdomDiplomacyProposalActionItemVM(GameTexts.FindText("str_resolve"), GameTexts.FindText("str_resolve_explanation"), 0, GetAreProposalActionsEnabledWithReason(DiplomacyItemType.Peace, 0f, out var disabledReason), disabledReason, delegate
			{
				_forceDecision(unresolvedPeaceDecision);
			}));
			return;
		}
		int durationInDays;
		int dailyPeaceTributeToPay = Campaign.Current.Models.DiplomacyModel.GetDailyTributeToPay(Clan.PlayerClan, item.Faction2.Leader.Clan, out durationInDays);
		dailyPeaceTributeToPay = 10 * (dailyPeaceTributeToPay / 10);
		TextObject textObject = ((dailyPeaceTributeToPay == 0) ? GameTexts.FindText("str_propose_peace_explanation") : ((dailyPeaceTributeToPay > 0) ? GameTexts.FindText("str_propose_peace_explanation_pay_tribute") : GameTexts.FindText("str_propose_peace_explanation_get_tribute")));
		textObject.SetTextVariable("SUPPORT", CalculatePeaceSupport(item.Faction2, dailyPeaceTributeToPay, durationInDays)).SetTextVariable("TRIBUTE_AMOUNT", TaleWorlds.Library.MathF.Abs(dailyPeaceTributeToPay)).SetTextVariable("TRIBUTE_DURATION", durationInDays);
		int influenceCostOfProposingPeace = Campaign.Current.Models.DiplomacyModel.GetInfluenceCostOfProposingPeace(Clan.PlayerClan);
		Actions.Add(new KingdomDiplomacyProposalActionItemVM((_playerKingdom.Clans.Count > 1) ? GameTexts.FindText("str_policy_propose") : GameTexts.FindText("str_policy_enact"), textObject, influenceCostOfProposingPeace, GetIsProposingPeaceEnabledWithReason(item, influenceCostOfProposingPeace, out var disabledReason2), disabledReason2, delegate
		{
			OnDeclarePeace(item, dailyPeaceTributeToPay, durationInDays);
		}));
	}

	private void OnSetPeaceItem(KingdomTruceItemVM item)
	{
		KingdomDecision unresolvedAllianceDecision = Clan.PlayerClan.Kingdom.UnresolvedDecisions.FirstOrDefault((KingdomDecision d) => d is StartAllianceDecision startAllianceDecision && startAllianceDecision.KingdomToStartAllianceWith == item.Faction2 && !d.ShouldBeCancelled());
		if (unresolvedAllianceDecision != null)
		{
			Actions.Add(new KingdomDiplomacyProposalActionItemVM(GameTexts.FindText("str_resolve"), GameTexts.FindText("str_resolve_explanation"), 0, GetAreProposalActionsEnabledWithReason(DiplomacyItemType.Alliance, 0f, out var disabledReason), disabledReason, delegate
			{
				_forceDecision(unresolvedAllianceDecision);
			}));
		}
		else if (!DiplomacyHelper.HasAllianceWithFaction(item.Faction1, item.Faction2))
		{
			int influenceCostOfProposingStartingAlliance = Campaign.Current.Models.AllianceModel.GetInfluenceCostOfProposingStartingAlliance(Clan.PlayerClan);
			Actions.Add(new KingdomDiplomacyProposalActionItemVM((_playerKingdom.Clans.Count > 1) ? GameTexts.FindText("str_policy_propose") : GameTexts.FindText("str_policy_enact"), GameTexts.FindText("str_propose_alliance_explanation").SetTextVariable("SUPPORT", CalculateAllianceSupport(item.Faction2)), influenceCostOfProposingStartingAlliance, GetIsProposingAllianceEnabledWithReason(item, influenceCostOfProposingStartingAlliance, out var disabledReason2), disabledReason2, delegate
			{
				OnStartAlliance(item);
			}));
		}
		KingdomDecision unresolvedWarDecision = Clan.PlayerClan.Kingdom.UnresolvedDecisions.FirstOrDefault((KingdomDecision d) => d is DeclareWarDecision declareWarDecision && declareWarDecision.FactionToDeclareWarOn == item.Faction2 && !d.ShouldBeCancelled());
		if (unresolvedWarDecision != null)
		{
			Actions.Add(new KingdomDiplomacyProposalActionItemVM(GameTexts.FindText("str_resolve"), GameTexts.FindText("str_resolve_explanation"), 0, GetAreProposalActionsEnabledWithReason(DiplomacyItemType.War, 0f, out var disabledReason3), disabledReason3, delegate
			{
				_forceDecision(unresolvedWarDecision);
			}));
		}
		else
		{
			int influenceCostOfProposingWar = Campaign.Current.Models.DiplomacyModel.GetInfluenceCostOfProposingWar(Clan.PlayerClan);
			Actions.Add(new KingdomDiplomacyProposalActionItemVM((_playerKingdom.Clans.Count > 1) ? GameTexts.FindText("str_policy_propose") : GameTexts.FindText("str_policy_enact"), GameTexts.FindText("str_propose_war_explanation").SetTextVariable("SUPPORT", CalculateWarSupport(item.Faction2)), influenceCostOfProposingWar, GetIsProposingWarEnabledWithReason(item, influenceCostOfProposingWar, out var disabledReason4), disabledReason4, delegate
			{
				OnDeclareWar(item);
			}));
		}
		KingdomDecision unresolvedTradeAgreementDecision = Clan.PlayerClan.Kingdom.UnresolvedDecisions.FirstOrDefault((KingdomDecision d) => d is TradeAgreementDecision tradeAgreementDecision && tradeAgreementDecision.TargetKingdom == item.Faction2 && !d.ShouldBeCancelled());
		if (unresolvedTradeAgreementDecision != null)
		{
			Actions.Add(new KingdomDiplomacyProposalActionItemVM(GameTexts.FindText("str_resolve"), GameTexts.FindText("str_resolve_explanation"), 0, GetAreProposalActionsEnabledWithReason(DiplomacyItemType.TradeAgreement, 0f, out var disabledReason5), disabledReason5, delegate
			{
				_forceDecision(unresolvedTradeAgreementDecision);
			}));
			return;
		}
		ITradeAgreementsCampaignBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<ITradeAgreementsCampaignBehavior>();
		if (campaignBehavior != null && !campaignBehavior.HasTradeAgreement(item.Faction1 as Kingdom, item.Faction2 as Kingdom, out var _))
		{
			int influenceCostOfProposingTradeAgreement = Campaign.Current.Models.TradeAgreementModel.GetInfluenceCostOfProposingTradeAgreement(Clan.PlayerClan);
			Actions.Add(new KingdomDiplomacyProposalActionItemVM((_playerKingdom.Clans.Count > 1) ? GameTexts.FindText("str_policy_propose") : GameTexts.FindText("str_policy_enact"), GameTexts.FindText("str_propose_trade_agreement_explanation").SetTextVariable("SUPPORT", CalculateTradeAgreementSupport(item.Faction2)), influenceCostOfProposingTradeAgreement, GetIsProposingTradeAgreementEnabledWithReason(item, influenceCostOfProposingTradeAgreement, out var disabledReason6), disabledReason6, delegate
			{
				OnStartTradeAgreement(item);
			}));
		}
	}

	private bool GetIsProposingWarEnabledWithReason(KingdomTruceItemVM item, float actionInfluenceCost, out TextObject disabledReason)
	{
		if (!GetAreProposalActionsEnabledWithReason(DiplomacyItemType.War, actionInfluenceCost, out var disabledReason2))
		{
			disabledReason = disabledReason2;
			return false;
		}
		if (!Campaign.Current.Models.KingdomDecisionPermissionModel.IsWarDecisionAllowedBetweenKingdoms(item.Faction1 as Kingdom, item.Faction2 as Kingdom, out var reason))
		{
			disabledReason = reason;
			return false;
		}
		disabledReason = TextObject.GetEmpty();
		return true;
	}

	private bool GetIsProposingPeaceEnabledWithReason(KingdomWarItemVM item, float actionInfluenceCost, out TextObject disabledReason)
	{
		if (!GetAreProposalActionsEnabledWithReason(DiplomacyItemType.Peace, actionInfluenceCost, out var disabledReason2))
		{
			disabledReason = disabledReason2;
			return false;
		}
		if (!Campaign.Current.Models.KingdomDecisionPermissionModel.IsPeaceDecisionAllowedBetweenKingdoms(item.Faction1 as Kingdom, item.Faction2 as Kingdom, out var reason))
		{
			disabledReason = reason;
			return false;
		}
		disabledReason = TextObject.GetEmpty();
		return true;
	}

	private bool GetIsProposingAllianceEnabledWithReason(KingdomTruceItemVM item, float actionInfluenceCost, out TextObject disabledReason)
	{
		if (!GetAreProposalActionsEnabledWithReason(DiplomacyItemType.Alliance, actionInfluenceCost, out var disabledReason2))
		{
			disabledReason = disabledReason2;
			return false;
		}
		if (!new StartAllianceDecision(Clan.PlayerClan, item.Faction2 as Kingdom).CanMakeDecision(out var reason, includeReason: true))
		{
			disabledReason = reason;
			return false;
		}
		disabledReason = TextObject.GetEmpty();
		return true;
	}

	private bool GetIsProposingTradeAgreementEnabledWithReason(KingdomTruceItemVM item, float actionInfluenceCost, out TextObject disabledReason)
	{
		if (!GetAreProposalActionsEnabledWithReason(DiplomacyItemType.TradeAgreement, actionInfluenceCost, out var disabledReason2))
		{
			disabledReason = disabledReason2;
			return false;
		}
		if (!new TradeAgreementDecision(Clan.PlayerClan, item.Faction2 as Kingdom).CanMakeDecision(out var reason, includeReason: true))
		{
			disabledReason = reason;
			return false;
		}
		disabledReason = TextObject.GetEmpty();
		return true;
	}

	private bool GetAreProposalActionsEnabledWithReason(DiplomacyItemType diplomacyItemType, float actionInfluenceCost, out TextObject disabledReason)
	{
		if (!CampaignUIHelper.GetMapScreenActionIsEnabledWithReason(out var disabledReason2))
		{
			disabledReason = disabledReason2;
			return false;
		}
		if (Clan.PlayerClan.IsUnderMercenaryService)
		{
			switch (diplomacyItemType)
			{
			case DiplomacyItemType.War:
			case DiplomacyItemType.Peace:
				disabledReason = GameTexts.FindText("str_cannot_propose_war_truce_while_mercenary");
				break;
			case DiplomacyItemType.Alliance:
				disabledReason = GameTexts.FindText("str_cannot_propose_alliance_while_mercenary");
				break;
			case DiplomacyItemType.TradeAgreement:
				disabledReason = GameTexts.FindText("str_cannot_propose_trade_agreement_while_mercenary");
				break;
			default:
				disabledReason = TextObject.GetEmpty();
				break;
			}
			return false;
		}
		if (actionInfluenceCost > 0f && Clan.PlayerClan.Influence < actionInfluenceCost)
		{
			disabledReason = GameTexts.FindText("str_warning_you_dont_have_enough_influence");
			return false;
		}
		disabledReason = TextObject.GetEmpty();
		return true;
	}

	private void RefreshCurrentWarVisuals(KingdomDiplomacyItemVM item)
	{
		if (item != null)
		{
			if (CurrentSelectedDiplomacyItem != null)
			{
				CurrentSelectedDiplomacyItem.IsSelected = false;
			}
			CurrentSelectedDiplomacyItem = item;
			if (CurrentSelectedDiplomacyItem != null)
			{
				CurrentSelectedDiplomacyItem.IsSelected = true;
			}
		}
	}

	private void OnDiplomacyItemSelection(KingdomDiplomacyItemVM item)
	{
		if (CurrentSelectedDiplomacyItem != item)
		{
			if (CurrentSelectedDiplomacyItem != null)
			{
				CurrentSelectedDiplomacyItem.IsSelected = false;
			}
			CurrentSelectedDiplomacyItem = item;
			base.IsAcceptableItemSelected = item != null;
			OnSetCurrentDiplomacyItem(item);
		}
	}

	private void OnDeclareWar(KingdomTruceItemVM item)
	{
		DeclareWarDecision declareWarDecision = new DeclareWarDecision(Clan.PlayerClan, item.Faction2);
		Clan.PlayerClan.Kingdom.AddDecision(declareWarDecision);
		_forceDecision(declareWarDecision);
	}

	private void OnDeclarePeace(KingdomWarItemVM item, int tributeToPay, int tributeDurationInDays)
	{
		MakePeaceKingdomDecision makePeaceKingdomDecision = new MakePeaceKingdomDecision(Clan.PlayerClan, item.Faction2 as Kingdom, tributeToPay, tributeDurationInDays);
		Clan.PlayerClan.Kingdom.AddDecision(makePeaceKingdomDecision);
		_forceDecision(makePeaceKingdomDecision);
	}

	private void OnStartAlliance(KingdomTruceItemVM item)
	{
		if (item.Faction2.IsKingdomFaction)
		{
			StartAllianceDecision startAllianceDecision = new StartAllianceDecision(Clan.PlayerClan, (Kingdom)item.Faction2);
			Clan.PlayerClan.Kingdom.AddDecision(startAllianceDecision);
			_forceDecision(startAllianceDecision);
		}
	}

	private void OnStartTradeAgreement(KingdomTruceItemVM item)
	{
		if (item.Faction2.IsKingdomFaction)
		{
			TradeAgreementDecision tradeAgreementDecision = new TradeAgreementDecision(Clan.PlayerClan, (Kingdom)item.Faction2);
			Clan.PlayerClan.Kingdom.AddDecision(tradeAgreementDecision);
			_forceDecision(tradeAgreementDecision);
		}
	}

	private void ExecuteShowWarLogs()
	{
		IsDisplayingWarLogs = true;
		IsDisplayingStatComparisons = false;
	}

	private void ExecuteShowStatComparisons()
	{
		IsDisplayingWarLogs = false;
		IsDisplayingStatComparisons = true;
	}

	private void SetDefaultSelectedItem()
	{
		KingdomDiplomacyItemVM kingdomDiplomacyItemVM = PlayerWars.FirstOrDefault();
		KingdomDiplomacyItemVM kingdomDiplomacyItemVM2 = PlayerTruces.FirstOrDefault();
		OnDiplomacyItemSelection(kingdomDiplomacyItemVM ?? kingdomDiplomacyItemVM2);
	}

	private void UpdateBehaviorSelection()
	{
		if (Hero.MainHero.MapFaction.IsKingdomFaction && Hero.MainHero.MapFaction.Leader == Hero.MainHero && CurrentSelectedDiplomacyItem != null)
		{
			StanceLink stanceWith = Hero.MainHero.MapFaction.GetStanceWith(CurrentSelectedDiplomacyItem.Faction2);
			BehaviorSelection.SelectedIndex = stanceWith.BehaviorPriority;
		}
	}

	private void OnBehaviorSelectionChanged(SelectorVM<SelectorItemVM> s)
	{
		if (!_isChangingDiplomacyItem && Hero.MainHero.MapFaction.IsKingdomFaction && Hero.MainHero.MapFaction.Leader == Hero.MainHero && CurrentSelectedDiplomacyItem != null)
		{
			Hero.MainHero.MapFaction.GetStanceWith(CurrentSelectedDiplomacyItem.Faction2).BehaviorPriority = s.SelectedIndex;
		}
	}

	private TextObject CalculateWarSupport(IFaction faction)
	{
		DeclareWarDecision decision = new DeclareWarDecision(Clan.PlayerClan, faction);
		return GameTexts.FindText("str_decision_outcome_support_status", KingdomElection.GetElectionOutcomeSupport(decision, Clan.PlayerClan).ToString());
	}

	private TextObject CalculateAllianceSupport(IFaction faction)
	{
		StartAllianceDecision decision = new StartAllianceDecision(Clan.PlayerClan, faction as Kingdom);
		return GameTexts.FindText("str_decision_outcome_support_status", KingdomElection.GetElectionOutcomeSupport(decision, Clan.PlayerClan).ToString());
	}

	private TextObject CalculatePeaceSupport(IFaction faction, int dailyTributeToBePaid, int durationInDays)
	{
		MakePeaceKingdomDecision decision = new MakePeaceKingdomDecision(Clan.PlayerClan, faction, dailyTributeToBePaid, durationInDays);
		return GameTexts.FindText("str_decision_outcome_support_status", KingdomElection.GetElectionOutcomeSupport(decision, Clan.PlayerClan).ToString());
	}

	private TextObject CalculateTradeAgreementSupport(IFaction faction)
	{
		TradeAgreementDecision decision = new TradeAgreementDecision(Clan.PlayerClan, faction as Kingdom);
		return GameTexts.FindText("str_decision_outcome_support_status", KingdomElection.GetElectionOutcomeSupport(decision, Clan.PlayerClan).ToString());
	}
}
