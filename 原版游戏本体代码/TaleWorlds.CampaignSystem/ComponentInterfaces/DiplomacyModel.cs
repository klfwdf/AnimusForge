using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.BarterSystem;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001A6 RID: 422
	public abstract class DiplomacyModel : MBGameModel<DiplomacyModel>
	{
		// Token: 0x17000719 RID: 1817
		// (get) Token: 0x06001C92 RID: 7314
		public abstract int MaxRelationLimit { get; }

		// Token: 0x1700071A RID: 1818
		// (get) Token: 0x06001C93 RID: 7315
		public abstract int MinRelationLimit { get; }

		// Token: 0x1700071B RID: 1819
		// (get) Token: 0x06001C94 RID: 7316
		public abstract int MaxNeutralRelationLimit { get; }

		// Token: 0x1700071C RID: 1820
		// (get) Token: 0x06001C95 RID: 7317
		public abstract int MinNeutralRelationLimit { get; }

		// Token: 0x1700071D RID: 1821
		// (get) Token: 0x06001C96 RID: 7318
		public abstract int MinimumRelationWithConversationCharacterToJoinKingdom { get; }

		// Token: 0x1700071E RID: 1822
		// (get) Token: 0x06001C97 RID: 7319
		public abstract int GiftingTownRelationshipBonus { get; }

		// Token: 0x1700071F RID: 1823
		// (get) Token: 0x06001C98 RID: 7320
		public abstract int GiftingCastleRelationshipBonus { get; }

		// Token: 0x17000720 RID: 1824
		// (get) Token: 0x06001C99 RID: 7321
		public abstract float WarDeclarationScorePenaltyAgainstAllies { get; }

		// Token: 0x17000721 RID: 1825
		// (get) Token: 0x06001C9A RID: 7322
		public abstract float WarDeclarationScoreBonusAgainstEnemiesOfAllies { get; }

		// Token: 0x06001C9B RID: 7323
		public abstract float GetStrengthThresholdForNonMutualWarsToBeIgnoredToJoinKingdom(Kingdom kingdomToJoin);

		// Token: 0x06001C9C RID: 7324
		public abstract float GetRelationIncreaseFactor(Hero hero1, Hero hero2, float relationValue);

		// Token: 0x06001C9D RID: 7325
		public abstract int GetInfluenceAwardForSettlementCapturer(Settlement settlement);

		// Token: 0x06001C9E RID: 7326
		public abstract float GetHourlyInfluenceAwardForRaidingEnemyVillage(MobileParty mobileParty);

		// Token: 0x06001C9F RID: 7327
		public abstract float GetHourlyInfluenceAwardForBesiegingEnemyFortification(MobileParty mobileParty);

		// Token: 0x06001CA0 RID: 7328
		public abstract float GetHourlyInfluenceAwardForBeingArmyMember(MobileParty mobileParty);

		// Token: 0x06001CA1 RID: 7329
		public abstract float GetScoreOfClanToJoinKingdom(Clan clan, Kingdom kingdom);

		// Token: 0x06001CA2 RID: 7330
		public abstract float GetScoreOfClanToLeaveKingdom(Clan clan, Kingdom kingdom);

		// Token: 0x06001CA3 RID: 7331
		public abstract float GetScoreOfKingdomToGetClan(Kingdom kingdom, Clan clan);

		// Token: 0x06001CA4 RID: 7332
		public abstract float GetScoreOfKingdomToSackClan(Kingdom kingdom, Clan clan);

		// Token: 0x06001CA5 RID: 7333
		public abstract float GetScoreOfMercenaryToJoinKingdom(Clan clan, Kingdom kingdom);

		// Token: 0x06001CA6 RID: 7334
		public abstract float GetScoreOfMercenaryToLeaveKingdom(Clan clan, Kingdom kingdom);

		// Token: 0x06001CA7 RID: 7335
		public abstract float GetScoreOfKingdomToHireMercenary(Kingdom kingdom, Clan mercenaryClan);

		// Token: 0x06001CA8 RID: 7336
		public abstract float GetScoreOfKingdomToSackMercenary(Kingdom kingdom, Clan mercenaryClan);

		// Token: 0x06001CA9 RID: 7337
		public abstract float GetScoreOfDeclaringPeaceForClan(IFaction factionDeclaresPeace, IFaction factionDeclaredPeace, Clan evaluatingClan, out TextObject reason, bool includeReason = false);

		// Token: 0x06001CAA RID: 7338
		public abstract float GetScoreOfDeclaringPeace(IFaction factionDeclaresPeace, IFaction factionDeclaredPeace);

		// Token: 0x06001CAB RID: 7339
		public abstract bool IsPeaceSuitable(IFaction factionDeclaresPeace, IFaction factionDeclaredPeace);

		// Token: 0x06001CAC RID: 7340
		public abstract float GetScoreOfDeclaringWar(IFaction factionDeclaresWar, IFaction factionDeclaredWar, Clan evaluatingClan, out TextObject reason, bool includeReason = false);

		// Token: 0x06001CAD RID: 7341
		public abstract ExplainedNumber GetWarProgressScore(IFaction factionDeclaresWar, IFaction factionDeclaredWar, bool includeDescriptions = false);

		// Token: 0x06001CAE RID: 7342
		public abstract float GetScoreOfLettingPartyGo(MobileParty party, MobileParty partyToLetGo);

		// Token: 0x06001CAF RID: 7343
		public abstract float GetValueOfHeroForFaction(Hero examinedHero, IFaction targetFaction, bool forMarriage = false);

		// Token: 0x06001CB0 RID: 7344
		public abstract int GetRelationCostOfExpellingClanFromKingdom();

		// Token: 0x06001CB1 RID: 7345
		public abstract int GetInfluenceCostOfSupportingClan();

		// Token: 0x06001CB2 RID: 7346
		public abstract int GetInfluenceCostOfExpellingClan(Clan proposingClan);

		// Token: 0x06001CB3 RID: 7347
		public abstract int GetInfluenceCostOfProposingPeace(Clan proposingClan);

		// Token: 0x06001CB4 RID: 7348
		public abstract int GetInfluenceCostOfProposingWar(Clan proposingClan);

		// Token: 0x06001CB5 RID: 7349
		public abstract int GetInfluenceValueOfSupportingClan();

		// Token: 0x06001CB6 RID: 7350
		public abstract int GetRelationValueOfSupportingClan();

		// Token: 0x06001CB7 RID: 7351
		public abstract int GetInfluenceCostOfAnnexation(Clan proposingClan);

		// Token: 0x06001CB8 RID: 7352
		public abstract int GetInfluenceCostOfChangingLeaderOfArmy();

		// Token: 0x06001CB9 RID: 7353
		public abstract int GetInfluenceCostOfDisbandingArmy();

		// Token: 0x06001CBA RID: 7354
		public abstract int GetRelationCostOfDisbandingArmy(bool isLeaderParty);

		// Token: 0x06001CBB RID: 7355
		public abstract int GetInfluenceCostOfPolicyProposalAndDisavowal(Clan proposingClan);

		// Token: 0x06001CBC RID: 7356
		public abstract int GetInfluenceCostOfAbandoningArmy();

		// Token: 0x06001CBD RID: 7357
		public abstract int GetEffectiveRelation(Hero hero, Hero hero1);

		// Token: 0x06001CBE RID: 7358
		public abstract int GetBaseRelation(Hero hero, Hero hero1);

		// Token: 0x06001CBF RID: 7359
		public abstract void GetHeroesForEffectiveRelation(Hero hero1, Hero hero2, out Hero effectiveHero1, out Hero effectiveHero2);

		// Token: 0x06001CC0 RID: 7360
		public abstract int GetRelationChangeAfterClanLeaderIsDead(Hero deadLeader, Hero relationHero);

		// Token: 0x06001CC1 RID: 7361
		public abstract int GetRelationChangeAfterVotingInSettlementOwnerPreliminaryDecision(Hero supporter, bool hasHeroVotedAgainstOwner);

		// Token: 0x06001CC2 RID: 7362
		public abstract float GetClanStrength(Clan clan);

		// Token: 0x06001CC3 RID: 7363
		public abstract float GetHeroCommandingStrengthForClan(Hero hero);

		// Token: 0x06001CC4 RID: 7364
		public abstract float GetHeroGoverningStrengthForClan(Hero hero);

		// Token: 0x06001CC5 RID: 7365
		public abstract uint GetNotificationColor(ChatNotificationType notificationType);

		// Token: 0x06001CC6 RID: 7366
		public abstract int GetDailyTributeToPay(Clan factionToPay, Clan factionToReceive, out int tributeDurationInDays);

		// Token: 0x06001CC7 RID: 7367
		public abstract float GetDecisionMakingThreshold(IFaction consideringFaction);

		// Token: 0x06001CC8 RID: 7368
		public abstract float GetValueOfSettlementsForFaction(IFaction faction);

		// Token: 0x06001CC9 RID: 7369
		public abstract bool CanSettlementBeGifted(Settlement settlement);

		// Token: 0x06001CCA RID: 7370
		public abstract bool IsClanEligibleToBecomeRuler(Clan clan);

		// Token: 0x06001CCB RID: 7371
		public abstract IEnumerable<BarterGroup> GetBarterGroups();

		// Token: 0x06001CCC RID: 7372
		public abstract int GetCharmExperienceFromRelationGain(Hero hero, float relationChange, ChangeRelationAction.ChangeRelationDetail detail);

		// Token: 0x06001CCD RID: 7373
		public abstract float DenarsToInfluence();

		// Token: 0x06001CCE RID: 7374
		public abstract DiplomacyModel.DiplomacyStance? GetShallowDiplomaticStance(IFaction faction1, IFaction faction2);

		// Token: 0x06001CCF RID: 7375
		public abstract DiplomacyModel.DiplomacyStance GetDefaultDiplomaticStance(IFaction faction1, IFaction faction2);

		// Token: 0x06001CD0 RID: 7376
		public abstract bool IsAtConstantWar(IFaction faction1, IFaction faction2);

		// Token: 0x020005F3 RID: 1523
		public enum DiplomacyStance
		{
			// Token: 0x0400189B RID: 6299
			Neutral,
			// Token: 0x0400189C RID: 6300
			War
		}
	}
}
