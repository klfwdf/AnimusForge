using System;
using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.BarterSystem;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation.Persuasion;
using TaleWorlds.CampaignSystem.CraftingSystem;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Incidents;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem;

public class CampaignEvents : CampaignEventReceiver
{
	private readonly MbEvent _onPlayerBodyPropertiesChangedEvent = new MbEvent();

	private readonly MbEvent<BarterData> _barterablesRequested = new MbEvent<BarterData>();

	private readonly MbEvent<Hero, bool> _heroLevelledUp = new MbEvent<Hero, bool>();

	private readonly MbEvent<BanditPartyComponent, Hideout> _onHomeHideoutChangedEvent = new MbEvent<BanditPartyComponent, Hideout>();

	private readonly MbEvent<Hero, SkillObject, int, bool> _heroGainedSkill = new MbEvent<Hero, SkillObject, int, bool>();

	private readonly MbEvent _onCharacterCreationIsOverEvent = new MbEvent();

	private readonly MbEvent<Hero, bool> _onHeroCreated = new MbEvent<Hero, bool>();

	private readonly MbEvent<Hero, Hero.CharacterStates> _onHeroActivatedEvent = new MbEvent<Hero, Hero.CharacterStates>();

	private readonly MbEvent<Hero, Occupation> _heroOccupationChangedEvent = new MbEvent<Hero, Occupation>();

	private readonly MbEvent<Hero> _onHeroWounded = new MbEvent<Hero>();

	private readonly MbEvent<Hero, Hero, List<Barterable>> _onBarterAcceptedEvent = new MbEvent<Hero, Hero, List<Barterable>>();

	private readonly MbEvent<Hero, Hero, List<Barterable>> _onBarterCanceledEvent = new MbEvent<Hero, Hero, List<Barterable>>();

	private readonly MbEvent<Hero, Hero, int, bool, ChangeRelationAction.ChangeRelationDetail, Hero, Hero> _heroRelationChanged = new MbEvent<Hero, Hero, int, bool, ChangeRelationAction.ChangeRelationDetail, Hero, Hero>();

	private readonly MbEvent<QuestBase, bool> _questLogAddedEvent = new MbEvent<QuestBase, bool>();

	private readonly MbEvent<IssueBase, bool> _issueLogAddedEvent = new MbEvent<IssueBase, bool>();

	private readonly MbEvent<Clan, bool> _clanTierIncrease = new MbEvent<Clan, bool>();

	private readonly MbEvent<Clan, Kingdom, Kingdom, ChangeKingdomAction.ChangeKingdomActionDetail, bool> _clanChangedKingdom = new MbEvent<Clan, Kingdom, Kingdom, ChangeKingdomAction.ChangeKingdomActionDetail, bool>();

	private readonly MbEvent<Clan, Kingdom, Kingdom> _onClanDefected = new MbEvent<Clan, Kingdom, Kingdom>();

	private readonly MbEvent<Clan, bool> _onClanCreatedEvent = new MbEvent<Clan, bool>();

	private readonly MbEvent<Hero, MobileParty> _onHeroJoinedPartyEvent = new MbEvent<Hero, MobileParty>();

	private readonly MbEvent<(Hero, PartyBase), (Hero, PartyBase), (int, string), bool> _heroOrPartyTradedGold = new MbEvent<(Hero, PartyBase), (Hero, PartyBase), (int, string), bool>();

	private readonly MbEvent<(Hero, PartyBase), (Hero, PartyBase), ItemRosterElement, bool> _heroOrPartyGaveItem = new MbEvent<(Hero, PartyBase), (Hero, PartyBase), ItemRosterElement, bool>();

	private readonly MbEvent<MobileParty> _banditPartyRecruited = new MbEvent<MobileParty>();

	private readonly MbEvent<KingdomDecision, bool> _kingdomDecisionAdded = new MbEvent<KingdomDecision, bool>();

	private readonly MbEvent<KingdomDecision, bool> _kingdomDecisionCancelled = new MbEvent<KingdomDecision, bool>();

	private readonly MbEvent<KingdomDecision, DecisionOutcome, bool> _kingdomDecisionConcluded = new MbEvent<KingdomDecision, DecisionOutcome, bool>();

	private readonly MbEvent<MobileParty> _partyAttachedParty = new MbEvent<MobileParty>();

	private readonly MbEvent<MobileParty> _nearbyPartyAddedToPlayerMapEvent = new MbEvent<MobileParty>();

	private readonly MbEvent<Army> _armyCreated = new MbEvent<Army>();

	private readonly MbEvent<Army, Army.ArmyDispersionReason, bool> _armyDispersed = new MbEvent<Army, Army.ArmyDispersionReason, bool>();

	private readonly MbEvent<Army, IMapPoint> _armyGathered = new MbEvent<Army, IMapPoint>();

	private readonly MbEvent<Hero, PerkObject> _perkOpenedEvent = new MbEvent<Hero, PerkObject>();

	private readonly MbEvent<Hero, PerkObject> _perkResetEvent = new MbEvent<Hero, PerkObject>();

	private readonly MbEvent<TraitObject, int> _playerTraitChangedEvent = new MbEvent<TraitObject, int>();

	private readonly MbEvent<Village, Village.VillageStates, Village.VillageStates, MobileParty> _villageStateChanged = new MbEvent<Village, Village.VillageStates, Village.VillageStates, MobileParty>();

	private readonly MbEvent<MobileParty, Settlement, Hero> _settlementEntered = new MbEvent<MobileParty, Settlement, Hero>();

	private readonly MbEvent<MobileParty, Settlement, Hero> _afterSettlementEntered = new MbEvent<MobileParty, Settlement, Hero>();

	private readonly MbEvent<MobileParty, Settlement, Hero> _beforeSettlementEntered = new MbEvent<MobileParty, Settlement, Hero>();

	private readonly MbEvent<Town, CharacterObject, CharacterObject> _mercenaryTroopChangedInTown = new MbEvent<Town, CharacterObject, CharacterObject>();

	private readonly MbEvent<Town, int, int> _mercenaryNumberChangedInTown = new MbEvent<Town, int, int>();

	private readonly MbEvent<Alley, Hero, Hero> _alleyOwnerChanged = new MbEvent<Alley, Hero, Hero>();

	private readonly MbEvent<Alley, TroopRoster> _alleyOccupiedByPlayer = new MbEvent<Alley, TroopRoster>();

	private readonly MbEvent<Alley> _alleyClearedByPlayer = new MbEvent<Alley>();

	private readonly MbEvent<Hero, Hero, Romance.RomanceLevelEnum> _romanticStateChanged = new MbEvent<Hero, Hero, Romance.RomanceLevelEnum>();

	private readonly MbEvent<Hero, Hero, bool> _beforeHeroesMarried = new MbEvent<Hero, Hero, bool>();

	private readonly MbEvent<int, Town> _playerEliminatedFromTournament = new MbEvent<int, Town>();

	private readonly MbEvent<Town> _playerStartedTournamentMatch = new MbEvent<Town>();

	private readonly MbEvent<Town> _tournamentStarted = new MbEvent<Town>();

	private readonly MbEvent<IFaction, IFaction, DeclareWarAction.DeclareWarDetail> _warDeclared = new MbEvent<IFaction, IFaction, DeclareWarAction.DeclareWarDetail>();

	private readonly MbEvent<CharacterObject, MBReadOnlyList<CharacterObject>, Town, ItemObject> _tournamentFinished = new MbEvent<CharacterObject, MBReadOnlyList<CharacterObject>, Town, ItemObject>();

	private readonly MbEvent<Town> _tournamentCancelled = new MbEvent<Town>();

	private readonly MbEvent<PartyBase, PartyBase, object, bool> _battleStarted = new MbEvent<PartyBase, PartyBase, object, bool>();

	private readonly MbEvent<Settlement, Clan> _rebellionFinished = new MbEvent<Settlement, Clan>();

	private readonly MbEvent<Town, bool> _townRebelliousStateChanged = new MbEvent<Town, bool>();

	private readonly MbEvent<Settlement, Clan> _rebelliousClanDisbandedAtSettlement = new MbEvent<Settlement, Clan>();

	private readonly MbEvent<MobileParty, ItemRoster> _itemsLooted = new MbEvent<MobileParty, ItemRoster>();

	private readonly MbEvent<MobileParty, PartyBase> _mobilePartyDestroyed = new MbEvent<MobileParty, PartyBase>();

	private readonly MbEvent<MobileParty> _mobilePartyCreated = new MbEvent<MobileParty>();

	private readonly MbEvent<IInteractablePoint> _mapInteractableCreated = new MbEvent<IInteractablePoint>();

	private readonly MbEvent<IInteractablePoint> _mapInteractableDestroyed = new MbEvent<IInteractablePoint>();

	private readonly MbEvent<MobileParty, bool> _mobilePartyQuestStatusChanged = new MbEvent<MobileParty, bool>();

	private readonly MbEvent<Hero, Hero, KillCharacterAction.KillCharacterActionDetail, bool> _heroKilled = new MbEvent<Hero, Hero, KillCharacterAction.KillCharacterActionDetail, bool>();

	private readonly MbEvent<Hero, Hero, KillCharacterAction.KillCharacterActionDetail, bool> _onBeforeHeroKilled = new MbEvent<Hero, Hero, KillCharacterAction.KillCharacterActionDetail, bool>();

	private readonly MbEvent<Hero, int> _childEducationCompleted = new MbEvent<Hero, int>();

	private readonly MbEvent<Hero> _heroComesOfAge = new MbEvent<Hero>();

	private readonly MbEvent<Hero> _heroGrowsOutOfInfancyEvent = new MbEvent<Hero>();

	private readonly MbEvent<Hero> _heroReachesTeenAgeEvent = new MbEvent<Hero>();

	private readonly MbEvent<Hero, Hero> _characterDefeated = new MbEvent<Hero, Hero>();

	private readonly MbEvent<Kingdom, Clan> _rulingClanChanged = new MbEvent<Kingdom, Clan>();

	private readonly MbEvent<PartyBase, Hero> _heroPrisonerTaken = new MbEvent<PartyBase, Hero>();

	private readonly MbEvent<Hero, PartyBase, IFaction, EndCaptivityDetail, bool> _heroPrisonerReleased = new MbEvent<Hero, PartyBase, IFaction, EndCaptivityDetail, bool>();

	private readonly MbEvent<Hero, bool> _characterBecameFugitiveEvent = new MbEvent<Hero, bool>();

	private readonly MbEvent<Hero> _playerMetHero = new MbEvent<Hero>();

	private readonly MbEvent<Hero> _playerLearnsAboutHero = new MbEvent<Hero>();

	private readonly MbEvent<Hero, int, bool> _renownGained = new MbEvent<Hero, int, bool>();

	private readonly MbEvent<IFaction, float> _crimeRatingChanged = new MbEvent<IFaction, float>();

	private readonly MbEvent<Hero> _newCompanionAdded = new MbEvent<Hero>();

	private readonly MbEvent<IMission> _afterMissionStarted = new MbEvent<IMission>();

	private readonly MbEvent<MenuCallbackArgs> _gameMenuOpened = new MbEvent<MenuCallbackArgs>();

	private readonly MbEvent<MenuCallbackArgs> _afterGameMenuInitializedEvent = new MbEvent<MenuCallbackArgs>();

	private readonly MbEvent<MenuCallbackArgs> _beforeGameMenuOpenedEvent = new MbEvent<MenuCallbackArgs>();

	private readonly MbEvent<IFaction, IFaction, MakePeaceAction.MakePeaceDetail> _makePeace = new MbEvent<IFaction, IFaction, MakePeaceAction.MakePeaceDetail>();

	private readonly MbEvent<Kingdom> _kingdomDestroyed = new MbEvent<Kingdom>();

	private readonly ReferenceMBEvent<Kingdom, bool> _canKingdomBeDiscontinued = new ReferenceMBEvent<Kingdom, bool>();

	private readonly MbEvent<Kingdom> _kingdomCreated = new MbEvent<Kingdom>();

	private readonly MbEvent<Village> _villageBecomeNormal = new MbEvent<Village>();

	private readonly MbEvent<Village> _villageBeingRaided = new MbEvent<Village>();

	private readonly MbEvent<Village> _villageLooted = new MbEvent<Village>();

	private readonly MbEvent<Hero, RemoveCompanionAction.RemoveCompanionDetail> _companionRemoved = new MbEvent<Hero, RemoveCompanionAction.RemoveCompanionDetail>();

	private readonly MbEvent<IAgent> _onAgentJoinedConversationEvent = new MbEvent<IAgent>();

	private readonly MbEvent<IEnumerable<CharacterObject>> _onConversationEnded = new MbEvent<IEnumerable<CharacterObject>>();

	private readonly MbEvent<MapEvent> _mapEventEnded = new MbEvent<MapEvent>();

	private readonly MbEvent<MapEvent, PartyBase, PartyBase> _mapEventStarted = new MbEvent<MapEvent, PartyBase, PartyBase>();

	private readonly MbEvent<Settlement, FlattenedTroopRoster, Hero, bool> _prisonersChangeInSettlement = new MbEvent<Settlement, FlattenedTroopRoster, Hero, bool>();

	private readonly MbEvent<Hero, BoardGameHelper.BoardGameState> _onPlayerBoardGameOver = new MbEvent<Hero, BoardGameHelper.BoardGameState>();

	private readonly MbEvent<Hero> _onRansomOfferedToPlayer = new MbEvent<Hero>();

	private readonly MbEvent<Hero> _onRansomOfferCancelled = new MbEvent<Hero>();

	private readonly MbEvent<IFaction, int, int> _onPeaceOfferedToPlayer = new MbEvent<IFaction, int, int>();

	private readonly MbEvent<Kingdom, Kingdom> _onTradeAgreementSignedEvent = new MbEvent<Kingdom, Kingdom>();

	private readonly MbEvent<IFaction> _onPeaceOfferResolved = new MbEvent<IFaction>();

	private readonly MbEvent<Hero, Hero> _onMarriageOfferedToPlayerEvent = new MbEvent<Hero, Hero>();

	private readonly MbEvent<Hero, Hero> _onMarriageOfferCanceledEvent = new MbEvent<Hero, Hero>();

	private readonly MbEvent<Kingdom> _onVassalOrMercenaryServiceOfferedToPlayerEvent = new MbEvent<Kingdom>();

	private readonly MbEvent<Kingdom> _onVassalOrMercenaryServiceOfferCanceledEvent = new MbEvent<Kingdom>();

	private readonly MbEvent<Clan, StartMercenaryServiceAction.StartMercenaryServiceActionDetails> _onMercenaryServiceStartedEvent = new MbEvent<Clan, StartMercenaryServiceAction.StartMercenaryServiceActionDetails>();

	private readonly MbEvent<Clan, EndMercenaryServiceAction.EndMercenaryServiceActionDetails> _onMercenaryServiceEndedEvent = new MbEvent<Clan, EndMercenaryServiceAction.EndMercenaryServiceActionDetails>();

	private readonly MbEvent<IMission> _onMissionStartedEvent = new MbEvent<IMission>();

	private readonly MbEvent _beforeMissionOpenedEvent = new MbEvent();

	private readonly MbEvent<PartyBase> _onPartyRemovedEvent = new MbEvent<PartyBase>();

	private readonly MbEvent<PartyBase> _onPartySizeChangedEvent = new MbEvent<PartyBase>();

	private readonly MbEvent<Settlement, bool, Hero, Hero, Hero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail> _onSettlementOwnerChangedEvent = new MbEvent<Settlement, bool, Hero, Hero, Hero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail>();

	private readonly MbEvent<Town, Hero, Hero> _onGovernorChangedEvent = new MbEvent<Town, Hero, Hero>();

	private readonly MbEvent<MobileParty, Settlement> _onSettlementLeftEvent = new MbEvent<MobileParty, Settlement>();

	private readonly MbEvent _weeklyTickEvent = new MbEvent();

	private readonly MbEvent _dailyTickEvent = new MbEvent();

	private readonly MbEvent<MobileParty> _dailyTickPartyEvent = new MbEvent<MobileParty>();

	private readonly MbEvent<Town> _dailyTickTownEvent = new MbEvent<Town>();

	private readonly MbEvent<Settlement> _dailyTickSettlementEvent = new MbEvent<Settlement>();

	private readonly MbEvent<Hero> _dailyTickHeroEvent = new MbEvent<Hero>();

	private readonly MbEvent<Clan> _dailyTickClanEvent = new MbEvent<Clan>();

	private readonly MbEvent<List<CampaignTutorial>> _collectAvailableTutorialsEvent = new MbEvent<List<CampaignTutorial>>();

	private readonly MbEvent<string> _onTutorialCompletedEvent = new MbEvent<string>();

	private readonly MbEvent<Town, Building, int> _onBuildingLevelChangedEvent = new MbEvent<Town, Building, int>();

	private readonly MbEvent _hourlyTickEvent = new MbEvent();

	private readonly MbEvent _quarterHourlyTickEvent = new MbEvent();

	private readonly MbEvent<MobileParty> _hourlyTickPartyEvent = new MbEvent<MobileParty>();

	private readonly MbEvent<Settlement> _hourlyTickSettlementEvent = new MbEvent<Settlement>();

	private readonly MbEvent<Clan> _hourlyTickClanEvent = new MbEvent<Clan>();

	private readonly MbEvent<float> _tickEvent = new MbEvent<float>();

	private readonly MbEvent<CampaignGameStarter> _onSessionLaunchedEvent = new MbEvent<CampaignGameStarter>();

	private readonly MbEvent<CampaignGameStarter> _onAfterSessionLaunchedEvent = new MbEvent<CampaignGameStarter>();

	public const int OnNewGameCreatedPartialFollowUpEventMaxIndex = 100;

	private readonly MbEvent<CampaignGameStarter> _onNewGameCreatedEvent = new MbEvent<CampaignGameStarter>();

	private readonly MbEvent<CampaignGameStarter, int> _onNewGameCreatedPartialFollowUpEvent = new MbEvent<CampaignGameStarter, int>();

