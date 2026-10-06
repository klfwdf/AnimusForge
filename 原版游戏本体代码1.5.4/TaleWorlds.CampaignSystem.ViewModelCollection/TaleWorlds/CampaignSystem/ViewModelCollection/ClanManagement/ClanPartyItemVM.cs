using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ModuleManager;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement;

public abstract class ClanPartyItemVM : ViewModel
{
	public enum ClanPartyType
	{
		Main,
		Member,
		Caravan,
		Garrison
	}

	private bool _areCommandControlsVisible;

	private bool _areNavalControlsVisible;

	private bool _mayJoinOtherArmies;

	private bool _allowRaiding;

	private bool _donateTroopsToGarrisons;

	private bool _hasFleet;

	private string _mayJoinOtherArmiesText;

	private string _allowRaidingText;

	private string _donateTroopsToGarrisonsText;

	private string _hasFleetText;

	private int _smallShipCount;

	private int _mediumShipCount;

	private int _largeShipCount;

	private HintViewModel _smallShipHint;

	private HintViewModel _mediumShipHint;

	private HintViewModel _largeShipHint;

	public abstract int Expense { get; protected set; }

	public abstract int Income { get; protected set; }

	public abstract Hero Leader { get; }

	public abstract CampaignVec2 Position { get; }

	public PartyBase Party { get; protected set; }

	[DataSourceProperty]
	public abstract CharacterViewModel CharacterModel { get; set; }

	[DataSourceProperty]
	public abstract CharacterImageIdentifierVM LeaderVisual { get; set; }

	[DataSourceProperty]
	public abstract bool IsPendingPartyCreation { get; set; }

	[DataSourceProperty]
	public abstract bool IsSelected { get; set; }

	[DataSourceProperty]
	public abstract bool HasHeroMembers { get; set; }

	[DataSourceProperty]
	public abstract bool IsClanRoleSelectionHighlightEnabled { get; set; }

	[DataSourceProperty]
	public abstract bool IsRoleSelectionPopupVisible { get; set; }

	[DataSourceProperty]
	public abstract bool IsDisbanding { get; set; }

	[DataSourceProperty]
	public abstract bool IsInArmy { get; set; }

	[DataSourceProperty]
	public abstract bool CanUseActions { get; set; }

	[DataSourceProperty]
	public abstract bool IsChangeLeaderVisible { get; set; }

	[DataSourceProperty]
	public abstract bool IsChangeLeaderEnabled { get; set; }

	[DataSourceProperty]
	public abstract HintViewModel ActionsDisabledHint { get; set; }

	[DataSourceProperty]
	public abstract bool IsCaravan { get; set; }

	[DataSourceProperty]
	public abstract bool ShouldPartyHaveExpense { get; set; }

	[DataSourceProperty]
	public abstract bool HasCompanion { get; set; }

	[DataSourceProperty]
	public abstract bool IsAutoRecruitmentVisible { get; set; }

	[DataSourceProperty]
	public abstract bool AutoRecruitmentValue { get; set; }

	[DataSourceProperty]
	public abstract bool IsMembersAndRolesVisible { get; set; }

	[DataSourceProperty]
	public abstract bool IsLeaderTeleporting { get; }

	[DataSourceProperty]
	public abstract bool IsMainHeroParty { get; set; }

	[DataSourceProperty]
	public abstract ClanFinanceExpenseItemVM ExpenseItem { get; set; }

	[DataSourceProperty]
	public abstract ClanPartyMemberItemVM LeaderMember { get; set; }

	[DataSourceProperty]
	public abstract string PartySizeText { get; set; }

	[DataSourceProperty]
	public abstract string ShipCountText { get; set; }

	[DataSourceProperty]
	public abstract string MembersText { get; set; }

	[DataSourceProperty]
	public abstract string AssigneesText { get; set; }

	[DataSourceProperty]
	public abstract string RolesText { get; set; }

	[DataSourceProperty]
	public abstract string PartyLeaderRoleEffectsText { get; set; }

	[DataSourceProperty]
	public abstract string PartyLocationText { get; set; }

	[DataSourceProperty]
	public abstract string Name { get; set; }

	[DataSourceProperty]
	public abstract string PartySizeSubTitleText { get; set; }

