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

	public List<Ship> CapturedShipsInEncounter { get; private set; } = new List<Ship>();

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
		if (!MBSaveLoad.IsUpdatingGameVersion || !MBSaveLoad.LastLoadedGameVersion.IsOlderThan(ApplicationVersion.FromString("v1.4.6")))
		{
			return;
		}
		bool flag = false;
		if (EncounteredBattle != null && EncounteredBattle.IsRaid && EncounterSettlementAux == null)
		{
			flag = true;
		}
		else if (Battle != null && EncounterSettlementAux == null)
		{
			foreach (MapEventParty party in Battle.GetMapEventSide(PlayerSide).Parties)
			{
				if (party.Party.IsMobile && party.Party.MobileParty.IsVillager && party.Party.MobileParty.CurrentSettlement != null && party.Party.MobileParty.CurrentSettlement.IsVillage)
				{
					flag = true;
					break;
				}
			}
		}
		if (flag)
		{
			Finish();
		}
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
							Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Encounters\\PlayerEncounter.cs", "Init", 559);
						}
					}
					else
					{
						Debug.FailedAssert("If there is no map event we should create one in order to join battle", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Encounters\\PlayerEncounter.cs", "Init", 564);
					}
				}
				_mapEvent.Component.AddNearbyPartiesToPlayerMapEvent();
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
			_mapEvent.Component.AddNearbyPartiesToPlayerMapEvent();
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
		_campaignBattleResult = null;
		if (_mapEvent == null)
		{
			if (ForceRaid)
			{
				_mapEvent = RaidEventComponent.CreateRaidEvent(_attackerParty, _defenderParty).MapEvent;
			}
			else if (ForceSallyOut)
			{
				_mapEvent = SiegeSallyOutEventComponent.CreateSiegeSallyOutEvent(_attackerParty, _defenderParty).MapEvent;
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
					_mapEvent = SiegeAssaultEventComponent.CreateSiegeAssaultMapEvent(_attackerParty, _defenderParty).MapEvent;
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
					Debug.FailedAssert("Proper mapEvent type could not be set for the battle.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Encounters\\PlayerEncounter.cs", "StartBattleInternal", 746);
				}
			}
			else if (_isSallyOutAmbush)
			{
				_mapEvent = SiegeAmbushEventComponent.CreateSiegeAmbushEvent(_attackerParty, _defenderParty).MapEvent;
			}
			else if (ForceBlockadeAttack)
			{
				_mapEvent = BlockadeBattleEventComponent.CreateBlockadeBattleMapEvent(_attackerParty, _defenderParty, isSallyOut: false).MapEvent;
			}
			else if (ForceBlockadeSallyOutAttack)
			{
				_mapEvent = BlockadeBattleEventComponent.CreateBlockadeBattleMapEvent(_attackerParty, _defenderParty, isSallyOut: true).MapEvent;
			}
			else if (_attackerParty.IsMobile && _attackerParty.MobileParty.CurrentSettlement != null && _attackerParty.MobileParty.CurrentSettlement.SiegeEvent != null)
			{
				if (_attackerParty.MobileParty.IsTargetingPort)
				{
					_mapEvent = BlockadeBattleEventComponent.CreateBlockadeBattleMapEvent(_attackerParty, _defenderParty, isSallyOut: true).MapEvent;
				}
				else
				{
					_mapEvent = SiegeSallyOutEventComponent.CreateSiegeSallyOutEvent(_attackerParty, _defenderParty).MapEvent;
				}
			}
			else if (_defenderParty.IsMobile && _defenderParty.MobileParty.BesiegedSettlement != null)
			{
				_mapEvent = SiegeOutsideEventComponent.CreateSiegeOutsideMapEvent(_attackerParty, _defenderParty).MapEvent;
			}
			else
			{
				_mapEvent = FieldBattleEventComponent.CreateFieldBattleEvent(_attackerParty, _defenderParty).MapEvent;
			}
		}
		if (!_mapEvent.IsFinalized)
		{
			_mapEvent.Component.AddNearbyPartiesToPlayerMapEvent();
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
			_mapEvent.Component.AddNearbyPartiesToPlayerMapEvent();
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
					Debug.FailedAssert("[DEBUG]Invalid map event state: " + _mapEventState, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Encounters\\PlayerEncounter.cs", "UpdateInternal", 1052);
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
		Debug.Print("Battle continues.");
		Debug.Print("Other side strength by party:");
		_mapEvent.RecalculateStrengthOfSides();
		foreach (MapEventParty party in _mapEvent.GetMapEventSide(_mapEvent.PlayerSide).OtherSide.Parties)
		{
			Debug.Print(string.Concat("party: ", party.Party.Id, ": ", party.Party.Name, ", strength: ", party.Party.CalculateCurrentStrength(), ", healthy count: ", party.Party.MemberRoster.TotalHealthyCount, ", wounded count: ", party.Party.MemberRoster.TotalWounded));
		}
		_mapEvent.Component.OnPlayerEncounterContinueBattle(_campaignBattleResult, out var nextEncounterState, out var stateHandled);
		EncounterState = nextEncounterState;
		_stateHandled = stateHandled;
		Debug.Print("[PlayerEncounter.ContinueBattle End]");
	}

	private void DoWait()
	{
		SetEncounterMenuTexts();
		if (CheckIfBattleShouldContinueAfterBattleMission())
		{
			ContinueBattle();
			return;
		}
		_mapEvent.Component.UpdatePlayerEncounterState(_campaignBattleResult, out var nextEncounterState, out var stateHandled);
		_stateHandled = stateHandled;
		EncounterState = nextEncounterState;
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
		EncounterState = _mapEvent.Component.GetPlayerEncounterStateOnMapEventEnd();
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
		MapEvent mapEvent = _mapEvent;
		bool flag = BattleSimulation != null && _mapEvent.WinningSide != PlayerSide;
		_stateHandled = true;
		bool playerIsAttacker = _mapEvent.PlayerSide == BattleSideEnum.Attacker;
		if (!flag)
		{
			Finish();
		}
		if (!mapEvent.Component.TryHandlePlayerEncounterEnd(playerIsAttacker) && flag)
		{
			EncounterState = PlayerEncounterState.Begin;
			GameMenu.SwitchToMenu("encounter");
		}
	}

	private bool CheckIfBattleShouldContinueAfterBattleMission()
	{
		if (_doesBattleContinue || _campaignBattleResult != null)
		{
			_doesBattleContinue = _mapEvent.Component.CheckIfBattleShouldContinueAfterBattleMission(_campaignBattleResult);
		}
		return _doesBattleContinue;
	}

	public void FinalizeBattle()
	{
		_mapEvent?.Component.OnPlayerEncounterFinalizeBattle();
	}

	public void FinalizeBattleFromComponent()
	{
		_mapEvent.FinalizeEvent();
		_mapEvent = null;
	}

	public void FindAllNpcPartiesWhoWillJoinEvent(List<MobileParty> partiesToJoinPlayerSide, List<MobileParty> partiesToJoinEnemySide)
	{
		Campaign.Current.Models.EncounterModel.FindNonAttachedNpcPartiesWhoWillJoinPlayerEncounter(partiesToJoinPlayerSide, partiesToJoinEnemySide);
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
			Current._campaignBattleResult = null;
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
			Debug.FailedAssert("Siege state is invalid!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Encounters\\PlayerEncounter.cs", "StartSiegeAmbushMission", 1815);
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