	private readonly MbEvent<CampaignGameStarter> _onNewGameCreatedPartialFollowUpEndEvent = new MbEvent<CampaignGameStarter>();

	private readonly MbEvent<CampaignGameStarter> _onGameEarlyLoadedEvent = new MbEvent<CampaignGameStarter>();

	private readonly MbEvent<CampaignGameStarter> _onGameLoadedEvent = new MbEvent<CampaignGameStarter>();

	private readonly MbEvent _onGameLoadFinishedEvent = new MbEvent();

	private readonly MbEvent<MobileParty, PartyThinkParams> _aiHourlyTickEvent = new MbEvent<MobileParty, PartyThinkParams>();

	private readonly MbEvent<MobileParty> _tickPartialHourlyAiEvent = new MbEvent<MobileParty>();

	private readonly MbEvent<MobileParty> _onPartyJoinedArmyEvent = new MbEvent<MobileParty>();

	private readonly MbEvent<MobileParty> _onPartyRemovedFromArmyEvent = new MbEvent<MobileParty>();

	private readonly MbEvent _onPlayerArmyLeaderChangedBehaviorEvent = new MbEvent();

	private readonly MbEvent<IMission> _onMissionEndedEvent = new MbEvent<IMission>();

	private readonly MbEvent<MobileParty> _onQuarterDailyPartyTick = new MbEvent<MobileParty>();

	private readonly MbEvent<MapEvent> _onPlayerBattleEndEvent = new MbEvent<MapEvent>();

	private readonly MbEvent<CharacterObject, int> _onUnitRecruitedEvent = new MbEvent<CharacterObject, int>();

	private readonly MbEvent<Hero> _onChildConceived = new MbEvent<Hero>();

	private readonly MbEvent<Hero, List<Hero>, int> _onGivenBirthEvent = new MbEvent<Hero, List<Hero>, int>();

	private readonly MbEvent<float> _missionTickEvent = new MbEvent<float>();

	private MbEvent _armyOverlaySetDirty = new MbEvent();

	private readonly MbEvent<int> _playerDesertedBattle = new MbEvent<int>();

	private MbEvent<PartyBase> _partyVisibilityChanged = new MbEvent<PartyBase>();

	private readonly MbEvent<Track> _trackDetectedEvent = new MbEvent<Track>();

	private readonly MbEvent<Track> _trackLostEvent = new MbEvent<Track>();

	private readonly MbEvent<Dictionary<string, int>> _locationCharactersAreReadyToSpawn = new MbEvent<Dictionary<string, int>>();

	private readonly ReferenceMBEvent<MatrixFrame> _onBeforePlayerAgentSpawn = new ReferenceMBEvent<MatrixFrame>();

	private readonly MbEvent _onPlayerAgentSpawned = new MbEvent();

	private readonly MbEvent _locationCharactersSimulatedSpawned = new MbEvent();

	private readonly MbEvent<CharacterObject, CharacterObject, int> _playerUpgradedTroopsEvent = new MbEvent<CharacterObject, CharacterObject, int>();

	private readonly MbEvent<CharacterObject, CharacterObject, PartyBase, WeaponComponentData, bool, int> _onHeroCombatHitEvent = new MbEvent<CharacterObject, CharacterObject, PartyBase, WeaponComponentData, bool, int>();

	private readonly MbEvent<CharacterObject> _characterPortraitPopUpOpenedEvent = new MbEvent<CharacterObject>();

	private CampaignTimeControlMode _timeControlModeBeforePopUpOpened;

	private readonly MbEvent _characterPortraitPopUpClosedEvent = new MbEvent();

	private readonly MbEvent<Hero> _playerStartTalkFromMenu = new MbEvent<Hero>();

	private readonly MbEvent<GameMenu, GameMenuOption> _gameMenuOptionSelectedEvent = new MbEvent<GameMenu, GameMenuOption>();

	private readonly MbEvent<CharacterObject> _playerStartRecruitmentEvent = new MbEvent<CharacterObject>();

	private readonly MbEvent<Hero, Hero> _onBeforePlayerCharacterChangedEvent = new MbEvent<Hero, Hero>();

	private readonly MbEvent<Hero, Hero, MobileParty, bool> _onPlayerCharacterChangedEvent = new MbEvent<Hero, Hero, MobileParty, bool>();

	private readonly MbEvent<Hero, Hero> _onClanLeaderChangedEvent = new MbEvent<Hero, Hero>();

	private readonly MbEvent<SiegeEvent> _onSiegeEventStartedEvent = new MbEvent<SiegeEvent>();

	private readonly MbEvent _onPlayerSiegeStartedEvent = new MbEvent();

	private readonly MbEvent<SiegeEvent> _onSiegeEventEndedEvent = new MbEvent<SiegeEvent>();

	private readonly MbEvent<MobileParty, Settlement, SiegeAftermathAction.SiegeAftermath, Clan, Dictionary<MobileParty, float>> _siegeAftermathAppliedEvent = new MbEvent<MobileParty, Settlement, SiegeAftermathAction.SiegeAftermath, Clan, Dictionary<MobileParty, float>>();

	private readonly MbEvent<MobileParty, Settlement, BattleSideEnum, SiegeEngineType, SiegeBombardTargets> _onSiegeBombardmentHitEvent = new MbEvent<MobileParty, Settlement, BattleSideEnum, SiegeEngineType, SiegeBombardTargets>();

	private readonly MbEvent<MobileParty, Settlement, BattleSideEnum, SiegeEngineType, bool> _onSiegeBombardmentWallHitEvent = new MbEvent<MobileParty, Settlement, BattleSideEnum, SiegeEngineType, bool>();

	private readonly MbEvent<MobileParty, Settlement, BattleSideEnum, SiegeEngineType> _onSiegeEngineDestroyedEvent = new MbEvent<MobileParty, Settlement, BattleSideEnum, SiegeEngineType>();

	private readonly MbEvent<List<TradeRumor>, Settlement> _onTradeRumorIsTakenEvent = new MbEvent<List<TradeRumor>, Settlement>();

	private readonly MbEvent<Hero> _onCheckForIssueEvent = new MbEvent<Hero>();

	private readonly MbEvent<IssueBase, IssueBase.IssueUpdateDetails, Hero> _onIssueUpdatedEvent = new MbEvent<IssueBase, IssueBase.IssueUpdateDetails, Hero>();

	private readonly MbEvent<MobileParty, TroopRoster> _onTroopsDesertedEvent = new MbEvent<MobileParty, TroopRoster>();

	private readonly MbEvent<Hero, Settlement, Hero, CharacterObject, int> _onTroopRecruitedEvent = new MbEvent<Hero, Settlement, Hero, CharacterObject, int>();

	private readonly MbEvent<Hero, Settlement, TroopRoster> _onTroopGivenToSettlementEvent = new MbEvent<Hero, Settlement, TroopRoster>();

	private readonly MbEvent<PartyBase, PartyBase, ItemRosterElement, int, Settlement> _onItemSoldEvent = new MbEvent<PartyBase, PartyBase, ItemRosterElement, int, Settlement>();

	private readonly MbEvent<MobileParty, Town, List<(EquipmentElement, int)>> _onCaravanTransactionCompletedEvent = new MbEvent<MobileParty, Town, List<(EquipmentElement, int)>>();

	private readonly MbEvent<PartyBase, PartyBase, TroopRoster> _onPrisonerSoldEvent = new MbEvent<PartyBase, PartyBase, TroopRoster>();

	private readonly MbEvent<MobileParty> _onPartyDisbandStartedEvent = new MbEvent<MobileParty>();

	private readonly MbEvent<MobileParty, Settlement> _onPartyDisbandedEvent = new MbEvent<MobileParty, Settlement>();

	private readonly MbEvent<MobileParty> _onPartyDisbandCanceledEvent = new MbEvent<MobileParty>();

	private readonly MbEvent<PartyBase, PartyBase> _hideoutSpottedEvent = new MbEvent<PartyBase, PartyBase>();

	private readonly MbEvent<Settlement> _hideoutDeactivatedEvent = new MbEvent<Settlement>();

	private readonly MbEvent<Hero, Hero, float> _heroSharedFoodWithAnotherHeroEvent = new MbEvent<Hero, Hero, float>();

	private readonly MbEvent<List<(ItemRosterElement, int)>, List<(ItemRosterElement, int)>, bool> _playerInventoryExchangeEvent = new MbEvent<List<(ItemRosterElement, int)>, List<(ItemRosterElement, int)>, bool>();

	private readonly MbEvent<ItemRoster> _onItemsDiscardedByPlayerEvent = new MbEvent<ItemRoster>();

	private readonly MbEvent<Tuple<PersuasionOptionArgs, PersuasionOptionResult>> _persuasionProgressCommittedEvent = new MbEvent<Tuple<PersuasionOptionArgs, PersuasionOptionResult>>();

	private readonly MbEvent<QuestBase, QuestBase.QuestCompleteDetails> _onQuestCompletedEvent = new MbEvent<QuestBase, QuestBase.QuestCompleteDetails>();

	private readonly MbEvent<QuestBase> _onQuestStartedEvent = new MbEvent<QuestBase>();

	private readonly MbEvent<ItemObject, Settlement, int> _itemProducedEvent = new MbEvent<ItemObject, Settlement, int>();

	private readonly MbEvent<ItemObject, Settlement, int> _itemConsumedEvent = new MbEvent<ItemObject, Settlement, int>();

	private readonly MbEvent<MobileParty> _onPartyConsumedFoodEvent = new MbEvent<MobileParty>();

	private readonly MbEvent<Hero, Hero, KillCharacterAction.KillCharacterActionDetail, bool> _onBeforeMainCharacterDiedEvent = new MbEvent<Hero, Hero, KillCharacterAction.KillCharacterActionDetail, bool>();

	private readonly MbEvent<IssueBase> _onNewIssueCreatedEvent = new MbEvent<IssueBase>();

	private readonly MbEvent<IssueBase, Hero> _onIssueOwnerChangedEvent = new MbEvent<IssueBase, Hero>();

	private readonly MbEvent _onGameOverEvent = new MbEvent();

	private readonly MbEvent<Settlement, MobileParty, bool, MapEvent.BattleTypes> _siegeCompletedEvent = new MbEvent<Settlement, MobileParty, bool, MapEvent.BattleTypes>();

	private readonly MbEvent<Settlement, MobileParty, bool, MapEvent.BattleTypes> _afterSiegeCompletedEvent = new MbEvent<Settlement, MobileParty, bool, MapEvent.BattleTypes>();

	private readonly MbEvent<SiegeEvent, BattleSideEnum, SiegeEngineType> _siegeEngineBuiltEvent = new MbEvent<SiegeEvent, BattleSideEnum, SiegeEngineType>();

	private readonly MbEvent<BattleSideEnum, RaidEventComponent> _raidCompletedEvent = new MbEvent<BattleSideEnum, RaidEventComponent>();

	private readonly MbEvent<BattleSideEnum, ForceVolunteersEventComponent> _forceVolunteersCompletedEvent = new MbEvent<BattleSideEnum, ForceVolunteersEventComponent>();

	private readonly MbEvent<BattleSideEnum, ForceSuppliesEventComponent> _forceSuppliesCompletedEvent = new MbEvent<BattleSideEnum, ForceSuppliesEventComponent>();

	private readonly MbEvent<BattleSideEnum, HideoutEventComponent, HideoutEventComponent.HideoutBattleEndState> _hideoutBattleCompletedEvent = new MbEvent<BattleSideEnum, HideoutEventComponent, HideoutEventComponent.HideoutBattleEndState>();

	private readonly MbEvent<Clan> _onClanDestroyedEvent = new MbEvent<Clan>();

	private readonly MbEvent<ItemObject, ItemModifier, bool> _onNewItemCraftedEvent = new MbEvent<ItemObject, ItemModifier, bool>();

	private readonly MbEvent<CraftingPiece> _craftingPartUnlockedEvent = new MbEvent<CraftingPiece>();

	private readonly MbEvent<Workshop> _onWorkshopInitializedEvent = new MbEvent<Workshop>();

	private readonly MbEvent<Workshop, Hero> _onWorkshopOwnerChangedEvent = new MbEvent<Workshop, Hero>();

	private readonly MbEvent<Workshop> _onWorkshopTypeChangedEvent = new MbEvent<Workshop>();

	private readonly MbEvent _onBeforeSaveEvent = new MbEvent();

	private readonly MbEvent _onSaveStartedEvent = new MbEvent();

	private readonly MbEvent<bool, string> _onSaveOverEvent = new MbEvent<bool, string>();

	private readonly MbEvent<List<KeyValuePair<string, string>>> _collectMetadataEntriesEvent = new MbEvent<List<KeyValuePair<string, string>>>();

	private readonly MbEvent<FlattenedTroopRoster> _onPrisonerTakenEvent = new MbEvent<FlattenedTroopRoster>();

	private readonly MbEvent<FlattenedTroopRoster> _onPrisonerReleasedEvent = new MbEvent<FlattenedTroopRoster>();

	private readonly MbEvent<FlattenedTroopRoster> _onMainPartyPrisonerRecruitedEvent = new MbEvent<FlattenedTroopRoster>();

	private readonly MbEvent<MobileParty, FlattenedTroopRoster, Settlement> _onPrisonerDonatedToSettlementEvent = new MbEvent<MobileParty, FlattenedTroopRoster, Settlement>();

	private readonly MbEvent<Hero, EquipmentElement> _onEquipmentSmeltedByHero = new MbEvent<Hero, EquipmentElement>();

	private readonly MbEvent<int> _onPlayerTradeProfit = new MbEvent<int>();

	private readonly MbEvent<Hero, Clan> _onHeroChangedClan = new MbEvent<Hero, Clan>();

	private readonly MbEvent<Hero, HeroGetsBusyReasons> _onHeroGetsBusy = new MbEvent<Hero, HeroGetsBusyReasons>();

	private readonly MbEvent<PartyBase, ItemRoster> _onCollectLootItems = new MbEvent<PartyBase, ItemRoster>();

	private readonly MbEvent<PartyBase, PartyBase, ItemRoster> _onLootDistributedToPartyEvent = new MbEvent<PartyBase, PartyBase, ItemRoster>();

	private readonly MbEvent<Hero, Settlement, MobileParty, TeleportHeroAction.TeleportationDetail> _onHeroTeleportationRequestedEvent = new MbEvent<Hero, Settlement, MobileParty, TeleportHeroAction.TeleportationDetail>();

	private readonly MbEvent<MobileParty> _onPartyLeaderChangeOfferCanceledEvent = new MbEvent<MobileParty>();

	private readonly MbEvent<MobileParty, Hero> _onPartyLeaderChangedEvent = new MbEvent<MobileParty, Hero>();

	private readonly MbEvent<Clan, float> _onClanInfluenceChangedEvent = new MbEvent<Clan, float>();

	private readonly MbEvent<CharacterObject> _onPlayerPartyKnockedOrKilledTroopEvent = new MbEvent<CharacterObject>();

	private readonly MbEvent<DefaultClanFinanceModel.AssetIncomeType, int> _onPlayerEarnedGoldFromAssetEvent = new MbEvent<DefaultClanFinanceModel.AssetIncomeType, int>();

	private readonly MbEvent<Clan, IFaction> _onClanEarnedGoldFromTributeEvent = new MbEvent<Clan, IFaction>();

	private readonly MbEvent _onMainPartyStarving = new MbEvent();

	private readonly MbEvent<Town, bool> _onPlayerJoinedTournamentEvent = new MbEvent<Town, bool>();

	private readonly MbEvent<Hero> _onHeroUnregisteredEvent = new MbEvent<Hero>();

	private readonly MbEvent _onConfigChanged = new MbEvent();

	private readonly MbEvent<Town, CraftingOrder, ItemObject, Hero> _onCraftingOrderCompleted = new MbEvent<Town, CraftingOrder, ItemObject, Hero>();

	private readonly MbEvent<Hero, Crafting.RefiningFormula> _onItemsRefined = new MbEvent<Hero, Crafting.RefiningFormula>();

	private readonly MbEvent<Dictionary<Hero, int>> _onHeirSelectionRequested = new MbEvent<Dictionary<Hero, int>>();

	private readonly MbEvent<Hero> _onHeirSelectionOver = new MbEvent<Hero>();

	private readonly MbEvent<MobileParty> _onMobilePartyRaftStateChanged = new MbEvent<MobileParty>();

	private readonly MbEvent<CharacterCreationManager> _onCharacterCreationInitialized = new MbEvent<CharacterCreationManager>();

	private readonly MbEvent<PartyBase, Ship, DestroyShipAction.ShipDestroyDetail> _onShipDestroyedEvent = new MbEvent<PartyBase, Ship, DestroyShipAction.ShipDestroyDetail>();

	private readonly MbEvent<Ship, PartyBase, ChangeShipOwnerAction.ShipOwnerChangeDetail> _onShipOwnerChangedEvent = new MbEvent<Ship, PartyBase, ChangeShipOwnerAction.ShipOwnerChangeDetail>();

	private readonly MbEvent<Ship, Settlement> _onShipRepairedEvent = new MbEvent<Ship, Settlement>();

	private readonly MbEvent<Ship, Settlement> _onShipCreatedEvent = new MbEvent<Ship, Settlement>();

	private readonly MbEvent<Figurehead> _onFigureheadUnlockedEvent = new MbEvent<Figurehead>();

	private readonly MbEvent<MobileParty, Army> _onPartyLeftArmyEvent = new MbEvent<MobileParty, Army>();

	private readonly MbEvent<PartyBase> _onPartyAddedToMapEventEvent = new MbEvent<PartyBase>();

	private readonly MbEvent<Incident> _onIncidentResolvedEvent = new MbEvent<Incident>();

	private readonly MbEvent<MobileParty> _onMobilePartyNavigationStateChangedEvent = new MbEvent<MobileParty>();

	private readonly MbEvent<MobileParty> _onMobilePartyJoinedToSiegeEventEvent = new MbEvent<MobileParty>();

