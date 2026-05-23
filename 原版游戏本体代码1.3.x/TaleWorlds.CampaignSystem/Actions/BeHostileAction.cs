using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.Actions
{
	// Token: 0x02000492 RID: 1170
	public static class BeHostileAction
	{
		// Token: 0x06004940 RID: 18752 RVA: 0x0017052C File Offset: 0x0016E72C
		private static void ApplyInternal(PartyBase attackerParty, PartyBase defenderParty, float value)
		{
			if (defenderParty.IsMobile && defenderParty.MobileParty.MapFaction == null)
			{
				return;
			}
			int num = (int)(-1f * value);
			int relationChange = (int)(-5f * value);
			int relationChange2 = (int)(-1f * value);
			int num2 = (int)(-4f * value);
			int relationChange3 = (int)(-4f * value);
			int num3 = (int)(-10f * value);
			int num4 = (int)(-2f * value);
			bool flag = attackerParty.MapFaction.IsAtWarWith(defenderParty.MapFaction);
			Hero leaderHero = attackerParty.LeaderHero;
			if (defenderParty.IsSettlement)
			{
				if (defenderParty.Settlement.IsVillage && !flag)
				{
					if (num2 < 0)
					{
						ChangeRelationAction.ApplyRelationChangeBetweenHeroes(leaderHero, defenderParty.Settlement.OwnerClan.Leader, num2, true);
						foreach (Hero gainedRelationWith in defenderParty.Settlement.Notables)
						{
							ChangeRelationAction.ApplyRelationChangeBetweenHeroes(leaderHero, gainedRelationWith, relationChange3, true);
						}
					}
					BeHostileAction.ApplyGeneralConsequencesOnPeace(attackerParty, defenderParty, value);
					return;
				}
			}
			else if (defenderParty.MobileParty != null)
			{
				if (defenderParty.MobileParty.IsVillager)
				{
					if (flag)
					{
						using (List<Hero>.Enumerator enumerator = defenderParty.MobileParty.HomeSettlement.Notables.GetEnumerator())
						{
							while (enumerator.MoveNext())
							{
								Hero gainedRelationWith2 = enumerator.Current;
								ChangeRelationAction.ApplyRelationChangeBetweenHeroes(leaderHero, gainedRelationWith2, relationChange2, true);
							}
							return;
						}
					}
					if (num < 0)
					{
						ChangeRelationAction.ApplyRelationChangeBetweenHeroes(leaderHero, defenderParty.MobileParty.HomeSettlement.OwnerClan.Leader, num, true);
						foreach (Hero gainedRelationWith3 in defenderParty.MobileParty.HomeSettlement.Notables)
						{
							ChangeRelationAction.ApplyRelationChangeBetweenHeroes(leaderHero, gainedRelationWith3, relationChange, true);
						}
					}
					BeHostileAction.ApplyGeneralConsequencesOnPeace(attackerParty, defenderParty, value);
					return;
				}
				if (defenderParty.MobileParty.IsCaravan)
				{
					if (flag)
					{
						if (num4 < 0 && defenderParty.Owner != null)
						{
							ChangeRelationAction.ApplyRelationChangeBetweenHeroes(leaderHero, defenderParty.Owner, num4, true);
							return;
						}
					}
					else
					{
						if (num3 < 0 && defenderParty.Owner != null)
						{
							ChangeRelationAction.ApplyRelationChangeBetweenHeroes(leaderHero, defenderParty.Owner, num3, true);
						}
						BeHostileAction.ApplyGeneralConsequencesOnPeace(attackerParty, defenderParty, value);
					}
				}
			}
		}

		// Token: 0x06004941 RID: 18753 RVA: 0x00170788 File Offset: 0x0016E988
		private static void ApplyGeneralConsequencesOnPeace(PartyBase attackerParty, PartyBase defenderParty, float value)
		{
			float num = -25f * value;
			float num2 = 10f * value;
			int num3 = (int)(-2f * value);
			float num4 = -50f * value;
			bool isClan = attackerParty.MapFaction.IsClan;
			bool isKingdomLeader = attackerParty.LeaderHero.IsKingdomLeader;
			bool isUnderMercenaryService = attackerParty.LeaderHero.Clan.IsUnderMercenaryService;
			Hero leaderHero = attackerParty.LeaderHero;
			if (leaderHero.Equals(Hero.MainHero))
			{
				if (num < 0f)
				{
					TraitLevelingHelper.OnHostileAction((int)num);
				}
				if (num2 > 0f)
				{
					ChangeCrimeRatingAction.Apply(defenderParty.MapFaction, num2, true);
				}
			}
			if (!isClan)
			{
				if (isKingdomLeader)
				{
					if (num4 < 0f)
					{
						GainKingdomInfluenceAction.ApplyForDefault(attackerParty.MobileParty.LeaderHero, num4);
						return;
					}
				}
				else if (isUnderMercenaryService)
				{
					if (num3 < 0)
					{
						ChangeRelationAction.ApplyRelationChangeBetweenHeroes(leaderHero, leaderHero.MapFaction.Leader, num3, true);
					}
					if (value.ApproximatelyEqualsTo(6f, 1E-05f))
					{
						ChangeKingdomAction.ApplyByLeaveKingdomAsMercenary(leaderHero.Clan, true);
						return;
					}
				}
				else
				{
					if (num3 < 0 && attackerParty.MapFaction != null && defenderParty.MapFaction != null)
					{
						ChangeRelationAction.ApplyRelationChangeBetweenHeroes(leaderHero, defenderParty.MapFaction.Leader, num3, true);
					}
					if (num4 < 0f)
					{
						GainKingdomInfluenceAction.ApplyForDefault(attackerParty.MobileParty.LeaderHero, num4);
					}
				}
			}
		}

		// Token: 0x06004942 RID: 18754 RVA: 0x001708BE File Offset: 0x0016EABE
		public static void ApplyHostileAction(PartyBase attackerParty, PartyBase defenderParty, float value)
		{
			if (attackerParty == null || defenderParty == null || value.ApproximatelyEqualsTo(0f, 1E-05f))
			{
				Debug.FailedAssert("BeHostileAction, attackerParty and/or defenderParty is null or value is 0.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Actions\\BeHostileAction.cs", "ApplyHostileAction", 197);
				return;
			}
			BeHostileAction.ApplyInternal(attackerParty, defenderParty, value);
		}

		// Token: 0x06004943 RID: 18755 RVA: 0x001708FA File Offset: 0x0016EAFA
		public static void ApplyMinorCoercionHostileAction(PartyBase attackerParty, PartyBase defenderParty)
		{
			if (attackerParty == null || defenderParty == null)
			{
				Debug.FailedAssert("BeHostileAction, attackerParty and/or defenderParty is null", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Actions\\BeHostileAction.cs", "ApplyMinorCoercionHostileAction", 209);
				return;
			}
			BeHostileAction.ApplyInternal(attackerParty, defenderParty, 1f);
		}

		// Token: 0x06004944 RID: 18756 RVA: 0x00170928 File Offset: 0x0016EB28
		public static void ApplyMajorCoercionHostileAction(PartyBase attackerParty, PartyBase defenderParty)
		{
			if (attackerParty == null || defenderParty == null)
			{
				Debug.FailedAssert("BeHostileAction, attackerParty and/or defenderParty is null", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Actions\\BeHostileAction.cs", "ApplyMajorCoercionHostileAction", 221);
				return;
			}
			BeHostileAction.ApplyInternal(attackerParty, defenderParty, 2f);
		}

		// Token: 0x06004945 RID: 18757 RVA: 0x00170958 File Offset: 0x0016EB58
		public static void ApplyEncounterHostileAction(PartyBase attackerParty, PartyBase defenderParty)
		{
			if (Campaign.Current.Models.EncounterModel.IsEncounterExemptFromHostileActions(attackerParty, defenderParty))
			{
				return;
			}
			BeHostileAction.ApplyInternal(attackerParty, defenderParty, 6f);
			if (attackerParty == PartyBase.MainParty && attackerParty.MapFaction != defenderParty.MapFaction && !FactionManager.IsAtWarAgainstFaction(attackerParty.MapFaction, defenderParty.MapFaction))
			{
				ChangeRelationAction.ApplyPlayerRelation(defenderParty.MapFaction.Leader, -10, true, true);
				DeclareWarAction.ApplyByPlayerHostility(attackerParty.MapFaction, defenderParty.MapFaction);
			}
		}

		// Token: 0x04001433 RID: 5171
		private const float MinorCoercionValue = 1f;

		// Token: 0x04001434 RID: 5172
		private const float MajorCoercionValue = 2f;

		// Token: 0x04001435 RID: 5173
		private const float EncounterValue = 6f;
	}
}
