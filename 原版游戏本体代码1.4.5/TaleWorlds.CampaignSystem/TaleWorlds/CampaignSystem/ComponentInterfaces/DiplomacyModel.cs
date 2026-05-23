using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.BarterSystem;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class DiplomacyModel : MBGameModel<DiplomacyModel>
{
	public enum DiplomacyStance
	{
		Neutral,
		War
	}

	public abstract int MaxRelationLimit { get; }

	public abstract int MinRelationLimit { get; }

	public abstract int MaxNeutralRelationLimit { get; }

	public abstract int MinNeutralRelationLimit { get; }

	public abstract int MinimumRelationWithConversationCharacterToJoinKingdom { get; }

	public abstract int GiftingTownRelationshipBonus { get; }

	public abstract int GiftingCastleRelationshipBonus { get; }

	public abstract float WarDeclarationScorePenaltyAgainstTradePartners { get; }

	public abstract float GetStrengthThresholdForNonMutualWarsToBeIgnoredToJoinKingdom(Kingdom kingdomToJoin);

	public abstract float GetRelationIncreaseFactor(Hero hero1, Hero hero2, float relationValue);

	public abstract int GetInfluenceAwardForSettlementCapturer(Settlement settlement);

	public abstract float GetHourlyInfluenceAwardForRaidingEnemyVillage(MobileParty mobileParty);

	public abstract float GetHourlyInfluenceAwardForBesiegingEnemyFortification(MobileParty mobileParty);

	public abstract float GetHourlyInfluenceAwardForBeingArmyMember(MobileParty mobileParty);

	public abstract float GetScoreOfClanToJoinKingdom(Clan clan, Kingdom kingdom);

	public abstract float GetScoreOfClanToLeaveKingdom(Clan clan, Kingdom kingdom);

	public abstract float GetScoreOfKingdomToGetClan(Kingdom kingdom, Clan clan);

	public abstract float GetScoreOfKingdomToSackClan(Kingdom kingdom, Clan clan);

	public abstract float GetScoreOfMercenaryToJoinKingdom(Clan clan, Kingdom kingdom);

	public abstract float GetScoreOfMercenaryToLeaveKingdom(Clan clan, Kingdom kingdom);

	public abstract float GetScoreOfKingdomToHireMercenary(Kingdom kingdom, Clan mercenaryClan);

	public abstract float GetScoreOfKingdomToSackMercenary(Kingdom kingdom, Clan mercenaryClan);

	public abstract float GetScoreOfDeclaringPeaceForClan(IFaction factionDeclaresPeace, IFaction factionDeclaredPeace, Clan evaluatingClan, out TextObject reason, bool includeReason = false);

	public abstract float GetScoreOfDeclaringPeace(IFaction factionDeclaresPeace, IFaction factionDeclaredPeace);

	public abstract bool IsPeaceSuitable(IFaction factionDeclaresPeace, IFaction factionDeclaredPeace);

	public abstract float GetScoreOfDeclaringWar(IFaction factionDeclaresWar, IFaction factionDeclaredWar, Clan evaluatingClan, out TextObject reason, bool includeReason = false);

	public abstract ExplainedNumber GetWarProgressScore(IFaction factionDeclaresWar, IFaction factionDeclaredWar, bool includeDescriptions = false);

	public abstract float GetScoreOfLettingPartyGo(MobileParty party, MobileParty partyToLetGo);

	public abstract float GetValueOfHeroForFaction(Hero examinedHero, IFaction targetFaction, bool forMarriage = false);

	public abstract int GetRelationCostOfExpellingClanFromKingdom();

	public abstract int GetInfluenceCostOfSupportingClan();

	public abstract int GetInfluenceCostOfExpellingClan(Clan proposingClan);

	public abstract int GetInfluenceCostOfProposingPeace(Clan proposingClan);

	public abstract int GetInfluenceCostOfProposingWar(Clan proposingClan);

	public abstract int GetInfluenceValueOfSupportingClan();

	public abstract int GetRelationValueOfSupportingClan();

	public abstract int GetInfluenceCostOfAnnexation(Clan proposingClan);

	public abstract int GetInfluenceCostOfChangingLeaderOfArmy();

	public abstract int GetInfluenceCostOfDisbandingArmy();

	public abstract int GetRelationCostOfDisbandingArmy(bool isLeaderParty);

	public abstract int GetInfluenceCostOfPolicyProposalAndDisavowal(Clan proposingClan);

	public abstract int GetInfluenceCostOfAbandoningArmy();

	public abstract int GetEffectiveRelation(Hero hero, Hero hero1);

	public abstract int GetBaseRelation(Hero hero, Hero hero1);

	public abstract void GetHeroesForEffectiveRelation(Hero hero1, Hero hero2, out Hero effectiveHero1, out Hero effectiveHero2);

	public abstract int GetRelationChangeAfterClanLeaderIsDead(Hero deadLeader, Hero relationHero);

	public abstract int GetRelationChangeAfterVotingInSettlementOwnerPreliminaryDecision(Hero supporter, bool hasHeroVotedAgainstOwner);

	public abstract float GetClanStrength(Clan clan);

	public abstract float GetHeroCommandingStrengthForClan(Hero hero);

	public abstract float GetHeroGoverningStrengthForClan(Hero hero);

	public abstract uint GetNotificationColor(ChatNotificationType notificationType);

	public abstract int GetDailyTributeToPay(Clan factionToPay, Clan factionToReceive, out int tributeDurationInDays);

	public abstract float GetDecisionMakingThreshold(IFaction consideringFaction);

	public abstract float GetValueOfSettlementsForFaction(IFaction faction);

	public abstract bool CanSettlementBeGifted(Settlement settlement);

	public abstract bool IsClanEligibleToBecomeRuler(Clan clan);

	public abstract IEnumerable<BarterGroup> GetBarterGroups();

	public abstract int GetCharmExperienceFromRelationGain(Hero hero, float relationChange, ChangeRelationAction.ChangeRelationDetail detail);

	public abstract float DenarsToInfluence();

	public abstract DiplomacyStance? GetShallowDiplomaticStance(IFaction faction1, IFaction faction2);

	public abstract DiplomacyStance GetDefaultDiplomaticStance(IFaction faction1, IFaction faction2);

	public abstract bool IsAtConstantWar(IFaction faction1, IFaction faction2);
}