	private readonly MbEvent<MobileParty> _onMobilePartyLeftSiegeEventEvent = new MbEvent<MobileParty>();

	private readonly MbEvent<SiegeEvent> _onBlockadeActivatedEvent = new MbEvent<SiegeEvent>();

	private readonly MbEvent<SiegeEvent> _onBlockadeDeactivatedEvent = new MbEvent<SiegeEvent>();

	private readonly MbEvent<MapMarker> _onMapMarkerCreatedEvent = new MbEvent<MapMarker>();

	private readonly MbEvent<MapMarker> _onMapMarkerRemovedEvent = new MbEvent<MapMarker>();

	private readonly MbEvent<Kingdom, Kingdom> _onAllianceStartedEvent = new MbEvent<Kingdom, Kingdom>();

	private readonly MbEvent<Kingdom, Kingdom> _onAllianceEndedEvent = new MbEvent<Kingdom, Kingdom>();

	private readonly MbEvent<Kingdom, Kingdom, Kingdom> _onCallToWarAgreementStartedEvent = new MbEvent<Kingdom, Kingdom, Kingdom>();

	private readonly MbEvent<Kingdom, Kingdom, Kingdom> _onCallToWarAgreementEndedEvent = new MbEvent<Kingdom, Kingdom, Kingdom>();

	private readonly ReferenceMBEvent<Hero, bool> _canHeroLeadPartyEvent = new ReferenceMBEvent<Hero, bool>();

	private readonly ReferenceMBEvent<Hero, bool> _canMarryEvent = new ReferenceMBEvent<Hero, bool>();

	private readonly ReferenceMBEvent<Hero, bool> _canHeroEquipmentBeChangedEvent = new ReferenceMBEvent<Hero, bool>();

	private readonly ReferenceMBEvent<Hero, bool> _canBeGovernorOrHavePartyRoleEvent = new ReferenceMBEvent<Hero, bool>();

	private readonly ReferenceMBEvent<Hero, KillCharacterAction.KillCharacterActionDetail, bool> _canHeroDieEvent = new ReferenceMBEvent<Hero, KillCharacterAction.KillCharacterActionDetail, bool>();

	private readonly ReferenceMBEvent<Hero, bool> _canPlayerMeetWithHeroAfterConversationEvent = new ReferenceMBEvent<Hero, bool>();

	private readonly ReferenceMBEvent<Hero, bool> _canHeroBecomePrisonerEvent = new ReferenceMBEvent<Hero, bool>();

	private readonly ReferenceMBEvent<Hero, bool> _canMoveToSettlementEvent = new ReferenceMBEvent<Hero, bool>();

	private readonly ReferenceMBEvent<Hero, bool> _canHaveCampaignIssues = new ReferenceMBEvent<Hero, bool>();

	private readonly ReferenceMBEvent<Settlement, object, int> _isSettlementBusy = new ReferenceMBEvent<Settlement, object, int>();

	private readonly MbEvent<IFaction> _onMapEventContinuityNeedsUpdate = new MbEvent<IFaction>();

	private static CampaignEvents Instance => Campaign.Current.CampaignEvents;

	public static IMbEvent OnPlayerBodyPropertiesChangedEvent => Instance._onPlayerBodyPropertiesChangedEvent;

	public static IMbEvent<BarterData> BarterablesRequested => Instance._barterablesRequested;

	public static IMbEvent<Hero, bool> HeroLevelledUp => Instance._heroLevelledUp;

	public static IMbEvent<BanditPartyComponent, Hideout> OnHomeHideoutChangedEvent => Instance._onHomeHideoutChangedEvent;

	public static IMbEvent<Hero, SkillObject, int, bool> HeroGainedSkill => Instance._heroGainedSkill;

	public static IMbEvent OnCharacterCreationIsOverEvent => Instance._onCharacterCreationIsOverEvent;

	public static IMbEvent<Hero, bool> HeroCreated => Instance._onHeroCreated;

	public static IMbEvent<Hero, Hero.CharacterStates> OnHeroActivatedEvent => Instance._onHeroActivatedEvent;

	public static IMbEvent<Hero, Occupation> HeroOccupationChangedEvent => Instance._heroOccupationChangedEvent;

	public static IMbEvent<Hero> HeroWounded => Instance._onHeroWounded;

	public static IMbEvent<Hero, Hero, List<Barterable>> OnBarterAcceptedEvent => Instance._onBarterAcceptedEvent;

	public static IMbEvent<Hero, Hero, List<Barterable>> OnBarterCanceledEvent => Instance._onBarterCanceledEvent;

	public static IMbEvent<Hero, Hero, int, bool, ChangeRelationAction.ChangeRelationDetail, Hero, Hero> HeroRelationChanged => Instance._heroRelationChanged;

	public static IMbEvent<QuestBase, bool> QuestLogAddedEvent => Instance._questLogAddedEvent;

	public static IMbEvent<IssueBase, bool> IssueLogAddedEvent => Instance._issueLogAddedEvent;

	public static IMbEvent<Clan, bool> ClanTierIncrease => Instance._clanTierIncrease;

	public static IMbEvent<Clan, Kingdom, Kingdom, ChangeKingdomAction.ChangeKingdomActionDetail, bool> OnClanChangedKingdomEvent => Instance._clanChangedKingdom;

	public static IMbEvent<Clan, Kingdom, Kingdom> OnClanDefectedEvent => Instance._onClanDefected;

	public static IMbEvent<Clan, bool> OnClanCreatedEvent => Instance._onClanCreatedEvent;

	public static IMbEvent<Hero, MobileParty> OnHeroJoinedPartyEvent => Instance._onHeroJoinedPartyEvent;

	public static IMbEvent<(Hero, PartyBase), (Hero, PartyBase), (int, string), bool> HeroOrPartyTradedGold => Instance._heroOrPartyTradedGold;

	public static IMbEvent<(Hero, PartyBase), (Hero, PartyBase), ItemRosterElement, bool> HeroOrPartyGaveItem => Instance._heroOrPartyGaveItem;

	public static IMbEvent<MobileParty> BanditPartyRecruited => Instance._banditPartyRecruited;

	public static IMbEvent<KingdomDecision, bool> KingdomDecisionAdded => Instance._kingdomDecisionAdded;

	public static IMbEvent<KingdomDecision, bool> KingdomDecisionCancelled => Instance._kingdomDecisionCancelled;

	public static IMbEvent<KingdomDecision, DecisionOutcome, bool> KingdomDecisionConcluded => Instance._kingdomDecisionConcluded;

	public static IMbEvent<MobileParty> PartyAttachedAnotherParty => Instance._partyAttachedParty;

	public static IMbEvent<MobileParty> NearbyPartyAddedToPlayerMapEvent => Instance._nearbyPartyAddedToPlayerMapEvent;

	public static IMbEvent<Army> ArmyCreated => Instance._armyCreated;

	public static IMbEvent<Army, Army.ArmyDispersionReason, bool> ArmyDispersed => Instance._armyDispersed;

	public static IMbEvent<Army, IMapPoint> ArmyGathered => Instance._armyGathered;

	public static IMbEvent<Hero, PerkObject> PerkOpenedEvent => Instance._perkOpenedEvent;

	public static IMbEvent<Hero, PerkObject> PerkResetEvent => Instance._perkResetEvent;

	public static IMbEvent<TraitObject, int> PlayerTraitChangedEvent => Instance._playerTraitChangedEvent;

	public static IMbEvent<Village, Village.VillageStates, Village.VillageStates, MobileParty> VillageStateChanged => Instance._villageStateChanged;

	public static IMbEvent<MobileParty, Settlement, Hero> SettlementEntered => Instance._settlementEntered;

	public static IMbEvent<MobileParty, Settlement, Hero> AfterSettlementEntered => Instance._afterSettlementEntered;

	public static IMbEvent<MobileParty, Settlement, Hero> BeforeSettlementEnteredEvent => Instance._beforeSettlementEntered;

	public static IMbEvent<Town, CharacterObject, CharacterObject> MercenaryTroopChangedInTown => Instance._mercenaryTroopChangedInTown;

	public static IMbEvent<Town, int, int> MercenaryNumberChangedInTown => Instance._mercenaryNumberChangedInTown;

	public static IMbEvent<Alley, Hero, Hero> AlleyOwnerChanged => Instance._alleyOwnerChanged;

	public static IMbEvent<Alley, TroopRoster> AlleyOccupiedByPlayer => Instance._alleyOccupiedByPlayer;

	public static IMbEvent<Alley> AlleyClearedByPlayer => Instance._alleyClearedByPlayer;

	public static IMbEvent<Hero, Hero, Romance.RomanceLevelEnum> RomanticStateChanged => Instance._romanticStateChanged;

	public static IMbEvent<Hero, Hero, bool> BeforeHeroesMarried => Instance._beforeHeroesMarried;

	public static IMbEvent<int, Town> PlayerEliminatedFromTournament => Instance._playerEliminatedFromTournament;

	public static IMbEvent<Town> PlayerStartedTournamentMatch => Instance._playerStartedTournamentMatch;

	public static IMbEvent<Town> TournamentStarted => Instance._tournamentStarted;

	public static IMbEvent<IFaction, IFaction, DeclareWarAction.DeclareWarDetail> WarDeclared => Instance._warDeclared;

	public static IMbEvent<CharacterObject, MBReadOnlyList<CharacterObject>, Town, ItemObject> TournamentFinished => Instance._tournamentFinished;

	public static IMbEvent<Town> TournamentCancelled => Instance._tournamentCancelled;

	public static IMbEvent<PartyBase, PartyBase, object, bool> BattleStarted => Instance._battleStarted;

	public static IMbEvent<Settlement, Clan> RebellionFinished => Instance._rebellionFinished;

	public static IMbEvent<Town, bool> TownRebelliosStateChanged => Instance._townRebelliousStateChanged;

	public static IMbEvent<Settlement, Clan> RebelliousClanDisbandedAtSettlement => Instance._rebelliousClanDisbandedAtSettlement;

	public static IMbEvent<MobileParty, ItemRoster> ItemsLooted => Instance._itemsLooted;

	public static IMbEvent<MobileParty, PartyBase> MobilePartyDestroyed => Instance._mobilePartyDestroyed;

	public static IMbEvent<MobileParty> MobilePartyCreated => Instance._mobilePartyCreated;

	public static IMbEvent<IInteractablePoint> MapInteractableCreated => Instance._mapInteractableCreated;

	public static IMbEvent<IInteractablePoint> MapInteractableDestroyed => Instance._mapInteractableDestroyed;

	public static IMbEvent<MobileParty, bool> MobilePartyQuestStatusChanged => Instance._mobilePartyQuestStatusChanged;

	public static IMbEvent<Hero, Hero, KillCharacterAction.KillCharacterActionDetail, bool> HeroKilledEvent => Instance._heroKilled;

	public static IMbEvent<Hero, Hero, KillCharacterAction.KillCharacterActionDetail, bool> BeforeHeroKilledEvent => Instance._onBeforeHeroKilled;

	public static IMbEvent<Hero, int> ChildEducationCompletedEvent => Instance._childEducationCompleted;

	public static IMbEvent<Hero> HeroComesOfAgeEvent => Instance._heroComesOfAge;

	public static IMbEvent<Hero> HeroGrowsOutOfInfancyEvent => Instance._heroGrowsOutOfInfancyEvent;

	public static IMbEvent<Hero> HeroReachesTeenAgeEvent => Instance._heroReachesTeenAgeEvent;

	public static IMbEvent<Hero, Hero> CharacterDefeated => Instance._characterDefeated;

	public static IMbEvent<Kingdom, Clan> RulingClanChanged => Instance._rulingClanChanged;

	public static IMbEvent<PartyBase, Hero> HeroPrisonerTaken => Instance._heroPrisonerTaken;

	public static IMbEvent<Hero, PartyBase, IFaction, EndCaptivityDetail, bool> HeroPrisonerReleased => Instance._heroPrisonerReleased;

	public static IMbEvent<Hero, bool> CharacterBecameFugitiveEvent => Instance._characterBecameFugitiveEvent;

	public static IMbEvent<Hero> OnPlayerMetHeroEvent => Instance._playerMetHero;

	public static IMbEvent<Hero> OnPlayerLearnsAboutHeroEvent => Instance._playerLearnsAboutHero;

	public static IMbEvent<Hero, int, bool> RenownGained => Instance._renownGained;

	public static IMbEvent<IFaction, float> CrimeRatingChanged => Instance._crimeRatingChanged;

	public static IMbEvent<Hero> NewCompanionAdded => Instance._newCompanionAdded;

	public static IMbEvent<IMission> AfterMissionStarted => Instance._afterMissionStarted;

	public static IMbEvent<MenuCallbackArgs> GameMenuOpened => Instance._gameMenuOpened;

	public static IMbEvent<MenuCallbackArgs> AfterGameMenuInitializedEvent => Instance._afterGameMenuInitializedEvent;

	public static IMbEvent<MenuCallbackArgs> BeforeGameMenuOpenedEvent => Instance._beforeGameMenuOpenedEvent;

	public static IMbEvent<IFaction, IFaction, MakePeaceAction.MakePeaceDetail> MakePeace => Instance._makePeace;

	public static IMbEvent<Kingdom> KingdomDestroyedEvent => Instance._kingdomDestroyed;

	public static ReferenceIMBEvent<Kingdom, bool> CanKingdomBeDiscontinuedEvent => Instance._canKingdomBeDiscontinued;

	public static IMbEvent<Kingdom> KingdomCreatedEvent => Instance._kingdomCreated;

	public static IMbEvent<Village> VillageBecomeNormal => Instance._villageBecomeNormal;

	public static IMbEvent<Village> VillageBeingRaided => Instance._villageBeingRaided;

	public static IMbEvent<Village> VillageLooted => Instance._villageLooted;

	public static IMbEvent<Hero, RemoveCompanionAction.RemoveCompanionDetail> CompanionRemoved => Instance._companionRemoved;

	public static IMbEvent<IAgent> OnAgentJoinedConversationEvent => Instance._onAgentJoinedConversationEvent;

	public static IMbEvent<IEnumerable<CharacterObject>> ConversationEnded => Instance._onConversationEnded;

	public static IMbEvent<MapEvent> MapEventEnded => Instance._mapEventEnded;

	public static IMbEvent<MapEvent, PartyBase, PartyBase> MapEventStarted => Instance._mapEventStarted;

	public static IMbEvent<Settlement, FlattenedTroopRoster, Hero, bool> PrisonersChangeInSettlement => Instance._prisonersChangeInSettlement;

	public static IMbEvent<Hero, BoardGameHelper.BoardGameState> OnPlayerBoardGameOverEvent => Instance._onPlayerBoardGameOver;

	public static IMbEvent<Hero> OnRansomOfferedToPlayerEvent => Instance._onRansomOfferedToPlayer;

	public static IMbEvent<Hero> OnRansomOfferCancelledEvent => Instance._onRansomOfferCancelled;

	public static IMbEvent<IFaction, int, int> OnPeaceOfferedToPlayerEvent => Instance._onPeaceOfferedToPlayer;

	public static IMbEvent<Kingdom, Kingdom> OnTradeAgreementSignedEvent => Instance._onTradeAgreementSignedEvent;

	public static IMbEvent<IFaction> OnPeaceOfferResolvedEvent => Instance._onPeaceOfferResolved;

	public static IMbEvent<Hero, Hero> OnMarriageOfferedToPlayerEvent => Instance._onMarriageOfferedToPlayerEvent;

	public static IMbEvent<Hero, Hero> OnMarriageOfferCanceledEvent => Instance._onMarriageOfferCanceledEvent;

	public static IMbEvent<Kingdom> OnVassalOrMercenaryServiceOfferedToPlayerEvent => Instance._onVassalOrMercenaryServiceOfferedToPlayerEvent;

	public static IMbEvent<Kingdom> OnVassalOrMercenaryServiceOfferCanceledEvent => Instance._onVassalOrMercenaryServiceOfferCanceledEvent;

	public static IMbEvent<Clan, StartMercenaryServiceAction.StartMercenaryServiceActionDetails> OnMercenaryServiceStartedEvent => Instance._onMercenaryServiceStartedEvent;

	public static IMbEvent<Clan, EndMercenaryServiceAction.EndMercenaryServiceActionDetails> OnMercenaryServiceEndedEvent => Instance._onMercenaryServiceEndedEvent;

	public static IMbEvent<IMission> OnMissionStartedEvent => Instance._onMissionStartedEvent;

	public static IMbEvent BeforeMissionOpenedEvent => Instance._beforeMissionOpenedEvent;

	public static IMbEvent<PartyBase> OnPartyRemovedEvent => Instance._onPartyRemovedEvent;

	public static IMbEvent<PartyBase> OnPartySizeChangedEvent => Instance._onPartySizeChangedEvent;

	public static IMbEvent<Settlement, bool, Hero, Hero, Hero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail> OnSettlementOwnerChangedEvent => Instance._onSettlementOwnerChangedEvent;

	public static IMbEvent<Town, Hero, Hero> OnGovernorChangedEvent => Instance._onGovernorChangedEvent;

	public static IMbEvent<MobileParty, Settlement> OnSettlementLeftEvent => Instance._onSettlementLeftEvent;

	public static IMbEvent WeeklyTickEvent => Instance._weeklyTickEvent;

	public static IMbEvent DailyTickEvent => Instance._dailyTickEvent;

	public static IMbEvent<MobileParty> DailyTickPartyEvent => Instance._dailyTickPartyEvent;

	public static IMbEvent<Town> DailyTickTownEvent => Instance._dailyTickTownEvent;

	public static IMbEvent<Settlement> DailyTickSettlementEvent => Instance._dailyTickSettlementEvent;

	public static IMbEvent<Hero> DailyTickHeroEvent => Instance._dailyTickHeroEvent;

	public static IMbEvent<Clan> DailyTickClanEvent => Instance._dailyTickClanEvent;

