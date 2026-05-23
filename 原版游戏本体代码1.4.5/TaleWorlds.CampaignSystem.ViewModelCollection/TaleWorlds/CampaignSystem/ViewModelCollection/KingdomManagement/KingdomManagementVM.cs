using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.Input;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Armies;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Clans;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Decisions;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Diplomacy;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Policies;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement;

public class KingdomManagementVM : ViewModel
{
	private readonly Action _onClose;

	private readonly Action<Army> _onShowArmyOnMap;

	private readonly int _categoryCount;

	private readonly LeaveKingdomPermissionEvent _leaveKingdomPermissionEvent;

	private (bool, TextObject)? _mostRecentLeaveKingdomPermission;

	private int _currentCategory;

	private bool _isPlayerTheRuler;

	private KingdomArmyVM _army;

	private KingdomSettlementVM _settlement;

	private KingdomClanVM _clan;

	private KingdomPoliciesVM _policy;

	private KingdomDiplomacyVM _diplomacy;

	private KingdomGiftFiefPopupVM _giftFief;

	private BannerImageIdentifierVM _kingdomBanner;

	private HeroVM _leader;

	private KingdomDecisionsVM _decision;

	private HintViewModel _changeKingdomNameHint;

	private string _name;

	private bool _canSwitchTabs;

	private bool _playerHasKingdom;

	private bool _isKingdomActionEnabled;

	private bool _playerCanChangeKingdomName;

	private string _kingdomActionText;

	private string _leaderText;

	private string _clansText;

	private string _fiefsText;

	private string _policiesText;

	private string _armiesText;

	private string _diplomacyText;

	private string _doneText;

	private BasicTooltipViewModel _kingdomActionHint;

	private InputKeyItemVM _doneInputKey;

	private InputKeyItemVM _previousTabInputKey;

	private InputKeyItemVM _nextTabInputKey;

	public Kingdom Kingdom { get; private set; }

	[DataSourceProperty]
	public BasicTooltipViewModel KingdomActionHint
	{
		get
		{
			return _kingdomActionHint;
		}
		set
		{
			if (value != _kingdomActionHint)
			{
				_kingdomActionHint = value;
				OnPropertyChangedWithValue(value, "KingdomActionHint");
			}
		}
	}

	[DataSourceProperty]
	public BannerImageIdentifierVM KingdomBanner
	{
		get
		{
			return _kingdomBanner;
		}
		set
		{
			if (value != _kingdomBanner)
			{
				_kingdomBanner = value;
				OnPropertyChangedWithValue(value, "KingdomBanner");
			}
		}
	}

	[DataSourceProperty]
	public HeroVM Leader
	{
		get
		{
			return _leader;
		}
		set
		{
			if (value != _leader)
			{
				_leader = value;
				OnPropertyChangedWithValue(value, "Leader");
			}
		}
	}

	[DataSourceProperty]
	public KingdomArmyVM Army
	{
		get
		{
			return _army;
		}
		set
		{
			if (value != _army)
			{
				_army = value;
				OnPropertyChangedWithValue(value, "Army");
			}
		}
	}

	[DataSourceProperty]
	public KingdomSettlementVM Settlement
	{
		get
		{
			return _settlement;
		}
		set
		{
			if (value != _settlement)
			{
				_settlement = value;
				OnPropertyChangedWithValue(value, "Settlement");
			}
		}
	}

	[DataSourceProperty]
	public KingdomClanVM Clan
	{
		get
		{
			return _clan;
		}
		set
		{
			if (value != _clan)
			{
				_clan = value;
				OnPropertyChangedWithValue(value, "Clan");
			}
		}
	}

	[DataSourceProperty]
	public KingdomPoliciesVM Policy
	{
		get
		{
			return _policy;
		}
		set
		{
			if (value != _policy)
			{
				_policy = value;
				OnPropertyChangedWithValue(value, "Policy");
			}
		}
	}

	[DataSourceProperty]
	public KingdomDiplomacyVM Diplomacy
	{
		get
		{
			return _diplomacy;
		}
		set
		{
			if (value != _diplomacy)
			{
				_diplomacy = value;
				OnPropertyChangedWithValue(value, "Diplomacy");
			}
		}
	}

