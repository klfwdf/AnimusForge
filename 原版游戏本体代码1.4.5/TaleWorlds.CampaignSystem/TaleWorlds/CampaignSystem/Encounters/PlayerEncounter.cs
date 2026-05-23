using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem.Encounters;

public class PlayerEncounter
{
	[SaveableField(1)]
	public bool FirstInit = true;

	[SaveableField(7)]
	public float PlayerPartyInitialStrength;

	[SaveableField(8)]
	private CampaignBattleResult _campaignBattleResult;

	[SaveableField(9)]
	public float PartiesStrengthRatioBeforePlayerJoin;

	[SaveableField(10)]
	public bool ForceRaid;

	[SaveableField(11)]
	public bool ForceSallyOut;

	[SaveableField(40)]
	public bool ForceHideoutSendTroops;

	[SaveableField(32)]
	public bool ForceVolunteers;

	[SaveableField(33)]
	public bool ForceSupplies;

	[SaveableField(34)]
	private bool _isSiegeInterruptedByEnemyDefection;

	public BattleSimulation BattleSimulation;

	[SaveableField(13)]
	private MapEvent _mapEvent;

	[SaveableField(14)]
	private PlayerEncounterState _mapEventState;

	[SaveableField(15)]
	private PartyBase _encounteredParty;

	[SaveableField(16)]
	private PartyBase _attackerParty;

	[SaveableField(17)]
	private PartyBase _defenderParty;

	[SaveableField(18)]
	private List<Hero> _helpedHeroes;

	[SaveableField(19)]
	private List<TroopRosterElement> _capturedHeroes;

	[SaveableField(20)]
	private List<TroopRosterElement> _capturedAlreadyPrisonerHeroes;

	[SaveableField(22)]
	private bool _leaveEncounter;

	[SaveableField(23)]
	private bool _playerSurrender;

	[SaveableField(24)]
	private bool _enemySurrender;

	[SaveableField(25)]
	private bool _battleChallenge;

	[SaveableField(26)]
	private bool _meetingDone;

	[SaveableField(27)]
	private bool _stateHandled;

	[SaveableField(36)]
	private ItemRoster _alternativeRosterToReceiveLootItems;

	public Figurehead PlayerLootedFigurehead;

	[SaveableField(37)]
	private TroopRoster _alternativeRosterToReceiveLootPrisoners;

	[SaveableField(38)]
	private TroopRoster _alternativeRosterToReceiveLootMembers;

	[SaveableField(53)]
	private List<Ship> _alternativeReceivedLootShips = new List<Ship>();

	[SaveableField(51)]
	private bool _doesBattleContinue;

	[SaveableField(52)]
	private bool _isSallyOutAmbush;

	[SaveableField(54)]
	public bool ForceBlockadeAttack;

	[SaveableField(55)]
	public bool ForceBlockadeSallyOutAttack;

	public static PlayerEncounter Current => Campaign.Current.PlayerEncounter;

	public static LocationEncounter LocationEncounter
	{
		get
		{
			return Campaign.Current.LocationEncounter;
		}
		set
		{
			Campaign.Current.LocationEncounter = value;
		}
	}

	public static MapEvent Battle
	{
		get
		{
			if (Current == null)
			{
				return null;
			}
			return Current._mapEvent;
		}
	}

	public static PartyBase EncounteredParty
	{
		get
		{
			if (Current != null)
			{
				return Current._encounteredParty;
			}
			return null;
		}
	}

	public static MobileParty EncounteredMobileParty => EncounteredParty?.MobileParty;

	public static MapEvent EncounteredBattle
	{
		get
		{
			if (Current._encounteredParty.MapEvent != null)
			{
				return Current._encounteredParty.MapEvent;
			}
			if (Current._encounteredParty.IsSettlement && Current._encounteredParty.SiegeEvent?.BesiegerCamp.LeaderParty.MapEvent != null)
			{
				return Current._encounteredParty.SiegeEvent.BesiegerCamp.LeaderParty.MapEvent;
			}
			return null;
		}
	}

	public static BattleState BattleState => Current._mapEvent.BattleState;

	public static BattleSideEnum WinningSide => Current._mapEvent.WinningSide;

	public static bool BattleChallenge
	{
		get
		{
			return Current._battleChallenge;
		}
		set
		{
			Current._battleChallenge = value;
		}
	}

	public static bool PlayerIsDefender => Current.PlayerSide == BattleSideEnum.Defender;

	public static bool PlayerIsAttacker => Current.PlayerSide == BattleSideEnum.Attacker;

	public static bool LeaveEncounter
	{
		get
		{
			return Current._leaveEncounter;
		}
		set
		{
			Current._leaveEncounter = value;
		}
	}

	public static bool MeetingDone => Current._meetingDone;

	public static bool PlayerSurrender
	{
		get
		{
			return Current._playerSurrender;
		}
		set
		{
			if (value)
			{
				Current.PlayerSurrenderInternal();
			}
		}
	}

	public static bool EnemySurrender
	{
		get
		{
			return Current._enemySurrender;
		}
		set
		{
			if (value)
			{
				Current.EnemySurrenderInternal();
			}
		}
	}

	public static bool IsActive => Current != null;

	[SaveableProperty(2)]
	public BattleSideEnum OpponentSide { get; private set; }

	[SaveableProperty(3)]
	public BattleSideEnum PlayerSide { get; private set; }

	[SaveableProperty(6)]
	public bool IsJoinedBattle { get; private set; }

	public static bool InsideSettlement
	{
		get
		{
			if (MobileParty.MainParty.IsActive)
			{
				return MobileParty.MainParty.CurrentSettlement != null;
			}
			return false;
		}
	}

	public static CampaignBattleResult CampaignBattleResult
	{
		get
		{
			return Current._campaignBattleResult;
		}
		set
		{
			Current._campaignBattleResult = value;
		}
	}

	public static BattleSimulation CurrentBattleSimulation
	{
		get
		{
			if (Current == null)
			{
				return null;
			}
			return Current.BattleSimulation;
		}
	}

	public PlayerEncounterState EncounterState
	{
		get
		{
			return _mapEventState;
		}
		private set
		{
			_mapEventState = value;
		}
	}

	[SaveableProperty(66)]
	public bool IsPlayerEncounterRestartedForRaid { get; private set; }

	public ItemRoster RosterToReceiveLootItems
	{
		get
		{
			if (_alternativeRosterToReceiveLootItems == null)
			{
				_alternativeRosterToReceiveLootItems = new ItemRoster();
			}
			return _alternativeRosterToReceiveLootItems;
		}
	}

	public TroopRoster RosterToReceiveLootPrisoners
	{
		get
		{
			if (_alternativeRosterToReceiveLootPrisoners == null)
			{
				_alternativeRosterToReceiveLootPrisoners = TroopRoster.CreateDummyTroopRoster();
			}
			return _alternativeRosterToReceiveLootPrisoners;
		}
	}

	public TroopRoster RosterToReceiveLootMembers
	{
		get
		{
			if (_alternativeRosterToReceiveLootMembers == null)
			{
				_alternativeRosterToReceiveLootMembers = TroopRoster.CreateDummyTroopRoster();
			}
			return _alternativeRosterToReceiveLootMembers;
		}
	}

	public List<Ship> ReceivedLootShips => _alternativeReceivedLootShips;

	public static Settlement EncounterSettlement => Current?.EncounterSettlementAux;

	[SaveableProperty(28)]
	public Settlement EncounterSettlementAux { get; private set; }

	[SaveableProperty(50)]
	public bool IsPlayerWaiting { get; set; }

	[SaveableProperty(56)]
	public bool InterruptedWhileWaiting { get; set; }

	[SaveableProperty(57)]
	public bool InterruptedWhileLooting { get; set; }

	public bool IsNavalEncounterFinishedWithDisengage { get; private set; }

	public List<Ship> CapturedShipsInEncounter { get; private set; } = new List<Ship>();

