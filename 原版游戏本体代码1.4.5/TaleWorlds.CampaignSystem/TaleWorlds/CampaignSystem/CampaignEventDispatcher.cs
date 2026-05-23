using System;
using System.Collections.Generic;
using System.Linq;
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

public class CampaignEventDispatcher : CampaignEventReceiver
{
	private CampaignEventReceiver[] _eventReceivers;

	public static CampaignEventDispatcher Instance => Campaign.Current?.CampaignEventDispatcher;

	internal CampaignEventDispatcher(IEnumerable<CampaignEventReceiver> eventReceivers)
	{
		_eventReceivers = eventReceivers.ToArray();
	}

	internal void AddCampaignEventReceiver(CampaignEventReceiver receiver)
	{
		CampaignEventReceiver[] array = new CampaignEventReceiver[_eventReceivers.Length + 1];
		for (int i = 0; i < _eventReceivers.Length; i++)
		{
			array[i] = _eventReceivers[i];
		}
		array[_eventReceivers.Length] = receiver;
		_eventReceivers = array;
	}

	public override void RemoveListeners(object o)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].RemoveListeners(o);
		}
	}

	public override void OnPlayerBodyPropertiesChanged()
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnPlayerBodyPropertiesChanged();
		}
	}

	public override void OnHeroLevelledUp(Hero hero, bool shouldNotify = true)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnHeroLevelledUp(hero, shouldNotify);
		}
	}

	public override void OnHomeHideoutChanged(BanditPartyComponent banditPartyComponent, Hideout oldHomeHideout)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnHomeHideoutChanged(banditPartyComponent, oldHomeHideout);
		}
	}

	public override void OnCharacterCreationIsOver()
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnCharacterCreationIsOver();
		}
	}

	public override void OnHeroGainedSkill(Hero hero, SkillObject skill, int change = 1, bool shouldNotify = true)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnHeroGainedSkill(hero, skill, change, shouldNotify);
		}
	}

	public override void OnHeroWounded(Hero woundedHero)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnHeroWounded(woundedHero);
		}
	}

	public override void OnHeroRelationChanged(Hero effectiveHero, Hero effectiveHeroGainedRelationWith, int relationChange, bool showNotification, ChangeRelationAction.ChangeRelationDetail detail, Hero originalHero, Hero originalGainedRelationWith)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnHeroRelationChanged(effectiveHero, effectiveHeroGainedRelationWith, relationChange, showNotification, detail, originalHero, originalGainedRelationWith);
		}
	}

	public override void OnLootDistributedToParty(PartyBase winnerParty, PartyBase defeatedParty, ItemRoster lootedItems)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnLootDistributedToParty(winnerParty, defeatedParty, lootedItems);
		}
	}

	public override void OnHeroOccupationChanged(Hero hero, Occupation oldOccupation)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnHeroOccupationChanged(hero, oldOccupation);
		}
	}

	public override void OnBarterAccepted(Hero offererHero, Hero otherHero, List<Barterable> barters)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnBarterAccepted(offererHero, otherHero, barters);
		}
	}

	public override void OnBarterCanceled(Hero offererHero, Hero otherHero, List<Barterable> barters)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnBarterCanceled(offererHero, otherHero, barters);
		}
	}

	public override void OnHeroCreated(Hero hero, bool isBornNaturally = false)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnHeroCreated(hero, isBornNaturally);
		}
	}

	public override void OnHeroActivated(Hero hero, Hero.CharacterStates previousState)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnHeroActivated(hero, previousState);
		}
	}

	public override void OnQuestLogAdded(QuestBase quest, bool hideInformation)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnQuestLogAdded(quest, hideInformation);
		}
	}

	public override void OnIssueLogAdded(IssueBase issue, bool hideInformation)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnIssueLogAdded(issue, hideInformation);
		}
	}

	public override void OnClanTierChanged(Clan clan, bool shouldNotify = true)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnClanTierChanged(clan, shouldNotify);
		}
	}

	public override void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail actionDetail, bool showNotification = true)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnClanChangedKingdom(clan, oldKingdom, newKingdom, actionDetail, showNotification);
		}
	}

	public override void OnClanDefected(Clan clan, Kingdom oldKingdom, Kingdom newKingdom)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnClanDefected(clan, oldKingdom, newKingdom);
		}
	}

	public override void OnClanCreated(Clan clan, bool isCompanion)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnClanCreated(clan, isCompanion);
		}
	}

	public override void OnHeroJoinedParty(Hero hero, MobileParty party)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnHeroJoinedParty(hero, party);
		}
	}

	public override void OnKingdomDecisionAdded(KingdomDecision decision, bool isPlayerInvolved)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnKingdomDecisionAdded(decision, isPlayerInvolved);
		}
	}

	public override void OnKingdomDecisionCancelled(KingdomDecision decision, bool isPlayerInvolved)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnKingdomDecisionCancelled(decision, isPlayerInvolved);
		}
	}

	public override void OnKingdomDecisionConcluded(KingdomDecision decision, DecisionOutcome chosenOutcome, bool isPlayerInvolved)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnKingdomDecisionConcluded(decision, chosenOutcome, isPlayerInvolved);
		}
	}

	public override void OnHeroOrPartyTradedGold((Hero, PartyBase) giver, (Hero, PartyBase) recipient, (int, string) goldAmount, bool showNotification)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnHeroOrPartyTradedGold(giver, recipient, goldAmount, showNotification);
		}
	}

	public override void OnHeroOrPartyGaveItem((Hero, PartyBase) giver, (Hero, PartyBase) receiver, ItemRosterElement itemRosterElement, bool showNotification)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnHeroOrPartyGaveItem(giver, receiver, itemRosterElement, showNotification);
		}
	}

	public override void OnBanditPartyRecruited(MobileParty banditParty)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnBanditPartyRecruited(banditParty);
		}
	}

	public override void OnArmyCreated(Army army)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnArmyCreated(army);
		}
	}

	public override void OnPartyAttachedAnotherParty(MobileParty mobileParty)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnPartyAttachedAnotherParty(mobileParty);
		}
	}

	public override void OnNearbyPartyAddedToPlayerMapEvent(MobileParty mobileParty)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnNearbyPartyAddedToPlayerMapEvent(mobileParty);
		}
	}

	public override void OnArmyDispersed(Army army, Army.ArmyDispersionReason reason, bool isPlayersArmy)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnArmyDispersed(army, reason, isPlayersArmy);
		}
	}

	public override void OnArmyGathered(Army army, IMapPoint gatheringPoint)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnArmyGathered(army, gatheringPoint);
		}
	}

	public override void OnPerkOpened(Hero hero, PerkObject perk)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnPerkOpened(hero, perk);
		}
	}

	public override void OnPerkReset(Hero hero, PerkObject perk)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnPerkReset(hero, perk);
		}
	}

	public override void OnPlayerTraitChanged(TraitObject trait, int previousLevel)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnPlayerTraitChanged(trait, previousLevel);
		}
	}

	public override void OnVillageStateChanged(Village village, Village.VillageStates oldState, Village.VillageStates newState, MobileParty raiderParty)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnVillageStateChanged(village, oldState, newState, raiderParty);
		}
	}

	public override void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnSettlementEntered(party, settlement, hero);
		}
	}

	public override void OnAfterSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnAfterSettlementEntered(party, settlement, hero);
		}
	}

	public override void OnBeforeSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnBeforeSettlementEntered(party, settlement, hero);
		}
	}

	public override void OnMercenaryTroopChangedInTown(Town town, CharacterObject oldTroopType, CharacterObject newTroopType)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnMercenaryTroopChangedInTown(town, oldTroopType, newTroopType);
		}
	}

	public override void OnMercenaryNumberChangedInTown(Town town, int oldNumber, int newNumber)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnMercenaryNumberChangedInTown(town, oldNumber, newNumber);
		}
	}

	public override void OnAlleyOccupiedByPlayer(Alley alley, TroopRoster troops)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnAlleyOccupiedByPlayer(alley, troops);
		}
	}

	public override void OnAlleyOwnerChanged(Alley alley, Hero newOwner, Hero oldOwner)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnAlleyOwnerChanged(alley, newOwner, oldOwner);
		}
	}

	public override void OnAlleyClearedByPlayer(Alley alley)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnAlleyClearedByPlayer(alley);
		}
	}

	public override void OnRomanticStateChanged(Hero hero1, Hero hero2, Romance.RomanceLevelEnum romanceLevel)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnRomanticStateChanged(hero1, hero2, romanceLevel);
		}
	}

	public override void OnBeforeHeroesMarried(Hero hero1, Hero hero2, bool showNotification)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnBeforeHeroesMarried(hero1, hero2, showNotification);
		}
	}

	public override void OnPlayerEliminatedFromTournament(int round, Town town)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnPlayerEliminatedFromTournament(round, town);
		}
	}

	public override void OnPlayerStartedTournamentMatch(Town town)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnPlayerStartedTournamentMatch(town);
		}
	}

	public override void OnTournamentStarted(Town town)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnTournamentStarted(town);
		}
	}

	public override void OnTournamentFinished(CharacterObject winner, MBReadOnlyList<CharacterObject> participants, Town town, ItemObject prize)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnTournamentFinished(winner, participants, town, prize);
		}
	}

	public override void OnTournamentCancelled(Town town)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnTournamentCancelled(town);
		}
	}

	public override void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail declareWarDetail)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnWarDeclared(faction1, faction2, declareWarDetail);
		}
	}

	public override void OnRulingClanChanged(Kingdom kingdom, Clan oldRulingClan)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnRulingClanChanged(kingdom, oldRulingClan);
		}
	}

	public override void OnStartBattle(PartyBase attackerParty, PartyBase defenderParty, object subject, bool showNotification)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnStartBattle(attackerParty, defenderParty, subject, showNotification);
		}
	}

	public override void OnRebellionFinished(Settlement settlement, Clan oldOwnerClan)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnRebellionFinished(settlement, oldOwnerClan);
		}
	}

	public override void TownRebelliousStateChanged(Town town, bool rebelliousState)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].TownRebelliousStateChanged(town, rebelliousState);
		}
	}

	public override void OnRebelliousClanDisbandedAtSettlement(Settlement settlement, Clan rebelliousClan)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnRebelliousClanDisbandedAtSettlement(settlement, rebelliousClan);
		}
	}

	public override void OnItemsLooted(MobileParty mobileParty, ItemRoster items)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnItemsLooted(mobileParty, items);
		}
	}

	public override void OnMobilePartyDestroyed(MobileParty mobileParty, PartyBase destroyerParty)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnMobilePartyDestroyed(mobileParty, destroyerParty);
		}
	}

	public override void OnMobilePartyCreated(MobileParty party)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnMobilePartyCreated(party);
		}
	}

	public override void OnMapInteractableCreated(IInteractablePoint interactable)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnMapInteractableCreated(interactable);
		}
	}

	public override void OnMapInteractableDestroyed(IInteractablePoint interactable)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnMapInteractableDestroyed(interactable);
		}
	}

	public override void OnMobilePartyQuestStatusChanged(MobileParty party, bool isUsedByQuest)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnMobilePartyQuestStatusChanged(party, isUsedByQuest);
		}
	}

	public override void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification = true)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnHeroKilled(victim, killer, detail, showNotification);
		}
	}

	public override void OnBeforeHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification = true)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnBeforeHeroKilled(victim, killer, detail, showNotification);
		}
	}

	public override void OnChildEducationCompleted(Hero hero, int age)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnChildEducationCompleted(hero, age);
		}
	}

	public override void OnHeroComesOfAge(Hero hero)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnHeroComesOfAge(hero);
		}
	}

	public override void OnHeroReachesTeenAge(Hero hero)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnHeroReachesTeenAge(hero);
		}
	}

	public override void OnHeroGrowsOutOfInfancy(Hero hero)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnHeroGrowsOutOfInfancy(hero);
		}
	}

	public override void OnCharacterDefeated(Hero winner, Hero loser)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnCharacterDefeated(winner, loser);
		}
	}

	public override void OnHeroPrisonerTaken(PartyBase capturer, Hero prisoner)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnHeroPrisonerTaken(capturer, prisoner);
		}
	}

	public override void OnHeroPrisonerReleased(Hero prisoner, PartyBase party, IFaction capturerFaction, EndCaptivityDetail detail, bool showNotification = true)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnHeroPrisonerReleased(prisoner, party, capturerFaction, detail, showNotification);
		}
	}

	public override void OnCharacterBecameFugitive(Hero hero, bool showNotification)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnCharacterBecameFugitive(hero, showNotification);
		}
	}

	public override void OnPlayerLearnsAboutHero(Hero hero)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnPlayerLearnsAboutHero(hero);
		}
	}

	public override void OnPlayerMetHero(Hero hero)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnPlayerMetHero(hero);
		}
	}

	public override void OnRenownGained(Hero hero, int gainedRenown, bool doNotNotify)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnRenownGained(hero, gainedRenown, doNotNotify);
		}
	}

	public override void OnCrimeRatingChanged(IFaction kingdom, float deltaCrimeAmount)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnCrimeRatingChanged(kingdom, deltaCrimeAmount);
		}
	}

	public override void OnNewCompanionAdded(Hero newCompanion)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnNewCompanionAdded(newCompanion);
		}
	}

	public override void OnAfterMissionStarted(IMission iMission)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnAfterMissionStarted(iMission);
		}
	}

	public override void OnGameMenuOpened(MenuCallbackArgs args)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnGameMenuOpened(args);
		}
	}

	public override void OnMakePeace(IFaction side1Faction, IFaction side2Faction, MakePeaceAction.MakePeaceDetail detail)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnMakePeace(side1Faction, side2Faction, detail);
		}
	}

	public override void OnKingdomDestroyed(Kingdom destroyedKingdom)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnKingdomDestroyed(destroyedKingdom);
		}
	}

	public override void CanKingdomBeDiscontinued(Kingdom kingdom, ref bool result)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].CanKingdomBeDiscontinued(kingdom, ref result);
		}
	}

	public override void OnKingdomCreated(Kingdom createdKingdom)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnKingdomCreated(createdKingdom);
		}
	}

	public override void OnVillageBecomeNormal(Village village)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnVillageBecomeNormal(village);
		}
	}

	public override void OnVillageBeingRaided(Village village)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnVillageBeingRaided(village);
		}
	}

	public override void OnVillageLooted(Village village)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnVillageLooted(village);
		}
	}

	public override void OnConversationEnded(IEnumerable<CharacterObject> characters)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnConversationEnded(characters);
		}
	}

	public override void OnAgentJoinedConversation(IAgent agent)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnAgentJoinedConversation(agent);
		}
	}

	public override void OnMapEventEnded(MapEvent mapEvent)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnMapEventEnded(mapEvent);
		}
	}

	public override void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnMapEventStarted(mapEvent, attackerParty, defenderParty);
		}
	}

	public override void OnPrisonersChangeInSettlement(Settlement settlement, FlattenedTroopRoster prisonerRoster, Hero prisonerHero, bool takenFromDungeon)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnPrisonersChangeInSettlement(settlement, prisonerRoster, prisonerHero, takenFromDungeon);
		}
	}

	public override void OnMissionStarted(IMission mission)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnMissionStarted(mission);
		}
	}

	public override void OnPlayerBoardGameOver(Hero opposingHero, BoardGameHelper.BoardGameState state)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnPlayerBoardGameOver(opposingHero, state);
		}
	}

	public override void OnRansomOfferedToPlayer(Hero captiveHero)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnRansomOfferedToPlayer(captiveHero);
		}
	}

	public override void OnRansomOfferCancelled(Hero captiveHero)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnRansomOfferCancelled(captiveHero);
		}
	}

	public override void OnPeaceOfferedToPlayer(IFaction opponentFaction, int tributeAmount, int tributeDurationInDays)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnPeaceOfferedToPlayer(opponentFaction, tributeAmount, tributeDurationInDays);
		}
	}

	public override void OnTradeAgreementSigned(Kingdom kingdom, Kingdom other)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnTradeAgreementSigned(kingdom, other);
		}
	}

	public override void OnPeaceOfferResolved(IFaction opponentFaction)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnPeaceOfferResolved(opponentFaction);
		}
	}

	public override void OnMarriageOfferedToPlayer(Hero suitor, Hero maiden)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnMarriageOfferedToPlayer(suitor, maiden);
		}
	}

	public override void OnMarriageOfferCanceled(Hero suitor, Hero maiden)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnMarriageOfferCanceled(suitor, maiden);
		}
	}

	public override void OnVassalOrMercenaryServiceOfferedToPlayer(Kingdom offeredKingdom)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnVassalOrMercenaryServiceOfferedToPlayer(offeredKingdom);
		}
	}

	public override void OnCommonAreaStateChanged(Alley alley, Alley.AreaState oldState, Alley.AreaState newState)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnCommonAreaStateChanged(alley, oldState, newState);
		}
	}

	public override void OnVassalOrMercenaryServiceOfferCanceled(Kingdom offeredKingdom)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnVassalOrMercenaryServiceOfferCanceled(offeredKingdom);
		}
	}

	public override void BeforeMissionOpened()
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].BeforeMissionOpened();
		}
	}

	public override void OnPartyRemoved(PartyBase party)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnPartyRemoved(party);
		}
	}

	public override void OnPartySizeChanged(PartyBase party)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnPartySizeChanged(party);
		}
	}

	public override void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnSettlementOwnerChanged(settlement, openToClaim, newOwner, oldOwner, capturerHero, detail);
		}
	}

	public override void OnGovernorChanged(Town fortification, Hero oldGovernor, Hero newGovernor)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnGovernorChanged(fortification, oldGovernor, newGovernor);
		}
	}

	public override void OnSettlementLeft(MobileParty party, Settlement settlement)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnSettlementLeft(party, settlement);
		}
	}

	public override void Tick(float dt)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].Tick(dt);
		}
	}

	public override void OnSessionStart(CampaignGameStarter campaignGameStarter)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnSessionStart(campaignGameStarter);
		}
	}

	public override void OnAfterSessionStart(CampaignGameStarter campaignGameStarter)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnAfterSessionStart(campaignGameStarter);
		}
	}

	public override void OnNewGameCreated(CampaignGameStarter campaignGameStarter)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnNewGameCreated(campaignGameStarter);
		}
	}

	public override void OnGameEarlyLoaded(CampaignGameStarter campaignGameStarter)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnGameEarlyLoaded(campaignGameStarter);
		}
	}

	public override void OnGameLoaded(CampaignGameStarter campaignGameStarter)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnGameLoaded(campaignGameStarter);
		}
	}

	public override void OnGameLoadFinished()
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnGameLoadFinished();
		}
	}

	public override void OnPartyJoinedArmy(MobileParty mobileParty)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnPartyJoinedArmy(mobileParty);
		}
	}

	public override void OnPartyRemovedFromArmy(MobileParty mobileParty)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnPartyRemovedFromArmy(mobileParty);
		}
	}

	public override void OnPlayerArmyLeaderChangedBehavior()
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnPlayerArmyLeaderChangedBehavior();
		}
	}

	public override void OnArmyOverlaySetDirty()
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnArmyOverlaySetDirty();
		}
	}

	public override void OnPlayerDesertedBattle(int sacrificedMenCount)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnPlayerDesertedBattle(sacrificedMenCount);
		}
	}

	public override void MissionTick(float dt)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].MissionTick(dt);
		}
	}

	public override void OnChildConceived(Hero mother)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnChildConceived(mother);
		}
	}

	public override void OnGivenBirth(Hero mother, List<Hero> aliveChildren, int stillbornCount)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnGivenBirth(mother, aliveChildren, stillbornCount);
		}
	}

	public override void OnUnitRecruited(CharacterObject character, int amount)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnUnitRecruited(character, amount);
		}
	}

	public override void OnPlayerBattleEnd(MapEvent mapEvent)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnPlayerBattleEnd(mapEvent);
		}
	}

	public override void OnMissionEnded(IMission mission)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnMissionEnded(mission);
		}
	}

	public override void TickPartialHourlyAi(MobileParty party)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].TickPartialHourlyAi(party);
		}
	}

	public override void QuarterDailyPartyTick(MobileParty party)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].QuarterDailyPartyTick(party);
		}
	}

	public override void AiHourlyTick(MobileParty party, PartyThinkParams partyThinkParams)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].AiHourlyTick(party, partyThinkParams);
		}
	}

	public override void HourlyTick()
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].HourlyTick();
		}
	}

	public override void QuarterHourlyTick()
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].QuarterHourlyTick();
		}
	}

	public override void HourlyTickParty(MobileParty mobileParty)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].HourlyTickParty(mobileParty);
		}
	}

	public override void HourlyTickSettlement(Settlement settlement)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].HourlyTickSettlement(settlement);
		}
	}

	public override void HourlyTickClan(Clan clan)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].HourlyTickClan(clan);
		}
	}

	public override void DailyTick()
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].DailyTick();
		}
	}

	public override void DailyTickParty(MobileParty mobileParty)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].DailyTickParty(mobileParty);
		}
	}

	public override void DailyTickTown(Town town)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].DailyTickTown(town);
		}
	}

	public override void DailyTickSettlement(Settlement settlement)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].DailyTickSettlement(settlement);
		}
	}

	public override void DailyTickHero(Hero hero)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].DailyTickHero(hero);
		}
	}

	public override void DailyTickClan(Clan clan)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].DailyTickClan(clan);
		}
	}

	public override void WeeklyTick()
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].WeeklyTick();
		}
	}

	public override void CollectAvailableTutorials(ref List<CampaignTutorial> tutorials)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].CollectAvailableTutorials(ref tutorials);
		}
	}

	public override void OnTutorialCompleted(string tutorial)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnTutorialCompleted(tutorial);
		}
	}

	public override void BeforeGameMenuOpened(MenuCallbackArgs args)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].BeforeGameMenuOpened(args);
		}
	}

	public override void AfterGameMenuInitialized(MenuCallbackArgs args)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].AfterGameMenuInitialized(args);
		}
	}

	public override void OnBarterablesRequested(BarterData args)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnBarterablesRequested(args);
		}
	}

	public override void OnPartyVisibilityChanged(PartyBase party)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnPartyVisibilityChanged(party);
		}
	}

	public override void OnCompanionRemoved(Hero companion, RemoveCompanionAction.RemoveCompanionDetail detail)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnCompanionRemoved(companion, detail);
		}
	}

	public override void TrackDetected(Track track)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].TrackDetected(track);
		}
	}

	public override void TrackLost(Track track)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].TrackLost(track);
		}
	}

	public override void LocationCharactersAreReadyToSpawn(Dictionary<string, int> unusedUsablePointCount)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].LocationCharactersAreReadyToSpawn(unusedUsablePointCount);
		}
	}

	public override void LocationCharactersSimulated()
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].LocationCharactersSimulated();
		}
	}

	public override void OnBeforePlayerAgentSpawn(ref MatrixFrame spawnFrame)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnBeforePlayerAgentSpawn(ref spawnFrame);
		}
	}

	public override void OnPlayerAgentSpawned()
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnPlayerAgentSpawned();
		}
	}

	public override void OnPlayerUpgradedTroops(CharacterObject upgradeFromTroop, CharacterObject upgradeToTroop, int number)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnPlayerUpgradedTroops(upgradeFromTroop, upgradeToTroop, number);
		}
	}

	public override void OnHeroCombatHit(CharacterObject attackerTroop, CharacterObject attackedTroop, PartyBase party, WeaponComponentData usedWeapon, bool isFatal, int xp)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnHeroCombatHit(attackerTroop, attackedTroop, party, usedWeapon, isFatal, xp);
		}
	}

	public override void OnCharacterPortraitPopUpOpened(CharacterObject character)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnCharacterPortraitPopUpOpened(character);
		}
	}

	public override void OnCharacterPortraitPopUpClosed()
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnCharacterPortraitPopUpClosed();
		}
	}

	public override void OnPlayerStartTalkFromMenu(Hero hero)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnPlayerStartTalkFromMenu(hero);
		}
	}

	public override void OnGameMenuOptionSelected(GameMenu gameMenu, GameMenuOption gameMenuOption)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnGameMenuOptionSelected(gameMenu, gameMenuOption);
		}
	}

	public override void OnPlayerStartRecruitment(CharacterObject recruitTroopCharacter)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnPlayerStartRecruitment(recruitTroopCharacter);
		}
	}

	public override void OnBeforePlayerCharacterChanged(Hero oldPlayer, Hero newPlayer)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnBeforePlayerCharacterChanged(oldPlayer, newPlayer);
		}
	}

	public override void OnPlayerCharacterChanged(Hero oldPlayer, Hero newPlayer, MobileParty newPlayerParty, bool isMainPartyChanged)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnPlayerCharacterChanged(oldPlayer, newPlayer, newPlayerParty, isMainPartyChanged);
		}
	}

	public override void OnClanLeaderChanged(Hero oldLeader, Hero newLeader)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnClanLeaderChanged(oldLeader, newLeader);
		}
	}

	public override void OnSiegeEventStarted(SiegeEvent siegeEvent)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnSiegeEventStarted(siegeEvent);
		}
	}

	public override void OnPlayerSiegeStarted()
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnPlayerSiegeStarted();
		}
	}

	public override void OnSiegeEventEnded(SiegeEvent siegeEvent)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnSiegeEventEnded(siegeEvent);
		}
	}

	public override void OnSiegeAftermathApplied(MobileParty attackerParty, Settlement settlement, SiegeAftermathAction.SiegeAftermath aftermathType, Clan previousSettlementOwner, Dictionary<MobileParty, float> partyContributions)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnSiegeAftermathApplied(attackerParty, settlement, aftermathType, previousSettlementOwner, partyContributions);
		}
	}

	public override void OnSiegeBombardmentHit(MobileParty besiegerParty, Settlement besiegedSettlement, BattleSideEnum side, SiegeEngineType weapon, SiegeBombardTargets target)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnSiegeBombardmentHit(besiegerParty, besiegedSettlement, side, weapon, target);
		}
	}

	public override void OnSiegeBombardmentWallHit(MobileParty besiegerParty, Settlement besiegedSettlement, BattleSideEnum side, SiegeEngineType weapon, bool isWallCracked)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnSiegeBombardmentWallHit(besiegerParty, besiegedSettlement, side, weapon, isWallCracked);
		}
	}

	public override void OnSiegeEngineDestroyed(MobileParty besiegerParty, Settlement besiegedSettlement, BattleSideEnum side, SiegeEngineType destroyedEngine)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnSiegeEngineDestroyed(besiegerParty, besiegedSettlement, side, destroyedEngine);
		}
	}

	public override void OnTradeRumorIsTaken(List<TradeRumor> newRumors, Settlement sourceSettlement = null)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnTradeRumorIsTaken(newRumors, sourceSettlement);
		}
	}

	public override void OnCheckForIssue(Hero hero)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnCheckForIssue(hero);
		}
	}

	public override void OnIssueUpdated(IssueBase issue, IssueBase.IssueUpdateDetails details, Hero issueSolver)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnIssueUpdated(issue, details, issueSolver);
		}
	}

	public override void OnTroopsDeserted(MobileParty mobileParty, TroopRoster desertedTroops)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnTroopsDeserted(mobileParty, desertedTroops);
		}
	}

	public override void OnTroopRecruited(Hero recruiterHero, Settlement recruitmentSettlement, Hero recruitmentSource, CharacterObject troop, int amount)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnTroopRecruited(recruiterHero, recruitmentSettlement, recruitmentSource, troop, amount);
		}
	}

	public override void OnTroopGivenToSettlement(Hero giverHero, Settlement recipientSettlement, TroopRoster roster)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnTroopGivenToSettlement(giverHero, recipientSettlement, roster);
		}
	}

	public override void OnItemSold(PartyBase receiverParty, PartyBase payerParty, ItemRosterElement itemRosterElement, int number, Settlement currentSettlement)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnItemSold(receiverParty, payerParty, itemRosterElement, number, currentSettlement);
		}
	}

	public override void OnCaravanTransactionCompleted(MobileParty caravanParty, Town town, List<(EquipmentElement, int)> itemRosterElements)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnCaravanTransactionCompleted(caravanParty, town, itemRosterElements);
		}
	}

	public override void OnPrisonerSold(PartyBase sellerParty, PartyBase buyerParty, TroopRoster prisoners)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnPrisonerSold(sellerParty, buyerParty, prisoners);
		}
	}

	public override void OnPartyDisbanded(MobileParty disbandParty, Settlement relatedSettlement)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnPartyDisbanded(disbandParty, relatedSettlement);
		}
	}

	public override void OnPartyDisbandStarted(MobileParty disbandParty)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnPartyDisbandStarted(disbandParty);
		}
	}

	public override void OnPartyDisbandCanceled(MobileParty disbandParty)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnPartyDisbandCanceled(disbandParty);
		}
	}

	public override void OnBuildingLevelChanged(Town town, Building building, int levelChange)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnBuildingLevelChanged(town, building, levelChange);
		}
	}

	public override void OnHideoutSpotted(PartyBase party, PartyBase hideoutParty)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnHideoutSpotted(party, hideoutParty);
		}
	}

	public override void OnHideoutDeactivated(Settlement hideout)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnHideoutDeactivated(hideout);
		}
	}

	public override void OnHeroSharedFoodWithAnother(Hero supporterHero, Hero supportedHero, float influence)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnHeroSharedFoodWithAnother(supporterHero, supportedHero, influence);
		}
	}

	public override void OnItemsDiscardedByPlayer(ItemRoster roster)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnItemsDiscardedByPlayer(roster);
		}
	}

	public override void OnPlayerInventoryExchange(List<(ItemRosterElement, int)> purchasedItems, List<(ItemRosterElement, int)> soldItems, bool isTrading)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnPlayerInventoryExchange(purchasedItems, soldItems, isTrading);
		}
	}

	public override void OnPersuasionProgressCommitted(Tuple<PersuasionOptionArgs, PersuasionOptionResult> progress)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnPersuasionProgressCommitted(progress);
		}
	}

	public override void OnQuestCompleted(QuestBase quest, QuestBase.QuestCompleteDetails detail)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnQuestCompleted(quest, detail);
		}
	}

	public override void OnQuestStarted(QuestBase quest)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnQuestStarted(quest);
		}
	}

	public override void OnItemProduced(ItemObject itemObject, Settlement settlement, int count)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnItemProduced(itemObject, settlement, count);
		}
	}

	public override void OnItemConsumed(ItemObject itemObject, Settlement settlement, int count)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnItemConsumed(itemObject, settlement, count);
		}
	}

	public override void OnPartyConsumedFood(MobileParty party)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnPartyConsumedFood(party);
		}
	}

	public override void OnNewIssueCreated(IssueBase issue)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnNewIssueCreated(issue);
		}
	}

	public override void OnIssueOwnerChanged(IssueBase issue, Hero oldOwner)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnIssueOwnerChanged(issue, oldOwner);
		}
	}

	public override void OnBeforeMainCharacterDied(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification = true)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnBeforeMainCharacterDied(victim, killer, detail, showNotification);
		}
	}

	public override void OnGameOver()
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnGameOver();
		}
	}

	public override void SiegeCompleted(Settlement siegeSettlement, MobileParty attackerParty, bool isWin, MapEvent.BattleTypes battleType)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].SiegeCompleted(siegeSettlement, attackerParty, isWin, battleType);
		}
	}

	public override void AfterSiegeCompleted(Settlement siegeSettlement, MobileParty attackerParty, bool isWin, MapEvent.BattleTypes battleType)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].AfterSiegeCompleted(siegeSettlement, attackerParty, isWin, battleType);
		}
	}

	public override void SiegeEngineBuilt(SiegeEvent siegeEvent, BattleSideEnum side, SiegeEngineType siegeEngine)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].SiegeEngineBuilt(siegeEvent, side, siegeEngine);
		}
	}

	public override void RaidCompleted(BattleSideEnum winnerSide, RaidEventComponent raidEvent)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].RaidCompleted(winnerSide, raidEvent);
		}
	}

	public override void ForceSuppliesCompleted(BattleSideEnum winnerSide, ForceSuppliesEventComponent forceSuppliesEvent)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].ForceSuppliesCompleted(winnerSide, forceSuppliesEvent);
		}
	}

	public override void ForceVolunteersCompleted(BattleSideEnum winnerSide, ForceVolunteersEventComponent forceVolunteersEvent)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].ForceVolunteersCompleted(winnerSide, forceVolunteersEvent);
		}
	}

	public override void OnHideoutBattleCompleted(BattleSideEnum winnerSide, HideoutEventComponent hideoutEventComponent, HideoutEventComponent.HideoutBattleEndState battleEndState)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnHideoutBattleCompleted(winnerSide, hideoutEventComponent, battleEndState);
		}
	}

	public override void OnClanDestroyed(Clan destroyedClan)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnClanDestroyed(destroyedClan);
		}
	}

	public override void OnNewItemCrafted(ItemObject itemObject, ItemModifier overriddenItemModifier, bool isCraftingOrderItem)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnNewItemCrafted(itemObject, overriddenItemModifier, isCraftingOrderItem);
		}
	}

	public override void OnWorkshopOwnerChanged(Workshop workshop, Hero oldOwner)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnWorkshopOwnerChanged(workshop, oldOwner);
		}
	}

	public override void OnWorkshopInitialized(Workshop workshop)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnWorkshopInitialized(workshop);
		}
	}

	public override void OnWorkshopTypeChanged(Workshop workshop)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnWorkshopTypeChanged(workshop);
		}
	}

	public override void OnMainPartyPrisonerRecruited(FlattenedTroopRoster roster)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnMainPartyPrisonerRecruited(roster);
		}
	}

	public override void OnPrisonerDonatedToSettlement(MobileParty donatingParty, FlattenedTroopRoster donatedPrisoners, Settlement donatedSettlement)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnPrisonerDonatedToSettlement(donatingParty, donatedPrisoners, donatedSettlement);
		}
	}

	public override void OnEquipmentSmeltedByHero(Hero hero, EquipmentElement equipmentElement)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnEquipmentSmeltedByHero(hero, equipmentElement);
		}
	}

	public override void OnPrisonerTaken(FlattenedTroopRoster roster)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnPrisonerTaken(roster);
		}
	}

	public override void OnBeforeSave()
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnBeforeSave();
		}
	}

	public override void OnSaveStarted()
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnSaveStarted();
		}
	}

	public override void OnSaveOver(bool isSuccessful, string saveName)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnSaveOver(isSuccessful, saveName);
		}
	}

	public override void CollectMetadataEntries(List<KeyValuePair<string, string>> pairs)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].CollectMetadataEntries(pairs);
		}
	}

	public override void OnPrisonerReleased(FlattenedTroopRoster roster)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnPrisonerReleased(roster);
		}
	}

	public override void OnHeroChangedClan(Hero hero, Clan oldClan)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnHeroChangedClan(hero, oldClan);
		}
	}

	public override void OnHeroGetsBusy(Hero hero, HeroGetsBusyReasons heroGetsBusyReason)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnHeroGetsBusy(hero, heroGetsBusyReason);
		}
	}

	public override void OnPlayerTradeProfit(int profit)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnPlayerTradeProfit(profit);
		}
	}

	public override void CraftingPartUnlocked(CraftingPiece craftingPiece)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].CraftingPartUnlocked(craftingPiece);
		}
	}

	public override void OnClanEarnedGoldFromTribute(Clan receiverClan, IFaction payingFaction)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnClanEarnedGoldFromTribute(receiverClan, payingFaction);
		}
	}

	public override void OnCollectLootItems(PartyBase winnerParty, ItemRoster gainedLoots)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnCollectLootItems(winnerParty, gainedLoots);
		}
	}

	public override void OnHeroTeleportationRequested(Hero hero, Settlement targetSettlement, MobileParty targetParty, TeleportHeroAction.TeleportationDetail detail)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnHeroTeleportationRequested(hero, targetSettlement, targetParty, detail);
		}
	}

	public override void OnClanInfluenceChanged(Clan clan, float change)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnClanInfluenceChanged(clan, change);
		}
	}

	public override void OnPlayerPartyKnockedOrKilledTroop(CharacterObject strikedTroop)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnPlayerPartyKnockedOrKilledTroop(strikedTroop);
		}
	}

	public override void OnPlayerEarnedGoldFromAsset(DefaultClanFinanceModel.AssetIncomeType incomeType, int incomeAmount)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnPlayerEarnedGoldFromAsset(incomeType, incomeAmount);
		}
	}

	public override void OnPartyLeaderChangeOfferCanceled(MobileParty party)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnPartyLeaderChangeOfferCanceled(party);
		}
	}

	public override void OnPartyLeaderChanged(MobileParty mobileParty, Hero oldLeader)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnPartyLeaderChanged(mobileParty, oldLeader);
		}
	}

	public override void OnMainPartyStarving()
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnMainPartyStarving();
		}
	}

	public override void OnPlayerJoinedTournament(Town town, bool isParticipant)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnPlayerJoinedTournament(town, isParticipant);
		}
	}

	public override void OnCraftingOrderCompleted(Town town, CraftingOrder craftingOrder, ItemObject craftedItem, Hero completerHero)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnCraftingOrderCompleted(town, craftingOrder, craftedItem, completerHero);
		}
	}

	public override void OnItemsRefined(Hero hero, Crafting.RefiningFormula refineFormula)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnItemsRefined(hero, refineFormula);
		}
	}

	public override void OnMapEventContinuityNeedsUpdate(IFaction faction)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnMapEventContinuityNeedsUpdate(faction);
		}
	}

	public override void OnHeirSelectionRequested(Dictionary<Hero, int> heirApparents)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnHeirSelectionRequested(heirApparents);
		}
	}

	public override void OnHeirSelectionOver(Hero selectedHeir)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnHeirSelectionOver(selectedHeir);
		}
	}

	public override void OnCharacterCreationInitialized(CharacterCreationManager characterCreationManager)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnCharacterCreationInitialized(characterCreationManager);
		}
	}

	public override void OnShipDestroyed(PartyBase owner, Ship ship, DestroyShipAction.ShipDestroyDetail detail)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnShipDestroyed(owner, ship, detail);
		}
	}

	public override void OnPartyLeftArmy(MobileParty party, Army army)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnPartyLeftArmy(party, army);
		}
	}

	public override void OnShipOwnerChanged(Ship ship, PartyBase oldOwner, ChangeShipOwnerAction.ShipOwnerChangeDetail changeDetail)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnShipOwnerChanged(ship, oldOwner, changeDetail);
		}
	}

	public override void OnShipRepaired(Ship ship, Settlement repairPort)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnShipRepaired(ship, repairPort);
		}
	}

	public override void OnFigureheadUnlocked(Figurehead figurehead)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnFigureheadUnlocked(figurehead);
		}
	}

	public override void OnPartyAddedToMapEvent(PartyBase party)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnPartyAddedToMapEvent(party);
		}
	}

	public override void OnIncidentResolved(Incident incident)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnIncidentResolved(incident);
		}
	}

	public override void OnMobilePartyNavigationStateChanged(MobileParty mobileParty)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnMobilePartyNavigationStateChanged(mobileParty);
		}
	}

	public override void OnMobilePartyJoinedToSiegeEvent(MobileParty mobileParty)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnMobilePartyJoinedToSiegeEvent(mobileParty);
		}
	}

	public override void OnMobilePartyLeftSiegeEvent(MobileParty mobileParty)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnMobilePartyLeftSiegeEvent(mobileParty);
		}
	}

	public override void OnBlockadeActivated(SiegeEvent siegeEvent)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnBlockadeActivated(siegeEvent);
		}
	}

	public override void OnBlockadeDeactivated(SiegeEvent siegeEvent)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnBlockadeDeactivated(siegeEvent);
		}
	}

	public override void OnMapMarkerCreated(MapMarker mapMarker)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnMapMarkerCreated(mapMarker);
		}
	}

	public override void OnMapMarkerRemoved(MapMarker mapMarker)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnMapMarkerRemoved(mapMarker);
		}
	}

	public override void OnMercenaryServiceStarted(Clan mercenaryClan, StartMercenaryServiceAction.StartMercenaryServiceActionDetails details)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnMercenaryServiceStarted(mercenaryClan, details);
		}
	}

	public override void OnMercenaryServiceEnded(Clan mercenaryClan, EndMercenaryServiceAction.EndMercenaryServiceActionDetails details)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnMercenaryServiceEnded(mercenaryClan, details);
		}
	}

	public override void OnAllianceStarted(Kingdom kingdom1, Kingdom kingdom2)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnAllianceStarted(kingdom1, kingdom2);
		}
	}

	public override void OnAllianceEnded(Kingdom kingdom1, Kingdom kingdom2)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnAllianceEnded(kingdom1, kingdom2);
		}
	}

	public override void OnCallToWarAgreementStarted(Kingdom callingKingdom, Kingdom calledKingdom, Kingdom kingdomToCallToWarAgainst)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnCallToWarAgreementStarted(callingKingdom, calledKingdom, kingdomToCallToWarAgainst);
		}
	}

	public override void OnCallToWarAgreementEnded(Kingdom callingKingdom, Kingdom calledKingdom, Kingdom kingdomToCallToWarAgainst)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnCallToWarAgreementEnded(callingKingdom, calledKingdom, kingdomToCallToWarAgainst);
		}
	}

	public override void CanHeroLeadParty(Hero hero, ref bool result)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].CanHeroLeadParty(hero, ref result);
			if (!result)
			{
				break;
			}
		}
	}

	public override void CanHeroMarry(Hero hero, ref bool result)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].CanHeroMarry(hero, ref result);
			if (!result)
			{
				break;
			}
		}
	}

	public override void CanHeroEquipmentBeChanged(Hero hero, ref bool result)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].CanHeroEquipmentBeChanged(hero, ref result);
			if (!result)
			{
				break;
			}
		}
	}

	public override void CanBeGovernorOrHavePartyRole(Hero hero, ref bool result)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].CanBeGovernorOrHavePartyRole(hero, ref result);
			if (!result)
			{
				break;
			}
		}
	}

	public override void CanHeroDie(Hero hero, KillCharacterAction.KillCharacterActionDetail causeOfDeath, ref bool result)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].CanHeroDie(hero, causeOfDeath, ref result);
			if (!result)
			{
				break;
			}
		}
	}

	public override void CanHeroBecomePrisoner(Hero hero, ref bool result)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].CanHeroBecomePrisoner(hero, ref result);
			if (!result)
			{
				break;
			}
		}
	}

	public override void CanPlayerMeetWithHeroAfterConversation(Hero hero, ref bool result)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].CanPlayerMeetWithHeroAfterConversation(hero, ref result);
			if (!result)
			{
				break;
			}
		}
	}

	public override void CanMoveToSettlement(Hero hero, ref bool result)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].CanMoveToSettlement(hero, ref result);
			if (!result)
			{
				break;
			}
		}
	}

	public override void CanHaveCampaignIssues(Hero hero, ref bool result)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].CanHaveCampaignIssues(hero, ref result);
			if (!result)
			{
				break;
			}
		}
	}

	public override void IsSettlementBusy(Settlement settlement, object asker, ref int priority)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].IsSettlementBusy(settlement, asker, ref priority);
		}
	}

	public override void OnHeroUnregistered(Hero hero)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnHeroUnregistered(hero);
		}
	}

	public override void OnShipCreated(Ship ship, Settlement createdSettlement)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnShipCreated(ship, createdSettlement);
		}
	}

	public override void OnConfigChanged()
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnConfigChanged();
		}
	}

	public override void OnMobilePartyRaftStateChanged(MobileParty mobileParty)
	{
		CampaignEventReceiver[] eventReceivers = _eventReceivers;
		for (int i = 0; i < eventReceivers.Length; i++)
		{
			eventReceivers[i].OnMobilePartyRaftStateChanged(mobileParty);
		}
	}
}
