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

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x0200003C RID: 60
	public abstract class CampaignEventReceiver
	{
		// Token: 0x060004FE RID: 1278 RVA: 0x000214D3 File Offset: 0x0001F6D3
		public virtual void RemoveListeners(object o)
		{
		}

		// Token: 0x060004FF RID: 1279 RVA: 0x000214D5 File Offset: 0x0001F6D5
		public virtual void OnCharacterCreationIsOver()
		{
		}

		// Token: 0x06000500 RID: 1280 RVA: 0x000214D7 File Offset: 0x0001F6D7
		public virtual void OnHeroLevelledUp(Hero hero, bool shouldNotify = true)
		{
		}

		// Token: 0x06000501 RID: 1281 RVA: 0x000214D9 File Offset: 0x0001F6D9
		public virtual void OnHomeHideoutChanged(BanditPartyComponent banditPartyComponent, Hideout oldHomeHideout)
		{
		}

		// Token: 0x06000502 RID: 1282 RVA: 0x000214DB File Offset: 0x0001F6DB
		public virtual void OnHeroGainedSkill(Hero hero, SkillObject skill, int change = 1, bool shouldNotify = true)
		{
		}

		// Token: 0x06000503 RID: 1283 RVA: 0x000214DD File Offset: 0x0001F6DD
		public virtual void OnHeroCreated(Hero hero, bool isBornNaturally = false)
		{
		}

		// Token: 0x06000504 RID: 1284 RVA: 0x000214DF File Offset: 0x0001F6DF
		public virtual void OnHeroWounded(Hero woundedHero)
		{
		}

		// Token: 0x06000505 RID: 1285 RVA: 0x000214E1 File Offset: 0x0001F6E1
		public virtual void OnHeroRelationChanged(Hero effectiveHero, Hero effectiveHeroGainedRelationWith, int relationChange, bool showNotification, ChangeRelationAction.ChangeRelationDetail detail, Hero originalHero, Hero originalGainedRelationWith)
		{
		}

		// Token: 0x06000506 RID: 1286 RVA: 0x000214E3 File Offset: 0x0001F6E3
		public virtual void OnQuestLogAdded(QuestBase quest, bool hideInformation)
		{
		}

		// Token: 0x06000507 RID: 1287 RVA: 0x000214E5 File Offset: 0x0001F6E5
		public virtual void OnIssueLogAdded(IssueBase issue, bool hideInformation)
		{
		}

		// Token: 0x06000508 RID: 1288 RVA: 0x000214E7 File Offset: 0x0001F6E7
		public virtual void OnClanTierChanged(Clan clan, bool shouldNotify = true)
		{
		}

		// Token: 0x06000509 RID: 1289 RVA: 0x000214E9 File Offset: 0x0001F6E9
		public virtual void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail actionDetail, bool showNotification = true)
		{
		}

		// Token: 0x0600050A RID: 1290 RVA: 0x000214EB File Offset: 0x0001F6EB
		public virtual void OnClanDefected(Clan clan, Kingdom oldKingdom, Kingdom newKingdom)
		{
		}

		// Token: 0x0600050B RID: 1291 RVA: 0x000214ED File Offset: 0x0001F6ED
		public virtual void OnClanCreated(Clan clan, bool isCompanion)
		{
		}

		// Token: 0x0600050C RID: 1292 RVA: 0x000214EF File Offset: 0x0001F6EF
		public virtual void OnHeroJoinedParty(Hero hero, MobileParty mobileParty)
		{
		}

		// Token: 0x0600050D RID: 1293 RVA: 0x000214F1 File Offset: 0x0001F6F1
		public virtual void OnKingdomDecisionAdded(KingdomDecision decision, bool isPlayerInvolved)
		{
		}

		// Token: 0x0600050E RID: 1294 RVA: 0x000214F3 File Offset: 0x0001F6F3
		public virtual void OnKingdomDecisionCancelled(KingdomDecision decision, bool isPlayerInvolved)
		{
		}

		// Token: 0x0600050F RID: 1295 RVA: 0x000214F5 File Offset: 0x0001F6F5
		public virtual void OnKingdomDecisionConcluded(KingdomDecision decision, DecisionOutcome chosenOutcome, bool isPlayerInvolved)
		{
		}

		// Token: 0x06000510 RID: 1296 RVA: 0x000214F7 File Offset: 0x0001F6F7
		public virtual void OnHeroOrPartyTradedGold(ValueTuple<Hero, PartyBase> giver, ValueTuple<Hero, PartyBase> recipient, ValueTuple<int, string> goldAmount, bool showNotification)
		{
		}

		// Token: 0x06000511 RID: 1297 RVA: 0x000214F9 File Offset: 0x0001F6F9
		public virtual void OnHeroOrPartyGaveItem(ValueTuple<Hero, PartyBase> giver, ValueTuple<Hero, PartyBase> receiver, ItemRosterElement itemRosterElement, bool showNotification)
		{
		}

		// Token: 0x06000512 RID: 1298 RVA: 0x000214FB File Offset: 0x0001F6FB
		public virtual void OnBanditPartyRecruited(MobileParty banditParty)
		{
		}

		// Token: 0x06000513 RID: 1299 RVA: 0x000214FD File Offset: 0x0001F6FD
		public virtual void OnArmyCreated(Army army)
		{
		}

		// Token: 0x06000514 RID: 1300 RVA: 0x000214FF File Offset: 0x0001F6FF
		public virtual void OnPartyAttachedAnotherParty(MobileParty mobileParty)
		{
		}

		// Token: 0x06000515 RID: 1301 RVA: 0x00021501 File Offset: 0x0001F701
		public virtual void OnNearbyPartyAddedToPlayerMapEvent(MobileParty mobileParty)
		{
		}

		// Token: 0x06000516 RID: 1302 RVA: 0x00021503 File Offset: 0x0001F703
		public virtual void OnArmyDispersed(Army army, Army.ArmyDispersionReason reason, bool isPlayersArmy)
		{
		}

		// Token: 0x06000517 RID: 1303 RVA: 0x00021505 File Offset: 0x0001F705
		public virtual void OnArmyGathered(Army army, IMapPoint gatheringPoint)
		{
		}

		// Token: 0x06000518 RID: 1304 RVA: 0x00021507 File Offset: 0x0001F707
		public virtual void OnPerkOpened(Hero hero, PerkObject perk)
		{
		}

		// Token: 0x06000519 RID: 1305 RVA: 0x00021509 File Offset: 0x0001F709
		public virtual void OnPerkReset(Hero hero, PerkObject perk)
		{
		}

		// Token: 0x0600051A RID: 1306 RVA: 0x0002150B File Offset: 0x0001F70B
		public virtual void OnPlayerTraitChanged(TraitObject trait, int previousLevel)
		{
		}

		// Token: 0x0600051B RID: 1307 RVA: 0x0002150D File Offset: 0x0001F70D
		public virtual void OnVillageStateChanged(Village village, Village.VillageStates oldState, Village.VillageStates newState, MobileParty raiderParty)
		{
		}

		// Token: 0x0600051C RID: 1308 RVA: 0x0002150F File Offset: 0x0001F70F
		public virtual void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
		{
		}

		// Token: 0x0600051D RID: 1309 RVA: 0x00021511 File Offset: 0x0001F711
		public virtual void OnAfterSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
		{
		}

		// Token: 0x0600051E RID: 1310 RVA: 0x00021513 File Offset: 0x0001F713
		public virtual void OnBeforeSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
		{
		}

		// Token: 0x0600051F RID: 1311 RVA: 0x00021515 File Offset: 0x0001F715
		public virtual void OnMercenaryTroopChangedInTown(Town town, CharacterObject oldTroopType, CharacterObject newTroopType)
		{
		}

		// Token: 0x06000520 RID: 1312 RVA: 0x00021517 File Offset: 0x0001F717
		public virtual void OnMercenaryNumberChangedInTown(Town town, int oldNumber, int newNumber)
		{
		}

		// Token: 0x06000521 RID: 1313 RVA: 0x00021519 File Offset: 0x0001F719
		public virtual void OnAlleyOwnerChanged(Alley alley, Hero newOwner, Hero oldOwner)
		{
		}

		// Token: 0x06000522 RID: 1314 RVA: 0x0002151B File Offset: 0x0001F71B
		public virtual void OnAlleyClearedByPlayer(Alley alley)
		{
		}

		// Token: 0x06000523 RID: 1315 RVA: 0x0002151D File Offset: 0x0001F71D
		public virtual void OnAlleyOccupiedByPlayer(Alley alley, TroopRoster troops)
		{
		}

		// Token: 0x06000524 RID: 1316 RVA: 0x0002151F File Offset: 0x0001F71F
		public virtual void OnRomanticStateChanged(Hero hero1, Hero hero2, Romance.RomanceLevelEnum romanceLevel)
		{
		}

		// Token: 0x06000525 RID: 1317 RVA: 0x00021521 File Offset: 0x0001F721
		public virtual void OnBeforeHeroesMarried(Hero hero1, Hero hero2, bool showNotification = true)
		{
		}

		// Token: 0x06000526 RID: 1318 RVA: 0x00021523 File Offset: 0x0001F723
		public virtual void OnPlayerEliminatedFromTournament(int round, Town town)
		{
		}

		// Token: 0x06000527 RID: 1319 RVA: 0x00021525 File Offset: 0x0001F725
		public virtual void OnPlayerStartedTournamentMatch(Town town)
		{
		}

		// Token: 0x06000528 RID: 1320 RVA: 0x00021527 File Offset: 0x0001F727
		public virtual void OnTournamentStarted(Town town)
		{
		}

		// Token: 0x06000529 RID: 1321 RVA: 0x00021529 File Offset: 0x0001F729
		public virtual void OnTournamentFinished(CharacterObject winner, MBReadOnlyList<CharacterObject> participants, Town town, ItemObject prize)
		{
		}

		// Token: 0x0600052A RID: 1322 RVA: 0x0002152B File Offset: 0x0001F72B
		public virtual void OnTournamentCancelled(Town town)
		{
		}

		// Token: 0x0600052B RID: 1323 RVA: 0x0002152D File Offset: 0x0001F72D
		public virtual void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail declareWarDetail)
		{
		}

		// Token: 0x0600052C RID: 1324 RVA: 0x0002152F File Offset: 0x0001F72F
		public virtual void OnMakePeace(IFaction side1Faction, IFaction side2Faction, MakePeaceAction.MakePeaceDetail detail)
		{
		}

		// Token: 0x0600052D RID: 1325 RVA: 0x00021531 File Offset: 0x0001F731
		public virtual void OnKingdomCreated(Kingdom createdKingdom)
		{
		}

		// Token: 0x0600052E RID: 1326 RVA: 0x00021533 File Offset: 0x0001F733
		public virtual void OnHeroOccupationChanged(Hero hero, Occupation oldOccupation)
		{
		}

		// Token: 0x0600052F RID: 1327 RVA: 0x00021535 File Offset: 0x0001F735
		public virtual void OnKingdomDestroyed(Kingdom kingdom)
		{
		}

		// Token: 0x06000530 RID: 1328 RVA: 0x00021537 File Offset: 0x0001F737
		public virtual void CanKingdomBeDiscontinued(Kingdom kingdom, ref bool result)
		{
		}

		// Token: 0x06000531 RID: 1329 RVA: 0x00021539 File Offset: 0x0001F739
		public virtual void OnBarterAccepted(Hero offererHero, Hero otherHero, List<Barterable> barters)
		{
		}

		// Token: 0x06000532 RID: 1330 RVA: 0x0002153B File Offset: 0x0001F73B
		public virtual void OnBarterCanceled(Hero offererHero, Hero otherHero, List<Barterable> barters)
		{
		}

		// Token: 0x06000533 RID: 1331 RVA: 0x0002153D File Offset: 0x0001F73D
		public virtual void OnStartBattle(PartyBase attackerParty, PartyBase defenderParty, object subject, bool showNotification)
		{
		}

		// Token: 0x06000534 RID: 1332 RVA: 0x0002153F File Offset: 0x0001F73F
		public virtual void OnRebellionFinished(Settlement settlement, Clan oldOwnerClan)
		{
		}

		// Token: 0x06000535 RID: 1333 RVA: 0x00021541 File Offset: 0x0001F741
		public virtual void TownRebelliousStateChanged(Town town, bool rebelliousState)
		{
		}

		// Token: 0x06000536 RID: 1334 RVA: 0x00021543 File Offset: 0x0001F743
		public virtual void OnRebelliousClanDisbandedAtSettlement(Settlement settlement, Clan clan)
		{
		}

		// Token: 0x06000537 RID: 1335 RVA: 0x00021545 File Offset: 0x0001F745
		public virtual void OnItemsLooted(MobileParty mobileParty, ItemRoster items)
		{
		}

		// Token: 0x06000538 RID: 1336 RVA: 0x00021547 File Offset: 0x0001F747
		public virtual void OnMobilePartyDestroyed(MobileParty mobileParty, PartyBase destroyerParty)
		{
		}

		// Token: 0x06000539 RID: 1337 RVA: 0x00021549 File Offset: 0x0001F749
		public virtual void OnMobilePartyCreated(MobileParty party)
		{
		}

		// Token: 0x0600053A RID: 1338 RVA: 0x0002154B File Offset: 0x0001F74B
		public virtual void OnMapInteractableCreated(IInteractablePoint interactable)
		{
		}

		// Token: 0x0600053B RID: 1339 RVA: 0x0002154D File Offset: 0x0001F74D
		public virtual void OnMapInteractableDestroyed(IInteractablePoint interactable)
		{
		}

		// Token: 0x0600053C RID: 1340 RVA: 0x0002154F File Offset: 0x0001F74F
		public virtual void OnMobilePartyQuestStatusChanged(MobileParty party, bool isUsedByQuest)
		{
		}

		// Token: 0x0600053D RID: 1341 RVA: 0x00021551 File Offset: 0x0001F751
		public virtual void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification = true)
		{
		}

		// Token: 0x0600053E RID: 1342 RVA: 0x00021553 File Offset: 0x0001F753
		public virtual void OnBeforeHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification = true)
		{
		}

		// Token: 0x0600053F RID: 1343 RVA: 0x00021555 File Offset: 0x0001F755
		public virtual void OnChildEducationCompleted(Hero hero, int age)
		{
		}

		// Token: 0x06000540 RID: 1344 RVA: 0x00021557 File Offset: 0x0001F757
		public virtual void OnHeroComesOfAge(Hero hero)
		{
		}

		// Token: 0x06000541 RID: 1345 RVA: 0x00021559 File Offset: 0x0001F759
		public virtual void OnHeroReachesTeenAge(Hero hero)
		{
		}

		// Token: 0x06000542 RID: 1346 RVA: 0x0002155B File Offset: 0x0001F75B
		public virtual void OnHeroGrowsOutOfInfancy(Hero hero)
		{
		}

		// Token: 0x06000543 RID: 1347 RVA: 0x0002155D File Offset: 0x0001F75D
		public virtual void OnCharacterDefeated(Hero winner, Hero loser)
		{
		}

		// Token: 0x06000544 RID: 1348 RVA: 0x0002155F File Offset: 0x0001F75F
		public virtual void OnHeroPrisonerTaken(PartyBase capturer, Hero prisoner)
		{
		}

		// Token: 0x06000545 RID: 1349 RVA: 0x00021561 File Offset: 0x0001F761
		public virtual void OnHeroPrisonerReleased(Hero prisoner, PartyBase party, IFaction capturerFaction, EndCaptivityDetail detail, bool showNotification = true)
		{
		}

		// Token: 0x06000546 RID: 1350 RVA: 0x00021563 File Offset: 0x0001F763
		public virtual void OnCharacterBecameFugitive(Hero hero, bool showNotification)
		{
		}

		// Token: 0x06000547 RID: 1351 RVA: 0x00021565 File Offset: 0x0001F765
		public virtual void OnPlayerMetHero(Hero hero)
		{
		}

		// Token: 0x06000548 RID: 1352 RVA: 0x00021567 File Offset: 0x0001F767
		public virtual void OnPlayerLearnsAboutHero(Hero hero)
		{
		}

		// Token: 0x06000549 RID: 1353 RVA: 0x00021569 File Offset: 0x0001F769
		public virtual void OnRenownGained(Hero hero, int gainedRenown, bool doNotNotify)
		{
		}

		// Token: 0x0600054A RID: 1354 RVA: 0x0002156B File Offset: 0x0001F76B
		public virtual void OnCrimeRatingChanged(IFaction kingdom, float deltaCrimeAmount)
		{
		}

		// Token: 0x0600054B RID: 1355 RVA: 0x0002156D File Offset: 0x0001F76D
		public virtual void OnNewCompanionAdded(Hero newCompanion)
		{
		}

		// Token: 0x0600054C RID: 1356 RVA: 0x0002156F File Offset: 0x0001F76F
		public virtual void OnAfterMissionStarted(IMission iMission)
		{
		}

		// Token: 0x0600054D RID: 1357 RVA: 0x00021571 File Offset: 0x0001F771
		public virtual void OnGameMenuOpened(MenuCallbackArgs args)
		{
		}

		// Token: 0x0600054E RID: 1358 RVA: 0x00021573 File Offset: 0x0001F773
		public virtual void OnVillageBecomeNormal(Village village)
		{
		}

		// Token: 0x0600054F RID: 1359 RVA: 0x00021575 File Offset: 0x0001F775
		public virtual void OnVillageBeingRaided(Village village)
		{
		}

		// Token: 0x06000550 RID: 1360 RVA: 0x00021577 File Offset: 0x0001F777
		public virtual void OnVillageLooted(Village village)
		{
		}

		// Token: 0x06000551 RID: 1361 RVA: 0x00021579 File Offset: 0x0001F779
		public virtual void OnAgentJoinedConversation(IAgent agent)
		{
		}

		// Token: 0x06000552 RID: 1362 RVA: 0x0002157B File Offset: 0x0001F77B
		public virtual void OnConversationEnded(IEnumerable<CharacterObject> characters)
		{
		}

		// Token: 0x06000553 RID: 1363 RVA: 0x0002157D File Offset: 0x0001F77D
		public virtual void OnMapEventEnded(MapEvent mapEvent)
		{
		}

		// Token: 0x06000554 RID: 1364 RVA: 0x0002157F File Offset: 0x0001F77F
		public virtual void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
		{
		}

		// Token: 0x06000555 RID: 1365 RVA: 0x00021581 File Offset: 0x0001F781
		public virtual void OnRansomOfferedToPlayer(Hero captiveHero)
		{
		}

		// Token: 0x06000556 RID: 1366 RVA: 0x00021583 File Offset: 0x0001F783
		public virtual void OnPrisonersChangeInSettlement(Settlement settlement, FlattenedTroopRoster prisonerRoster, Hero prisonerHero, bool takenFromDungeon)
		{
		}

		// Token: 0x06000557 RID: 1367 RVA: 0x00021585 File Offset: 0x0001F785
		public virtual void OnMissionStarted(IMission mission)
		{
		}

		// Token: 0x06000558 RID: 1368 RVA: 0x00021587 File Offset: 0x0001F787
		public virtual void OnRansomOfferCancelled(Hero captiveHero)
		{
		}

		// Token: 0x06000559 RID: 1369 RVA: 0x00021589 File Offset: 0x0001F789
		public virtual void OnPeaceOfferedToPlayer(IFaction opponentFaction, int tributeAmount, int tributeDuration)
		{
		}

		// Token: 0x0600055A RID: 1370 RVA: 0x0002158B File Offset: 0x0001F78B
		public virtual void OnTradeAgreementSigned(Kingdom kingdom, Kingdom other)
		{
		}

		// Token: 0x0600055B RID: 1371 RVA: 0x0002158D File Offset: 0x0001F78D
		public virtual void OnPeaceOfferResolved(IFaction opponentFaction)
		{
		}

		// Token: 0x0600055C RID: 1372 RVA: 0x0002158F File Offset: 0x0001F78F
		public virtual void OnMarriageOfferedToPlayer(Hero suitor, Hero maiden)
		{
		}

		// Token: 0x0600055D RID: 1373 RVA: 0x00021591 File Offset: 0x0001F791
		public virtual void OnMarriageOfferCanceled(Hero suitor, Hero maiden)
		{
		}

		// Token: 0x0600055E RID: 1374 RVA: 0x00021593 File Offset: 0x0001F793
		public virtual void OnVassalOrMercenaryServiceOfferedToPlayer(Kingdom offeredKingdom)
		{
		}

		// Token: 0x0600055F RID: 1375 RVA: 0x00021595 File Offset: 0x0001F795
		public virtual void OnVassalOrMercenaryServiceOfferCanceled(Kingdom offeredKingdom)
		{
		}

		// Token: 0x06000560 RID: 1376 RVA: 0x00021597 File Offset: 0x0001F797
		public virtual void OnPlayerBoardGameOver(Hero opposingHero, BoardGameHelper.BoardGameState state)
		{
		}

		// Token: 0x06000561 RID: 1377 RVA: 0x00021599 File Offset: 0x0001F799
		public virtual void OnCommonAreaStateChanged(Alley alley, Alley.AreaState oldState, Alley.AreaState newState)
		{
		}

		// Token: 0x06000562 RID: 1378 RVA: 0x0002159B File Offset: 0x0001F79B
		public virtual void BeforeMissionOpened()
		{
		}

		// Token: 0x06000563 RID: 1379 RVA: 0x0002159D File Offset: 0x0001F79D
		public virtual void OnPartyRemoved(PartyBase party)
		{
		}

		// Token: 0x06000564 RID: 1380 RVA: 0x0002159F File Offset: 0x0001F79F
		public virtual void OnPartySizeChanged(PartyBase party)
		{
		}

		// Token: 0x06000565 RID: 1381 RVA: 0x000215A1 File Offset: 0x0001F7A1
		public virtual void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
		{
		}

		// Token: 0x06000566 RID: 1382 RVA: 0x000215A3 File Offset: 0x0001F7A3
		public virtual void OnGovernorChanged(Town fortification, Hero oldGovernor, Hero newGovernor)
		{
		}

		// Token: 0x06000567 RID: 1383 RVA: 0x000215A5 File Offset: 0x0001F7A5
		public virtual void OnSettlementLeft(MobileParty party, Settlement settlement)
		{
		}

		// Token: 0x06000568 RID: 1384 RVA: 0x000215A7 File Offset: 0x0001F7A7
		public virtual void Tick(float dt)
		{
		}

		// Token: 0x06000569 RID: 1385 RVA: 0x000215A9 File Offset: 0x0001F7A9
		public virtual void OnSessionStart(CampaignGameStarter campaignGameStarter)
		{
		}

		// Token: 0x0600056A RID: 1386 RVA: 0x000215AB File Offset: 0x0001F7AB
		public virtual void OnAfterSessionStart(CampaignGameStarter campaignGameStarter)
		{
		}

		// Token: 0x0600056B RID: 1387 RVA: 0x000215AD File Offset: 0x0001F7AD
		public virtual void OnNewGameCreated(CampaignGameStarter campaignGameStarter)
		{
		}

		// Token: 0x0600056C RID: 1388 RVA: 0x000215AF File Offset: 0x0001F7AF
		public virtual void OnGameLoaded(CampaignGameStarter campaignGameStarter)
		{
		}

		// Token: 0x0600056D RID: 1389 RVA: 0x000215B1 File Offset: 0x0001F7B1
		public virtual void OnGameEarlyLoaded(CampaignGameStarter campaignGameStarter)
		{
		}

		// Token: 0x0600056E RID: 1390 RVA: 0x000215B3 File Offset: 0x0001F7B3
		public virtual void OnPlayerTradeProfit(int profit)
		{
		}

		// Token: 0x0600056F RID: 1391 RVA: 0x000215B5 File Offset: 0x0001F7B5
		public virtual void OnRulingClanChanged(Kingdom kingdom, Clan newRulingClan)
		{
		}

		// Token: 0x06000570 RID: 1392 RVA: 0x000215B7 File Offset: 0x0001F7B7
		public virtual void OnPrisonerReleased(FlattenedTroopRoster roster)
		{
		}

		// Token: 0x06000571 RID: 1393 RVA: 0x000215B9 File Offset: 0x0001F7B9
		public virtual void OnGameLoadFinished()
		{
		}

		// Token: 0x06000572 RID: 1394 RVA: 0x000215BB File Offset: 0x0001F7BB
		public virtual void OnPartyJoinedArmy(MobileParty mobileParty)
		{
		}

		// Token: 0x06000573 RID: 1395 RVA: 0x000215BD File Offset: 0x0001F7BD
		public virtual void OnPartyRemovedFromArmy(MobileParty mobileParty)
		{
		}

		// Token: 0x06000574 RID: 1396 RVA: 0x000215BF File Offset: 0x0001F7BF
		public virtual void OnArmyOverlaySetDirty()
		{
		}

		// Token: 0x06000575 RID: 1397 RVA: 0x000215C1 File Offset: 0x0001F7C1
		public virtual void OnPlayerDesertedBattle(int sacrificedMenCount)
		{
		}

		// Token: 0x06000576 RID: 1398 RVA: 0x000215C3 File Offset: 0x0001F7C3
		public virtual void OnPlayerArmyLeaderChangedBehavior()
		{
		}

		// Token: 0x06000577 RID: 1399 RVA: 0x000215C5 File Offset: 0x0001F7C5
		public virtual void MissionTick(float dt)
		{
		}

		// Token: 0x06000578 RID: 1400 RVA: 0x000215C7 File Offset: 0x0001F7C7
		public virtual void OnChildConceived(Hero mother)
		{
		}

		// Token: 0x06000579 RID: 1401 RVA: 0x000215C9 File Offset: 0x0001F7C9
		public virtual void OnGivenBirth(Hero mother, List<Hero> aliveChildren, int stillbornCount)
		{
		}

		// Token: 0x0600057A RID: 1402 RVA: 0x000215CB File Offset: 0x0001F7CB
		public virtual void OnUnitRecruited(CharacterObject character, int amount)
		{
		}

		// Token: 0x0600057B RID: 1403 RVA: 0x000215CD File Offset: 0x0001F7CD
		public virtual void OnPlayerBattleEnd(MapEvent mapEvent)
		{
		}

		// Token: 0x0600057C RID: 1404 RVA: 0x000215CF File Offset: 0x0001F7CF
		public virtual void OnMissionEnded(IMission mission)
		{
		}

		// Token: 0x0600057D RID: 1405 RVA: 0x000215D1 File Offset: 0x0001F7D1
		public virtual void TickPartialHourlyAi(MobileParty party)
		{
		}

		// Token: 0x0600057E RID: 1406 RVA: 0x000215D3 File Offset: 0x0001F7D3
		public virtual void QuarterDailyPartyTick(MobileParty party)
		{
		}

		// Token: 0x0600057F RID: 1407 RVA: 0x000215D5 File Offset: 0x0001F7D5
		public virtual void AiHourlyTick(MobileParty party, PartyThinkParams partyThinkParams)
		{
		}

		// Token: 0x06000580 RID: 1408 RVA: 0x000215D7 File Offset: 0x0001F7D7
		public virtual void HourlyTick()
		{
		}

		// Token: 0x06000581 RID: 1409 RVA: 0x000215D9 File Offset: 0x0001F7D9
		public virtual void QuarterHourlyTick()
		{
		}

		// Token: 0x06000582 RID: 1410 RVA: 0x000215DB File Offset: 0x0001F7DB
		public virtual void HourlyTickParty(MobileParty mobileParty)
		{
		}

		// Token: 0x06000583 RID: 1411 RVA: 0x000215DD File Offset: 0x0001F7DD
		public virtual void HourlyTickSettlement(Settlement settlement)
		{
		}

		// Token: 0x06000584 RID: 1412 RVA: 0x000215DF File Offset: 0x0001F7DF
		public virtual void HourlyTickClan(Clan clan)
		{
		}

		// Token: 0x06000585 RID: 1413 RVA: 0x000215E1 File Offset: 0x0001F7E1
		public virtual void DailyTick()
		{
		}

		// Token: 0x06000586 RID: 1414 RVA: 0x000215E3 File Offset: 0x0001F7E3
		public virtual void DailyTickParty(MobileParty mobileParty)
		{
		}

		// Token: 0x06000587 RID: 1415 RVA: 0x000215E5 File Offset: 0x0001F7E5
		public virtual void DailyTickTown(Town town)
		{
		}

		// Token: 0x06000588 RID: 1416 RVA: 0x000215E7 File Offset: 0x0001F7E7
		public virtual void DailyTickSettlement(Settlement settlement)
		{
		}

		// Token: 0x06000589 RID: 1417 RVA: 0x000215E9 File Offset: 0x0001F7E9
		public virtual void DailyTickClan(Clan clan)
		{
		}

		// Token: 0x0600058A RID: 1418 RVA: 0x000215EB File Offset: 0x0001F7EB
		public virtual void OnPlayerBodyPropertiesChanged()
		{
		}

		// Token: 0x0600058B RID: 1419 RVA: 0x000215ED File Offset: 0x0001F7ED
		public virtual void WeeklyTick()
		{
		}

		// Token: 0x0600058C RID: 1420 RVA: 0x000215EF File Offset: 0x0001F7EF
		public virtual void CollectAvailableTutorials(ref List<CampaignTutorial> tutorials)
		{
		}

		// Token: 0x0600058D RID: 1421 RVA: 0x000215F1 File Offset: 0x0001F7F1
		public virtual void DailyTickHero(Hero hero)
		{
		}

		// Token: 0x0600058E RID: 1422 RVA: 0x000215F3 File Offset: 0x0001F7F3
		public virtual void OnTutorialCompleted(string tutorial)
		{
		}

		// Token: 0x0600058F RID: 1423 RVA: 0x000215F5 File Offset: 0x0001F7F5
		public virtual void OnBuildingLevelChanged(Town town, Building building, int levelChange)
		{
		}

		// Token: 0x06000590 RID: 1424 RVA: 0x000215F7 File Offset: 0x0001F7F7
		public virtual void BeforeGameMenuOpened(MenuCallbackArgs args)
		{
		}

		// Token: 0x06000591 RID: 1425 RVA: 0x000215F9 File Offset: 0x0001F7F9
		public virtual void AfterGameMenuInitialized(MenuCallbackArgs args)
		{
		}

		// Token: 0x06000592 RID: 1426 RVA: 0x000215FB File Offset: 0x0001F7FB
		public virtual void OnBarterablesRequested(BarterData args)
		{
		}

		// Token: 0x06000593 RID: 1427 RVA: 0x000215FD File Offset: 0x0001F7FD
		public virtual void OnPartyVisibilityChanged(PartyBase party)
		{
		}

		// Token: 0x06000594 RID: 1428 RVA: 0x000215FF File Offset: 0x0001F7FF
		public virtual void OnCompanionRemoved(Hero companion, RemoveCompanionAction.RemoveCompanionDetail detail)
		{
		}

		// Token: 0x06000595 RID: 1429 RVA: 0x00021601 File Offset: 0x0001F801
		public virtual void TrackDetected(Track track)
		{
		}

		// Token: 0x06000596 RID: 1430 RVA: 0x00021603 File Offset: 0x0001F803
		public virtual void TrackLost(Track track)
		{
		}

		// Token: 0x06000597 RID: 1431 RVA: 0x00021605 File Offset: 0x0001F805
		public virtual void LocationCharactersAreReadyToSpawn(Dictionary<string, int> unusedUsablePointCount)
		{
		}

		// Token: 0x06000598 RID: 1432 RVA: 0x00021607 File Offset: 0x0001F807
		public virtual void LocationCharactersSimulated()
		{
		}

		// Token: 0x06000599 RID: 1433 RVA: 0x00021609 File Offset: 0x0001F809
		public virtual void OnBeforePlayerAgentSpawn(ref MatrixFrame spawnFrame)
		{
		}

		// Token: 0x0600059A RID: 1434 RVA: 0x0002160B File Offset: 0x0001F80B
		public virtual void OnPlayerAgentSpawned()
		{
		}

		// Token: 0x0600059B RID: 1435 RVA: 0x0002160D File Offset: 0x0001F80D
		public virtual void OnPlayerUpgradedTroops(CharacterObject upgradeFromTroop, CharacterObject upgradeToTroop, int number)
		{
		}

		// Token: 0x0600059C RID: 1436 RVA: 0x0002160F File Offset: 0x0001F80F
		public virtual void OnHeroCombatHit(CharacterObject attackerTroop, CharacterObject attackedTroop, PartyBase party, WeaponComponentData usedWeapon, bool isFatal, int xp)
		{
		}

		// Token: 0x0600059D RID: 1437 RVA: 0x00021611 File Offset: 0x0001F811
		public virtual void OnCharacterPortraitPopUpOpened(CharacterObject character)
		{
		}

		// Token: 0x0600059E RID: 1438 RVA: 0x00021613 File Offset: 0x0001F813
		public virtual void OnCharacterPortraitPopUpClosed()
		{
		}

		// Token: 0x0600059F RID: 1439 RVA: 0x00021615 File Offset: 0x0001F815
		public virtual void OnPlayerStartTalkFromMenu(Hero hero)
		{
		}

		// Token: 0x060005A0 RID: 1440 RVA: 0x00021617 File Offset: 0x0001F817
		public virtual void OnGameMenuOptionSelected(GameMenu gameMenu, GameMenuOption gameMenuOption)
		{
		}

		// Token: 0x060005A1 RID: 1441 RVA: 0x00021619 File Offset: 0x0001F819
		public virtual void OnPlayerStartRecruitment(CharacterObject recruitTroopCharacter)
		{
		}

		// Token: 0x060005A2 RID: 1442 RVA: 0x0002161B File Offset: 0x0001F81B
		public virtual void OnBeforePlayerCharacterChanged(Hero oldPlayer, Hero newPlayer)
		{
		}

		// Token: 0x060005A3 RID: 1443 RVA: 0x0002161D File Offset: 0x0001F81D
		public virtual void OnPlayerCharacterChanged(Hero oldPlayer, Hero newPlayer, MobileParty newMainParty, bool isMainPartyChanged)
		{
		}

		// Token: 0x060005A4 RID: 1444 RVA: 0x0002161F File Offset: 0x0001F81F
		public virtual void OnClanLeaderChanged(Hero oldLeader, Hero newLeader)
		{
		}

		// Token: 0x060005A5 RID: 1445 RVA: 0x00021621 File Offset: 0x0001F821
		public virtual void OnSiegeEventStarted(SiegeEvent siegeEvent)
		{
		}

		// Token: 0x060005A6 RID: 1446 RVA: 0x00021623 File Offset: 0x0001F823
		public virtual void OnPlayerSiegeStarted()
		{
		}

		// Token: 0x060005A7 RID: 1447 RVA: 0x00021625 File Offset: 0x0001F825
		public virtual void OnSiegeEventEnded(SiegeEvent siegeEvent)
		{
		}

		// Token: 0x060005A8 RID: 1448 RVA: 0x00021627 File Offset: 0x0001F827
		public virtual void OnSiegeAftermathApplied(MobileParty attackerParty, Settlement settlement, SiegeAftermathAction.SiegeAftermath aftermathType, Clan previousSettlementOwner, Dictionary<MobileParty, float> partyContributions)
		{
		}

		// Token: 0x060005A9 RID: 1449 RVA: 0x00021629 File Offset: 0x0001F829
		public virtual void OnSiegeBombardmentHit(MobileParty besiegerParty, Settlement besiegedSettlement, BattleSideEnum side, SiegeEngineType weapon, SiegeBombardTargets target)
		{
		}

		// Token: 0x060005AA RID: 1450 RVA: 0x0002162B File Offset: 0x0001F82B
		public virtual void OnSiegeBombardmentWallHit(MobileParty besiegerParty, Settlement besiegedSettlement, BattleSideEnum side, SiegeEngineType weapon, bool isWallCracked)
		{
		}

		// Token: 0x060005AB RID: 1451 RVA: 0x0002162D File Offset: 0x0001F82D
		public virtual void OnSiegeEngineDestroyed(MobileParty besiegerParty, Settlement besiegedSettlement, BattleSideEnum side, SiegeEngineType destroyedEngine)
		{
		}

		// Token: 0x060005AC RID: 1452 RVA: 0x0002162F File Offset: 0x0001F82F
		public virtual void OnTradeRumorIsTaken(List<TradeRumor> newRumors, Settlement sourceSettlement = null)
		{
		}

		// Token: 0x060005AD RID: 1453 RVA: 0x00021631 File Offset: 0x0001F831
		public virtual void OnCheckForIssue(Hero hero)
		{
		}

		// Token: 0x060005AE RID: 1454 RVA: 0x00021633 File Offset: 0x0001F833
		public virtual void OnIssueUpdated(IssueBase issue, IssueBase.IssueUpdateDetails details, Hero issueSolver)
		{
		}

		// Token: 0x060005AF RID: 1455 RVA: 0x00021635 File Offset: 0x0001F835
		public virtual void OnTroopsDeserted(MobileParty mobileParty, TroopRoster desertedTroops)
		{
		}

		// Token: 0x060005B0 RID: 1456 RVA: 0x00021637 File Offset: 0x0001F837
		public virtual void OnTroopRecruited(Hero recruiterHero, Settlement recruitmentSettlement, Hero recruitmentSource, CharacterObject troop, int amount)
		{
		}

		// Token: 0x060005B1 RID: 1457 RVA: 0x00021639 File Offset: 0x0001F839
		public virtual void OnTroopGivenToSettlement(Hero giverHero, Settlement recipientSettlement, TroopRoster roster)
		{
		}

		// Token: 0x060005B2 RID: 1458 RVA: 0x0002163B File Offset: 0x0001F83B
		public virtual void OnItemSold(PartyBase receiverParty, PartyBase payerParty, ItemRosterElement itemRosterElement, int number, Settlement currentSettlement)
		{
		}

		// Token: 0x060005B3 RID: 1459 RVA: 0x0002163D File Offset: 0x0001F83D
		public virtual void OnCaravanTransactionCompleted(MobileParty caravanParty, Town town, List<ValueTuple<EquipmentElement, int>> itemRosterElements)
		{
		}

		// Token: 0x060005B4 RID: 1460 RVA: 0x0002163F File Offset: 0x0001F83F
		public virtual void OnPrisonerSold(PartyBase sellerParty, PartyBase buyerParty, TroopRoster prisoners)
		{
		}

		// Token: 0x060005B5 RID: 1461 RVA: 0x00021641 File Offset: 0x0001F841
		public virtual void OnPartyDisbanded(MobileParty disbandParty, Settlement relatedSettlement)
		{
		}

		// Token: 0x060005B6 RID: 1462 RVA: 0x00021643 File Offset: 0x0001F843
		public virtual void OnPartyDisbandStarted(MobileParty disbandParty)
		{
		}

		// Token: 0x060005B7 RID: 1463 RVA: 0x00021645 File Offset: 0x0001F845
		public virtual void OnPartyDisbandCanceled(MobileParty disbandParty)
		{
		}

		// Token: 0x060005B8 RID: 1464 RVA: 0x00021647 File Offset: 0x0001F847
		public virtual void OnHideoutSpotted(PartyBase party, PartyBase hideoutParty)
		{
		}

		// Token: 0x060005B9 RID: 1465 RVA: 0x00021649 File Offset: 0x0001F849
		public virtual void OnHideoutDeactivated(Settlement hideout)
		{
		}

		// Token: 0x060005BA RID: 1466 RVA: 0x0002164B File Offset: 0x0001F84B
		public virtual void OnHideoutBattleCompleted(BattleSideEnum winnerSide, HideoutEventComponent hideoutEventComponent)
		{
		}

		// Token: 0x060005BB RID: 1467 RVA: 0x0002164D File Offset: 0x0001F84D
		public virtual void OnPlayerInventoryExchange(List<ValueTuple<ItemRosterElement, int>> purchasedItems, List<ValueTuple<ItemRosterElement, int>> soldItems, bool isTrading)
		{
		}

		// Token: 0x060005BC RID: 1468 RVA: 0x0002164F File Offset: 0x0001F84F
		public virtual void OnItemsDiscardedByPlayer(ItemRoster roster)
		{
		}

		// Token: 0x060005BD RID: 1469 RVA: 0x00021651 File Offset: 0x0001F851
		public virtual void OnPersuasionProgressCommitted(Tuple<PersuasionOptionArgs, PersuasionOptionResult> progress)
		{
		}

		// Token: 0x060005BE RID: 1470 RVA: 0x00021653 File Offset: 0x0001F853
		public virtual void OnHeroSharedFoodWithAnother(Hero supporterHero, Hero supportedHero, float influence)
		{
		}

		// Token: 0x060005BF RID: 1471 RVA: 0x00021655 File Offset: 0x0001F855
		public virtual void OnQuestCompleted(QuestBase quest, QuestBase.QuestCompleteDetails detail)
		{
		}

		// Token: 0x060005C0 RID: 1472 RVA: 0x00021657 File Offset: 0x0001F857
		public virtual void OnQuestStarted(QuestBase quest)
		{
		}

		// Token: 0x060005C1 RID: 1473 RVA: 0x00021659 File Offset: 0x0001F859
		public virtual void OnItemProduced(ItemObject itemObject, Settlement settlement, int count)
		{
		}

		// Token: 0x060005C2 RID: 1474 RVA: 0x0002165B File Offset: 0x0001F85B
		public virtual void OnItemConsumed(ItemObject itemObject, Settlement settlement, int count)
		{
		}

		// Token: 0x060005C3 RID: 1475 RVA: 0x0002165D File Offset: 0x0001F85D
		public virtual void OnPartyConsumedFood(MobileParty party)
		{
		}

		// Token: 0x060005C4 RID: 1476 RVA: 0x0002165F File Offset: 0x0001F85F
		public virtual void SiegeCompleted(Settlement siegeSettlement, MobileParty attackerParty, bool isWin, MapEvent.BattleTypes battleType)
		{
		}

		// Token: 0x060005C5 RID: 1477 RVA: 0x00021661 File Offset: 0x0001F861
		public virtual void AfterSiegeCompleted(Settlement siegeSettlement, MobileParty attackerParty, bool isWin, MapEvent.BattleTypes battleType)
		{
		}

		// Token: 0x060005C6 RID: 1478 RVA: 0x00021663 File Offset: 0x0001F863
		public virtual void SiegeEngineBuilt(SiegeEvent siegeEvent, BattleSideEnum side, SiegeEngineType siegeEngine)
		{
		}

		// Token: 0x060005C7 RID: 1479 RVA: 0x00021665 File Offset: 0x0001F865
		public virtual void RaidCompleted(BattleSideEnum winnerSide, RaidEventComponent raidEvent)
		{
		}

		// Token: 0x060005C8 RID: 1480 RVA: 0x00021667 File Offset: 0x0001F867
		public virtual void ForceSuppliesCompleted(BattleSideEnum winnerSide, ForceSuppliesEventComponent forceSuppliesEvent)
		{
		}

		// Token: 0x060005C9 RID: 1481 RVA: 0x00021669 File Offset: 0x0001F869
		public virtual void ForceVolunteersCompleted(BattleSideEnum winnerSide, ForceVolunteersEventComponent forceVolunteersEvent)
		{
		}

		// Token: 0x060005CA RID: 1482 RVA: 0x0002166B File Offset: 0x0001F86B
		public virtual void OnBeforeMainCharacterDied(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification = true)
		{
		}

		// Token: 0x060005CB RID: 1483 RVA: 0x0002166D File Offset: 0x0001F86D
		public virtual void OnGameOver()
		{
		}

		// Token: 0x060005CC RID: 1484 RVA: 0x0002166F File Offset: 0x0001F86F
		public virtual void OnClanDestroyed(Clan destroyedClan)
		{
		}

		// Token: 0x060005CD RID: 1485 RVA: 0x00021671 File Offset: 0x0001F871
		public virtual void OnNewIssueCreated(IssueBase issue)
		{
		}

		// Token: 0x060005CE RID: 1486 RVA: 0x00021673 File Offset: 0x0001F873
		public virtual void OnIssueOwnerChanged(IssueBase issue, Hero oldOwner)
		{
		}

		// Token: 0x060005CF RID: 1487 RVA: 0x00021675 File Offset: 0x0001F875
		public virtual void OnNewItemCrafted(ItemObject itemObject)
		{
		}

		// Token: 0x060005D0 RID: 1488 RVA: 0x00021677 File Offset: 0x0001F877
		public virtual void OnWorkshopInitialized(Workshop workshop)
		{
		}

		// Token: 0x060005D1 RID: 1489 RVA: 0x00021679 File Offset: 0x0001F879
		public virtual void OnWorkshopOwnerChanged(Workshop workshop, Hero oldOwner)
		{
		}

		// Token: 0x060005D2 RID: 1490 RVA: 0x0002167B File Offset: 0x0001F87B
		public virtual void OnWorkshopTypeChanged(Workshop workshop)
		{
		}

		// Token: 0x060005D3 RID: 1491 RVA: 0x0002167D File Offset: 0x0001F87D
		public virtual void CraftingPartUnlocked(CraftingPiece craftingPiece)
		{
		}

		// Token: 0x060005D4 RID: 1492 RVA: 0x0002167F File Offset: 0x0001F87F
		public virtual void OnNewItemCrafted(ItemObject itemObject, ItemModifier overriddenItemModifier, bool isCraftingOrderItem)
		{
		}

		// Token: 0x060005D5 RID: 1493 RVA: 0x00021681 File Offset: 0x0001F881
		public virtual void OnEquipmentSmeltedByHero(Hero hero, EquipmentElement equipmentElement)
		{
		}

		// Token: 0x060005D6 RID: 1494 RVA: 0x00021683 File Offset: 0x0001F883
		public virtual void OnBeforeSave()
		{
		}

		// Token: 0x060005D7 RID: 1495 RVA: 0x00021685 File Offset: 0x0001F885
		public virtual void OnMainPartyPrisonerRecruited(FlattenedTroopRoster roster)
		{
		}

		// Token: 0x060005D8 RID: 1496 RVA: 0x00021687 File Offset: 0x0001F887
		public virtual void OnPrisonerTaken(FlattenedTroopRoster roster)
		{
		}

		// Token: 0x060005D9 RID: 1497 RVA: 0x00021689 File Offset: 0x0001F889
		public virtual void OnPrisonerDonatedToSettlement(MobileParty donatingParty, FlattenedTroopRoster donatedPrisoners, Settlement donatedSettlement)
		{
		}

		// Token: 0x060005DA RID: 1498 RVA: 0x0002168B File Offset: 0x0001F88B
		public virtual void CanMoveToSettlement(Hero hero, ref bool result)
		{
		}

		// Token: 0x060005DB RID: 1499 RVA: 0x0002168D File Offset: 0x0001F88D
		public virtual void OnHeroChangedClan(Hero hero, Clan oldClan)
		{
		}

		// Token: 0x060005DC RID: 1500 RVA: 0x0002168F File Offset: 0x0001F88F
		public virtual void CanHeroDie(Hero hero, KillCharacterAction.KillCharacterActionDetail causeOfDeath, ref bool result)
		{
		}

		// Token: 0x060005DD RID: 1501 RVA: 0x00021691 File Offset: 0x0001F891
		public virtual void CanPlayerMeetWithHeroAfterConversation(Hero hero, ref bool result)
		{
		}

		// Token: 0x060005DE RID: 1502 RVA: 0x00021693 File Offset: 0x0001F893
		public virtual void CanHeroBecomePrisoner(Hero hero, ref bool result)
		{
		}

		// Token: 0x060005DF RID: 1503 RVA: 0x00021695 File Offset: 0x0001F895
		public virtual void CanBeGovernorOrHavePartyRole(Hero hero, ref bool result)
		{
		}

		// Token: 0x060005E0 RID: 1504 RVA: 0x00021697 File Offset: 0x0001F897
		public virtual void OnSaveOver(bool isSuccessful, string saveName)
		{
		}

		// Token: 0x060005E1 RID: 1505 RVA: 0x00021699 File Offset: 0x0001F899
		public virtual void OnSaveStarted()
		{
		}

		// Token: 0x060005E2 RID: 1506 RVA: 0x0002169B File Offset: 0x0001F89B
		public virtual void CanHeroMarry(Hero hero, ref bool result)
		{
		}

		// Token: 0x060005E3 RID: 1507 RVA: 0x0002169D File Offset: 0x0001F89D
		public virtual void OnHeroTeleportationRequested(Hero hero, Settlement targetSettlement, MobileParty targetParty, TeleportHeroAction.TeleportationDetail detail)
		{
		}

		// Token: 0x060005E4 RID: 1508 RVA: 0x0002169F File Offset: 0x0001F89F
		public virtual void OnPartyLeaderChangeOfferCanceled(MobileParty party)
		{
		}

		// Token: 0x060005E5 RID: 1509 RVA: 0x000216A1 File Offset: 0x0001F8A1
		public virtual void OnPartyLeaderChanged(MobileParty mobileParty, Hero oldLeader)
		{
		}

		// Token: 0x060005E6 RID: 1510 RVA: 0x000216A3 File Offset: 0x0001F8A3
		public virtual void OnClanInfluenceChanged(Clan clan, float change)
		{
		}

		// Token: 0x060005E7 RID: 1511 RVA: 0x000216A5 File Offset: 0x0001F8A5
		public virtual void OnPlayerPartyKnockedOrKilledTroop(CharacterObject strikedTroop)
		{
		}

		// Token: 0x060005E8 RID: 1512 RVA: 0x000216A7 File Offset: 0x0001F8A7
		public virtual void OnPlayerEarnedGoldFromAsset(DefaultClanFinanceModel.AssetIncomeType incomeType, int incomeAmount)
		{
		}

		// Token: 0x060005E9 RID: 1513 RVA: 0x000216A9 File Offset: 0x0001F8A9
		public virtual void OnClanEarnedGoldFromTribute(Clan receiverClan, IFaction payingFaction)
		{
		}

		// Token: 0x060005EA RID: 1514 RVA: 0x000216AB File Offset: 0x0001F8AB
		public virtual void OnCollectLootItems(PartyBase winnerParty, ItemRoster gainedLoots)
		{
		}

		// Token: 0x060005EB RID: 1515 RVA: 0x000216AD File Offset: 0x0001F8AD
		public virtual void OnLootDistributedToParty(PartyBase winnerParty, PartyBase defeatedParty, ItemRoster lootedItems)
		{
		}

		// Token: 0x060005EC RID: 1516 RVA: 0x000216AF File Offset: 0x0001F8AF
		public virtual void OnPlayerJoinedTournament(Town town, bool isParticipant)
		{
		}

		// Token: 0x060005ED RID: 1517 RVA: 0x000216B1 File Offset: 0x0001F8B1
		public virtual void OnConfigChanged()
		{
		}

		// Token: 0x060005EE RID: 1518 RVA: 0x000216B3 File Offset: 0x0001F8B3
		public virtual void OnMobilePartyRaftStateChanged(MobileParty mobileParty)
		{
		}

		// Token: 0x060005EF RID: 1519 RVA: 0x000216B5 File Offset: 0x0001F8B5
		public virtual void OnCharacterCreationInitialized(CharacterCreationManager characterCreationManager)
		{
		}

		// Token: 0x060005F0 RID: 1520 RVA: 0x000216B7 File Offset: 0x0001F8B7
		public virtual void OnShipDestroyed(PartyBase owner, Ship ship, DestroyShipAction.ShipDestroyDetail detail)
		{
		}

		// Token: 0x060005F1 RID: 1521 RVA: 0x000216B9 File Offset: 0x0001F8B9
		public virtual void OnShipOwnerChanged(Ship ship, PartyBase oldOwner, ChangeShipOwnerAction.ShipOwnerChangeDetail shipOwnerChangeDetail)
		{
		}

		// Token: 0x060005F2 RID: 1522 RVA: 0x000216BB File Offset: 0x0001F8BB
		public virtual void OnFigureheadUnlocked(Figurehead figurehead)
		{
		}

		// Token: 0x060005F3 RID: 1523 RVA: 0x000216BD File Offset: 0x0001F8BD
		public virtual void OnShipRepaired(Ship ship, Settlement repairPort)
		{
		}

		// Token: 0x060005F4 RID: 1524 RVA: 0x000216BF File Offset: 0x0001F8BF
		public virtual void OnPartyLeftArmy(MobileParty party, Army army)
		{
		}

		// Token: 0x060005F5 RID: 1525 RVA: 0x000216C1 File Offset: 0x0001F8C1
		public virtual void OnIncidentResolved(Incident incident)
		{
		}

		// Token: 0x060005F6 RID: 1526 RVA: 0x000216C3 File Offset: 0x0001F8C3
		public virtual void OnPartyAddedToMapEvent(PartyBase partyBase)
		{
		}

		// Token: 0x060005F7 RID: 1527 RVA: 0x000216C5 File Offset: 0x0001F8C5
		public virtual void OnMobilePartyNavigationStateChanged(MobileParty mobileParty)
		{
		}

		// Token: 0x060005F8 RID: 1528 RVA: 0x000216C7 File Offset: 0x0001F8C7
		public virtual void OnMobilePartyJoinedToSiegeEvent(MobileParty mobileParty)
		{
		}

		// Token: 0x060005F9 RID: 1529 RVA: 0x000216C9 File Offset: 0x0001F8C9
		public virtual void OnMobilePartyLeftSiegeEvent(MobileParty mobileParty)
		{
		}

		// Token: 0x060005FA RID: 1530 RVA: 0x000216CB File Offset: 0x0001F8CB
		public virtual void OnBlockadeActivated(SiegeEvent siegeEvent)
		{
		}

		// Token: 0x060005FB RID: 1531 RVA: 0x000216CD File Offset: 0x0001F8CD
		public virtual void OnBlockadeDeactivated(SiegeEvent siegeEvent)
		{
		}

		// Token: 0x060005FC RID: 1532 RVA: 0x000216CF File Offset: 0x0001F8CF
		public virtual void OnShipCreated(Ship ship, Settlement createdSettlement)
		{
		}

		// Token: 0x060005FD RID: 1533 RVA: 0x000216D1 File Offset: 0x0001F8D1
		public virtual void OnMercenaryServiceStarted(Clan mercenaryClan, StartMercenaryServiceAction.StartMercenaryServiceActionDetails details)
		{
		}

		// Token: 0x060005FE RID: 1534 RVA: 0x000216D3 File Offset: 0x0001F8D3
		public virtual void OnMercenaryServiceEnded(Clan mercenaryClan, EndMercenaryServiceAction.EndMercenaryServiceActionDetails details)
		{
		}

		// Token: 0x060005FF RID: 1535 RVA: 0x000216D5 File Offset: 0x0001F8D5
		public virtual void OnMapMarkerCreated(MapMarker mapMarker)
		{
		}

		// Token: 0x06000600 RID: 1536 RVA: 0x000216D7 File Offset: 0x0001F8D7
		public virtual void OnMapMarkerRemoved(MapMarker mapMarker)
		{
		}

		// Token: 0x06000601 RID: 1537 RVA: 0x000216D9 File Offset: 0x0001F8D9
		public virtual void OnAllianceStarted(Kingdom kingdom1, Kingdom kingdom2)
		{
		}

		// Token: 0x06000602 RID: 1538 RVA: 0x000216DB File Offset: 0x0001F8DB
		public virtual void OnAllianceEnded(Kingdom kingdom1, Kingdom kingdom2)
		{
		}

		// Token: 0x06000603 RID: 1539 RVA: 0x000216DD File Offset: 0x0001F8DD
		public virtual void OnCallToWarAgreementStarted(Kingdom callingKingdom, Kingdom calledKingdom, Kingdom kingdomToCallToWarAgainst)
		{
		}

		// Token: 0x06000604 RID: 1540 RVA: 0x000216DF File Offset: 0x0001F8DF
		public virtual void OnCallToWarAgreementEnded(Kingdom callingKingdom, Kingdom calledKingdom, Kingdom kingdomToCallToWarAgainst)
		{
		}

		// Token: 0x06000605 RID: 1541 RVA: 0x000216E1 File Offset: 0x0001F8E1
		public virtual void CanHeroLeadParty(Hero hero, ref bool result)
		{
		}

		// Token: 0x06000606 RID: 1542 RVA: 0x000216E3 File Offset: 0x0001F8E3
		public virtual void OnCraftingOrderCompleted(Town town, CraftingOrder craftingOrder, ItemObject craftedItem, Hero completerHero)
		{
		}

		// Token: 0x06000607 RID: 1543 RVA: 0x000216E5 File Offset: 0x0001F8E5
		public virtual void OnItemsRefined(Hero hero, Crafting.RefiningFormula refineFormula)
		{
		}

		// Token: 0x06000608 RID: 1544 RVA: 0x000216E7 File Offset: 0x0001F8E7
		public virtual void OnMapEventContinuityNeedsUpdate(IFaction faction)
		{
		}

		// Token: 0x06000609 RID: 1545 RVA: 0x000216E9 File Offset: 0x0001F8E9
		public virtual void OnHeirSelectionOver(Hero selectedHeir)
		{
		}

		// Token: 0x0600060A RID: 1546 RVA: 0x000216EB File Offset: 0x0001F8EB
		public virtual void OnHeirSelectionRequested(Dictionary<Hero, int> heirApparents)
		{
		}

		// Token: 0x0600060B RID: 1547 RVA: 0x000216ED File Offset: 0x0001F8ED
		public virtual void OnMainPartyStarving()
		{
		}

		// Token: 0x0600060C RID: 1548 RVA: 0x000216EF File Offset: 0x0001F8EF
		public virtual void OnHeroGetsBusy(Hero hero, HeroGetsBusyReasons heroGetsBusyReason)
		{
		}

		// Token: 0x0600060D RID: 1549 RVA: 0x000216F1 File Offset: 0x0001F8F1
		public virtual void CanHeroEquipmentBeChanged(Hero hero, ref bool result)
		{
		}

		// Token: 0x0600060E RID: 1550 RVA: 0x000216F3 File Offset: 0x0001F8F3
		public virtual void CanHaveCampaignIssues(Hero hero, ref bool result)
		{
		}

		// Token: 0x0600060F RID: 1551 RVA: 0x000216F5 File Offset: 0x0001F8F5
		public virtual void IsSettlementBusy(Settlement settlement, object asker, ref int flags)
		{
		}

		// Token: 0x06000610 RID: 1552 RVA: 0x000216F7 File Offset: 0x0001F8F7
		public virtual void OnHeroUnregistered(Hero hero)
		{
		}
	}
}
