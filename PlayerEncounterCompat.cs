using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;

namespace AnimusForge;

internal static class PlayerEncounterCompat
{
	private static readonly FieldInfo _campaignBattleResultField = AccessTools.Field(typeof(PlayerEncounter), "_campaignBattleResult");

	private static readonly FieldInfo _mapEventField = AccessTools.Field(typeof(PlayerEncounter), "_mapEvent");

	private static readonly FieldInfo _encounteredPartyField = AccessTools.Field(typeof(PlayerEncounter), "_encounteredParty");

	internal static PlayerEncounter GetCurrentSafe()
	{
		try
		{
			return Campaign.Current?.PlayerEncounter;
		}
		catch
		{
			return null;
		}
	}

	internal static MapEvent GetBattleSafe()
	{
		PlayerEncounter currentSafe = GetCurrentSafe();
		if (currentSafe == null)
		{
			return null;
		}
		try
		{
			return _mapEventField?.GetValue(currentSafe) as MapEvent;
		}
		catch
		{
			return null;
		}
	}

	internal static PartyBase GetEncounteredPartySafe()
	{
		PlayerEncounter currentSafe = GetCurrentSafe();
		if (currentSafe == null)
		{
			return null;
		}
		try
		{
			return _encounteredPartyField?.GetValue(currentSafe) as PartyBase;
		}
		catch
		{
			return null;
		}
	}

	internal static MapEvent GetEncounteredBattleSafe()
	{
		PartyBase encounteredPartySafe = GetEncounteredPartySafe();
		if (encounteredPartySafe == null)
		{
			return null;
		}
		try
		{
			if (encounteredPartySafe.MapEvent != null)
			{
				return encounteredPartySafe.MapEvent;
			}
		}
		catch
		{
		}
		try
		{
			if (encounteredPartySafe.IsSettlement)
			{
				return encounteredPartySafe.SiegeEvent?.BesiegerCamp?.LeaderParty?.MapEvent;
			}
		}
		catch
		{
		}
		return null;
	}

	internal static MapEvent GetBattleOrEncounteredBattleSafe()
	{
		return GetBattleSafe() ?? GetEncounteredBattleSafe();
	}

	internal static MapEvent GetCurrentMapEventSafe()
	{
		return GetBattleOrEncounteredBattleSafe() ?? GetPlayerMapEventSafe();
	}

	internal static bool HasBattleOrEncounteredBattle()
	{
		return GetBattleOrEncounteredBattleSafe() != null;
	}

	internal static bool HasEncounterBattleContext()
	{
		return GetCurrentMapEventSafe() != null;
	}

	internal static CampaignBattleResult GetCampaignBattleResultSafe()
	{
		PlayerEncounter currentSafe = GetCurrentSafe();
		if (currentSafe == null)
		{
			return null;
		}
		try
		{
			return _campaignBattleResultField?.GetValue(currentSafe) as CampaignBattleResult;
		}
		catch
		{
			return null;
		}
	}

	internal static bool HasCampaignBattleResult()
	{
		return GetCampaignBattleResultSafe() != null;
	}

	internal static bool TrySetCampaignBattleResult(CampaignBattleResult result)
	{
		PlayerEncounter currentSafe = GetCurrentSafe();
		if (currentSafe == null)
		{
			return false;
		}
		try
		{
			if (_campaignBattleResultField != null)
			{
				_campaignBattleResultField.SetValue(currentSafe, result);
				return true;
			}
			PlayerEncounter.CampaignBattleResult = result;
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static MapEvent GetPlayerMapEventSafe()
	{
		try
		{
			return MapEvent.PlayerMapEvent;
		}
		catch
		{
			return null;
		}
	}
}
