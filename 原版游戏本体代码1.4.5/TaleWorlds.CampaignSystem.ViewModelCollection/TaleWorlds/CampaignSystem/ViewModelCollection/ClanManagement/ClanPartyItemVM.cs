using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement;

public class ClanPartyItemVM : ViewModel
{
	public enum ClanPartyType
	{
		Main,
		Member,
		Caravan,
		Garrison
	}

	private readonly Action<ClanPartyItemVM> _onAssignment;

	private readonly Action _onExpenseChange;

	private readonly Action _onShowChangeLeaderPopup;

	private readonly ClanPartyType _type;

	private readonly TextObject _changeLeaderHintText = GameTexts.FindText("str_change_party_leader");

	private readonly IDisbandPartyCampaignBehavior _disbandBehavior;

	private readonly bool _isLeaderTeleporting;

	private readonly CharacterObject _leader;

	private ClanPartyBehaviorSelectorVM _partyBehaviorSelector;

	private ClanFinanceExpenseItemVM _expenseItem;

	private ClanRoleItemVM _lastOpenedRoleSelection;

	private ClanPartyMemberItemVM _leaderMember;

	private CharacterImageIdentifierVM _leaderVisual;

	private bool _isMainHeroParty;

	private bool _isSelected;

	private bool _hasHeroMembers;

	private string _partyLocationText;

	private string _partySizeText;

	private string _shipCountText;

	private string _membersText;

	private string _assigneesText;

	private string _rolesText;

	private string _partyLeaderRoleEffectsText;

	private string _name;

	private string _partySizeSubTitleText;

	private string _partyWageSubTitleText;

	private string _partyBehaviorText;

	private int _infantryCount;

	private int _rangedCount;

	private int _cavalryCount;

	private int _horseArcherCount;

	private int _shipCount;

	private string _inArmyText;

	private string _disbandingText;

	private string _autoRecruitmentText;

	private bool _autoRecruitmentValue;

	private bool _isAutoRecruitmentVisible;

	private bool _shouldPartyHaveExpense;

	private bool _hasCompanion;

	private bool _isPartyBehaviorEnabled;

	private bool _isMembersAndRolesVisible;

	private bool _isCaravan;

	private bool _isDisbanding;

	private bool _isInArmy;

	private bool _canUseActions;

	private bool _isChangeLeaderVisible;

	private bool _isChangeLeaderEnabled;

	private bool _isClanRoleSelectionHighlightEnabled;

	private bool _isRoleSelectionPopupVisible;

	private HintViewModel _actionsDisabledHint;

	private CharacterViewModel _characterModel;

	private HintViewModel _autoRecruitmentHint;

	private HintViewModel _inArmyHint;

	private HintViewModel _changeLeaderHint;

	private BasicTooltipViewModel _infantryHint;

	private BasicTooltipViewModel _rangedHint;

	private BasicTooltipViewModel _cavalryHint;

	private BasicTooltipViewModel _horseArcherHint;

	private MBBindingList<ClanPartyMemberItemVM> _heroMembers;

	private MBBindingList<ClanRoleItemVM> _roles;

	public int Expense { get; private set; }

	public int Income { get; private set; }

	public PartyBase Party { get; }

	[DataSourceProperty]
	public CharacterViewModel CharacterModel
	{
		get
		{
			return _characterModel;
		}
		set
		{
			if (value != _characterModel)
			{
				_characterModel = value;
				OnPropertyChangedWithValue(value, "CharacterModel");
			}
		}
	}

	[DataSourceProperty]
	public ClanPartyBehaviorSelectorVM PartyBehaviorSelector
	{
		get
		{
			return _partyBehaviorSelector;
		}
		set
		{
			if (value != _partyBehaviorSelector)
			{
				_partyBehaviorSelector = value;
				OnPropertyChangedWithValue(value, "PartyBehaviorSelector");
			}
		}
	}

