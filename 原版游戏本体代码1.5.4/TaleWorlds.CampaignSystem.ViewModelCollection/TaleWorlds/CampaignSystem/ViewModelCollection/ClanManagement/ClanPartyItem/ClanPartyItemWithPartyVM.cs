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
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.ClanPartyItem;

public class ClanPartyItemWithPartyVM : ClanPartyItemVM
{
	private readonly Action<ClanPartyItemVM> _onAssignment;

	private readonly Action _onExpenseChange;

	private readonly Action _onShowChangeLeaderPopup;

	private readonly Action<ClanRoleItemVM> _onShowChangeRolePopup;

	private readonly ClanPartyType _type;

	private readonly TextObject _changeLeaderHintText = GameTexts.FindText("str_change_party_leader");

	private readonly IDisbandPartyCampaignBehavior _disbandBehavior;

	private readonly bool _isLeaderTeleporting;

	private readonly CharacterObject _leader;

	private ClanFinanceExpenseItemVM _expenseItem;

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

	private bool _isMembersAndRolesVisible;

	private bool _isPendingPartyCreation;

	private bool _isCaravan;

	private bool _isDisbanding;

	private bool _isInArmy;

	private bool _canUseActions;

	private bool _isChangeLeaderVisible;

	private bool _isChangeLeaderEnabled;

	private bool _isClanRoleSelectionHighlightEnabled;

	private bool _isRoleSelectionPopupVisible;

	private HintViewModel _leaderIsMovingToPartyHint;

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

	public override int Expense { get; protected set; }

	public override int Income { get; protected set; }

	public override Hero Leader => _leader?.HeroObject;

	public override CampaignVec2 Position => base.Party?.Position ?? CampaignVec2.Invalid;

	public override bool IsLeaderTeleporting => _isLeaderTeleporting;

	[DataSourceProperty]
	public override CharacterViewModel CharacterModel
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
	public override CharacterImageIdentifierVM LeaderVisual
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
	public override bool IsSelected
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
	public override bool HasHeroMembers
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
	public override bool IsClanRoleSelectionHighlightEnabled
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
	public override bool IsRoleSelectionPopupVisible
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
	public override bool IsDisbanding
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
	public override bool IsInArmy
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
	public override bool CanUseActions
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
	public override bool IsChangeLeaderVisible
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
	public override bool IsChangeLeaderEnabled
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
	public override HintViewModel ActionsDisabledHint
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
	public override bool IsPendingPartyCreation
	{
		get
		{
			return _isPendingPartyCreation;
		}
		set
		{
			if (value != _isPendingPartyCreation)
			{
				_isPendingPartyCreation = value;
				OnPropertyChangedWithValue(value, "IsPendingPartyCreation");
			}
		}
	}

	[DataSourceProperty]
	public override bool IsCaravan
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
	public override bool ShouldPartyHaveExpense
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
	public override bool HasCompanion
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
	public override bool IsAutoRecruitmentVisible
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
	public override bool AutoRecruitmentValue
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
	public override bool IsMembersAndRolesVisible
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
	public override bool IsMainHeroParty
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
	public override ClanFinanceExpenseItemVM ExpenseItem
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
	public override ClanPartyMemberItemVM LeaderMember
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
	public override string PartySizeText
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
	public override string ShipCountText
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
	public override string MembersText
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
	public override string AssigneesText
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
	public override string RolesText
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
	public override string PartyLeaderRoleEffectsText
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
	public override string PartyLocationText
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
	public override string Name
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
	public override string PartySizeSubTitleText
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
	public override string PartyWageSubTitleText
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
	public override int InfantryCount
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
	public override int RangedCount
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
	public override int CavalryCount
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
	public override int HorseArcherCount
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
	public override int ShipCount
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
	public override string InArmyText
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
	public override string DisbandingText
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
	public override string AutoRecruitmentText
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
	public override HintViewModel AutoRecruitmentHint
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
	public override HintViewModel LeaderIsMovingToPartyHint
	{
		get
		{
			return _leaderIsMovingToPartyHint;
		}
		set
		{
			if (value != _leaderIsMovingToPartyHint)
			{
				_leaderIsMovingToPartyHint = value;
				OnPropertyChangedWithValue(value, "LeaderIsMovingToPartyHint");
			}
		}
	}

