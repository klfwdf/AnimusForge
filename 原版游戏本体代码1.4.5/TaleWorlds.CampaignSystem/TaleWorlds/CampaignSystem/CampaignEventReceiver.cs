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

public abstract class CampaignEventReceiver
{
	public virtual void RemoveListeners(object o)
	{
	}

	public virtual void OnCharacterCreationIsOver()
	{
	}

	public virtual void OnHeroLevelledUp(Hero hero, bool shouldNotify = true)
	{
	}

	public virtual void OnHomeHideoutChanged(BanditPartyComponent banditPartyComponent, Hideout oldHomeHideout)
	{
	}

	public virtual void OnHeroGainedSkill(Hero hero, SkillObject skill, int change = 1, bool shouldNotify = true)
	{
	}

	public virtual void OnHeroCreated(Hero hero, bool isBornNaturally = false)
	{
	}

	public virtual void OnHeroActivated(Hero hero, Hero.CharacterStates previousState)
	{
	}

	public virtual void OnHeroWounded(Hero woundedHero)
	{
	}

	public virtual void OnHeroRelationChanged(Hero effectiveHero, Hero effectiveHeroGainedRelationWith, int relationChange, bool showNotification, ChangeRelationAction.ChangeRelationDetail detail, Hero originalHero, Hero originalGainedRelationWith)
	{
	}

	public virtual void OnQuestLogAdded(QuestBase quest, bool hideInformation)
	{
	}

	public virtual void OnIssueLogAdded(IssueBase issue, bool hideInformation)
	{
	}

	public virtual void OnClanTierChanged(Clan clan, bool shouldNotify = true)
	{
	}