	internal static void AutoGeneratedStaticCollectObjectsPlayerEncounter(object o, List<object> collectedObjects)
	{
		((PlayerEncounter)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
	}

	protected virtual void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
	{
		collectedObjects.Add(_campaignBattleResult);
		collectedObjects.Add(_mapEvent);
		collectedObjects.Add(_encounteredParty);
		collectedObjects.Add(_attackerParty);
		collectedObjects.Add(_defenderParty);
		collectedObjects.Add(_helpedHeroes);
		collectedObjects.Add(_capturedHeroes);
		collectedObjects.Add(_capturedAlreadyPrisonerHeroes);
		collectedObjects.Add(_alternativeRosterToReceiveLootItems);
		collectedObjects.Add(_alternativeRosterToReceiveLootPrisoners);
		collectedObjects.Add(_alternativeRosterToReceiveLootMembers);
		collectedObjects.Add(_alternativeReceivedLootShips);
		collectedObjects.Add(EncounterSettlementAux);
	}

	internal static object AutoGeneratedGetMemberValueOpponentSide(object o)
	{
		return ((PlayerEncounter)o).OpponentSide;
	}

	internal static object AutoGeneratedGetMemberValuePlayerSide(object o)
	{
		return ((PlayerEncounter)o).PlayerSide;
	}

	internal static object AutoGeneratedGetMemberValueIsJoinedBattle(object o)
	{
		return ((PlayerEncounter)o).IsJoinedBattle;
	}

	internal static object AutoGeneratedGetMemberValueIsPlayerEncounterRestartedForRaid(object o)
	{
		return ((PlayerEncounter)o).IsPlayerEncounterRestartedForRaid;
	}

	internal static object AutoGeneratedGetMemberValueEncounterSettlementAux(object o)
	{
		return ((PlayerEncounter)o).EncounterSettlementAux;
	}

	internal static object AutoGeneratedGetMemberValueIsPlayerWaiting(object o)
	{
		return ((PlayerEncounter)o).IsPlayerWaiting;
	}

	internal static object AutoGeneratedGetMemberValueInterruptedWhileWaiting(object o)
	{
		return ((PlayerEncounter)o).InterruptedWhileWaiting;
	}

	internal static object AutoGeneratedGetMemberValueInterruptedWhileLooting(object o)
	{
		return ((PlayerEncounter)o).InterruptedWhileLooting;
	}

	internal static object AutoGeneratedGetMemberValueFirstInit(object o)
	{
		return ((PlayerEncounter)o).FirstInit;
	}

	internal static object AutoGeneratedGetMemberValuePlayerPartyInitialStrength(object o)
	{
		return ((PlayerEncounter)o).PlayerPartyInitialStrength;
	}

	internal static object AutoGeneratedGetMemberValuePartiesStrengthRatioBeforePlayerJoin(object o)
	{
		return ((PlayerEncounter)o).PartiesStrengthRatioBeforePlayerJoin;
	}

	internal static object AutoGeneratedGetMemberValueForceRaid(object o)
	{
		return ((PlayerEncounter)o).ForceRaid;
	}

	internal static object AutoGeneratedGetMemberValueForceSallyOut(object o)
	{
		return ((PlayerEncounter)o).ForceSallyOut;
	}

	internal static object AutoGeneratedGetMemberValueForceHideoutSendTroops(object o)
	{
		return ((PlayerEncounter)o).ForceHideoutSendTroops;
	}

	internal static object AutoGeneratedGetMemberValueForceVolunteers(object o)
	{
		return ((PlayerEncounter)o).ForceVolunteers;
	}

	internal static object AutoGeneratedGetMemberValueForceSupplies(object o)
	{
		return ((PlayerEncounter)o).ForceSupplies;
	}

	internal static object AutoGeneratedGetMemberValueForceBlockadeAttack(object o)
	{
		return ((PlayerEncounter)o).ForceBlockadeAttack;
	}

	internal static object AutoGeneratedGetMemberValueForceBlockadeSallyOutAttack(object o)
	{
		return ((PlayerEncounter)o).ForceBlockadeSallyOutAttack;
	}

	internal static object AutoGeneratedGetMemberValue_campaignBattleResult(object o)
	{
		return ((PlayerEncounter)o)._campaignBattleResult;
	}

	internal static object AutoGeneratedGetMemberValue_isSiegeInterruptedByEnemyDefection(object o)
	{
		return ((PlayerEncounter)o)._isSiegeInterruptedByEnemyDefection;
	}

	internal static object AutoGeneratedGetMemberValue_mapEvent(object o)
	{
		return ((PlayerEncounter)o)._mapEvent;
	}

	internal static object AutoGeneratedGetMemberValue_mapEventState(object o)
	{
		return ((PlayerEncounter)o)._mapEventState;
	}

	internal static object AutoGeneratedGetMemberValue_encounteredParty(object o)
	{
		return ((PlayerEncounter)o)._encounteredParty;
	}

	internal static object AutoGeneratedGetMemberValue_attackerParty(object o)
	{
		return ((PlayerEncounter)o)._attackerParty;
	}

	internal static object AutoGeneratedGetMemberValue_defenderParty(object o)
	{
		return ((PlayerEncounter)o)._defenderParty;
	}

	internal static object AutoGeneratedGetMemberValue_helpedHeroes(object o)
	{
		return ((PlayerEncounter)o)._helpedHeroes;
	}

	internal static object AutoGeneratedGetMemberValue_capturedHeroes(object o)
	{
		return ((PlayerEncounter)o)._capturedHeroes;
	}

	internal static object AutoGeneratedGetMemberValue_capturedAlreadyPrisonerHeroes(object o)
	{
		return ((PlayerEncounter)o)._capturedAlreadyPrisonerHeroes;
	}

	internal static object AutoGeneratedGetMemberValue_leaveEncounter(object o)
	{
		return ((PlayerEncounter)o)._leaveEncounter;
	}

	internal static object AutoGeneratedGetMemberValue_playerSurrender(object o)
	{
		return ((PlayerEncounter)o)._playerSurrender;
	}

	internal static object AutoGeneratedGetMemberValue_enemySurrender(object o)
	{
		return ((PlayerEncounter)o)._enemySurrender;
	}

	internal static object AutoGeneratedGetMemberValue_battleChallenge(object o)
	{
		return ((PlayerEncounter)o)._battleChallenge;
	}

	internal static object AutoGeneratedGetMemberValue_meetingDone(object o)
	{
		return ((PlayerEncounter)o)._meetingDone;
	}

	internal static object AutoGeneratedGetMemberValue_stateHandled(object o)
	{
		return ((PlayerEncounter)o)._stateHandled;
	}

	internal static object AutoGeneratedGetMemberValue_alternativeRosterToReceiveLootItems(object o)
	{
		return ((PlayerEncounter)o)._alternativeRosterToReceiveLootItems;
	}

	internal static object AutoGeneratedGetMemberValue_alternativeRosterToReceiveLootPrisoners(object o)
	{
		return ((PlayerEncounter)o)._alternativeRosterToReceiveLootPrisoners;
	}

	internal static object AutoGeneratedGetMemberValue_alternativeRosterToReceiveLootMembers(object o)
	{
		return ((PlayerEncounter)o)._alternativeRosterToReceiveLootMembers;
	}

	internal static object AutoGeneratedGetMemberValue_alternativeReceivedLootShips(object o)
	{
		return ((PlayerEncounter)o)._alternativeReceivedLootShips;
	}

	internal static object AutoGeneratedGetMemberValue_doesBattleContinue(object o)
	{
		return ((PlayerEncounter)o)._doesBattleContinue;
	}

	internal static object AutoGeneratedGetMemberValue_isSallyOutAmbush(object o)
	{
		return ((PlayerEncounter)o)._isSallyOutAmbush;
	}

	[LoadInitializationCallback]
	private void OnLoadInitialization(MetaData meta)
	{
		if (MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion.IsOlderThan(ApplicationVersion.FromString("v1.3.0")))
		{
			_alternativeReceivedLootShips = new List<Ship>();
		}
	}

	private PlayerEncounter()
	{
	}

	public void OnLoad()
	{
		if (InsideSettlement && Battle == null)
		{
			CreateLocationEncounter(MobileParty.MainParty.CurrentSettlement);
		}
		else if (Current != null && EncounterSettlement != null && EncounterSettlement.IsVillage && Current.IsPlayerWaiting)
		{
			CreateLocationEncounter(EncounterSettlementAux);
		}
		if (MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion.IsOlderThan(ApplicationVersion.FromString("v1.3.13.106222")) && _mapEvent != null && MobileParty.MainParty.MapEvent == null && (_mapEvent.EventType == MapEvent.BattleTypes.BlockadeSallyOutBattle || _mapEvent.EventType == MapEvent.BattleTypes.BlockadeBattle))
		{
			Current.FinalizeBattle();
			Current.SetupFields(MobileParty.MainParty.Party, PlayerSiege.PlayerSiegeEvent.BesiegedSettlement.Party);
		}
		CapturedShipsInEncounter = new List<Ship>();
	}

	public static void RestartPlayerEncounter(PartyBase defenderParty, PartyBase attackerParty, bool forcePlayerOutFromSettlement = true, bool isPlayerEncounterRestartedForRaid = false)
	{
		if (Current != null)
		{
			Finish(forcePlayerOutFromSettlement);
		}
		Start();
		Current.SetupFields(attackerParty, defenderParty);
		Current.IsPlayerEncounterRestartedForRaid = isPlayerEncounterRestartedForRaid || Current.IsPlayerEncounterRestartedForRaid;
	}

	internal void Init(PartyBase attackerParty, PartyBase defenderParty, Settlement settlement = null)
	{
		IsNavalEncounterFinishedWithDisengage = false;
		InterruptedWhileLooting = false;
		EncounterSettlementAux = ((settlement != null) ? settlement : (defenderParty.IsSettlement ? defenderParty.Settlement : attackerParty.Settlement));
		EnemySurrender = false;
		PlayerPartyInitialStrength = MobileParty.MainParty.Party.CalculateCurrentStrength();
		SetupFields(attackerParty, defenderParty);
		if (defenderParty.MapEvent != null && attackerParty != MobileParty.MainParty.Party && defenderParty != MobileParty.MainParty.Party)
		{
			_mapEvent = defenderParty.MapEvent;
			if (_mapEvent.CanPartyJoinBattle(PartyBase.MainParty, BattleSideEnum.Defender))
			{
				MobileParty.MainParty.Party.MapEventSide = _mapEvent.DefenderSide;
			}
			else if (_mapEvent.CanPartyJoinBattle(PartyBase.MainParty, BattleSideEnum.Attacker))
			{
				MobileParty.MainParty.Party.MapEventSide = _mapEvent.AttackerSide;
			}
		}
		bool joinBattle = false;
		bool startBattle = false;
		string encounterMenu = Campaign.Current.Models.EncounterGameMenuModel.GetEncounterMenu(attackerParty, defenderParty, out startBattle, out joinBattle);
		if (!string.IsNullOrEmpty(encounterMenu))
		{
			if (startBattle)
			{
				StartBattle();
				if (MobileParty.MainParty.MapEvent == null)
				{
					encounterMenu = Campaign.Current.Models.EncounterGameMenuModel.GetEncounterMenu(attackerParty, defenderParty, out startBattle, out joinBattle);
				}
			}
			if (joinBattle)
			{
				if (MobileParty.MainParty.MapEvent == null)
				{
					if (defenderParty.MapEvent != null)
					{
						if (defenderParty.MapEvent.CanPartyJoinBattle(PartyBase.MainParty, BattleSideEnum.Attacker))
						{
							JoinBattle(BattleSideEnum.Attacker);
						}
						else if (defenderParty.MapEvent.CanPartyJoinBattle(PartyBase.MainParty, BattleSideEnum.Defender))
						{
							JoinBattle(BattleSideEnum.Defender);
						}
						else
						{
							Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Encounters\\PlayerEncounter.cs", "Init", 532);
						}
					}
					else
					{
						Debug.FailedAssert("If there is no map event we should create one in order to join battle", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Encounters\\PlayerEncounter.cs", "Init", 537);
					}
				}
				CheckNearbyPartiesToJoinPlayerMapEvent();
			}
			if (attackerParty == PartyBase.MainParty && defenderParty.IsSettlement && !defenderParty.Settlement.IsUnderRaid && !defenderParty.Settlement.IsUnderSiege)
			{
				EnterSettlement();
			}
			GameMenu.ActivateGameMenu(encounterMenu);
		}
		else if (attackerParty == PartyBase.MainParty && defenderParty.IsSettlement && !defenderParty.Settlement.IsUnderRaid && !defenderParty.Settlement.IsUnderSiege)
		{
			EnterSettlement();
		}
		ForceSallyOut = false;
		ForceBlockadeSallyOutAttack = false;
		ForceRaid = false;
		ForceSupplies = false;
		ForceVolunteers = false;
		_isSallyOutAmbush = false;
	}

	public static void Init()
	{
		if (Current == null)
		{
			Start();
		}
		Current.InitAux();
	}

	private void InitAux()
	{
		if (MobileParty.MainParty.MapEvent != null)
		{
			_mapEvent = MobileParty.MainParty.MapEvent;
			SetupFields(_mapEvent.AttackerSide.LeaderParty, _mapEvent.DefenderSide.LeaderParty);
			CheckNearbyPartiesToJoinPlayerMapEvent();
		}
	}

	public void SetupFields(PartyBase attackerParty, PartyBase defenderParty)
	{
		_attackerParty = attackerParty;
		_defenderParty = defenderParty;
		MobileParty mobileParty = ((defenderParty.IsMobile && defenderParty != PartyBase.MainParty && defenderParty.MobileParty != MobileParty.MainParty.AttachedTo) ? defenderParty.MobileParty : ((attackerParty.IsMobile && attackerParty != PartyBase.MainParty && attackerParty.MobileParty != MobileParty.MainParty.AttachedTo) ? attackerParty.MobileParty : null));
		if (_defenderParty.IsSettlement)
		{
			EncounterSettlementAux = defenderParty.Settlement;
		}
		else if (_attackerParty.IsSettlement)
		{
			EncounterSettlementAux = _attackerParty.Settlement;
		}
		else if (mobileParty.BesiegerCamp != null)
		{
			EncounterSettlementAux = mobileParty.BesiegerCamp.SiegeEvent.BesiegedSettlement;
		}
		_encounteredParty = ((mobileParty != null) ? mobileParty.Party : EncounterSettlementAux?.Party);
		if (MapEvent.PlayerMapEvent != null)
		{
			PlayerSide = MapEvent.PlayerMapEvent.PlayerSide;
		}
		else if (defenderParty == PartyBase.MainParty || (defenderParty.MobileParty != null && defenderParty.MobileParty == MobileParty.MainParty.AttachedTo) || (defenderParty.IsSettlement && (defenderParty.Settlement.MapFaction == MobileParty.MainParty.MapFaction || MobileParty.MainParty.CurrentSettlement == defenderParty.Settlement)))
		{
			PlayerSide = BattleSideEnum.Defender;
		}
		else
		{
			PlayerSide = BattleSideEnum.Attacker;
		}
		OpponentSide = PlayerSide.GetOppositeSide();
	}

	internal void OnPartyJoinEncounter(MobileParty newParty)
	{
		if (Battle == null)
		{
			return;
		}
		if (Battle.CanPartyJoinBattle(newParty.Party, PartyBase.MainParty.Side))
		{
			newParty.Party.MapEventSide = PartyBase.MainParty.MapEventSide;
		}
		else if (newParty != MobileParty.MainParty || !Battle.IsRaid || Battle.AttackerSide.LeaderParty == MobileParty.MainParty.Party || Battle.DefenderSide.TroopCount != 0)
		{
			MobileParty.MainParty.SetMoveModeHold();
			string newPartyJoinMenu = Campaign.Current.Models.EncounterGameMenuModel.GetNewPartyJoinMenu(newParty);
			if (Battle.CanPartyJoinBattle(newParty.Party, PartyBase.MainParty.OpponentSide))
			{
				newParty.Party.MapEventSide = PartyBase.MainParty.MapEventSide.OtherSide;
			}
			if (!string.IsNullOrEmpty(newPartyJoinMenu))
			{
				GameMenu.SwitchToMenu(newPartyJoinMenu);
			}
		}
	}

	private void CheckNearbyPartiesToJoinPlayerMapEvent()
	{
		if (_mapEvent == null || _mapEvent.IsRaid || _mapEvent.IsSiegeAssault || _mapEvent.IsForcingSupplies || _mapEvent.IsForcingVolunteers || (_mapEvent.MapEventSettlement != null && _mapEvent.MapEventSettlement.IsHideout))
		{
			return;
		}
		List<MobileParty> list = new List<MobileParty>();
		List<MobileParty> list2 = new List<MobileParty>();
		foreach (MapEventParty item in _mapEvent.PartiesOnSide(PlayerSide))
		{
			if (item.Party.IsMobile)
			{
				list.Add(item.Party.MobileParty);
			}
		}
		foreach (MapEventParty item2 in _mapEvent.PartiesOnSide(PlayerSide.GetOppositeSide()))
		{
			if (item2.Party.IsMobile)
			{
				list2.Add(item2.Party.MobileParty);
			}
		}
		Current.FindNonAttachedNpcPartiesWhoWillJoinEvent(list, list2);
		foreach (MobileParty item3 in list)
		{
			_mapEvent.GetMapEventSide(PlayerSide).AddNearbyPartyToPlayerMapEvent(item3);
		}
		foreach (MobileParty item4 in list2)
		{
			_mapEvent.GetMapEventSide(PlayerSide.GetOppositeSide()).AddNearbyPartyToPlayerMapEvent(item4);
		}
	}

	public static bool IsNavalEncounter()
	{
		PlayerEncounter current = Current;
		if (current == null)
		{
			return false;
		}
		return current._mapEvent?.IsNavalMapEvent == true;
	}

	private MapEvent StartBattleInternal()
	{
		if (_mapEvent == null)
		{
			if (ForceRaid)
			{
				_mapEvent = RaidEventComponent.CreateRaidEvent(_attackerParty, _defenderParty).MapEvent;
			}
			else if (ForceSallyOut)
			{
				_mapEvent = Campaign.Current.MapEventManager.StartSallyOutMapEvent(_attackerParty, _defenderParty);
			}
			else if (ForceVolunteers)
			{
				_mapEvent = ForceVolunteersEventComponent.CreateForceSuppliesEvent(_attackerParty, _defenderParty).MapEvent;
			}
			else if (ForceSupplies)
			{
				_mapEvent = ForceSuppliesEventComponent.CreateForceSuppliesEvent(_attackerParty, _defenderParty).MapEvent;
			}
			else if (_defenderParty.IsSettlement)
			{
				if (_defenderParty.Settlement.IsFortification)
				{
					_mapEvent = Campaign.Current.MapEventManager.StartSiegeMapEvent(_attackerParty, _defenderParty);
				}
				else if (_defenderParty.Settlement.IsVillage)
				{
					_mapEvent = RaidEventComponent.CreateRaidEvent(_attackerParty, _defenderParty).MapEvent;
				}
				else if (_defenderParty.Settlement.IsHideout)
				{
					_mapEvent = HideoutEventComponent.CreateHideoutEvent(_attackerParty, _defenderParty, ForceHideoutSendTroops).MapEvent;
				}
				else
				{
					Debug.FailedAssert("Proper mapEvent type could not be set for the battle.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Encounters\\PlayerEncounter.cs", "StartBattleInternal", 768);
				}
			}
			else if (_isSallyOutAmbush)
			{
				_mapEvent = SiegeAmbushEventComponent.CreateSiegeAmbushEvent(_attackerParty, _defenderParty).MapEvent;
			}
			else if (ForceBlockadeAttack)
			{
				_mapEvent = BlockadeBattleMapEvent.CreateBlockadeBattleMapEvent(_attackerParty, _defenderParty, isSallyOut: false).MapEvent;
			}
			else if (ForceBlockadeSallyOutAttack)
			{
				_mapEvent = BlockadeBattleMapEvent.CreateBlockadeBattleMapEvent(_attackerParty, _defenderParty, isSallyOut: true).MapEvent;
			}
			else if (_attackerParty.IsMobile && _attackerParty.MobileParty.CurrentSettlement != null && _attackerParty.MobileParty.CurrentSettlement.SiegeEvent != null)
			{
				if (_attackerParty.MobileParty.IsTargetingPort)
				{
					_mapEvent = BlockadeBattleMapEvent.CreateBlockadeBattleMapEvent(_attackerParty, _defenderParty, isSallyOut: true).MapEvent;
				}
				else
				{
					_mapEvent = Campaign.Current.MapEventManager.StartSallyOutMapEvent(_attackerParty, _defenderParty);
				}
			}
			else if (_defenderParty.IsMobile && _defenderParty.MobileParty.BesiegedSettlement != null)
			{
				_mapEvent = Campaign.Current.MapEventManager.StartSiegeOutsideMapEvent(_attackerParty, _defenderParty);
			}
			else
			{
				_mapEvent = FieldBattleEventComponent.CreateFieldBattleEvent(_attackerParty, _defenderParty).MapEvent;
			}
		}
		if (!_mapEvent.IsFinalized)
		{
			CheckNearbyPartiesToJoinPlayerMapEvent();
		}
		return _mapEvent;
	}

	public static MapEvent StartBattle()
	{
		return Current.StartBattleInternal();
	}

	private void JoinBattleInternal(BattleSideEnum side)
	{
		PlayerSide = side;
		switch (side)
		{
		case BattleSideEnum.Defender:
			OpponentSide = BattleSideEnum.Attacker;
			break;
		case BattleSideEnum.Attacker:
			OpponentSide = BattleSideEnum.Defender;
			break;
		}
		if (EncounteredBattle != null)
		{
			_mapEvent = EncounteredBattle;
			_encounteredParty = ((PlayerSide == BattleSideEnum.Attacker) ? EncounteredBattle.DefenderSide.LeaderParty : EncounteredBattle.AttackerSide.LeaderParty);
			PartiesStrengthRatioBeforePlayerJoin = CalculateStrengthOfParties();
			PartyBase.MainParty.MapEventSide = EncounteredBattle.GetMapEventSide(side);
			EncounterSettlementAux = _mapEvent.MapEventSettlement;
			if (EncounteredBattle.IsSiegeAssault && PlayerSide == BattleSideEnum.Attacker)
			{
				MobileParty.MainParty.BesiegerCamp = _encounteredParty.SiegeEvent.BesiegerCamp;
			}
			IsJoinedBattle = true;
			CheckNearbyPartiesToJoinPlayerMapEvent();
		}
		else
		{
			Finish(InsideSettlement);
		}
	}

	private float CalculateStrengthOfParties()
	{
		MapEvent.PowerCalculationContext contextForPosition = Campaign.Current.Models.MilitaryPowerModel.GetContextForPosition(_mapEvent.Position);
		float num = 0f;
		float num2 = 0f;
		foreach (MapEventParty party in _mapEvent.DefenderSide.Parties)
		{
			BattleSideEnum side = BattleSideEnum.Defender;
			num += party.Party.GetCustomStrength(side, contextForPosition);
		}
		foreach (MapEventParty party2 in _mapEvent.AttackerSide.Parties)
		{
			BattleSideEnum side2 = BattleSideEnum.Attacker;
			num2 += party2.Party.GetCustomStrength(side2, contextForPosition);
		}
		return num / num2;
	}

	public static void JoinBattle(BattleSideEnum side)
	{
		Current.JoinBattleInternal(side);
	}

	private void PlayerSurrenderInternal()
	{
		_playerSurrender = true;
		if (Battle == null)
		{
			StartBattle();
		}
		_mapEvent.DoSurrender(PartyBase.MainParty.Side);
		MobileParty.MainParty.BesiegerCamp = null;
	}

	private void EnemySurrenderInternal()
	{
		_enemySurrender = true;
		_mapEvent.DoSurrender(PartyBase.MainParty.OpponentSide);
	}

	public static void Start()
	{
		Campaign.Current.PlayerEncounter = new PlayerEncounter();
	}

	public static void ProtectPlayerSide(float hoursToProtect = 1f)
	{
		MobileParty.MainParty.TeleportPartyToOutSideOfEncounterRadius();
		MobileParty.MainParty.IgnoreForHours(hoursToProtect);
	}

	public static void Finish(bool forcePlayerOutFromSettlement = true)
	{
		if (MobileParty.MainParty.Army == null || MobileParty.MainParty.Army.LeaderParty == EncounteredMobileParty)
		{
			Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
		}
		if (Campaign.Current.CurrentMenuContext != null)
		{
			GameMenu.ExitToLast();
		}
		else
		{
			Campaign.Current.MapStateData.GameMenuId = null;
		}
		int num;
		if (Current != null)
		{
			if (PlayerSiege.PlayerSiegeEvent != null && PlayerSiege.PlayerSide == BattleSideEnum.Attacker && MobileParty.MainParty.MapEvent != null && !MobileParty.MainParty.MapEvent.IsSiegeAssault && MobileParty.MainParty.MapEvent.HasWinner && MobileParty.MainParty.MapEvent.PlayerSide == BattleSideEnum.Defender && MobileParty.MainParty.BesiegedSettlement != null)
			{
				num = (PlayerSiege.PlayerSiegeEvent.BesiegedSettlement.GetInvolvedPartiesForEventType(MobileParty.MainParty.MapEvent.EventType).Any((PartyBase x) => x.NumberOfHealthyMembers > 0) ? 1 : 0);
				if (num != 0)
				{
					goto IL_0111;
				}
			}
			else
			{
				num = 0;
			}
			if (Current._isSiegeInterruptedByEnemyDefection)
			{
				goto IL_0111;
			}
			goto IL_016a;
		}
		goto IL_023f;
		IL_0111:
		if (Hero.MainHero.PartyBelongedToAsPrisoner == null && !Current._leaveEncounter && Current._encounteredParty.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
		{
			GameMenu.ActivateGameMenu("continue_siege_after_attack");
			if (Current._isSiegeInterruptedByEnemyDefection)
			{
				Current._isSiegeInterruptedByEnemyDefection = false;
			}
		}
		goto IL_016a;
		IL_023f:
		Campaign.Current.PlayerEncounter = null;
		Campaign.Current.LocationEncounter = null;
		return;
		IL_016a:
		if ((num != 0 || Current._isSiegeInterruptedByEnemyDefection) && Hero.MainHero.PartyBelongedToAsPrisoner != null && Current._leaveEncounter)
		{
			MobileParty.MainParty.BesiegerCamp = null;
		}
		Current.FirstInit = true;
		bool playerIsWinner = Current._mapEvent?.IsWinnerSide(PartyBase.MainParty.Side) ?? false;
		EncounterSettlement?.OnPlayerEncounterFinish();
		Current.FinalizeBattle();
		Current.FinishEncounterInternal(playerIsWinner);
		if (CurrentBattleSimulation != null)
		{
			MapState mapState = Game.Current.GameStateManager.LastOrDefault<MapState>();
			if (mapState != null && mapState.IsSimulationActive)
			{
				mapState.EndBattleSimulation();
			}
			Current.BattleSimulation = null;
		}
		if (InsideSettlement && MobileParty.MainParty.AttachedTo == null && forcePlayerOutFromSettlement)
		{
			LeaveSettlement();
		}
		goto IL_023f;
	}

	private void FinishEncounterInternal(bool playerIsWinner)
	{
		if (!playerIsWinner && _encounteredParty != null && _encounteredParty.IsMobile && MobileParty.MainParty.AttachedTo == null && MobileParty.MainParty.IsActive && !LeaveEncounter && FactionManager.IsAtWarAgainstFaction(_encounteredParty.MapFaction, PartyBase.MainParty.MapFaction) && _encounteredParty.MobileParty.IsActive)
		{
			MobileParty.MainParty.TeleportPartyToOutSideOfEncounterRadius();
			_encounteredParty.MobileParty.Ai.SetDoNotAttackMainParty(2);
		}
	}

	private void UpdateInternal()
	{
		_mapEvent = MapEvent.PlayerMapEvent;
		if (EnemySurrender && EncounterState == PlayerEncounterState.Begin)
		{
			EncounterState = PlayerEncounterState.Wait;
		}
		_stateHandled = false;
		while (!_stateHandled)
		{
			if (Current._leaveEncounter)
			{
				Finish();
				_stateHandled = true;
			}
			if (!_stateHandled)
			{
				switch (EncounterState)
				{
				case PlayerEncounterState.Begin:
					DoBegin();
					break;
				case PlayerEncounterState.Wait:
					DoWait();
					break;
				case PlayerEncounterState.PrepareResults:
					DoPrepareResults();
					break;
				case PlayerEncounterState.ApplyResults:
					DoApplyMapEventResults();
					break;
				case PlayerEncounterState.PlayerVictory:
					DoPlayerVictory();
					break;
				case PlayerEncounterState.PlayerTotalDefeat:
					DoPlayerDefeat();
					break;
				case PlayerEncounterState.CaptureHeroes:
					DoCaptureHeroes();
					break;
				case PlayerEncounterState.FreeHeroes:
					DoFreeOrCapturePrisonerHeroes();
					break;
				case PlayerEncounterState.LootParty:
					DoLootMembersAndPrisonersOfParty();
					break;
				case PlayerEncounterState.LootInventory:
					DoLootInventory();
					break;
				case PlayerEncounterState.LootShips:
					DoLootShips();
					break;
				case PlayerEncounterState.End:
					DoEnd();
					break;
				default:
					Debug.FailedAssert("[DEBUG]Invalid map event state: " + _mapEventState, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Encounters\\PlayerEncounter.cs", "UpdateInternal", 1073);
					break;
				}
			}
		}
	}

	private void EndBattleByCheatInternal(bool playerWon)
	{
		if (!playerWon)
		{
			return;
		}
		foreach (MapEventParty item in _mapEvent.PartiesOnSide(OpponentSide))
		{
			for (int i = 0; i < item.Party.MemberRoster.Count; i++)
			{
				int elementNumber = item.Party.MemberRoster.GetElementNumber(i);
				int elementWoundedNumber = item.Party.MemberRoster.GetElementWoundedNumber(i);
				int maxValue = elementNumber - elementWoundedNumber;
				int num = elementWoundedNumber + MBRandom.RandomInt(maxValue);
				num = ((num <= 0 && elementNumber >= 0) ? 1 : num);
				item.Party.MemberRoster.SetElementNumber(i, num);
				item.Party.MemberRoster.SetElementWoundedNumber(i, num);
			}
		}
	}

	public static void EndBattleByCheat(bool playerWon)
	{
		Current.EndBattleByCheatInternal(playerWon);
	}

	public static void Update()
	{
		Current.UpdateInternal();
	}

	private void DoBegin()
	{
		EncounterState = PlayerEncounterState.Wait;
		_stateHandled = true;
	}

	public static void DoMeeting()
	{
		Current.DoMeetingInternal();
	}

	public static void SetMeetingDone()
	{
		Current._meetingDone = true;
	}

	public void SetMeetingFalseForCompanion()
	{
		Current._meetingDone = false;
	}

	private void DoMeetingInternal()
	{
		PartyBase partyBase = _encounteredParty;
		if (partyBase.IsSettlement)
		{
			foreach (MapEventParty party in MobileParty.MainParty.MapEvent.DefenderSide.Parties)
			{
				if (!party.Party.IsSettlement)
				{
					partyBase = party.Party;
					break;
				}
			}
		}
		EncounterState = PlayerEncounterState.Begin;
		_stateHandled = true;
		bool num = PlayerIsAttacker && _defenderParty.IsMobile && _defenderParty.MobileParty.Army != null && _defenderParty.MobileParty.Army.LeaderParty == _defenderParty.MobileParty && (_defenderParty.SiegeEvent != null || (!_defenderParty.MobileParty.MapFaction.IsAtWarWith(MobileParty.MainParty.MapFaction) && !_defenderParty.MobileParty.Army.LeaderParty.AttachedParties.Contains(MobileParty.MainParty)));
		bool flag = PlayerIsDefender && _defenderParty.IsMobile && _defenderParty.MobileParty.Army != null && _defenderParty.MobileParty.Army.LeaderParty.AttachedParties.Contains(MobileParty.MainParty);
		if (num)
		{
			GameMenu.SwitchToMenu("army_encounter");
			return;
		}
		if (flag)
		{
			GameMenu.SwitchToMenu("encounter");
			return;
		}
		Campaign.Current.CurrentConversationContext = ConversationContext.PartyEncounter;
		_meetingDone = true;
		CharacterObject conversationCharacterPartyLeader = ConversationHelper.GetConversationCharacterPartyLeader(partyBase);
		ConversationCharacterData playerCharacterData = new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty, noHorse: true);
		ConversationCharacterData conversationPartnerData = new ConversationCharacterData(conversationCharacterPartyLeader, partyBase, noHorse: true);
		if (partyBase.MobileParty.IsCurrentlyAtSea)
		{
			CampaignMission.OpenConversationMission(playerCharacterData, conversationPartnerData);
		}
		else
		{
			CampaignMapConversation.OpenConversation(playerCharacterData, conversationPartnerData);
		}
	}

	private void ContinueBattle()
	{
		Debug.Print("[PlayerEncounter.ContinueBattle Start]");
		MapEventSide mapEventSide = _mapEvent.GetMapEventSide(_mapEvent.PlayerSide);
		MapEventSide otherSide = mapEventSide.OtherSide;
		_mapEvent.RecalculateStrengthOfSides();
		if (_mapEvent.IsNavalMapEvent && !_mapEvent.IsRaid && !_mapEvent.IsForcingSupplies && !_mapEvent.IsForcingVolunteers && otherSide.Parties.Sum((MapEventParty x) => x.Ships.Count) == 0)
		{
			Debug.FailedAssert("This case should not be called anymore, make sure this is intended", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Encounters\\PlayerEncounter.cs", "ContinueBattle", 1215);
			Debug.Print("Player side wins according to the strength ratio.");
			_mapEvent?.SetOverrideWinner(_mapEvent.PlayerSide);
			EnemySurrender = true;
			EncounterState = PlayerEncounterState.PrepareResults;
		}
		else if (_mapEvent.IsNavalMapEvent && mapEventSide.Parties.Sum((MapEventParty x) => x.Ships.Count) == 0)
		{
			Debug.FailedAssert("This case should not be called anymore, make sure this is intended", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Encounters\\PlayerEncounter.cs", "ContinueBattle", 1224);
			Debug.Print("Other side wins according to the strength ratio.");
			_mapEvent?.SetOverrideWinner(otherSide.MissionSide);
			EncounterState = PlayerEncounterState.PrepareResults;
		}
		else
		{
			Debug.Print("Battle continues.");
			Debug.Print("Other side strength by party:");
			foreach (MapEventParty party in otherSide.Parties)
			{
				Debug.Print(string.Concat("party: ", party.Party.Id, ": ", party.Party.Name, ", strength: ", party.Party.CalculateCurrentStrength(), ", healthy count: ", party.Party.MemberRoster.TotalHealthyCount, ", wounded count: ", party.Party.MemberRoster.TotalWounded));
			}
			_mapEvent.ApplyGainedVariablesOnPlayerBattleContinues();
			_mapEvent.SetOverrideWinner(BattleSideEnum.None);
			if (_mapEvent.IsSiegeAssault && otherSide == _mapEvent.AttackerSide)
			{
				CampaignBattleResult campaignBattleResult = _campaignBattleResult;
				if (campaignBattleResult != null && campaignBattleResult.EnemyRetreated)
				{
					_mapEvent.FinishBattleAndKeepSiegeEvent();
					_mapEvent = null;
					GameMenu.ActivateGameMenu("menu_siege_strategies");
				}
			}
			_campaignBattleResult = null;
			_stateHandled = true;
		}
		Debug.Print("[PlayerEncounter.ContinueBattle End]");
	}

	private void DoWait()
	{
		SetEncounterMenuTexts();
		if (CheckIfBattleShouldContinueAfterBattleMission() && !IsNavalEncounterFinishedWithDisengage)
		{
			ContinueBattle();
			return;
		}
		if (_mapEvent != null && _mapEvent.IsSiegeAssault)
		{
			_mapEvent.CheckIfOneSideHasLost();
			_campaignBattleResult = CampaignBattleResult.GetResult(_mapEvent.BattleState);
		}
		if (_campaignBattleResult != null && _campaignBattleResult.BattleResolved)
		{
			if (_campaignBattleResult.PlayerVictory)
			{
				_mapEvent?.SetOverrideWinner(PartyBase.MainParty.Side);
			}
			else
			{
				bool flag = true;
				if (_mapEvent != null && _mapEvent.IsHideoutBattle)
				{
					_mapEvent.MapEventSettlement.Hideout.SetNextPossibleAttackTime(Campaign.Current.Models.HideoutModel.HideoutHiddenDuration);
					if (_mapEvent.GetMapEventSide(PlayerSide).RecalculateMemberCountOfSide() > 0)
					{
						flag = false;
					}
				}
				if (flag)
				{
					_mapEvent?.SetOverrideWinner(PartyBase.MainParty.OpponentSide);
				}
			}
			EncounterState = PlayerEncounterState.PrepareResults;
		}
		else if (BattleSimulation != null && (BattleState == BattleState.AttackerVictory || BattleState == BattleState.DefenderVictory))
		{
			if (_mapEvent.WinningSide == PlayerSide && Battle.RetreatingSide == BattleSideEnum.None)
			{
				EnemySurrender = true;
			}
			else
			{
				int totalManCount = MobileParty.MainParty.MemberRoster.TotalManCount;
				int totalWounded = MobileParty.MainParty.MemberRoster.TotalWounded;
				if (totalManCount - totalWounded == 0)
				{
					PlayerSurrender = true;
				}
			}
			EncounterState = PlayerEncounterState.PrepareResults;
		}
		else if (BattleSimulation != null && BattleSimulation.IsSimulationFinished && _mapEvent?.MapEventSettlement != null && BattleState == BattleState.None && _mapEvent.IsSiegeAssault && PlayerSiege.PlayerSiegeEvent != null)
		{
			_stateHandled = true;
			PlayerSiege.PlayerSiegeEvent.BreakSiegeEngine(PlayerSiege.PlayerSiegeEvent.GetSiegeEventSide(_mapEvent.PlayerSide), DefaultSiegeEngineTypes.Preparations);
		}
		else if (_mapEvent != null && (!_mapEvent.IsRaid || PlayerSurrender) && _mapEvent.HasWinner)
		{
			EncounterState = PlayerEncounterState.PrepareResults;
		}
		else if (_mapEvent != null && _mapEvent.IsNavalMapEvent && _mapEvent.IsFieldBattle && _mapEvent.WinningSide == BattleSideEnum.None && (_mapEvent.PartiesOnSide(_mapEvent.PlayerSide).AnyQ((MapEventParty x) => x.Party.Ships.Count == 0) || _mapEvent.PartiesOnSide(_mapEvent.PlayerSide.GetOppositeSide()).AnyQ((MapEventParty x) => x.Party.Ships.Count == 0)))
		{
			IsNavalEncounterFinishedWithDisengage = false;
			if (_mapEvent.PartiesOnSide(_mapEvent.PlayerSide).SumQ((MapEventParty x) => x.Party.Ships.Count) == 0)
			{
				_mapEvent.SetOverrideWinner(_mapEvent.PlayerSide.GetOppositeSide());
				EncounterState = PlayerEncounterState.PrepareResults;
				return;
			}
			if (_mapEvent.PartiesOnSide(_mapEvent.PlayerSide.GetOppositeSide()).SumQ((MapEventParty x) => x.Party.Ships.Count) == 0)
			{
				_mapEvent.SetOverrideWinner(_mapEvent.PlayerSide);
				EncounterState = PlayerEncounterState.PrepareResults;
				return;
			}
			List<MapEventParty> list = _mapEvent.AttackerSide.Parties.ToList();
			list.AddRange(_mapEvent.DefenderSide.Parties);
			if (MobileParty.MainParty.Ships.Count == 0)
			{
				PartyBase.MainParty.MapEventSide = null;
				Finish();
				RaftStateChangeAction.ActivateRaftStateForParty(MobileParty.MainParty);
			}
			foreach (MapEventParty item in list)
			{
				if (item.Ships.Count != 0 || item.Party == PartyBase.MainParty)
				{
					continue;
				}
				item.Party.MapEventSide = null;
				if (item.Party.IsMobile)
				{
					if (!item.Party.MobileParty.HasNavalNavigationCapability && item.Party.MobileParty.HasLandNavigationCapability)
					{
						RaftStateChangeAction.ActivateRaftStateForParty(item.Party.MobileParty);
					}
					else if (!item.Party.MobileParty.HasLandNavigationCapability)
					{
						DestroyPartyAction.Apply(null, item.Party.MobileParty);
					}
				}
			}
			_stateHandled = true;
		}
		else
		{
			_stateHandled = true;
			if (IsJoinedBattle && Campaign.Current.CurrentMenuContext != null && Campaign.Current.CurrentMenuContext.GameMenu.StringId == "join_encounter")
			{
				LeaveBattle();
			}
			if (_mapEvent != null && _mapEvent.IsHideoutBattle)
			{
				_mapEvent.MapEventSettlement.Hideout.SetNextPossibleAttackTime(Campaign.Current.Models.HideoutModel.HideoutHiddenDuration);
			}
		}
	}

	private static void SetEncounterMenuTexts()
	{
		MBTextManager.SetTextVariable("PARTY", MapEvent.PlayerMapEvent.GetLeaderParty(PartyBase.MainParty.OpponentSide).Name);
		if (!EnemySurrender)
		{
			MBTextManager.SetTextVariable("ENCOUNTER_TEXT", GameTexts.FindText("str_you_have_encountered_PARTY"), sendClients: true);
		}
		else
		{
			MBTextManager.SetTextVariable("ENCOUNTER_TEXT", GameTexts.FindText("str_you_have_encountered_PARTY_they_surrendered"), sendClients: true);
		}
	}

	public static bool CheckIfLeadingAvaliable()
	{
		bool flag = Hero.MainHero.PartyBelongedTo != null && !Hero.MainHero.IsWounded;
		bool flag2 = Hero.MainHero.PartyBelongedTo != null && Hero.MainHero.PartyBelongedTo.Army != null && Hero.MainHero.PartyBelongedTo.Army.ArmyOwner != Hero.MainHero;
		bool flag3 = false;
		foreach (MapEventParty item in MobileParty.MainParty.MapEvent.PartiesOnSide(MobileParty.MainParty.MapEvent.PlayerSide))
		{
			if (item.Party != MobileParty.MainParty.Party && item.Party.LeaderHero != null && item.Party.LeaderHero.Clan.Renown > Clan.PlayerClan.Renown)
			{
				flag3 = true;
				break;
			}
		}
		if (flag)
		{
			return flag2 || flag3;
		}
		return false;
	}

	public static Hero GetLeadingHero()
	{
		if (Hero.MainHero.PartyBelongedTo != null && Hero.MainHero.PartyBelongedTo.Army != null)
		{
			return MobileParty.MainParty.Army.ArmyOwner;
		}
		foreach (MapEventParty item in MobileParty.MainParty.MapEvent.PartiesOnSide(MobileParty.MainParty.MapEvent.PlayerSide))
		{
			if (item.Party != MobileParty.MainParty.Party && item.Party.LeaderHero != null && item.Party.LeaderHero.Clan.Renown > Clan.PlayerClan.Renown)
			{
				return item.Party.LeaderHero;
			}
		}
		return Hero.MainHero;
	}

	private void DoPrepareResults()
	{
		EncounterState = PlayerEncounterState.ApplyResults;
	}

	public static void SetPlayerVictorious()
	{
		Current.SetPlayerVictoriousInternal();
	}

	public void SetIsSallyOutAmbush(bool value)
	{
		if (Current._isSallyOutAmbush && !value)
		{
			_campaignBattleResult = null;
		}
		Current._isSallyOutAmbush = value;
	}

	public void SetIsBlockadeAttack(bool value)
	{
		Current.ForceBlockadeAttack = value;
	}

	public void SetIsBlockadeSallyOutAttack(bool value)
	{
		Current.ForceBlockadeSallyOutAttack = value;
	}

	public void SetPlayerSiegeInterruptedByEnemyDefection()
	{
		Current._isSiegeInterruptedByEnemyDefection = true;
	}

	private void SetPlayerVictoriousInternal()
	{
		if (PlayerSide == BattleSideEnum.Attacker || PlayerSide == BattleSideEnum.Defender)
		{
			_mapEvent.SetOverrideWinner(PlayerSide);
		}
	}

	public static void SetPlayerSiegeContinueWithDefenderPullBack()
	{
		Current._mapEvent.SetDefenderPulledBack();
	}

	private void DoApplyMapEventResults()
	{
		CampaignEventDispatcher.Instance.OnPlayerBattleEnd(_mapEvent);
		_mapEvent.CalculateAndCommitMapEventResults();
		if (_mapEvent.WinningSide == PartyBase.MainParty.Side)
		{
			EncounterState = PlayerEncounterState.PlayerVictory;
		}
		else if (_mapEvent.DefeatedSide == PartyBase.MainParty.Side && !IsNavalEncounterFinishedWithDisengage)
		{
			EncounterState = PlayerEncounterState.PlayerTotalDefeat;
		}
		else
		{
			EncounterState = PlayerEncounterState.End;
		}
	}

	public static void StartAttackMission()
	{
		Current._campaignBattleResult = new CampaignBattleResult();
	}

	private void DoPlayerVictory()
	{
		if (_helpedHeroes != null)
		{
			if (_helpedHeroes.Count > 0)
			{
				if (_helpedHeroes[0].DeathMark == KillCharacterAction.KillCharacterActionDetail.None)
				{
					Campaign.Current.CurrentConversationContext = ConversationContext.PartyEncounter;
					ConversationCharacterData playerCharacterData = new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty);
					ConversationCharacterData conversationPartnerData = new ConversationCharacterData(_helpedHeroes[0].CharacterObject, _helpedHeroes[0].PartyBelongedTo.Party);
					if (PartyBase.MainParty.MobileParty.IsCurrentlyAtSea || (conversationPartnerData.Party.IsMobile && conversationPartnerData.Party.MobileParty.IsCurrentlyAtSea))
					{
						CampaignMission.OpenConversationMission(playerCharacterData, conversationPartnerData);
					}
					else
					{
						CampaignMapConversation.OpenConversation(playerCharacterData, conversationPartnerData);
					}
				}
				_helpedHeroes.RemoveAt(0);
				_stateHandled = true;
			}
			else
			{
				MobileParty.MainParty.MemberRoster.RemoveZeroCounts();
				MobileParty.MainParty.PrisonRoster.RemoveZeroCounts();
				EncounterState = PlayerEncounterState.CaptureHeroes;
			}
			return;
		}
		_helpedHeroes = new List<Hero>();
		foreach (PartyBase involvedParty in MapEvent.PlayerMapEvent.InvolvedParties)
		{
			if (involvedParty != PartyBase.MainParty && involvedParty.Side == PartyBase.MainParty.Side && involvedParty.Owner != null && involvedParty.Owner != Hero.MainHero && involvedParty.LeaderHero != null && (MapEvent.PlayerMapEvent.AttackerSide.LeaderParty == involvedParty || MapEvent.PlayerMapEvent.DefenderSide.LeaderParty == involvedParty) && involvedParty.MobileParty != null && (involvedParty.MobileParty.Army == null || involvedParty.MobileParty.Army != MobileParty.MainParty.Army) && Campaign.Current.Models.BattleRewardModel.GetPlayerGainedRelationAmount(MapEvent.PlayerMapEvent, involvedParty.LeaderHero) > 0)
			{
				_helpedHeroes.Add(involvedParty.LeaderHero);
			}
		}
	}

	private void DoPlayerDefeat()
	{
		bool playerSurrender = PlayerSurrender;
		bool endedByRetreat = _mapEvent.EndedByRetreat;
		Finish();
		if (MobileParty.MainParty.BesiegerCamp != null)
		{
			if (MobileParty.MainParty.BesiegerCamp != null)
			{
				MobileParty.MainParty.BesiegerCamp = null;
			}
			else
			{
				PlayerSiege.FinalizePlayerSiege();
			}
		}
		if (Hero.MainHero.DeathMark != KillCharacterAction.KillCharacterActionDetail.DiedInBattle && !endedByRetreat)
		{
			GameMenu.ActivateGameMenu(playerSurrender ? "taken_prisoner" : "defeated_and_taken_prisoner");
		}
		_stateHandled = true;
	}

	private void DoCaptureHeroes()
	{
		if (_capturedHeroes == null)
		{
			_capturedHeroes = RosterToReceiveLootPrisoners.RemoveIf((TroopRosterElement lordElement) => lordElement.Character.IsHero).ToList();
		}
		if (_capturedHeroes.Count > 0)
		{
			TroopRosterElement troopRosterElement = _capturedHeroes[_capturedHeroes.Count - 1];
			Campaign.Current.CurrentConversationContext = ConversationContext.CapturedLord;
			ConversationCharacterData playerCharacterData = new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty);
			ConversationCharacterData conversationPartnerData = new ConversationCharacterData(troopRosterElement.Character, null, noHorse: true, noWeapon: true, spawnAfterFight: true);
			if (InsideSettlement && Settlement.CurrentSettlement.IsHideout)
			{
				CampaignMission.OpenConversationMission(playerCharacterData, conversationPartnerData);
			}
			else if (PartyBase.MainParty.MobileParty.IsCurrentlyAtSea)
			{
				CampaignMission.OpenConversationMission(playerCharacterData, conversationPartnerData);
			}
			else
			{
				CampaignMapConversation.OpenConversation(playerCharacterData, conversationPartnerData);
			}
			Campaign.Current.ConversationManager.ConversationEndOneShot += delegate
			{
				_capturedHeroes.RemoveRange(_capturedHeroes.Count - 1, 1);
			};
			_stateHandled = true;
		}
		else
		{
			EncounterState = PlayerEncounterState.FreeHeroes;
		}
	}

	private void DoFreeOrCapturePrisonerHeroes()
	{
		if (_capturedAlreadyPrisonerHeroes == null)
		{
			_capturedAlreadyPrisonerHeroes = RosterToReceiveLootMembers.RemoveIf((TroopRosterElement lordElement) => lordElement.Character.IsHero && lordElement.Character.HeroObject.PartyBelongedToAsPrisoner != PartyBase.MainParty).ToList();
		}
		if (_capturedAlreadyPrisonerHeroes.AnyQ((TroopRosterElement h) => h.Character.HeroObject.IsPrisoner && h.Character.HeroObject.PartyBelongedToAsPrisoner != PartyBase.MainParty))
		{
			TroopRosterElement troopRosterElement = _capturedAlreadyPrisonerHeroes.Last((TroopRosterElement h) => h.Character.HeroObject.IsPrisoner && h.Character.HeroObject.PartyBelongedToAsPrisoner != PartyBase.MainParty);
			Campaign.Current.CurrentConversationContext = ConversationContext.FreeOrCapturePrisonerHero;
			ConversationCharacterData playerCharacterData = new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty);
			ConversationCharacterData conversationPartnerData = new ConversationCharacterData(troopRosterElement.Character, null, noHorse: true, noWeapon: true);
			if (PartyBase.MainParty.MobileParty.IsCurrentlyAtSea)
			{
				CampaignMission.OpenConversationMission(playerCharacterData, conversationPartnerData);
			}
			else
			{
				CampaignMapConversation.OpenConversation(playerCharacterData, conversationPartnerData);
			}
			_stateHandled = true;
		}
		else
		{
			EncounterState = PlayerEncounterState.LootParty;
		}
	}

	private void DoLootInventory()
	{
		if (RosterToReceiveLootItems.Count > 0)
		{
			InventoryScreenHelper.OpenScreenAsLoot(new Dictionary<PartyBase, ItemRoster> { 
			{
				PartyBase.MainParty,
				RosterToReceiveLootItems
			} });
			_stateHandled = true;
		}
		EncounterState = PlayerEncounterState.LootShips;
	}

	private void DoLootShips()
	{
		if (PlayerLootedFigurehead != null)
		{
			Campaign.Current.UnlockFigurehead(PlayerLootedFigurehead);
		}
		if (!ReceivedLootShips.IsEmpty())
		{
			PortStateHelper.OpenAsLoot(ReceivedLootShips.ToMBList(), OnPlayerLootShipEnd);
			_stateHandled = true;
		}
		EncounterState = PlayerEncounterState.End;
	}

	private void OnPlayerLootShipEnd()
	{
		foreach (Ship receivedLootShip in ReceivedLootShips)
		{
			if (receivedLootShip.Owner != PartyBase.MainParty)
			{
				DestroyShipAction.Apply(receivedLootShip);
			}
		}
	}

	private void DoLootMembersAndPrisonersOfParty()
	{
		if (RosterToReceiveLootMembers.Count > 0 || RosterToReceiveLootPrisoners.Count > 0)
		{
			PartyScreenHelper.OpenScreenAsLoot(RosterToReceiveLootMembers, RosterToReceiveLootPrisoners, TextObject.GetEmpty(), RosterToReceiveLootMembers.TotalManCount + RosterToReceiveLootPrisoners.TotalManCount, OnPlayerLootMembersAndPrisonerEnd);
			_stateHandled = true;
		}
		EncounterState = PlayerEncounterState.LootInventory;
	}

	private void OnPlayerLootMembersAndPrisonerEnd(PartyBase leftOwnerParty, TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, PartyBase rightOwnerParty, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, bool fromCancel)
	{
		RosterToReceiveLootMembers.Clear();
		RosterToReceiveLootPrisoners.Clear();
	}

	private void DoEnd()
	{
		bool num = _mapEvent?.IsSiegeAssault ?? false;
		bool flag = _mapEvent?.IsSallyOut ?? false;
		bool isHideoutBattle = _mapEvent.IsHideoutBattle;
		bool flag2 = num && MobileParty.MainParty.MapEvent != null && MobileParty.MainParty.MapEvent == _mapEvent;
		bool flag3 = flag && MobileParty.MainParty.MapEvent != null && MobileParty.MainParty.MapEvent == _mapEvent;
		bool flag4 = MobileParty.MainParty.MapEvent != null && PlayerSide == BattleSideEnum.Attacker;
		bool flag5 = MobileParty.MainParty.MapEvent != null && PlayerSide == BattleSideEnum.Defender;
		bool isRaid = _mapEvent.IsRaid;
		bool isForcingVolunteers = _mapEvent.IsForcingVolunteers;
		bool isForcingSupplies = _mapEvent.IsForcingSupplies;
		bool isBlockadeSallyOut = _mapEvent.IsBlockadeSallyOut;
		bool flag6 = BattleSimulation != null && _mapEvent.WinningSide != PlayerSide;
		Settlement mapEventSettlement = _mapEvent.MapEventSettlement;
		BattleState battleState = _mapEvent.BattleState;
		_stateHandled = true;
		if (!flag6)
		{
			Finish();
		}
		if (num || flag || isBlockadeSallyOut)
		{
			if (mapEventSettlement == null)
			{
				return;
			}
			if (flag2)
			{
				if (flag4)
				{
					EncounterManager.StartSettlementEncounter((MobileParty.MainParty.Army != null) ? MobileParty.MainParty.Army.LeaderParty : MobileParty.MainParty, mapEventSettlement);
					GameMenu.SwitchToMenu("menu_settlement_taken");
				}
			}
			else if (flag3)
			{
				if (flag5)
				{
					EncounterManager.StartSettlementEncounter((MobileParty.MainParty.Army != null) ? MobileParty.MainParty.Army.LeaderParty : MobileParty.MainParty, mapEventSettlement);
					GameMenu.SwitchToMenu("menu_settlement_taken");
				}
			}
			else if (isBlockadeSallyOut)
			{
				if (flag5)
				{
					EncounterManager.StartSettlementEncounter((MobileParty.MainParty.Army != null) ? MobileParty.MainParty.Army.LeaderParty : MobileParty.MainParty, mapEventSettlement);
					GameMenu.SwitchToMenu("menu_settlement_taken");
				}
			}
			else if (InsideSettlement)
			{
				LeaveSettlement();
			}
		}
		else if (isRaid || isForcingVolunteers || isForcingSupplies)
		{
			if ((_attackerParty.IsMobile && _attackerParty.MobileParty.Army != null && _attackerParty.MobileParty.Army.LeaderParty != _attackerParty.MobileParty) || !flag4 || _attackerParty != MobileParty.MainParty.Party)
			{
				return;
			}
			EncounterManager.StartSettlementEncounter(MobileParty.MainParty, mapEventSettlement);
			Current.ForceSupplies = isForcingSupplies;
			Current.ForceVolunteers = isForcingVolunteers;
			Current.ForceRaid = isRaid;
			BeHostileAction.ApplyEncounterHostileAction(PartyBase.MainParty, Settlement.CurrentSettlement.Party);
			if (isForcingSupplies)
			{
				GameMenu.SwitchToMenu("force_supplies_village");
			}
			else if (isForcingVolunteers)
			{
				GameMenu.SwitchToMenu("force_volunteers_village");
			}
			else if (isRaid)
			{
				if (InsideSettlement)
				{
					LeaveSettlementAction.ApplyForParty(MobileParty.MainParty);
				}
				StartBattle();
				GameMenu.SwitchToMenu("raiding_village");
				Current.ForceRaid = false;
				Current.ForceVolunteers = false;
				Current.ForceSupplies = false;
			}
		}
		else if (isHideoutBattle)
		{
			if (mapEventSettlement == null)
			{
				return;
			}
			switch (battleState)
			{
			case BattleState.AttackerVictory:
				if (mapEventSettlement.Parties.Count > 0)
				{
					foreach (MobileParty item in new List<MobileParty>(mapEventSettlement.Parties))
					{
						LeaveSettlementAction.ApplyForParty(item);
						item.Ai.SetDoNotAttackMainParty(3);
					}
				}
				mapEventSettlement.Hideout.IsSpotted = false;
				mapEventSettlement.IsVisible = false;
				break;
			case BattleState.None:
				EncounterManager.StartSettlementEncounter(MobileParty.MainParty, mapEventSettlement);
				GameMenu.SwitchToMenu("hideout_after_defeated_and_saved");
				break;
			}
		}
		else if (IsNavalEncounterFinishedWithDisengage)
		{
			GameMenu.ActivateGameMenu("naval_encounter_disengaged");
		}
		else if (flag6)
		{
			EncounterState = PlayerEncounterState.Begin;
			GameMenu.SwitchToMenu("encounter");
		}
	}

	private bool CheckIfBattleShouldContinueAfterBattleMission()
	{
		if (_doesBattleContinue || _campaignBattleResult != null)
		{
			_doesBattleContinue = CheckIfBattleShouldContinueAfterBattleMissionInternal(_campaignBattleResult);
		}
		return _doesBattleContinue;
	}

	private bool CheckIfBattleShouldContinueAfterBattleMissionInternal(CampaignBattleResult campaignBattleResult)
	{
		MapEventSide mapEventSide = _mapEvent.GetMapEventSide(PlayerSide);
		if (PlayerSurrender || campaignBattleResult == null || (campaignBattleResult.EnemyRetreated && (!_mapEvent.IsSiegeAssault || mapEventSide.OtherSide != _mapEvent.AttackerSide)))
		{
			return false;
		}
		bool flag = _mapEvent.IsSiegeAssault && BattleState == BattleState.AttackerVictory;
		bool flag2 = !_mapEvent.CheckIfOneSideHasLost();
		if (_mapEvent.DefeatedSide != BattleSideEnum.None)
		{
			MapEventSide mapEventSide2 = _mapEvent.GetMapEventSide(_mapEvent.DefeatedSide);
			bool num = campaignBattleResult.PlayerDefeat || campaignBattleResult.PlayerVictory || campaignBattleResult.EnemyPulledBack;
			bool flag3 = mapEventSide2.GetTotalHealthyTroopCountOfSide() + mapEventSide2.GetTotalHealthyHeroCountOfSide() >= 1;
			flag2 = num && flag3;
			if (_mapEvent.IsNavalMapEvent && _mapEvent.IsFieldBattle && flag2)
			{
				if (((_mapEvent.DefeatedSide != BattleSideEnum.None) ? _mapEvent.GetMapEventSide(_mapEvent.DefeatedSide).Parties : _mapEvent.PartiesOnSide(_mapEvent.PlayerSide)).SumQ((MapEventParty t) => t.Ships.Count) - CapturedShipsInEncounter.Count <= 0)
				{
					return false;
				}
				if (((_mapEvent.DefeatedSide != BattleSideEnum.None) ? _mapEvent.GetMapEventSide(WinningSide).Parties : _mapEvent.GetMapEventSide(_mapEvent.PlayerSide.GetOppositeSide()).Parties).SumQ((MapEventParty t) => t.Ships.Count) == 0)
				{
					return false;
				}
				_mapEvent.RecalculateStrengthOfSides();
				float num2 = _mapEvent.StrengthOfSide[(int)_mapEvent.PlayerSide];
				float num3 = _mapEvent.StrengthOfSide[(int)_mapEvent.PlayerSide.GetOppositeSide()];
				if ((PlayerIsAttacker && num2 * 0.45f >= num3) || (PlayerIsDefender && num2 <= num3 * 0.15f))
				{
					return false;
				}
				IsNavalEncounterFinishedWithDisengage = true;
				return false;
			}
		}
		if (!_mapEvent.IsHideoutBattle && !flag && flag2)
		{
			return !mapEventSide.IsSurrendered;
		}
		return false;
	}

	public void FinalizeBattle()
	{
		if (_mapEvent != null)
		{
			if (_mapEvent.HasWinner || _mapEvent.DiplomaticallyFinished || _mapEvent.IsSiegeAmbush || (_mapEvent.IsRaid && (_mapEvent.MapEventSettlement.SettlementHitPoints.ApproximatelyEqualsTo(0f) || (_mapEvent.PlayerSide != BattleSideEnum.None && _mapEvent.PartiesOnSide(_mapEvent.PlayerSide).Any((MapEventParty x) => x.Party.MobileParty != null && !x.Party.MobileParty.IsMainParty)))))
			{
				_mapEvent.FinalizeEvent();
				_mapEvent = null;
			}
			else
			{
				LeaveBattle();
			}
		}
	}

	public void FindNonAttachedNpcPartiesWhoWillJoinEvent(List<MobileParty> partiesToJoinPlayerSide, List<MobileParty> partiesToJoinEnemySide)
	{
		Campaign.Current.Models.EncounterModel.FindNonAttachedNpcPartiesWhoWillJoinPlayerEncounter(partiesToJoinPlayerSide, partiesToJoinEnemySide);
	}

	public void FindAllNpcPartiesWhoWillJoinEvent(List<MobileParty> partiesToJoinPlayerSide, List<MobileParty> partiesToJoinEnemySide)
	{
		FindNonAttachedNpcPartiesWhoWillJoinEvent(partiesToJoinPlayerSide, partiesToJoinEnemySide);
		foreach (MobileParty item in partiesToJoinPlayerSide.ToList())
		{
			partiesToJoinPlayerSide.AddRange(item.AttachedParties.Except(partiesToJoinPlayerSide));
		}
		foreach (MobileParty item2 in partiesToJoinEnemySide.ToList())
		{
			partiesToJoinEnemySide.AddRange(item2.AttachedParties.Except(partiesToJoinEnemySide));
		}
	}

	public static void EnterSettlement()
	{
		Settlement encounterSettlement = EncounterSettlement;
		CreateLocationEncounter(encounterSettlement);
		EnterSettlementAction.ApplyForParty(MobileParty.MainParty, encounterSettlement);
	}

	private static void CreateLocationEncounter(Settlement settlement)
	{
		if (settlement.IsTown)
		{
			LocationEncounter = new TownEncounter(settlement);
		}
		else if (settlement.IsVillage)
		{
			LocationEncounter = new VillageEncounter(settlement);
		}
		else if (settlement.IsCastle)
		{
			LocationEncounter = new CastleEncounter(settlement);
		}
		else if (settlement.IsHideout)
		{
			LocationEncounter = new HideoutEncounter(settlement);
		}
	}

	public static void LeaveBattle()
	{
		MapEvent playerMapEvent = MapEvent.PlayerMapEvent;
		bool flag = false;
		if (playerMapEvent != null)
		{
			int numberOfInvolvedMen = playerMapEvent.GetNumberOfInvolvedMen(PartyBase.MainParty.Side);
			Army playerArmy = MobileParty.MainParty.Army;
			if ((PartyBase.MainParty.MapEventSide.LeaderParty != PartyBase.MainParty && PartyBase.MainParty.MapEventSide.Parties.Any((MapEventParty p) => p.IsNpcParty && (playerArmy == null || p.Party.MobileParty?.Army != playerArmy))) || (PartyBase.MainParty.MapEvent.IsSallyOut && Campaign.Current.Models.EncounterModel.GetLeaderOfMapEvent(PartyBase.MainParty.MapEvent, PartyBase.MainParty.MapEventSide.MissionSide) != Hero.MainHero))
			{
				PartyBase.MainParty.MapEventSide = null;
			}
			else
			{
				playerMapEvent.FinalizeEvent();
			}
			flag = numberOfInvolvedMen > PartyBase.MainParty.NumberOfHealthyMembers && playerMapEvent.AttackerSide.LeaderParty != PartyBase.MainParty && playerMapEvent.DefenderSide.LeaderParty != PartyBase.MainParty;
		}
		if (CurrentBattleSimulation != null)
		{
			MapState mapState = Game.Current.GameStateManager.LastOrDefault<MapState>();
			if (mapState != null && mapState.IsSimulationActive)
			{
				mapState.EndBattleSimulation();
			}
			Current.BattleSimulation = null;
			Current._mapEvent.BattleObserver = null;
		}
		Current.IsJoinedBattle = false;
		Current._mapEvent = null;
		if (flag && !playerMapEvent.HasWinner)
		{
			playerMapEvent.SimulateBattleSetup(Current.BattleSimulation?.SelectedTroops);
		}
	}

	public static void LeaveSettlement()
	{
		LeaveSettlementAction.ApplyForParty(MobileParty.MainParty);
		LocationEncounter = null;
		PartyBase.MainParty.SetVisualAsDirty();
	}

	public static void InitSimulation(FlattenedTroopRoster selectedTroopsForPlayerSide, FlattenedTroopRoster selectedTroopsForOtherSide)
	{
		if (Current != null)
		{
			Current.BattleSimulation = new BattleSimulation(selectedTroopsForPlayerSide, selectedTroopsForOtherSide);
			Current.BattleSimulation.ResetSimulation();
		}
	}

	public void InterruptEncounter(string encounterInterrupedType)
	{
		_ = Game.Current.GameStateManager.ActiveState;
		if (MapEvent.PlayerMapEvent != null)
		{
			LeaveBattle();
		}
		GameMenu.ActivateGameMenu(encounterInterrupedType);
	}

	public static void StartSiegeAmbushMission()
	{
		Settlement mapEventSettlement = Battle.MapEventSettlement;
		SiegeEvent playerSiegeEvent = PlayerSiege.PlayerSiegeEvent;
		switch (mapEventSettlement.CurrentSiegeState)
		{
		case Settlement.SiegeState.OnTheWalls:
		{
			List<MissionSiegeWeapon> preparedAndActiveSiegeEngines = playerSiegeEvent.GetPreparedAndActiveSiegeEngines(playerSiegeEvent.GetSiegeEventSide(BattleSideEnum.Attacker));
			List<MissionSiegeWeapon> preparedAndActiveSiegeEngines2 = playerSiegeEvent.GetPreparedAndActiveSiegeEngines(playerSiegeEvent.GetSiegeEventSide(BattleSideEnum.Defender));
			bool hasAnySiegeTower = preparedAndActiveSiegeEngines.Exists((MissionSiegeWeapon data) => data.Type == DefaultSiegeEngineTypes.SiegeTower);
			int wallLevel = mapEventSettlement.Town.GetWallLevel();
			CampaignMission.OpenSiegeMissionWithDeployment(mapEventSettlement.LocationComplex.GetLocationWithId("center").GetSceneName(wallLevel), mapEventSettlement.SettlementWallSectionHitPointsRatioList.ToArray(), hasAnySiegeTower, preparedAndActiveSiegeEngines, preparedAndActiveSiegeEngines2, Current.PlayerSide == BattleSideEnum.Attacker, wallLevel, isSallyOut: true);
			break;
		}
		case Settlement.SiegeState.InTheLordsHall:
		case Settlement.SiegeState.Invalid:
			Debug.FailedAssert("Siege state is invalid!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Encounters\\PlayerEncounter.cs", "StartSiegeAmbushMission", 2266);
			break;
		}
	}

	public static void StartVillageBattleMission()
	{
		Settlement mapEventSettlement = Battle.MapEventSettlement;
		int upgradeLevel = ((!mapEventSettlement.IsTown) ? 1 : mapEventSettlement.Town.GetWallLevel());
		CampaignMission.OpenBattleMission(mapEventSettlement.LocationComplex.GetScene("village_center", upgradeLevel), usesTownDecalAtlas: false, "land_raid");
	}

	public static void StartCombatMissionWithDialogueInTownCenter(CharacterObject characterToTalkTo)
	{
		int wallLevel = Settlement.CurrentSettlement.Town.GetWallLevel();
		CampaignMission.OpenCombatMissionWithDialogue(Settlement.CurrentSettlement.LocationComplex.GetScene("center", wallLevel), characterToTalkTo, wallLevel);
	}

	public static void StartHostileAction()
	{
		Current.StartHostileActionInternal();
	}

	private void StartHostileActionInternal()
	{
		if (_mapEvent != null)
		{
			if (InsideSettlement)
			{
				LeaveSettlement();
			}
			Update();
		}
	}

	public void GetBattleRewards(out ExplainedNumber renownChange, out ExplainedNumber influenceChange, out ExplainedNumber moraleChange, out float playerEarnedLootRate, out Figurehead playerEarnedFigurehead)
	{
		MapEventParty mapEventParty = _mapEvent.PartiesOnSide(_mapEvent.PlayerSide).Find((MapEventParty x) => x.Party == PartyBase.MainParty);
		renownChange = mapEventParty.GainedRenownExplained;
		influenceChange = mapEventParty.GainedInfluenceExplained;
		moraleChange = mapEventParty.GainedMoraleExplained;
		playerEarnedFigurehead = PlayerLootedFigurehead;
		playerEarnedLootRate = _mapEvent.GetPlayerBattleContributionRate();
	}
}
