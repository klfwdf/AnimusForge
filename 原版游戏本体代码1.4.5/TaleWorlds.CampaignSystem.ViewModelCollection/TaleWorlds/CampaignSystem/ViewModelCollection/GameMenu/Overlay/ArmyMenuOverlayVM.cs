using System;
using System.Linq;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Core.ViewModelCollection.Tutorial;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Overlay;

[MenuOverlay("ArmyMenuOverlay")]
public class ArmyMenuOverlayVM : GameMenuOverlay
{
	private const float CohesionWarningMin = 30f;

	public Action OpenArmyManagement;

	private readonly Concept _cohesionConceptObj;

	private string _latestTutorialElementID;

	private bool _isVisualsDirty;

	private MBBindingList<GameMenuPartyItemVM> _partyList;

	private string _manCountText;

	private int _cohesion;

	private int _food;

	private bool _isCohesionWarningEnabled;

	private bool _isPlayerArmyLeader;

	private bool _canManageArmy;

	private HintViewModel _manageArmyHint;

	public ElementNotificationVM _tutorialNotification;

	private BasicTooltipViewModel _cohesionHint;

	private BasicTooltipViewModel _manCountHint;

	private BasicTooltipViewModel _foodHint;

	private MBBindingList<StringItemWithHintVM> _issueList;

	private Army ArmyToUse
	{
		get
		{
			object obj = MobileParty.MainParty?.Army;
			if (obj == null)
			{
				MobileParty mainParty = MobileParty.MainParty;
				if (mainParty == null)
				{
					return null;
				}
				MobileParty targetParty = mainParty.TargetParty;
				if (targetParty == null)
				{
					return null;
				}
				obj = targetParty.Army;
			}
			return (Army)obj;
		}
	}