	public static IMbEvent<List<CampaignTutorial>> CollectAvailableTutorialsEvent => Instance._collectAvailableTutorialsEvent;

	public static IMbEvent<string> OnTutorialCompletedEvent => Instance._onTutorialCompletedEvent;

	public static IMbEvent<Town, Building, int> OnBuildingLevelChangedEvent => Instance._onBuildingLevelChangedEvent;

	public static IMbEvent HourlyTickEvent => Instance._hourlyTickEvent;

	public static IMbEvent QuarterHourlyTickEvent => Instance._quarterHourlyTickEvent;

	public static IMbEvent<MobileParty> HourlyTickPartyEvent => Instance._hourlyTickPartyEvent;

	public static IMbEvent<Settlement> HourlyTickSettlementEvent => Instance._hourlyTickSettlementEvent;

	public static IMbEvent<Clan> HourlyTickClanEvent => Instance._hourlyTickClanEvent;

	public static IMbEvent<float> TickEvent => Instance._tickEvent;

	public static IMbEvent<CampaignGameStarter> OnSessionLaunchedEvent => Instance._onSessionLaunchedEvent;

	public static IMbEvent<CampaignGameStarter> OnAfterSessionLaunchedEvent => Instance._onAfterSessionLaunchedEvent;

	public static IMbEvent<CampaignGameStarter> OnNewGameCreatedEvent => Instance._onNewGameCreatedEvent;

	public static IMbEvent<CampaignGameStarter, int> OnNewGameCreatedPartialFollowUpEvent => Instance._onNewGameCreatedPartialFollowUpEvent;

	public static IMbEvent<CampaignGameStarter> OnNewGameCreatedPartialFollowUpEndEvent => Instance._onNewGameCreatedPartialFollowUpEndEvent;

	public static IMbEvent<CampaignGameStarter> OnGameEarlyLoadedEvent => Instance._onGameEarlyLoadedEvent;

	public static IMbEvent<CampaignGameStarter> OnGameLoadedEvent => Instance._onGameLoadedEvent;

	public static IMbEvent OnGameLoadFinishedEvent => Instance._onGameLoadFinishedEvent;

	public static IMbEvent<MobileParty, PartyThinkParams> AiHourlyTickEvent => Instance._aiHourlyTickEvent;

	public static IMbEvent<MobileParty> TickPartialHourlyAiEvent => Instance._tickPartialHourlyAiEvent;

	public static IMbEvent<MobileParty> OnPartyJoinedArmyEvent => Instance._onPartyJoinedArmyEvent;

	public static IMbEvent<MobileParty> PartyRemovedFromArmyEvent => Instance._onPartyRemovedFromArmyEvent;

	public static IMbEvent OnPlayerArmyLeaderChangedBehaviorEvent => Instance._onPlayerArmyLeaderChangedBehaviorEvent;

	public static IMbEvent<IMission> OnMissionEndedEvent => Instance._onMissionEndedEvent;

	public static IMbEvent<MobileParty> OnQuarterDailyPartyTick => Instance._onQuarterDailyPartyTick;

	public static IMbEvent<MapEvent> OnPlayerBattleEndEvent => Instance._onPlayerBattleEndEvent;

	public static IMbEvent<CharacterObject, int> OnUnitRecruitedEvent => Instance._onUnitRecruitedEvent;

	public static IMbEvent<Hero> OnChildConceivedEvent => Instance._onChildConceived;

	public static IMbEvent<Hero, List<Hero>, int> OnGivenBirthEvent => Instance._onGivenBirthEvent;

	public static IMbEvent<float> MissionTickEvent => Instance._missionTickEvent;

	public static IMbEvent ArmyOverlaySetDirtyEvent => Instance._armyOverlaySetDirty ?? (Instance._armyOverlaySetDirty = new MbEvent());

	public static IMbEvent<int> PlayerDesertedBattleEvent => Instance._playerDesertedBattle;

	public static IMbEvent<PartyBase> PartyVisibilityChangedEvent => Instance._partyVisibilityChanged ?? (Instance._partyVisibilityChanged = new MbEvent<PartyBase>());

	public static IMbEvent<Track> TrackDetectedEvent => Instance._trackDetectedEvent;

	public static IMbEvent<Track> TrackLostEvent => Instance._trackLostEvent;

	public static IMbEvent<Dictionary<string, int>> LocationCharactersAreReadyToSpawnEvent => Instance._locationCharactersAreReadyToSpawn;

	public static ReferenceIMBEvent<MatrixFrame> BeforePlayerAgentSpawnEvent => Instance._onBeforePlayerAgentSpawn;

	public static IMbEvent PlayerAgentSpawned => Instance._onPlayerAgentSpawned;

	public static IMbEvent LocationCharactersSimulatedEvent => Instance._locationCharactersSimulatedSpawned;

	public static IMbEvent<CharacterObject, CharacterObject, int> PlayerUpgradedTroopsEvent => Instance._playerUpgradedTroopsEvent;

	public static IMbEvent<CharacterObject, CharacterObject, PartyBase, WeaponComponentData, bool, int> OnHeroCombatHitEvent => Instance._onHeroCombatHitEvent;

	public static IMbEvent<CharacterObject> CharacterPortraitPopUpOpenedEvent => Instance._characterPortraitPopUpOpenedEvent;

	public static IMbEvent CharacterPortraitPopUpClosedEvent => Instance._characterPortraitPopUpClosedEvent;

	public static IMbEvent<Hero> PlayerStartTalkFromMenu => Instance._playerStartTalkFromMenu;

	public static IMbEvent<GameMenu, GameMenuOption> GameMenuOptionSelectedEvent => Instance._gameMenuOptionSelectedEvent;

	public static IMbEvent<CharacterObject> PlayerStartRecruitmentEvent => Instance._playerStartRecruitmentEvent;

	public static IMbEvent<Hero, Hero> OnBeforePlayerCharacterChangedEvent => Instance._onBeforePlayerCharacterChangedEvent;

	public static IMbEvent<Hero, Hero, MobileParty, bool> OnPlayerCharacterChangedEvent => Instance._onPlayerCharacterChangedEvent;

	public static IMbEvent<Hero, Hero> OnClanLeaderChangedEvent => Instance._onClanLeaderChangedEvent;

	public static IMbEvent<SiegeEvent> OnSiegeEventStartedEvent => Instance._onSiegeEventStartedEvent;

	public static IMbEvent OnPlayerSiegeStartedEvent => Instance._onPlayerSiegeStartedEvent;

	public static IMbEvent<SiegeEvent> OnSiegeEventEndedEvent => Instance._onSiegeEventEndedEvent;

	public static IMbEvent<MobileParty, Settlement, SiegeAftermathAction.SiegeAftermath, Clan, Dictionary<MobileParty, float>> OnSiegeAftermathAppliedEvent => Instance._siegeAftermathAppliedEvent;

	public static IMbEvent<MobileParty, Settlement, BattleSideEnum, SiegeEngineType, SiegeBombardTargets> OnSiegeBombardmentHitEvent => Instance._onSiegeBombardmentHitEvent;

	public static IMbEvent<MobileParty, Settlement, BattleSideEnum, SiegeEngineType, bool> OnSiegeBombardmentWallHitEvent => Instance._onSiegeBombardmentWallHitEvent;

	public static IMbEvent<MobileParty, Settlement, BattleSideEnum, SiegeEngineType> OnSiegeEngineDestroyedEvent => Instance._onSiegeEngineDestroyedEvent;

	public static IMbEvent<List<TradeRumor>, Settlement> OnTradeRumorIsTakenEvent => Instance._onTradeRumorIsTakenEvent;

	public static IMbEvent<Hero> OnCheckForIssueEvent => Instance._onCheckForIssueEvent;

	public static IMbEvent<IssueBase, IssueBase.IssueUpdateDetails, Hero> OnIssueUpdatedEvent => Instance._onIssueUpdatedEvent;

	public static IMbEvent<MobileParty, TroopRoster> OnTroopsDesertedEvent => Instance._onTroopsDesertedEvent;

	public static IMbEvent<Hero, Settlement, Hero, CharacterObject, int> OnTroopRecruitedEvent => Instance._onTroopRecruitedEvent;

	public static IMbEvent<Hero, Settlement, TroopRoster> OnTroopGivenToSettlementEvent => Instance._onTroopGivenToSettlementEvent;

	public static IMbEvent<PartyBase, PartyBase, ItemRosterElement, int, Settlement> OnItemSoldEvent => Instance._onItemSoldEvent;

	public static IMbEvent<MobileParty, Town, List<(EquipmentElement, int)>> OnCaravanTransactionCompletedEvent => Instance._onCaravanTransactionCompletedEvent;

	public static IMbEvent<PartyBase, PartyBase, TroopRoster> OnPrisonerSoldEvent => Instance._onPrisonerSoldEvent;

	public static IMbEvent<MobileParty> OnPartyDisbandStartedEvent => Instance._onPartyDisbandStartedEvent;

	public static IMbEvent<MobileParty, Settlement> OnPartyDisbandedEvent => Instance._onPartyDisbandedEvent;

	public static IMbEvent<MobileParty> OnPartyDisbandCanceledEvent => Instance._onPartyDisbandCanceledEvent;

	public static IMbEvent<PartyBase, PartyBase> OnHideoutSpottedEvent => Instance._hideoutSpottedEvent;

	public static IMbEvent<Settlement> OnHideoutDeactivatedEvent => Instance._hideoutDeactivatedEvent;

	public static IMbEvent<Hero, Hero, float> OnHeroSharedFoodWithAnotherHeroEvent => Instance._heroSharedFoodWithAnotherHeroEvent;

	public static IMbEvent<List<(ItemRosterElement, int)>, List<(ItemRosterElement, int)>, bool> PlayerInventoryExchangeEvent => Instance._playerInventoryExchangeEvent;

	public static IMbEvent<ItemRoster> OnItemsDiscardedByPlayerEvent => Instance._onItemsDiscardedByPlayerEvent;

	public static IMbEvent<Tuple<PersuasionOptionArgs, PersuasionOptionResult>> PersuasionProgressCommittedEvent => Instance._persuasionProgressCommittedEvent;

	public static IMbEvent<QuestBase, QuestBase.QuestCompleteDetails> OnQuestCompletedEvent => Instance._onQuestCompletedEvent;

	public static IMbEvent<QuestBase> OnQuestStartedEvent => Instance._onQuestStartedEvent;

	public static IMbEvent<ItemObject, Settlement, int> OnItemProducedEvent => Instance._itemProducedEvent;

	public static IMbEvent<ItemObject, Settlement, int> OnItemConsumedEvent => Instance._itemConsumedEvent;

	public static IMbEvent<MobileParty> OnPartyConsumedFoodEvent => Instance._onPartyConsumedFoodEvent;

	public static IMbEvent<Hero, Hero, KillCharacterAction.KillCharacterActionDetail, bool> OnBeforeMainCharacterDiedEvent => Instance._onBeforeMainCharacterDiedEvent;

	public static IMbEvent<IssueBase> OnNewIssueCreatedEvent => Instance._onNewIssueCreatedEvent;

	public static IMbEvent<IssueBase, Hero> OnIssueOwnerChangedEvent => Instance._onIssueOwnerChangedEvent;

	public static IMbEvent OnGameOverEvent => Instance._onGameOverEvent;

	public static IMbEvent<Settlement, MobileParty, bool, MapEvent.BattleTypes> SiegeCompletedEvent => Instance._siegeCompletedEvent;

	public static IMbEvent<Settlement, MobileParty, bool, MapEvent.BattleTypes> AfterSiegeCompletedEvent => Instance._afterSiegeCompletedEvent;

	public static IMbEvent<SiegeEvent, BattleSideEnum, SiegeEngineType> SiegeEngineBuiltEvent => Instance._siegeEngineBuiltEvent;

	public static IMbEvent<BattleSideEnum, RaidEventComponent> RaidCompletedEvent => Instance._raidCompletedEvent;

	public static IMbEvent<BattleSideEnum, ForceVolunteersEventComponent> ForceVolunteersCompletedEvent => Instance._forceVolunteersCompletedEvent;

	public static IMbEvent<BattleSideEnum, ForceSuppliesEventComponent> ForceSuppliesCompletedEvent => Instance._forceSuppliesCompletedEvent;

	public static MbEvent<BattleSideEnum, HideoutEventComponent, HideoutEventComponent.HideoutBattleEndState> OnHideoutBattleCompletedEvent => Instance._hideoutBattleCompletedEvent;

	public static IMbEvent<Clan> OnClanDestroyedEvent => Instance._onClanDestroyedEvent;

	public static IMbEvent<ItemObject, ItemModifier, bool> OnNewItemCraftedEvent => Instance._onNewItemCraftedEvent;

	public static IMbEvent<CraftingPiece> CraftingPartUnlockedEvent => Instance._craftingPartUnlockedEvent;

	public static IMbEvent<Workshop> WorkshopInitializedEvent => Instance._onWorkshopInitializedEvent;

	public static IMbEvent<Workshop, Hero> WorkshopOwnerChangedEvent => Instance._onWorkshopOwnerChangedEvent;

	public static IMbEvent<Workshop> WorkshopTypeChangedEvent => Instance._onWorkshopTypeChangedEvent;

	public static IMbEvent OnBeforeSaveEvent => Instance._onBeforeSaveEvent;

	public static IMbEvent OnSaveStartedEvent => Instance._onSaveStartedEvent;

	public static IMbEvent<bool, string> OnSaveOverEvent => Instance._onSaveOverEvent;

	public static IMbEvent<List<KeyValuePair<string, string>>> CollectMetadataEntriesEvent => Instance._collectMetadataEntriesEvent;

	public static IMbEvent<FlattenedTroopRoster> OnPrisonerTakenEvent => Instance._onPrisonerTakenEvent;

	public static IMbEvent<FlattenedTroopRoster> OnPrisonerReleasedEvent => Instance._onPrisonerReleasedEvent;

	public static IMbEvent<FlattenedTroopRoster> OnMainPartyPrisonerRecruitedEvent => Instance._onMainPartyPrisonerRecruitedEvent;

	public static IMbEvent<MobileParty, FlattenedTroopRoster, Settlement> OnPrisonerDonatedToSettlementEvent => Instance._onPrisonerDonatedToSettlementEvent;

	public static IMbEvent<Hero, EquipmentElement> OnEquipmentSmeltedByHeroEvent => Instance._onEquipmentSmeltedByHero;

	public static IMbEvent<int> OnPlayerTradeProfitEvent => Instance._onPlayerTradeProfit;

	public static IMbEvent<Hero, Clan> OnHeroChangedClanEvent => Instance._onHeroChangedClan;

	public static IMbEvent<Hero, HeroGetsBusyReasons> OnHeroGetsBusyEvent => Instance._onHeroGetsBusy;

	public static IMbEvent<PartyBase, ItemRoster> OnCollectLootsItemsEvent => Instance._onCollectLootItems;

	public static IMbEvent<PartyBase, PartyBase, ItemRoster> OnLootDistributedToPartyEvent => Instance._onLootDistributedToPartyEvent;

	public static IMbEvent<Hero, Settlement, MobileParty, TeleportHeroAction.TeleportationDetail> OnHeroTeleportationRequestedEvent => Instance._onHeroTeleportationRequestedEvent;

	public static IMbEvent<MobileParty> OnPartyLeaderChangeOfferCanceledEvent => Instance._onPartyLeaderChangeOfferCanceledEvent;

	public static IMbEvent<MobileParty, Hero> OnPartyLeaderChangedEvent => Instance._onPartyLeaderChangedEvent;

	public static IMbEvent<Clan, float> OnClanInfluenceChangedEvent => Instance._onClanInfluenceChangedEvent;

	public static IMbEvent<CharacterObject> OnPlayerPartyKnockedOrKilledTroopEvent => Instance._onPlayerPartyKnockedOrKilledTroopEvent;

	public static IMbEvent<DefaultClanFinanceModel.AssetIncomeType, int> OnPlayerEarnedGoldFromAssetEvent => Instance._onPlayerEarnedGoldFromAssetEvent;

	public static IMbEvent<Clan, IFaction> OnClanEarnedGoldFromTributeEvent => Instance._onClanEarnedGoldFromTributeEvent;

	public static IMbEvent OnMainPartyStarvingEvent => Instance._onMainPartyStarving;

	public static IMbEvent<Town, bool> OnPlayerJoinedTournamentEvent => Instance._onPlayerJoinedTournamentEvent;

	public static IMbEvent<Hero> OnHeroUnregisteredEvent => Instance._onHeroUnregisteredEvent;

	public static IMbEvent OnConfigChangedEvent => Instance._onConfigChanged;

	public static IMbEvent<Town, CraftingOrder, ItemObject, Hero> OnCraftingOrderCompletedEvent => Instance._onCraftingOrderCompleted;

	public static IMbEvent<Hero, Crafting.RefiningFormula> OnItemsRefinedEvent => Instance._onItemsRefined;

	public static IMbEvent<Dictionary<Hero, int>> OnHeirSelectionRequestedEvent => Instance._onHeirSelectionRequested;

	public static IMbEvent<Hero> OnHeirSelectionOverEvent => Instance._onHeirSelectionOver;

	public static IMbEvent<CharacterCreationManager> OnCharacterCreationInitializedEvent => Instance._onCharacterCreationInitialized;

	public static IMbEvent<MobileParty> OnMobilePartyRaftStateChangedEvent => Instance._onMobilePartyRaftStateChanged;

	public static IMbEvent<PartyBase, Ship, DestroyShipAction.ShipDestroyDetail> OnShipDestroyedEvent => Instance._onShipDestroyedEvent;

	public static IMbEvent<Ship, PartyBase, ChangeShipOwnerAction.ShipOwnerChangeDetail> OnShipOwnerChangedEvent => Instance._onShipOwnerChangedEvent;

	public static IMbEvent<Ship, Settlement> OnShipRepairedEvent => Instance._onShipRepairedEvent;

	public static IMbEvent<Ship, Settlement> OnShipCreatedEvent => Instance._onShipCreatedEvent;