	[DataSourceProperty]
	public override HintViewModel InArmyHint
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
	public override HintViewModel ChangeLeaderHint
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
	public override BasicTooltipViewModel InfantryHint
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
	public override BasicTooltipViewModel RangedHint
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
	public override BasicTooltipViewModel CavalryHint
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
	public override BasicTooltipViewModel HorseArcherHint
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
	public override MBBindingList<ClanPartyMemberItemVM> HeroMembers
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
	public override MBBindingList<ClanRoleItemVM> Roles
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

	public ClanPartyItemWithPartyVM(PartyBase party, Action<ClanPartyItemVM> onAssignment, Action onExpenseChange, Action onShowChangeLeaderPopup, Action<ClanRoleItemVM> onShowChangeRolePopup, ClanPartyType type, IDisbandPartyCampaignBehavior disbandBehavior, ITeleportationCampaignBehavior teleportationBehavior)
	{
		base.Party = party;
		_type = type;
		_disbandBehavior = disbandBehavior;
		_leader = CampaignUIHelper.GetVisualPartyLeader(base.Party);
		HasHeroMembers = party.IsMobile;
		IsPendingPartyCreation = false;
		if (_leader == null)
		{
			TroopRosterElement troopRosterElement = base.Party.MemberRoster.GetTroopRoster().FirstOrDefault();
			if (!troopRosterElement.Equals(default(TroopRosterElement)))
			{
				_leader = troopRosterElement.Character;
			}
			else
			{
				_leader = base.Party.MapFaction?.BasicTroop;
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
			CharacterCode characterCode = ClanPartyItemVM.GetCharacterCode(_leader);
			LeaderVisual = new CharacterImageIdentifierVM(characterCode);
			CharacterModel = new CharacterViewModel(CharacterViewModel.StanceTypes.None);
			CharacterModel.FillFrom(_leader, -1, base.Party.Banner?.BannerCode);
			CharacterModel.ArmorColor1 = base.Party.MapFaction?.Color ?? 0;
			CharacterModel.ArmorColor2 = base.Party.MapFaction?.Color2 ?? 0;
		}
		else
		{
			LeaderVisual = new CharacterImageIdentifierVM(null);
			CharacterModel = new CharacterViewModel();
		}
		_onAssignment = onAssignment;
		_onExpenseChange = onExpenseChange;
		_onShowChangeLeaderPopup = onShowChangeLeaderPopup;
		_onShowChangeRolePopup = onShowChangeRolePopup;
		IsDisbanding = base.Party.MobileParty.IsDisbanding || (_disbandBehavior?.IsPartyWaitingForDisband(party.MobileParty) ?? false);
		ShouldPartyHaveExpense = !party.MobileParty.IsMilitia && !party.MobileParty.IsVillager && party.MobileParty.IsActive && !IsDisbanding && (type == ClanPartyType.Garrison || type == ClanPartyType.Member);
		IsCaravan = type == ClanPartyType.Caravan;
		base.AreCommandControlsVisible = type == ClanPartyType.Member;
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
				Debug.FailedAssert("This party should have expense info but it doesn't", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\ClanManagement\\ClanPartyItem\\ClanPartyItemWithPartyVM.cs", ".ctor", 115);
			}
		}
		if (IsCaravan)
		{
			Income = Campaign.Current.Models.ClanFinanceModel.CalculateOwnerIncomeFromCaravan(party.MobileParty);
		}
		AutoRecruitmentHint = new HintViewModel(GameTexts.FindText("str_clan_auto_recruitment_hint"));
		IsAutoRecruitmentVisible = party.MobileParty.IsGarrison;
		AutoRecruitmentValue = party.MobileParty.IsGarrison && base.Party.MobileParty.CurrentSettlement.Town.GarrisonAutoRecruitmentIsEnabled;
		if (Leader != null)
		{
			base.AllowRaiding = Leader.CanRaid;
			base.DonateTroopsToGarrisons = Leader.CanDonateTroopsToGarrison;
			base.MayJoinOtherArmies = Leader.CanJoinArmy;
			base.HasFleet = Leader.CanHaveFleet;
		}
		HeroMembers = new MBBindingList<ClanPartyMemberItemVM>();
		Roles = new MBBindingList<ClanRoleItemVM>();
		base.SmallShipHint = new HintViewModel(new TextObject("{=SeXdiWJL}Small Ships"));
		base.MediumShipHint = new HintViewModel(new TextObject("{=XcIDr42e}Medium Ships"));
		base.LargeShipHint = new HintViewModel(new TextObject("{=ReqtAxsC}Large Ships"));
		InfantryHint = new BasicTooltipViewModel(() => GetPartyTroopInfo(base.Party, FormationClass.Infantry));
		CavalryHint = new BasicTooltipViewModel(() => GetPartyTroopInfo(base.Party, FormationClass.Cavalry));
		RangedHint = new BasicTooltipViewModel(() => GetPartyTroopInfo(base.Party, FormationClass.Ranged));
		HorseArcherHint = new BasicTooltipViewModel(() => GetPartyTroopInfo(base.Party, FormationClass.HorseArcher));
		ActionsDisabledHint = new HintViewModel();
		InArmyHint = new HintViewModel();
		RefreshValues();
	}

