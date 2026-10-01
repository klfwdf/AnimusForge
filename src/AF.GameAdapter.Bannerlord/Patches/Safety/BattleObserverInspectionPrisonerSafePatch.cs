using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

/// <summary>
/// Prevents the vanilla battle scoreboard observer from accounting synthetic
/// prisoner agents spawned by the troop-inspection/GCCZ castle scene.
/// Their normal mission-origin and GCCZ cleanup callbacks still run.
/// </summary>
public static class BattleObserverInspectionPrisonerSafePatch
{
	private const string LogSource = "TroopInspection";

	private static bool _patched;

	private static int _skippedCallbacks;

	public static void EnsurePatched()
	{
		if (_patched)
		{
			return;
		}
		try
		{
			Type type = AccessTools.TypeByName("TaleWorlds.MountAndBlade.BattleObserverMissionLogic");
			if (type == null)
			{
				Logger.LogTrace("System", "BattleObserverInspectionPrisonerSafePatch: BattleObserverMissionLogic type not found.");
				return;
			}
			MethodInfo target = AccessTools.Method(type, "OnAgentRemoved", new[]
			{
				typeof(Agent),
				typeof(Agent),
				typeof(AgentState),
				typeof(KillingBlow)
			});
			if (target == null)
			{
				Logger.LogTrace("System", "BattleObserverInspectionPrisonerSafePatch: OnAgentRemoved method not found.");
				return;
			}
			Harmony harmony = new Harmony("AnimusForge.battleobserver.inspection.prisoner.safety");
			harmony.Patch(target, prefix: new HarmonyMethod(typeof(BattleObserverInspectionPrisonerSafePatch), nameof(Prefix)));
			_patched = true;
			Logger.LogTrace("System", "BattleObserverInspectionPrisonerSafePatch applied.");
		}
		catch (Exception ex)
		{
			Logger.LogTrace("System", "BattleObserverInspectionPrisonerSafePatch failed: " + ex.Message);
		}
	}

	public static bool Prefix(object __instance, Agent affectedAgent)
	{
		try
		{
			if (!(affectedAgent?.Origin is PrisonerAgentOrigin))
			{
				return true;
			}
			Mission mission = (__instance as MissionBehavior)?.Mission ?? Mission.Current;
			if (mission?.GetMissionBehavior<TroopInspectionMissionLogic>() == null)
			{
				return true;
			}
			_skippedCallbacks++;
			if (_skippedCallbacks <= 3)
			{
				Logger.Log(LogSource, "[TroopInspection] Skipped synthetic prisoner battle-observer removal. count=" + _skippedCallbacks);
			}
			return false;
		}
		catch
		{
			return true;
		}
	}
}