	public static IMbEvent<Figurehead> OnFigureheadUnlockedEvent => Instance._onFigureheadUnlockedEvent;

	public static IMbEvent<MobileParty, Army> OnPartyLeftArmyEvent => Instance._onPartyLeftArmyEvent;

	public static IMbEvent<PartyBase> OnPartyAddedToMapEventEvent => Instance._onPartyAddedToMapEventEvent;

	public static IMbEvent<Incident> OnIncidentResolvedEvent => Instance._onIncidentResolvedEvent;

	public static IMbEvent<MobileParty> OnMobilePartyNavigationStateChangedEvent => Instance._onMobilePartyNavigationStateChangedEvent;

	public static IMbEvent<MobileParty> OnMobilePartyJoinedToSiegeEventEvent => Instance._onMobilePartyJoinedToSiegeEventEvent;

	public static IMbEvent<MobileParty> OnMobilePartyLeftSiegeEventEvent => Instance._onMobilePartyLeftSiegeEventEvent;

	public static IMbEvent<SiegeEvent> OnBlockadeActivatedEvent => Instance._onBlockadeActivatedEvent;

	public static IMbEvent<SiegeEvent> OnBlockadeDeactivatedEvent => Instance._onBlockadeDeactivatedEvent;

	public static IMbEvent<MapMarker> OnMapMarkerCreatedEvent => Instance._onMapMarkerCreatedEvent;

	public static IMbEvent<MapMarker> OnMapMarkerRemovedEvent => Instance._onMapMarkerRemovedEvent;

	public static IMbEvent<Kingdom, Kingdom> OnAllianceStartedEvent => Instance._onAllianceStartedEvent;

	public static IMbEvent<Kingdom, Kingdom> OnAllianceEndedEvent => Instance._onAllianceEndedEvent;

	public static IMbEvent<Kingdom, Kingdom, Kingdom> OnCallToWarAgreementStartedEvent => Instance._onCallToWarAgreementStartedEvent;

	public static IMbEvent<Kingdom, Kingdom, Kingdom> OnCallToWarAgreementEndedEvent => Instance._onCallToWarAgreementEndedEvent;

	public static ReferenceIMBEvent<Hero, bool> CanHeroLeadPartyEvent => Instance._canHeroLeadPartyEvent;

	public static ReferenceIMBEvent<Hero, bool> CanHeroMarryEvent => Instance._canMarryEvent;

	public static ReferenceIMBEvent<Hero, bool> CanHeroEquipmentBeChangedEvent => Instance._canHeroEquipmentBeChangedEvent;

	public static ReferenceIMBEvent<Hero, bool> CanBeGovernorOrHavePartyRoleEvent => Instance._canBeGovernorOrHavePartyRoleEvent;

	public static ReferenceIMBEvent<Hero, KillCharacterAction.KillCharacterActionDetail, bool> CanHeroDieEvent => Instance._canHeroDieEvent;

	public static ReferenceIMBEvent<Hero, bool> CanPlayerMeetWithHeroAfterConversationEvent => Instance._canPlayerMeetWithHeroAfterConversationEvent;

	public static ReferenceIMBEvent<Hero, bool> CanHeroBecomePrisonerEvent => Instance._canHeroBecomePrisonerEvent;

	public static ReferenceIMBEvent<Hero, bool> CanMoveToSettlementEvent => Instance._canMoveToSettlementEvent;

	public static ReferenceIMBEvent<Hero, bool> CanHaveCampaignIssuesEvent => Instance._canHaveCampaignIssues;

	public static ReferenceIMBEvent<Settlement, object, int> IsSettlementBusyEvent => Instance._isSettlementBusy;

	public static IMbEvent<IFaction> OnMapEventContinuityNeedsUpdateEvent => Instance._onMapEventContinuityNeedsUpdate;

	public override void RemoveListeners(object obj)
	{
		_heroLevelledUp.ClearListeners(obj);
		_onHomeHideoutChangedEvent.ClearListeners(obj);
		_heroGainedSkill.ClearListeners(obj);
		_heroRelationChanged.ClearListeners(obj);
		_questLogAddedEvent.ClearListeners(obj);
		_issueLogAddedEvent.ClearListeners(obj);
		_onCharacterCreationIsOverEvent.ClearListeners(obj);
		_clanChangedKingdom.ClearListeners(obj);
		_onClanDefected.ClearListeners(obj);
		_onClanCreatedEvent.ClearListeners(obj);
		_onHeroJoinedPartyEvent.ClearListeners(obj);
		_partyAttachedParty.ClearListeners(obj);
		_nearbyPartyAddedToPlayerMapEvent.ClearListeners(obj);
		_armyCreated.ClearListeners(obj);
		_armyGathered.ClearListeners(obj);
		_armyDispersed.ClearListeners(obj);
		_villageStateChanged.ClearListeners(obj);
		_settlementEntered.ClearListeners(obj);
		_afterSettlementEntered.ClearListeners(obj);
		_beforeSettlementEntered.ClearListeners(obj);
		_mercenaryTroopChangedInTown.ClearListeners(obj);
		_mercenaryNumberChangedInTown.ClearListeners(obj);
		_alleyOwnerChanged.ClearListeners(obj);
		_alleyOccupiedByPlayer.ClearListeners(obj);
		_alleyClearedByPlayer.ClearListeners(obj);
		_romanticStateChanged.ClearListeners(obj);
		_warDeclared.ClearListeners(obj);
		_battleStarted.ClearListeners(obj);
		_rebellionFinished.ClearListeners(obj);
		_townRebelliousStateChanged.ClearListeners(obj);
		_rebelliousClanDisbandedAtSettlement.ClearListeners(obj);
		_mobilePartyDestroyed.ClearListeners(obj);
		_mobilePartyCreated.ClearListeners(obj);
		_mapInteractableCreated.ClearListeners(obj);
		_mapInteractableDestroyed.ClearListeners(obj);
		_mobilePartyQuestStatusChanged.ClearListeners(obj);
		_heroKilled.ClearListeners(obj);
		_characterDefeated.ClearListeners(obj);
		_heroPrisonerTaken.ClearListeners(obj);
		_onPartySizeChangedEvent.ClearListeners(obj);
		_characterBecameFugitiveEvent.ClearListeners(obj);
		_playerMetHero.ClearListeners(obj);
		_playerLearnsAboutHero.ClearListeners(obj);
		_renownGained.ClearListeners(obj);
		_barterablesRequested.ClearListeners(obj);
		_crimeRatingChanged.ClearListeners(obj);
		_newCompanionAdded.ClearListeners(obj);
		_afterMissionStarted.ClearListeners(obj);
		_gameMenuOpened.ClearListeners(obj);
		_makePeace.ClearListeners(obj);
		_kingdomCreated.ClearListeners(obj);
		_kingdomDestroyed.ClearListeners(obj);
		_canKingdomBeDiscontinued.ClearListeners(obj);
		_villageBeingRaided.ClearListeners(obj);
		_villageLooted.ClearListeners(obj);
		_mapEventEnded.ClearListeners(obj);
		_mapEventStarted.ClearListeners(obj);
		_prisonersChangeInSettlement.ClearListeners(obj);
		_onMissionStartedEvent.ClearListeners(obj);
		_beforeMissionOpenedEvent.ClearListeners(obj);
		_onPartyRemovedEvent.ClearListeners(obj);
		_onPartyLeaderChangedEvent.ClearListeners(obj);
		_banditPartyRecruited.ClearListeners(obj);
		_onSettlementOwnerChangedEvent.ClearListeners(obj);
		_onGovernorChangedEvent.ClearListeners(obj);
		_onSettlementLeftEvent.ClearListeners(obj);
		_weeklyTickEvent.ClearListeners(obj);
		_dailyTickEvent.ClearListeners(obj);
		_dailyTickPartyEvent.ClearListeners(obj);
		_hourlyTickEvent.ClearListeners(obj);
		_tickEvent.ClearListeners(obj);
		_onSessionLaunchedEvent.ClearListeners(obj);
		_onAfterSessionLaunchedEvent.ClearListeners(obj);
		_onNewGameCreatedPartialFollowUpEvent.ClearListeners(obj);
		_onNewGameCreatedPartialFollowUpEndEvent.ClearListeners(obj);
		_onNewGameCreatedEvent.ClearListeners(obj);
		_onGameLoadedEvent.ClearListeners(obj);
		_onBarterAcceptedEvent.ClearListeners(obj);
		_onBarterCanceledEvent.ClearListeners(obj);
		_onGameEarlyLoadedEvent.ClearListeners(obj);
		_onGameLoadFinishedEvent.ClearListeners(obj);
		_aiHourlyTickEvent.ClearListeners(obj);
		_tickPartialHourlyAiEvent.ClearListeners(obj);
		_onPartyJoinedArmyEvent.ClearListeners(obj);
		_onPartyRemovedFromArmyEvent.ClearListeners(obj);
		_onMissionEndedEvent.ClearListeners(obj);
		_onPlayerBattleEndEvent.ClearListeners(obj);
		_onPlayerBoardGameOver.ClearListeners(obj);
		_onRansomOfferedToPlayer.ClearListeners(obj);
		_onRansomOfferCancelled.ClearListeners(obj);
		_onPeaceOfferedToPlayer.ClearListeners(obj);
		_onTradeAgreementSignedEvent.ClearListeners(obj);
		_onPeaceOfferResolved.ClearListeners(obj);
		_onMarriageOfferedToPlayerEvent.ClearListeners(obj);
		_onMarriageOfferCanceledEvent.ClearListeners(obj);
		_onVassalOrMercenaryServiceOfferedToPlayerEvent.ClearListeners(obj);
		_onVassalOrMercenaryServiceOfferCanceledEvent.ClearListeners(obj);
		_afterGameMenuInitializedEvent.ClearListeners(obj);
		_beforeGameMenuOpenedEvent.ClearListeners(obj);
		_onChildConceived.ClearListeners(obj);
		_onGivenBirthEvent.ClearListeners(obj);
		_missionTickEvent.ClearListeners(obj);
		_armyOverlaySetDirty.ClearListeners(obj);
		_onPlayerArmyLeaderChangedBehaviorEvent.ClearListeners(obj);
		_partyVisibilityChanged.ClearListeners(obj);
		_onHeroCreated.ClearListeners(obj);
		_heroOccupationChangedEvent.ClearListeners(obj);
		_onHeroWounded.ClearListeners(obj);
		_playerDesertedBattle.ClearListeners(obj);
		_companionRemoved.ClearListeners(obj);
		_trackLostEvent.ClearListeners(obj);
		_trackDetectedEvent.ClearListeners(obj);
		_locationCharactersAreReadyToSpawn.ClearListeners(obj);
		_locationCharactersSimulatedSpawned.ClearListeners(obj);
		_playerUpgradedTroopsEvent.ClearListeners(obj);
		_onHeroCombatHitEvent.ClearListeners(obj);
		_characterPortraitPopUpOpenedEvent.ClearListeners(obj);
		_characterPortraitPopUpClosedEvent.ClearListeners(obj);
		_playerStartTalkFromMenu.ClearListeners(obj);
		_gameMenuOptionSelectedEvent.ClearListeners(obj);
		_playerStartRecruitmentEvent.ClearListeners(obj);
		_onAgentJoinedConversationEvent.ClearListeners(obj);
		_onConversationEnded.ClearListeners(obj);
		_beforeHeroesMarried.ClearListeners(obj);
		_onTroopsDesertedEvent.ClearListeners(obj);
		_onBeforePlayerCharacterChangedEvent.ClearListeners(obj);
		_onPlayerCharacterChangedEvent.ClearListeners(obj);
		_onClanLeaderChangedEvent.ClearListeners(obj);
		_onSiegeEventStartedEvent.ClearListeners(obj);
		_onPlayerSiegeStartedEvent.ClearListeners(obj);
		_onSiegeEventEndedEvent.ClearListeners(obj);
		_siegeAftermathAppliedEvent.ClearListeners(obj);
		_onSiegeBombardmentHitEvent.ClearListeners(obj);
		_onSiegeBombardmentWallHitEvent.ClearListeners(obj);
		_onSiegeEngineDestroyedEvent.ClearListeners(obj);
		_kingdomDecisionAdded.ClearListeners(obj);
		_kingdomDecisionCancelled.ClearListeners(obj);
		_kingdomDecisionConcluded.ClearListeners(obj);
		_childEducationCompleted.ClearListeners(obj);
		_heroComesOfAge.ClearListeners(obj);
		_heroGrowsOutOfInfancyEvent.ClearListeners(obj);
		_heroReachesTeenAgeEvent.ClearListeners(obj);
		_onCheckForIssueEvent.ClearListeners(obj);
		_onIssueUpdatedEvent.ClearListeners(obj);
		_onTroopRecruitedEvent.ClearListeners(obj);
		_onTroopGivenToSettlementEvent.ClearListeners(obj);
		_onItemSoldEvent.ClearListeners(obj);
		_onCaravanTransactionCompletedEvent.ClearListeners(obj);
		_onPrisonerSoldEvent.ClearListeners(obj);
		_heroPrisonerReleased.ClearListeners(obj);
		_heroOrPartyTradedGold.ClearListeners(obj);
		_heroOrPartyGaveItem.ClearListeners(obj);
		_perkOpenedEvent.ClearListeners(obj);
		_playerTraitChangedEvent.ClearListeners(obj);
		_onPartyDisbandedEvent.ClearListeners(obj);
		_onPartyDisbandStartedEvent.ClearListeners(obj);
		_onPartyDisbandCanceledEvent.ClearListeners(obj);
		_itemsLooted.ClearListeners(obj);
		_hideoutSpottedEvent.ClearListeners(obj);
		_hideoutBattleCompletedEvent.ClearListeners(obj);
		_hideoutDeactivatedEvent.ClearListeners(obj);
		_heroSharedFoodWithAnotherHeroEvent.ClearListeners(obj);
		_onQuestCompletedEvent.ClearListeners(obj);
		_itemProducedEvent.ClearListeners(obj);
		_itemConsumedEvent.ClearListeners(obj);
		_onQuestStartedEvent.ClearListeners(obj);
		_onPartyConsumedFoodEvent.ClearListeners(obj);
		_siegeCompletedEvent.ClearListeners(obj);
		_afterSiegeCompletedEvent.ClearListeners(obj);
		_raidCompletedEvent.ClearListeners(obj);
		_forceVolunteersCompletedEvent.ClearListeners(obj);
		_forceSuppliesCompletedEvent.ClearListeners(obj);
		_onBeforeMainCharacterDiedEvent.ClearListeners(obj);
		_onGameOverEvent.ClearListeners(obj);
		_onClanDestroyedEvent.ClearListeners(obj);
		_onNewIssueCreatedEvent.ClearListeners(obj);
		_onIssueOwnerChangedEvent.ClearListeners(obj);
		_onTutorialCompletedEvent.ClearListeners(obj);
		_collectAvailableTutorialsEvent.ClearListeners(obj);
		_playerEliminatedFromTournament.ClearListeners(obj);
		_playerStartedTournamentMatch.ClearListeners(obj);
		_tournamentStarted.ClearListeners(obj);
		_tournamentFinished.ClearListeners(obj);
		_tournamentCancelled.ClearListeners(obj);
		_playerInventoryExchangeEvent.ClearListeners(obj);
		_onItemsDiscardedByPlayerEvent.ClearListeners(obj);
		_onNewItemCraftedEvent.ClearListeners(obj);
		_craftingPartUnlockedEvent.ClearListeners(obj);
		_onWorkshopInitializedEvent.ClearListeners(obj);
		_onWorkshopOwnerChangedEvent.ClearListeners(obj);
		_onWorkshopTypeChangedEvent.ClearListeners(obj);
		_persuasionProgressCommittedEvent.ClearListeners(obj);
		_onBeforeSaveEvent.ClearListeners(obj);
		_onPrisonerTakenEvent.ClearListeners(obj);
		_onPrisonerReleasedEvent.ClearListeners(obj);
		_onMainPartyPrisonerRecruitedEvent.ClearListeners(obj);
		_onPrisonerDonatedToSettlementEvent.ClearListeners(obj);
		_onEquipmentSmeltedByHero.ClearListeners(obj);
		_onPlayerTradeProfit.ClearListeners(obj);
		_onBeforeHeroKilled.ClearListeners(obj);
		_onBuildingLevelChangedEvent.ClearListeners(obj);
		_hourlyTickSettlementEvent.ClearListeners(obj);
		_hourlyTickClanEvent.ClearListeners(obj);
		_onUnitRecruitedEvent.ClearListeners(obj);
		_trackDetectedEvent.ClearListeners(obj);
		_trackLostEvent.ClearListeners(obj);
		_onTradeRumorIsTakenEvent.ClearListeners(obj);
		_siegeEngineBuiltEvent.ClearListeners(obj);
		_dailyTickHeroEvent.ClearListeners(obj);
		_dailyTickSettlementEvent.ClearListeners(obj);
		_hourlyTickPartyEvent.ClearListeners(obj);
		_dailyTickClanEvent.ClearListeners(obj);
		_villageBecomeNormal.ClearListeners(obj);
		_clanTierIncrease.ClearListeners(obj);
		_dailyTickTownEvent.ClearListeners(obj);
		_onHeroChangedClan.ClearListeners(obj);
		_onHeroGetsBusy.ClearListeners(obj);
		_onSaveStartedEvent.ClearListeners(obj);
		_onSaveOverEvent.ClearListeners(obj);
		_collectMetadataEntriesEvent.ClearListeners(obj);
		_onPlayerBodyPropertiesChangedEvent.ClearListeners(obj);
		_rulingClanChanged.ClearListeners(obj);
		_onCollectLootItems.ClearListeners(obj);
		_onLootDistributedToPartyEvent.ClearListeners(obj);
		_onHeroTeleportationRequestedEvent.ClearListeners(obj);
		_onPartyLeaderChangeOfferCanceledEvent.ClearListeners(obj);
		_canBeGovernorOrHavePartyRoleEvent.ClearListeners(obj);
		_canHeroLeadPartyEvent.ClearListeners(obj);
		_canMarryEvent.ClearListeners(obj);
		_canHeroDieEvent.ClearListeners(obj);
		_canHeroBecomePrisonerEvent.ClearListeners(obj);
		_canHeroEquipmentBeChangedEvent.ClearListeners(obj);
		_canHaveCampaignIssues.ClearListeners(obj);
		_isSettlementBusy.ClearListeners(obj);
		_canMoveToSettlementEvent.ClearListeners(obj);
		_onQuarterDailyPartyTick.ClearListeners(obj);
		_onMainPartyStarving.ClearListeners(obj);
		_onClanInfluenceChangedEvent.ClearListeners(obj);
		_onPlayerPartyKnockedOrKilledTroopEvent.ClearListeners(obj);
		_onPlayerEarnedGoldFromAssetEvent.ClearListeners(obj);
		_onClanEarnedGoldFromTributeEvent.ClearListeners(obj);
		_onPlayerJoinedTournamentEvent.ClearListeners(obj);
		_onHeroUnregisteredEvent.ClearListeners(obj);
		_onConfigChanged.ClearListeners(obj);
		_onCraftingOrderCompleted.ClearListeners(obj);
		_onItemsRefined.ClearListeners(obj);
		_onMapEventContinuityNeedsUpdate.ClearListeners(obj);
		_onPlayerAgentSpawned.ClearListeners(obj);
		_onBeforePlayerAgentSpawn.ClearListeners(obj);
		_perkResetEvent.ClearListeners(obj);
		_onHeirSelectionOver.ClearListeners(obj);
		_onMobilePartyRaftStateChanged.ClearListeners(obj);
		_onCharacterCreationInitialized.ClearListeners(obj);
		_onShipDestroyedEvent.ClearListeners(obj);
		_onShipRepairedEvent.ClearListeners(obj);
		_onShipCreatedEvent.ClearListeners(obj);
		_onPartyLeftArmyEvent.ClearListeners(obj);
		_onPartyAddedToMapEventEvent.ClearListeners(obj);
		_onIncidentResolvedEvent.ClearListeners(obj);
		_onFigureheadUnlockedEvent.ClearListeners(obj);
		_onShipOwnerChangedEvent.ClearListeners(obj);
		_onMobilePartyNavigationStateChangedEvent.ClearListeners(obj);
		_onMobilePartyJoinedToSiegeEventEvent.ClearListeners(obj);
		_onMobilePartyLeftSiegeEventEvent.ClearListeners(obj);
		_onBlockadeActivatedEvent.ClearListeners(obj);
		_onBlockadeDeactivatedEvent.ClearListeners(obj);
		_onMercenaryServiceStartedEvent.ClearListeners(obj);
		_onMercenaryServiceEndedEvent.ClearListeners(obj);
		_canPlayerMeetWithHeroAfterConversationEvent.ClearListeners(obj);
		_onMapMarkerCreatedEvent.ClearListeners(obj);
		_onMapMarkerRemovedEvent.ClearListeners(obj);
		_onAllianceStartedEvent.ClearListeners(obj);
		_onAllianceEndedEvent.ClearListeners(obj);
		_onCallToWarAgreementStartedEvent.ClearListeners(obj);
		_onCallToWarAgreementEndedEvent.ClearListeners(obj);
		_onHeroActivatedEvent.ClearListeners(obj);
	}

