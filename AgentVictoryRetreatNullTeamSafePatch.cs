using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public static class AgentVictoryRetreatNullTeamSafePatch
{
	private static bool _patched;

	private static int _skippedNullTeamAgents;

	public static void EnsurePatched()
	{
		if (_patched)
		{
			return;
		}
		try
		{
			Type type = AccessTools.TypeByName("TaleWorlds.MountAndBlade.AgentVictoryLogic");
			if (type == null)
			{
				Logger.LogTrace("System", "❌ AgentVictoryRetreatNullTeamSafePatch: 找不到 AgentVictoryLogic 类型。");
				return;
			}
			MethodInfo target = FindRetreatPredicate(type);
			if (target == null)
			{
				Logger.LogTrace("System", "❌ AgentVictoryRetreatNullTeamSafePatch: 找不到 SetTimersOfVictoryReactionsOnRetreat 谓词方法。");
				return;
			}
			Harmony harmony = new Harmony("AnimusForge.agentvictory.retreat.nullteam.safety");
			HarmonyMethod prefix = new HarmonyMethod(typeof(AgentVictoryRetreatNullTeamSafePatch), nameof(Prefix));
			harmony.Patch(target, prefix: prefix);
			_patched = true;
			Logger.LogTrace("System", "✅ AgentVictoryRetreatNullTeamSafePatch 已打补丁: " + target.DeclaringType?.FullName + "." + target.Name);
		}
		catch (Exception ex)
		{
			Logger.LogTrace("System", "❌ AgentVictoryRetreatNullTeamSafePatch 打补丁失败: " + ex.Message);
		}
	}

	public static bool Prefix(object[] __args, ref bool __result)
	{
		Agent agent = null;
		try
		{
			if (__args == null || __args.Length == 0)
			{
				__result = false;
				return false;
			}
			agent = __args[0] as Agent;
			if (agent == null)
			{
				__result = false;
				return false;
			}
			if (!agent.IsHuman || !agent.IsAIControlled)
			{
				return true;
			}
			if (agent.Team != null)
			{
				return true;
			}
			__result = false;
			LogSkippedAgent(agent);
			return false;
		}
		catch (NullReferenceException)
		{
			__result = false;
			LogSkippedAgent(agent);
			return false;
		}
		catch
		{
			return true;
		}
	}

	private static MethodInfo FindRetreatPredicate(Type agentVictoryLogicType)
	{
		Type[] nestedTypes = agentVictoryLogicType.GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public);
		foreach (Type nestedType in nestedTypes)
		{
			MethodInfo[] methods = nestedType.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
			foreach (MethodInfo method in methods)
			{
				if (method == null || method.ReturnType != typeof(bool) || !method.Name.Contains("SetTimersOfVictoryReactionsOnRetreat"))
				{
					continue;
				}
				ParameterInfo[] parameters = method.GetParameters();
				if (parameters.Length == 1 && parameters[0].ParameterType == typeof(Agent))
				{
					return method;
				}
			}
		}
		return null;
	}

	private static void LogSkippedAgent(Agent agent)
	{
		_skippedNullTeamAgents++;
		if (_skippedNullTeamAgents > 3)
		{
			return;
		}
		string name = "null";
		int index = -1;
		try
		{
			name = agent?.Name?.ToString() ?? "null";
			index = agent?.Index ?? -1;
		}
		catch
		{
		}
		Logger.Log("AgentVictory", $"Skipped null-team agent during retreat victory reaction. count={_skippedNullTeamAgents}, agent={name}, index={index}");
	}
}
