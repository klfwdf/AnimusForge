using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.BarterSystem;
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

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x0200010E RID: 270
	public class DefaultDiplomacyModel : DiplomacyModel
	{
		// Token: 0x17000646 RID: 1606
		// (get) Token: 0x06001746 RID: 5958 RVA: 0x0006C60E File Offset: 0x0006A80E
		public override int MinimumRelationWithConversationCharacterToJoinKingdom
		{
			get
			{
				return -10;
			}
		}

		// Token: 0x17000647 RID: 1607
		// (get) Token: 0x06001747 RID: 5959 RVA: 0x0006C612 File Offset: 0x0006A812
		public override int GiftingTownRelationshipBonus
		{
			get
			{
				return 20;
			}
		}

		// Token: 0x17000648 RID: 1608
		// (get) Token: 0x06001748 RID: 5960 RVA: 0x0006C616 File Offset: 0x0006A816
		public override int GiftingCastleRelationshipBonus
		{
			get
			{
				return 10;
			}
		}

		// Token: 0x17000649 RID: 1609
		// (get) Token: 0x06001749 RID: 5961 RVA: 0x0006C61A File Offset: 0x0006A81A
		public override int MaxRelationLimit
		{
			get
			{
				return 100;
			}
		}

		// Token: 0x1700064A RID: 1610
		// (get) Token: 0x0600174A RID: 5962 RVA: 0x0006C61E File Offset: 0x0006A81E
		public override int MinRelationLimit
		{
			get
			{
				return -100;
			}
		}

		// Token: 0x1700064B RID: 1611
		// (get) Token: 0x0600174B RID: 5963 RVA: 0x0006C622 File Offset: 0x0006A822
		public override int MaxNeutralRelationLimit
		{
			get
			{
				return 50;
			}
		}

		// Token: 0x1700064C RID: 1612
		// (get) Token: 0x0600174C RID: 5964 RVA: 0x0006C626 File Offset: 0x0006A826
		public override int MinNeutralRelationLimit
		{
			get
			{
				return -25;
			}
		}

		// Token: 0x1700064D RID: 1613
		// (get) Token: 0x0600174D RID: 5965 RVA: 0x0006C62A File Offset: 0x0006A82A
		public override float WarDeclarationScorePenaltyAgainstAllies
		{
			get
			{
				return 0.4f;
			}
		}

		// Token: 0x1700064E RID: 1614
		// (get) Token: 0x0600174E RID: 5966 RVA: 0x0006C631 File Offset: 0x0006A831
		public override float WarDeclarationScoreBonusAgainstEnemiesOfAllies
		{
			get
			{
				return 0.3f;
			}
		}

		// Token: 0x0600174F RID: 5967 RVA: 0x0006C638 File Offset: 0x0006A838
		public override float GetStrengthThresholdForNonMutualWarsToBeIgnoredToJoinKingdom(Kingdom kingdomToJoin)
		{
			return kingdomToJoin.CurrentTotalStrength * 0.05f;
		}

		// Token: 0x06001750 RID: 5968 RVA: 0x0006C648 File Offset: 0x0006A848
		public override float GetClanStrength(Clan clan)
		{
			float num = 0f;
			foreach (Hero hero in clan.Heroes)
			{
				num += this.GetHeroCommandingStrengthForClan(hero);
			}
			float num2 = clan.Influence * 1.2f;
			float num3 = (float)clan.Settlements.Count * 4f;
			return num + num2 + num3;
		}

		// Token: 0x06001751 RID: 5969 RVA: 0x0006C6CC File Offset: 0x0006A8CC
		public override float GetHeroCommandingStrengthForClan(Hero hero)
		{
			if (!hero.IsAlive)
			{
				return 0f;
			}
			float num = (float)hero.GetSkillValue(DefaultSkills.Tactics) * 1f;
			float num2 = (float)hero.GetSkillValue(DefaultSkills.Steward) * 1f;
			float num3 = (float)hero.GetSkillValue(DefaultSkills.Trade) * 1f;
			float num4 = (float)hero.GetSkillValue(DefaultSkills.Leadership) * 1f;
			float num5 = (float)((hero.GetTraitLevel(DefaultTraits.Commander) > 0) ? 300 : 0);
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
			float num12 = num5 + num + num2 + num3 + num4 + num6 + num7 + num8 + num9 + num10 + num11;
			if (num12 <= 0f)
			{
				return 0f;
			}
			return num12;
		}

		// Token: 0x06001752 RID: 5970 RVA: 0x0006C860 File Offset: 0x0006AA60
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
				float num7 = (float)((hero.GetTraitLevel(DefaultTraits.Honor) > 0) ? 100 : 0);
				float num8 = (float)MathF.Min(100000, hero.Gold) * 0.005f;
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
				return num7 + num + num4 + num5 + num6 + num8 + num9 + num2 + num3;
			}
			return 0f;
		}

		// Token: 0x06001753 RID: 5971 RVA: 0x0006C9D0 File Offset: 0x0006ABD0
		public override float GetRelationIncreaseFactor(Hero hero1, Hero hero2, float relationChange)
		{
			ExplainedNumber explainedNumber = new ExplainedNumber(relationChange, false, null);
			Hero hero3;
			if (hero1.IsHumanPlayerCharacter || hero2.IsHumanPlayerCharacter)
			{
				hero3 = (hero1.IsHumanPlayerCharacter ? hero1 : hero2);
			}
			else
			{
				hero3 = ((MBRandom.RandomFloat < 0.5f) ? hero1 : hero2);
			}
			SkillHelper.AddSkillBonusForCharacter(DefaultSkillEffects.CharmRelationBonus, hero3.CharacterObject, ref explainedNumber);
			if (hero1.IsFemale != hero2.IsFemale)
			{
				if (hero3.GetPerkValue(DefaultPerks.Charm.InBloom))
				{
					explainedNumber.AddFactor(DefaultPerks.Charm.InBloom.PrimaryBonus, null);
				}
			}
			else if (hero3.GetPerkValue(DefaultPerks.Charm.YoungAndRespectful))
			{
				explainedNumber.AddFactor(DefaultPerks.Charm.YoungAndRespectful.PrimaryBonus, null);
			}
			if (hero3.GetPerkValue(DefaultPerks.Charm.GoodNatured) && hero2.GetTraitLevel(DefaultTraits.Mercy) > 0)
			{
				explainedNumber.Add(DefaultPerks.Charm.GoodNatured.SecondaryBonus, DefaultPerks.Charm.GoodNatured.Name, null);
			}
			if (hero3.GetPerkValue(DefaultPerks.Charm.Tribute) && hero2.GetTraitLevel(DefaultTraits.Mercy) < 0)
			{
				explainedNumber.Add(DefaultPerks.Charm.Tribute.SecondaryBonus, DefaultPerks.Charm.Tribute.Name, null);
			}
			return explainedNumber.ResultNumber;
		}

		// Token: 0x06001754 RID: 5972 RVA: 0x0006CAEC File Offset: 0x0006ACEC
		public override int GetInfluenceAwardForSettlementCapturer(Settlement settlement)
		{
			int result;
			if (settlement.IsTown || settlement.IsCastle)
			{
				int num = (settlement.IsTown ? 30 : 10);
				int num2 = 0;
				foreach (Village village in settlement.BoundVillages)
				{
					num2 += this.GetInfluenceAwardForSettlementCapturer(village.Settlement);
				}
				result = num + num2;
			}
			else
			{
				result = 10;
			}
			return result;
		}

		// Token: 0x06001755 RID: 5973 RVA: 0x0006CB74 File Offset: 0x0006AD74
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

		// Token: 0x06001756 RID: 5974 RVA: 0x0006CBB4 File Offset: 0x0006ADB4
		public override float GetHourlyInfluenceAwardForRaidingEnemyVillage(MobileParty mobileParty)
		{
			int num = 0;
			foreach (MapEventParty mapEventParty in mobileParty.MapEvent.AttackerSide.Parties)
			{
				if (mapEventParty.Party.MobileParty != mobileParty)
				{
					MobileParty mobileParty2 = mapEventParty.Party.MobileParty;
					if (((mobileParty2 != null) ? mobileParty2.Army : null) == null || mapEventParty.Party.MobileParty.Army.LeaderParty != mobileParty)
					{
						continue;
					}
				}
				num += mapEventParty.Party.MemberRoster.TotalManCount;
			}
			return (MathF.Sqrt((float)num) + 2f) / 240f;
		}

		// Token: 0x06001757 RID: 5975 RVA: 0x0006CC74 File Offset: 0x0006AE74
		public override float GetHourlyInfluenceAwardForBesiegingEnemyFortification(MobileParty mobileParty)
		{
			int num = 0;
			foreach (PartyBase partyBase in mobileParty.BesiegedSettlement.SiegeEvent.GetSiegeEventSide(BattleSideEnum.Attacker).GetInvolvedPartiesForEventType(MapEvent.BattleTypes.Siege))
			{
				if (partyBase.MobileParty == mobileParty || (partyBase.MobileParty.Army != null && partyBase.MobileParty.Army.LeaderParty == mobileParty))
				{
					num += partyBase.MemberRoster.TotalManCount;
				}
			}
			return (MathF.Sqrt((float)num) + 2f) / 240f;
		}

		// Token: 0x06001758 RID: 5976 RVA: 0x0006CD18 File Offset: 0x0006AF18
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
			float num4 = MathF.Max(-100f, MathF.Min(100f, (float)relationBetweenClans + num3));
			float num5 = MathF.Min(2f, MathF.Max(0.33f, 1f + MathF.Sqrt(MathF.Abs(num4)) * ((num4 < 0f) ? (-0.067f) : 0.1f)));
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
			int commanderLimit = clan.CommanderLimit;
			float num9 = 0f;
			float num10 = 0f;
			if (!clan.IsMinorFaction)
			{
				float num11 = 0f;
				foreach (Town town in kingdom.Fiefs)
				{
					num11 += town.Settlement.GetSettlementValueForFaction(kingdom);
				}
				int num12 = 0;
				foreach (Clan clan3 in kingdom.Clans)
				{
					if (!clan3.IsUnderMercenaryService && clan3 != clan)
					{
						num12 += clan3.CommanderLimit;
					}
				}
				num9 = num11 / (float)(num12 + commanderLimit);
				num10 = -((float)(num12 * num12) * 100f) + 10000f;
			}
			float num13 = num9 * MathF.Sqrt((float)commanderLimit) * 0.15f * 0.2f;
			num13 *= num5 * num6;
			num13 += (clan.MapFaction.IsAtWarWith(kingdom) ? (num8 - num7) : 0f);
			num13 += num10;
			if (clan.Kingdom != null && clan.Kingdom.Leader == Hero.MainHero && num13 > 0f)
			{
				num13 *= 0.2f;
			}
			return num13;
		}

		// Token: 0x06001759 RID: 5977 RVA: 0x0006CFC4 File Offset: 0x0006B1C4
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
			float num4 = MathF.Max(-100f, MathF.Min(100f, (float)relationBetweenClans + num3));
			float num5 = MathF.Min(2f, MathF.Max(0.33f, 1f + MathF.Sqrt(MathF.Abs(num4)) * ((num4 < 0f) ? (-0.067f) : 0.1f)));
			float num6 = 1f + ((kingdom.Culture == clan.Culture) ? 0.15f : ((kingdom.Leader == Hero.MainHero) ? 0f : (-0.15f)));
			float num7 = clan.CalculateTotalSettlementBaseValue();
			float num8 = clan.CalculateTotalSettlementValueForFaction(kingdom);
			int commanderLimit = clan.CommanderLimit;
			float num9 = 0f;
			if (!clan.IsMinorFaction)
			{
				float num10 = 0f;
				foreach (Town town in kingdom.Fiefs)
				{
					num10 += town.Settlement.GetSettlementValueForFaction(kingdom);
				}
				int num11 = 0;
				foreach (Clan clan3 in kingdom.Clans)
				{
					if (!clan3.IsUnderMercenaryService && clan3 != clan)
					{
						num11 += clan3.CommanderLimit;
					}
				}
				num9 = num10 / (float)(num11 + commanderLimit);
			}
			float num12 = HeroHelper.CalculateReliabilityConstant(clan.Leader, 1f);
			float b = (float)(CampaignTime.Now - clan.LastFactionChangeTime).ToDays;
			float num13 = 4000f * (15f - MathF.Sqrt(MathF.Min(225f, b)));
			int num14 = 0;
			int num15 = 0;
			using (List<Town>.Enumerator enumerator2 = clan.Fiefs.GetEnumerator())
			{
				while (enumerator2.MoveNext())
				{
					if (enumerator2.Current.IsCastle)
					{
						num15++;
					}
					else
					{
						num14++;
					}
				}
			}
			float num16 = -70000f - (float)num15 * 10000f - (float)num14 * 30000f;
			num16 /= 0.15f;
			float num17 = -num9 * MathF.Sqrt((float)commanderLimit) * 0.15f * 0.2f + num16 * num12 + -num13;
			num17 *= num5 * num6;
			if (num5 < 1f && num7 - num8 < 0f)
			{
				num17 += num5 * (num7 - num8);
			}
			else
			{
				num17 += num7 - num8;
			}
			if (num5 < 1f)
			{
				num17 += (1f - num5) * 200000f;
			}
			if (kingdom.Leader == Hero.MainHero)
			{
				if (num17 > 0f)
				{
					num17 *= 0.2f;
				}
				else
				{
					num17 *= 5f;
				}
			}
			return num17 + ((kingdom.Leader == Hero.MainHero) ? (-(1000000f * num5)) : 0f);
		}

		// Token: 0x0600175A RID: 5978 RVA: 0x0006D34C File Offset: 0x0006B54C
		public override float GetScoreOfKingdomToGetClan(Kingdom kingdom, Clan clan)
		{
			float num = MathF.Min(2f, MathF.Max(0.33f, 1f + 0.02f * (float)FactionManager.GetRelationBetweenClans(kingdom.RulingClan, clan)));
			float num2 = 1f + ((kingdom.Culture == clan.Culture) ? 1f : 0f);
			int commanderLimit = clan.CommanderLimit;
			float num3 = (clan.CurrentTotalStrength + 150f * (float)commanderLimit) * 20f;
			float powerRatioToEnemies = FactionHelper.GetPowerRatioToEnemies(kingdom);
			float num4 = HeroHelper.CalculateReliabilityConstant(clan.Leader, 1f);
			float num5 = 1f / MathF.Max(0.4f, MathF.Min(2.5f, MathF.Sqrt(powerRatioToEnemies)));
			num3 *= num5;
			return (clan.CalculateTotalSettlementValueForFaction(kingdom) * 0.1f + num3) * num * num2 * num4;
		}

		// Token: 0x0600175B RID: 5979 RVA: 0x0006D420 File Offset: 0x0006B620
		public override float GetScoreOfKingdomToSackClan(Kingdom kingdom, Clan clan)
		{
			float num = MathF.Min(2f, MathF.Max(0.33f, 1f + 0.02f * (float)FactionManager.GetRelationBetweenClans(kingdom.RulingClan, clan)));
			float num2 = 1f + ((kingdom.Culture == clan.Culture) ? 1f : 0.5f);
			int commanderLimit = clan.CommanderLimit;
			float num3 = (clan.CurrentTotalStrength + 150f * (float)commanderLimit) * 20f;
			float num4 = clan.CalculateTotalSettlementValueForFaction(kingdom);
			return 10f - 1f * num3 * num2 * num - num4;
		}

		// Token: 0x0600175C RID: 5980 RVA: 0x0006D4B8 File Offset: 0x0006B6B8
		public override float GetScoreOfMercenaryToJoinKingdom(Clan mercenaryClan, Kingdom kingdom)
		{
			int num = ((mercenaryClan.Kingdom == kingdom) ? mercenaryClan.MercenaryAwardMultiplier : Campaign.Current.Models.MinorFactionsModel.GetMercenaryAwardFactorToJoinKingdom(mercenaryClan, kingdom, false));
			float num2 = mercenaryClan.CurrentTotalStrength + (float)mercenaryClan.CommanderLimit * 50f;
			int mercenaryAwardFactorToJoinKingdom = Campaign.Current.Models.MinorFactionsModel.GetMercenaryAwardFactorToJoinKingdom(mercenaryClan, kingdom, true);
			if (kingdom.Leader == Hero.MainHero)
			{
				return 0f;
			}
			return (float)(num - mercenaryAwardFactorToJoinKingdom) * num2 * 0.5f;
		}

		// Token: 0x0600175D RID: 5981 RVA: 0x0006D53C File Offset: 0x0006B73C
		public override float GetScoreOfMercenaryToLeaveKingdom(Clan mercenaryClan, Kingdom kingdom)
		{
			float num = 0.005f * MathF.Min(200f, mercenaryClan.LastFactionChangeTime.ElapsedDaysUntilNow);
			return 10000f * num - 5000f - this.GetScoreOfMercenaryToJoinKingdom(mercenaryClan, kingdom);
		}

		// Token: 0x0600175E RID: 5982 RVA: 0x0006D580 File Offset: 0x0006B780
		public override float GetScoreOfKingdomToHireMercenary(Kingdom kingdom, Clan mercenaryClan)
		{
			int num = 0;
			foreach (Clan clan in kingdom.Clans)
			{
				num += clan.CommanderLimit;
			}
			float num2 = (float)((num < 12) ? ((12 - num) * 100) : 0);
			int count = kingdom.Settlements.Count;
			int num3 = ((count < 40) ? ((40 - count) * 30) : 0);
			return num2 + (float)num3;
		}

		// Token: 0x0600175F RID: 5983 RVA: 0x0006D608 File Offset: 0x0006B808
		public override float GetScoreOfKingdomToSackMercenary(Kingdom kingdom, Clan mercenaryClan)
		{
			float b = (((float)kingdom.Leader.Gold > 20000f) ? (MathF.Sqrt((float)kingdom.Leader.Gold / 20000f) - 1f) : (-1f));
			int relationBetweenClans = FactionManager.GetRelationBetweenClans(kingdom.RulingClan, mercenaryClan);
			float num = MathF.Min(5f, FactionHelper.GetPowerRatioToEnemies(kingdom));
			return (MathF.Min(2f + (float)relationBetweenClans / 100f - num, b) * -1f - 0.1f) * 50f * mercenaryClan.CurrentTotalStrength * 5f;
		}

		// Token: 0x06001760 RID: 5984 RVA: 0x0006D6A0 File Offset: 0x0006B8A0
		public override float GetScoreOfDeclaringPeaceForClan(IFaction factionDeclaresPeace, IFaction factionDeclaredPeace, Clan evaluatingClan, out TextObject reason, bool includeReason = false)
		{
			reason = null;
			if (includeReason)
			{
				reason = this.GetReasonForDeclaringPeace(factionDeclaresPeace, factionDeclaredPeace, evaluatingClan);
			}
			float num = DefaultDiplomacyModel.GetExposureScoreToOtherFaction(factionDeclaresPeace, factionDeclaredPeace);
			if (num.ApproximatelyEqualsTo(-3.4028235E+38f, 1E-05f))
			{
				return 10000000f;
			}
			num = MathF.Min(num * 1.4f, DefaultDiplomacyModel.GetExposureScoreToOtherFaction(factionDeclaredPeace, factionDeclaresPeace));
			float num2;
			float num3;
			DefaultDiplomacyModel.GetBenefitAndRiskScoreForPeace(factionDeclaresPeace, factionDeclaredPeace, evaluatingClan, out num2, out num3);
			DefaultDiplomacyModel.UpdateOurBenefitMinusOurRiskBasedOnEvaluatingFaction(evaluatingClan, ref num2, ref num3);
			num3 = DefaultDiplomacyModel.ApplyWarProgressToRiskScore(factionDeclaresPeace, factionDeclaredPeace, num3);
			num2 *= DefaultDiplomacyModel.GetWarScale(factionDeclaresPeace, factionDeclaredPeace);
			float relationScore = DefaultDiplomacyModel.GetRelationScore(factionDeclaresPeace, factionDeclaredPeace, evaluatingClan);
			return (DefaultDiplomacyModel.GetSameCultureTownScore(factionDeclaresPeace, factionDeclaredPeace) + num2 * num - num3 + relationScore) * -1f;
		}

		// Token: 0x06001761 RID: 5985 RVA: 0x0006D73C File Offset: 0x0006B93C
		public override float GetScoreOfDeclaringPeace(IFaction factionDeclaresPeace, IFaction factionDeclaredPeace)
		{
			float num = DefaultDiplomacyModel.GetExposureScoreToOtherFaction(factionDeclaresPeace, factionDeclaredPeace);
			if (num.ApproximatelyEqualsTo(-3.4028235E+38f, 1E-05f))
			{
				return 10000000f;
			}
			num = MathF.Min(num * 1.4f, DefaultDiplomacyModel.GetExposureScoreToOtherFaction(factionDeclaredPeace, factionDeclaresPeace));
			float num2;
			float num3;
			DefaultDiplomacyModel.GetBenefitAndRiskScoreForPeace(factionDeclaresPeace, factionDeclaredPeace, factionDeclaresPeace.Leader.Clan, out num2, out num3);
			num3 = DefaultDiplomacyModel.ApplyWarProgressToRiskScore(factionDeclaresPeace, factionDeclaredPeace, num3);
			num2 *= DefaultDiplomacyModel.GetWarScale(factionDeclaresPeace, factionDeclaredPeace);
			return (DefaultDiplomacyModel.GetSameCultureTownScore(factionDeclaresPeace, factionDeclaredPeace) + num2 * num - num3) * -1f;
		}

		// Token: 0x06001762 RID: 5986 RVA: 0x0006D7BC File Offset: 0x0006B9BC
		private TextObject GetReasonForDeclaringPeace(IFaction factionDeclaresPeace, IFaction factionDeclaredPeace, Clan evaluatingClan)
		{
			if (DefaultDiplomacyModel.GetExposureScoreToOtherFaction(factionDeclaresPeace, factionDeclaredPeace).ApproximatelyEqualsTo(-3.4028235E+38f, 1E-05f))
			{
				return new TextObject("{=i0h0LKa0}Our borders are far from those of the enemy. It is too arduous to pursue this war.", null);
			}
			DefaultDiplomacyModel.WarStats warStats = DefaultDiplomacyModel.CalculateWarStatsForPeace(factionDeclaresPeace, factionDeclaredPeace, evaluatingClan);
			DefaultDiplomacyModel.WarStats warStats2 = DefaultDiplomacyModel.CalculateWarStatsForPeace(factionDeclaredPeace, factionDeclaresPeace, evaluatingClan);
			float num;
			float num2;
			DefaultDiplomacyModel.GetBenefitAndRiskScoreForPeace(factionDeclaresPeace, factionDeclaredPeace, evaluatingClan, out num, out num2);
			float num3 = DefaultDiplomacyModel.ApplyWarProgressToRiskScore(factionDeclaresPeace, factionDeclaredPeace, num2);
			TextObject textObject;
			if (num - num2 > 0f)
			{
				if (num - num3 < 0f)
				{
					textObject = new TextObject("{=QQtJobYP}We need time to recover from the hardships of war.", null);
				}
				else
				{
					textObject = new TextObject("{=vwjs6EjJ}On balance, the gains we stand to make are not worth the costs and risks.", null);
				}
			}
			else if (warStats.Strength < warStats2.Strength)
			{
				textObject = new TextObject("{=JOe3BC41}The {ENEMY_KINGDOM_INFORMAL_NAME} is currently more powerful than us. We need time to build up our strength.", null);
			}
			else if (warStats.Strength > warStats2.Strength && warStats.Strength < warStats2.Strength + warStats.TotalStrengthOfEnemies)
			{
				textObject = new TextObject("{=vwjs6EjJ}On balance, the gains we stand to make are not worth the costs and risks.", null);
			}
			else if (warStats.Strength < warStats2.Strength + warStats.TotalStrengthOfEnemies)
			{
				textObject = new TextObject("{=nuqv4GAA}We have too many enemies. We need to make peace with at least some of them.", null);
			}
			else
			{
				textObject = new TextObject("{=HqJSNG3M}Our realm is currently doing well, but we stand to lose this wealth if we go on fighting.", null);
			}
			if (!TextObject.IsNullOrEmpty(textObject))
			{
				textObject.SetTextVariable("ENEMY_KINGDOM_INFORMAL_NAME", factionDeclaredPeace.InformalName);
			}
			return textObject;
		}

		// Token: 0x06001763 RID: 5987 RVA: 0x0006D8EC File Offset: 0x0006BAEC
		public override ExplainedNumber GetWarProgressScore(IFaction factionDeclaresWar, IFaction factionDeclaredWar, bool includeDescriptions = false)
		{
			ExplainedNumber result = new ExplainedNumber(0f, includeDescriptions, null);
			StanceLink stanceWith = factionDeclaresWar.GetStanceWith(factionDeclaredWar);
			if (!stanceWith.IsAtWar)
			{
				return result;
			}
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			foreach (Town town in factionDeclaredWar.Fiefs)
			{
				if (town.IsTown)
				{
					num++;
				}
				else if (town.IsCastle)
				{
					num2++;
				}
				num3 += town.Villages.Count;
			}
			int num4 = factionDeclaredWar.WarPartyComponents.Sum((WarPartyComponent x) => x.Party.NumberOfAllMembers);
			int num5 = factionDeclaredWar.Fiefs.Sum(delegate(Town x)
			{
				MobileParty garrisonParty = x.GarrisonParty;
				if (garrisonParty == null)
				{
					return 0;
				}
				return garrisonParty.Party.NumberOfAllMembers;
			}) + num4;
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
			result.Add(value, new TextObject("{=FKe05WtJ}Kills", null), null);
			result.Add(value2, new TextObject("{=bVa5jNbd}Town Sieges", null), null);
			result.Add(value3, new TextObject("{=Sdu2FmgY}Castle Sieges", null), null);
			result.Add(value4, new TextObject("{=w6E2lb09}Raids", null), null);
			result.LimitMin(0f);
			result.LimitMax(750f, null);
			return result;
		}

		// Token: 0x06001764 RID: 5988 RVA: 0x0006DB3C File Offset: 0x0006BD3C
		private static float GetWarScale(IFaction factionDeclaresWar, IFaction factionDeclaredWar)
		{
			StanceLink stanceWith = factionDeclaresWar.GetStanceWith(factionDeclaredWar);
			if (!stanceWith.IsAtWar)
			{
				return 1f;
			}
			int casualties = stanceWith.GetCasualties(factionDeclaredWar);
			int casualties2 = stanceWith.GetCasualties(factionDeclaresWar);
			int num = MathF.Max(1, (int)stanceWith.WarStartDate.ElapsedDaysUntilNow);
			if (num <= 20)
			{
				return 1f;
			}
			float num2 = (float)MathF.Max(casualties + casualties2, 1) / (20f * MathF.Pow((float)num, 1.5f));
			if (num2 >= 1f || num2 <= 0f)
			{
				return 1f;
			}
			return num2;
		}

		// Token: 0x06001765 RID: 5989 RVA: 0x0006DBCC File Offset: 0x0006BDCC
		public override float GetScoreOfDeclaringWar(IFaction factionDeclaresWar, IFaction factionDeclaredWar, Clan evaluatingClan, out TextObject reason, bool includeReason = false)
		{
			reason = null;
			if (includeReason)
			{
				reason = this.GetReasonForDeclaringWar(factionDeclaresWar, factionDeclaredWar, evaluatingClan);
			}
			if (factionDeclaresWar.CurrentTotalStrength <= 500f || factionDeclaresWar.WarPartyComponents.Count < 2)
			{
				return -10000000f;
			}
			float exposureScoreToOtherFaction = DefaultDiplomacyModel.GetExposureScoreToOtherFaction(factionDeclaresWar, factionDeclaredWar);
			if (exposureScoreToOtherFaction.ApproximatelyEqualsTo(-3.4028235E+38f, 1E-05f))
			{
				return -10000000f;
			}
			float num;
			float num2;
			DefaultDiplomacyModel.GetBenefitAndRiskScoreForWar(factionDeclaresWar, factionDeclaredWar, evaluatingClan, out num, out num2);
			float relationScore = DefaultDiplomacyModel.GetRelationScore(factionDeclaresWar, factionDeclaredWar, evaluatingClan);
			float sameCultureTownScore = DefaultDiplomacyModel.GetSameCultureTownScore(factionDeclaresWar, factionDeclaredWar);
			float allianceFactor = DefaultDiplomacyModel.GetAllianceFactor(factionDeclaresWar, factionDeclaredWar);
			return sameCultureTownScore + num * exposureScoreToOtherFaction * allianceFactor - num2 + relationScore;
		}

		// Token: 0x06001766 RID: 5990 RVA: 0x0006DC60 File Offset: 0x0006BE60
		private static float GetAllianceFactor(IFaction factionDeclaresWar, IFaction factionDeclaredWar)
		{
			if (factionDeclaresWar.IsKingdomFaction && factionDeclaredWar.IsKingdomFaction)
			{
				bool flag = false;
				float num = 1f;
				Kingdom kingdom = (Kingdom)factionDeclaresWar;
				Kingdom kingdom2 = (Kingdom)factionDeclaredWar;
				foreach (Kingdom kingdom3 in kingdom.AlliedKingdoms)
				{
					if (kingdom3 == kingdom2)
					{
						num *= 1f - Campaign.Current.Models.DiplomacyModel.WarDeclarationScorePenaltyAgainstAllies;
						break;
					}
					if (!flag && kingdom3.IsAtWarWith(kingdom2))
					{
						num *= 1f + Campaign.Current.Models.DiplomacyModel.WarDeclarationScoreBonusAgainstEnemiesOfAllies;
						flag = true;
					}
				}
				return num;
			}
			return 1f;
		}

		// Token: 0x06001767 RID: 5991 RVA: 0x0006DD30 File Offset: 0x0006BF30
		private TextObject GetReasonForDeclaringWar(IFaction factionDeclaresWar, IFaction factionDeclaredWar, Clan evaluatingClan)
		{
			DefaultDiplomacyModel.WarStats warStats = DefaultDiplomacyModel.CalculateWarStatsForWar(factionDeclaresWar, factionDeclaredWar, evaluatingClan);
			DefaultDiplomacyModel.WarStats warStats2 = DefaultDiplomacyModel.CalculateWarStatsForWar(factionDeclaredWar, factionDeclaresWar, evaluatingClan);
			float num;
			float num2;
			DefaultDiplomacyModel.GetBenefitAndRiskScoreForWar(factionDeclaresWar, factionDeclaredWar, evaluatingClan, out num, out num2);
			float relationScore = DefaultDiplomacyModel.GetRelationScore(factionDeclaresWar, factionDeclaredWar, evaluatingClan);
			float sameCultureTownScore = DefaultDiplomacyModel.GetSameCultureTownScore(factionDeclaresWar, factionDeclaredWar);
			if (factionDeclaresWar.CurrentTotalStrength <= 500f || factionDeclaresWar.WarPartyComponents.Count < 2)
			{
				return new TextObject("{=JOe3BC41}The {ENEMY_KINGDOM_INFORMAL_NAME} is currently more powerful than us. We need time to build up our strength.", null);
			}
			if (DefaultDiplomacyModel.GetExposureScoreToOtherFaction(factionDeclaresWar, factionDeclaredWar).ApproximatelyEqualsTo(-3.4028235E+38f, 1E-05f))
			{
				return new TextObject("{=i0h0LKa0}Our borders are far from those of the enemy. It is too arduous to pursue this war.", null);
			}
			TextObject textObject;
			if (num - num2 > 0f)
			{
				if (relationScore > num - num2 && relationScore > sameCultureTownScore)
				{
					textObject = new TextObject("{=dov3iRlt}{ENEMY_RULER.NAME} of the {ENEMY_KINGDOM_INFORMAL_NAME} is vile and dangerous. We must deal with {?ENEMY_RULER.GENDER}her{?}him{\\?} before it is too late.", null);
				}
				else if (sameCultureTownScore > num - num2)
				{
					textObject = new TextObject("{=79lEPn1u}The {ENEMY_KINGDOM_INFORMAL_NAME} have occupied our ancestral lands and they oppress our kinfolk.", null);
				}
				else if (warStats.Strength > warStats2.Strength)
				{
					textObject = new TextObject("{=az3K3j4C}Right now we are stronger than the {ENEMY_KINGDOM_INFORMAL_NAME}. We should strike while we can.", null);
				}
				else
				{
					textObject = new TextObject("{=1aQAmENB}The {ENEMY_KINGDOM_INFORMAL_NAME} may be strong, but their lands are rich and ripe for the taking.", null);
				}
			}
			else if (relationScore > sameCultureTownScore)
			{
				textObject = new TextObject("{=dov3iRlt}{ENEMY_RULER.NAME} of the {ENEMY_KINGDOM_INFORMAL_NAME} is vile and dangerous. We must deal with {?ENEMY_RULER.GENDER}her{?}him{\\?} before it is too late.", null);
			}
			else
			{
				textObject = new TextObject("{=79lEPn1u}The {ENEMY_KINGDOM_INFORMAL_NAME} have occupied our ancestral lands and they oppress our kinfolk.", null);
			}
			if (!TextObject.IsNullOrEmpty(textObject))
			{
				textObject.SetTextVariable("ENEMY_KINGDOM_INFORMAL_NAME", factionDeclaredWar.InformalName);
				textObject.SetCharacterProperties("ENEMY_RULER", factionDeclaredWar.Leader.CharacterObject, false);
			}
			return textObject;
		}

		// Token: 0x06001768 RID: 5992 RVA: 0x0006DE7C File Offset: 0x0006C07C
		private static float ApplyWarProgressToRiskScore(IFaction factionDeclaresPeace, IFaction factionDeclaredPeace, float riskScore)
		{
			float resultNumber = Campaign.Current.Models.DiplomacyModel.GetWarProgressScore(factionDeclaresPeace, factionDeclaredPeace, false).ResultNumber;
			float resultNumber2 = Campaign.Current.Models.DiplomacyModel.GetWarProgressScore(factionDeclaredPeace, factionDeclaresPeace, false).ResultNumber;
			float num = MathF.Abs(resultNumber2 - resultNumber);
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

		// Token: 0x06001769 RID: 5993 RVA: 0x0006DF18 File Offset: 0x0006C118
		private static void GetBenefitAndRiskScoreForPeace(IFaction factionDeclaresPeace, IFaction factionDeclaredPeace, IFaction evaluatingFaction, out float benefitScore, out float riskScore)
		{
			DefaultDiplomacyModel.WarStats warStats = DefaultDiplomacyModel.CalculateWarStatsForPeace(factionDeclaresPeace, factionDeclaredPeace, evaluatingFaction);
			DefaultDiplomacyModel.WarStats warStats2 = DefaultDiplomacyModel.CalculateWarStatsForPeace(factionDeclaredPeace, factionDeclaresPeace, evaluatingFaction);
			benefitScore = DefaultDiplomacyModel.CalculateBenefitScore(warStats, warStats2);
			riskScore = DefaultDiplomacyModel.CalculateRiskScore(warStats, warStats2);
			riskScore = MathF.Min(warStats2.ValueOfSettlements * 0.75f, riskScore);
			benefitScore = MathF.Min(warStats.ValueOfSettlements * 1.5f, benefitScore);
		}

		// Token: 0x0600176A RID: 5994 RVA: 0x0006DF78 File Offset: 0x0006C178
		private static void GetBenefitAndRiskScoreForWar(IFaction factionDeclaresWar, IFaction factionDeclaredWar, IFaction evaluatingFaction, out float benefitScore, out float riskScore)
		{
			DefaultDiplomacyModel.WarStats faction1Stats = DefaultDiplomacyModel.CalculateWarStatsForWar(factionDeclaresWar, factionDeclaredWar, evaluatingFaction);
			DefaultDiplomacyModel.WarStats faction2Stats = DefaultDiplomacyModel.CalculateWarStatsForWar(factionDeclaredWar, factionDeclaresWar, evaluatingFaction);
			benefitScore = DefaultDiplomacyModel.CalculateBenefitScore(faction1Stats, faction2Stats);
			riskScore = DefaultDiplomacyModel.CalculateRiskScore(faction1Stats, faction2Stats);
			DefaultDiplomacyModel.ApplyTributeEffectToBenefitScoreForWar(factionDeclaresWar, factionDeclaredWar, evaluatingFaction, ref benefitScore);
			DefaultDiplomacyModel.UpdateOurBenefitMinusOurRiskBasedOnEvaluatingFaction(evaluatingFaction, ref benefitScore, ref riskScore);
		}

		// Token: 0x0600176B RID: 5995 RVA: 0x0006DFBC File Offset: 0x0006C1BC
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
				return;
			}
			if (dailyTributeToPay2 > 0)
			{
				float num3 = factionDeclaredWar.Fiefs.Sum((Town x) => x.Prosperity) + 1f;
				float num4 = 1f + (float)dailyTributeToPay2 / num3;
				benefitScore = (flag ? (benefitScore * num4) : (benefitScore / num4));
			}
		}

		// Token: 0x0600176C RID: 5996 RVA: 0x0006E0B8 File Offset: 0x0006C2B8
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
			foreach (TroopRosterElement troopRosterElement in party.MemberRoster.GetTroopRoster())
			{
				num3 += MathF.Min(1000f, 10f * (float)troopRosterElement.Character.Level * MathF.Sqrt((float)troopRosterElement.Character.Level));
			}
			float num4 = 0f;
			foreach (TroopRosterElement troopRosterElement2 in partyToLetGo.MemberRoster.GetTroopRoster())
			{
				num4 += MathF.Min(1000f, 10f * (float)troopRosterElement2.Character.Level * MathF.Sqrt((float)troopRosterElement2.Character.Level));
			}
			float num5 = 0f;
			foreach (TroopRosterElement troopRosterElement3 in partyToLetGo.MemberRoster.GetTroopRoster())
			{
				if (troopRosterElement3.Character.IsHero)
				{
					num5 += 500f;
				}
				num5 += (float)Campaign.Current.Models.RansomValueCalculationModel.PrisonerRansomValue(troopRosterElement3.Character, partyToLetGo.LeaderHero) * 0.3f;
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

		// Token: 0x0600176D RID: 5997 RVA: 0x0006E3B4 File Offset: 0x0006C5B4
		public override float GetValueOfHeroForFaction(Hero examinedHero, IFaction targetFaction, bool forMarriage = false)
		{
			return this.GetHeroCommandingStrengthForClan(examinedHero) * 10f;
		}

		// Token: 0x0600176E RID: 5998 RVA: 0x0006E3C3 File Offset: 0x0006C5C3
		public override int GetRelationCostOfExpellingClanFromKingdom()
		{
			return -20;
		}

		// Token: 0x0600176F RID: 5999 RVA: 0x0006E3C7 File Offset: 0x0006C5C7
		public override int GetInfluenceCostOfSupportingClan()
		{
			return 50;
		}

		// Token: 0x06001770 RID: 6000 RVA: 0x0006E3CC File Offset: 0x0006C5CC
		public override int GetInfluenceCostOfExpellingClan(Clan proposingClan)
		{
			ExplainedNumber explainedNumber = new ExplainedNumber(200f, false, null);
			this.GetPerkEffectsOnKingdomDecisionInfluenceCost(proposingClan, ref explainedNumber);
			return MathF.Round(explainedNumber.ResultNumber);
		}

		// Token: 0x06001771 RID: 6001 RVA: 0x0006E3FC File Offset: 0x0006C5FC
		public override int GetInfluenceCostOfProposingPeace(Clan proposingClan)
		{
			ExplainedNumber explainedNumber = new ExplainedNumber(100f, false, null);
			this.GetPerkEffectsOnKingdomDecisionInfluenceCost(proposingClan, ref explainedNumber);
			return MathF.Round(explainedNumber.ResultNumber);
		}

		// Token: 0x06001772 RID: 6002 RVA: 0x0006E42C File Offset: 0x0006C62C
		public override int GetInfluenceCostOfProposingWar(Clan proposingClan)
		{
			ExplainedNumber explainedNumber = new ExplainedNumber(200f, false, null);
			if (proposingClan.Kingdom.ActivePolicies.Contains(DefaultPolicies.WarTax) && proposingClan == proposingClan.Kingdom.RulingClan)
			{
				explainedNumber.AddFactor(1f, null);
			}
			this.GetPerkEffectsOnKingdomDecisionInfluenceCost(proposingClan, ref explainedNumber);
			return MathF.Round(explainedNumber.ResultNumber);
		}

		// Token: 0x06001773 RID: 6003 RVA: 0x0006E48E File Offset: 0x0006C68E
		public override int GetInfluenceValueOfSupportingClan()
		{
			return this.GetInfluenceCostOfSupportingClan() / 4;
		}

		// Token: 0x06001774 RID: 6004 RVA: 0x0006E498 File Offset: 0x0006C698
		public override int GetRelationValueOfSupportingClan()
		{
			return 1;
		}

		// Token: 0x06001775 RID: 6005 RVA: 0x0006E49C File Offset: 0x0006C69C
		public override int GetInfluenceCostOfAnnexation(Clan proposingClan)
		{
			ExplainedNumber explainedNumber = new ExplainedNumber(200f, false, null);
			if (proposingClan.Kingdom != null)
			{
				if (proposingClan.Kingdom.ActivePolicies.Contains(DefaultPolicies.FeudalInheritance))
				{
					explainedNumber.AddFactor(1f, null);
				}
				if (proposingClan.Kingdom.ActivePolicies.Contains(DefaultPolicies.PrecarialLandTenure) && proposingClan == proposingClan.Kingdom.RulingClan)
				{
					explainedNumber.AddFactor(-0.5f, null);
				}
			}
			this.GetPerkEffectsOnKingdomDecisionInfluenceCost(proposingClan, ref explainedNumber);
			return MathF.Round(explainedNumber.ResultNumber);
		}

		// Token: 0x06001776 RID: 6006 RVA: 0x0006E52A File Offset: 0x0006C72A
		public override int GetInfluenceCostOfChangingLeaderOfArmy()
		{
			return 30;
		}

		// Token: 0x06001777 RID: 6007 RVA: 0x0006E530 File Offset: 0x0006C730
		public override int GetInfluenceCostOfDisbandingArmy()
		{
			int num = 30;
			if (Clan.PlayerClan.Kingdom != null && Clan.PlayerClan == Clan.PlayerClan.Kingdom.RulingClan)
			{
				num /= 2;
			}
			return num;
		}

		// Token: 0x06001778 RID: 6008 RVA: 0x0006E567 File Offset: 0x0006C767
		public override int GetRelationCostOfDisbandingArmy(bool isLeaderParty)
		{
			if (!isLeaderParty)
			{
				return -1;
			}
			return -4;
		}

		// Token: 0x06001779 RID: 6009 RVA: 0x0006E570 File Offset: 0x0006C770
		public override int GetInfluenceCostOfPolicyProposalAndDisavowal(Clan proposerClan)
		{
			ExplainedNumber explainedNumber = new ExplainedNumber(100f, false, null);
			this.GetPerkEffectsOnKingdomDecisionInfluenceCost(proposerClan, ref explainedNumber);
			return MathF.Round(explainedNumber.ResultNumber);
		}

		// Token: 0x0600177A RID: 6010 RVA: 0x0006E5A0 File Offset: 0x0006C7A0
		public override int GetInfluenceCostOfAbandoningArmy()
		{
			return 2;
		}

		// Token: 0x0600177B RID: 6011 RVA: 0x0006E5A3 File Offset: 0x0006C7A3
		private void GetPerkEffectsOnKingdomDecisionInfluenceCost(Clan proposingClan, ref ExplainedNumber cost)
		{
			if (proposingClan.Leader.GetPerkValue(DefaultPerks.Charm.Firebrand))
			{
				cost.AddFactor(DefaultPerks.Charm.Firebrand.PrimaryBonus, DefaultPerks.Charm.Firebrand.Name);
			}
		}

		// Token: 0x0600177C RID: 6012 RVA: 0x0006E5D1 File Offset: 0x0006C7D1
		private int GetBaseRelationBetweenHeroes(Hero hero1, Hero hero2)
		{
			return CharacterRelationManager.GetHeroRelation(hero1, hero2);
		}

		// Token: 0x0600177D RID: 6013 RVA: 0x0006E5DA File Offset: 0x0006C7DA
		public override int GetBaseRelation(Hero hero1, Hero hero2)
		{
			return this.GetBaseRelationBetweenHeroes(hero1, hero2);
		}

		// Token: 0x0600177E RID: 6014 RVA: 0x0006E5E4 File Offset: 0x0006C7E4
		public override int GetEffectiveRelation(Hero hero1, Hero hero2)
		{
			Hero hero3;
			Hero hero4;
			this.GetHeroesForEffectiveRelation(hero1, hero2, out hero3, out hero4);
			if (hero3 == null || hero4 == null)
			{
				return 0;
			}
			int baseRelationBetweenHeroes = this.GetBaseRelationBetweenHeroes(hero3, hero4);
			this.GetPersonalityEffects(ref baseRelationBetweenHeroes, hero1, hero4);
			return MBMath.ClampInt(baseRelationBetweenHeroes, this.MinRelationLimit, this.MaxRelationLimit);
		}

		// Token: 0x0600177F RID: 6015 RVA: 0x0006E62C File Offset: 0x0006C82C
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

		// Token: 0x06001780 RID: 6016 RVA: 0x0006E698 File Offset: 0x0006C898
		public override int GetRelationChangeAfterClanLeaderIsDead(Hero deadLeader, Hero relationHero)
		{
			return (int)((float)CharacterRelationManager.GetHeroRelation(deadLeader, relationHero) * 0.7f);
		}

		// Token: 0x06001781 RID: 6017 RVA: 0x0006E6AC File Offset: 0x0006C8AC
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

		// Token: 0x06001782 RID: 6018 RVA: 0x0006E6E7 File Offset: 0x0006C8E7
		private void GetPersonalityEffects(ref int effectiveRelation, Hero hero1, Hero effectiveHero2)
		{
			this.GetTraitEffect(ref effectiveRelation, hero1, effectiveHero2, DefaultTraits.Honor, 2);
			this.GetTraitEffect(ref effectiveRelation, hero1, effectiveHero2, DefaultTraits.Valor, 1);
			this.GetTraitEffect(ref effectiveRelation, hero1, effectiveHero2, DefaultTraits.Mercy, 1);
		}

		// Token: 0x06001783 RID: 6019 RVA: 0x0006E718 File Offset: 0x0006C918
		private void GetTraitEffect(ref int effectiveRelation, Hero hero1, Hero effectiveHero2, TraitObject trait, int effectMagnitude)
		{
			int traitLevel = hero1.GetTraitLevel(trait);
			int traitLevel2 = effectiveHero2.GetTraitLevel(trait);
			int num = traitLevel * traitLevel2;
			if (num > 0)
			{
				effectiveRelation += effectMagnitude;
				return;
			}
			if (num < 0)
			{
				effectiveRelation -= effectMagnitude;
			}
		}

		// Token: 0x06001784 RID: 6020 RVA: 0x0006E750 File Offset: 0x0006C950
		public override int GetCharmExperienceFromRelationGain(Hero hero, float relationChange, ChangeRelationAction.ChangeRelationDetail detail)
		{
			float num = 20f;
			if (detail != ChangeRelationAction.ChangeRelationDetail.Emissary)
			{
				if (!hero.IsNotable)
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
				else
				{
					num *= 10f;
				}
			}
			else
			{
				num *= 20f;
			}
			return MathF.Round(num * relationChange);
		}

		// Token: 0x06001785 RID: 6021 RVA: 0x0006E814 File Offset: 0x0006CA14
		public override uint GetNotificationColor(ChatNotificationType notificationType)
		{
			switch (notificationType)
			{
			case ChatNotificationType.Default:
				return 10066329U;
			case ChatNotificationType.PlayerFactionPositive:
				return 2284902U;
			case ChatNotificationType.PlayerClanPositive:
				return 3407803U;
			case ChatNotificationType.PlayerFactionNegative:
				return 14509602U;
			case ChatNotificationType.PlayerClanNegative:
				return 16750899U;
			case ChatNotificationType.Civilian:
				return 10053324U;
			case ChatNotificationType.PlayerClanCivilian:
				return 15623935U;
			case ChatNotificationType.PlayerFactionCivilian:
				return 11163101U;
			case ChatNotificationType.Neutral:
				return 12303291U;
			case ChatNotificationType.PlayerFactionIndirectPositive:
				return 12298820U;
			case ChatNotificationType.PlayerFactionIndirectNegative:
				return 13382502U;
			case ChatNotificationType.PlayerClanPolitical:
				return 6745855U;
			case ChatNotificationType.PlayerFactionPolitical:
				return 5614301U;
			case ChatNotificationType.Political:
				return 6724044U;
			default:
				return 13369548U;
			}
		}

		// Token: 0x06001786 RID: 6022 RVA: 0x0006E8BA File Offset: 0x0006CABA
		public override float DenarsToInfluence()
		{
			return 0.002f;
		}

		// Token: 0x06001787 RID: 6023 RVA: 0x0006E8C1 File Offset: 0x0006CAC1
		public override float GetDecisionMakingThreshold(IFaction consideringFaction)
		{
			return Campaign.Current.Models.DiplomacyModel.GetValueOfSettlementsForFaction(consideringFaction) / 6f;
		}

		// Token: 0x06001788 RID: 6024 RVA: 0x0006E8DE File Offset: 0x0006CADE
		public override bool CanSettlementBeGifted(Settlement settlementToGift)
		{
			return settlementToGift.Town != null && !settlementToGift.Town.IsOwnerUnassigned;
		}

		// Token: 0x06001789 RID: 6025 RVA: 0x0006E8F8 File Offset: 0x0006CAF8
		public override float GetValueOfSettlementsForFaction(IFaction faction)
		{
			float num = 0f;
			float num2 = 0f;
			foreach (Town town in faction.Fiefs)
			{
				if (town.IsTown)
				{
					num += 2000f;
				}
				else
				{
					num += 1000f;
				}
				num += town.Prosperity * 0.33f;
				num2 += (float)town.Villages.Count * 300f;
			}
			num *= 50f;
			num += num2 * 25f;
			num = DefaultDiplomacyModel.AdjustValueOfSettlements(num);
			return num;
		}

		// Token: 0x0600178A RID: 6026 RVA: 0x0006E9A8 File Offset: 0x0006CBA8
		public override IEnumerable<BarterGroup> GetBarterGroups()
		{
			return new BarterGroup[]
			{
				new GoldBarterGroup(),
				new ItemBarterGroup(),
				new PrisonerBarterGroup(),
				new FiefBarterGroup(),
				new OtherBarterGroup(),
				new DefaultsBarterGroup()
			};
		}

		// Token: 0x0600178B RID: 6027 RVA: 0x0006E9E0 File Offset: 0x0006CBE0
		public override bool IsPeaceSuitable(IFaction factionDeclaresPeace, IFaction factionDeclaredPeace)
		{
			if (factionDeclaresPeace.IsEliminated || factionDeclaredPeace.IsEliminated)
			{
				return false;
			}
			float scoreOfDeclaringPeace = Campaign.Current.Models.DiplomacyModel.GetScoreOfDeclaringPeace(factionDeclaresPeace, factionDeclaredPeace);
			float scoreOfDeclaringPeace2 = Campaign.Current.Models.DiplomacyModel.GetScoreOfDeclaringPeace(factionDeclaredPeace, factionDeclaresPeace);
			float valueOfSettlementsForFaction = Campaign.Current.Models.DiplomacyModel.GetValueOfSettlementsForFaction(factionDeclaresPeace);
			float num;
			if (scoreOfDeclaringPeace2 > 0f)
			{
				num = scoreOfDeclaringPeace2 - scoreOfDeclaringPeace;
			}
			else
			{
				num = Campaign.Current.Models.DiplomacyModel.GetDecisionMakingThreshold(factionDeclaredPeace) - scoreOfDeclaringPeace2;
			}
			return num <= valueOfSettlementsForFaction || factionDeclaresPeace.GetStanceWith(factionDeclaredPeace).WarStartDate.ElapsedDaysUntilNow >= 150f;
		}

		// Token: 0x0600178C RID: 6028 RVA: 0x0006EA8C File Offset: 0x0006CC8C
		public override int GetDailyTributeToPay(Clan factionToPay, Clan factionToReceive, out int tributeDurationInDays)
		{
			float scoreOfDeclaringPeace = Campaign.Current.Models.DiplomacyModel.GetScoreOfDeclaringPeace(factionToReceive.MapFaction, factionToPay.MapFaction);
			float scoreOfDeclaringPeace2 = Campaign.Current.Models.DiplomacyModel.GetScoreOfDeclaringPeace(factionToPay.MapFaction, factionToReceive.MapFaction);
			float valueOfSettlementsForFaction = Campaign.Current.Models.DiplomacyModel.GetValueOfSettlementsForFaction(factionToPay.MapFaction);
			float num;
			if (scoreOfDeclaringPeace > 0f)
			{
				num = scoreOfDeclaringPeace - scoreOfDeclaringPeace2;
			}
			else
			{
				num = Campaign.Current.Models.DiplomacyModel.GetDecisionMakingThreshold(factionToReceive.MapFaction) - scoreOfDeclaringPeace;
			}
			float resultNumber = Campaign.Current.Models.DiplomacyModel.GetWarProgressScore(factionToPay.MapFaction, factionToReceive.MapFaction, false).ResultNumber;
			float resultNumber2 = Campaign.Current.Models.DiplomacyModel.GetWarProgressScore(factionToReceive.MapFaction, factionToPay.MapFaction, false).ResultNumber;
			float num2 = MathF.Abs(resultNumber - resultNumber2);
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
				if (num3 < 0.05f)
				{
					num3 = 0f;
				}
				else if (num3 > 0.05f && num3 < 0.1f)
				{
					num3 = 0.05f;
				}
				else if (num3 > 0.1f && num3 < 0.15f)
				{
					num3 = 0.1f;
				}
				else
				{
					num3 = 0.15f;
				}
			}
			int num4 = (int)(num3 * factionToPay.MapFaction.Fiefs.Sum((Town x) => x.Prosperity) * 0.35f);
			num4 = 10 * (num4 / 10);
			tributeDurationInDays = ((num4 == 0) ? 0 : 100);
			return num4;
		}

		// Token: 0x0600178D RID: 6029 RVA: 0x0006EC53 File Offset: 0x0006CE53
		public override bool IsClanEligibleToBecomeRuler(Clan clan)
		{
			return !clan.IsEliminated && clan.Leader.IsAlive && !clan.IsUnderMercenaryService;
		}

		// Token: 0x0600178E RID: 6030 RVA: 0x0006EC78 File Offset: 0x0006CE78
		public override DiplomacyModel.DiplomacyStance? GetShallowDiplomaticStance(IFaction faction1, IFaction faction2)
		{
			if (faction1.IsBanditFaction != faction2.IsBanditFaction)
			{
				return new DiplomacyModel.DiplomacyStance?(DiplomacyModel.DiplomacyStance.War);
			}
			return null;
		}

		// Token: 0x0600178F RID: 6031 RVA: 0x0006ECA3 File Offset: 0x0006CEA3
		public override DiplomacyModel.DiplomacyStance GetDefaultDiplomaticStance(IFaction faction1, IFaction faction2)
		{
			if (this.IsAtConstantWar(faction1, faction2))
			{
				return DiplomacyModel.DiplomacyStance.War;
			}
			return DiplomacyModel.DiplomacyStance.Neutral;
		}

		// Token: 0x06001790 RID: 6032 RVA: 0x0006ECB4 File Offset: 0x0006CEB4
		public override bool IsAtConstantWar(IFaction faction1, IFaction faction2)
		{
			if (((faction1.IsOutlaw && faction1.IsMinorFaction && faction2.IsKingdomFaction) || (faction2.IsOutlaw && faction2.IsMinorFaction && faction1.IsKingdomFaction)) && faction1.Culture == faction2.Culture)
			{
				return true;
			}
			DiplomacyModel.DiplomacyStance? shallowDiplomaticStance = this.GetShallowDiplomaticStance(faction1, faction2);
			DiplomacyModel.DiplomacyStance diplomacyStance = DiplomacyModel.DiplomacyStance.War;
			return (shallowDiplomaticStance.GetValueOrDefault() == diplomacyStance) & (shallowDiplomaticStance != null);
		}

		// Token: 0x06001791 RID: 6033 RVA: 0x0006ED24 File Offset: 0x0006CF24
		private static DefaultDiplomacyModel.WarStats CalculateWarStatsForPeace(IFaction faction, IFaction targetFaction, IFaction evaluatingFaction)
		{
			float num = 0f;
			float num2 = 0f;
			bool flag = evaluatingFaction.MapFaction == faction.MapFaction;
			float val = faction.WarPartyComponents.Sum((WarPartyComponent x) => x.Party.EstimatedStrength);
			float num3;
			if (!flag)
			{
				num3 = faction.Fiefs.Sum(delegate(Town x)
				{
					MobileParty garrisonParty = x.GarrisonParty;
					if (garrisonParty == null)
					{
						return 0f;
					}
					return garrisonParty.Party.EstimatedStrength;
				}) * 0.7f;
			}
			else
			{
				num3 = faction.Fiefs.Sum(delegate(Town x)
				{
					MobileParty garrisonParty = x.GarrisonParty;
					if (garrisonParty == null)
					{
						return 0f;
					}
					return garrisonParty.Party.EstimatedStrength;
				});
			}
			float num4 = num3;
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
			num += num4 + Math.Max(val, num2);
			float num5 = 0f;
			IEnumerable<IFaction> factionsAtWarWith = faction.FactionsAtWarWith;
			Func<IFaction, bool> <>9__3;
			Func<IFaction, bool> predicate;
			if ((predicate = <>9__3) == null)
			{
				predicate = (<>9__3 = (IFaction x) => x != targetFaction);
			}
			foreach (IFaction faction2 in factionsAtWarWith.Where(predicate))
			{
				float num6 = 0f;
				if (!faction2.IsBanditFaction && (!faction2.IsMinorFaction || faction2.Leader == Hero.MainHero) && faction2.IsKingdomFaction)
				{
					int num7 = 0;
					foreach (Clan clan2 in from x in ((Kingdom)faction2).Clans
						where !x.IsUnderMercenaryService
						select x)
					{
						num7 += Campaign.Current.Models.ClanTierModel.GetPartyLimitForTier(clan2, clan2.Tier);
					}
					num6 += (float)(num7 * 64);
					float val2 = faction2.WarPartyComponents.Sum((WarPartyComponent x) => x.Party.EstimatedStrength);
					num5 += Math.Max(val2, num6);
				}
			}
			return new DefaultDiplomacyModel.WarStats
			{
				Strength = num,
				ValueOfSettlements = Campaign.Current.Models.DiplomacyModel.GetValueOfSettlementsForFaction(faction),
				TotalStrengthOfEnemies = (flag ? (num5 * 0.6f) : num5)
			};
		}

		// Token: 0x06001792 RID: 6034 RVA: 0x0006F054 File Offset: 0x0006D254
		private static DefaultDiplomacyModel.WarStats CalculateWarStatsForWar(IFaction faction, IFaction targetFaction, IFaction evaluatingFaction)
		{
			float val = faction.WarPartyComponents.Sum((WarPartyComponent x) => x.Party.EstimatedStrength);
			float num = faction.Fiefs.Sum(delegate(Town x)
			{
				MobileParty garrisonParty = x.GarrisonParty;
				if (garrisonParty == null)
				{
					return 0f;
				}
				return garrisonParty.Party.EstimatedStrength;
			});
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
			IEnumerable<IFaction> factionsAtWarWith = faction.FactionsAtWarWith;
			Func<IFaction, bool> <>9__2;
			Func<IFaction, bool> predicate;
			if ((predicate = <>9__2) == null)
			{
				predicate = (<>9__2 = (IFaction x) => x.MapFaction != targetFaction.MapFaction);
			}
			foreach (IFaction faction2 in factionsAtWarWith.Where(predicate))
			{
				if (!faction2.IsBanditFaction && (!faction2.IsMinorFaction || faction2.Leader == Hero.MainHero) && faction2.IsKingdomFaction)
				{
					int num6 = 0;
					foreach (Clan clan2 in from x in ((Kingdom)faction2).Clans
						where !x.IsUnderMercenaryService
						select x)
					{
						num6 += Campaign.Current.Models.ClanTierModel.GetPartyLimitForTier(clan2, clan2.Tier);
					}
					num5 += (float)(num6 * 64);
					float val2 = faction2.WarPartyComponents.Sum((WarPartyComponent x) => x.Party.EstimatedStrength);
					float num7 = faction2.Fiefs.Sum(delegate(Town x)
					{
						MobileParty garrisonParty = x.GarrisonParty;
						if (garrisonParty == null)
						{
							return 0f;
						}
						return garrisonParty.Party.EstimatedStrength;
					}) + Math.Max(num5, val2);
					num4 += num7;
				}
			}
			return new DefaultDiplomacyModel.WarStats
			{
				Strength = num2,
				ValueOfSettlements = Campaign.Current.Models.DiplomacyModel.GetValueOfSettlementsForFaction(faction),
				TotalStrengthOfEnemies = (flag ? (num4 * 1.1f) : num4)
			};
		}

		// Token: 0x06001793 RID: 6035 RVA: 0x0006F380 File Offset: 0x0006D580
		private static float GetExposureScoreToOtherFaction(IFaction factionDeclaresWar, IFaction factionDeclaredWar)
		{
			HashSet<Settlement> hashSet = new HashSet<Settlement>();
			float num = 0f;
			float num2 = 0f;
			if (factionDeclaresWar.Fiefs.Count == 0 || factionDeclaredWar.Fiefs.Count == 0)
			{
				return 1f;
			}
			foreach (Town town in factionDeclaresWar.Fiefs)
			{
				foreach (Settlement settlement in town.GetNeighborFortifications(MobileParty.NavigationType.All))
				{
					if (settlement.MapFaction != factionDeclaresWar && !hashSet.Contains(settlement))
					{
						if (settlement.MapFaction == factionDeclaredWar)
						{
							num2 += 1f;
						}
						num += 1f;
						hashSet.Add(settlement);
					}
				}
			}
			HashSet<Settlement> hashSet2 = new HashSet<Settlement>();
			foreach (Settlement settlement2 in hashSet)
			{
				foreach (Settlement settlement3 in settlement2.Town.GetNeighborFortifications(MobileParty.NavigationType.All))
				{
					if (settlement3.MapFaction != factionDeclaresWar && !hashSet.Contains(settlement3) && !hashSet2.Contains(settlement3))
					{
						if (settlement3.MapFaction == factionDeclaredWar)
						{
							num2 += 0.2f;
						}
						num += 0.2f;
						hashSet2.Add(settlement3);
					}
				}
			}
			if (num2 < 0.2f)
			{
				return float.MinValue;
			}
			return 0.8f + num2 / num;
		}

		// Token: 0x06001794 RID: 6036 RVA: 0x0006F54C File Offset: 0x0006D74C
		private static float CalculateBenefitScore(DefaultDiplomacyModel.WarStats faction1Stats, DefaultDiplomacyModel.WarStats faction2Stats)
		{
			float num = MathF.Clamp(faction2Stats.ValueOfSettlements, 10000f, 10000000f);
			float num2 = (faction2Stats.Strength + faction1Stats.TotalStrengthOfEnemies) / faction1Stats.Strength;
			float num3 = MathF.Clamp(1f / (1f + num2 * num2), 0.1f, 0.9f);
			return num * num3;
		}

		// Token: 0x06001795 RID: 6037 RVA: 0x0006F5A4 File Offset: 0x0006D7A4
		private static float CalculateRiskScore(DefaultDiplomacyModel.WarStats faction1Stats, DefaultDiplomacyModel.WarStats faction2Stats)
		{
			float num = MathF.Clamp(faction1Stats.ValueOfSettlements, 10000f, 10000000f);
			float num2 = faction1Stats.Strength / (faction2Stats.Strength + faction1Stats.TotalStrengthOfEnemies);
			float num3 = MathF.Clamp(1f / (1f + num2 * num2), 0.1f, 0.9f);
			return num * num3;
		}

		// Token: 0x06001796 RID: 6038 RVA: 0x0006F5FC File Offset: 0x0006D7FC
		private static float AdjustValueOfSettlements(float valueOfSettlements)
		{
			if (valueOfSettlements <= 2000000f)
			{
				return valueOfSettlements + 10000f;
			}
			return (valueOfSettlements - 2000000f) * 0.5f + 10000f + 2000000f;
		}

		// Token: 0x06001797 RID: 6039 RVA: 0x0006F628 File Offset: 0x0006D828
		private static float GetRelationScore(IFaction factionDeclaresWar, IFaction factionDeclaredWar, IFaction evaluatingFaction)
		{
			float relationWithClan = (float)factionDeclaresWar.Leader.Clan.GetRelationWithClan(factionDeclaredWar.Leader.Clan);
			int relationWithClan2 = evaluatingFaction.Leader.Clan.GetRelationWithClan(factionDeclaredWar.Leader.Clan);
			float num = (relationWithClan + (float)relationWithClan2) / 2f;
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

		// Token: 0x06001798 RID: 6040 RVA: 0x0006F6C4 File Offset: 0x0006D8C4
		private static float GetSameCultureTownScore(IFaction factionDeclaresWar, IFaction factionDeclaredWar)
		{
			float b = factionDeclaredWar.Settlements.Sum(delegate(Settlement s)
			{
				if (s.Culture != factionDeclaresWar.Culture || !s.IsFortification)
				{
					return 0f;
				}
				return s.Town.Prosperity * 0.5f * 50f;
			});
			float num = MathF.Min(100000f, b);
			return 0.3f * num;
		}

		// Token: 0x06001799 RID: 6041 RVA: 0x0006F70C File Offset: 0x0006D90C
		private static void UpdateOurBenefitMinusOurRiskBasedOnEvaluatingFaction(IFaction evaluatingFaction, ref float ourBenefit, ref float ourRisk)
		{
			if (ourBenefit.ApproximatelyEqualsTo(ourRisk, 1E-05f))
			{
				return;
			}
			if (!evaluatingFaction.IsKingdomFaction && evaluatingFaction.Leader != evaluatingFaction.MapFaction.Leader)
			{
				bool flag = ourBenefit > ourRisk;
				if (flag && evaluatingFaction.Leader.GetTraitLevel(DefaultTraits.Valor) != 0)
				{
					ourBenefit *= 1f - 0.05f * (float)MathF.Min(2, MathF.Max(-2, evaluatingFaction.Leader.GetTraitLevel(DefaultTraits.Valor)));
					return;
				}
				if (!flag && evaluatingFaction.Leader.GetTraitLevel(DefaultTraits.Calculating) != 0)
				{
					ourRisk *= 1f + 0.05f * (float)MathF.Min(2, MathF.Max(-2, evaluatingFaction.Leader.GetTraitLevel(DefaultTraits.Calculating)));
				}
			}
		}

		// Token: 0x040007B7 RID: 1975
		private const int DailyValueFactorForTributes = 70;

		// Token: 0x040007B8 RID: 1976
		private const float ProsperityValueFactor = 50f;

		// Token: 0x040007B9 RID: 1977
		private const float StrengthFactor = 50f;

		// Token: 0x040007BA RID: 1978
		private const float DenarsToInfluenceValue = 0.002f;

		// Token: 0x040007BB RID: 1979
		private const float RulingClanToJoinOtherKingdomScore = -100000000f;

		// Token: 0x040007BC RID: 1980
		private const float MinStrengthRequiredForFactionToConsiderWar = 500f;

		// Token: 0x040007BD RID: 1981
		private const int MinWarPartyRequiredToConsiderWar = 2;

		// Token: 0x040007BE RID: 1982
		private const float ClanRichnessEffectMultiplier = 0.15f;

		// Token: 0x040007BF RID: 1983
		private const float FirstDegreeNeighborScore = 1f;

		// Token: 0x040007C0 RID: 1984
		private const float SecondDegreeNeighborScore = 0.2f;

		// Token: 0x040007C1 RID: 1985
		private const float MaxBenefitValue = 10000000f;

		// Token: 0x040007C2 RID: 1986
		private const float MeaningfulBenefitValue = 2000000f;

		// Token: 0x040007C3 RID: 1987
		private const float MinBenefitValue = 10000f;

		// Token: 0x040007C4 RID: 1988
		private const float DefaultRelationMultiplierForScoreOfWar = -250f;

		// Token: 0x040007C5 RID: 1989
		private const float SameCultureTownMultiplier = 0.3f;

		// Token: 0x040007C6 RID: 1990
		private const float MaxAcceptableProsperityValue = 100000f;

		// Token: 0x0200057B RID: 1403
		private struct WarStats
		{
			// Token: 0x0400173A RID: 5946
			public float Strength;

			// Token: 0x0400173B RID: 5947
			public float ValueOfSettlements;

			// Token: 0x0400173C RID: 5948
			public float TotalStrengthOfEnemies;
		}
	}
}