	public override void OnPlayerBodyPropertiesChanged()
	{
		Instance._onPlayerBodyPropertiesChangedEvent.Invoke();
	}

	public override void OnBarterablesRequested(BarterData args)
	{
		Instance._barterablesRequested.Invoke(args);
	}

	public override void OnHeroLevelledUp(Hero hero, bool shouldNotify = true)
	{
		_heroLevelledUp.Invoke(hero, shouldNotify);
	}

	public override void OnHomeHideoutChanged(BanditPartyComponent banditPartyComponent, Hideout oldHomeHideout)
	{
		Instance._onHomeHideoutChangedEvent.Invoke(banditPartyComponent, oldHomeHideout);
	}

	public override void OnHeroGainedSkill(Hero hero, SkillObject skill, int change = 1, bool shouldNotify = true)
	{
		_heroGainedSkill.Invoke(hero, skill, change, shouldNotify);
	}

	public override void OnCharacterCreationIsOver()
	{
		Instance._onCharacterCreationIsOverEvent.Invoke();
	}

	public override void OnHeroCreated(Hero hero, bool isBornNaturally = false)
	{
		_onHeroCreated.Invoke(hero, isBornNaturally);
	}

	public override void OnHeroActivated(Hero hero, Hero.CharacterStates previousState)
	{
		_onHeroActivatedEvent.Invoke(hero, previousState);
	}

	public override void OnHeroOccupationChanged(Hero hero, Occupation oldOccupation)
	{
		_heroOccupationChangedEvent.Invoke(hero, oldOccupation);
	}

	public override void OnHeroWounded(Hero woundedHero)
	{
		_onHeroWounded.Invoke(woundedHero);
	}

	public override void OnBarterAccepted(Hero offererHero, Hero otherHero, List<Barterable> barters)
	{
		Instance._onBarterAcceptedEvent.Invoke(offererHero, otherHero, barters);
	}

	public override void OnBarterCanceled(Hero offererHero, Hero otherHero, List<Barterable> barters)
	{
		Instance._onBarterCanceledEvent.Invoke(offererHero, otherHero, barters);
	}

	public override void OnHeroRelationChanged(Hero effectiveHero, Hero effectiveHeroGainedRelationWith, int relationChange, bool showNotification, ChangeRelationAction.ChangeRelationDetail detail, Hero originalHero, Hero originalGainedRelationWith)
	{
		Instance._heroRelationChanged.Invoke(effectiveHero, effectiveHeroGainedRelationWith, relationChange, showNotification, detail, originalHero, originalGainedRelationWith);
	}

	public override void OnQuestLogAdded(QuestBase quest, bool hideInformation)
	{
		Instance._questLogAddedEvent.Invoke(quest, hideInformation);
	}

	public override void OnIssueLogAdded(IssueBase issue, bool hideInformation)
	{
		Instance._issueLogAddedEvent.Invoke(issue, hideInformation);
	}

	public override void OnClanTierChanged(Clan clan, bool shouldNotify = true)
	{
		Instance._clanTierIncrease.Invoke(clan, shouldNotify);
	}

	public override void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
	{
		Instance._clanChangedKingdom.Invoke(clan, oldKingdom, newKingdom, detail, showNotification);
	}

	public override void OnClanDefected(Clan clan, Kingdom oldKingdom, Kingdom newKingdom)
	{
		Instance._onClanDefected.Invoke(clan, oldKingdom, newKingdom);
	}

	public override void OnClanCreated(Clan clan, bool isCompanion)
	{
		Instance._onClanCreatedEvent.Invoke(clan, isCompanion);
	}

	public override void OnHeroJoinedParty(Hero hero, MobileParty mobileParty)
	{
		Instance._onHeroJoinedPartyEvent.Invoke(hero, mobileParty);
	}

	public override void OnHeroOrPartyTradedGold((Hero, PartyBase) giver, (Hero, PartyBase) recipient, (int, string) goldAmount, bool showNotification)
	{
		Instance._heroOrPartyTradedGold.Invoke(giver, recipient, goldAmount, showNotification);
	}

	public override void OnHeroOrPartyGaveItem((Hero, PartyBase) giver, (Hero, PartyBase) receiver, ItemRosterElement itemRosterElement, bool showNotification)
	{
		Instance._heroOrPartyGaveItem.Invoke(giver, receiver, itemRosterElement, showNotification);
	}

	public override void OnBanditPartyRecruited(MobileParty banditParty)
	{
		Instance._banditPartyRecruited.Invoke(banditParty);
	}

	public override void OnKingdomDecisionAdded(KingdomDecision decision, bool isPlayerInvolved)
	{
		Instance._kingdomDecisionAdded.Invoke(decision, isPlayerInvolved);
	}

	public override void OnKingdomDecisionCancelled(KingdomDecision decision, bool isPlayerInvolved)
	{
		Instance._kingdomDecisionCancelled.Invoke(decision, isPlayerInvolved);
	}

	public override void OnKingdomDecisionConcluded(KingdomDecision decision, DecisionOutcome chosenOutcome, bool isPlayerInvolved)
	{
		Instance._kingdomDecisionConcluded.Invoke(decision, chosenOutcome, isPlayerInvolved);
	}

	public override void OnPartyAttachedAnotherParty(MobileParty mobileParty)
	{
		Instance._partyAttachedParty.Invoke(mobileParty);
	}

	public override void OnNearbyPartyAddedToPlayerMapEvent(MobileParty mobileParty)
	{
		Instance._nearbyPartyAddedToPlayerMapEvent.Invoke(mobileParty);
	}

	public override void OnArmyCreated(Army army)
	{
		Instance._armyCreated.Invoke(army);
	}

	public override void OnArmyDispersed(Army army, Army.ArmyDispersionReason reason, bool isPlayersArmy)
	{
		Instance._armyDispersed.Invoke(army, reason, isPlayersArmy);
	}

	public override void OnArmyGathered(Army army, IMapPoint gatheringPoint)
	{
		Instance._armyGathered.Invoke(army, gatheringPoint);
	}

	public override void OnPerkOpened(Hero hero, PerkObject perk)
	{
		Instance._perkOpenedEvent.Invoke(hero, perk);
	}

	public override void OnPerkReset(Hero hero, PerkObject perk)
	{
		Instance._perkResetEvent.Invoke(hero, perk);
	}

	public override void OnPlayerTraitChanged(TraitObject trait, int previousLevel)
	{
		Instance._playerTraitChangedEvent.Invoke(trait, previousLevel);
	}

	public override void OnVillageStateChanged(Village village, Village.VillageStates oldState, Village.VillageStates newState, MobileParty raiderParty)
	{
		Instance._villageStateChanged.Invoke(village, oldState, newState, raiderParty);
	}

	public override void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
	{
		Instance._settlementEntered.Invoke(party, settlement, hero);
	}

	public override void OnAfterSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
	{
		Instance._afterSettlementEntered.Invoke(party, settlement, hero);
	}

	public override void OnBeforeSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
	{
		Instance._beforeSettlementEntered.Invoke(party, settlement, hero);
	}

	public override void OnMercenaryTroopChangedInTown(Town town, CharacterObject oldTroopType, CharacterObject newTroopType)
	{
		Instance._mercenaryTroopChangedInTown.Invoke(town, oldTroopType, newTroopType);
	}

	public override void OnMercenaryNumberChangedInTown(Town town, int oldNumber, int newNumber)
	{
		Instance._mercenaryNumberChangedInTown.Invoke(town, oldNumber, newNumber);
	}

	public override void OnAlleyOccupiedByPlayer(Alley alley, TroopRoster troops)
	{
		Instance._alleyOccupiedByPlayer.Invoke(alley, troops);
	}

	public override void OnAlleyOwnerChanged(Alley alley, Hero newOwner, Hero oldOwner)
	{
		Instance._alleyOwnerChanged.Invoke(alley, newOwner, oldOwner);
	}

	public override void OnAlleyClearedByPlayer(Alley alley)
	{
		Instance._alleyClearedByPlayer.Invoke(alley);
	}

	public override void OnRomanticStateChanged(Hero hero1, Hero hero2, Romance.RomanceLevelEnum romanceLevel)
	{
		Instance._romanticStateChanged.Invoke(hero1, hero2, romanceLevel);
	}

	public override void OnBeforeHeroesMarried(Hero hero1, Hero hero2, bool showNotification = true)
	{
		Instance._beforeHeroesMarried.Invoke(hero1, hero2, showNotification);
	}

	public override void OnPlayerEliminatedFromTournament(int round, Town town)
	{
		Instance._playerEliminatedFromTournament.Invoke(round, town);
	}

	public override void OnPlayerStartedTournamentMatch(Town town)
	{
		Instance._playerStartedTournamentMatch.Invoke(town);
	}

	public override void OnTournamentStarted(Town town)
	{
		Instance._tournamentStarted.Invoke(town);
	}