	[DataSourceProperty]
	public ElementNotificationVM TutorialNotification
	{
		get
		{
			return _tutorialNotification;
		}
		set
		{
			if (value != _tutorialNotification)
			{
				_tutorialNotification = value;
				OnPropertyChangedWithValue(value, "TutorialNotification");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel ManageArmyHint
	{
		get
		{
			return _manageArmyHint;
		}
		set
		{
			if (value != _manageArmyHint)
			{
				_manageArmyHint = value;
				OnPropertyChangedWithValue(value, "ManageArmyHint");
			}
		}
	}

	[DataSourceProperty]
	public int Cohesion
	{
		get
		{
			return _cohesion;
		}
		set
		{
			if (value != _cohesion)
			{
				_cohesion = value;
				OnPropertyChangedWithValue(value, "Cohesion");
			}
		}
	}

	[DataSourceProperty]
	public bool IsCohesionWarningEnabled
	{
		get
		{
			return _isCohesionWarningEnabled;
		}
		set
		{
			if (value != _isCohesionWarningEnabled)
			{
				_isCohesionWarningEnabled = value;
				OnPropertyChangedWithValue(value, "IsCohesionWarningEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool CanManageArmy
	{
		get
		{
			return _canManageArmy;
		}
		set
		{
			if (value != _canManageArmy)
			{
				_canManageArmy = value;
				OnPropertyChangedWithValue(value, "CanManageArmy");
			}
		}
	}

	[DataSourceProperty]
	public bool IsPlayerArmyLeader
	{
		get
		{
			return _isPlayerArmyLeader;
		}
		set
		{
			if (value != _isPlayerArmyLeader)
			{
				_isPlayerArmyLeader = value;
				OnPropertyChangedWithValue(value, "IsPlayerArmyLeader");
			}
		}
	}

	[DataSourceProperty]
	public string ManCountText
	{
		get
		{
			return _manCountText;
		}
		set
		{
			if (value != _manCountText)
			{
				_manCountText = value;
				OnPropertyChangedWithValue(value, "ManCountText");
			}
		}
	}

	[DataSourceProperty]
	public int Food
	{
		get
		{
			return _food;
		}
		set
		{
			if (value != _food)
			{
				_food = value;
				OnPropertyChangedWithValue(value, "Food");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<GameMenuPartyItemVM> PartyList
	{
		get
		{
			return _partyList;
		}
		set
		{
			if (value != _partyList)
			{
				_partyList = value;
				OnPropertyChangedWithValue(value, "PartyList");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel CohesionHint
	{
		get
		{
			return _cohesionHint;
		}
		set
		{
			if (value != _cohesionHint)
			{
				_cohesionHint = value;
				OnPropertyChangedWithValue(value, "CohesionHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel ManCountHint
	{
		get
		{
			return _manCountHint;
		}
		set
		{
			if (value != _manCountHint)
			{
				_manCountHint = value;
				OnPropertyChangedWithValue(value, "ManCountHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel FoodHint
	{
		get
		{
			return _foodHint;
		}
		set
		{
			if (value != _foodHint)
			{
				_foodHint = value;
				OnPropertyChangedWithValue(value, "FoodHint");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<StringItemWithHintVM> IssueList
	{
		get
		{
			if (_issueList == null)
			{
				_issueList = new MBBindingList<StringItemWithHintVM>();
			}
			return _issueList;
		}
	}

	public ArmyMenuOverlayVM()
	{
		PartyList = new MBBindingList<GameMenuPartyItemVM>();
		base.CurrentOverlayType = 2;
		base.IsInitializationOver = false;
		CohesionHint = new BasicTooltipViewModel();
		ManCountHint = new BasicTooltipViewModel();
		FoodHint = new BasicTooltipViewModel();
		TutorialNotification = new ElementNotificationVM();
		ManageArmyHint = new HintViewModel();
		Refresh();
		_contextMenuItem = null;
		CampaignEvents.ArmyOverlaySetDirtyEvent.AddNonSerializedListener(this, Refresh);
		CampaignEvents.PartyAttachedAnotherParty.AddNonSerializedListener(this, OnPartyAttachedAnotherParty);
		CampaignEvents.OnTroopRecruitedEvent.AddNonSerializedListener(this, OnTroopRecruited);
		Game.Current.EventManager.RegisterEvent<TutorialNotificationElementChangeEvent>(OnTutorialNotificationElementIDChange);
		_cohesionConceptObj = Concept.All.SingleOrDefault((Concept c) => c.StringId == "str_game_objects_army_cohesion");
		base.IsInitializationOver = true;
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		TutorialNotification?.RefreshValues();
		Refresh();
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		CampaignEvents.ArmyOverlaySetDirtyEvent.ClearListeners(this);
		CampaignEvents.PartyAttachedAnotherParty.ClearListeners(this);
		CampaignEvents.OnTroopRecruitedEvent.ClearListeners(this);
		Game.Current.EventManager.UnregisterEvent<TutorialNotificationElementChangeEvent>(OnTutorialNotificationElementIDChange);
	}

	protected override void ExecuteOnSetAsActiveContextMenuItem(GameMenuPartyItemVM troop)
	{
		base.ExecuteOnSetAsActiveContextMenuItem(troop);
		base.ContextList.Clear();
		if (_contextMenuItem.Party.MobileParty?.Army != null && GetIsPlayerArmyLeader(_contextMenuItem.Party.MobileParty.Army) && _contextMenuItem.Party.MapEvent == null && _contextMenuItem.Party != _contextMenuItem.Party.MobileParty.Army.LeaderParty.Party)
		{
			TextObject disabledReason;
			bool mapScreenActionIsEnabledWithReason = CampaignUIHelper.GetMapScreenActionIsEnabledWithReason(out disabledReason);
			base.ContextList.Add(new StringItemWithEnabledAndHintVM(base.ExecuteTroopAction, GameTexts.FindText("str_menu_overlay_context_list", MenuOverlayContextList.ArmyDismiss.ToString()).ToString(), mapScreenActionIsEnabledWithReason, MenuOverlayContextList.ArmyDismiss, disabledReason));
		}
		float getEncounterJoiningRadius = Campaign.Current.Models.EncounterModel.GetEncounterJoiningRadius;
		CampaignVec2 v = MobileParty.MainParty.MapEvent?.Position ?? MobileParty.MainParty.Position;
		bool flag = troop.Party.MobileParty?.Position.DistanceSquared(v) < getEncounterJoiningRadius * getEncounterJoiningRadius;
		bool flag2 = troop.Party.MobileParty.MapEvent == MobileParty.MainParty.MapEvent;
		bool flag3 = PlayerEncounter.EncounteredParty != null && PlayerEncounter.EncounteredParty.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction);
		if (_contextMenuItem.Party.LeaderHero != null && flag && flag2 && !flag3 && _contextMenuItem.Party != PartyBase.MainParty && PlayerEncounter.Current?.BattleSimulation == null)
		{
			base.ContextList.Add(new StringItemWithEnabledAndHintVM(base.ExecuteTroopAction, GameTexts.FindText("str_menu_overlay_context_list", MenuOverlayContextList.DonateTroops.ToString()).ToString(), enabled: true, MenuOverlayContextList.DonateTroops));
			if (MobileParty.MainParty.CurrentSettlement == null && LocationComplex.Current == null)
			{
				base.ContextList.Add(new StringItemWithEnabledAndHintVM(base.ExecuteTroopAction, GameTexts.FindText("str_menu_overlay_context_list", MenuOverlayContextList.ConverseWithLeader.ToString()).ToString(), enabled: true, MenuOverlayContextList.ConverseWithLeader));
			}
		}
		base.ContextList.Add(new StringItemWithEnabledAndHintVM(base.ExecuteTroopAction, GameTexts.FindText("str_menu_overlay_context_list", MenuOverlayContextList.Encyclopedia.ToString()).ToString(), enabled: true, MenuOverlayContextList.Encyclopedia));
		CharacterObject characterObject = _contextMenuItem.Character ?? _contextMenuItem.Party.LeaderHero?.CharacterObject;
		if (characterObject == null)
		{
			Debug.FailedAssert("ArmyMenuOverlayVM.ExecuteOnSetAsActiveContextMenuItem called on party with no leader hero: " + _contextMenuItem.Party.Name, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\GameMenu\\Overlay\\ArmyMenuOverlayVM.cs", "ExecuteOnSetAsActiveContextMenuItem", 124);
		}
		else
		{
			CampaignEventDispatcher.Instance.OnCharacterPortraitPopUpOpened(characterObject);
		}
	}

	public override void OnFrameTick(float dt)
	{
		base.OnFrameTick(dt);
		CanManageArmy = CampaignUIHelper.GetCanManageCurrentArmyWithReason(out var disabledReason);
		ManageArmyHint.HintText = disabledReason;
		for (int i = 0; i < PartyList.Count; i++)
		{
			PartyList[i].RefreshQuestStatus();
		}
		if (_isVisualsDirty)
		{
			RefreshVisualsOfItems();
			_isVisualsDirty = false;
		}
	}

	public sealed override void Refresh()
	{
		if (ArmyToUse != null)
		{
			base.IsInitializationOver = false;
			UpdateLists();
			UpdateProperties();
			base.IsInitializationOver = true;
		}
	}

	private void UpdateProperties()
	{
		MBTextManager.SetTextVariable("newline", "\n");
		Army army = ArmyToUse;
		if (army == null)
		{
			Debug.FailedAssert("Army is null but trying to update army overlay properties", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\GameMenu\\Overlay\\ArmyMenuOverlayVM.cs", "UpdateProperties", 169);
			return;
		}
		float num = army.LeaderParty.Food;
		foreach (MobileParty attachedParty in army.LeaderParty.AttachedParties)
		{
			num += attachedParty.Food;
		}
		Food = (int)num;
		Cohesion = (int)army.Cohesion;
		ManCountText = CampaignUIHelper.GetPartyNameplateText(army.LeaderParty, includeAttachedParties: true);
		FoodHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetArmyFoodTooltip(army));
		CohesionHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetArmyCohesionTooltip(army));
		ManCountHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetArmyManCountTooltip(army));
		IsCohesionWarningEnabled = army.Cohesion <= 30f;
		IsPlayerArmyLeader = GetIsPlayerArmyLeader(army);
	}

	private void UpdateLists()
	{
		Army armyToUse = ArmyToUse;
		if (armyToUse == null)
		{
			Debug.FailedAssert("Army is null but trying to update army overlay lists", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\GameMenu\\Overlay\\ArmyMenuOverlayVM.cs", "UpdateLists", 198);
			return;
		}
		for (int num = PartyList.Count - 1; num >= 0; num--)
		{
			GameMenuPartyItemVM partyVM = PartyList[num];
			if (!armyToUse.Parties.Any((MobileParty p) => p.Party == partyVM.Party))
			{
				PartyList.RemoveAt(num);
			}
		}
		for (int num2 = 0; num2 < armyToUse.Parties.Count; num2++)
		{
			MobileParty party = armyToUse.Parties[num2];
			if (!PartyList.Any((GameMenuPartyItemVM p) => p.Party == party.Party))
			{
				bool flag = party == armyToUse.LeaderParty;
				GameMenuPartyItemVM item = new GameMenuPartyItemVM(ExecuteOnSetAsActiveContextMenuItem, party.Party, canShowQuest: true)
				{
					IsLeader = flag
				};
				if (flag)
				{
					PartyList.Insert(0, item);
				}
				else
				{
					PartyList.Add(item);
				}
			}
		}
		foreach (GameMenuPartyItemVM party2 in PartyList)
		{
			party2.RefreshProperties();
		}
	}

	public void ExecuteOpenArmyManagement()
	{
		Army armyToUse = ArmyToUse;
		if (armyToUse != null && GetIsPlayerArmyLeader(armyToUse))
		{
			OpenArmyManagement?.Invoke();
		}
	}

	private void ExecuteCohesionLink()
	{
		if (_cohesionConceptObj != null)
		{
			Campaign.Current.EncyclopediaManager.GoToLink(_cohesionConceptObj.EncyclopediaLink);
		}
		else
		{
			Debug.FailedAssert("Couldn't find Cohesion encyclopedia page", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\GameMenu\\Overlay\\ArmyMenuOverlayVM.cs", "ExecuteCohesionLink", 257);
		}
	}

	private void OnTutorialNotificationElementIDChange(TutorialNotificationElementChangeEvent obj)
	{
		if (obj.NewNotificationElementID != _latestTutorialElementID)
		{
			if (_latestTutorialElementID != null)
			{
				TutorialNotification.ElementID = string.Empty;
			}
			_latestTutorialElementID = obj.NewNotificationElementID;
			if (_latestTutorialElementID != null)
			{
				TutorialNotification.ElementID = _latestTutorialElementID;
			}
		}
	}

	private void RefreshVisualsOfItems()
	{
		for (int i = 0; i < PartyList.Count; i++)
		{
			PartyList[i].RefreshVisual();
		}
	}

	private void OnPartyAttachedAnotherParty(MobileParty party)
	{
		if (party.AttachedTo?.Army != null && party.AttachedTo.Army == MobileParty.MainParty.Army)
		{
			_isVisualsDirty = true;
		}
	}

	private void OnTroopRecruited(Hero recruiterHero, Settlement settlement, Hero troopSource, CharacterObject troop, int number)
	{
		if (recruiterHero?.PartyBelongedTo == null || !recruiterHero.IsPartyLeader)
		{
			return;
		}
		for (int i = 0; i < PartyList.Count; i++)
		{
			if (PartyList[i].Party == recruiterHero.PartyBelongedTo.Party)
			{
				PartyList[i].RefreshProperties();
				break;
			}
		}
	}

	private static bool GetIsPlayerArmyLeader(Army army)
	{
		if (army.LeaderParty != MobileParty.MainParty)
		{
			return army.LeaderParty == MobileParty.MainParty.TargetParty;
		}
		return true;
	}
}
