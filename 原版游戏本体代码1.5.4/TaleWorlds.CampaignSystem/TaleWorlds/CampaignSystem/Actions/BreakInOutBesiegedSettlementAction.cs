using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.Actions;

public static class BreakInOutBesiegedSettlementAction
{
	public static void ApplyBreakIn(out TroopRoster casualties, out int armyCasualtiesCount, bool isFromPort)
	{
		ApplyInternal(breakIn: true, out casualties, out armyCasualtiesCount, isFromPort);
	}

	public static void ApplyBreakOut(out TroopRoster casualties, out int armyCasualtiesCount, bool isFromPort)
	{
		ApplyInternal(breakIn: false, out casualties, out armyCasualtiesCount, isFromPort);
	}

	private static void ApplyInternal(bool breakIn, out TroopRoster casualties, out int armyCasualtiesCount, bool isFromPort)
	{
		casualties = TroopRoster.CreateDummyTroopRoster();
		armyCasualtiesCount = -1;
		MobileParty mainParty = MobileParty.MainParty;
		SiegeEvent siegeEvent = Settlement.CurrentSettlement.SiegeEvent;
		int num = ((!breakIn) ? Campaign.Current.Models.TroopSacrificeModel.GetLostTroopCountForBreakingOutOfBesiegedSettlement(mainParty, siegeEvent, isFromPort).RoundedResultNumber : Campaign.Current.Models.TroopSacrificeModel.GetLostTroopCountForBreakingInBesiegedSettlement(mainParty, siegeEvent).RoundedResultNumber);
		if (mainParty.Army == null || mainParty.Army.LeaderParty != mainParty)
		{
			TroopRoster memberRoster = mainParty.MemberRoster;
			for (int i = 0; i < num; i++)
			{
				int index = MBRandom.RandomInt(memberRoster.Count);
				CharacterObject characterAtIndex = memberRoster.GetCharacterAtIndex(index);
				if (!characterAtIndex.IsRegular || memberRoster.GetElementNumber(index) == 0)
				{
					i--;
					continue;
				}
				memberRoster.AddToCountsAtIndex(index, -1);
				casualties.AddToCounts(characterAtIndex, 1);
			}
			if (mainParty.Army == null || mainParty.Army.LeaderParty == MobileParty.MainParty)
			{
				return;
			}
			TroopSacrificeModel troopSacrificeModel = Campaign.Current.Models.TroopSacrificeModel;
			ChangeRelationAction.ApplyPlayerRelation(mainParty.Army.LeaderParty.LeaderHero, troopSacrificeModel.BreakOutArmyLeaderRelationPenalty);
			foreach (MobileParty attachedParty in mainParty.Army.LeaderParty.AttachedParties)
			{
				if (attachedParty.LeaderHero != null && attachedParty != mainParty)
				{
					ChangeRelationAction.ApplyPlayerRelation(attachedParty.LeaderHero, troopSacrificeModel.BreakOutArmyMemberRelationPenalty);
				}
			}
			MobileParty.MainParty.Army = null;
			return;
		}
		armyCasualtiesCount = 0;
		Army army = mainParty.Army;
		int num2 = 0;
		foreach (MobileParty party in army.Parties)
		{
			num2 += party.MemberRoster.TotalManCount - party.MemberRoster.TotalHeroes;
		}
		for (int j = 0; j < num; j++)
		{
			float num3 = MBRandom.RandomFloat * (float)num2;
			foreach (MobileParty party2 in army.Parties)
			{
				num3 -= (float)(party2.MemberRoster.TotalManCount - party2.MemberRoster.TotalHeroes);
				if (!(num3 < 0f))
				{
					continue;
				}
				num3 += (float)(party2.MemberRoster.TotalManCount - party2.MemberRoster.TotalHeroes);
				int num4 = -1;
				for (int k = 0; k < party2.MemberRoster.Count; k++)
				{
					if (!party2.MemberRoster.GetCharacterAtIndex(k).IsHero)
					{
						num3 -= (float)(party2.MemberRoster.GetElementNumber(k) + party2.MemberRoster.GetElementWoundedNumber(k));
						if (num3 < 0f)
						{
							num4 = k;
							break;
						}
					}
				}
				if (num4 >= 0)
				{
					CharacterObject characterAtIndex2 = party2.MemberRoster.GetCharacterAtIndex(num4);
					party2.MemberRoster.AddToCountsAtIndex(num4, -1);
					num2--;
					if (party2 == MobileParty.MainParty)
					{
						casualties.AddToCounts(characterAtIndex2, 1);
					}
					else
					{
						armyCasualtiesCount++;
					}
					break;
				}
			}
		}
	}
}