	[DataSourceProperty]
	public abstract string PartyWageSubTitleText { get; set; }

	[DataSourceProperty]
	public abstract int InfantryCount { get; set; }

	[DataSourceProperty]
	public abstract int RangedCount { get; set; }

	[DataSourceProperty]
	public abstract int CavalryCount { get; set; }

	[DataSourceProperty]
	public abstract int HorseArcherCount { get; set; }

	[DataSourceProperty]
	public abstract int ShipCount { get; set; }

	[DataSourceProperty]
	public abstract string InArmyText { get; set; }

	[DataSourceProperty]
	public abstract string DisbandingText { get; set; }

	[DataSourceProperty]
	public abstract string AutoRecruitmentText { get; set; }

	[DataSourceProperty]
	public abstract HintViewModel AutoRecruitmentHint { get; set; }

	[DataSourceProperty]
	public abstract HintViewModel LeaderIsMovingToPartyHint { get; set; }

	[DataSourceProperty]
	public abstract HintViewModel InArmyHint { get; set; }

	[DataSourceProperty]
	public abstract HintViewModel ChangeLeaderHint { get; set; }

	[DataSourceProperty]
	public abstract BasicTooltipViewModel InfantryHint { get; set; }

	[DataSourceProperty]
	public abstract BasicTooltipViewModel RangedHint { get; set; }

	[DataSourceProperty]
	public abstract BasicTooltipViewModel CavalryHint { get; set; }

	[DataSourceProperty]
	public abstract BasicTooltipViewModel HorseArcherHint { get; set; }

	[DataSourceProperty]
	public abstract MBBindingList<ClanPartyMemberItemVM> HeroMembers { get; set; }

	[DataSourceProperty]
	public abstract MBBindingList<ClanRoleItemVM> Roles { get; set; }

	[DataSourceProperty]
	public bool AreCommandControlsVisible
	{
		get
		{
			return _areCommandControlsVisible;
		}
		set
		{
			if (value != _areCommandControlsVisible)
			{
				_areCommandControlsVisible = value;
				OnPropertyChangedWithValue(value, "AreCommandControlsVisible");
			}
		}
	}

	[DataSourceProperty]
	public bool AreNavalControlsVisible
	{
		get
		{
			return _areNavalControlsVisible;
		}
		set
		{
			if (value != _areNavalControlsVisible)
			{
				_areNavalControlsVisible = value;
				OnPropertyChangedWithValue(value, "AreNavalControlsVisible");
			}
		}
	}

	[DataSourceProperty]
	public bool MayJoinOtherArmies
	{
		get
		{
			return _mayJoinOtherArmies;
		}
		set
		{
			if (value != _mayJoinOtherArmies)
			{
				_mayJoinOtherArmies = value;
				OnPropertyChangedWithValue(value, "MayJoinOtherArmies");
				OnMayJoinOtherArmiesChanged(value);
			}
		}
	}

	[DataSourceProperty]
	public bool AllowRaiding
	{
		get
		{
			return _allowRaiding;
		}
		set
		{
			if (value != _allowRaiding)
			{
				_allowRaiding = value;
				OnPropertyChangedWithValue(value, "AllowRaiding");
				OnAllowRaidingChanged(value);
			}
		}
	}

	[DataSourceProperty]
	public bool DonateTroopsToGarrisons
	{
		get
		{
			return _donateTroopsToGarrisons;
		}
		set
		{
			if (value != _donateTroopsToGarrisons)
			{
				_donateTroopsToGarrisons = value;
				OnPropertyChangedWithValue(value, "DonateTroopsToGarrisons");
				OnDonateTroopsToGarrisonsChanged(value);
			}
		}
	}

	[DataSourceProperty]
	public bool HasFleet
	{
		get
		{
			return _hasFleet;
		}
		set
		{
			if (value != _hasFleet)
			{
				_hasFleet = value;
				OnPropertyChangedWithValue(value, "HasFleet");
				OnHasFleetChanged(value);
			}
		}
	}

	[DataSourceProperty]
	public string MayJoinOtherArmiesText
	{
		get
		{
			return _mayJoinOtherArmiesText;
		}
		set
		{
			if (value != _mayJoinOtherArmiesText)
			{
				_mayJoinOtherArmiesText = value;
				OnPropertyChangedWithValue(value, "MayJoinOtherArmiesText");
			}
		}
	}

