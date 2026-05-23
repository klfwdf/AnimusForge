using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;

namespace Helpers;

public static class MapEventHelper
{
	public static bool GetRaidContext(MapEvent mapEvent, out BattleSideEnum raiderSide, out bool raiderHasSeaPresence, out bool raiderHasLandPresence, out bool villageFactionSideHasSeaPresence, out bool villageFactionSideHasLandPresence, out bool wasEverInLootingPhase)
	{
		raiderSide = BattleSideEnum.None;
		raiderHasSeaPresence = false;
		raiderHasLandPresence = false;
		villageFactionSideHasSeaPresence = false;
		villageFactionSideHasLandPresence = false;
		wasEverInLootingPhase = false;
		if (mapEvent.MapEventSettlement == null || !mapEvent.MapEventSettlement.IsVillage)
		{
			return false;
		}
		IFaction mapFaction = mapEvent.MapEventSettlement.MapFaction;
		raiderSide = (mapEvent.AttackerSide.LeaderParty.MapFaction.IsAtWarWith(mapFaction) ? BattleSideEnum.Attacker : BattleSideEnum.Defender);
		BattleSideEnum otherSide = mapEvent.GetOtherSide(raiderSide);
		raiderHasSeaPresence = mapEvent.PartiesOnSide(raiderSide).AnyQ((MapEventParty mapEventParty) => mapEventParty.Party.IsMobile && mapEventParty.Party.NumberOfHealthyMembers > 0 && mapEventParty.Party.MobileParty.IsCurrentlyAtSea);
		raiderHasLandPresence = mapEvent.PartiesOnSide(raiderSide).AnyQ((MapEventParty mapEventParty) => mapEventParty.Party.IsMobile && mapEventParty.Party.NumberOfHealthyMembers > 0 && !mapEventParty.Party.MobileParty.IsCurrentlyAtSea);
		villageFactionSideHasSeaPresence = mapEvent.PartiesOnSide(otherSide).AnyQ((MapEventParty mapEventParty) => mapEventParty.Party.IsMobile && mapEventParty.Party.NumberOfHealthyMembers > 0 && mapEventParty.Party.MobileParty.IsCurrentlyAtSea);
		villageFactionSideHasLandPresence = mapEvent.PartiesOnSide(otherSide).AnyQ((MapEventParty mapEventParty) => mapEventParty.Party.IsMobile && mapEventParty.Party.NumberOfHealthyMembers > 0 && !mapEventParty.Party.MobileParty.IsCurrentlyAtSea);
		wasEverInLootingPhase = mapEvent.WasEverInLootingPhase || (mapEvent == MapEvent.PlayerMapEvent && PlayerEncounter.Current != null && PlayerEncounter.Current.InterruptedWhileLooting);
		return true;
	}

	public static bool IsNavalRaid(MapEvent mapEvent)
	{
		if (!GetRaidContext(mapEvent, out var _, out var raiderHasSeaPresence, out var raiderHasLandPresence, out var villageFactionSideHasSeaPresence, out var villageFactionSideHasLandPresence, out var wasEverInLootingPhase))
		{
			return false;
		}
		if (!(!wasEverInLootingPhase && raiderHasSeaPresence))
		{
			if (wasEverInLootingPhase && villageFactionSideHasSeaPresence)
			{
				if (raiderHasSeaPresence && !raiderHasLandPresence)
				{
					return villageFactionSideHasLandPresence;
				}
				return true;
			}
			return false;
		}
		return true;
	}

	public static PartyBase GetSallyOutDefenderLeader()
	{
		if (MobileParty.MainParty.CurrentSettlement.Town.GarrisonParty != null)
		{
			return MobileParty.MainParty.CurrentSettlement.Town.GarrisonParty.MapEvent.DefenderSide.LeaderParty;
		}
		if (MobileParty.MainParty.CurrentSettlement.Party?.MapEvent != null)
		{
			return MobileParty.MainParty.CurrentSettlement.Party.MapEvent.DefenderSide.LeaderParty;
		}
		return MobileParty.MainParty.CurrentSettlement.SiegeEvent.BesiegerCamp.LeaderParty.Party;
	}

	public static bool CanMainPartyLeaveBattleCommonCondition()
	{
		if (MobileParty.MainParty.MapEvent.PlayerSide == BattleSideEnum.Defender && !MobileParty.MainParty.MapEvent.IsRaid)
		{
			if (MobileParty.MainParty.SiegeEvent != null && !MobileParty.MainParty.SiegeEvent.BesiegerCamp.IsBesiegerSideParty(MobileParty.MainParty))
			{
				return MobileParty.MainParty.CurrentSettlement == null;
			}
			return false;
		}
		return true;
	}

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
		if (defenderParty.MapEvent == null)
		{
			return attackerParty;
		}
		return defenderParty;
	}

	public static void OnConversationEnd()
	{
		if (PlayerEncounter.Current != null && ((PlayerEncounter.EncounteredMobileParty != null && PlayerEncounter.EncounteredMobileParty.MapFaction != null && !PlayerEncounter.EncounteredMobileParty.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction)) || (PlayerEncounter.EncounteredParty != null && PlayerEncounter.EncounteredParty.MapFaction != null && !PlayerEncounter.EncounteredParty.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))))
		{
			PlayerEncounter.LeaveEncounter = true;
		}
	}

	public static FlattenedTroopRoster GetPriorityListForHideoutMission(List<MobileParty> partyList, out int firstPhaseTroopCount)
	{
		int num = partyList.SumQ((MobileParty x) => x.Party.MemberRoster.TotalHealthyCount);
		firstPhaseTroopCount = MathF.Min(MathF.Floor((float)num * Campaign.Current.Models.BanditDensityModel.SpawnPercentageForFirstFightInHideoutMission), Campaign.Current.Models.BanditDensityModel.NumberOfMaximumTroopCountForFirstFightInHideout);
		int num2 = num - firstPhaseTroopCount;
		FlattenedTroopRoster flattenedTroopRoster = new FlattenedTroopRoster(num);
		foreach (MobileParty party in partyList)
		{
			flattenedTroopRoster.Add(party.Party.MemberRoster.GetTroopRoster());
		}
		flattenedTroopRoster.RemoveIf((FlattenedTroopRosterElement x) => x.IsWounded);
		int count = flattenedTroopRoster.RemoveIf((FlattenedTroopRosterElement x) => x.Troop.IsHero || x.Troop.Culture.BanditBoss == x.Troop).ToList().Count;
		int num3 = 0;
		int num4 = num2 - count;
		if (num4 > 0)
		{
			IEnumerable<FlattenedTroopRosterElement> selectedRegularTroops = flattenedTroopRoster.OrderByDescending((FlattenedTroopRosterElement x) => x.Troop.Level).Take(num4);
			flattenedTroopRoster.RemoveIf((FlattenedTroopRosterElement x) => selectedRegularTroops.Contains(x));
			num3 += selectedRegularTroops.Count();
		}
		Debug.Print("Picking bandit troops for hideout mission...", 0, Debug.DebugColor.Yellow, 256uL);
		Debug.Print("- First phase troop count: " + firstPhaseTroopCount, 0, Debug.DebugColor.Yellow, 256uL);
		Debug.Print("- Second phase boss troop count: " + count, 0, Debug.DebugColor.Yellow, 256uL);
		Debug.Print("- Second phase regular troop count: " + num3, 0, Debug.DebugColor.Yellow, 256uL);
		return flattenedTroopRoster;
	}
}