	[DataSourceProperty]
	public CharacterImageIdentifierVM LeaderVisual
	{
		get
		{
			return _leaderVisual;
		}
		set
		{
			if (value != _leaderVisual)
			{
				_leaderVisual = value;
				OnPropertyChangedWithValue(value, "LeaderVisual");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSelected
	{
		get
		{
			return _isSelected;
		}
		set
		{
			if (value != _isSelected)
			{
				_isSelected = value;
				OnPropertyChangedWithValue(value, "IsSelected");
			}
		}
	}

	[DataSourceProperty]
	public bool HasHeroMembers
	{
		get
		{
			return _hasHeroMembers;
		}
		set
		{
			if (value != _hasHeroMembers)
			{
				_hasHeroMembers = value;
				OnPropertyChangedWithValue(value, "HasHeroMembers");
			}
		}
	}

	[DataSourceProperty]
	public bool IsClanRoleSelectionHighlightEnabled
	{
		get
		{
			return _isClanRoleSelectionHighlightEnabled;
		}
		set
		{
			if (value != _isClanRoleSelectionHighlightEnabled)
			{
				_isClanRoleSelectionHighlightEnabled = value;
				OnPropertyChangedWithValue(value, "IsClanRoleSelectionHighlightEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsRoleSelectionPopupVisible
	{
		get
		{
			return _isRoleSelectionPopupVisible;
		}
		set
		{
			if (value != _isRoleSelectionPopupVisible)
			{
				_isRoleSelectionPopupVisible = value;
				OnPropertyChangedWithValue(value, "IsRoleSelectionPopupVisible");
			}
		}
	}

	[DataSourceProperty]
	public bool IsDisbanding
	{
		get
		{
			return _isDisbanding;
		}
		set
		{
			if (value != _isDisbanding)
			{
				_isDisbanding = value;
				OnPropertyChangedWithValue(value, "IsDisbanding");
			}
		}
	}

	[DataSourceProperty]
	public bool IsInArmy
	{
		get
		{
			return _isInArmy;
		}
		set
		{
			if (value != _isInArmy)
			{
				_isInArmy = value;
				OnPropertyChangedWithValue(value, "IsInArmy");
			}
		}
	}

	[DataSourceProperty]
	public bool CanUseActions
	{
		get
		{
			return _canUseActions;
		}
		set
		{
			if (value != _canUseActions)
			{
				_canUseActions = value;
				OnPropertyChangedWithValue(value, "CanUseActions");
			}
		}
	}

	[DataSourceProperty]
	public bool IsChangeLeaderVisible
	{
		get
		{
			return _isChangeLeaderVisible;
		}
		set
		{
			if (value != _isChangeLeaderVisible)
			{
				_isChangeLeaderVisible = value;
				OnPropertyChangedWithValue(value, "IsChangeLeaderVisible");
			}
		}
	}

	[DataSourceProperty]
	public bool IsChangeLeaderEnabled
	{
		get
		{
			return _isChangeLeaderEnabled;
		}
		set
		{
			if (value != _isChangeLeaderEnabled)
			{
				_isChangeLeaderEnabled = value;
				OnPropertyChangedWithValue(value, "IsChangeLeaderEnabled");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel ActionsDisabledHint
	{
		get
		{
			return _actionsDisabledHint;
		}
		set
		{
			if (value != _actionsDisabledHint)
			{
				_actionsDisabledHint = value;
				OnPropertyChangedWithValue(value, "ActionsDisabledHint");
			}
		}
	}

	[DataSourceProperty]
	public bool IsCaravan
	{
		get
		{
			return _isCaravan;
		}
		set
		{
			if (value != _isCaravan)
			{
				_isCaravan = value;
				OnPropertyChangedWithValue(value, "IsCaravan");
			}
		}
	}

	[DataSourceProperty]
	public bool ShouldPartyHaveExpense
	{
		get
		{
			return _shouldPartyHaveExpense;
		}
		set
		{
			if (value != _shouldPartyHaveExpense)
			{
				_shouldPartyHaveExpense = value;
				OnPropertyChangedWithValue(value, "ShouldPartyHaveExpense");
			}
		}
	}

	[DataSourceProperty]
	public bool HasCompanion
	{
		get
		{
			return _hasCompanion;
		}
		set
		{
			if (value != _hasCompanion)
			{
				_hasCompanion = value;
				OnPropertyChangedWithValue(value, "HasCompanion");
			}
		}
	}

	[DataSourceProperty]
	public bool IsAutoRecruitmentVisible
	{
		get
		{
			return _isAutoRecruitmentVisible;
		}
		set
		{
			if (value != _isAutoRecruitmentVisible)
			{
				_isAutoRecruitmentVisible = value;
				OnPropertyChangedWithValue(value, "IsAutoRecruitmentVisible");
			}
		}
	}

	[DataSourceProperty]
	public bool AutoRecruitmentValue
	{
		get
		{
			return _autoRecruitmentValue;
		}
		set
		{
			if (value != _autoRecruitmentValue)
			{
				_autoRecruitmentValue = value;
				OnPropertyChangedWithValue(value, "AutoRecruitmentValue");
				OnAutoRecruitChanged(value);
			}
		}
	}

	[DataSourceProperty]
	public bool IsPartyBehaviorEnabled
	{
		get
		{
			return _isPartyBehaviorEnabled;
		}
		set
		{
			if (value != _isPartyBehaviorEnabled)
			{
				_isPartyBehaviorEnabled = value;
				OnPropertyChangedWithValue(value, "IsPartyBehaviorEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsMembersAndRolesVisible
	{
		get
		{
			return _isMembersAndRolesVisible;
		}
		set
		{
			if (value != _isMembersAndRolesVisible)
			{
				_isMembersAndRolesVisible = value;
				OnPropertyChangedWithValue(value, "IsMembersAndRolesVisible");
			}
		}
	}

	[DataSourceProperty]
	public bool IsMainHeroParty
	{
		get
		{
			return _isMainHeroParty;
		}
		set
		{
			if (value != _isMainHeroParty)
			{
				_isMainHeroParty = value;
				OnPropertyChangedWithValue(value, "IsMainHeroParty");
			}
		}
	}

	[DataSourceProperty]
	public ClanFinanceExpenseItemVM ExpenseItem
	{
		get
		{
			return _expenseItem;
		}
		set
		{
			if (value != _expenseItem)
			{
				_expenseItem = value;
				OnPropertyChangedWithValue(value, "ExpenseItem");
			}
		}
	}

	[DataSourceProperty]
	public ClanRoleItemVM LastOpenedRoleSelection
	{
		get
		{
			return _lastOpenedRoleSelection;
		}
		set
		{
			if (value != _lastOpenedRoleSelection)
			{
				_lastOpenedRoleSelection = value;
				OnPropertyChangedWithValue(value, "LastOpenedRoleSelection");
			}
		}
	}

	[DataSourceProperty]
	public ClanPartyMemberItemVM LeaderMember
	{
		get
		{
			return _leaderMember;
		}
		set
		{
			if (value != _leaderMember)
			{
				_leaderMember = value;
				OnPropertyChangedWithValue(value, "LeaderMember");
			}
		}
	}

	[DataSourceProperty]
	public string PartySizeText
	{
		get
		{
			return _partySizeText;
		}
		set
		{
			if (value != _partySizeText)
			{
				_partySizeText = value;
				OnPropertyChanged("PartyStrengthText");
			}
		}
	}

	[DataSourceProperty]
	public string ShipCountText
	{
		get
		{
			return _shipCountText;
		}
		set
		{
			if (value != _shipCountText)
			{
				_shipCountText = value;
				OnPropertyChangedWithValue(value, "ShipCountText");
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
			if (value != null)
			{
				_membersText = value;
				OnPropertyChangedWithValue(value, "MembersText");
			}
		}
	}

	[DataSourceProperty]
	public string AssigneesText
	{
		get
		{
			return _assigneesText;
		}
		set
		{
			if (value != _assigneesText)
			{
				_assigneesText = value;
				OnPropertyChangedWithValue(value, "AssigneesText");
			}
		}
	}

	[DataSourceProperty]
	public string RolesText
	{
		get
		{
			return _rolesText;
		}
		set
		{
			if (value != _rolesText)
			{
				_rolesText = value;
				OnPropertyChangedWithValue(value, "RolesText");
			}
		}
	}

	[DataSourceProperty]
	public string PartyLeaderRoleEffectsText
	{
		get
		{
			return _partyLeaderRoleEffectsText;
		}
		set
		{
			if (value != _partyLeaderRoleEffectsText)
			{
				_partyLeaderRoleEffectsText = value;
				OnPropertyChangedWithValue(value, "PartyLeaderRoleEffectsText");
			}
		}
	}

	[DataSourceProperty]
	public string PartyLocationText
	{
		get
		{
			return _partyLocationText;
		}
		set
		{
			if (value != _partyLocationText)
			{
				_partyLocationText = value;
				OnPropertyChangedWithValue(value, "PartyLocationText");
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
	public string PartySizeSubTitleText
	{
		get
		{
			return _partySizeSubTitleText;
		}
		set
		{
			if (value != _partySizeSubTitleText)
			{
				_partySizeSubTitleText = value;
				OnPropertyChangedWithValue(value, "PartySizeSubTitleText");
			}
		}
	}

	[DataSourceProperty]
	public string PartyWageSubTitleText
	{
		get
		{
			return _partyWageSubTitleText;
		}
		set
		{
			if (value != _partyWageSubTitleText)
			{
				_partyWageSubTitleText = value;
				OnPropertyChangedWithValue(value, "PartyWageSubTitleText");
			}
		}
	}

	[DataSourceProperty]
	public string PartyBehaviorText
	{
		get
		{
			return _partyBehaviorText;
		}
		set
		{
			if (value != _partyBehaviorText)
			{
				_partyBehaviorText = value;
				OnPropertyChangedWithValue(value, "PartyBehaviorText");
			}
		}
	}

	[DataSourceProperty]
	public int InfantryCount
	{
		get
		{
			return _infantryCount;
		}
		set
		{
			if (value != _infantryCount)
			{
				_infantryCount = value;
				OnPropertyChangedWithValue(value, "InfantryCount");
			}
		}
	}

	[DataSourceProperty]
	public int RangedCount
	{
		get
		{
			return _rangedCount;
		}
		set
		{
			if (value != _rangedCount)
			{
				_rangedCount = value;
				OnPropertyChangedWithValue(value, "RangedCount");
			}
		}
	}

	[DataSourceProperty]
	public int CavalryCount
	{
		get
		{
			return _cavalryCount;
		}
		set
		{
			if (value != _cavalryCount)
			{
				_cavalryCount = value;
				OnPropertyChangedWithValue(value, "CavalryCount");
			}
		}
	}

	[DataSourceProperty]
	public int HorseArcherCount
	{
		get
		{
			return _horseArcherCount;
		}
		set
		{
			if (value != _horseArcherCount)
			{
				_horseArcherCount = value;
				OnPropertyChangedWithValue(value, "HorseArcherCount");
			}
		}
	}

	[DataSourceProperty]
	public int ShipCount
	{
		get
		{
			return _shipCount;
		}
		set
		{
			if (value != _shipCount)
			{
				_shipCount = value;
				OnPropertyChangedWithValue(value, "ShipCount");
			}
		}
	}

	[DataSourceProperty]
	public string InArmyText
	{
		get
		{
			return _inArmyText;
		}
		set
		{
			if (value != _inArmyText)
			{
				_inArmyText = value;
				OnPropertyChangedWithValue(value, "InArmyText");
			}
		}
	}

	[DataSourceProperty]
	public string DisbandingText
	{
		get
		{
			return _disbandingText;
		}
		set
		{
			if (value != _disbandingText)
			{
				_disbandingText = value;
				OnPropertyChangedWithValue(value, "DisbandingText");
			}
		}
	}

	[DataSourceProperty]
	public string AutoRecruitmentText
	{
		get
		{
			return _autoRecruitmentText;
		}
		set
		{
			if (value != _autoRecruitmentText)
			{
				_autoRecruitmentText = value;
				OnPropertyChangedWithValue(value, "AutoRecruitmentText");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel AutoRecruitmentHint
	{
		get
		{
			return _autoRecruitmentHint;
		}
		set
		{
			if (value != _autoRecruitmentHint)
			{
				_autoRecruitmentHint = value;
				OnPropertyChangedWithValue(value, "AutoRecruitmentHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel InArmyHint
	{
		get
		{
			return _inArmyHint;
		}
		set
		{
			if (value != _inArmyHint)
			{
				_inArmyHint = value;
				OnPropertyChangedWithValue(value, "InArmyHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel ChangeLeaderHint
	{
		get
		{
			return _changeLeaderHint;
		}
		set
		{
			if (value != _changeLeaderHint)
			{
				_changeLeaderHint = value;
				OnPropertyChangedWithValue(value, "ChangeLeaderHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel InfantryHint
	{
		get
		{
			return _infantryHint;
		}
		set
		{
			if (value != _infantryHint)
			{
				_infantryHint = value;
				OnPropertyChangedWithValue(value, "InfantryHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel RangedHint
	{
		get
		{
			return _rangedHint;
		}
		set
		{
			if (value != _rangedHint)
			{
				_rangedHint = value;
				OnPropertyChangedWithValue(value, "RangedHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel CavalryHint
	{
		get
		{
			return _cavalryHint;
		}
		set
		{
			if (value != _cavalryHint)
			{
				_cavalryHint = value;
				OnPropertyChangedWithValue(value, "CavalryHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel HorseArcherHint
	{
		get
		{
			return _horseArcherHint;
		}
		set
		{
			if (value != _horseArcherHint)
			{
				_horseArcherHint = value;
				OnPropertyChangedWithValue(value, "HorseArcherHint");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<ClanPartyMemberItemVM> HeroMembers
	{
		get
		{
			return _heroMembers;
		}
		set
		{
			if (value != _heroMembers)
			{
				_heroMembers = value;
				OnPropertyChangedWithValue(value, "HeroMembers");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<ClanRoleItemVM> Roles
	{
		get
		{
			return _roles;
		}
		set
		{
			if (value != _roles)
			{
				_roles = value;
				OnPropertyChangedWithValue(value, "Roles");
			}
		}
	}

	public ClanPartyItemVM(PartyBase party, Action<ClanPartyItemVM> onAssignment, Action onExpenseChange, Action onShowChangeLeaderPopup, ClanPartyType type, IDisbandPartyCampaignBehavior disbandBehavior, ITeleportationCampaignBehavior teleportationBehavior)
	{
		Party = party;
		_type = type;
		_disbandBehavior = disbandBehavior;
		_leader = CampaignUIHelper.GetVisualPartyLeader(Party);
		HasHeroMembers = party.IsMobile;
		if (_leader == null)
		{
			TroopRosterElement troopRosterElement = Party.MemberRoster.GetTroopRoster().FirstOrDefault();
			if (!troopRosterElement.Equals(default(TroopRosterElement)))
			{
				_leader = troopRosterElement.Character;
			}
			else
			{
				_leader = Party.MapFaction?.BasicTroop;
			}
		}
		CharacterObject leader = _leader;
		if ((leader == null || !leader.IsHero) && party.IsMobile && (_type == ClanPartyType.Member || _type == ClanPartyType.Caravan))
		{
			_leader = CampaignUIHelper.GetTeleportingLeaderHero(party.MobileParty, teleportationBehavior)?.CharacterObject;
			_isLeaderTeleporting = _leader != null;
		}
		if (_leader != null)
		{
			CharacterCode characterCode = GetCharacterCode(_leader);
			LeaderVisual = new CharacterImageIdentifierVM(characterCode);
			CharacterModel = new CharacterViewModel(CharacterViewModel.StanceTypes.None);
			CharacterModel.FillFrom(_leader, -1, Party.Banner?.BannerCode);
			CharacterModel.ArmorColor1 = Party.MapFaction?.Color ?? 0;
			CharacterModel.ArmorColor2 = Party.MapFaction?.Color2 ?? 0;
		}
		else
		{
			LeaderVisual = new CharacterImageIdentifierVM(null);
			CharacterModel = new CharacterViewModel();
		}
		_onAssignment = onAssignment;
		_onExpenseChange = onExpenseChange;
		_onShowChangeLeaderPopup = onShowChangeLeaderPopup;
		IsDisbanding = Party.MobileParty.IsDisbanding || (_disbandBehavior?.IsPartyWaitingForDisband(party.MobileParty) ?? false);
		ShouldPartyHaveExpense = !party.MobileParty.IsMilitia && !party.MobileParty.IsVillager && party.MobileParty.IsActive && !IsDisbanding && (type == ClanPartyType.Garrison || type == ClanPartyType.Member);
		IsCaravan = type == ClanPartyType.Caravan;
		TextObject disabledReason = TextObject.GetEmpty();
		IsChangeLeaderVisible = type == ClanPartyType.Caravan || type == ClanPartyType.Member;
		IsChangeLeaderEnabled = IsChangeLeaderVisible && CampaignUIHelper.GetMapScreenActionIsEnabledWithReason(out disabledReason);
		ChangeLeaderHint = new HintViewModel(IsChangeLeaderEnabled ? _changeLeaderHintText : disabledReason);
		if (ShouldPartyHaveExpense)
		{
			if (party.MobileParty != null)
			{
				ExpenseItem = new ClanFinanceExpenseItemVM(party.MobileParty);
				OnExpenseChange();
			}
			else
			{
				Debug.FailedAssert("This party should have expense info but it doesn't", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\ClanManagement\\ClanPartyItemVM.cs", ".ctor", 116);
			}
		}
		if (IsCaravan)
		{
			Income = Campaign.Current.Models.ClanFinanceModel.CalculateOwnerIncomeFromCaravan(party.MobileParty);
		}
		AutoRecruitmentHint = new HintViewModel(GameTexts.FindText("str_clan_auto_recruitment_hint"));
		IsAutoRecruitmentVisible = party.MobileParty.IsGarrison;
		AutoRecruitmentValue = party.MobileParty.IsGarrison && Party.MobileParty.CurrentSettlement.Town.GarrisonAutoRecruitmentIsEnabled;
		HeroMembers = new MBBindingList<ClanPartyMemberItemVM>();
		Roles = new MBBindingList<ClanRoleItemVM>();
		InfantryHint = new BasicTooltipViewModel(() => GetPartyTroopInfo(Party, FormationClass.Infantry));
		CavalryHint = new BasicTooltipViewModel(() => GetPartyTroopInfo(Party, FormationClass.Cavalry));
		RangedHint = new BasicTooltipViewModel(() => GetPartyTroopInfo(Party, FormationClass.Ranged));
		HorseArcherHint = new BasicTooltipViewModel(() => GetPartyTroopInfo(Party, FormationClass.HorseArcher));
		ActionsDisabledHint = new HintViewModel();
		InArmyHint = new HintViewModel();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		UpdateProperties();
	}

	public void UpdateProperties()
	{
		MembersText = GameTexts.FindText("str_members").ToString();
		AssigneesText = GameTexts.FindText("str_clan_assignee_title").ToString();
		RolesText = GameTexts.FindText("str_clan_role_title").ToString();
		PartyLeaderRoleEffectsText = GameTexts.FindText("str_clan_party_leader_roles_and_effects").ToString();
		AutoRecruitmentText = GameTexts.FindText("str_clan_auto_recruitment").ToString();
		IsPartyBehaviorEnabled = Party?.LeaderHero != null && Party.LeaderHero.Clan.Leader != Party.LeaderHero && !Party.MobileParty.IsCaravan && !IsDisbanding;
		if (Party == PartyBase.MainParty && Hero.MainHero.IsPrisoner)
		{
			TextObject textObject = new TextObject("{=shL0WElC}{TROOP.NAME}{.o} Party");
			textObject.SetCharacterProperties("TROOP", Hero.MainHero.CharacterObject);
			Name = textObject.ToString();
		}
		else if (_isLeaderTeleporting)
		{
			TextObject textObject2 = new TextObject("{=P5YtNXHR}{LEADER.NAME}{.o} Party");
			StringHelpers.SetCharacterProperties("LEADER", _leader, textObject2);
			Name = textObject2.ToString();
		}
		else
		{
			Name = Party.Name.ToString();
		}
		IsMainHeroParty = _type == ClanPartyType.Main;
		PartyLocationText = CampaignUIHelper.GetPartyLocationText(Party.MobileParty);
		GameTexts.SetVariable("LEFT", Party.MobileParty.MemberRoster.TotalManCount);
		GameTexts.SetVariable("RIGHT", Party.MobileParty.Party.PartySizeLimit);
		string text = GameTexts.FindText("str_LEFT_over_RIGHT").ToString();
		string content = GameTexts.FindText("str_party_morale_party_size").ToString();
		PartySizeText = text;
		GameTexts.SetVariable("LEFT", content);
		GameTexts.SetVariable("RIGHT", text);
		PartySizeSubTitleText = GameTexts.FindText("str_LEFT_colon_RIGHT").ToString();
		GameTexts.SetVariable("LEFT", GameTexts.FindText("str_party_wage"));
		GameTexts.SetVariable("RIGHT", Party.MobileParty.TotalWage);
		PartyWageSubTitleText = GameTexts.FindText("str_LEFT_colon_RIGHT").ToString();
		InArmyText = "";
		if (Party.MobileParty.Army != null)
		{
			IsInArmy = true;
			TextObject textObject3 = GameTexts.FindText("str_clan_in_army_hint");
			textObject3.SetTextVariable("ARMY_LEADER", Party.MobileParty.Army.LeaderParty?.LeaderHero?.Name.ToString() ?? string.Empty);
			InArmyHint = new HintViewModel(textObject3);
			InArmyText = GameTexts.FindText("str_in_army").ToString();
		}
		DisbandingText = "";
		IsMembersAndRolesVisible = !IsDisbanding && _type != ClanPartyType.Garrison;
		if (IsDisbanding)
		{
			DisbandingText = GameTexts.FindText("str_disbanding").ToString();
		}
		PartyBehaviorText = "";
		if (IsPartyBehaviorEnabled)
		{
			PartyBehaviorSelector = new ClanPartyBehaviorSelectorVM(0, UpdatePartyBehaviorSelectionUpdate);
			for (int i = 0; i < 3; i++)
			{
				string s = GameTexts.FindText("str_clan_party_objective", i.ToString()).ToString();
				TextObject hint = GameTexts.FindText("str_clan_party_objective_hint", i.ToString());
				PartyBehaviorSelector.AddItem(new SelectorItemVM(s, hint));
			}
			PartyBehaviorSelector.SelectedIndex = (int)Party.MobileParty.Objective;
			PartyBehaviorText = GameTexts.FindText("str_clan_party_behavior").ToString();
		}
		if (_leader != null)
		{
			CharacterModel.FillFrom(_leader, -1, Party.Banner?.BannerCode);
			CharacterModel.ArmorColor1 = Party.MapFaction?.Color ?? 0;
			CharacterModel.ArmorColor2 = Party.MapFaction?.Color2 ?? 0;
		}
		HeroMembers.Clear();
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		foreach (TroopRosterElement item2 in Party.MemberRoster.GetTroopRoster())
		{
			Hero heroObject = item2.Character.HeroObject;
			if (heroObject != null && heroObject.Clan == Clan.PlayerClan && heroObject.GovernorOf == null)
			{
				ClanPartyMemberItemVM clanPartyMemberItemVM = new ClanPartyMemberItemVM(item2.Character.HeroObject, Party.MobileParty);
				HeroMembers.Add(clanPartyMemberItemVM);
				if (clanPartyMemberItemVM.IsLeader)
				{
					LeaderMember = clanPartyMemberItemVM;
				}
			}
			else if (item2.Character.DefaultFormationClass.Equals(FormationClass.Infantry))
			{
				num += item2.Number;
			}
			else if (item2.Character.DefaultFormationClass.Equals(FormationClass.Ranged))
			{
				num2 += item2.Number;
			}
			else if (item2.Character.DefaultFormationClass.Equals(FormationClass.Cavalry))
			{
				num3 += item2.Number;
			}
			else if (item2.Character.DefaultFormationClass.Equals(FormationClass.HorseArcher))
			{
				num4 += item2.Number;
			}
		}
		if (_isLeaderTeleporting)
		{
			ClanPartyMemberItemVM item = (LeaderMember = new ClanPartyMemberItemVM(_leader.HeroObject, Party.MobileParty));
			HeroMembers.Insert(0, item);
		}
		HasCompanion = HeroMembers.Count > 1;
		if (IsMembersAndRolesVisible)
		{
			Roles.ApplyActionOnAllItems(delegate(ClanRoleItemVM x)
			{
				x.OnFinalize();
			});
			Roles.Clear();
			foreach (PartyRole assignablePartyRole in Campaign.Current.Models.ClanMemberPartyRoleModel.GetAssignablePartyRoles())
			{
				Roles.Add(new ClanRoleItemVM(Party.MobileParty, assignablePartyRole, HeroMembers, OnRoleSelectionToggled, OnRoleAssigned));
			}
		}
		InfantryCount = num;
		RangedCount = num2;
		CavalryCount = num3;
		HorseArcherCount = num4;
		CanUseActions = CampaignUIHelper.GetMapScreenActionIsEnabledWithReason(out var disabledReason);
		ActionsDisabledHint.HintText = (CanUseActions ? TextObject.GetEmpty() : disabledReason);
		if (!CanUseActions)
		{
			AutoRecruitmentHint.HintText = ActionsDisabledHint.HintText;
			if (ExpenseItem != null)
			{
				ExpenseItem.IsEnabled = CanUseActions;
				ExpenseItem.WageLimitHint.HintText = ActionsDisabledHint.HintText;
			}
			foreach (ClanRoleItemVM role in Roles)
			{
				role.SetEnabled(enabled: false, ActionsDisabledHint.HintText);
			}
		}
		if (PartyBehaviorSelector != null)
		{
			PartyBehaviorSelector.CanUseActions = CanUseActions;
			PartyBehaviorSelector.ActionsDisabledHint.HintText = ActionsDisabledHint.HintText;
		}
		ShipCount = Party.Ships.Count;
		ShipCountText = GameTexts.FindText("str_LEFT_colon_RIGHT").SetTextVariable("LEFT", new TextObject("{=URbKirPS}Ship Count").ToString()).SetTextVariable("RIGHT", ShipCount)
			.ToString();
	}

	private void OnExpenseChange()
	{
		_onExpenseChange();
	}

	public void OnPartySelection()
	{
		int selectedIndex = (IsPartyBehaviorEnabled ? PartyBehaviorSelector.SelectedIndex : (-1));
		_onAssignment(this);
		if (IsPartyBehaviorEnabled)
		{
			PartyBehaviorSelector.SelectedIndex = selectedIndex;
		}
	}

	public void ExecuteChangeLeader()
	{
		_onShowChangeLeaderPopup?.Invoke();
	}

	private void OnRoleAssigned()
	{
		Roles.ApplyActionOnAllItems(delegate(ClanRoleItemVM x)
		{
			x.Refresh();
		});
	}

	private void ExecuteLocationLink(string link)
	{
		Campaign.Current.EncyclopediaManager.GoToLink(link);
	}

	private void UpdatePartyBehaviorSelectionUpdate(SelectorVM<SelectorItemVM> s)
	{
		if (s.SelectedIndex != (int)Party.MobileParty.Objective)
		{
			Party.MobileParty.SetPartyObjective((MobileParty.PartyObjective)s.SelectedIndex);
		}
	}

	private void OnAutoRecruitChanged(bool value)
	{
		if (Party.IsMobile && Party.MobileParty.IsGarrison && Party.MobileParty.HomeSettlement?.Town != null)
		{
			Party.MobileParty.HomeSettlement.Town.GarrisonAutoRecruitmentIsEnabled = value;
		}
	}

	private void OnRoleSelectionToggled(ClanRoleItemVM role)
	{
		LastOpenedRoleSelection = role;
	}

	private static CharacterCode GetCharacterCode(CharacterObject character)
	{
		if (character.IsHero)
		{
			return CampaignUIHelper.GetCharacterCode(character);
		}
		uint color = Hero.MainHero.MapFaction.Color;
		uint color2 = Hero.MainHero.MapFaction.Color2;
		string equipmentCode = character.Equipment?.CalculateEquipmentCode();
		BodyProperties bodyProperties = character.GetBodyProperties(character.Equipment);
		return CharacterCode.CreateFrom(equipmentCode, bodyProperties, character.IsFemale, character.IsHero, color, color2, character.DefaultFormationClass, character.Race);
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		HeroMembers.ApplyActionOnAllItems(delegate(ClanPartyMemberItemVM h)
		{
			h.OnFinalize();
		});
		Roles.ApplyActionOnAllItems(delegate(ClanRoleItemVM x)
		{
			x.OnFinalize();
		});
	}

	private List<TooltipProperty> GetPartyTroopInfo(PartyBase party, FormationClass formationClass)
	{
		List<TooltipProperty> list = new List<TooltipProperty>();
		list.Add(new TooltipProperty("", GameTexts.FindText("str_formation_class_string", formationClass.GetName()).ToString(), 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.Title));
		foreach (TroopRosterElement item in Party.MemberRoster.GetTroopRoster())
		{
			if (!item.Character.IsHero && item.Character.DefaultFormationClass.Equals(formationClass))
			{
				list.Add(new TooltipProperty(item.Character.Name.ToString(), item.Number.ToString(), 0));
			}
		}
		return list;
	}
}
