using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public static class AgentVictoryRetreatNullTeamSafePatch
{
	private const string LogSource = "AgentVictory";

	private static bool _patched;

	private static int _skippedNullTeamAgents;

	private static int _suppressedRetreatExceptions;

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
				Logger.LogTrace("System", "AgentVictoryRetreatNullTeamSafePatch: AgentVictoryLogic type not found.");
				return;
			}
			Harmony harmony = new Harmony("AnimusForge.agentvictory.retreat.nullteam.safety");
			bool patchedAny = false;
			MethodInfo retreatMethod = AccessTools.Method(type, "SetTimersOfVictoryReactionsOnRetreat", new[] { typeof(BattleSideEnum) });
			if (retreatMethod != null)
			{
				harmony.Patch(retreatMethod, finalizer: new HarmonyMethod(typeof(AgentVictoryRetreatNullTeamSafePatch), nameof(RetreatFinalizer)));
				patchedAny = true;
				Logger.LogTrace("System", "AgentVictoryRetreatNullTeamSafePatch finalizer applied: " + retreatMethod.DeclaringType?.FullName + "." + retreatMethod.Name);
			}
			else
			{
				Logger.LogTrace("System", "AgentVictoryRetreatNullTeamSafePatch: SetTimersOfVictoryReactionsOnRetreat method not found.");
			}
			MethodInfo predicate = FindRetreatPredicate(type);
			if (predicate != null)
			{
				harmony.Patch(predicate, prefix: new HarmonyMethod(typeof(AgentVictoryRetreatNullTeamSafePatch), nameof(Prefix)));
				patchedAny = true;
				Logger.LogTrace("System", "AgentVictoryRetreatNullTeamSafePatch predicate guard applied: " + predicate.DeclaringType?.FullName + "." + predicate.Name);
			}
			else
			{
				Logger.LogTrace("System", "AgentVictoryRetreatNullTeamSafePatch: retreat predicate method not found; finalizer fallback remains active.");
			}
			_patched = patchedAny;
		}
		catch (Exception ex)
		{
			Logger.LogTrace("System", "AgentVictoryRetreatNullTeamSafePatch patch failed: " + ex.Message);
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

	public static Exception RetreatFinalizer(Exception __exception, object __instance, object[] __args, MethodBase __originalMethod)
	{
		if (__exception == null)
		{
			return null;
		}
		try
		{
			if (!(__exception is NullReferenceException))
			{
				return __exception;
			}
			BattleSideEnum side = ExtractSide(__args);
			if (!ContainsNullTeamRetreatAgent(__instance, side, out string detail))
			{
				return __exception;
			}
			_suppressedRetreatExceptions++;
			if (_suppressedRetreatExceptions <= 3)
			{
				Logger.Log(LogSource, "Suppressed AgentVictory retreat null-team crash. count=" + _suppressedRetreatExceptions + ", method=" + (__originalMethod?.Name ?? "null") + ", " + detail);
			}
			return null;
		}
		catch
		{
			return __exception;
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

	private static BattleSideEnum ExtractSide(object[] args)
	{
		try
		{
			if (args != null && args.Length > 0 && args[0] is BattleSideEnum side)
			{
				return side;
			}
		}
		catch
		{
		}
		return BattleSideEnum.None;
	}

	private static bool ContainsNullTeamRetreatAgent(object instance, BattleSideEnum side, out string detail)
	{
		detail = "";
		try
		{
			Mission mission = ExtractMission(instance);
			if (mission?.Agents == null)
			{
				return false;
			}
			int checkedAgents = 0;
			int nullTeamAgents = 0;
			Agent firstNullTeamAgent = null;
			foreach (Agent agent in mission.Agents)
			{
				if (agent == null)
				{
					continue;
				}
				bool matchesOriginalFilter;
				try
				{
					matchesOriginalFilter = agent.IsHuman && agent.IsAIControlled;
				}
				catch (NullReferenceException)
				{
					matchesOriginalFilter = true;
				}
				catch
				{
					continue;
				}
				if (!matchesOriginalFilter)
				{
					continue;
				}
				checkedAgents++;
				bool isNullTeam;
				try
				{
					isNullTeam = agent.Team == null;
				}
				catch (NullReferenceException)
				{
					isNullTeam = true;
				}
				catch
				{
					continue;
				}
				if (!isNullTeam)
				{
					continue;
				}
				nullTeamAgents++;
				if (firstNullTeamAgent == null)
				{
					firstNullTeamAgent = agent;
				}
			}
			if (nullTeamAgents <= 0)
			{
				return false;
			}
			detail = "side=" + side + ", nullTeamAgents=" + nullTeamAgents + ", checkedAiHumanAgents=" + checkedAgents + ", firstAgent=" + DescribeAgent(firstNullTeamAgent);
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static Mission ExtractMission(object instance)
	{
		try
		{
			MissionBehavior behavior = instance as MissionBehavior;
			return behavior?.Mission ?? Mission.Current;
		}
		catch
		{
			return Mission.Current;
		}
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
		Logger.Log(LogSource, $"Skipped null-team agent during retreat victory reaction. count={_skippedNullTeamAgents}, agent={name}, index={index}");
	}

	private static string DescribeAgent(Agent agent)
	{
		if (agent == null)
		{
			return "null";
		}
		try
		{
			return (agent.Name?.ToString() ?? "null") + "#" + agent.Index;
		}
		catch
		{
			return "describe_failed";
		}
	}
}
