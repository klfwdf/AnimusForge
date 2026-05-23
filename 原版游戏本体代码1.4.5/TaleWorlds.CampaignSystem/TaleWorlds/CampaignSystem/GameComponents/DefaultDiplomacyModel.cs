using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.BarterSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultDiplomacyModel : DiplomacyModel
{
	private struct WarStats
	{
		public float Strength;

		public float ValueOfSettlements;

		public float TotalStrengthOfEnemies;
	}

	private const int DailyValueFactorForTributes = 70;

	private const float ProsperityValueFactor = 50f;

	private const float StrengthFactor = 50f;

	private const float DenarsToInfluenceValue = 0.002f;

	private const float RulingClanToJoinOtherKingdomScore = -100000000f;

	private const float MinStrengthRequiredForFactionToConsiderWar = 500f;

	private const int MinWarPartyRequiredToConsiderWar = 2;

	private const float ClanRichnessEffectMultiplier = 0.15f;

	private const float MaxBenefitValue = 10000000f;

	private const float MeaningfulBenefitValue = 2000000f;

	private const float MinBenefitValue = 10000f;

	private const float DefaultRelationMultiplierForScoreOfWar = -250f;

	private const float SameCultureTownMultiplier = 0.3f;

	private const float MaxAcceptableProsperityValue = 100000f;

	public override int MinimumRelationWithConversationCharacterToJoinKingdom => -10;

	public override int GiftingTownRelationshipBonus => 20;

	public override int GiftingCastleRelationshipBonus => 10;

	public override int MaxRelationLimit => 100;

	public override int MinRelationLimit => -100;

	public override int MaxNeutralRelationLimit => 50;

	public override int MinNeutralRelationLimit => -50;

	public override float WarDeclarationScorePenaltyAgainstTradePartners => 0.7f;

	public override float GetStrengthThresholdForNonMutualWarsToBeIgnoredToJoinKingdom(Kingdom kingdomToJoin)
	{
		return kingdomToJoin.CurrentTotalStrength * 0.05f;
	}

	public override float GetClanStrength(Clan clan)
	{
		float num = 0f;
		foreach (Hero hero in clan.Heroes)
		{
			num += GetHeroCommandingStrengthForClan(hero);
		}
		float num2 = clan.Influence * 1.2f;
		float num3 = (float)clan.Settlements.Count * 4f;
		return num + num2 + num3;
	}

	public override float GetHeroCommandingStrengthForClan(Hero hero)
	{
		if (hero.IsAlive)
		{
			float num = (float)hero.GetSkillValue(DefaultSkills.Tactics) * 1f;
			float num2 = (float)hero.GetSkillValue(DefaultSkills.Steward) * 1f;
			float num3 = (float)hero.GetSkillValue(DefaultSkills.Trade) * 1f;
			float num4 = (float)hero.GetSkillValue(DefaultSkills.Leadership) * 1f;
			int num5 = ((hero.GetTraitLevel(DefaultTraits.Commander) > 0) ? 300 : 0);
			float num6 = (float)hero.Gold * 0.1f;
			float num7 = ((hero.PartyBelongedTo != null) ? (5f * hero.PartyBelongedTo.Party.CalculateCurrentStrength()) : 0f);
			float num8 = 0f;
			if (hero.Clan.Leader == hero)
			{
				num8 += 500f;
			}
			float num9 = 0f;
			if (hero.Father == hero.Clan.Leader || hero.Clan.Leader.Father == hero || hero.Mother == hero.Clan.Leader || hero.Clan.Leader.Mother == hero)
			{
				num9 += 100f;
			}
			float num10 = 0f;
			if (hero.IsNoncombatant)
			{
				num10 -= 250f;
			}
			float num11 = 0f;
			if (hero.GovernorOf != null)
			{
				num11 -= 250f;
			}
			float num12 = (float)num5 + num + num2 + num3 + num4 + num6 + num7 + num8 + num9 + num10 + num11;
			if (!(num12 > 0f))
			{
				return 0f;
			}
			return num12;
		}
		return 0f;
	}

	public override float GetHeroGoverningStrengthForClan(Hero hero)
	{
		if (hero.IsAlive)
		{
			float num = (float)hero.GetSkillValue(DefaultSkills.Tactics) * 0.3f;
			float num2 = (float)hero.GetSkillValue(DefaultSkills.Charm) * 0.9f;
			float num3 = (float)hero.GetSkillValue(DefaultSkills.Engineering) * 0.8f;
			float num4 = (float)hero.GetSkillValue(DefaultSkills.Steward) * 2f;
			float num5 = (float)hero.GetSkillValue(DefaultSkills.Trade) * 1.2f;
			float num6 = (float)hero.GetSkillValue(DefaultSkills.Leadership) * 1f;
			int num7 = ((hero.GetTraitLevel(DefaultTraits.Honor) > 0) ? 100 : 0);
			float num8 = (float)TaleWorlds.Library.MathF.Min(100000, hero.Gold) * 0.005f;
			float num9 = 0f;
			if (hero.Spouse == hero.Clan.Leader)
			{
				num9 += 1000f;
			}
			if (hero.Father == hero.Clan.Leader || hero.Clan.Leader.Father == hero || hero.Mother == hero.Clan.Leader || hero.Clan.Leader.Mother == hero)
			{
				num9 += 750f;
			}
			if (hero.Siblings.Contains(hero.Clan.Leader))
			{
				num9 += 500f;
			}
			return (float)num7 + num + num4 + num5 + num6 + num8 + num9 + num2 + num3;
		}
		return 0f;
	}

	public override float GetRelationIncreaseFactor(Hero hero1, Hero hero2, float relationChange)
	{
		ExplainedNumber explainedNumber = new ExplainedNumber(relationChange);
		Hero hero3 = ((!hero1.IsHumanPlayerCharacter && !hero2.IsHumanPlayerCharacter) ? ((MBRandom.RandomFloat < 0.5f) ? hero1 : hero2) : (hero1.IsHumanPlayerCharacter ? hero1 : hero2));
		SkillHelper.AddSkillBonusForCharacter(DefaultSkillEffects.CharmRelationBonus, hero3.CharacterObject, ref explainedNumber);
		if (hero1.IsFemale != hero2.IsFemale)
		{
			if (hero3.GetPerkValue(DefaultPerks.Charm.InBloom))
			{
				explainedNumber.AddFactor(DefaultPerks.Charm.InBloom.PrimaryBonus);
			}
		}
		else if (hero3.GetPerkValue(DefaultPerks.Charm.YoungAndRespectful))
		{
			explainedNumber.AddFactor(DefaultPerks.Charm.YoungAndRespectful.PrimaryBonus);
		}
		if (hero3.GetPerkValue(DefaultPerks.Charm.GoodNatured) && hero2.GetTraitLevel(DefaultTraits.Mercy) > 0)
		{
			explainedNumber.Add(DefaultPerks.Charm.GoodNatured.SecondaryBonus, DefaultPerks.Charm.GoodNatured.Name);
		}
		if (hero3.GetPerkValue(DefaultPerks.Charm.Tribute) && hero2.GetTraitLevel(DefaultTraits.Mercy) < 0)
		{
			explainedNumber.Add(DefaultPerks.Charm.Tribute.SecondaryBonus, DefaultPerks.Charm.Tribute.Name);
		}
		return explainedNumber.ResultNumber;
	}

	public override int GetInfluenceAwardForSettlementCapturer(Settlement settlement)
	{
		if (settlement.IsTown || settlement.IsCastle)
		{
			int num = (settlement.IsTown ? 30 : 10);
			int num2 = 0;
			foreach (Village boundVillage in settlement.BoundVillages)
			{
				num2 += GetInfluenceAwardForSettlementCapturer(boundVillage.Settlement);
			}
			return num + num2;
		}
		return 10;
	}

	public override float GetHourlyInfluenceAwardForBeingArmyMember(MobileParty mobileParty)
	{
		float num = mobileParty.Party.CalculateCurrentStrength();
		float num2 = 0.0001f * (20f + num);
		if (mobileParty.BesiegedSettlement != null || mobileParty.MapEvent != null)
		{
			num2 *= 2f;
		}
		return num2;
	}

	public override float GetHourlyInfluenceAwardForRaidingEnemyVillage(MobileParty mobileParty)
	{
		int num = 0;
		foreach (MapEventParty party in mobileParty.MapEvent.AttackerSide.Parties)
		{
			if (party.Party.MobileParty == mobileParty || (party.Party.MobileParty?.Army != null && party.Party.MobileParty.Army.LeaderParty == mobileParty))
			{
				num += party.Party.MemberRoster.TotalManCount;
			}
		}
		return (TaleWorlds.Library.MathF.Sqrt(num) + 2f) / 240f;
	}

	public override float GetHourlyInfluenceAwardForBesiegingEnemyFortification(MobileParty mobileParty)
	{
		int num = 0;
		foreach (PartyBase item in mobileParty.BesiegedSettlement.SiegeEvent.GetSiegeEventSide(BattleSideEnum.Attacker).GetInvolvedPartiesForEventType())
		{
			if (item.MobileParty == mobileParty || (item.MobileParty.Army != null && item.MobileParty.Army.LeaderParty == mobileParty))
			{
				num += item.MemberRoster.TotalManCount;
			}
		}
		return (TaleWorlds.Library.MathF.Sqrt(num) + 2f) / 240f;
	}

	public override float GetScoreOfClanToJoinKingdom(Clan clan, Kingdom kingdom)
	{
		if (clan.Kingdom != null && clan.Kingdom.RulingClan == clan)
		{
			return -100000000f;
		}
		int relationBetweenClans = FactionManager.GetRelationBetweenClans(kingdom.RulingClan, clan);
		int num = 0;
		int num2 = 0;
		foreach (Clan clan2 in kingdom.Clans)
		{
			int relationBetweenClans2 = FactionManager.GetRelationBetweenClans(clan, clan2);
			num += relationBetweenClans2;
			num2++;
		}
		float num3 = ((num2 > 0) ? ((float)num / (float)num2) : 0f);
		float num4 = TaleWorlds.Library.MathF.Max(-100f, TaleWorlds.Library.MathF.Min(100f, (float)relationBetweenClans + num3));
		float num5 = TaleWorlds.Library.MathF.Min(2f, TaleWorlds.Library.MathF.Max(0.33f, 1f + TaleWorlds.Library.MathF.Sqrt(TaleWorlds.Library.MathF.Abs(num4)) * ((num4 < 0f) ? (-0.067f) : 0.1f)));
		float num6 = 1f;
		if (kingdom.Culture == clan.Culture)
		{
			num6 += 0.15f;
		}
		else if (kingdom.Leader != Hero.MainHero)
		{
			num6 -= 0.15f;
		}
		float num7 = clan.CalculateTotalSettlementBaseValue();
		float num8 = clan.CalculateTotalSettlementValueForFaction(kingdom);
		int warPartyLimit = clan.WarPartyLimit;
		float num9 = 0f;
		float num10 = 0f;
		if (!clan.IsMinorFaction)
		{
			float num11 = 0f;
			foreach (Town fief in kingdom.Fiefs)
			{
				num11 += fief.Settlement.GetSettlementValueForFaction(kingdom);
			}
			int num12 = 0;
			foreach (Clan clan3 in kingdom.Clans)
			{
				if (!clan3.IsUnderMercenaryService && clan3 != clan)
				{
					num12 += clan3.WarPartyLimit;
				}
			}
			num9 = num11 / (float)(num12 + warPartyLimit);
			num10 = 0f - (float)(num12 * num12) * 100f + 10000f;
		}
		float num13 = num9 * TaleWorlds.Library.MathF.Sqrt(warPartyLimit) * 0.15f * 0.2f;
		num13 *= num5 * num6;
		num13 += (clan.MapFaction.IsAtWarWith(kingdom) ? (num8 - num7) : 0f);
		num13 += num10;
		if (clan.Kingdom != null && clan.Kingdom.Leader == Hero.MainHero && num13 > 0f)
		{
			num13 *= 0.2f;
		}
		return num13;
	}

	public override float GetScoreOfClanToLeaveKingdom(Clan clan, Kingdom kingdom)
	{
		int relationBetweenClans = FactionManager.GetRelationBetweenClans(kingdom.RulingClan, clan);
		int num = 0;
		int num2 = 0;
		foreach (Clan clan2 in kingdom.Clans)
		{
			int relationBetweenClans2 = FactionManager.GetRelationBetweenClans(clan, clan2);
			num += relationBetweenClans2;
			num2++;
		}
		float num3 = ((num2 > 0) ? ((float)num / (float)num2) : 0f);
		float num4 = TaleWorlds.Library.MathF.Max(-100f, TaleWorlds.Library.MathF.Min(100f, (float)relationBetweenClans + num3));
		float num5 = TaleWorlds.Library.MathF.Min(2f, TaleWorlds.Library.MathF.Max(0.33f, 1f + TaleWorlds.Library.MathF.Sqrt(TaleWorlds.Library.MathF.Abs(num4)) * ((num4 < 0f) ? (-0.067f) : 0.1f)));
		float num6 = 1f + ((kingdom.Culture == clan.Culture) ? 0.15f : ((kingdom.Leader == Hero.MainHero) ? 0f : (-0.15f)));
		float num7 = clan.CalculateTotalSettlementBaseValue();
		float num8 = clan.CalculateTotalSettlementValueForFaction(kingdom);
		int warPartyLimit = clan.WarPartyLimit;
		float num9 = 0f;
		if (!clan.IsMinorFaction)
		{
			float num10 = 0f;
			foreach (Town fief in kingdom.Fiefs)
			{
				num10 += fief.Settlement.GetSettlementValueForFaction(kingdom);
			}
			int num11 = 0;
			foreach (Clan clan3 in kingdom.Clans)
			{
				if (!clan3.IsUnderMercenaryService && clan3 != clan)
				{
					num11 += clan3.WarPartyLimit;
				}
			}
			num9 = num10 / (float)(num11 + warPartyLimit);
		}
		float num12 = HeroHelper.CalculateReliabilityConstant(clan.Leader);
		float b = (float)(CampaignTime.Now - clan.LastFactionChangeTime).ToDays;
		float num13 = 4000f * (15f - TaleWorlds.Library.MathF.Sqrt(TaleWorlds.Library.MathF.Min(225f, b)));
		int num14 = 0;
		int num15 = 0;
		foreach (Town fief2 in clan.Fiefs)
		{
			if (fief2.IsCastle)
			{
				num15++;
			}
			else
			{
				num14++;
			}
		}
		float num16 = -70000f - (float)num15 * 10000f - (float)num14 * 30000f;
		num16 /= 0.15f;
		float num17 = (0f - num9) * TaleWorlds.Library.MathF.Sqrt(warPartyLimit) * 0.15f * 0.2f + num16 * num12 + (0f - num13);
		num17 *= num5 * num6;
		num17 = ((!(num5 < 1f) || !(num7 - num8 < 0f)) ? (num17 + (num7 - num8)) : (num17 + num5 * (num7 - num8)));
		if (num5 < 1f)
		{
			num17 += (1f - num5) * 200000f;
		}
		if (kingdom.Leader == Hero.MainHero)
		{
			num17 = ((!(num17 > 0f)) ? (num17 * 5f) : (num17 * 0.2f));
		}
		return num17 + ((kingdom.Leader == Hero.MainHero) ? (0f - 1000000f * num5) : 0f);
	}

	public override float GetScoreOfKingdomToGetClan(Kingdom kingdom, Clan clan)
	{
		float num = TaleWorlds.Library.MathF.Min(2f, TaleWorlds.Library.MathF.Max(0.33f, 1f + 0.02f * (float)FactionManager.GetRelationBetweenClans(kingdom.RulingClan, clan)));
		float num2 = 1f + ((kingdom.Culture == clan.Culture) ? 1f : 0f);
		int warPartyLimit = clan.WarPartyLimit;
		float num3 = (clan.CurrentTotalStrength + 150f * (float)warPartyLimit) * 20f;
		float powerRatioToEnemies = FactionHelper.GetPowerRatioToEnemies(kingdom);
		float num4 = HeroHelper.CalculateReliabilityConstant(clan.Leader);
		float num5 = 1f / TaleWorlds.Library.MathF.Max(0.4f, TaleWorlds.Library.MathF.Min(2.5f, TaleWorlds.Library.MathF.Sqrt(powerRatioToEnemies)));
		num3 *= num5;
		return (clan.CalculateTotalSettlementValueForFaction(kingdom) * 0.1f + num3) * num * num2 * num4;
	}

	public override float GetScoreOfKingdomToSackClan(Kingdom kingdom, Clan clan)
	{
		float num = TaleWorlds.Library.MathF.Min(2f, TaleWorlds.Library.MathF.Max(0.33f, 1f + 0.02f * (float)FactionManager.GetRelationBetweenClans(kingdom.RulingClan, clan)));
		float num2 = 1f + ((kingdom.Culture == clan.Culture) ? 1f : 0.5f);
		int warPartyLimit = clan.WarPartyLimit;
		float num3 = (clan.CurrentTotalStrength + 150f * (float)warPartyLimit) * 20f;
		float num4 = clan.CalculateTotalSettlementValueForFaction(kingdom);
		return 10f - 1f * num3 * num2 * num - num4;
	}

	public override float GetScoreOfMercenaryToJoinKingdom(Clan mercenaryClan, Kingdom kingdom)
	{
		int num = ((mercenaryClan.Kingdom == kingdom) ? mercenaryClan.MercenaryAwardMultiplier : Campaign.Current.Models.MinorFactionsModel.GetMercenaryAwardFactorToJoinKingdom(mercenaryClan, kingdom));
		float num2 = mercenaryClan.CurrentTotalStrength + (float)mercenaryClan.WarPartyLimit * 50f;
		int mercenaryAwardFactorToJoinKingdom = Campaign.Current.Models.MinorFactionsModel.GetMercenaryAwardFactorToJoinKingdom(mercenaryClan, kingdom, neededAmountForClanToJoinCalculation: true);
		if (kingdom.Leader == Hero.MainHero)
		{
			return 0f;
		}
		return (float)(num - mercenaryAwardFactorToJoinKingdom) * num2 * 0.5f;
	}

	public override float GetScoreOfMercenaryToLeaveKingdom(Clan mercenaryClan, Kingdom kingdom)
	{
		float num = 0.005f * TaleWorlds.Library.MathF.Min(200f, mercenaryClan.LastFactionChangeTime.ElapsedDaysUntilNow);
		return 10000f * num - 5000f - GetScoreOfMercenaryToJoinKingdom(mercenaryClan, kingdom);
	}

	public override float GetScoreOfKingdomToHireMercenary(Kingdom kingdom, Clan mercenaryClan)
	{
		int num = 0;
		foreach (Clan clan in kingdom.Clans)
		{
			num += clan.WarPartyLimit;
		}
		int num2 = ((num < 12) ? ((12 - num) * 100) : 0);
		int count = kingdom.Settlements.Count;
		int num3 = ((count < 40) ? ((40 - count) * 30) : 0);
		return num2 + num3;
	}

	public override float GetScoreOfKingdomToSackMercenary(Kingdom kingdom, Clan mercenaryClan)
	{
		float b = (((float)kingdom.Leader.Gold > 20000f) ? (TaleWorlds.Library.MathF.Sqrt((float)kingdom.Leader.Gold / 20000f) - 1f) : (-1f));
		int relationBetweenClans = FactionManager.GetRelationBetweenClans(kingdom.RulingClan, mercenaryClan);
		float num = TaleWorlds.Library.MathF.Min(5f, FactionHelper.GetPowerRatioToEnemies(kingdom));
		return (TaleWorlds.Library.MathF.Min(2f + (float)relationBetweenClans / 100f - num, b) * -1f - 0.1f) * 50f * mercenaryClan.CurrentTotalStrength * 5f;
	}

	public override float GetScoreOfDeclaringPeaceForClan(IFaction factionDeclaresPeace, IFaction factionDeclaredPeace, Clan evaluatingClan, out TextObject reason, bool includeReason = false)
	{
		reason = null;
		if (includeReason)
		{
			reason = GetReasonForDeclaringPeace(factionDeclaresPeace, factionDeclaredPeace, evaluatingClan);
		}
		float exposureScoreToOtherFaction = GetExposureScoreToOtherFaction(factionDeclaresPeace, factionDeclaredPeace);
		if (exposureScoreToOtherFaction.ApproximatelyEqualsTo(float.MinValue))
		{
			return 10000000f;
		}
		exposureScoreToOtherFaction = TaleWorlds.Library.MathF.Min(exposureScoreToOtherFaction * 1.4f, GetExposureScoreToOtherFaction(factionDeclaredPeace, factionDeclaresPeace));
		GetBenefitAndRiskScoreForPeace(factionDeclaresPeace, factionDeclaredPeace, evaluatingClan, out var benefitScore, out var riskScore);
		UpdateOurBenefitMinusOurRiskBasedOnEvaluatingFaction(evaluatingClan, ref benefitScore, ref riskScore);
		riskScore = ApplyWarProgressToRiskScore(factionDeclaresPeace, factionDeclaredPeace, riskScore);
		benefitScore *= GetWarScale(factionDeclaresPeace, factionDeclaredPeace);
		float relationScore = GetRelationScore(factionDeclaresPeace, factionDeclaredPeace, evaluatingClan);
		float sameCultureTownScore = GetSameCultureTownScore(factionDeclaresPeace, factionDeclaredPeace);
		float allianceFactorForDeclaringPeace = Campaign.Current.Models.AllianceModel.GetAllianceFactorForDeclaringPeace(factionDeclaresPeace, factionDeclaredPeace);
		return (sameCultureTownScore + benefitScore * exposureScoreToOtherFaction - riskScore * allianceFactorForDeclaringPeace + relationScore) * -1f;
	}

	public override float GetScoreOfDeclaringPeace(IFaction factionDeclaresPeace, IFaction factionDeclaredPeace)
	{
		float exposureScoreToOtherFaction = GetExposureScoreToOtherFaction(factionDeclaresPeace, factionDeclaredPeace);
		if (exposureScoreToOtherFaction.ApproximatelyEqualsTo(float.MinValue))
		{
			return 10000000f;
		}
		exposureScoreToOtherFaction = TaleWorlds.Library.MathF.Min(exposureScoreToOtherFaction * 1.4f, GetExposureScoreToOtherFaction(factionDeclaredPeace, factionDeclaresPeace));
		GetBenefitAndRiskScoreForPeace(factionDeclaresPeace, factionDeclaredPeace, factionDeclaresPeace.Leader.Clan, out var benefitScore, out var riskScore);
		riskScore = ApplyWarProgressToRiskScore(factionDeclaresPeace, factionDeclaredPeace, riskScore);
		benefitScore *= GetWarScale(factionDeclaresPeace, factionDeclaredPeace);
		float allianceFactorForDeclaringPeace = Campaign.Current.Models.AllianceModel.GetAllianceFactorForDeclaringPeace(factionDeclaresPeace, factionDeclaredPeace);
		return (GetSameCultureTownScore(factionDeclaresPeace, factionDeclaredPeace) + benefitScore * exposureScoreToOtherFaction - riskScore * allianceFactorForDeclaringPeace) * -1f;
	}

	private TextObject GetReasonForDeclaringPeace(IFaction factionDeclaresPeace, IFaction factionDeclaredPeace, Clan evaluatingClan)
	{
		if (GetExposureScoreToOtherFaction(factionDeclaresPeace, factionDeclaredPeace).ApproximatelyEqualsTo(float.MinValue))
		{
			return new TextObject("{=i0h0LKa0}Our borders are far from those of the enemy. It is too arduous to pursue this war.");
		}
		WarStats warStats = CalculateWarStatsForPeace(factionDeclaresPeace, factionDeclaredPeace, evaluatingClan);
		WarStats warStats2 = CalculateWarStatsForPeace(factionDeclaredPeace, factionDeclaresPeace, evaluatingClan);
		GetBenefitAndRiskScoreForPeace(factionDeclaresPeace, factionDeclaredPeace, evaluatingClan, out var benefitScore, out var riskScore);
		float num = ApplyWarProgressToRiskScore(factionDeclaresPeace, factionDeclaredPeace, riskScore);
		TextObject textObject = ((benefitScore - riskScore > 0f) ? ((!(benefitScore - num < 0f)) ? new TextObject("{=vwjs6EjJ}On balance, the gains we stand to make are not worth the costs and risks.") : new TextObject("{=QQtJobYP}We need time to recover from the hardships of war.")) : ((warStats.Strength < warStats2.Strength) ? new TextObject("{=JOe3BC41}The {ENEMY_KINGDOM_INFORMAL_NAME} is currently more powerful than us. We need time to build up our strength.") : ((warStats.Strength > warStats2.Strength && warStats.Strength < warStats2.Strength + warStats.TotalStrengthOfEnemies) ? new TextObject("{=vwjs6EjJ}On balance, the gains we stand to make are not worth the costs and risks.") : ((!(warStats.Strength < warStats2.Strength + warStats.TotalStrengthOfEnemies)) ? new TextObject("{=HqJSNG3M}Our realm is currently doing well, but we stand to lose this wealth if we go on fighting.") : new TextObject("{=nuqv4GAA}We have too many enemies. We need to make peace with at least some of them.")))));
		if (!TextObject.IsNullOrEmpty(textObject))
		{
			textObject.SetTextVariable("ENEMY_KINGDOM_INFORMAL_NAME", factionDeclaredPeace.InformalName);
		}
		return textObject;
	}

	public override ExplainedNumber GetWarProgressScore(IFaction factionDeclaresWar, IFaction factionDeclaredWar, bool includeDescriptions = false)
	{
		ExplainedNumber result = new ExplainedNumber(0f, includeDescriptions);
		StanceLink stanceWith = factionDeclaresWar.GetStanceWith(factionDeclaredWar);
		if (!stanceWith.IsAtWar)
		{
			return result;
		}
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		foreach (Town fief in factionDeclaredWar.Fiefs)
		{
			if (fief.IsTown)
			{
				num++;
			}
			else if (fief.IsCastle)
			{
				num2++;
			}
			num3 += fief.Villages.Count;
		}
		int num4 = factionDeclaredWar.WarPartyComponents.Sum((WarPartyComponent x) => x.Party.NumberOfAllMembers);
		int num5 = factionDeclaredWar.Fiefs.Sum((Town x) => x.GarrisonParty?.Party.NumberOfAllMembers ?? 0) + num4;
		int casualties = stanceWith.GetCasualties(factionDeclaredWar);
		int successfulTownSieges = stanceWith.GetSuccessfulTownSieges(factionDeclaresWar);
		int num6 = stanceWith.GetSuccessfulSieges(factionDeclaresWar) - successfulTownSieges;
		int successfulRaids = stanceWith.GetSuccessfulRaids(factionDeclaresWar);
		int casualties2 = stanceWith.GetCasualties(factionDeclaresWar);
		int successfulTownSieges2 = stanceWith.GetSuccessfulTownSieges(factionDeclaredWar);
		int num7 = stanceWith.GetSuccessfulSieges(factionDeclaredWar) - successfulTownSieges2;
		int successfulRaids2 = stanceWith.GetSuccessfulRaids(factionDeclaredWar);
		float value = Math.Max(0f, (float)(casualties - casualties2) / (float)Math.Max(1, num5 * 4) * 500f);
		float value2 = Math.Max(0f, (float)(successfulTownSieges - successfulTownSieges2) / (float)Math.Max(1, num + successfulTownSieges - successfulTownSieges2) * 1000f);
		float value3 = Math.Max(0f, (float)(num6 - num7) / (float)Math.Max(1, num2 + num6 - num7) * 500f);
		float value4 = Math.Max(0f, (float)(successfulRaids - successfulRaids2) / (float)Math.Max(1, num3) * 250f);
		result.Add(value, new TextObject("{=FKe05WtJ}Kills"));
		result.Add(value2, new TextObject("{=bVa5jNbd}Town Sieges"));
		result.Add(value3, new TextObject("{=Sdu2FmgY}Castle Sieges"));
		result.Add(value4, new TextObject("{=w6E2lb09}Raids"));
		result.LimitMin(0f);
		result.LimitMax(750f);
		return result;
	}

	private static float GetWarScale(IFaction factionDeclaresWar, IFaction factionDeclaredWar)
	{
		StanceLink stanceWith = factionDeclaresWar.GetStanceWith(factionDeclaredWar);
		if (!stanceWith.IsAtWar)
		{
			return 1f;
		}
		int casualties = stanceWith.GetCasualties(factionDeclaredWar);
		int casualties2 = stanceWith.GetCasualties(factionDeclaresWar);
		int num = TaleWorlds.Library.MathF.Max(1, (int)stanceWith.WarStartDate.ElapsedDaysUntilNow);
		if (num <= 20)
		{
			return 1f;
		}
		float num2 = (float)TaleWorlds.Library.MathF.Max(casualties + casualties2, 1) / (20f * TaleWorlds.Library.MathF.Pow(num, 1.5f));
		if (num2 >= 1f || num2 <= 0f)
		{
			return 1f;
		}
		return num2;
	}

	public override float GetScoreOfDeclaringWar(IFaction factionDeclaresWar, IFaction factionDeclaredWar, Clan evaluatingClan, out TextObject reason, bool includeReason = false)
	{
		reason = null;
		if (includeReason)
		{
			reason = GetReasonForDeclaringWar(factionDeclaresWar, factionDeclaredWar, evaluatingClan);
		}
		if (factionDeclaresWar.CurrentTotalStrength <= 500f || factionDeclaresWar.WarPartyComponents.Count < 2)
		{
			return -10000000f;
		}
		float exposureScoreToOtherFaction = GetExposureScoreToOtherFaction(factionDeclaresWar, factionDeclaredWar);
		if (exposureScoreToOtherFaction.ApproximatelyEqualsTo(float.MinValue))
		{
			return -10000000f;
		}
		GetBenefitAndRiskScoreForWar(factionDeclaresWar, factionDeclaredWar, evaluatingClan, out var benefitScore, out var riskScore);
		float relationScore = GetRelationScore(factionDeclaresWar, factionDeclaredWar, evaluatingClan);
		float sameCultureTownScore = GetSameCultureTownScore(factionDeclaresWar, factionDeclaredWar);
		float allianceFactorForDeclaringWar = Campaign.Current.Models.AllianceModel.GetAllianceFactorForDeclaringWar(factionDeclaresWar, factionDeclaredWar);
		float tradeAgreementFactor = GetTradeAgreementFactor(factionDeclaresWar, factionDeclaredWar);
		return sameCultureTownScore + benefitScore * exposureScoreToOtherFaction * allianceFactorForDeclaringWar * tradeAgreementFactor - riskScore + relationScore;
	}

	private static float GetTradeAgreementFactor(IFaction factionDeclaresWar, IFaction factionDeclaredWar)
	{
		float num = 1f;
		if (factionDeclaresWar.IsKingdomFaction && factionDeclaredWar.IsKingdomFaction)
		{
			Kingdom kingdom = (Kingdom)factionDeclaresWar;
			Kingdom kingdom2 = (Kingdom)factionDeclaredWar;
			if (!kingdom2.IsAllyWith(kingdom))
			{
				ITradeAgreementsCampaignBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<ITradeAgreementsCampaignBehavior>();
				if (campaignBehavior != null && campaignBehavior.HasTradeAgreement(kingdom, kingdom2, out var _))
				{
					num *= Campaign.Current.Models.DiplomacyModel.WarDeclarationScorePenaltyAgainstTradePartners;
				}
			}
		}
		return num;
	}

	private TextObject GetReasonForDeclaringWar(IFaction factionDeclaresWar, IFaction factionDeclaredWar, Clan evaluatingClan)
	{
		WarStats warStats = CalculateWarStatsForWar(factionDeclaresWar, factionDeclaredWar, evaluatingClan);
		WarStats warStats2 = CalculateWarStatsForWar(factionDeclaredWar, factionDeclaresWar, evaluatingClan);
		GetBenefitAndRiskScoreForWar(factionDeclaresWar, factionDeclaredWar, evaluatingClan, out var benefitScore, out var riskScore);
		float relationScore = GetRelationScore(factionDeclaresWar, factionDeclaredWar, evaluatingClan);
		float sameCultureTownScore = GetSameCultureTownScore(factionDeclaresWar, factionDeclaredWar);
		if (factionDeclaresWar.CurrentTotalStrength <= 500f || factionDeclaresWar.WarPartyComponents.Count < 2)
		{
			return new TextObject("{=JOe3BC41}The {ENEMY_KINGDOM_INFORMAL_NAME} is currently more powerful than us. We need time to build up our strength.");
		}
		if (GetExposureScoreToOtherFaction(factionDeclaresWar, factionDeclaredWar).ApproximatelyEqualsTo(float.MinValue))
		{
			return new TextObject("{=i0h0LKa0}Our borders are far from those of the enemy. It is too arduous to pursue this war.");
		}
		TextObject textObject = ((benefitScore - riskScore > 0f) ? ((relationScore > benefitScore - riskScore && relationScore > sameCultureTownScore) ? new TextObject("{=dov3iRlt}{ENEMY_RULER.NAME} of the {ENEMY_KINGDOM_INFORMAL_NAME} is vile and dangerous. We must deal with {?ENEMY_RULER.GENDER}her{?}him{\\?} before it is too late.") : ((sameCultureTownScore > benefitScore - riskScore) ? new TextObject("{=79lEPn1u}The {ENEMY_KINGDOM_INFORMAL_NAME} have occupied our ancestral lands and they oppress our kinfolk.") : ((!(warStats.Strength > warStats2.Strength)) ? new TextObject("{=1aQAmENB}The {ENEMY_KINGDOM_INFORMAL_NAME} may be strong, but their lands are rich and ripe for the taking.") : new TextObject("{=az3K3j4C}Right now we are stronger than the {ENEMY_KINGDOM_INFORMAL_NAME}. We should strike while we can.")))) : ((!(relationScore > sameCultureTownScore)) ? new TextObject("{=79lEPn1u}The {ENEMY_KINGDOM_INFORMAL_NAME} have occupied our ancestral lands and they oppress our kinfolk.") : new TextObject("{=dov3iRlt}{ENEMY_RULER.NAME} of the {ENEMY_KINGDOM_INFORMAL_NAME} is vile and dangerous. We must deal with {?ENEMY_RULER.GENDER}her{?}him{\\?} before it is too late.")));
		if (!TextObject.IsNullOrEmpty(textObject))
		{
			textObject.SetTextVariable("ENEMY_KINGDOM_INFORMAL_NAME", factionDeclaredWar.InformalName);
			textObject.SetCharacterProperties("ENEMY_RULER", factionDeclaredWar.Leader.CharacterObject);
		}
		return textObject;
	}

	private static float ApplyWarProgressToRiskScore(IFaction factionDeclaresPeace, IFaction factionDeclaredPeace, float riskScore)
	{
		float resultNumber = Campaign.Current.Models.DiplomacyModel.GetWarProgressScore(factionDeclaresPeace, factionDeclaredPeace).ResultNumber;
		float resultNumber2 = Campaign.Current.Models.DiplomacyModel.GetWarProgressScore(factionDeclaredPeace, factionDeclaresPeace).ResultNumber;
		float num = TaleWorlds.Library.MathF.Abs(resultNumber2 - resultNumber);
		if (num < 75f)
		{
			riskScore *= MBMath.Map(num, 0f, 75f, 0.5f, 1f);
		}
		else if (resultNumber2 > resultNumber)
		{
			float num2 = (resultNumber2 - resultNumber + 650f) / 650f;
			riskScore *= num2;
		}
		return riskScore;
	}

	private static void GetBenefitAndRiskScoreForPeace(IFaction factionDeclaresPeace, IFaction factionDeclaredPeace, IFaction evaluatingFaction, out float benefitScore, out float riskScore)
	{
		WarStats faction1Stats = CalculateWarStatsForPeace(factionDeclaresPeace, factionDeclaredPeace, evaluatingFaction);
		WarStats faction2Stats = CalculateWarStatsForPeace(factionDeclaredPeace, factionDeclaresPeace, evaluatingFaction);
		benefitScore = CalculateBenefitScore(faction1Stats, faction2Stats);
		riskScore = CalculateRiskScore(faction1Stats, faction2Stats);
		riskScore = TaleWorlds.Library.MathF.Min(faction2Stats.ValueOfSettlements * 0.75f, riskScore);
		benefitScore = TaleWorlds.Library.MathF.Min(faction1Stats.ValueOfSettlements * 1.5f, benefitScore);
	}

	private static void GetBenefitAndRiskScoreForWar(IFaction factionDeclaresWar, IFaction factionDeclaredWar, IFaction evaluatingFaction, out float benefitScore, out float riskScore)
	{
		WarStats faction1Stats = CalculateWarStatsForWar(factionDeclaresWar, factionDeclaredWar, evaluatingFaction);
		WarStats faction2Stats = CalculateWarStatsForWar(factionDeclaredWar, factionDeclaresWar, evaluatingFaction);
		benefitScore = CalculateBenefitScore(faction1Stats, faction2Stats);
		riskScore = CalculateRiskScore(faction1Stats, faction2Stats);
		ApplyTributeEffectToBenefitScoreForWar(factionDeclaresWar, factionDeclaredWar, evaluatingFaction, ref benefitScore);
		UpdateOurBenefitMinusOurRiskBasedOnEvaluatingFaction(evaluatingFaction, ref benefitScore, ref riskScore);
	}

	private static void ApplyTributeEffectToBenefitScoreForWar(IFaction factionDeclaresWar, IFaction factionDeclaredWar, IFaction evaluatingFaction, ref float benefitScore)
	{
		StanceLink stanceWith = factionDeclaresWar.GetStanceWith(factionDeclaredWar);
		if (stanceWith.GetRemainingTributePaymentCount() == 0)
		{
			return;
		}
		int dailyTributeToPay = stanceWith.GetDailyTributeToPay(factionDeclaresWar);
		int dailyTributeToPay2 = stanceWith.GetDailyTributeToPay(factionDeclaredWar);
		if (dailyTributeToPay == 0 && dailyTributeToPay2 == 0)
		{
			return;
		}
		bool flag = stanceWith.GetDailyTributeToPay(evaluatingFaction.MapFaction) > 0 && evaluatingFaction.MapFaction == factionDeclaresWar;
		if (dailyTributeToPay > 0)
		{
			float num = factionDeclaresWar.Fiefs.Sum((Town x) => x.Prosperity) + 1f;
			float num2 = 1f + (float)dailyTributeToPay / num;
			benefitScore = (flag ? (benefitScore * num2) : (benefitScore / num2));
		}
		else if (dailyTributeToPay2 > 0)
		{
			float num3 = factionDeclaredWar.Fiefs.Sum((Town x) => x.Prosperity) + 1f;
			float num4 = 1f + (float)dailyTributeToPay2 / num3;
			benefitScore = (flag ? (benefitScore * num4) : (benefitScore / num4));
		}
	}

	public override float GetScoreOfLettingPartyGo(MobileParty party, MobileParty partyToLetGo)
	{
		float num = 0f;
		for (int i = 0; i < partyToLetGo.ItemRoster.Count; i++)
		{
			ItemRosterElement elementCopyAtIndex = partyToLetGo.ItemRoster.GetElementCopyAtIndex(i);
			num += (float)(elementCopyAtIndex.Amount * elementCopyAtIndex.EquipmentElement.GetBaseValue());
		}
		float num2 = 0f;
		for (int j = 0; j < party.ItemRoster.Count; j++)
		{
			ItemRosterElement elementCopyAtIndex2 = party.ItemRoster.GetElementCopyAtIndex(j);
			num2 += (float)(elementCopyAtIndex2.Amount * elementCopyAtIndex2.EquipmentElement.GetBaseValue());
		}
		float num3 = 0f;
		foreach (TroopRosterElement item in party.MemberRoster.GetTroopRoster())
		{
			num3 += TaleWorlds.Library.MathF.Min(1000f, 10f * (float)item.Character.Level * TaleWorlds.Library.MathF.Sqrt(item.Character.Level));
		}
		float num4 = 0f;
		foreach (TroopRosterElement item2 in partyToLetGo.MemberRoster.GetTroopRoster())
		{
			num4 += TaleWorlds.Library.MathF.Min(1000f, 10f * (float)item2.Character.Level * TaleWorlds.Library.MathF.Sqrt(item2.Character.Level));
		}
		float num5 = 0f;
		foreach (TroopRosterElement item3 in partyToLetGo.MemberRoster.GetTroopRoster())
		{
			if (item3.Character.IsHero)
			{
				num5 += 500f;
			}
			num5 += (float)Campaign.Current.Models.RansomValueCalculationModel.PrisonerRansomValue(item3.Character, partyToLetGo.LeaderHero) * 0.3f;
		}
		float num6 = (party.IsPartyTradeActive ? ((float)party.PartyTradeGold) : 0f);
		num6 += ((party.LeaderHero != null) ? ((float)party.PartyTradeGold * 0.15f) : 0f);
		float num7 = (partyToLetGo.IsPartyTradeActive ? ((float)partyToLetGo.PartyTradeGold) : 0f);
		num6 += ((partyToLetGo.LeaderHero != null) ? ((float)partyToLetGo.PartyTradeGold * 0.15f) : 0f);
		float num8 = num4 + 10000f;
		if (partyToLetGo.BesiegedSettlement != null)
		{
			num8 += 20000f;
		}
		return -1000f + 0.01999998f * num3 - 0.98f * num8 - 0.98f * num7 + 0.01999998f * num6 + 0.98f * num5 + (num2 * 0.01999998f - 0.98f * num);
	}

	public override float GetValueOfHeroForFaction(Hero examinedHero, IFaction targetFaction, bool forMarriage = false)
	{
		return GetHeroCommandingStrengthForClan(examinedHero) * 10f;
	}

	public override int GetRelationCostOfExpellingClanFromKingdom()
	{
		return -20;
	}

	public override int GetInfluenceCostOfSupportingClan()
	{
		return 50;
	}

	public override int GetInfluenceCostOfExpellingClan(Clan proposingClan)
	{
		ExplainedNumber cost = new ExplainedNumber(200f);
		GetPerkEffectsOnKingdomDecisionInfluenceCost(proposingClan, ref cost);
		return TaleWorlds.Library.MathF.Round(cost.ResultNumber);
	}

	public override int GetInfluenceCostOfProposingPeace(Clan proposingClan)
	{
		ExplainedNumber cost = new ExplainedNumber(100f);
		GetPerkEffectsOnKingdomDecisionInfluenceCost(proposingClan, ref cost);
		return TaleWorlds.Library.MathF.Round(cost.ResultNumber);
	}

	public override int GetInfluenceCostOfProposingWar(Clan proposingClan)
	{
		ExplainedNumber cost = new ExplainedNumber(200f);
		if (proposingClan.Kingdom.ActivePolicies.Contains(DefaultPolicies.WarTax) && proposingClan == proposingClan.Kingdom.RulingClan)
		{
			cost.AddFactor(1f);
		}
		GetPerkEffectsOnKingdomDecisionInfluenceCost(proposingClan, ref cost);
		return TaleWorlds.Library.MathF.Round(cost.ResultNumber);
	}

	public override int GetInfluenceValueOfSupportingClan()
	{
		return GetInfluenceCostOfSupportingClan() / 4;
	}

	public override int GetRelationValueOfSupportingClan()
	{
		return 1;
	}

	public override int GetInfluenceCostOfAnnexation(Clan proposingClan)
	{
		ExplainedNumber cost = new ExplainedNumber(200f);
		if (proposingClan.Kingdom != null)
		{
			if (proposingClan.Kingdom.ActivePolicies.Contains(DefaultPolicies.FeudalInheritance))
			{
				cost.AddFactor(1f);
			}
			if (proposingClan.Kingdom.ActivePolicies.Contains(DefaultPolicies.PrecarialLandTenure) && proposingClan == proposingClan.Kingdom.RulingClan)
			{
				cost.AddFactor(-0.5f);
			}
		}
		GetPerkEffectsOnKingdomDecisionInfluenceCost(proposingClan, ref cost);
		return TaleWorlds.Library.MathF.Round(cost.ResultNumber);
	}

	public override int GetInfluenceCostOfChangingLeaderOfArmy()
	{
		return 30;
	}

	public override int GetInfluenceCostOfDisbandingArmy()
	{
		int num = 30;
		if (Clan.PlayerClan.Kingdom != null && Clan.PlayerClan == Clan.PlayerClan.Kingdom.RulingClan)
		{
			num /= 2;
		}
		return num;
	}

	public override int GetRelationCostOfDisbandingArmy(bool isLeaderParty)
	{
		if (!isLeaderParty)
		{
			return -1;
		}
		return -4;
	}

	public override int GetInfluenceCostOfPolicyProposalAndDisavowal(Clan proposerClan)
	{
		ExplainedNumber cost = new ExplainedNumber(100f);
		GetPerkEffectsOnKingdomDecisionInfluenceCost(proposerClan, ref cost);
		return TaleWorlds.Library.MathF.Round(cost.ResultNumber);
	}

	public override int GetInfluenceCostOfAbandoningArmy()
	{
		return 2;
	}

	private void GetPerkEffectsOnKingdomDecisionInfluenceCost(Clan proposingClan, ref ExplainedNumber cost)
	{
		if (proposingClan.Leader.GetPerkValue(DefaultPerks.Charm.Firebrand))
		{
			cost.AddFactor(DefaultPerks.Charm.Firebrand.PrimaryBonus, DefaultPerks.Charm.Firebrand.Name);
		}
	}

	private int GetBaseRelationBetweenHeroes(Hero hero1, Hero hero2)
	{
		return CharacterRelationManager.GetHeroRelation(hero1, hero2);
	}

	public override int GetBaseRelation(Hero hero1, Hero hero2)
	{
		return GetBaseRelationBetweenHeroes(hero1, hero2);
	}

	public override int GetEffectiveRelation(Hero hero1, Hero hero2)
	{
		GetHeroesForEffectiveRelation(hero1, hero2, out var effectiveHero, out var effectiveHero2);
		if (effectiveHero == null || effectiveHero2 == null)
		{
			return 0;
		}
		int effectiveRelation = GetBaseRelationBetweenHeroes(effectiveHero, effectiveHero2);
		GetPersonalityEffects(ref effectiveRelation, hero1, effectiveHero2);
		return MBMath.ClampInt(effectiveRelation, MinRelationLimit, MaxRelationLimit);
	}

	public override void GetHeroesForEffectiveRelation(Hero hero1, Hero hero2, out Hero effectiveHero1, out Hero effectiveHero2)
	{
		effectiveHero1 = ((hero1.Clan != null) ? hero1.Clan.Leader : hero1);
		effectiveHero2 = ((hero2.Clan != null) ? hero2.Clan.Leader : hero2);
		if (effectiveHero1 == effectiveHero2 || (hero1.IsPlayerCompanion && hero2.IsHumanPlayerCharacter) || (hero2.IsPlayerCompanion && hero1.IsHumanPlayerCharacter))
		{
			effectiveHero1 = hero1;
			effectiveHero2 = hero2;
		}
	}

	public override int GetRelationChangeAfterClanLeaderIsDead(Hero deadLeader, Hero relationHero)
	{
		return (int)((float)CharacterRelationManager.GetHeroRelation(deadLeader, relationHero) * 0.7f);
	}

	public override int GetRelationChangeAfterVotingInSettlementOwnerPreliminaryDecision(Hero supporter, bool hasHeroVotedAgainstOwner)
	{
		int num;
		if (hasHeroVotedAgainstOwner)
		{
			num = -20;
			if (supporter.Culture.HasFeat(DefaultCulturalFeats.SturgianDecisionPenaltyFeat))
			{
				num += (int)((float)num * DefaultCulturalFeats.SturgianDecisionPenaltyFeat.EffectBonus);
			}
		}
		else
		{
			num = 5;
		}
		return num;
	}

	private void GetPersonalityEffects(ref int effectiveRelation, Hero hero1, Hero effectiveHero2)
	{
		GetTraitEffect(ref effectiveRelation, hero1, effectiveHero2, DefaultTraits.Honor, 2);
		GetTraitEffect(ref effectiveRelation, hero1, effectiveHero2, DefaultTraits.Valor, 1);
		GetTraitEffect(ref effectiveRelation, hero1, effectiveHero2, DefaultTraits.Mercy, 1);
	}

	private void GetTraitEffect(ref int effectiveRelation, Hero hero1, Hero effectiveHero2, TraitObject trait, int effectMagnitude)
	{
		int traitLevel = hero1.GetTraitLevel(trait);
		int traitLevel2 = effectiveHero2.GetTraitLevel(trait);
		int num = traitLevel * traitLevel2;
		if (num > 0)
		{
			effectiveRelation += effectMagnitude;
		}
		else if (num < 0)
		{
			effectiveRelation -= effectMagnitude;
		}
	}

	public override int GetCharmExperienceFromRelationGain(Hero hero, float relationChange, ChangeRelationAction.ChangeRelationDetail detail)
	{
		float num = 20f;
		if (detail == ChangeRelationAction.ChangeRelationDetail.Emissary)
		{
			num = (hero.IsNotable ? (num * 20f) : ((hero.MapFaction != null && hero.MapFaction.Leader == hero) ? (num * 30f) : ((hero.Clan == null || hero.Clan.Leader != hero) ? (num * 10f) : (num * 20f))));
		}
		else if (!hero.IsNotable)
		{
			if (hero.MapFaction != null && hero.MapFaction.Leader == hero)
			{
				num *= 30f;
			}
			else if (hero.Clan != null && hero.Clan.Leader == hero)
			{
				num *= 20f;
			}
		}
		return TaleWorlds.Library.MathF.Round(num * relationChange);
	}

	public override uint GetNotificationColor(ChatNotificationType notificationType)
	{
		return notificationType switch
		{
			ChatNotificationType.Default => 10066329u, 
			ChatNotificationType.Neutral => 12303291u, 
			ChatNotificationType.PlayerClanPositive => 3407803u, 
			ChatNotificationType.PlayerClanNegative => 16750899u, 
			ChatNotificationType.PlayerFactionPositive => 2284902u, 
			ChatNotificationType.PlayerFactionNegative => 14509602u, 
			ChatNotificationType.PlayerFactionIndirectPositive => 12298820u, 
			ChatNotificationType.PlayerFactionIndirectNegative => 13382502u, 
			ChatNotificationType.Civilian => 10053324u, 
			ChatNotificationType.PlayerFactionCivilian => 11163101u, 
			ChatNotificationType.PlayerClanCivilian => 15623935u, 
			ChatNotificationType.Political => 6724044u, 
			ChatNotificationType.PlayerFactionPolitical => 5614301u, 
			ChatNotificationType.PlayerClanPolitical => 6745855u, 
			_ => 13369548u, 
		};
	}

	public override float DenarsToInfluence()
	{
		return 0.002f;
	}

	public override float GetDecisionMakingThreshold(IFaction consideringFaction)
	{
		return Campaign.Current.Models.DiplomacyModel.GetValueOfSettlementsForFaction(consideringFaction) / 6f;
	}

	public override bool CanSettlementBeGifted(Settlement settlementToGift)
	{
		if (settlementToGift.Town != null)
		{
			return !settlementToGift.Town.IsOwnerUnassigned;
		}
		return false;
	}

	public override float GetValueOfSettlementsForFaction(IFaction faction)
	{
		float num = 0f;
		float num2 = 0f;
		foreach (Town fief in faction.Fiefs)
		{
			num = ((!fief.IsTown) ? (num + 1000f) : (num + 2000f));
			num += fief.Prosperity * 0.33f;
			num2 += (float)fief.Villages.Count * 300f;
		}
		num *= 50f;
		num += num2 * 25f;
		return AdjustValueOfSettlements(num);
	}

	public override IEnumerable<BarterGroup> GetBarterGroups()
	{
		return new BarterGroup[6]
		{
			new GoldBarterGroup(),
			new ItemBarterGroup(),
			new PrisonerBarterGroup(),
			new FiefBarterGroup(),
			new OtherBarterGroup(),
			new DefaultsBarterGroup()
		};
	}

	public override bool IsPeaceSuitable(IFaction factionDeclaresPeace, IFaction factionDeclaredPeace)
	{
		if (factionDeclaresPeace.IsEliminated || factionDeclaredPeace.IsEliminated)
		{
			return false;
		}
		float scoreOfDeclaringPeace = Campaign.Current.Models.DiplomacyModel.GetScoreOfDeclaringPeace(factionDeclaresPeace, factionDeclaredPeace);
		float scoreOfDeclaringPeace2 = Campaign.Current.Models.DiplomacyModel.GetScoreOfDeclaringPeace(factionDeclaredPeace, factionDeclaresPeace);
		float valueOfSettlementsForFaction = Campaign.Current.Models.DiplomacyModel.GetValueOfSettlementsForFaction(factionDeclaresPeace);
		float num = ((!(scoreOfDeclaringPeace2 > 0f)) ? (Campaign.Current.Models.DiplomacyModel.GetDecisionMakingThreshold(factionDeclaredPeace) - scoreOfDeclaringPeace2) : (scoreOfDeclaringPeace2 - scoreOfDeclaringPeace));
		if (num > valueOfSettlementsForFaction && factionDeclaresPeace.GetStanceWith(factionDeclaredPeace).WarStartDate.ElapsedDaysUntilNow < 150f)
		{
			return false;
		}
		return true;
	}

	public override int GetDailyTributeToPay(Clan factionToPay, Clan factionToReceive, out int tributeDurationInDays)
	{
		float scoreOfDeclaringPeace = Campaign.Current.Models.DiplomacyModel.GetScoreOfDeclaringPeace(factionToReceive.MapFaction, factionToPay.MapFaction);
		float scoreOfDeclaringPeace2 = Campaign.Current.Models.DiplomacyModel.GetScoreOfDeclaringPeace(factionToPay.MapFaction, factionToReceive.MapFaction);
		float valueOfSettlementsForFaction = Campaign.Current.Models.DiplomacyModel.GetValueOfSettlementsForFaction(factionToPay.MapFaction);
		float num = ((!(scoreOfDeclaringPeace > 0f)) ? (Campaign.Current.Models.DiplomacyModel.GetDecisionMakingThreshold(factionToReceive.MapFaction) - scoreOfDeclaringPeace) : (scoreOfDeclaringPeace - scoreOfDeclaringPeace2));
		float resultNumber = Campaign.Current.Models.DiplomacyModel.GetWarProgressScore(factionToPay.MapFaction, factionToReceive.MapFaction).ResultNumber;
		float resultNumber2 = Campaign.Current.Models.DiplomacyModel.GetWarProgressScore(factionToReceive.MapFaction, factionToPay.MapFaction).ResultNumber;
		float num2 = TaleWorlds.Library.MathF.Abs(resultNumber - resultNumber2);
		if (resultNumber > resultNumber2)
		{
			tributeDurationInDays = 0;
			return 0;
		}
		float num3 = num / (valueOfSettlementsForFaction + 1f);
		if (num2 < 75f)
		{
			num3 = 0.05f;
		}
		else
		{
			num3 /= 2f;
			num3 = ((num3 < 0.05f) ? 0f : ((num3 > 0.05f && num3 < 0.1f) ? 0.05f : ((!(num3 > 0.1f) || !(num3 < 0.15f)) ? 0.15f : 0.1f)));
		}
		int num4 = (int)(num3 * factionToPay.MapFaction.Fiefs.Sum((Town x) => x.Prosperity) * 0.35f);
		num4 = 10 * (num4 / 10);
		tributeDurationInDays = ((num4 != 0) ? 100 : 0);
		return num4;
	}

	public override bool IsClanEligibleToBecomeRuler(Clan clan)
	{
		if (!clan.IsEliminated && clan.Leader.IsAlive)
		{
			return !clan.IsUnderMercenaryService;
		}
		return false;
	}

	public override DiplomacyStance? GetShallowDiplomaticStance(IFaction faction1, IFaction faction2)
	{
		if (faction1.IsBanditFaction != faction2.IsBanditFaction)
		{
			return DiplomacyStance.War;
		}
		return null;
	}

	public override DiplomacyStance GetDefaultDiplomaticStance(IFaction faction1, IFaction faction2)
	{
		if (IsAtConstantWar(faction1, faction2))
		{
			return DiplomacyStance.War;
		}
		return DiplomacyStance.Neutral;
	}

	public override bool IsAtConstantWar(IFaction faction1, IFaction faction2)
	{
		if (((faction1.IsOutlaw && faction1.IsMinorFaction && faction2.IsKingdomFaction) || (faction2.IsOutlaw && faction2.IsMinorFaction && faction1.IsKingdomFaction)) && faction1.Culture == faction2.Culture)
		{
			return true;
		}
		if (GetShallowDiplomaticStance(faction1, faction2) == DiplomacyStance.War)
		{
			return true;
		}
		return false;
	}

	private static WarStats CalculateWarStatsForPeace(IFaction faction, IFaction targetFaction, IFaction evaluatingFaction)
	{
		float num = 0f;
		float num2 = 0f;
		bool flag = evaluatingFaction.MapFaction == faction.MapFaction;
		float val = faction.WarPartyComponents.Sum((WarPartyComponent x) => x.Party.EstimatedStrength);
		float num3 = (flag ? faction.Fiefs.Sum((Town x) => x.GarrisonParty?.Party.EstimatedStrength ?? 0f) : (faction.Fiefs.Sum((Town x) => x.GarrisonParty?.Party.EstimatedStrength ?? 0f) * 0.7f));
		if (faction.IsKingdomFaction)
		{
			foreach (Clan clan in ((Kingdom)faction).Clans)
			{
				if (!clan.IsUnderMercenaryService)
				{
					int partyLimitForTier = Campaign.Current.Models.ClanTierModel.GetPartyLimitForTier(clan, clan.Tier);
					num2 += (float)(partyLimitForTier * 64);
				}
			}
		}
		num += num3 + Math.Max(val, num2);
		float num4 = 0f;
		foreach (IFaction item in faction.FactionsAtWarWith.Where((IFaction x) => x != targetFaction))
		{
			float num5 = 0f;
			if (item.IsBanditFaction || (item.IsMinorFaction && item.Leader != Hero.MainHero) || !item.IsKingdomFaction)
			{
				continue;
			}
			int num6 = 0;
			foreach (Clan item2 in ((Kingdom)item).Clans.Where((Clan x) => !x.IsUnderMercenaryService))
			{
				num6 += Campaign.Current.Models.ClanTierModel.GetPartyLimitForTier(item2, item2.Tier);
			}
			num5 += (float)(num6 * 64);
			float val2 = item.WarPartyComponents.Sum((WarPartyComponent x) => x.Party.EstimatedStrength);
			num4 += Math.Max(val2, num5);
		}
		return new WarStats
		{
			Strength = num,
			ValueOfSettlements = Campaign.Current.Models.DiplomacyModel.GetValueOfSettlementsForFaction(faction),
			TotalStrengthOfEnemies = (flag ? (num4 * 0.6f) : num4)
		};
	}

	private static WarStats CalculateWarStatsForWar(IFaction faction, IFaction targetFaction, IFaction evaluatingFaction)
	{
		float val = faction.WarPartyComponents.Sum((WarPartyComponent x) => x.Party.EstimatedStrength);
		float num = faction.Fiefs.Sum((Town x) => x.GarrisonParty?.Party.EstimatedStrength ?? 0f);
		float num2 = 0f;
		float num3 = 0f;
		bool flag = evaluatingFaction.MapFaction == faction.MapFaction;
		if (faction.IsKingdomFaction)
		{
			foreach (Clan clan in ((Kingdom)faction).Clans)
			{
				if (!clan.IsUnderMercenaryService)
				{
					int partyLimitForTier = Campaign.Current.Models.ClanTierModel.GetPartyLimitForTier(clan, clan.Tier);
					num3 += (float)(partyLimitForTier * 64);
				}
			}
		}
		num2 += num + Math.Max(val, num3);
		float num4 = 0f;
		float num5 = 0f;
		foreach (IFaction item in faction.FactionsAtWarWith.Where((IFaction x) => x.MapFaction != targetFaction.MapFaction))
		{
			if (item.IsBanditFaction || (item.IsMinorFaction && item.Leader != Hero.MainHero) || !item.IsKingdomFaction)
			{
				continue;
			}
			int num6 = 0;
			foreach (Clan item2 in ((Kingdom)item).Clans.Where((Clan x) => !x.IsUnderMercenaryService))
			{
				num6 += Campaign.Current.Models.ClanTierModel.GetPartyLimitForTier(item2, item2.Tier);
			}
			num5 += (float)(num6 * 64);
			float val2 = item.WarPartyComponents.Sum((WarPartyComponent x) => x.Party.EstimatedStrength);
			float num7 = item.Fiefs.Sum((Town x) => x.GarrisonParty?.Party.EstimatedStrength ?? 0f) + Math.Max(num5, val2);
			num4 += num7;
		}
		return new WarStats
		{
			Strength = num2,
			ValueOfSettlements = Campaign.Current.Models.DiplomacyModel.GetValueOfSettlementsForFaction(faction),
			TotalStrengthOfEnemies = (flag ? (num4 * 1.1f) : num4)
		};
	}

	private static float GetExposureScoreToOtherFaction(IFaction factionDeclaresWar, IFaction factionDeclaredWar)
	{
		HashSet<Settlement> hashSet = new HashSet<Settlement>();
		float num = 0f;
		float num2 = 0f;
		if (factionDeclaresWar.Fiefs.Count == 0 || factionDeclaredWar.Fiefs.Count == 0)
		{
			return 1f;
		}
		foreach (Town fief in factionDeclaresWar.Fiefs)
		{
			foreach (Settlement neighborFortification in fief.GetNeighborFortifications(MobileParty.NavigationType.All))
			{
				if (neighborFortification.MapFaction != factionDeclaresWar && !hashSet.Contains(neighborFortification))
				{
					if (neighborFortification.MapFaction == factionDeclaredWar)
					{
						num2 += 1f;
					}
					num += 1f;
					hashSet.Add(neighborFortification);
				}
			}
		}
		if (num2 < 1f)
		{
			return float.MinValue;
		}
		return 0.8f + num2 / num;
	}

	private static float CalculateBenefitScore(WarStats faction1Stats, WarStats faction2Stats)
	{
		float num = TaleWorlds.Library.MathF.Clamp(faction2Stats.ValueOfSettlements, 10000f, 10000000f);
		float num2 = (faction2Stats.Strength + faction1Stats.TotalStrengthOfEnemies) / faction1Stats.Strength;
		float num3 = TaleWorlds.Library.MathF.Clamp(1f / (1f + num2 * num2), 0.1f, 0.9f);
		return num * num3;
	}

	private static float CalculateRiskScore(WarStats faction1Stats, WarStats faction2Stats)
	{
		float num = TaleWorlds.Library.MathF.Clamp(faction1Stats.ValueOfSettlements, 10000f, 10000000f);
		float num2 = faction1Stats.Strength / (faction2Stats.Strength + faction1Stats.TotalStrengthOfEnemies);
		float num3 = TaleWorlds.Library.MathF.Clamp(1f / (1f + num2 * num2), 0.1f, 0.9f);
		return num * num3;
	}

	private static float AdjustValueOfSettlements(float valueOfSettlements)
	{
		if (!(valueOfSettlements > 2000000f))
		{
			return valueOfSettlements + 10000f;
		}
		return (valueOfSettlements - 2000000f) * 0.5f + 10000f + 2000000f;
	}

	private static float GetRelationScore(IFaction factionDeclaresWar, IFaction factionDeclaredWar, IFaction evaluatingFaction)
	{
		int relationWithClan = factionDeclaresWar.Leader.Clan.GetRelationWithClan(factionDeclaredWar.Leader.Clan);
		int relationWithClan2 = evaluatingFaction.Leader.Clan.GetRelationWithClan(factionDeclaredWar.Leader.Clan);
		float num = (float)(relationWithClan + relationWithClan2) / 2f;
		float result = 0f;
		if (num < 0f)
		{
			if (factionDeclaresWar.CurrentTotalStrength > factionDeclaredWar.CurrentTotalStrength * 2f)
			{
				result = -250f * num;
			}
			else
			{
				float num2 = factionDeclaresWar.CurrentTotalStrength / (2f * factionDeclaredWar.CurrentTotalStrength);
				result = -250f * (num2 * num2) * num;
			}
		}
		return result;
	}

	private static float GetSameCultureTownScore(IFaction factionDeclaresWar, IFaction factionDeclaredWar)
	{
		float b = factionDeclaredWar.Settlements.Sum((Settlement s) => (s.Culture != factionDeclaresWar.Culture || !s.IsFortification) ? 0f : (s.Town.Prosperity * 0.5f * 50f));
		float num = TaleWorlds.Library.MathF.Min(100000f, b);
		return 0.3f * num;
	}

	private static void UpdateOurBenefitMinusOurRiskBasedOnEvaluatingFaction(IFaction evaluatingFaction, ref float ourBenefit, ref float ourRisk)
	{
		if (!ourBenefit.ApproximatelyEqualsTo(ourRisk) && !evaluatingFaction.IsKingdomFaction && evaluatingFaction.Leader != evaluatingFaction.MapFaction.Leader)
		{
			bool flag = ourBenefit > ourRisk;
			if (flag && evaluatingFaction.Leader.GetTraitLevel(DefaultTraits.Valor) != 0)
			{
				ourBenefit *= 1f - 0.05f * (float)TaleWorlds.Library.MathF.Min(2, TaleWorlds.Library.MathF.Max(-2, evaluatingFaction.Leader.GetTraitLevel(DefaultTraits.Valor)));
			}
			else if (!flag && evaluatingFaction.Leader.GetTraitLevel(DefaultTraits.Calculating) != 0)
			{
				ourRisk *= 1f + 0.05f * (float)TaleWorlds.Library.MathF.Min(2, TaleWorlds.Library.MathF.Max(-2, evaluatingFaction.Leader.GetTraitLevel(DefaultTraits.Calculating)));
			}
		}
	}
}