	public override void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail declareWarDetail)
	{
		Instance._warDeclared.Invoke(faction1, faction2, declareWarDetail);
	}

	public override void OnTournamentFinished(CharacterObject winner, MBReadOnlyList<CharacterObject> participants, Town town, ItemObject prize)
	{
		Instance._tournamentFinished.Invoke(winner, participants, town, prize);
	}

	public override void OnTournamentCancelled(Town town)
	{
		Instance._tournamentCancelled.Invoke(town);
	}

	public override void OnStartBattle(PartyBase attackerParty, PartyBase defenderParty, object subject, bool showNotification)
	{
		Instance._battleStarted.Invoke(attackerParty, defenderParty, subject, showNotification);
	}

	public override void OnRebellionFinished(Settlement settlement, Clan oldOwnerClan)
	{
		Instance._rebellionFinished.Invoke(settlement, oldOwnerClan);
	}

	public override void TownRebelliousStateChanged(Town town, bool rebelliousState)
	{
		Instance._townRebelliousStateChanged.Invoke(town, rebelliousState);
	}

	public override void OnRebelliousClanDisbandedAtSettlement(Settlement settlement, Clan clan)
	{
		Instance._rebelliousClanDisbandedAtSettlement.Invoke(settlement, clan);
	}

	public override void OnItemsLooted(MobileParty mobileParty, ItemRoster items)
	{
		Instance._itemsLooted.Invoke(mobileParty, items);
	}

	public override void OnMobilePartyDestroyed(MobileParty mobileParty, PartyBase destroyerParty)
	{
		Instance._mobilePartyDestroyed.Invoke(mobileParty, destroyerParty);
	}

	public override void OnMobilePartyCreated(MobileParty party)
	{
		Instance._mobilePartyCreated.Invoke(party);
	}

	public override void OnMapInteractableCreated(IInteractablePoint interactable)
	{
		Instance._mapInteractableCreated.Invoke(interactable);
	}

	public override void OnMapInteractableDestroyed(IInteractablePoint interactable)
	{
		Instance._mapInteractableDestroyed.Invoke(interactable);
	}

	public override void OnMobilePartyQuestStatusChanged(MobileParty party, bool isUsedByQuest)
	{
		Instance._mobilePartyQuestStatusChanged.Invoke(party, isUsedByQuest);
	}

	public override void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification = true)
	{
		Instance._heroKilled.Invoke(victim, killer, detail, showNotification);
	}

	public override void OnBeforeHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification = true)
	{
		Instance._onBeforeHeroKilled.Invoke(victim, killer, detail, showNotification);
	}

	public override void OnChildEducationCompleted(Hero hero, int age)
	{
		Instance._childEducationCompleted.Invoke(hero, age);
	}

	public override void OnHeroComesOfAge(Hero hero)
	{
		Instance._heroComesOfAge.Invoke(hero);
	}

	public override void OnHeroGrowsOutOfInfancy(Hero hero)
	{
		Instance._heroGrowsOutOfInfancyEvent.Invoke(hero);
	}

	public override void OnHeroReachesTeenAge(Hero hero)
	{
		Instance._heroReachesTeenAgeEvent.Invoke(hero);
	}

	public override void OnCharacterDefeated(Hero winner, Hero loser)
	{
		Instance._characterDefeated.Invoke(winner, loser);
	}

	public override void OnRulingClanChanged(Kingdom kingdom, Clan oldRulingClan)
	{
		Instance._rulingClanChanged.Invoke(kingdom, oldRulingClan);
	}

	public override void OnHeroPrisonerTaken(PartyBase capturer, Hero prisoner)
	{
		Instance._heroPrisonerTaken.Invoke(capturer, prisoner);
	}

	public override void OnHeroPrisonerReleased(Hero prisoner, PartyBase party, IFaction capturerFaction, EndCaptivityDetail detail, bool showNotification = true)
	{
		Instance._heroPrisonerReleased.Invoke(prisoner, party, capturerFaction, detail, showNotification);
	}

	public override void OnCharacterBecameFugitive(Hero hero, bool showNotification)
	{
		Instance._characterBecameFugitiveEvent.Invoke(hero, showNotification);
	}

	public override void OnPlayerMetHero(Hero hero)
	{
		Instance._playerMetHero.Invoke(hero);
	}

	public override void OnPlayerLearnsAboutHero(Hero hero)
	{
		Instance._playerLearnsAboutHero.Invoke(hero);
	}

	public override void OnRenownGained(Hero hero, int gainedRenown, bool doNotNotify)
	{
		Instance._renownGained.Invoke(hero, gainedRenown, doNotNotify);
	}

	public override void OnCrimeRatingChanged(IFaction kingdom, float deltaCrimeAmount)
	{
		Instance._crimeRatingChanged.Invoke(kingdom, deltaCrimeAmount);
	}

	public override void OnNewCompanionAdded(Hero newCompanion)
	{
		Instance._newCompanionAdded.Invoke(newCompanion);
	}

	public override void OnAfterMissionStarted(IMission iMission)
	{
		Instance._afterMissionStarted.Invoke(iMission);
	}

	public override void OnGameMenuOpened(MenuCallbackArgs args)
	{
		Instance._gameMenuOpened.Invoke(args);
	}

	public override void AfterGameMenuInitialized(MenuCallbackArgs args)
	{
		Instance._afterGameMenuInitializedEvent.Invoke(args);
	}

	public override void BeforeGameMenuOpened(MenuCallbackArgs args)
	{
		Instance._beforeGameMenuOpenedEvent.Invoke(args);
	}

	public override void OnMakePeace(IFaction side1Faction, IFaction side2Faction, MakePeaceAction.MakePeaceDetail detail)
	{
		Instance._makePeace.Invoke(side1Faction, side2Faction, detail);
	}

	public override void OnKingdomDestroyed(Kingdom destroyedKingdom)
	{
		Instance._kingdomDestroyed.Invoke(destroyedKingdom);
	}

	public override void CanKingdomBeDiscontinued(Kingdom kingdom, ref bool result)
	{
		Instance._canKingdomBeDiscontinued.Invoke(kingdom, ref result);
	}

	public override void OnKingdomCreated(Kingdom createdKingdom)
	{
		Instance._kingdomCreated.Invoke(createdKingdom);
	}

	public override void OnVillageBecomeNormal(Village village)
	{
		Instance._villageBecomeNormal.Invoke(village);
	}

	public override void OnVillageBeingRaided(Village village)
	{
		Instance._villageBeingRaided.Invoke(village);
	}

	public override void OnVillageLooted(Village village)
	{
		Instance._villageLooted.Invoke(village);
	}

	public override void OnCompanionRemoved(Hero companion, RemoveCompanionAction.RemoveCompanionDetail detail)
	{
		Instance._companionRemoved.Invoke(companion, detail);
	}

	public override void OnAgentJoinedConversation(IAgent agent)
	{
		Instance._onAgentJoinedConversationEvent.Invoke(agent);
	}

	public override void OnConversationEnded(IEnumerable<CharacterObject> characters)
	{
		Instance._onConversationEnded.Invoke(characters);
	}

	public override void OnMapEventEnded(MapEvent mapEvent)
	{
		Instance._mapEventEnded.Invoke(mapEvent);
	}

	public override void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
	{
		Instance._mapEventStarted.Invoke(mapEvent, attackerParty, defenderParty);
	}

	public override void OnPrisonersChangeInSettlement(Settlement settlement, FlattenedTroopRoster prisonerRoster, Hero prisonerHero, bool takenFromDungeon)
	{
		Instance._prisonersChangeInSettlement.Invoke(settlement, prisonerRoster, prisonerHero, takenFromDungeon);
	}

	public override void OnPlayerBoardGameOver(Hero opposingHero, BoardGameHelper.BoardGameState state)
	{
		Instance._onPlayerBoardGameOver.Invoke(opposingHero, state);
	}

	public override void OnRansomOfferedToPlayer(Hero captiveHero)
	{
		Instance._onRansomOfferedToPlayer.Invoke(captiveHero);
	}

	public override void OnRansomOfferCancelled(Hero captiveHero)
	{
		Instance._onRansomOfferCancelled.Invoke(captiveHero);
	}

	public override void OnPeaceOfferedToPlayer(IFaction opponentFaction, int tributeAmount, int tributeDurationInDays)
	{
		Instance._onPeaceOfferedToPlayer.Invoke(opponentFaction, tributeAmount, tributeDurationInDays);
	}

	public override void OnTradeAgreementSigned(Kingdom kingdom, Kingdom other)
	{
		Instance._onTradeAgreementSignedEvent.Invoke(kingdom, other);
	}

	public override void OnPeaceOfferResolved(IFaction opponentFaction)
	{
		Instance._onPeaceOfferResolved.Invoke(opponentFaction);
	}

	public override void OnMarriageOfferedToPlayer(Hero suitor, Hero maiden)
	{
		Instance._onMarriageOfferedToPlayerEvent.Invoke(suitor, maiden);
	}

	public override void OnMarriageOfferCanceled(Hero suitor, Hero maiden)
	{
		Instance._onMarriageOfferCanceledEvent.Invoke(suitor, maiden);
	}

	public override void OnVassalOrMercenaryServiceOfferedToPlayer(Kingdom offeredKingdom)
	{
		Instance._onVassalOrMercenaryServiceOfferedToPlayerEvent.Invoke(offeredKingdom);
	}

	public override void OnVassalOrMercenaryServiceOfferCanceled(Kingdom offeredKingdom)
	{
		Instance._onVassalOrMercenaryServiceOfferCanceledEvent.Invoke(offeredKingdom);
	}

	public override void OnMercenaryServiceStarted(Clan mercenaryClan, StartMercenaryServiceAction.StartMercenaryServiceActionDetails details)
	{
		Instance._onMercenaryServiceStartedEvent.Invoke(mercenaryClan, details);
	}

	public override void OnMercenaryServiceEnded(Clan mercenaryClan, EndMercenaryServiceAction.EndMercenaryServiceActionDetails details)
	{
		Instance._onMercenaryServiceEndedEvent.Invoke(mercenaryClan, details);
	}

	public override void OnMissionStarted(IMission mission)
	{
		Instance._onMissionStartedEvent.Invoke(mission);
	}

	public override void BeforeMissionOpened()
	{
		Instance._beforeMissionOpenedEvent.Invoke();
	}

	public override void OnPartyRemoved(PartyBase party)
	{
		Instance._onPartyRemovedEvent.Invoke(party);
	}

	public override void OnPartySizeChanged(PartyBase party)
	{
		Instance._onPartySizeChangedEvent.Invoke(party);
	}

	public override void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
	{
		Instance._onSettlementOwnerChangedEvent.Invoke(settlement, openToClaim, newOwner, oldOwner, capturerHero, detail);
	}

	public override void OnGovernorChanged(Town fortification, Hero oldGovernor, Hero newGovernor)
	{
		Instance._onGovernorChangedEvent.Invoke(fortification, oldGovernor, newGovernor);
	}

	public override void OnSettlementLeft(MobileParty party, Settlement settlement)
	{
		Instance._onSettlementLeftEvent.Invoke(party, settlement);
	}

	public override void WeeklyTick()
	{
		Instance._weeklyTickEvent.Invoke();
	}

	public override void DailyTick()
	{
		Instance._dailyTickEvent.Invoke();
	}

	public override void DailyTickParty(MobileParty mobileParty)
	{
		Instance._dailyTickPartyEvent.Invoke(mobileParty);
	}

	public override void DailyTickTown(Town town)
	{
		Instance._dailyTickTownEvent.Invoke(town);
	}

	public override void DailyTickSettlement(Settlement settlement)
	{
		Instance._dailyTickSettlementEvent.Invoke(settlement);
	}

	public override void DailyTickHero(Hero hero)
	{
		Instance._dailyTickHeroEvent.Invoke(hero);
	}

	public override void DailyTickClan(Clan clan)
	{
		Instance._dailyTickClanEvent.Invoke(clan);
	}

	public override void CollectAvailableTutorials(ref List<CampaignTutorial> tutorials)
	{
		Instance._collectAvailableTutorialsEvent.Invoke(tutorials);
	}

	public override void OnTutorialCompleted(string tutorial)
	{
		Instance._onTutorialCompletedEvent.Invoke(tutorial);
	}

	public override void OnBuildingLevelChanged(Town town, Building building, int levelChange)
	{
		Instance._onBuildingLevelChangedEvent.Invoke(town, building, levelChange);
	}

	public override void HourlyTick()
	{
		Instance._hourlyTickEvent.Invoke();
	}

	public override void QuarterHourlyTick()
	{
		Instance._quarterHourlyTickEvent.Invoke();
	}

	public override void HourlyTickParty(MobileParty mobileParty)
	{
		Instance._hourlyTickPartyEvent.Invoke(mobileParty);
	}

	public override void HourlyTickSettlement(Settlement settlement)
	{
		Instance._hourlyTickSettlementEvent.Invoke(settlement);
	}

	public override void HourlyTickClan(Clan clan)
	{
		Instance._hourlyTickClanEvent.Invoke(clan);
	}

	public override void Tick(float dt)
	{
		Instance._tickEvent.Invoke(dt);
	}

	public override void OnSessionStart(CampaignGameStarter campaignGameStarter)
	{
		Instance._onSessionLaunchedEvent.Invoke(campaignGameStarter);
	}

	public override void OnAfterSessionStart(CampaignGameStarter campaignGameStarter)
	{
		Instance._onAfterSessionLaunchedEvent.Invoke(campaignGameStarter);
	}

	public override void OnNewGameCreated(CampaignGameStarter campaignGameStarter)
	{
		Instance._onNewGameCreatedEvent.Invoke(campaignGameStarter);
		for (int i = 0; i < 100; i++)
		{
			Instance._onNewGameCreatedPartialFollowUpEvent.Invoke(campaignGameStarter, i);
		}
		Instance._onNewGameCreatedPartialFollowUpEndEvent.Invoke(campaignGameStarter);
	}

	public override void OnGameEarlyLoaded(CampaignGameStarter campaignGameStarter)
	{
		Instance._onGameEarlyLoadedEvent.Invoke(campaignGameStarter);
	}

	public override void OnGameLoaded(CampaignGameStarter campaignGameStarter)
	{
		Instance._onGameLoadedEvent.Invoke(campaignGameStarter);
	}

	public override void OnGameLoadFinished()
	{
		Instance._onGameLoadFinishedEvent.Invoke();
	}

	public override void AiHourlyTick(MobileParty party, PartyThinkParams partyThinkParams)
	{
		Instance._aiHourlyTickEvent.Invoke(party, partyThinkParams);
	}

	public override void TickPartialHourlyAi(MobileParty party)
	{
		Instance._tickPartialHourlyAiEvent.Invoke(party);
	}

	public override void OnPartyJoinedArmy(MobileParty mobileParty)
	{
		Instance._onPartyJoinedArmyEvent.Invoke(mobileParty);
	}

	public override void OnPartyRemovedFromArmy(MobileParty mobileParty)
	{
		Instance._onPartyRemovedFromArmyEvent.Invoke(mobileParty);
	}

	public override void OnPlayerArmyLeaderChangedBehavior()
	{
		Instance._onPlayerArmyLeaderChangedBehaviorEvent.Invoke();
	}

	public override void OnMissionEnded(IMission mission)
	{
		Instance._onMissionEndedEvent.Invoke(mission);
	}

	public override void QuarterDailyPartyTick(MobileParty mobileParty)
	{
		Instance._onQuarterDailyPartyTick.Invoke(mobileParty);
	}

	public override void OnPlayerBattleEnd(MapEvent mapEvent)
	{
		Instance._onPlayerBattleEndEvent.Invoke(mapEvent);
	}

	public override void OnUnitRecruited(CharacterObject character, int amount)
	{
		Instance._onUnitRecruitedEvent.Invoke(character, amount);
	}

	public override void OnChildConceived(Hero mother)
	{
		Instance._onChildConceived.Invoke(mother);
	}

	public override void OnGivenBirth(Hero mother, List<Hero> aliveChildren, int stillbornCount)
	{
		Instance._onGivenBirthEvent.Invoke(mother, aliveChildren, stillbornCount);
	}

	public override void MissionTick(float dt)
	{
		Instance._missionTickEvent.Invoke(dt);
	}

	public override void OnArmyOverlaySetDirty()
	{
		if (Instance._armyOverlaySetDirty == null)
		{
			Instance._armyOverlaySetDirty = new MbEvent();
		}
		Instance._armyOverlaySetDirty.Invoke();
	}

	public override void OnPlayerDesertedBattle(int sacrificedMenCount)
	{
		Instance._playerDesertedBattle.Invoke(sacrificedMenCount);
	}

	public override void OnPartyVisibilityChanged(PartyBase party)
	{
		if (Instance._partyVisibilityChanged == null)
		{
			Instance._partyVisibilityChanged = new MbEvent<PartyBase>();
		}
		Instance._partyVisibilityChanged.Invoke(party);
	}

	public override void TrackDetected(Track track)
	{
		Instance._trackDetectedEvent.Invoke(track);
	}

	public override void TrackLost(Track track)
	{
		Instance._trackLostEvent.Invoke(track);
	}

	public override void LocationCharactersAreReadyToSpawn(Dictionary<string, int> unusedUsablePointCount)
	{
		foreach (KeyValuePair<string, int> item in unusedUsablePointCount)
		{
			_ = item;
		}
		Instance._locationCharactersAreReadyToSpawn.Invoke(unusedUsablePointCount);
	}

	public override void OnBeforePlayerAgentSpawn(ref MatrixFrame spawnFrame)
	{
		Instance._onBeforePlayerAgentSpawn.Invoke(ref spawnFrame);
	}

	public override void OnPlayerAgentSpawned()
	{
		Instance._onPlayerAgentSpawned.Invoke();
	}

	public override void LocationCharactersSimulated()
	{
		Instance._locationCharactersSimulatedSpawned.Invoke();
	}

	public override void OnPlayerUpgradedTroops(CharacterObject upgradeFromTroop, CharacterObject upgradeToTroop, int number)
	{
		Instance._playerUpgradedTroopsEvent.Invoke(upgradeFromTroop, upgradeToTroop, number);
	}

	public override void OnHeroCombatHit(CharacterObject attackerTroop, CharacterObject attackedTroop, PartyBase party, WeaponComponentData usedWeapon, bool isFatal, int xp)
	{
		Instance._onHeroCombatHitEvent.Invoke(attackerTroop, attackedTroop, party, usedWeapon, isFatal, xp);
	}

	public override void OnCharacterPortraitPopUpOpened(CharacterObject character)
	{
		_timeControlModeBeforePopUpOpened = Campaign.Current.TimeControlMode;
		Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
		Campaign.Current.SetTimeControlModeLock(isLocked: true);
		Instance._characterPortraitPopUpOpenedEvent.Invoke(character);
	}

	public override void OnCharacterPortraitPopUpClosed()
	{
		Campaign.Current.SetTimeControlModeLock(isLocked: false);
		Campaign.Current.TimeControlMode = _timeControlModeBeforePopUpOpened;
		_timeControlModeBeforePopUpOpened = CampaignTimeControlMode.Stop;
		Instance._characterPortraitPopUpClosedEvent.Invoke();
	}

	public override void OnPlayerStartTalkFromMenu(Hero hero)
	{
		Instance._playerStartTalkFromMenu.Invoke(hero);
	}

	public override void OnGameMenuOptionSelected(GameMenu gameMenu, GameMenuOption gameMenuOption)
	{
		Instance._gameMenuOptionSelectedEvent.Invoke(gameMenu, gameMenuOption);
	}

	public override void OnPlayerStartRecruitment(CharacterObject recruitTroopCharacter)
	{
		Instance._playerStartRecruitmentEvent.Invoke(recruitTroopCharacter);
	}

	public override void OnBeforePlayerCharacterChanged(Hero oldPlayer, Hero newPlayer)
	{
		Instance._onBeforePlayerCharacterChangedEvent.Invoke(oldPlayer, newPlayer);
	}

	public override void OnPlayerCharacterChanged(Hero oldPlayer, Hero newPlayer, MobileParty newMainParty, bool isMainPartyChanged)
	{
		Instance._onPlayerCharacterChangedEvent.Invoke(oldPlayer, newPlayer, newMainParty, isMainPartyChanged);
	}

	public override void OnClanLeaderChanged(Hero oldLeader, Hero newLeader)
	{
		Instance._onClanLeaderChangedEvent.Invoke(oldLeader, newLeader);
	}

	public override void OnSiegeEventStarted(SiegeEvent siegeEvent)
	{
		Instance._onSiegeEventStartedEvent.Invoke(siegeEvent);
	}

	public override void OnPlayerSiegeStarted()
	{
		Instance._onPlayerSiegeStartedEvent.Invoke();
	}

	public override void OnSiegeEventEnded(SiegeEvent siegeEvent)
	{
		Instance._onSiegeEventEndedEvent.Invoke(siegeEvent);
	}

	public override void OnSiegeAftermathApplied(MobileParty attackerParty, Settlement settlement, SiegeAftermathAction.SiegeAftermath aftermathType, Clan previousSettlementOwner, Dictionary<MobileParty, float> partyContributions)
	{
		Instance._siegeAftermathAppliedEvent.Invoke(attackerParty, settlement, aftermathType, previousSettlementOwner, partyContributions);
	}

	public override void OnSiegeBombardmentHit(MobileParty besiegerParty, Settlement besiegedSettlement, BattleSideEnum side, SiegeEngineType weapon, SiegeBombardTargets target)
	{
		Instance._onSiegeBombardmentHitEvent.Invoke(besiegerParty, besiegedSettlement, side, weapon, target);
	}

	public override void OnSiegeBombardmentWallHit(MobileParty besiegerParty, Settlement besiegedSettlement, BattleSideEnum side, SiegeEngineType weapon, bool isWallCracked)
	{
		Instance._onSiegeBombardmentWallHitEvent.Invoke(besiegerParty, besiegedSettlement, side, weapon, isWallCracked);
	}

	public override void OnSiegeEngineDestroyed(MobileParty besiegerParty, Settlement besiegedSettlement, BattleSideEnum side, SiegeEngineType destroyedEngine)
	{
		Instance._onSiegeEngineDestroyedEvent.Invoke(besiegerParty, besiegedSettlement, side, destroyedEngine);
	}

	public override void OnTradeRumorIsTaken(List<TradeRumor> newRumors, Settlement sourceSettlement = null)
	{
		Instance._onTradeRumorIsTakenEvent.Invoke(newRumors, sourceSettlement);
	}

	public override void OnCheckForIssue(Hero hero)
	{
		Instance._onCheckForIssueEvent.Invoke(hero);
	}

	public override void OnIssueUpdated(IssueBase issue, IssueBase.IssueUpdateDetails details, Hero issueSolver = null)
	{
		Instance._onIssueUpdatedEvent.Invoke(issue, details, issueSolver);
	}

	public override void OnTroopsDeserted(MobileParty mobileParty, TroopRoster desertedTroops)
	{
		Instance._onTroopsDesertedEvent.Invoke(mobileParty, desertedTroops);
	}

	public override void OnTroopRecruited(Hero recruiterHero, Settlement recruitmentSettlement, Hero recruitmentSource, CharacterObject troop, int amount)
	{
		Instance._onTroopRecruitedEvent.Invoke(recruiterHero, recruitmentSettlement, recruitmentSource, troop, amount);
	}

	public override void OnTroopGivenToSettlement(Hero giverHero, Settlement recipientSettlement, TroopRoster roster)
	{
		Instance._onTroopGivenToSettlementEvent.Invoke(giverHero, recipientSettlement, roster);
	}

	public override void OnItemSold(PartyBase receiverParty, PartyBase payerParty, ItemRosterElement itemRosterElement, int number, Settlement currentSettlement)
	{
		Instance._onItemSoldEvent.Invoke(receiverParty, payerParty, itemRosterElement, number, currentSettlement);
	}

	public override void OnCaravanTransactionCompleted(MobileParty caravanParty, Town town, List<(EquipmentElement, int)> itemRosterElements)
	{
		Instance._onCaravanTransactionCompletedEvent.Invoke(caravanParty, town, itemRosterElements);
	}

	public override void OnPrisonerSold(PartyBase sellerParty, PartyBase buyerParty, TroopRoster prisoners)
	{
		Instance._onPrisonerSoldEvent.Invoke(sellerParty, buyerParty, prisoners);
	}

	public override void OnPartyDisbandStarted(MobileParty disbandParty)
	{
		Instance._onPartyDisbandStartedEvent.Invoke(disbandParty);
	}

	public override void OnPartyDisbanded(MobileParty disbandParty, Settlement relatedSettlement)
	{
		Instance._onPartyDisbandedEvent.Invoke(disbandParty, relatedSettlement);
	}

	public override void OnPartyDisbandCanceled(MobileParty disbandParty)
	{
		Instance._onPartyDisbandCanceledEvent.Invoke(disbandParty);
	}

	public override void OnHideoutSpotted(PartyBase party, PartyBase hideoutParty)
	{
		Instance._hideoutSpottedEvent.Invoke(party, hideoutParty);
	}

	public override void OnHideoutDeactivated(Settlement hideout)
	{
		Instance._hideoutDeactivatedEvent.Invoke(hideout);
	}

	public override void OnHeroSharedFoodWithAnother(Hero supporterHero, Hero supportedHero, float influence)
	{
		Instance._heroSharedFoodWithAnotherHeroEvent.Invoke(supporterHero, supportedHero, influence);
	}

	public override void OnPlayerInventoryExchange(List<(ItemRosterElement, int)> purchasedItems, List<(ItemRosterElement, int)> soldItems, bool isTrading)
	{
		Instance._playerInventoryExchangeEvent.Invoke(purchasedItems, soldItems, isTrading);
	}

	public override void OnItemsDiscardedByPlayer(ItemRoster discardedItems)
	{
		Instance._onItemsDiscardedByPlayerEvent.Invoke(discardedItems);
	}

	public override void OnPersuasionProgressCommitted(Tuple<PersuasionOptionArgs, PersuasionOptionResult> progress)
	{
		Instance._persuasionProgressCommittedEvent.Invoke(progress);
	}

	public override void OnQuestCompleted(QuestBase quest, QuestBase.QuestCompleteDetails detail)
	{
		Instance._onQuestCompletedEvent.Invoke(quest, detail);
	}

	public override void OnQuestStarted(QuestBase quest)
	{
		Instance._onQuestStartedEvent.Invoke(quest);
	}

	public override void OnItemProduced(ItemObject itemObject, Settlement settlement, int count)
	{
		Instance._itemProducedEvent.Invoke(itemObject, settlement, count);
	}

	public override void OnItemConsumed(ItemObject itemObject, Settlement settlement, int count)
	{
		Instance._itemConsumedEvent.Invoke(itemObject, settlement, count);
	}

	public override void OnPartyConsumedFood(MobileParty party)
	{
		Instance._onPartyConsumedFoodEvent.Invoke(party);
	}

	public override void OnBeforeMainCharacterDied(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification = true)
	{
		Instance._onBeforeMainCharacterDiedEvent.Invoke(victim, killer, detail, showNotification);
	}

	public override void OnNewIssueCreated(IssueBase issue)
	{
		Instance._onNewIssueCreatedEvent.Invoke(issue);
	}

	public override void OnIssueOwnerChanged(IssueBase issue, Hero oldOwner)
	{
		Instance._onIssueOwnerChangedEvent.Invoke(issue, oldOwner);
	}

	public override void OnGameOver()
	{
		Instance._onGameOverEvent.Invoke();
	}

	public override void SiegeCompleted(Settlement siegeSettlement, MobileParty attackerParty, bool isWin, MapEvent.BattleTypes battleType)
	{
		Instance._siegeCompletedEvent.Invoke(siegeSettlement, attackerParty, isWin, battleType);
	}

	public override void AfterSiegeCompleted(Settlement siegeSettlement, MobileParty attackerParty, bool isWin, MapEvent.BattleTypes battleType)
	{
		Instance._afterSiegeCompletedEvent.Invoke(siegeSettlement, attackerParty, isWin, battleType);
	}

	public override void SiegeEngineBuilt(SiegeEvent siegeEvent, BattleSideEnum side, SiegeEngineType siegeEngineType)
	{
		Instance._siegeEngineBuiltEvent.Invoke(siegeEvent, side, siegeEngineType);
	}

	public override void RaidCompleted(BattleSideEnum winnerSide, RaidEventComponent raidEvent)
	{
		Instance._raidCompletedEvent.Invoke(winnerSide, raidEvent);
	}

	public override void ForceVolunteersCompleted(BattleSideEnum winnerSide, ForceVolunteersEventComponent forceVolunteersEvent)
	{
		Instance._forceVolunteersCompletedEvent.Invoke(winnerSide, forceVolunteersEvent);
	}

	public override void ForceSuppliesCompleted(BattleSideEnum winnerSide, ForceSuppliesEventComponent forceSuppliesEvent)
	{
		Instance._forceSuppliesCompletedEvent.Invoke(winnerSide, forceSuppliesEvent);
	}

	public override void OnHideoutBattleCompleted(BattleSideEnum winnerSide, HideoutEventComponent hideoutEventComponent, HideoutEventComponent.HideoutBattleEndState battleEndState)
	{
		Instance._hideoutBattleCompletedEvent.Invoke(winnerSide, hideoutEventComponent, battleEndState);
	}

	public override void OnClanDestroyed(Clan destroyedClan)
	{
		Instance._onClanDestroyedEvent.Invoke(destroyedClan);
	}

	public override void OnNewItemCrafted(ItemObject itemObject, ItemModifier overriddenItemModifier, bool isCraftingOrderItem)
	{
		Instance._onNewItemCraftedEvent.Invoke(itemObject, overriddenItemModifier, isCraftingOrderItem);
	}

	public override void CraftingPartUnlocked(CraftingPiece craftingPiece)
	{
		Instance._craftingPartUnlockedEvent.Invoke(craftingPiece);
	}

	public override void OnWorkshopInitialized(Workshop workshop)
	{
		Instance._onWorkshopInitializedEvent.Invoke(workshop);
	}

	public override void OnWorkshopOwnerChanged(Workshop workshop, Hero oldOwner)
	{
		Instance._onWorkshopOwnerChangedEvent.Invoke(workshop, oldOwner);
	}

	public override void OnWorkshopTypeChanged(Workshop workshop)
	{
		Instance._onWorkshopTypeChangedEvent.Invoke(workshop);
	}

	public override void OnBeforeSave()
	{
		Instance._onBeforeSaveEvent.Invoke();
	}

	public override void OnSaveStarted()
	{
		Instance._onSaveStartedEvent.Invoke();
	}

	public override void OnSaveOver(bool isSuccessful, string saveName)
	{
		Instance._onSaveOverEvent.Invoke(isSuccessful, saveName);
	}

	public override void CollectMetadataEntries(List<KeyValuePair<string, string>> pairs)
	{
		Instance._collectMetadataEntriesEvent.Invoke(pairs);
	}

	public override void OnPrisonerTaken(FlattenedTroopRoster roster)
	{
		Instance._onPrisonerTakenEvent.Invoke(roster);
	}

	public override void OnPrisonerReleased(FlattenedTroopRoster roster)
	{
		Instance._onPrisonerReleasedEvent.Invoke(roster);
	}

	public override void OnMainPartyPrisonerRecruited(FlattenedTroopRoster roster)
	{
		Instance._onMainPartyPrisonerRecruitedEvent.Invoke(roster);
	}

	public override void OnPrisonerDonatedToSettlement(MobileParty donatingParty, FlattenedTroopRoster donatedPrisoners, Settlement donatedSettlement)
	{
		Instance._onPrisonerDonatedToSettlementEvent.Invoke(donatingParty, donatedPrisoners, donatedSettlement);
	}

	public override void OnEquipmentSmeltedByHero(Hero hero, EquipmentElement smeltedEquipmentElement)
	{
		Instance._onEquipmentSmeltedByHero.Invoke(hero, smeltedEquipmentElement);
	}

	public override void OnPlayerTradeProfit(int profit)
	{
		Instance._onPlayerTradeProfit.Invoke(profit);
	}

	public override void OnHeroChangedClan(Hero hero, Clan oldClan)
	{
		Instance._onHeroChangedClan.Invoke(hero, oldClan);
	}

	public override void OnHeroGetsBusy(Hero hero, HeroGetsBusyReasons heroGetsBusyReason)
	{
		Instance._onHeroGetsBusy.Invoke(hero, heroGetsBusyReason);
	}

	public override void OnCollectLootItems(PartyBase winnerParty, ItemRoster gainedLoots)
	{
		Instance._onCollectLootItems.Invoke(winnerParty, gainedLoots);
	}

	public override void OnLootDistributedToParty(PartyBase winnerParty, PartyBase defeatedParty, ItemRoster lootedItems)
	{
		Instance._onLootDistributedToPartyEvent.Invoke(winnerParty, defeatedParty, lootedItems);
	}

	public override void OnHeroTeleportationRequested(Hero hero, Settlement targetSettlement, MobileParty targetParty, TeleportHeroAction.TeleportationDetail detail)
	{
		Instance._onHeroTeleportationRequestedEvent.Invoke(hero, targetSettlement, targetParty, detail);
	}

	public override void OnPartyLeaderChangeOfferCanceled(MobileParty party)
	{
		Instance._onPartyLeaderChangeOfferCanceledEvent.Invoke(party);
	}

	public override void OnPartyLeaderChanged(MobileParty mobileParty, Hero oldLeader)
	{
		Instance._onPartyLeaderChangedEvent.Invoke(mobileParty, oldLeader);
	}

	public override void OnClanInfluenceChanged(Clan clan, float change)
	{
		Instance._onClanInfluenceChangedEvent.Invoke(clan, change);
	}

	public override void OnPlayerPartyKnockedOrKilledTroop(CharacterObject strikedTroop)
	{
		Instance._onPlayerPartyKnockedOrKilledTroopEvent.Invoke(strikedTroop);
	}

	public override void OnPlayerEarnedGoldFromAsset(DefaultClanFinanceModel.AssetIncomeType incomeType, int incomeAmount)
	{
		Instance._onPlayerEarnedGoldFromAssetEvent.Invoke(incomeType, incomeAmount);
	}

	public override void OnClanEarnedGoldFromTribute(Clan receiverClan, IFaction payingFaction)
	{
		Instance._onClanEarnedGoldFromTributeEvent.Invoke(receiverClan, payingFaction);
	}

	public override void OnMainPartyStarving()
	{
		Instance._onMainPartyStarving.Invoke();
	}

	public override void OnPlayerJoinedTournament(Town town, bool isParticipant)
	{
		Instance._onPlayerJoinedTournamentEvent.Invoke(town, isParticipant);
	}

	public override void OnHeroUnregistered(Hero hero)
	{
		Instance._onHeroUnregisteredEvent.Invoke(hero);
	}

	public override void OnConfigChanged()
	{
		Instance._onConfigChanged.Invoke();
	}

	public override void OnCraftingOrderCompleted(Town town, CraftingOrder craftingOrder, ItemObject craftedItem, Hero completerHero)
	{
		Instance._onCraftingOrderCompleted.Invoke(town, craftingOrder, craftedItem, completerHero);
	}

	public override void OnItemsRefined(Hero hero, Crafting.RefiningFormula refineFormula)
	{
		Instance._onItemsRefined.Invoke(hero, refineFormula);
	}

	public override void OnHeirSelectionRequested(Dictionary<Hero, int> heirApparents)
	{
		Instance._onHeirSelectionRequested.Invoke(heirApparents);
	}

	public override void OnHeirSelectionOver(Hero selectedHero)
	{
		Instance._onHeirSelectionOver.Invoke(selectedHero);
	}

	public override void OnMobilePartyRaftStateChanged(MobileParty mobileParty)
	{
		Instance._onMobilePartyRaftStateChanged.Invoke(mobileParty);
	}

	public override void OnCharacterCreationInitialized(CharacterCreationManager characterCreationManager)
	{
		Instance._onCharacterCreationInitialized.Invoke(characterCreationManager);
	}

	public override void OnShipDestroyed(PartyBase owner, Ship ship, DestroyShipAction.ShipDestroyDetail detail)
	{
		Instance._onShipDestroyedEvent.Invoke(owner, ship, detail);
	}

	public override void OnShipOwnerChanged(Ship ship, PartyBase oldOwner, ChangeShipOwnerAction.ShipOwnerChangeDetail changeDetail)
	{
		Instance._onShipOwnerChangedEvent.Invoke(ship, oldOwner, changeDetail);
	}

	public override void OnShipRepaired(Ship ship, Settlement repairPort)
	{
		Instance._onShipRepairedEvent.Invoke(ship, repairPort);
	}

	public override void OnShipCreated(Ship ship, Settlement createdSettlement)
	{
		Instance._onShipCreatedEvent.Invoke(ship, createdSettlement);
	}

	public override void OnFigureheadUnlocked(Figurehead figurehead)
	{
		Instance._onFigureheadUnlockedEvent.Invoke(figurehead);
	}

	public override void OnPartyLeftArmy(MobileParty party, Army army)
	{
		Instance._onPartyLeftArmyEvent.Invoke(party, army);
	}

	public override void OnPartyAddedToMapEvent(PartyBase partyBase)
	{
		Instance._onPartyAddedToMapEventEvent.Invoke(partyBase);
	}

	public override void OnIncidentResolved(Incident incident)
	{
		Instance._onIncidentResolvedEvent.Invoke(incident);
	}

	public override void OnMobilePartyNavigationStateChanged(MobileParty mobileParty)
	{
		Instance._onMobilePartyNavigationStateChangedEvent.Invoke(mobileParty);
	}

	public override void OnMobilePartyJoinedToSiegeEvent(MobileParty mobileParty)
	{
		Instance._onMobilePartyJoinedToSiegeEventEvent.Invoke(mobileParty);
	}

	public override void OnMobilePartyLeftSiegeEvent(MobileParty mobileParty)
	{
		Instance._onMobilePartyLeftSiegeEventEvent.Invoke(mobileParty);
	}

	public override void OnBlockadeActivated(SiegeEvent siegeEvent)
	{
		Instance._onBlockadeActivatedEvent.Invoke(siegeEvent);
	}

	public override void OnBlockadeDeactivated(SiegeEvent siegeEvent)
	{
		Instance._onBlockadeDeactivatedEvent.Invoke(siegeEvent);
	}

	public override void OnMapMarkerCreated(MapMarker mapMarker)
	{
		Instance._onMapMarkerCreatedEvent.Invoke(mapMarker);
	}

	public override void OnMapMarkerRemoved(MapMarker mapMarker)
	{
		Instance._onMapMarkerRemovedEvent.Invoke(mapMarker);
	}

	public override void OnAllianceStarted(Kingdom kingdom1, Kingdom kingdom2)
	{
		Instance._onAllianceStartedEvent.Invoke(kingdom1, kingdom2);
	}

	public override void OnAllianceEnded(Kingdom kingdom1, Kingdom kingdom2)
	{
		Instance._onAllianceEndedEvent.Invoke(kingdom1, kingdom2);
	}

	public override void OnCallToWarAgreementStarted(Kingdom callingKingdom, Kingdom calledKingdom, Kingdom kingdomToCallToWarAgainst)
	{
		Instance._onCallToWarAgreementStartedEvent.Invoke(callingKingdom, calledKingdom, kingdomToCallToWarAgainst);
	}

	public override void OnCallToWarAgreementEnded(Kingdom callingKingdom, Kingdom calledKingdom, Kingdom kingdomToCallToWarAgainst)
	{
		Instance._onCallToWarAgreementEndedEvent.Invoke(callingKingdom, calledKingdom, kingdomToCallToWarAgainst);
	}

	public override void CanHeroLeadParty(Hero hero, ref bool result)
	{
		Instance._canHeroLeadPartyEvent.Invoke(hero, ref result);
	}

	public override void CanHeroMarry(Hero hero, ref bool result)
	{
		Instance._canMarryEvent.Invoke(hero, ref result);
	}

	public override void CanHeroEquipmentBeChanged(Hero hero, ref bool result)
	{
		Instance._canHeroEquipmentBeChangedEvent.Invoke(hero, ref result);
	}

	public override void CanBeGovernorOrHavePartyRole(Hero hero, ref bool result)
	{
		Instance._canBeGovernorOrHavePartyRoleEvent.Invoke(hero, ref result);
	}

	public override void CanHeroDie(Hero hero, KillCharacterAction.KillCharacterActionDetail causeOfDeath, ref bool result)
	{
		Instance._canHeroDieEvent.Invoke(hero, causeOfDeath, ref result);
	}

	public override void CanPlayerMeetWithHeroAfterConversation(Hero hero, ref bool result)
	{
		Instance._canPlayerMeetWithHeroAfterConversationEvent.Invoke(hero, ref result);
	}

	public override void CanHeroBecomePrisoner(Hero hero, ref bool result)
	{
		Instance._canHeroBecomePrisonerEvent.Invoke(hero, ref result);
	}

	public override void CanMoveToSettlement(Hero hero, ref bool result)
	{
		Instance._canMoveToSettlementEvent.Invoke(hero, ref result);
	}

	public override void CanHaveCampaignIssues(Hero hero, ref bool result)
	{
		Instance._canHaveCampaignIssues.Invoke(hero, ref result);
	}

	public override void IsSettlementBusy(Settlement settlement, object asker, ref int priority)
	{
		Instance._isSettlementBusy.Invoke(settlement, asker, ref priority);
	}

	public override void OnMapEventContinuityNeedsUpdate(IFaction faction)
	{
		Instance._onMapEventContinuityNeedsUpdate.Invoke(faction);
	}
}
