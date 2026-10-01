using System;
using System.Reflection;
using HarmonyLib;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace AnimusForge;

[HarmonyPatch(typeof(MenuHelper), nameof(MenuHelper.EncounterAttackConsequence))]
public static class Patch_MenuHelper_EncounterAttackConsequence_RaidVillageRestore
{
	public static void Prefix(MenuCallbackArgs args)
	{
		try
		{
			TryRestoreRaidVillageSettlement();
		}
		catch (Exception ex)
		{
			Logger.LogImmediate("Logic", "[RaidVillageRestore] stage=EncounterAttackConsequence.Prefix reason=exception error=" + ex);
		}
	}

	private static void TryRestoreRaidVillageSettlement()
	{
		PlayerEncounter current = PlayerEncounter.Current;
		if (!BannerlordApiCompat.IsPlayerEncounterRestartedForRaid(current))
		{
			return;
		}
		MapEvent mapEvent = null;
		try
		{
			mapEvent = PlayerEncounter.Battle ?? MapEvent.PlayerMapEvent;
		}
		catch
		{
			mapEvent = MapEvent.PlayerMapEvent;
		}
		if (mapEvent == null)
		{
			Log("skip_mapevent_null", null, null, null, restored: false);
			return;
		}
		try
		{
			if (mapEvent.MapEventSettlement != null)
			{
				return;
			}
			if (!mapEvent.IsFieldBattle)
			{
				return;
			}
		}
		catch
		{
			return;
		}
		MobileParty raidParty = ResolveRaidMobileParty(mapEvent);
		Settlement settlement = ResolveRaidVillageSettlement(raidParty);
		if (settlement == null || !settlement.IsVillage)
		{
			Log("skip_village_not_resolved", mapEvent, raidParty, settlement, restored: false);
			return;
		}
		if (!LooksLikeRaidParty(raidParty, settlement))
		{
			Log("skip_party_not_raid_behavior", mapEvent, raidParty, settlement, restored: false);
			return;
		}
		bool restored = BannerlordApiCompat.TryOverrideMapEventSettlementForRaidToFieldBattleSwitch(mapEvent, settlement);
		if (restored)
		{
			BannerlordApiCompat.TrySetMapEventWasEverInLootingPhase(mapEvent, true);
		}
		Log(restored ? "restored" : "restore_failed", mapEvent, raidParty, settlement, restored);
	}

	private static MobileParty ResolveRaidMobileParty(MapEvent mapEvent)
	{
		try
		{
			MobileParty party = PlayerEncounter.EncounteredMobileParty;
			if (party != null && !party.IsMainParty)
			{
				return party;
			}
		}
		catch
		{
		}
		try
		{
			PartyBase partyBase = PlayerEncounterCompat.GetEncounteredPartySafe() ?? PlayerEncounter.EncounteredParty;
			if (partyBase?.MobileParty != null && !partyBase.MobileParty.IsMainParty)
			{
				return partyBase.MobileParty;
			}
		}
		catch
		{
		}
		return ResolveNonPlayerLeaderParty(mapEvent);
	}

	private static MobileParty ResolveNonPlayerLeaderParty(MapEvent mapEvent)
	{
		if (mapEvent == null)
		{
			return null;
		}
		try
		{
			PartyBase attacker = mapEvent.AttackerSide?.LeaderParty;
			if (attacker?.MobileParty != null && !attacker.MobileParty.IsMainParty)
			{
				return attacker.MobileParty;
			}
		}
		catch
		{
		}
		try
		{
			PartyBase defender = mapEvent.DefenderSide?.LeaderParty;
			if (defender?.MobileParty != null && !defender.MobileParty.IsMainParty)
			{
				return defender.MobileParty;
			}
		}
		catch
		{
		}
		return null;
	}

	private static Settlement ResolveRaidVillageSettlement(MobileParty raidParty)
	{
		try
		{
			if (PlayerEncounter.EncounterSettlement?.IsVillage == true)
			{
				return PlayerEncounter.EncounterSettlement;
			}
		}
		catch
		{
		}
		try
		{
			if (raidParty?.TargetSettlement?.IsVillage == true)
			{
				return raidParty.TargetSettlement;
			}
		}
		catch
		{
		}
		try
		{
			if (raidParty?.ShortTermTargetSettlement?.IsVillage == true)
			{
				return raidParty.ShortTermTargetSettlement;
			}
		}
		catch
		{
		}
		return null;
	}

	private static bool LooksLikeRaidParty(MobileParty raidParty, Settlement settlement)
	{
		if (raidParty == null || settlement == null)
		{
			return false;
		}
		try
		{
			if (raidParty.DefaultBehavior == AiBehavior.RaidSettlement && raidParty.TargetSettlement == settlement)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			if (raidParty.ShortTermBehavior == AiBehavior.RaidSettlement && raidParty.ShortTermTargetSettlement == settlement)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			return settlement.IsUnderRaid && settlement.LastAttackerParty == raidParty;
		}
		catch
		{
			return false;
		}
	}

	private static void Log(string reason, MapEvent mapEvent, MobileParty raidParty, Settlement settlement, bool restored)
	{
		Logger.LogImmediate("Logic",
			"[RaidVillageRestore] stage=EncounterAttackConsequence.Prefix"
			+ " reason=" + (reason ?? "null")
			+ " restored=" + (restored ? "1" : "0")
			+ " event=" + DescribeMapEvent(mapEvent)
			+ " raidParty=" + DescribeParty(raidParty)
			+ " settlement=" + DescribeSettlement(settlement));
	}

	private static string DescribeMapEvent(MapEvent mapEvent)
	{
		if (mapEvent == null)
		{
			return "null";
		}
		try
		{
			return (mapEvent.EventType.ToString() ?? "?")
				+ ",settlement=" + DescribeSettlement(mapEvent.MapEventSettlement)
				+ ",player=" + (mapEvent.IsPlayerMapEvent ? "1" : "0")
				+ ",wasLooting=" + (ReadWasEverInLootingPhase(mapEvent) ? "1" : "0");
		}
		catch
		{
			return "describe_failed";
		}
	}

	private static bool ReadWasEverInLootingPhase(MapEvent mapEvent)
	{
		try
		{
			PropertyInfo property = mapEvent?.GetType().GetProperty("WasEverInLootingPhase", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			return property != null && Convert.ToBoolean(property.GetValue(mapEvent));
		}
		catch
		{
			return false;
		}
	}

	private static string DescribeParty(MobileParty party)
	{
		if (party == null)
		{
			return "null";
		}
		try
		{
			return (party.StringId ?? "?")
				+ ",default=" + party.DefaultBehavior
				+ ",short=" + party.ShortTermBehavior
				+ ",target=" + DescribeSettlement(party.TargetSettlement)
				+ ",shortTarget=" + DescribeSettlement(party.ShortTermTargetSettlement);
		}
		catch
		{
			return "describe_failed";
		}
	}

	private static string DescribeSettlement(Settlement settlement)
	{
		if (settlement == null)
		{
			return "null";
		}
		try
		{
			return (settlement.StringId ?? "?") + "/" + (settlement.Name?.ToString() ?? "?");
		}
		catch
		{
			return "describe_failed";
		}
	}
}