	public virtual void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail actionDetail, bool showNotification = true)
	{
	}

	public virtual void OnClanDefected(Clan clan, Kingdom oldKingdom, Kingdom newKingdom)
	{
	}

	public virtual void OnClanCreated(Clan clan, bool isCompanion)
	{
	}

	public virtual void OnHeroJoinedParty(Hero hero, MobileParty mobileParty)
	{
	}

	public virtual void OnKingdomDecisionAdded(KingdomDecision decision, bool isPlayerInvolved)
	{
	}

	public virtual void OnKingdomDecisionCancelled(KingdomDecision decision, bool isPlayerInvolved)
	{
	}

	public virtual void OnKingdomDecisionConcluded(KingdomDecision decision, DecisionOutcome chosenOutcome, bool isPlayerInvolved)
	{
	}

	public virtual void OnHeroOrPartyTradedGold((Hero, PartyBase) giver, (Hero, PartyBase) recipient, (int, string) goldAmount, bool showNotification)
	{
	}

	public virtual void OnHeroOrPartyGaveItem((Hero, PartyBase) giver, (Hero, PartyBase) receiver, ItemRosterElement itemRosterElement, bool showNotification)
	{
	}

	public virtual void OnBanditPartyRecruited(MobileParty banditParty)
	{
	}

	public virtual void OnArmyCreated(Army army)
	{
	}

	public virtual void OnPartyAttachedAnotherParty(MobileParty mobileParty)
	{
	}

	public virtual void OnNearbyPartyAddedToPlayerMapEvent(MobileParty mobileParty)
	{
	}

	public virtual void OnArmyDispersed(Army army, Army.ArmyDispersionReason reason, bool isPlayersArmy)
	{
	}

	public virtual void OnArmyGathered(Army army, IMapPoint gatheringPoint)
	{
	}

	public virtual void OnPerkOpened(Hero hero, PerkObject perk)
	{
	}

	public virtual void OnPerkReset(Hero hero, PerkObject perk)
	{
	}

	public virtual void OnPlayerTraitChanged(TraitObject trait, int previousLevel)
	{
	}

	public virtual void OnVillageStateChanged(Village village, Village.VillageStates oldState, Village.VillageStates newState, MobileParty raiderParty)
	{
	}

	public virtual void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
	{
	}

	public virtual void OnAfterSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
	{
	}

	public virtual void OnBeforeSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
	{
	}

	public virtual void OnMercenaryTroopChangedInTown(Town town, CharacterObject oldTroopType, CharacterObject newTroopType)
	{
	}

	public virtual void OnMercenaryNumberChangedInTown(Town town, int oldNumber, int newNumber)
	{
	}

	public virtual void OnAlleyOwnerChanged(Alley alley, Hero newOwner, Hero oldOwner)
	{
	}

	public virtual void OnAlleyClearedByPlayer(Alley alley)
	{
	}

	public virtual void OnAlleyOccupiedByPlayer(Alley alley, TroopRoster troops)
	{
	}

	public virtual void OnRomanticStateChanged(Hero hero1, Hero hero2, Romance.RomanceLevelEnum romanceLevel)
	{
	}

	public virtual void OnBeforeHeroesMarried(Hero hero1, Hero hero2, bool showNotification = true)
	{
	}

	public virtual void OnPlayerEliminatedFromTournament(int round, Town town)
	{
	}

	public virtual void OnPlayerStartedTournamentMatch(Town town)
	{
	}

	public virtual void OnTournamentStarted(Town town)
	{
	}

	public virtual void OnTournamentFinished(CharacterObject winner, MBReadOnlyList<CharacterObject> participants, Town town, ItemObject prize)
	{
	}

	public virtual void OnTournamentCancelled(Town town)
	{
	}

	public virtual void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail declareWarDetail)
	{
	}

	public virtual void OnMakePeace(IFaction side1Faction, IFaction side2Faction, MakePeaceAction.MakePeaceDetail detail)
	{
	}

	public virtual void OnKingdomCreated(Kingdom createdKingdom)
	{
	}

	public virtual void OnHeroOccupationChanged(Hero hero, Occupation oldOccupation)
	{
	}

	public virtual void OnKingdomDestroyed(Kingdom kingdom)
	{
	}

	public virtual void CanKingdomBeDiscontinued(Kingdom kingdom, ref bool result)
	{
	}

	public virtual void OnBarterAccepted(Hero offererHero, Hero otherHero, List<Barterable> barters)
	{
	}

	public virtual void OnBarterCanceled(Hero offererHero, Hero otherHero, List<Barterable> barters)
	{
	}

	public virtual void OnStartBattle(PartyBase attackerParty, PartyBase defenderParty, object subject, bool showNotification)
	{
	}

	public virtual void OnRebellionFinished(Settlement settlement, Clan oldOwnerClan)
	{
	}

	public virtual void TownRebelliousStateChanged(Town town, bool rebelliousState)
	{
	}

	public virtual void OnRebelliousClanDisbandedAtSettlement(Settlement settlement, Clan clan)
	{
	}

	public virtual void OnItemsLooted(MobileParty mobileParty, ItemRoster items)
	{
	}

	public virtual void OnMobilePartyDestroyed(MobileParty mobileParty, PartyBase destroyerParty)
	{
	}

	public virtual void OnMobilePartyCreated(MobileParty party)
	{
	}

	public virtual void OnMapInteractableCreated(IInteractablePoint interactable)
	{
	}

	public virtual void OnMapInteractableDestroyed(IInteractablePoint interactable)
	{
	}

	public virtual void OnMobilePartyQuestStatusChanged(MobileParty party, bool isUsedByQuest)
	{
	}

	public virtual void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification = true)
	{
	}

	public virtual void OnBeforeHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification = true)
	{
	}

	public virtual void OnChildEducationCompleted(Hero hero, int age)
	{
	}

	public virtual void OnHeroComesOfAge(Hero hero)
	{
	}

	public virtual void OnHeroReachesTeenAge(Hero hero)
	{
	}

	public virtual void OnHeroGrowsOutOfInfancy(Hero hero)
	{
	}

	public virtual void OnCharacterDefeated(Hero winner, Hero loser)
	{
	}

	public virtual void OnHeroPrisonerTaken(PartyBase capturer, Hero prisoner)
	{
	}

	public virtual void OnHeroPrisonerReleased(Hero prisoner, PartyBase party, IFaction capturerFaction, EndCaptivityDetail detail, bool showNotification = true)
	{
	}

	public virtual void OnCharacterBecameFugitive(Hero hero, bool showNotification)
	{
	}

	public virtual void OnPlayerMetHero(Hero hero)
	{
	}

	public virtual void OnPlayerLearnsAboutHero(Hero hero)
	{
	}

	public virtual void OnRenownGained(Hero hero, int gainedRenown, bool doNotNotify)
	{
	}

	public virtual void OnCrimeRatingChanged(IFaction kingdom, float deltaCrimeAmount)
	{
	}

	public virtual void OnNewCompanionAdded(Hero newCompanion)
	{
	}

	public virtual void OnAfterMissionStarted(IMission iMission)
	{
	}

	public virtual void OnGameMenuOpened(MenuCallbackArgs args)
	{
	}

	public virtual void OnVillageBecomeNormal(Village village)
	{
	}

	public virtual void OnVillageBeingRaided(Village village)
	{
	}

	public virtual void OnVillageLooted(Village village)
	{
	}

	public virtual void OnAgentJoinedConversation(IAgent agent)
	{
	}

	public virtual void OnConversationEnded(IEnumerable<CharacterObject> characters)
	{
	}

	public virtual void OnMapEventEnded(MapEvent mapEvent)
	{
	}

	public virtual void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
	{
	}

	public virtual void OnRansomOfferedToPlayer(Hero captiveHero)
	{
	}

	public virtual void OnPrisonersChangeInSettlement(Settlement settlement, FlattenedTroopRoster prisonerRoster, Hero prisonerHero, bool takenFromDungeon)
	{
	}

	public virtual void OnMissionStarted(IMission mission)
	{
	}

	public virtual void OnRansomOfferCancelled(Hero captiveHero)
	{
	}

	public virtual void OnPeaceOfferedToPlayer(IFaction opponentFaction, int tributeAmount, int tributeDuration)
	{
	}

	public virtual void OnTradeAgreementSigned(Kingdom kingdom, Kingdom other)
	{
	}

	public virtual void OnPeaceOfferResolved(IFaction opponentFaction)
	{
	}

	public virtual void OnMarriageOfferedToPlayer(Hero suitor, Hero maiden)
	{
	}

	public virtual void OnMarriageOfferCanceled(Hero suitor, Hero maiden)
	{
	}

	public virtual void OnVassalOrMercenaryServiceOfferedToPlayer(Kingdom offeredKingdom)
	{
	}

	public virtual void OnVassalOrMercenaryServiceOfferCanceled(Kingdom offeredKingdom)
	{
	}

	public virtual void OnPlayerBoardGameOver(Hero opposingHero, BoardGameHelper.BoardGameState state)
	{
	}

	public virtual void OnCommonAreaStateChanged(Alley alley, Alley.AreaState oldState, Alley.AreaState newState)
	{
	}

	public virtual void BeforeMissionOpened()
	{
	}

	public virtual void OnPartyRemoved(PartyBase party)
	{
	}

	public virtual void OnPartySizeChanged(PartyBase party)
	{
	}

	public virtual void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
	{
	}

	public virtual void OnGovernorChanged(Town fortification, Hero oldGovernor, Hero newGovernor)
	{
	}

	public virtual void OnSettlementLeft(MobileParty party, Settlement settlement)
	{
	}

	public virtual void Tick(float dt)
	{
	}

	public virtual void OnSessionStart(CampaignGameStarter campaignGameStarter)
	{
	}

	public virtual void OnAfterSessionStart(CampaignGameStarter campaignGameStarter)
	{
	}

	public virtual void OnNewGameCreated(CampaignGameStarter campaignGameStarter)
	{
	}

	public virtual void OnGameLoaded(CampaignGameStarter campaignGameStarter)
	{
	}

	public virtual void OnGameEarlyLoaded(CampaignGameStarter campaignGameStarter)
	{
	}

	public virtual void OnPlayerTradeProfit(int profit)
	{
	}

	public virtual void OnRulingClanChanged(Kingdom kingdom, Clan oldRulingClan)
	{
	}

	public virtual void OnPrisonerReleased(FlattenedTroopRoster roster)
	{
	}

	public virtual void OnGameLoadFinished()
	{
	}

	public virtual void OnPartyJoinedArmy(MobileParty mobileParty)
	{
	}

	public virtual void OnPartyRemovedFromArmy(MobileParty mobileParty)
	{
	}

	public virtual void OnArmyOverlaySetDirty()
	{
	}

	public virtual void OnPlayerDesertedBattle(int sacrificedMenCount)
	{
	}

	public virtual void OnPlayerArmyLeaderChangedBehavior()
	{
	}

	public virtual void MissionTick(float dt)
	{
	}

	public virtual void OnChildConceived(Hero mother)
	{
	}

	public virtual void OnGivenBirth(Hero mother, List<Hero> aliveChildren, int stillbornCount)
	{
	}

	public virtual void OnUnitRecruited(CharacterObject character, int amount)
	{
	}

	public virtual void OnPlayerBattleEnd(MapEvent mapEvent)
	{
	}

	public virtual void OnMissionEnded(IMission mission)
	{
	}

	public virtual void TickPartialHourlyAi(MobileParty party)
	{
	}

	public virtual void QuarterDailyPartyTick(MobileParty party)
	{
	}

	public virtual void AiHourlyTick(MobileParty party, PartyThinkParams partyThinkParams)
	{
	}

	public virtual void HourlyTick()
	{
	}

	public virtual void QuarterHourlyTick()
	{
	}

	public virtual void HourlyTickParty(MobileParty mobileParty)
	{
	}

	public virtual void HourlyTickSettlement(Settlement settlement)
	{
	}

	public virtual void HourlyTickClan(Clan clan)
	{
	}

	public virtual void DailyTick()
	{
	}

	public virtual void DailyTickParty(MobileParty mobileParty)
	{
	}

	public virtual void DailyTickTown(Town town)
	{
	}

	public virtual void DailyTickSettlement(Settlement settlement)
	{
	}

	public virtual void DailyTickClan(Clan clan)
	{
	}

	public virtual void OnPlayerBodyPropertiesChanged()
	{
	}

	public virtual void WeeklyTick()
	{
	}

	public virtual void CollectAvailableTutorials(ref List<CampaignTutorial> tutorials)
	{
	}

	public virtual void DailyTickHero(Hero hero)
	{
	}

	public virtual void OnTutorialCompleted(string tutorial)
	{
	}

	public virtual void OnBuildingLevelChanged(Town town, Building building, int levelChange)
	{
	}

	public virtual void BeforeGameMenuOpened(MenuCallbackArgs args)
	{
	}

	public virtual void AfterGameMenuInitialized(MenuCallbackArgs args)
	{
	}

	public virtual void OnBarterablesRequested(BarterData args)
	{
	}

	public virtual void OnPartyVisibilityChanged(PartyBase party)
	{
	}

	public virtual void OnCompanionRemoved(Hero companion, RemoveCompanionAction.RemoveCompanionDetail detail)
	{
	}

	public virtual void TrackDetected(Track track)
	{
	}

	public virtual void TrackLost(Track track)
	{
	}

	public virtual void LocationCharactersAreReadyToSpawn(Dictionary<string, int> unusedUsablePointCount)
	{
	}

	public virtual void LocationCharactersSimulated()
	{
	}

	public virtual void OnBeforePlayerAgentSpawn(ref MatrixFrame spawnFrame)
	{
	}

	public virtual void OnPlayerAgentSpawned()
	{
	}

	public virtual void OnPlayerUpgradedTroops(CharacterObject upgradeFromTroop, CharacterObject upgradeToTroop, int number)
	{
	}

	public virtual void OnHeroCombatHit(CharacterObject attackerTroop, CharacterObject attackedTroop, PartyBase party, WeaponComponentData usedWeapon, bool isFatal, int xp)
	{
	}

	public virtual void OnCharacterPortraitPopUpOpened(CharacterObject character)
	{
	}

	public virtual void OnCharacterPortraitPopUpClosed()
	{
	}

	public virtual void OnPlayerStartTalkFromMenu(Hero hero)
	{
	}

	public virtual void OnGameMenuOptionSelected(GameMenu gameMenu, GameMenuOption gameMenuOption)
	{
	}

	public virtual void OnPlayerStartRecruitment(CharacterObject recruitTroopCharacter)
	{
	}

	public virtual void OnBeforePlayerCharacterChanged(Hero oldPlayer, Hero newPlayer)
	{
	}

	public virtual void OnPlayerCharacterChanged(Hero oldPlayer, Hero newPlayer, MobileParty newMainParty, bool isMainPartyChanged)
	{
	}

	public virtual void OnClanLeaderChanged(Hero oldLeader, Hero newLeader)
	{
	}

	public virtual void OnSiegeEventStarted(SiegeEvent siegeEvent)
	{
	}

	public virtual void OnPlayerSiegeStarted()
	{
	}

	public virtual void OnSiegeEventEnded(SiegeEvent siegeEvent)
	{
	}

	public virtual void OnSiegeAftermathApplied(MobileParty attackerParty, Settlement settlement, SiegeAftermathAction.SiegeAftermath aftermathType, Clan previousSettlementOwner, Dictionary<MobileParty, float> partyContributions)
	{
	}

	public virtual void OnSiegeBombardmentHit(MobileParty besiegerParty, Settlement besiegedSettlement, BattleSideEnum side, SiegeEngineType weapon, SiegeBombardTargets target)
	{
	}

	public virtual void OnSiegeBombardmentWallHit(MobileParty besiegerParty, Settlement besiegedSettlement, BattleSideEnum side, SiegeEngineType weapon, bool isWallCracked)
	{
	}

	public virtual void OnSiegeEngineDestroyed(MobileParty besiegerParty, Settlement besiegedSettlement, BattleSideEnum side, SiegeEngineType destroyedEngine)
	{
	}

	public virtual void OnTradeRumorIsTaken(List<TradeRumor> newRumors, Settlement sourceSettlement = null)
	{
	}

	public virtual void OnCheckForIssue(Hero hero)
	{
	}

	public virtual void OnIssueUpdated(IssueBase issue, IssueBase.IssueUpdateDetails details, Hero issueSolver)
	{
	}

	public virtual void OnTroopsDeserted(MobileParty mobileParty, TroopRoster desertedTroops)
	{
	}

	public virtual void OnTroopRecruited(Hero recruiterHero, Settlement recruitmentSettlement, Hero recruitmentSource, CharacterObject troop, int amount)
	{
	}

	public virtual void OnTroopGivenToSettlement(Hero giverHero, Settlement recipientSettlement, TroopRoster roster)
	{
	}

	public virtual void OnItemSold(PartyBase receiverParty, PartyBase payerParty, ItemRosterElement itemRosterElement, int number, Settlement currentSettlement)
	{
	}

	public virtual void OnCaravanTransactionCompleted(MobileParty caravanParty, Town town, List<(EquipmentElement, int)> itemRosterElements)
	{
	}

	public virtual void OnPrisonerSold(PartyBase sellerParty, PartyBase buyerParty, TroopRoster prisoners)
	{
	}

	public virtual void OnPartyDisbanded(MobileParty disbandParty, Settlement relatedSettlement)
	{
	}

	public virtual void OnPartyDisbandStarted(MobileParty disbandParty)
	{
	}

	public virtual void OnPartyDisbandCanceled(MobileParty disbandParty)
	{
	}

	public virtual void OnHideoutSpotted(PartyBase party, PartyBase hideoutParty)
	{
	}

	public virtual void OnHideoutDeactivated(Settlement hideout)
	{
	}

	public virtual void OnHideoutBattleCompleted(BattleSideEnum winnerSide, HideoutEventComponent hideoutEventComponent, HideoutEventComponent.HideoutBattleEndState battleEndState)
	{
	}

	public virtual void OnPlayerInventoryExchange(List<(ItemRosterElement, int)> purchasedItems, List<(ItemRosterElement, int)> soldItems, bool isTrading)
	{
	}

	public virtual void OnItemsDiscardedByPlayer(ItemRoster roster)
	{
	}

	public virtual void OnPersuasionProgressCommitted(Tuple<PersuasionOptionArgs, PersuasionOptionResult> progress)
	{
	}

	public virtual void OnHeroSharedFoodWithAnother(Hero supporterHero, Hero supportedHero, float influence)
	{
	}

	public virtual void OnQuestCompleted(QuestBase quest, QuestBase.QuestCompleteDetails detail)
	{
	}

	public virtual void OnQuestStarted(QuestBase quest)
	{
	}

	public virtual void OnItemProduced(ItemObject itemObject, Settlement settlement, int count)
	{
	}

	public virtual void OnItemConsumed(ItemObject itemObject, Settlement settlement, int count)
	{
	}

	public virtual void OnPartyConsumedFood(MobileParty party)
	{
	}

	public virtual void SiegeCompleted(Settlement siegeSettlement, MobileParty attackerParty, bool isWin, MapEvent.BattleTypes battleType)
	{
	}

	public virtual void AfterSiegeCompleted(Settlement siegeSettlement, MobileParty attackerParty, bool isWin, MapEvent.BattleTypes battleType)
	{
	}

	public virtual void SiegeEngineBuilt(SiegeEvent siegeEvent, BattleSideEnum side, SiegeEngineType siegeEngine)
	{
	}

	public virtual void RaidCompleted(BattleSideEnum winnerSide, RaidEventComponent raidEvent)
	{
	}

	public virtual void ForceSuppliesCompleted(BattleSideEnum winnerSide, ForceSuppliesEventComponent forceSuppliesEvent)
	{
	}

	public virtual void ForceVolunteersCompleted(BattleSideEnum winnerSide, ForceVolunteersEventComponent forceVolunteersEvent)
	{
	}

	public virtual void OnBeforeMainCharacterDied(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification = true)
	{
	}

	public virtual void OnGameOver()
	{
	}

	public virtual void OnClanDestroyed(Clan destroyedClan)
	{
	}

	public virtual void OnNewIssueCreated(IssueBase issue)
	{
	}

	public virtual void OnIssueOwnerChanged(IssueBase issue, Hero oldOwner)
	{
	}

	public virtual void OnNewItemCrafted(ItemObject itemObject)
	{
	}

	public virtual void OnWorkshopInitialized(Workshop workshop)
	{
	}

	public virtual void OnWorkshopOwnerChanged(Workshop workshop, Hero oldOwner)
	{
	}

	public virtual void OnWorkshopTypeChanged(Workshop workshop)
	{
	}

	public virtual void CraftingPartUnlocked(CraftingPiece craftingPiece)
	{
	}

	public virtual void OnNewItemCrafted(ItemObject itemObject, ItemModifier overriddenItemModifier, bool isCraftingOrderItem)
	{
	}

	public virtual void OnEquipmentSmeltedByHero(Hero hero, EquipmentElement equipmentElement)
	{
	}

	public virtual void OnBeforeSave()
	{
	}

	public virtual void OnMainPartyPrisonerRecruited(FlattenedTroopRoster roster)
	{
	}

	public virtual void OnPrisonerTaken(FlattenedTroopRoster roster)
	{
	}

	public virtual void OnPrisonerDonatedToSettlement(MobileParty donatingParty, FlattenedTroopRoster donatedPrisoners, Settlement donatedSettlement)
	{
	}

	public virtual void CanMoveToSettlement(Hero hero, ref bool result)
	{
	}

	public virtual void OnHeroChangedClan(Hero hero, Clan oldClan)
	{
	}

	public virtual void CanHeroDie(Hero hero, KillCharacterAction.KillCharacterActionDetail causeOfDeath, ref bool result)
	{
	}

	public virtual void CanPlayerMeetWithHeroAfterConversation(Hero hero, ref bool result)
	{
	}

	public virtual void CanHeroBecomePrisoner(Hero hero, ref bool result)
	{
	}

	public virtual void CanBeGovernorOrHavePartyRole(Hero hero, ref bool result)
	{
	}

	public virtual void OnSaveOver(bool isSuccessful, string saveName)
	{
	}

	public virtual void CollectMetadataEntries(List<KeyValuePair<string, string>> pairs)
	{
	}

	public virtual void OnSaveStarted()
	{
	}

	public virtual void CanHeroMarry(Hero hero, ref bool result)
	{
	}

	public virtual void OnHeroTeleportationRequested(Hero hero, Settlement targetSettlement, MobileParty targetParty, TeleportHeroAction.TeleportationDetail detail)
	{
	}

	public virtual void OnPartyLeaderChangeOfferCanceled(MobileParty party)
	{
	}

	public virtual void OnPartyLeaderChanged(MobileParty mobileParty, Hero oldLeader)
	{
	}

	public virtual void OnClanInfluenceChanged(Clan clan, float change)
	{
	}

	public virtual void OnPlayerPartyKnockedOrKilledTroop(CharacterObject strikedTroop)
	{
	}

	public virtual void OnPlayerEarnedGoldFromAsset(DefaultClanFinanceModel.AssetIncomeType incomeType, int incomeAmount)
	{
	}

	public virtual void OnClanEarnedGoldFromTribute(Clan receiverClan, IFaction payingFaction)
	{
	}

	public virtual void OnCollectLootItems(PartyBase winnerParty, ItemRoster gainedLoots)
	{
	}

	public virtual void OnLootDistributedToParty(PartyBase winnerParty, PartyBase defeatedParty, ItemRoster lootedItems)
	{
	}

	public virtual void OnPlayerJoinedTournament(Town town, bool isParticipant)
	{
	}

	public virtual void OnConfigChanged()
	{
	}

	public virtual void OnMobilePartyRaftStateChanged(MobileParty mobileParty)
	{
	}

	public virtual void OnCharacterCreationInitialized(CharacterCreationManager characterCreationManager)
	{
	}

	public virtual void OnShipDestroyed(PartyBase owner, Ship ship, DestroyShipAction.ShipDestroyDetail detail)
	{
	}

	public virtual void OnShipOwnerChanged(Ship ship, PartyBase oldOwner, ChangeShipOwnerAction.ShipOwnerChangeDetail shipOwnerChangeDetail)
	{
	}

	public virtual void OnFigureheadUnlocked(Figurehead figurehead)
	{
	}

	public virtual void OnShipRepaired(Ship ship, Settlement repairPort)
	{
	}

	public virtual void OnPartyLeftArmy(MobileParty party, Army army)
	{
	}

	public virtual void OnIncidentResolved(Incident incident)
	{
	}

	public virtual void OnPartyAddedToMapEvent(PartyBase partyBase)
	{
	}

	public virtual void OnMobilePartyNavigationStateChanged(MobileParty mobileParty)
	{
	}

	public virtual void OnMobilePartyJoinedToSiegeEvent(MobileParty mobileParty)
	{
	}

	public virtual void OnMobilePartyLeftSiegeEvent(MobileParty mobileParty)
	{
	}

	public virtual void OnBlockadeActivated(SiegeEvent siegeEvent)
	{
	}

	public virtual void OnBlockadeDeactivated(SiegeEvent siegeEvent)
	{
	}

	public virtual void OnShipCreated(Ship ship, Settlement createdSettlement)
	{
	}

	public virtual void OnMercenaryServiceStarted(Clan mercenaryClan, StartMercenaryServiceAction.StartMercenaryServiceActionDetails details)
	{
	}

	public virtual void OnMercenaryServiceEnded(Clan mercenaryClan, EndMercenaryServiceAction.EndMercenaryServiceActionDetails details)
	{
	}

	public virtual void OnMapMarkerCreated(MapMarker mapMarker)
	{
	}

	public virtual void OnMapMarkerRemoved(MapMarker mapMarker)
	{
	}

	public virtual void OnAllianceStarted(Kingdom kingdom1, Kingdom kingdom2)
	{
	}

	public virtual void OnAllianceEnded(Kingdom kingdom1, Kingdom kingdom2)
	{
	}

	public virtual void OnCallToWarAgreementStarted(Kingdom callingKingdom, Kingdom calledKingdom, Kingdom kingdomToCallToWarAgainst)
	{
	}

	public virtual void OnCallToWarAgreementEnded(Kingdom callingKingdom, Kingdom calledKingdom, Kingdom kingdomToCallToWarAgainst)
	{
	}

	public virtual void CanHeroLeadParty(Hero hero, ref bool result)
	{
	}

	public virtual void OnCraftingOrderCompleted(Town town, CraftingOrder craftingOrder, ItemObject craftedItem, Hero completerHero)
	{
	}

	public virtual void OnItemsRefined(Hero hero, Crafting.RefiningFormula refineFormula)
	{
	}

	public virtual void OnMapEventContinuityNeedsUpdate(IFaction faction)
	{
	}

	public virtual void OnHeirSelectionOver(Hero selectedHeir)
	{
	}

	public virtual void OnHeirSelectionRequested(Dictionary<Hero, int> heirApparents)
	{
	}

	public virtual void OnMainPartyStarving()
	{
	}

	public virtual void OnHeroGetsBusy(Hero hero, HeroGetsBusyReasons heroGetsBusyReason)
	{
	}

	public virtual void CanHeroEquipmentBeChanged(Hero hero, ref bool result)
	{
	}

	public virtual void CanHaveCampaignIssues(Hero hero, ref bool result)
	{
	}

	public virtual void IsSettlementBusy(Settlement settlement, object asker, ref int flags)
	{
	}

	public virtual void OnHeroUnregistered(Hero hero)
	{
	}
}