	public override void UpdateProperties()
	{
		MembersText = GameTexts.FindText("str_members").ToString();
		AssigneesText = GameTexts.FindText("str_clan_assignee_title").ToString();
		RolesText = GameTexts.FindText("str_clan_role_title").ToString();
		PartyLeaderRoleEffectsText = GameTexts.FindText("str_clan_party_leader_roles_and_effects").ToString();
		AutoRecruitmentText = GameTexts.FindText("str_clan_auto_recruitment").ToString();
		if (base.Party == PartyBase.MainParty && Hero.MainHero.IsPrisoner)
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
			Name = base.Party.Name.ToString();
		}
		IsMainHeroParty = _type == ClanPartyType.Main;
		PartyLocationText = CampaignUIHelper.GetPartyLocationText(base.Party.MobileParty);
		GameTexts.SetVariable("LEFT", base.Party.MobileParty.MemberRoster.TotalManCount);
		if (base.Party?.LeaderHero != null)
		{
			GameTexts.SetVariable("RIGHT", base.Party.PartySizeLimit);
		}
		else if (Leader != null)
		{
			LeaderIsMovingToPartyHint = new HintViewModel(new TextObject("{=g08mptth}Moving to a party to be the new leader"));
			GameTexts.SetVariable("RIGHT", CampaignUIHelper.GetPartySizeLimitForLeader(Leader));
		}
		else
		{
			GameTexts.SetVariable("RIGHT", base.Party.MobileParty.MemberRoster.TotalManCount);
		}
		string text = GameTexts.FindText("str_LEFT_over_RIGHT").ToString();
		string content = GameTexts.FindText("str_party_morale_party_size").ToString();
		PartySizeText = text;
		GameTexts.SetVariable("LEFT", content);
		GameTexts.SetVariable("RIGHT", text);
		PartySizeSubTitleText = GameTexts.FindText("str_LEFT_colon_RIGHT").ToString();
		GameTexts.SetVariable("LEFT", GameTexts.FindText("str_party_wage"));
		GameTexts.SetVariable("RIGHT", base.Party.MobileParty.TotalWage);
		PartyWageSubTitleText = GameTexts.FindText("str_LEFT_colon_RIGHT").ToString();
		InArmyText = "";
		if (base.Party.MobileParty.Army != null)
		{
			IsInArmy = true;
			TextObject textObject3 = GameTexts.FindText("str_clan_in_army_hint");
			textObject3.SetTextVariable("ARMY_LEADER", base.Party.MobileParty.Army.LeaderParty?.LeaderHero?.Name.ToString() ?? string.Empty);
			InArmyHint = new HintViewModel(textObject3);
			InArmyText = GameTexts.FindText("str_in_army").ToString();
		}
		DisbandingText = "";
		IsMembersAndRolesVisible = !IsDisbanding && _type != ClanPartyType.Garrison;
		if (IsDisbanding)
		{
			DisbandingText = GameTexts.FindText("str_disbanding").ToString();
		}
		if (_leader != null)
		{
			CharacterModel.FillFrom(_leader, -1, base.Party.Banner?.BannerCode);
			CharacterModel.ArmorColor1 = base.Party.MapFaction?.Color ?? 0;
			CharacterModel.ArmorColor2 = base.Party.MapFaction?.Color2 ?? 0;
		}
		HeroMembers.Clear();
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		foreach (TroopRosterElement item2 in base.Party.MemberRoster.GetTroopRoster())
		{
			Hero heroObject = item2.Character.HeroObject;
			if (heroObject != null && heroObject.Clan == Clan.PlayerClan && heroObject.GovernorOf == null)
			{
				ClanPartyMemberItemVM clanPartyMemberItemVM = new ClanPartyMemberItemVM(item2.Character.HeroObject, base.Party.MobileParty);
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
			ClanPartyMemberItemVM item = (LeaderMember = new ClanPartyMemberItemVM(_leader.HeroObject, base.Party.MobileParty));
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
				Roles.Add(new ClanRoleItemVM(base.Party.MobileParty, assignablePartyRole, HeroMembers, OnRoleSelectionToggled));
			}
		}
		InfantryCount = num;
		RangedCount = num2;
		CavalryCount = num3;
		HorseArcherCount = num4;
		TextObject disabledReason;
		bool mapScreenActionIsEnabledWithReason = CampaignUIHelper.GetMapScreenActionIsEnabledWithReason(out disabledReason);
		CanUseActions = mapScreenActionIsEnabledWithReason && !IsDisbanding;
		if (!mapScreenActionIsEnabledWithReason)
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
			ActionsDisabledHint.HintText = disabledReason;
		}
		else if (IsDisbanding)
		{
			ActionsDisabledHint.HintText = new TextObject("{=BHFxYCpv}You cannot perform this action while the party is disbanding");
		}
		else
		{
			ActionsDisabledHint.HintText = TextObject.GetEmpty();
		}
		ShipCount = base.Party.Ships.Count;
		ShipCountText = GameTexts.FindText("str_LEFT_colon_RIGHT").SetTextVariable("LEFT", new TextObject("{=7Q8ufo5X}Ships").ToString()).SetTextVariable("RIGHT", ShipCount)
			.ToString();
		RefreshFleetComposition();
	}

	private void RefreshFleetComposition()
	{
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		using (List<TaleWorlds.CampaignSystem.Naval.Ship>.Enumerator enumerator = base.Party.Ships.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				switch (enumerator.Current.ShipHull?.Type)
				{
				case ShipHull.ShipType.Light:
					num++;
					break;
				case ShipHull.ShipType.Medium:
					num2++;
					break;
				case ShipHull.ShipType.Heavy:
					num3++;
					break;
				}
			}
		}
		base.SmallShipCount = num;
		base.MediumShipCount = num2;
		base.LargeShipCount = num3;
	}

	private void OnExpenseChange()
	{
		_onExpenseChange();
	}

	public override void OnPartySelection()
	{
		_onAssignment(this);
	}

	public override void ExecuteChangeLeader()
	{
		_onShowChangeLeaderPopup?.Invoke();
	}

	private void ExecuteLocationLink(string link)
	{
		Campaign.Current.EncyclopediaManager.GoToLink(link);
	}

	private void OnAutoRecruitChanged(bool value)
	{
		if (base.Party.IsMobile && base.Party.MobileParty.IsGarrison && base.Party.MobileParty.HomeSettlement?.Town != null)
		{
			base.Party.MobileParty.HomeSettlement.Town.GarrisonAutoRecruitmentIsEnabled = value;
		}
	}

	private void OnRoleSelectionToggled(ClanRoleItemVM role)
	{
		_onShowChangeRolePopup?.Invoke(role);
	}

	private List<TooltipProperty> GetPartyTroopInfo(PartyBase party, FormationClass formationClass)
	{
		List<TooltipProperty> list = new List<TooltipProperty>();
		list.Add(new TooltipProperty("", GameTexts.FindText("str_formation_class_string", formationClass.GetName()).ToString(), 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.Title));
		foreach (TroopRosterElement item in base.Party.MemberRoster.GetTroopRoster())
		{
			if (!item.Character.IsHero && item.Character.DefaultFormationClass.Equals(formationClass))
			{
				list.Add(new TooltipProperty(item.Character.Name.ToString(), item.Number.ToString(), 0));
			}
		}
		return list;
	}
}