	[DataSourceProperty]
	public string AllowRaidingText
	{
		get
		{
			return _allowRaidingText;
		}
		set
		{
			if (value != _allowRaidingText)
			{
				_allowRaidingText = value;
				OnPropertyChangedWithValue(value, "AllowRaidingText");
			}
		}
	}

	[DataSourceProperty]
	public string DonateTroopsToGarrisonsText
	{
		get
		{
			return _donateTroopsToGarrisonsText;
		}
		set
		{
			if (value != _donateTroopsToGarrisonsText)
			{
				_donateTroopsToGarrisonsText = value;
				OnPropertyChangedWithValue(value, "DonateTroopsToGarrisonsText");
			}
		}
	}

	[DataSourceProperty]
	public string HasFleetText
	{
		get
		{
			return _hasFleetText;
		}
		set
		{
			if (value != _hasFleetText)
			{
				_hasFleetText = value;
				OnPropertyChangedWithValue(value, "HasFleetText");
			}
		}
	}

	[DataSourceProperty]
	public int SmallShipCount
	{
		get
		{
			return _smallShipCount;
		}
		set
		{
			if (value != _smallShipCount)
			{
				_smallShipCount = value;
				OnPropertyChangedWithValue(value, "SmallShipCount");
			}
		}
	}

	[DataSourceProperty]
	public int MediumShipCount
	{
		get
		{
			return _mediumShipCount;
		}
		set
		{
			if (value != _mediumShipCount)
			{
				_mediumShipCount = value;
				OnPropertyChangedWithValue(value, "MediumShipCount");
			}
		}
	}

	[DataSourceProperty]
	public int LargeShipCount
	{
		get
		{
			return _largeShipCount;
		}
		set
		{
			if (value != _largeShipCount)
			{
				_largeShipCount = value;
				OnPropertyChangedWithValue(value, "LargeShipCount");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel SmallShipHint
	{
		get
		{
			return _smallShipHint;
		}
		set
		{
			if (value != _smallShipHint)
			{
				_smallShipHint = value;
				OnPropertyChangedWithValue(value, "SmallShipHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel MediumShipHint
	{
		get
		{
			return _mediumShipHint;
		}
		set
		{
			if (value != _mediumShipHint)
			{
				_mediumShipHint = value;
				OnPropertyChangedWithValue(value, "MediumShipHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel LargeShipHint
	{
		get
		{
			return _largeShipHint;
		}
		set
		{
			if (value != _largeShipHint)
			{
				_largeShipHint = value;
				OnPropertyChangedWithValue(value, "LargeShipHint");
			}
		}
	}

	public ClanPartyItemVM()
	{
		AreNavalControlsVisible = ModuleHelper.IsModuleActive("NavalDLC");
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		MayJoinOtherArmiesText = new TextObject("{=obmd0SWw}Allow joining other armies").ToString();
		AllowRaidingText = new TextObject("{=Kv7bQSkn}Allow raiding villages").ToString();
		DonateTroopsToGarrisonsText = new TextObject("{=bdqzhsnR}Allow donating troops to garrisons").ToString();
		HasFleetText = new TextObject("{=V4F2jNj7}Allow naval fleet").ToString();
		UpdateProperties();
	}

	public abstract void UpdateProperties();

	public abstract void OnPartySelection();

	public abstract void ExecuteChangeLeader();

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

	protected static CharacterCode GetCharacterCode(CharacterObject character)
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

	private void OnMayJoinOtherArmiesChanged(bool value)
	{
		if (Leader != null)
		{
			Leader.CanJoinArmy = value;
		}
	}

	private void OnAllowRaidingChanged(bool value)
	{
		if (Leader != null)
		{
			Leader.CanRaid = value;
		}
	}

	private void OnDonateTroopsToGarrisonsChanged(bool value)
	{
		if (Leader != null)
		{
			Leader.CanDonateTroopsToGarrison = value;
		}
	}

	private void OnHasFleetChanged(bool value)
	{
		if (Leader != null)
		{
			Leader.CanHaveFleet = value;
		}
	}
}
