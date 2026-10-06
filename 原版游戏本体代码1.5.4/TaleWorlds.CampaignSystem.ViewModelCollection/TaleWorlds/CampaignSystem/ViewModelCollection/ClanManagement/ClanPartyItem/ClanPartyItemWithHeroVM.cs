using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.ClanPartyItem;

public class ClanPartyItemWithHeroVM : ClanPartyItemVM
{
	private readonly Action<ClanPartyItemVM> _onAssignment;

	private readonly Action _onShowChangeLeaderPopup;

	private readonly TextObject _changeLeaderHintText = GameTexts.FindText("str_change_party_leader");

	private ClanFinanceExpenseItemVM _expenseItem;

	private ClanPartyMemberItemVM _leaderMember;

	private CharacterImageIdentifierVM _leaderVisual;

	private bool _isSelected;

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

	private bool _isPendingPartyCreation;

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

	public override Hero Leader { get; }

	public override CampaignVec2 Position => CampaignVec2.Invalid;

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
			return true;
		}
		set
		{
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
	public override bool IsCaravan
	{
		get
		{
			return false;
		}
		set
		{
		}
	}

	[DataSourceProperty]
	public override bool ShouldPartyHaveExpense
	{
		get
		{
			return false;
		}
		set
		{
		}
	}

	[DataSourceProperty]
	public override bool HasCompanion
	{
		get
		{
			return false;
		}
		set
		{
		}
	}

	[DataSourceProperty]
	public override bool IsAutoRecruitmentVisible
	{
		get
		{
			return false;
		}
		set
		{
		}
	}

	[DataSourceProperty]
	public override bool AutoRecruitmentValue
	{
		get
		{
			return false;
		}
		set
		{
		}
	}

	[DataSourceProperty]
	public override bool IsMembersAndRolesVisible
	{
		get
		{
			return true;
		}
		set
		{
		}
	}

	[DataSourceProperty]
	public override bool IsMainHeroParty
	{
		get
		{
			return false;
		}
		set
		{
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

	public override bool IsLeaderTeleporting => false;

	public ClanPartyItemWithHeroVM(Hero hero, Action<ClanPartyItemVM> onAssignment, Action onExpenseChange, Action onShowChangeLeaderPopup, Action<ClanRoleItemVM> onShowChangeRolePopup, ClanPartyType type, IDisbandPartyCampaignBehavior disbandBehavior, ITeleportationCampaignBehavior teleportationBehavior)
	{
		Leader = hero;
		HasHeroMembers = true;
		IsPendingPartyCreation = true;
		base.AreCommandControlsVisible = type == ClanPartyType.Member;
		if (Leader != null)
		{
			CharacterCode characterCode = ClanPartyItemVM.GetCharacterCode(Leader.CharacterObject);
			LeaderVisual = new CharacterImageIdentifierVM(characterCode);
			CharacterModel = new CharacterViewModel(CharacterViewModel.StanceTypes.None);
			CharacterModel.FillFrom(Leader.CharacterObject, -1, Leader.MapFaction.Banner?.BannerCode);
			CharacterModel.ArmorColor1 = Leader.MapFaction?.Color ?? 0;
			CharacterModel.ArmorColor2 = Leader.MapFaction?.Color2 ?? 0;
			base.AllowRaiding = Leader.CanRaid;
			base.DonateTroopsToGarrisons = Leader.CanDonateTroopsToGarrison;
			base.MayJoinOtherArmies = Leader.CanJoinArmy;
			base.HasFleet = Leader.CanHaveFleet;
		}
		else
		{
			LeaderVisual = new CharacterImageIdentifierVM(null);
			CharacterModel = new CharacterViewModel();
		}
		_onAssignment = onAssignment;
		_onShowChangeLeaderPopup = onShowChangeLeaderPopup;
		IsDisbanding = false;
		TextObject disabledReason = TextObject.GetEmpty();
		IsChangeLeaderVisible = true;
		IsChangeLeaderEnabled = IsChangeLeaderVisible && CampaignUIHelper.GetMapScreenActionIsEnabledWithReason(out disabledReason);
		ChangeLeaderHint = new HintViewModel(IsChangeLeaderEnabled ? _changeLeaderHintText : disabledReason);
		ActionsDisabledHint = new HintViewModel();
		CanUseActions = CampaignUIHelper.GetMapScreenActionIsEnabledWithReason(out var disabledReason2);
		ActionsDisabledHint.HintText = (CanUseActions ? TextObject.GetEmpty() : disabledReason2);
		AutoRecruitmentHint = null;
		IsAutoRecruitmentVisible = false;
		AutoRecruitmentValue = false;
		HeroMembers = new MBBindingList<ClanPartyMemberItemVM>();
		Roles = new MBBindingList<ClanRoleItemVM>();
		InfantryHint = null;
		CavalryHint = null;
		RangedHint = null;
		HorseArcherHint = null;
		InArmyHint = new HintViewModel();
		base.SmallShipHint = new HintViewModel(new TextObject("{=SeXdiWJL}Small Ships"));
		base.MediumShipHint = new HintViewModel(new TextObject("{=XcIDr42e}Medium Ships"));
		base.LargeShipHint = new HintViewModel(new TextObject("{=ReqtAxsC}Large Ships"));
		RefreshValues();
	}

	public override void UpdateProperties()
	{
		MembersText = GameTexts.FindText("str_members").ToString();
		AssigneesText = GameTexts.FindText("str_clan_assignee_title").ToString();
		RolesText = GameTexts.FindText("str_clan_role_title").ToString();
		PartyLeaderRoleEffectsText = GameTexts.FindText("str_clan_party_leader_roles_and_effects").ToString();
		AutoRecruitmentText = GameTexts.FindText("str_clan_auto_recruitment").ToString();
		TextObject textObject = new TextObject("{=shL0WElC}{TROOP.NAME}{.o} Party");
		textObject.SetCharacterProperties("TROOP", Leader.CharacterObject);
		Name = textObject.ToString();
		IEmptyClanPartiesCampaignBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<IEmptyClanPartiesCampaignBehavior>();
		ShipCount = campaignBehavior.GetShipCountForCachedLordPartyForPlayerClan(Leader);
		PartyLocationText = CampaignUIHelper.GetHeroBehaviorText(Leader);
		TextObject textObject2 = GameTexts.FindText("str_LEFT_over_RIGHT");
		textObject2.SetTextVariable("LEFT", 0);
		textObject2.SetTextVariable("RIGHT", CampaignUIHelper.GetPartySizeLimitForLeader(Leader));
		PartySizeText = textObject2.ToString();
		TextObject textObject3 = GameTexts.FindText("str_LEFT_colon_RIGHT");
		textObject3.SetTextVariable("LEFT", GameTexts.FindText("str_party_morale_party_size"));
		textObject3.SetTextVariable("RIGHT", textObject2);
		PartySizeSubTitleText = textObject3.ToString();
		HeroMembers.Clear();
		ClanPartyMemberItemVM clanPartyMemberItemVM = new ClanPartyMemberItemVM(Leader, null);
		HeroMembers.Add(clanPartyMemberItemVM);
		if (clanPartyMemberItemVM.IsLeader)
		{
			LeaderMember = clanPartyMemberItemVM;
		}
		if (IsMembersAndRolesVisible)
		{
			Roles.ApplyActionOnAllItems(delegate(ClanRoleItemVM x)
			{
				x.OnFinalize();
			});
			Roles.Clear();
			foreach (PartyRole assignablePartyRole in Campaign.Current.Models.ClanMemberPartyRoleModel.GetAssignablePartyRoles())
			{
				Roles.Add(new ClanRoleItemVM(null, assignablePartyRole, HeroMembers, OnRoleSelectionToggled));
			}
		}
		RefreshFleetComposition();
	}

	private void RefreshFleetComposition()
	{
		MBReadOnlyList<Ship> shipsForCachedLordPartyForPlayerClan = Campaign.Current.GetCampaignBehavior<IEmptyClanPartiesCampaignBehavior>().GetShipsForCachedLordPartyForPlayerClan(Leader);
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		using (List<Ship>.Enumerator enumerator = shipsForCachedLordPartyForPlayerClan.GetEnumerator())
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

	public override void ExecuteChangeLeader()
	{
		_onShowChangeLeaderPopup?.Invoke();
	}

	public override void OnPartySelection()
	{
		_onAssignment(this);
	}

	private void OnRoleSelectionToggled(ClanRoleItemVM role)
	{
	}
}
