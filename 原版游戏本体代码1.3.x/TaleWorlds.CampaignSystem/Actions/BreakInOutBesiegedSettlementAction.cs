using System;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.Actions
{
	// Token: 0x02000493 RID: 1171
	public static class BreakInOutBesiegedSettlementAction
	{
		// Token: 0x06004946 RID: 18758 RVA: 0x001709D8 File Offset: 0x0016EBD8
		public static void ApplyBreakIn(out TroopRoster casualties, out int armyCasualtiesCount, bool isFromPort)
		{
			BreakInOutBesiegedSettlementAction.ApplyInternal(true, out casualties, out armyCasualtiesCount, isFromPort);
		}

		// Token: 0x06004947 RID: 18759 RVA: 0x001709E3 File Offset: 0x0016EBE3
		public static void ApplyBreakOut(out TroopRoster casualties, out int armyCasualtiesCount, bool isFromPort)
		{
			BreakInOutBesiegedSettlementAction.ApplyInternal(false, out casualties, out armyCasualtiesCount, isFromPort);
		}

		// Token: 0x06004948 RID: 18760 RVA: 0x001709F0 File Offset: 0x0016EBF0
		private static void ApplyInternal(bool breakIn, out TroopRoster casualties, out int armyCasualtiesCount, bool isFromPort)
		{
			casualties = TroopRoster.CreateDummyTroopRoster();
			armyCasualtiesCount = -1;
			MobileParty mainParty = MobileParty.MainParty;
			SiegeEvent siegeEvent = Settlement.CurrentSettlement.SiegeEvent;
			int roundedResultNumber;
			if (breakIn)
			{
				roundedResultNumber = Campaign.Current.Models.TroopSacrificeModel.GetLostTroopCountForBreakingInBesiegedSettlement(mainParty, siegeEvent).RoundedResultNumber;
			}
			else
			{
				roundedResultNumber = Campaign.Current.Models.TroopSacrificeModel.GetLostTroopCountForBreakingOutOfBesiegedSettlement(mainParty, siegeEvent, isFromPort).RoundedResultNumber;
			}
			if (mainParty.Army == null || mainParty.Army.LeaderParty != mainParty)
			{
				TroopRoster memberRoster = mainParty.MemberRoster;
				for (int i = 0; i < roundedResultNumber; i++)
				{
					int index = MBRandom.RandomInt(memberRoster.Count);
					CharacterObject characterAtIndex = memberRoster.GetCharacterAtIndex(index);
					if (!characterAtIndex.IsRegular || memberRoster.GetElementNumber(index) == 0)
					{
						i--;
					}
					else
					{
						memberRoster.AddToCountsAtIndex(index, -1, 0, 0, true);
						casualties.AddToCounts(characterAtIndex, 1, false, 0, 0, true, -1);
					}
				}
				if (mainParty.Army != null && mainParty.Army.LeaderParty != MobileParty.MainParty)
				{
					TroopSacrificeModel troopSacrificeModel = Campaign.Current.Models.TroopSacrificeModel;
					ChangeRelationAction.ApplyPlayerRelation(mainParty.Army.LeaderParty.LeaderHero, troopSacrificeModel.BreakOutArmyLeaderRelationPenalty, true, true);
					foreach (MobileParty mobileParty in mainParty.Army.LeaderParty.AttachedParties)
					{
						if (mobileParty.LeaderHero != null && mobileParty != mainParty)
						{
							ChangeRelationAction.ApplyPlayerRelation(mobileParty.LeaderHero, troopSacrificeModel.BreakOutArmyMemberRelationPenalty, true, true);
						}
					}
					MobileParty.MainParty.Army = null;
					return;
				}
			}
			else
			{
				armyCasualtiesCount = 0;
				Army army = mainParty.Army;
				int num = 0;
				foreach (MobileParty mobileParty2 in army.Parties)
				{
					num += mobileParty2.MemberRoster.TotalManCount - mobileParty2.MemberRoster.TotalHeroes;
				}
				for (int j = 0; j < roundedResultNumber; j++)
				{
					float num2 = MBRandom.RandomFloat * (float)num;
					foreach (MobileParty mobileParty3 in army.Parties)
					{
						num2 -= (float)(mobileParty3.MemberRoster.TotalManCount - mobileParty3.MemberRoster.TotalHeroes);
						if (num2 < 0f)
						{
							num2 += (float)(mobileParty3.MemberRoster.TotalManCount - mobileParty3.MemberRoster.TotalHeroes);
							int num3 = -1;
							for (int k = 0; k < mobileParty3.MemberRoster.Count; k++)
							{
								if (!mobileParty3.MemberRoster.GetCharacterAtIndex(k).IsHero)
								{
									num2 -= (float)(mobileParty3.MemberRoster.GetElementNumber(k) + mobileParty3.MemberRoster.GetElementWoundedNumber(k));
									if (num2 < 0f)
									{
										num3 = k;
										break;
									}
								}
							}
							if (num3 >= 0)
							{
								CharacterObject characterAtIndex2 = mobileParty3.MemberRoster.GetCharacterAtIndex(num3);
								mobileParty3.MemberRoster.AddToCountsAtIndex(num3, -1, 0, 0, true);
								num--;
								if (mobileParty3 == MobileParty.MainParty)
								{
									casualties.AddToCounts(characterAtIndex2, 1, false, 0, 0, true, -1);
									break;
								}
								armyCasualtiesCount++;
								break;
							}
						}
					}
				}
			}
		}
	}
}
