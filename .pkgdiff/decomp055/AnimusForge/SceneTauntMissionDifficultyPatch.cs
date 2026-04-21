using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public static class SceneTauntMissionDifficultyPatch
{
	private static bool _patched;

	public static void EnsurePatched()
	{
		if (_patched)
		{
			return;
		}
		try
		{
			Type type = AccessTools.TypeByName("SandBox.GameComponents.SandboxMissionDifficultyModel");
			MethodInfo methodInfo = AccessTools.Method(type, "GetDamageMultiplierOfCombatDifficulty");
			MethodInfo method = typeof(SceneTauntMissionDifficultyPatch).GetMethod("Postfix", BindingFlags.Static | BindingFlags.Public);
			if (!(type == null) && !(methodInfo == null) && !(method == null))
			{
				Harmony harmony = new Harmony("AnimusForge.scene.taunt.damagemultiplier");
				harmony.Patch(methodInfo, null, new HarmonyMethod(method));
				_patched = true;
				Logger.LogTrace("System", "✅ SceneTauntMissionDifficultyPatch 已打补丁。");
			}
		}
		catch (Exception ex)
		{
			Logger.LogTrace("System", "❌ SceneTauntMissionDifficultyPatch 打补丁失败: " + ex.Message);
		}
	}

	public static void Postfix(Agent victimAgent, Agent attackerAgent, ref float __result)
	{
		try
		{
			if (SceneTauntMissionBehavior.ShouldUseFullCombatDamageExternal(victimAgent, attackerAgent))
			{
				__result = 1f;
			}
		}
		catch
		{
		}
	}
}
