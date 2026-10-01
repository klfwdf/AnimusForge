using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

/// <summary>
/// Keeps Fourberie's town/fight-club mission controllers out of AnimusForge-owned duel missions.
/// Fourberie assumes a native CampaignMission/Location context in OnMissionBehaviorInitialize,
/// while AF's arena and standalone wilderness duels deliberately open isolated missions.
/// </summary>
internal static class FourberieDuelCompatibility
{
	private const string HarmonyId = "com.AnimusForge.spy.fourberie_duel_guard";

	private const string FourberieMainTypeName = "Fourberie.Main";

	private const string FourberieDamageModelTypeName = "Fourberie.FModelDamage";

	private const string FourberieGlobMissionControllerTypeName = "Fourberie.GlobMissionController";

	private const string FourberieMissionControllerTypeName = "Fourberie.FourbMissionController";

	private const string FourberieStealthMissionLogicTypeName = "Fourberie.FStealthMissionLogic";

	private const string FourberieFightClubControllerTypeName = "Fourberie.FourbFightClubController";

	private const string FourberiePitControllerTypeName = "Fourberie.FourbPitCivilcontroller";

	private const string FourberieBanditryMissionControllerTypeName = "Fourberie.BanditryMissionController";

	private const string FourberieSafeHouseControllerTypeName = "Fourberie.FourbSafeHouseController";

	private const string FourberieStealthSubMissionLogicTypeName = "Fourberie.FStealthSubMissionLogic";

	private static readonly string[] GuardedMissionControllerTypeNames = new string[8]
	{
		FourberieGlobMissionControllerTypeName,
		FourberieMissionControllerTypeName,
		FourberieStealthMissionLogicTypeName,
		FourberieStealthSubMissionLogicTypeName,
		FourberieFightClubControllerTypeName,
		FourberiePitControllerTypeName,
		FourberieBanditryMissionControllerTypeName,
		FourberieSafeHouseControllerTypeName
	};

	private static readonly string[] BlockingMissionControllerTypeNames = new string[6]
	{
		FourberieStealthMissionLogicTypeName,
		FourberieStealthSubMissionLogicTypeName,
		FourberieFightClubControllerTypeName,
		FourberiePitControllerTypeName,
		FourberieBanditryMissionControllerTypeName,
		FourberieSafeHouseControllerTypeName
	};

	private static readonly string[] GuardedMissionCallbackNames = new string[6]
	{
		"OnAgentTeamChanged",
		"OnMissionTick",
		"OnAgentHit",
		"OnEarlyAgentRemoved",
		"OnAgentRemoved",
		"OnAgentAlarmedStateChanged"
	};

	private static readonly object PatchLock = new object();

	private static Harmony _harmony;

	private static bool _patched;

	private static bool _missingLogged;

	private static long _pendingWildernessMissionOpenUntilUtcTicks;

	private static int _suppressedCallbackLogCount;

	internal static void EnsurePatched(Harmony harmony = null)
	{
		if (_patched)
		{
			return;
		}
		lock (PatchLock)
		{
			if (_patched)
			{
				return;
			}
			if (harmony != null)
			{
				_harmony = harmony;
			}
			try
			{
				Type type = FindType(FourberieMainTypeName);
				if (type == null)
				{
					LogMissingOnce("Fourberie not loaded");
					return;
				}
				MethodInfo methodInfo = AccessTools.Method(type, "OnMissionBehaviorInitialize", new Type[1] { typeof(Mission) });
				MethodInfo methodInfo2 = AccessTools.Method(typeof(FourberieDuelCompatibility), nameof(OnMissionBehaviorInitializePrefix));
				if (methodInfo == null || methodInfo2 == null)
				{
					LogMissingOnce("Fourberie.Main.OnMissionBehaviorInitialize not found");
					return;
				}
				Harmony harmony2 = _harmony ?? new Harmony(HarmonyId);
				harmony2.Patch(methodInfo, prefix: new HarmonyMethod(methodInfo2));
				int num = 0;
				foreach (string guardedMissionControllerTypeName in GuardedMissionControllerTypeNames)
				{
					num += PatchMissionBehaviorCallbacks(harmony2, guardedMissionControllerTypeName);
				}
				_harmony = harmony2;
				_patched = true;
				Log("guard_patched callbacks=" + num);
			}
			catch (Exception ex)
			{
				Log("guard_patch_failed " + ex.GetType().Name + ": " + ex.Message);
			}
		}
	}

	/// <summary>
	/// CampaignMission.OpenBattleMission creates its vanilla behaviors before AF can attach its
	/// wilderness-duel behavior. Keep a short, explicit window for that one initialization pass.
	/// </summary>
	internal static void BeginWildernessMissionOpening()
	{
		_pendingWildernessMissionOpenUntilUtcTicks = DateTime.UtcNow.AddSeconds(120.0).Ticks;
	}

	internal static void CompleteWildernessMissionOpening()
	{
		_pendingWildernessMissionOpenUntilUtcTicks = 0L;
	}

	internal static void CancelWildernessMissionOpening()
	{
		_pendingWildernessMissionOpenUntilUtcTicks = 0L;
	}

