using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.Actions;

public static class BeHostileAction
{
	private const float MinorCoercionValue = 1f;

	private const float MajorCoercionValue = 2f;

	private const float EncounterValue = 6f;

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
			if (!defenderParty.Settlement.IsVillage || flag)
			{
				return;
			}
			if (num2 < 0)
			{
				ChangeRelationAction.ApplyRelationChangeBetweenHeroes(leaderHero, defenderParty.Settlement.OwnerClan.Leader, num2);
				foreach (Hero notable in defenderParty.Settlement.Notables)
				{
					ChangeRelationAction.ApplyRelationChangeBetweenHeroes(leaderHero, notable, relationChange3);
				}
			}
			ApplyGeneralConsequencesOnPeace(attackerParty, defenderParty, value);
		}
		else
		{
			if (defenderParty.MobileParty == null)
			{
				return;
			}
			if (defenderParty.MobileParty.IsVillager)
			{
				if (flag)
				{
					foreach (Hero notable2 in defenderParty.MobileParty.HomeSettlement.Notables)
					{
						ChangeRelationAction.ApplyRelationChangeBetweenHeroes(leaderHero, notable2, relationChange2);
					}
					return;
				}
				if (num < 0)
				{
					ChangeRelationAction.ApplyRelationChangeBetweenHeroes(leaderHero, defenderParty.MobileParty.HomeSettlement.OwnerClan.Leader, num);
					foreach (Hero notable3 in defenderParty.MobileParty.HomeSettlement.Notables)
					{
						ChangeRelationAction.ApplyRelationChangeBetweenHeroes(leaderHero, notable3, relationChange);
					}
				}
				ApplyGeneralConsequencesOnPeace(attackerParty, defenderParty, value);
			}
			else
			{
				if (!defenderParty.MobileParty.IsCaravan)
				{
					return;
				}
				if (flag)
				{
					if (num4 < 0 && defenderParty.Owner != null)
					{
						ChangeRelationAction.ApplyRelationChangeBetweenHeroes(leaderHero, defenderParty.Owner, num4);
					}
					return;
				}
				if (num3 < 0 && defenderParty.Owner != null)
				{
					ChangeRelationAction.ApplyRelationChangeBetweenHeroes(leaderHero, defenderParty.Owner, num3);
				}
				ApplyGeneralConsequencesOnPeace(attackerParty, defenderParty, value);
			}
		}
	}

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
				ChangeCrimeRatingAction.Apply(defenderParty.MapFaction, num2);
			}
		}
		if (isClan)
		{
			return;
		}
		if (isKingdomLeader)
		{
			if (num4 < 0f)
			{
				GainKingdomInfluenceAction.ApplyForDefault(attackerParty.MobileParty.LeaderHero, num4);
			}
		}
		else if (isUnderMercenaryService)
		{
			if (num3 < 0)
			{
				ChangeRelationAction.ApplyRelationChangeBetweenHeroes(leaderHero, leaderHero.MapFaction.Leader, num3);
			}
			if (value.ApproximatelyEqualsTo(6f))
			{
				ChangeKingdomAction.ApplyByLeaveKingdomAsMercenary(leaderHero.Clan);
			}
		}
		else
		{
			if (num3 < 0 && attackerParty.MapFaction != null && defenderParty.MapFaction != null)
			{
				ChangeRelationAction.ApplyRelationChangeBetweenHeroes(leaderHero, defenderParty.MapFaction.Leader, num3);
			}
			if (num4 < 0f)
			{
				GainKingdomInfluenceAction.ApplyForDefault(attackerParty.MobileParty.LeaderHero, num4);
			}
		}
	}

	public static void ApplyHostileAction(PartyBase attackerParty, PartyBase defenderParty, float value)
	{
		if (attackerParty == null || defenderParty == null || value.ApproximatelyEqualsTo(0f))
		{
			Debug.FailedAssert("BeHostileAction, attackerParty and/or defenderParty is null or value is 0.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Actions\\BeHostileAction.cs", "ApplyHostileAction", 197);
		}
		else
		{
			ApplyInternal(attackerParty, defenderParty, value);
		}
	}

	public static void ApplyMinorCoercionHostileAction(PartyBase attackerParty, PartyBase defenderParty)
	{
		if (attackerParty == null || defenderParty == null)
		{
			Debug.FailedAssert("BeHostileAction, attackerParty and/or defenderParty is null", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Actions\\BeHostileAction.cs", "ApplyMinorCoercionHostileAction", 209);
		}
		else
		{
			ApplyInternal(attackerParty, defenderParty, 1f);
		}
	}

	public static void ApplyMajorCoercionHostileAction(PartyBase attackerParty, PartyBase defenderParty)
	{
		if (attackerParty == null || defenderParty == null)
		{
			Debug.FailedAssert("BeHostileAction, attackerParty and/or defenderParty is null", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Actions\\BeHostileAction.cs", "ApplyMajorCoercionHostileAction", 221);
		}
		else
		{
			ApplyInternal(attackerParty, defenderParty, 2f);
		}
	}

	public static void ApplyEncounterHostileAction(PartyBase attackerParty, PartyBase defenderParty)
	{
		if (!Campaign.Current.Models.EncounterModel.IsEncounterExemptFromHostileActions(attackerParty, defenderParty))
		{
			ApplyInternal(attackerParty, defenderParty, 6f);
			if (attackerParty == PartyBase.MainParty && attackerParty.MapFaction != defenderParty.MapFaction && !FactionManager.IsAtWarAgainstFaction(attackerParty.MapFaction, defenderParty.MapFaction))
			{
				ChangeRelationAction.ApplyPlayerRelation(defenderParty.MapFaction.Leader, -10);
				DeclareWarAction.ApplyByPlayerHostility(attackerParty.MapFaction, defenderParty.MapFaction);
			}
		}
	}
}
