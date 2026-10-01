using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
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

	private static readonly MethodInfo _restartPlayerEncounterMethod = ResolveRestartPlayerEncounterMethod();

	internal static void RestartPlayerEncounter(PartyBase defenderParty, PartyBase attackerParty, bool forcePlayerOutFromSettlement = true, bool isPlayerEncounterRestartedForRaid = false)
	{
		MethodInfo methodInfo = _restartPlayerEncounterMethod;
		if (methodInfo == null)
		{
			throw new MissingMethodException(typeof(PlayerEncounter).FullName, "RestartPlayerEncounter");
		}
		object[] parameters = methodInfo.GetParameters().Length >= 4
			? new object[] { defenderParty, attackerParty, forcePlayerOutFromSettlement, isPlayerEncounterRestartedForRaid }
			: new object[] { defenderParty, attackerParty, forcePlayerOutFromSettlement };
		try
		{
			methodInfo.Invoke(null, parameters);
		}
		catch (TargetInvocationException ex) when (ex.InnerException != null)
		{
			ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
			throw;
		}
	}

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

	internal static bool IsResolvedMapEvent(MapEvent mapEvent)
	{
		if (mapEvent == null)
		{
			return false;
		}
		try
		{
			return mapEvent.HasWinner || mapEvent.IsFinalized;
		}
		catch
		{
			return false;
		}
	}

	internal static bool HasResolvedEncounterBattleContext()
	{
		return IsResolvedMapEvent(GetCurrentMapEventSafe());
	}

	internal static bool IsInPostBattleResultFlow()
	{
		try
		{
			ConversationContext context = Campaign.Current?.CurrentConversationContext ?? ConversationContext.Default;
			if (context == ConversationContext.CapturedLord || context == ConversationContext.FreeOrCapturePrisonerHero)
			{
				return true;
			}
		}
		catch
		{
		}
		PlayerEncounter currentSafe = GetCurrentSafe();
		if (currentSafe == null)
		{
			return false;
		}
		try
		{
			if (IsPostBattleResultState(currentSafe.EncounterState))
			{
				return true;
			}
		}
		catch
		{
		}
		return IsResolvedMapEvent(GetCurrentMapEventSafe());
	}

	private static bool IsPostBattleResultState(PlayerEncounterState state)
	{
		switch (state)
		{
		case PlayerEncounterState.PrepareResults:
		case PlayerEncounterState.ApplyResults:
		case PlayerEncounterState.PlayerVictory:
		case PlayerEncounterState.PlayerTotalDefeat:
		case PlayerEncounterState.CaptureHeroes:
		case PlayerEncounterState.FreeHeroes:
		case PlayerEncounterState.LootParty:
		case PlayerEncounterState.LootInventory:
		case PlayerEncounterState.LootShips:
		case PlayerEncounterState.End:
			return true;
		default:
			return false;
		}
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

	private static MethodInfo ResolveRestartPlayerEncounterMethod()
	{
		MethodInfo fallback = null;
		MethodInfo[] methods = typeof(PlayerEncounter).GetMethods(BindingFlags.Public | BindingFlags.Static);
		for (int i = 0; i < methods.Length; i++)
		{
			MethodInfo method = methods[i];
			if (method.Name != "RestartPlayerEncounter")
			{
				continue;
			}
			ParameterInfo[] parameters = method.GetParameters();
			if (parameters.Length < 3 || parameters.Length > 4)
			{
				continue;
			}
			if (parameters[0].ParameterType != typeof(PartyBase) || parameters[1].ParameterType != typeof(PartyBase) || parameters[2].ParameterType != typeof(bool))
			{
				continue;
			}
			if (parameters.Length == 4 && parameters[3].ParameterType == typeof(bool))
			{
				return method;
			}
			fallback = method;
		}
		return fallback;
	}
}
