using System;
using System.Collections.Generic;
using System.Threading;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

internal static class SceneActionBehavior
{
	private const int ActionChannel = 1;
	private const int MaxActionsPerTick = 8;
	private const float InitialDelaySeconds = 0.05f;
	private const float LargeGroupStaggerSeconds = 0.1f;
	private const int LargeGroupThreshold = 4;
	private static readonly object PendingLock = new object();
	private static readonly List<PendingSceneAction> PendingActions = new List<PendingSceneAction>();
	private static readonly Dictionary<string, ActionIndexCache> ActionCache = new Dictionary<string, ActionIndexCache>(StringComparer.OrdinalIgnoreCase);
	private static readonly Dictionary<string, SceneActionDefinition> Definitions = BuildDefinitions();
	private static long _nextSequence;
	private static int _pendingCount;
	private static Mission _activeMission;

	internal static IReadOnlyList<int> TryQueuePlayerShout(
		string input,
		IReadOnlyList<Agent> framedAgents)
	{
		if (!SceneActionIntentResolver.TryResolvePlayerInput(input, out var intent))
		{
			return Array.Empty<int>();
		}
		return QueueIntent(intent, framedAgents, "player_shout");
	}

	internal static void Tick(Mission mission)
	{
		if (mission == null || Volatile.Read(ref _pendingCount) <= 0)
		{
			return;
		}
		if (!ReferenceEquals(mission, _activeMission))
		{
			Reset(mission, "mission_changed");
			return;
		}
		List<PendingSceneAction> dueActions = null;
		lock (PendingLock)
		{
			float now = mission.CurrentTime;
			int dueCount = 0;
			while (dueCount < PendingActions.Count &&
				dueCount < MaxActionsPerTick &&
				PendingActions[dueCount].ExecuteAtMissionTime <= now)
			{
				dueCount++;
			}
			if (dueCount == 0)
			{
				return;
			}
			dueActions = PendingActions.GetRange(0, dueCount);
			PendingActions.RemoveRange(0, dueCount);
			Volatile.Write(ref _pendingCount, PendingActions.Count);
		}
		for (int i = 0; i < dueActions.Count; i++)
		{
			Play(mission, dueActions[i]);
		}
	}

	internal static void Reset(Mission mission, string reason)
	{
		lock (PendingLock)
		{
			PendingActions.Clear();
			Volatile.Write(ref _pendingCount, 0);
			_activeMission = mission;
		}
		Logger.Log("SceneActionBehavior", "reset reason=" + (reason ?? "") + " mission=" + (mission?.SceneName ?? "none"));
	}

	private static IReadOnlyList<int> QueueIntent(
		SceneActionIntent intent,
		IReadOnlyList<Agent> framedAgents,
		string source)
	{
		if (intent == null || !TryResolveDefinition(intent.ActionKey, out var definition))
		{
			return Array.Empty<int>();
		}
		Mission mission = Mission.Current;
		if (mission == null)
		{
			return Array.Empty<int>();
		}
		List<int> npcAgentIndices = new List<int>();
		List<Agent> targets = ResolveTargets(intent.TargetKind, framedAgents, npcAgentIndices);
		if (targets.Count == 0)
		{
			Logger.Log("SceneActionBehavior", "queue skipped no live targets source=" + (source ?? "") + " key=" + intent.ActionKey);
			return npcAgentIndices;
		}
		if (!ReferenceEquals(_activeMission, mission))
		{
			Reset(mission, "first_queue");
		}
		float stagger = targets.Count >= LargeGroupThreshold ? LargeGroupStaggerSeconds : 0f;
		float executeAt = mission.CurrentTime + InitialDelaySeconds;
		lock (PendingLock)
		{
			for (int i = 0; i < targets.Count; i++)
			{
				PendingSceneAction pending = new PendingSceneAction(
					mission,
					targets[i],
					definition,
					executeAt + (i * stagger),
					Interlocked.Increment(ref _nextSequence),
					source);
				InsertPendingAction(pending);
			}
			Volatile.Write(ref _pendingCount, PendingActions.Count);
		}
		Logger.Log(
			"SceneActionBehavior",
			"queued source=" + (source ?? "") +
			" key=" + definition.Key +
			" action=" + definition.ActionName +
			" targets=" + targets.Count +
			" targetKind=" + intent.TargetKind +
			" forced=" + intent.IsForced +
			" stagger=" + stagger.ToString("0.###"));
		return npcAgentIndices;
	}

	private static List<Agent> ResolveTargets(
		SceneActionTargetKind targetKind,
		IReadOnlyList<Agent> framedAgents,
		List<int> npcAgentIndices)
	{
		List<Agent> targets = new List<Agent>();
		if (targetKind == SceneActionTargetKind.Player)
		{
			Agent player = Agent.Main;
			if (IsPlayableHuman(player))
			{
				targets.Add(player);
			}
			return targets;
		}
		if (framedAgents == null)
		{
			return targets;
		}
		HashSet<int> seen = new HashSet<int>();
		for (int i = 0; i < framedAgents.Count; i++)
		{
			Agent agent = framedAgents[i];
			if (!IsPlayableHuman(agent) || agent == Agent.Main || !seen.Add(agent.Index))
			{
				continue;
			}
			targets.Add(agent);
			npcAgentIndices.Add(agent.Index);
		}
		return targets;
	}

	private static bool IsPlayableHuman(Agent agent)
	{
		return agent != null && agent.IsActive() && agent.IsHuman;
	}

