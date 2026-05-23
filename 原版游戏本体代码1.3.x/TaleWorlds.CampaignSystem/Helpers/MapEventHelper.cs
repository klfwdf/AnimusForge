using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;

namespace Helpers
{
	// Token: 0x02000012 RID: 18
	public static class MapEventHelper
	{
		// Token: 0x060000A0 RID: 160 RVA: 0x00008A98 File Offset: 0x00006C98
		public static PartyBase GetSallyOutDefenderLeader()
		{
			PartyBase result;
			if (MobileParty.MainParty.CurrentSettlement.Town.GarrisonParty != null)
			{
				result = MobileParty.MainParty.CurrentSettlement.Town.GarrisonParty.MapEvent.DefenderSide.LeaderParty;
			}
			else
			{
				PartyBase party = MobileParty.MainParty.CurrentSettlement.Party;
				if (((party != null) ? party.MapEvent : null) != null)
				{
					result = MobileParty.MainParty.CurrentSettlement.Party.MapEvent.DefenderSide.LeaderParty;
				}
				else
				{
					result = MobileParty.MainParty.CurrentSettlement.SiegeEvent.BesiegerCamp.LeaderParty.Party;
				}
			}
			return result;
		}

		// Token: 0x060000A1 RID: 161 RVA: 0x00008B40 File Offset: 0x00006D40
		public static bool CanMainPartyLeaveBattleCommonCondition()
		{
			return MobileParty.MainParty.MapEvent.PlayerSide != BattleSideEnum.Defender || (MobileParty.MainParty.SiegeEvent != null && !MobileParty.MainParty.SiegeEvent.BesiegerCamp.IsBesiegerSideParty(MobileParty.MainParty) && MobileParty.MainParty.CurrentSettlement == null);
		}

		// Token: 0x060000A2 RID: 162 RVA: 0x00008B96 File Offset: 0x00006D96
		public static PartyBase GetEncounteredPartyBase(PartyBase attackerParty, PartyBase defenderParty)
		{
			if (attackerParty == PartyBase.MainParty || defenderParty == PartyBase.MainParty)
			{
				if (attackerParty != PartyBase.MainParty)
				{
					return attackerParty;
				}
				return defenderParty;
			}
			else
			{
				if (defenderParty.MapEvent == null)
				{
					return attackerParty;
				}
				return defenderParty;
			}
		}

		// Token: 0x060000A3 RID: 163 RVA: 0x00008BC0 File Offset: 0x00006DC0
		public static void OnConversationEnd()
		{
			if (PlayerEncounter.Current != null && ((PlayerEncounter.EncounteredMobileParty != null && PlayerEncounter.EncounteredMobileParty.MapFaction != null && !PlayerEncounter.EncounteredMobileParty.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction)) || (PlayerEncounter.EncounteredParty != null && PlayerEncounter.EncounteredParty.MapFaction != null && !PlayerEncounter.EncounteredParty.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))))
			{
				PlayerEncounter.LeaveEncounter = true;
			}
		}

		// Token: 0x060000A4 RID: 164 RVA: 0x00008C38 File Offset: 0x00006E38
		public static FlattenedTroopRoster GetPriorityListForHideoutMission(List<MobileParty> partyList, out int firstPhaseTroopCount)
		{
			int num = partyList.SumQ((MobileParty x) => x.Party.MemberRoster.TotalHealthyCount);
			firstPhaseTroopCount = MathF.Min(MathF.Floor((float)num * Campaign.Current.Models.BanditDensityModel.SpawnPercentageForFirstFightInHideoutMission), Campaign.Current.Models.BanditDensityModel.NumberOfMaximumTroopCountForFirstFightInHideout);
			int num2 = num - firstPhaseTroopCount;
			FlattenedTroopRoster flattenedTroopRoster = new FlattenedTroopRoster(num);
			foreach (MobileParty mobileParty in partyList)
			{
				flattenedTroopRoster.Add(mobileParty.Party.MemberRoster.GetTroopRoster());
			}
			flattenedTroopRoster.RemoveIf((FlattenedTroopRosterElement x) => x.IsWounded);
			int count = flattenedTroopRoster.RemoveIf((FlattenedTroopRosterElement x) => x.Troop.IsHero || x.Troop.Culture.BanditBoss == x.Troop).ToList<FlattenedTroopRosterElement>().Count;
			int num3 = 0;
			int num4 = num2 - count;
			if (num4 > 0)
			{
				IEnumerable<FlattenedTroopRosterElement> selectedRegularTroops = (from x in flattenedTroopRoster
					orderby x.Troop.Level descending
					select x).Take(num4);
				flattenedTroopRoster.RemoveIf((FlattenedTroopRosterElement x) => selectedRegularTroops.Contains(x));
				num3 += selectedRegularTroops.Count<FlattenedTroopRosterElement>();
			}
			Debug.Print("Picking bandit troops for hideout mission...", 0, Debug.DebugColor.Yellow, 256UL);
			Debug.Print("- First phase troop count: " + firstPhaseTroopCount, 0, Debug.DebugColor.Yellow, 256UL);
			Debug.Print("- Second phase boss troop count: " + count, 0, Debug.DebugColor.Yellow, 256UL);
			Debug.Print("- Second phase regular troop count: " + num3, 0, Debug.DebugColor.Yellow, 256UL);
			return flattenedTroopRoster;
		}
	}
}