	internal static bool TryGetDuelStartBlockReason(out string blockedReason)
	{
		blockedReason = "";
		if (FindType(FourberieMainTypeName) == null)
		{
			return false;
		}
		Mission current = Mission.Current;
		if (HasAnyMissionBehavior(current, BlockingMissionControllerTypeNames))
		{
			blockedReason = "检测到 Fourberie 正在接管当前特殊战斗或潜行场景。请先正常离开该场景，再发起 AnimusForge 决斗。";
			return true;
		}
		if (IsFourberieHandToHandBonusActive())
		{
			blockedReason = "检测到 Fourberie 的徒手战斗状态尚未复位。为避免城镇或野外决斗闪退，请先结束或离开 Fourberie 的斗技场/地下擂台；若仍提示，请重载存档后再试。";
			return true;
		}
		return false;
	}

	private static bool OnMissionBehaviorInitializePrefix(Mission mission)
	{
		if (!TryGetSuppressionReason(mission, out string reason))
		{
			return true;
		}
		Log("mission_initialize_skipped reason=" + reason + " scene=" + GetSceneName(mission));
		return false;
	}

	private static bool OnMissionBehaviorCallbackPrefix(object __instance)
	{
		Mission mission = GetMission(__instance);
		if (!ShouldSuppressExistingMissionCallback(mission))
		{
			return true;
		}
		_suppressedCallbackLogCount++;
		if (_suppressedCallbackLogCount <= 8)
		{
			string text = "null";
			try
			{
				text = __instance?.GetType().FullName ?? "null";
			}
			catch
			{
			}
			Log("mission_callback_skipped type=" + text + " count=" + _suppressedCallbackLogCount);
		}
		return false;
	}

	private static int PatchMissionBehaviorCallbacks(Harmony harmony, string typeName)
	{
		Type type = FindType(typeName);
		if (type == null || harmony == null)
		{
			return 0;
		}
		MethodInfo methodInfo = AccessTools.Method(typeof(FourberieDuelCompatibility), nameof(OnMissionBehaviorCallbackPrefix));
		if (methodInfo == null)
		{
			return 0;
		}
		int num = 0;
		try
		{
			MethodInfo[] methods = type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (MethodInfo methodInfo2 in methods)
			{
				if (methodInfo2 == null || Array.IndexOf(GuardedMissionCallbackNames, methodInfo2.Name) < 0)
				{
					continue;
				}
				try
				{
					harmony.Patch(methodInfo2, prefix: new HarmonyMethod(methodInfo));
					num++;
				}
				catch (Exception ex)
				{
					Log("callback_patch_failed type=" + typeName + " method=" + methodInfo2.Name + " error=" + ex.GetType().Name + ": " + ex.Message);
				}
			}
		}
		catch (Exception ex2)
		{
			Log("callback_patch_scan_failed type=" + typeName + " error=" + ex2.GetType().Name + ": " + ex2.Message);
		}
		return num;
	}

	private static bool TryGetSuppressionReason(Mission mission, out string reason)
	{
		reason = "";
		try
		{
			if (DuelBehavior.IsAnimusForgeIndependentDuelMission(mission))
			{
				reason = "animusforge_independent_duel";
				return true;
			}
			if (IsPendingWildernessMissionOpening())
			{
				reason = "animusforge_wilderness_opening";
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private static bool ShouldSuppressExistingMissionCallback(Mission mission)
	{
		try
		{
			if (!DuelBehavior.IsFormalDuelActive)
			{
				return false;
			}
			Mission current = Mission.Current;
			return mission == null || current == null || ReferenceEquals(mission, current);
		}
		catch
		{
			return false;
		}
	}

	private static bool HasAnyMissionBehavior(Mission mission, string[] typeNames)
	{
		if (mission?.MissionBehaviors == null || typeNames == null || typeNames.Length == 0)
		{
			return false;
		}
		try
		{
			foreach (MissionBehavior missionBehavior in mission.MissionBehaviors)
			{
				if (missionBehavior != null && Array.IndexOf(typeNames, missionBehavior.GetType().FullName) >= 0)
				{
					return true;
				}
			}
		}
		catch
		{
		}
		return false;
	}

	private static bool IsFourberieHandToHandBonusActive()
	{
		try
		{
			FieldInfo fieldInfo = AccessTools.Field(FindType(FourberieDamageModelTypeName), "_HandToHandBonusActive");
			return fieldInfo != null && fieldInfo.GetValue(null) is bool flag && flag;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsPendingWildernessMissionOpening()
	{
		long num = _pendingWildernessMissionOpenUntilUtcTicks;
		if (num <= 0L)
		{
			return false;
		}
		long ticks = DateTime.UtcNow.Ticks;
		if (ticks <= num)
		{
			return true;
		}
		_pendingWildernessMissionOpenUntilUtcTicks = 0L;
		return false;
	}

	private static Mission GetMission(object instance)
	{
		try
		{
			return (instance as MissionBehavior)?.Mission ?? Mission.Current;
		}
		catch
		{
			return Mission.Current;
		}
	}

	private static string GetSceneName(Mission mission)
	{
		try
		{
			return mission?.SceneName ?? "null";
		}
		catch
		{
			return "unavailable";
		}
	}

	private static Type FindType(string typeName)
	{
		Type type = AccessTools.TypeByName(typeName);
		if (type != null)
		{
			return type;
		}
		foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
		{
			try
			{
				type = assembly.GetType(typeName, throwOnError: false);
				if (type != null)
				{
					return type;
				}
			}
			catch
			{
			}
		}
		return null;
	}

	private static void LogMissingOnce(string message)
	{
		if (_missingLogged)
		{
			return;
		}
		_missingLogged = true;
		Log("guard_not_active " + message);
	}

	private static void Log(string message)
	{
		Logger.Log("FourberieCompat", "[FourberieDuel] " + message);
	}
}