	private static void InsertPendingAction(PendingSceneAction pending)
	{
		int low = 0;
		int high = PendingActions.Count;
		while (low < high)
		{
			int middle = low + ((high - low) / 2);
			PendingSceneAction current = PendingActions[middle];
			if (current.ExecuteAtMissionTime < pending.ExecuteAtMissionTime ||
				(current.ExecuteAtMissionTime == pending.ExecuteAtMissionTime && current.Sequence <= pending.Sequence))
			{
				low = middle + 1;
			}
			else
			{
				high = middle;
			}
		}
		PendingActions.Insert(low, pending);
	}

	private static void Play(Mission mission, PendingSceneAction pending)
	{
		Agent agent = pending.Agent;
		if (!ReferenceEquals(mission, pending.Mission) || !IsPlayableHuman(agent))
		{
			Logger.Log("SceneActionBehavior", "play skipped stale target source=" + pending.Source + " action=" + pending.Definition.ActionName);
			return;
		}
		try
		{
			if (agent != Agent.Main && IsPlayableHuman(Agent.Main))
			{
				agent.SetLookAgent(Agent.Main);
			}
			ActionIndexCache action = GetAction(pending.Definition.ActionName);
			if (action.Index < 0)
			{
				Logger.Log("SceneActionBehavior", "play failed missing action=" + pending.Definition.ActionName + " source=" + pending.Source);
				return;
			}
			bool played = SetActionChannel(agent, action, pending.Definition.BlendInPeriod);
			Logger.Log(
				"SceneActionBehavior",
				"play " + (played ? "ok" : "rejected") +
				" agent=" + agent.Index +
				" key=" + pending.Definition.Key +
				" action=" + pending.Definition.ActionName +
				" channel=" + ActionChannel +
				" blendIn=" + pending.Definition.BlendInPeriod.ToString("0.###") +
				" source=" + pending.Source);
		}
		catch (Exception ex)
		{
			Logger.Log("SceneActionBehavior", "play exception action=" + pending.Definition.ActionName + " error=" + ex.Message);
		}
	}

	private static ActionIndexCache GetAction(string actionName)
	{
		if (!ActionCache.TryGetValue(actionName, out var action))
		{
			action = ActionIndexCache.Create(actionName);
			ActionCache[actionName] = action;
		}
		return action;
	}

	private static bool SetActionChannel(Agent agent, ActionIndexCache action, float blendInPeriod)
	{
#if BANNERLORD_1_4_OR_GREATER
		return agent.SetActionChannel(
			ActionChannel,
			in action,
			ignorePriority: true,
			(AnimFlags)0UL,
			0f,
			1f,
			blendInPeriod,
			0.4f,
			0f,
			false,
			-0.2f,
			0,
			true);
#else
		return agent.SetActionChannel(
			ActionChannel,
			action,
			ignorePriority: true,
			(AnimFlags)0UL,
			0f,
			1f,
			blendInPeriod,
			0.4f,
			0f,
			false,
			-0.2f,
			0,
			true);
#endif
	}

	private static bool TryResolveDefinition(string actionKey, out SceneActionDefinition definition)
	{
		if (Definitions.TryGetValue(actionKey ?? "", out definition))
		{
			return true;
		}
		string directActionName = (actionKey ?? "").Trim();
		if (!directActionName.StartsWith("act_", StringComparison.OrdinalIgnoreCase))
		{
			definition = null;
			return false;
		}
		definition = new SceneActionDefinition(directActionName, directActionName, ResolveBlendInPeriod(directActionName));
		return true;
	}

	private static Dictionary<string, SceneActionDefinition> BuildDefinitions()
	{
		return new Dictionary<string, SceneActionDefinition>(StringComparer.OrdinalIgnoreCase)
		{
			["kneel"] = new SceneActionDefinition(
				"kneel",
				"act_main_story_conspirator_kneel_down_1_continue",
				0.35f),
			["standup"] = new SceneActionDefinition(
				"standup",
				"act_stand_up_floor_1",
				0.35f),
			["xihai"] = new SceneActionDefinition(
				"xihai",
				"act_af_xihai",
				0.18f)
		};
	}

	private static float ResolveBlendInPeriod(string actionName)
	{
		string text = (actionName ?? "").ToLowerInvariant();
		if (text.Contains("kneel") || text.Contains("stand_up") || text.Contains("sit_"))
		{
			return 0.35f;
		}
		if (text.Contains("cheer") || text.Contains("applaud") || text.Contains("taunt") || text.Contains("conversation"))
		{
			return 0.22f;
		}
		return 0.25f;
	}

	private sealed class SceneActionDefinition
	{
		internal string Key { get; }

		internal string ActionName { get; }

		internal float BlendInPeriod { get; }

		internal SceneActionDefinition(string key, string actionName, float blendInPeriod)
		{
			Key = key ?? "";
			ActionName = actionName ?? "";
			BlendInPeriod = blendInPeriod;
		}
	}

	private sealed class PendingSceneAction
	{
		internal Mission Mission { get; }

		internal Agent Agent { get; }

		internal SceneActionDefinition Definition { get; }

		internal float ExecuteAtMissionTime { get; }

		internal long Sequence { get; }

		internal string Source { get; }

		internal PendingSceneAction(
			Mission mission,
			Agent agent,
			SceneActionDefinition definition,
			float executeAtMissionTime,
			long sequence,
			string source)
		{
			Mission = mission;
			Agent = agent;
			Definition = definition;
			ExecuteAtMissionTime = executeAtMissionTime;
			Sequence = sequence;
			Source = source ?? "";
		}
	}
}