	[DataSourceProperty]
	public KingdomGiftFiefPopupVM GiftFief
	{
		get
		{
			return _giftFief;
		}
		set
		{
			if (value != _giftFief)
			{
				_giftFief = value;
				OnPropertyChangedWithValue(value, "GiftFief");
			}
		}
	}

	[DataSourceProperty]
	public KingdomDecisionsVM Decision
	{
		get
		{
			return _decision;
		}
		set
		{
			if (value != _decision)
			{
				_decision = value;
				OnPropertyChangedWithValue(value, "Decision");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel ChangeKingdomNameHint
	{
		get
		{
			return _changeKingdomNameHint;
		}
		set
		{
			if (value != _changeKingdomNameHint)
			{
				_changeKingdomNameHint = value;
				OnPropertyChangedWithValue(value, "ChangeKingdomNameHint");
			}
		}
	}

	[DataSourceProperty]
	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			if (value != _name)
			{
				_name = value;
				OnPropertyChangedWithValue(value, "Name");
			}
		}
	}

	[DataSourceProperty]
	public bool CanSwitchTabs
	{
		get
		{
			return _canSwitchTabs;
		}
		set
		{
			if (value != _canSwitchTabs)
			{
				_canSwitchTabs = value;
				OnPropertyChangedWithValue(value, "CanSwitchTabs");
			}
		}
	}

	[DataSourceProperty]
	public bool PlayerHasKingdom
	{
		get
		{
			return _playerHasKingdom;
		}
		set
		{
			if (value != _playerHasKingdom)
			{
				_playerHasKingdom = value;
				OnPropertyChangedWithValue(value, "PlayerHasKingdom");
			}
		}
	}

	[DataSourceProperty]
	public bool IsKingdomActionEnabled
	{
		get
		{
			return _isKingdomActionEnabled;
		}
		set
		{
			if (value != _isKingdomActionEnabled)
			{
				_isKingdomActionEnabled = value;
				OnPropertyChangedWithValue(value, "IsKingdomActionEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool PlayerCanChangeKingdomName
	{
		get
		{
			return _playerCanChangeKingdomName;
		}
		set
		{
			if (value != _playerCanChangeKingdomName)
			{
				_playerCanChangeKingdomName = value;
				OnPropertyChangedWithValue(value, "PlayerCanChangeKingdomName");
			}
		}
	}

	[DataSourceProperty]
	public string LeaderText
	{
		get
		{
			return _leaderText;
		}
		set
		{
			if (value != _leaderText)
			{
				_leaderText = value;
				OnPropertyChangedWithValue(value, "LeaderText");
			}
		}
	}

	[DataSourceProperty]
	public string KingdomActionText
	{
		get
		{
			return _kingdomActionText;
		}
		set
		{
			if (value != _kingdomActionText)
			{
				_kingdomActionText = value;
				OnPropertyChangedWithValue(value, "KingdomActionText");
			}
		}
	}

	[DataSourceProperty]
	public string ClansText
	{
		get
		{
			return _clansText;
		}
		set
		{
			if (value != _clansText)
			{
				_clansText = value;
				OnPropertyChangedWithValue(value, "ClansText");
			}
		}
	}

	[DataSourceProperty]
	public string DiplomacyText
	{
		get
		{
			return _diplomacyText;
		}
		set
		{
			if (value != _diplomacyText)
			{
				_diplomacyText = value;
				OnPropertyChangedWithValue(value, "DiplomacyText");
			}
		}
	}

	[DataSourceProperty]
	public string DoneText
	{
		get
		{
			return _doneText;
		}
		set
		{
			if (value != _doneText)
			{
				_doneText = value;
				OnPropertyChangedWithValue(value, "DoneText");
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
	public string ArmiesText
	{
		get
		{
			return _armiesText;
		}
		set
		{
			if (value != _armiesText)
			{
				_armiesText = value;
				OnPropertyChangedWithValue(value, "ArmiesText");
			}
		}
	}

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

	public InputKeyItemVM PreviousTabInputKey
	{
		get
		{
			return _previousTabInputKey;
		}
		set
		{
			if (value != _previousTabInputKey)
			{
				_previousTabInputKey = value;
				OnPropertyChangedWithValue(value, "PreviousTabInputKey");
			}
		}
	}

	public InputKeyItemVM NextTabInputKey
	{
		get
		{
			return _nextTabInputKey;
		}
		set
		{
			if (value != _nextTabInputKey)
			{
				_nextTabInputKey = value;
				OnPropertyChangedWithValue(value, "NextTabInputKey");
			}
		}
	}

	public KingdomManagementVM(Action onClose, Action onManageArmy, Action<Army> onShowArmyOnMap)
	{
		_onClose = onClose;
		_onShowArmyOnMap = onShowArmyOnMap;
		Army = new KingdomArmyVM(onManageArmy, OnRefreshDecision, _onShowArmyOnMap);
		Settlement = CreateSettlementVM(ForceDecideDecision, OnGrantFief);
		Clan = new KingdomClanVM(ForceDecideDecision);
		Policy = new KingdomPoliciesVM(ForceDecideDecision);
		Diplomacy = new KingdomDiplomacyVM(ForceDecideDecision);
		GiftFief = new KingdomGiftFiefPopupVM(OnSettlementGranted);
		Decision = new KingdomDecisionsVM(OnRefresh);
		_categoryCount = 5;
		_leaveKingdomPermissionEvent = new LeaveKingdomPermissionEvent(OnLeaveKingdomRequest);
		SetSelectedCategory(1);
		ChangeKingdomNameHint = new HintViewModel();
		RefreshValues();
	}

	protected virtual KingdomSettlementVM CreateSettlementVM(Action<KingdomDecision> forceDecision, Action<Settlement> onGrantFief)
	{
		return new KingdomSettlementVM(forceDecision, onGrantFief);
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		LeaderText = GameTexts.FindText("str_sort_by_leader_name_label").ToString();
		ClansText = GameTexts.FindText("str_encyclopedia_clans").ToString();
		FiefsText = GameTexts.FindText("str_fiefs").ToString();
		PoliciesText = GameTexts.FindText("str_policies").ToString();
		ArmiesText = GameTexts.FindText("str_armies").ToString();
		DiplomacyText = GameTexts.FindText("str_diplomatic_group").ToString();
		DoneText = GameTexts.FindText("str_done").ToString();
		RefreshDynamicKingdomProperties();
		Army.RefreshValues();
		Policy.RefreshValues();
		Clan.RefreshValues();
		Settlement.RefreshValues();
		Diplomacy.RefreshValues();
	}

	private void RefreshDynamicKingdomProperties()
	{
		Name = ((Hero.MainHero.MapFaction == null) ? new TextObject("{=kQsXUvgO}You are not under a kingdom.").ToString() : Hero.MainHero.MapFaction.Name.ToString());
		PlayerHasKingdom = Hero.MainHero.MapFaction is Kingdom;
		if (PlayerHasKingdom)
		{
			Kingdom = Hero.MainHero.MapFaction as Kingdom;
			Leader = new HeroVM(Kingdom.Leader);
			KingdomBanner = new BannerImageIdentifierVM(Kingdom.Banner, nineGrid: true);
			_isPlayerTheRuler = Kingdom.Leader == Hero.MainHero;
			KingdomActionText = (_isPlayerTheRuler ? GameTexts.FindText("str_abdicate_leadership").ToString() : GameTexts.FindText("str_leave_kingdom").ToString());
		}
		else
		{
			Kingdom = null;
			Leader = null;
			KingdomBanner = null;
			_isPlayerTheRuler = false;
			KingdomActionText = string.Empty;
		}
		PlayerCanChangeKingdomName = GetCanChangeKingdomNameWithReason(out var disabledReason);
		ChangeKingdomNameHint.HintText = disabledReason;
		IsKingdomActionEnabled = GetIsKingdomActionEnabledWithReason(_isPlayerTheRuler, out var kingdomActionDisabledReasons);
		KingdomActionHint = new BasicTooltipViewModel(() => CampaignUIHelper.MergeTextObjectsWithNewline(kingdomActionDisabledReasons));
	}

	private bool GetCanChangeKingdomNameWithReason(out TextObject disabledReason)
	{
		if (!PlayerHasKingdom)
		{
			disabledReason = new TextObject("{=kQsXUvgO}You are not under a kingdom.");
			return false;
		}
		if (!_isPlayerTheRuler)
		{
			disabledReason = new TextObject("{=HFZdseH9}Only the ruler of the kingdom can change its name.");
			return false;
		}
		if (!CampaignUIHelper.GetMapScreenActionIsEnabledWithReason(out var disabledReason2))
		{
			disabledReason = disabledReason2;
			return false;
		}
		disabledReason = TextObject.GetEmpty();
		return true;
	}

	private bool GetIsKingdomActionEnabledWithReason(bool isPlayerTheRuler, out List<TextObject> disabledReasons)
	{
		disabledReasons = new List<TextObject>();
		if (!PlayerHasKingdom)
		{
			disabledReasons.Add(new TextObject("{=kQsXUvgO}You are not under a kingdom."));
			return false;
		}
		if (isPlayerTheRuler && !Campaign.Current.Models.KingdomCreationModel.IsPlayerKingdomAbdicationPossible(out var explanations))
		{
			disabledReasons.AddRange(explanations);
			return false;
		}
		if (!isPlayerTheRuler && MobileParty.MainParty.Army != null)
		{
			disabledReasons.Add(new TextObject("{=4Y8u4JKO}You can't leave the kingdom while in an army"));
			return false;
		}
		Game.Current.EventManager.TriggerEvent(_leaveKingdomPermissionEvent);
		ref(bool, TextObject)? mostRecentLeaveKingdomPermission = ref _mostRecentLeaveKingdomPermission;
		if (mostRecentLeaveKingdomPermission.HasValue && !mostRecentLeaveKingdomPermission.GetValueOrDefault().Item1)
		{
			disabledReasons.Add(_mostRecentLeaveKingdomPermission?.Item2);
			return false;
		}
		if (!CampaignUIHelper.GetMapScreenActionIsEnabledWithReason(out var disabledReason))
		{
			disabledReasons.Add(disabledReason);
			return false;
		}
		return true;
	}

	public void OnRefresh()
	{
		RefreshDynamicKingdomProperties();
		Army.RefreshArmyList();
		Policy.RefreshPolicyList();
		Clan.RefreshClan();
		Settlement.RefreshSettlementList();
		Diplomacy.RefreshDiplomacyList();
	}

	public void OnFrameTick()
	{
		Decision?.OnFrameTick();
	}

	private void OnRefreshDecision()
	{
		Decision.HandleNextDecision();
	}

	private void ForceDecideDecision(KingdomDecision decision)
	{
		Decision.RefreshWith(decision);
	}

	private void OnGrantFief(Settlement settlement)
	{
		if (Kingdom.Leader == Hero.MainHero)
		{
			GiftFief.OpenWith(settlement);
			return;
		}
		string titleText = new TextObject("{=eIGFuGOx}Give Settlement").ToString();
		string text = new TextObject("{=rkubGa4K}Are you sure want to give this settlement back to your kingdom?").ToString();
		InformationManager.ShowInquiry(new InquiryData(titleText, text, isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_yes").ToString(), GameTexts.FindText("str_no").ToString(), delegate
		{
			Campaign.Current.KingdomManager.RelinquishSettlementOwnership(settlement);
			ForceDecideDecision(Kingdom.UnresolvedDecisions[Kingdom.UnresolvedDecisions.Count - 1]);
		}, null));
	}

	private void OnSettlementGranted()
	{
		Settlement.RefreshSettlementList();
	}

	public void ExecuteClose()
	{
		_onClose();
	}

	private void ExecuteShowClan()
	{
		SetSelectedCategory(0);
	}

	private void ExecuteShowFiefs()
	{
		SetSelectedCategory(1);
	}

	private void ExecuteShowPolicies()
	{
		if (PlayerHasKingdom)
		{
			SetSelectedCategory(2);
		}
	}

	private void ExecuteShowDiplomacy()
	{
		if (PlayerHasKingdom)
		{
			SetSelectedCategory(4);
		}
	}

	private void ExecuteShowArmy()
	{
		SetSelectedCategory(3);
	}

	private void ExecuteKingdomAction()
	{
		if (!IsKingdomActionEnabled)
		{
			return;
		}
		if (_isPlayerTheRuler)
		{
			GameTexts.SetVariable("WILL_DESTROY", (Kingdom.Clans.Count == 1) ? 1 : 0);
			InformationManager.ShowInquiry(new InquiryData(GameTexts.FindText("str_abdicate_leadership").ToString(), GameTexts.FindText("str_abdicate_leadership_question").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_yes").ToString(), GameTexts.FindText("str_no").ToString(), OnConfirmAbdicateLeadership, null));
			return;
		}
		ref(bool, TextObject)? mostRecentLeaveKingdomPermission = ref _mostRecentLeaveKingdomPermission;
		if (mostRecentLeaveKingdomPermission.HasValue && mostRecentLeaveKingdomPermission.GetValueOrDefault().Item1 && _mostRecentLeaveKingdomPermission?.Item2 != null)
		{
			InformationManager.ShowInquiry(new InquiryData(new TextObject("{=3sxtCWPe}Leaving Kingdom").ToString(), _mostRecentLeaveKingdomPermission?.Item2.ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_yes").ToString(), GameTexts.FindText("str_no").ToString(), OnConfirmLeaveKingdom, null));
		}
		else if (TaleWorlds.CampaignSystem.Clan.PlayerClan.Settlements.Count == 0)
		{
			if (TaleWorlds.CampaignSystem.Clan.PlayerClan.IsUnderMercenaryService)
			{
				TextObject textObject = new TextObject("{=b7muQ9mt}Are you sure you want to end your mercenary contract with the {KINGDOM_INFORMALNAME}?");
				textObject.SetTextVariable("KINGDOM_INFORMALNAME", Kingdom.InformalName);
				InformationManager.ShowInquiry(new InquiryData(new TextObject("{=3sxtCWPe}Leaving Kingdom").ToString(), textObject.ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, new TextObject("{=5Unqsx3N}Confirm").ToString(), GameTexts.FindText("str_cancel").ToString(), OnConfirmLeaveKingdom, null));
			}
			else
			{
				InformationManager.ShowInquiry(new InquiryData(new TextObject("{=3sxtCWPe}Leaving Kingdom").ToString(), new TextObject("{=BgqZWbga}The nobles of the realm will dislike you for abandoning your fealty. Are you sure you want to leave the Kingdom?").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, new TextObject("{=5Unqsx3N}Confirm").ToString(), GameTexts.FindText("str_cancel").ToString(), OnConfirmLeaveKingdom, null));
			}
		}
		else
		{
			List<InquiryElement> inquiryElements = new List<InquiryElement>
			{
				new InquiryElement("keep", new TextObject("{=z8h0BRAb}Keep all holdings").ToString(), null, isEnabled: true, new TextObject("{=lkJfq1ap}Owned settlements remain under your control but nobles will dislike this dishonorable act and the kingdom will declare war on you.").ToString()),
				new InquiryElement("dontkeep", new TextObject("{=JIr3Jc7b}Relinquish all holdings").ToString(), null, isEnabled: true, new TextObject("{=ZjaSde0X}Owned settlements are returned to the kingdom. This will avert a war and nobles will dislike you less for abandoning your fealty.").ToString())
			};
			MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(new TextObject("{=3sxtCWPe}Leaving Kingdom").ToString(), new TextObject("{=xtlIFKaa}Are you sure you want to leave the Kingdom?{newline}If so, choose how you want to leave the kingdom.").ToString(), inquiryElements, isExitShown: true, 1, 1, new TextObject("{=5Unqsx3N}Confirm").ToString(), string.Empty, OnConfirmLeaveKingdomWithOption, null));
		}
	}

	private void OnLeaveKingdomRequest(bool isPossible, TextObject disabledReasonOrWarning)
	{
		_mostRecentLeaveKingdomPermission = (isPossible, disabledReasonOrWarning);
	}

	private void OnConfirmAbdicateLeadership()
	{
		Campaign.Current.KingdomManager.AbdicateTheThrone(Kingdom);
		KingdomDecision kingdomDecision = Kingdom.UnresolvedDecisions.LastOrDefault();
		if (kingdomDecision != null)
		{
			ForceDecideDecision(kingdomDecision);
		}
		else
		{
			ExecuteClose();
		}
	}

	private void OnConfirmLeaveKingdomWithOption(List<InquiryElement> obj)
	{
		InquiryElement inquiryElement = obj.FirstOrDefault();
		if (inquiryElement != null)
		{
			string text = inquiryElement.Identifier as string;
			if (text == "keep")
			{
				ChangeKingdomAction.ApplyByLeaveWithRebellionAgainstKingdom(TaleWorlds.CampaignSystem.Clan.PlayerClan);
			}
			else if (text == "dontkeep")
			{
				ChangeKingdomAction.ApplyByLeaveKingdom(TaleWorlds.CampaignSystem.Clan.PlayerClan);
			}
			ExecuteClose();
		}
	}

	private void OnConfirmLeaveKingdom()
	{
		if (TaleWorlds.CampaignSystem.Clan.PlayerClan.IsUnderMercenaryService)
		{
			ChangeKingdomAction.ApplyByLeaveKingdomAsMercenary(TaleWorlds.CampaignSystem.Clan.PlayerClan);
		}
		else
		{
			ChangeKingdomAction.ApplyByLeaveKingdom(TaleWorlds.CampaignSystem.Clan.PlayerClan);
		}
		ExecuteClose();
	}

	private void ExecuteChangeKingdomName()
	{
		InformationManager.ShowTextInquiry(new TextInquiryData(GameTexts.FindText("str_change_kingdom_name").ToString(), string.Empty, isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_done").ToString(), GameTexts.FindText("str_cancel").ToString(), OnChangeKingdomNameDone, null, shouldInputBeObfuscated: false, FactionHelper.IsKingdomNameApplicable));
	}

	private void OnChangeKingdomNameDone(string newKingdomName)
	{
		TextObject variable = new TextObject(newKingdomName);
		TextObject textObject = GameTexts.FindText("str_generic_kingdom_name");
		TextObject textObject2 = GameTexts.FindText("str_generic_kingdom_short_name");
		textObject.SetTextVariable("KINGDOM_NAME", variable);
		textObject2.SetTextVariable("KINGDOM_SHORT_NAME", variable);
		Kingdom.ChangeKingdomName(textObject, textObject2);
		OnRefresh();
		RefreshValues();
	}

	public void SelectArmy(Army army)
	{
		SetSelectedCategory(3);
		Army.SelectArmy(army);
	}

	public void SelectSettlement(Settlement settlement)
	{
		SetSelectedCategory(1);
		Settlement.SelectSettlement(settlement);
	}

	public void SelectClan(Clan clan)
	{
		SetSelectedCategory(0);
		Clan.SelectClan(clan);
	}

	public void SelectPolicy(PolicyObject policy)
	{
		SetSelectedCategory(2);
		Policy.SelectPolicy(policy);
	}

	public void SelectKingdom(Kingdom kingdom)
	{
		SetSelectedCategory(4);
		Diplomacy.SelectKingdom(kingdom);
	}

	public void SelectPreviousCategory()
	{
		int selectedCategory = ((_currentCategory == 0) ? (_categoryCount - 1) : (_currentCategory - 1));
		SetSelectedCategory(selectedCategory);
	}

	public void SelectNextCategory()
	{
		int selectedCategory = (_currentCategory + 1) % _categoryCount;
		SetSelectedCategory(selectedCategory);
	}

	private void SetSelectedCategory(int index)
	{
		Clan.Show = false;
		Settlement.Show = false;
		Policy.Show = false;
		Army.Show = false;
		Diplomacy.Show = false;
		_currentCategory = index;
		switch (index)
		{
		case 0:
			Clan.Show = true;
			break;
		case 1:
			Settlement.Show = true;
			break;
		case 2:
			Policy.Show = true;
			break;
		case 3:
			Army.Show = true;
			break;
		default:
			_currentCategory = 4;
			Diplomacy.Show = true;
			break;
		}
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		DoneInputKey.OnFinalize();
		PreviousTabInputKey.OnFinalize();
		NextTabInputKey.OnFinalize();
		Decision.OnFinalize();
		Clan.OnFinalize();
	}

	public void SetDoneInputKey(HotKey hotkey)
	{
		DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, isConsoleOnly: true);
		Decision.SetDoneInputKey(hotkey);
		GiftFief.SetDoneInputKey(hotkey);
	}

	public void SetCancelInputKey(HotKey hotkey)
	{
		GiftFief.SetCancelInputKey(hotkey);
	}

	public void SetPreviousTabInputKey(HotKey hotkey)
	{
		PreviousTabInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, isConsoleOnly: true);
	}

	public void SetNextTabInputKey(HotKey hotkey)
	{
		NextTabInputKey = InputKeyItemVM.CreateFromHotKey(hotkey, isConsoleOnly: true);
	}
}
