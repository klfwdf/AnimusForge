using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.SceneActions.Core;
using AnimusForge.XihaiAction;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

/// <summary>
/// Scene natural-language action decided inside AF's unified postprocess
/// ([ACTION:SCENE_ACT:*]) instead of a separate SceneActions classifier
/// request.  The rule is only offered when a postprocess request already runs
/// for the reply (scene shout with a topic, or native conversation); it never
/// makes a postprocess run by itself.  All members run once per NPC reply on
/// the game thread; the capture ledger is bounded by live Agent indices.
/// </summary>
public partial class ShoutBehavior
{
	private sealed class SceneActionReplyCapture
	{
		public Mission Mission;
		public double MissionTime;
	}

	private static readonly Dictionary<int, SceneActionReplyCapture> SceneActionReplyCaptures =
		new Dictionary<int, SceneActionReplyCapture>();

	/// <summary>Records when an NPC reply was handed to presentation (game thread).</summary>
	private static void RecordSceneActionReplyCapture(int agentIndex)
	{
		Mission mission = Mission.Current;
		if (agentIndex < 0 || mission == null)
		{
			return;
		}
		SceneActionReplyCaptures[agentIndex] = new SceneActionReplyCapture
		{
			Mission = mission,
			MissionTime = mission.CurrentTime
		};
	}

	private static bool TryGetSceneActionReplyCapture(Mission mission, int agentIndex, out double missionTime)
	{
		missionTime = 0d;
		if (mission == null
			|| !SceneActionReplyCaptures.TryGetValue(agentIndex, out SceneActionReplyCapture capture)
			|| !ReferenceEquals(capture.Mission, mission))
		{
			return false;
		}
		missionTime = capture.MissionTime;
		return true;
	}

	private static Agent FindSceneActionAgent(Mission mission, int agentIndex)
	{
		if (mission?.Agents == null || agentIndex < 0)
		{
			return null;
		}
		for (int i = 0; i < mission.Agents.Count; i++)
		{
			Agent agent = mission.Agents[i];
			if (agent != null && agent.Index == agentIndex)
			{
				return agent;
			}
		}
		return null;
	}

	/// <summary>
	/// Game-thread offer check for one postprocess request.  Returns the
	/// allowed logical keys, or null when the reply keeps the local result.
	/// </summary>
	private static IReadOnlyList<string> TryOfferSceneActionDirective(int targetAgentIndex, string rawReply)
	{
		try
		{
			Mission mission = Mission.Current;
			if (!TryGetSceneActionReplyCapture(mission, targetAgentIndex, out _))
			{
				return null;
			}
			Agent speaker = FindSceneActionAgent(mission, targetAgentIndex);
			return speaker == null
				? null
				: SceneActionsRuntimeHost.TryBuildNpcReplyDirectiveOffer(mission, speaker, rawReply);
		}
		catch (Exception ex)
		{
			// The ordinary postprocess must not depend on the action subsystem.
			Logger.Log("ShoutBehavior", "[SceneActionDirective] offer failed open: " + ex.Message);
			return null;
		}
	}

	private static List<PostprocessRuleEntry> BuildSceneActionDirectiveRules(IReadOnlyList<string> offeredKeys)
	{
		if (offeredKeys == null || offeredKeys.Count == 0)
		{
			return new List<PostprocessRuleEntry>();
		}
		string catalog = NpcReplyDirectiveTagV1.BuildCatalogText(offeredKeys);
		return (AIConfigHandler.SceneActionPostprocessRules ?? new List<PostprocessRuleEntry>())
			.Where(rule => string.Equals((rule?.Tag ?? "").Trim(), NpcReplyDirectiveTagV1.RuleTemplateTag, StringComparison.Ordinal))
			.Select(rule => new PostprocessRuleEntry
			{
				Tag = rule.Tag,
				Description = (rule.Description ?? "").Replace(NpcReplyDirectiveTagV1.CatalogPlaceholder, catalog)
			})
			.Take(1)
			.ToList();
	}

	/// <summary>Keeps at most one concrete, allow-listed tag from the raw postprocess output.</summary>
	private static string NormalizeSceneActionDirectiveTag(string raw, IReadOnlyList<string> offeredKeys)
	{
		if (offeredKeys == null || offeredKeys.Count == 0)
		{
			return "";
		}
		return NpcReplyDirectiveTagV1.TryExtract(raw, offeredKeys, out string value, out _, out _)
			? NpcReplyDirectiveTagV1.BuildTag(value)
			: "";
	}

	/// <summary>
	/// Removes every [ACTION:SCENE_ACT:*] tag from <paramref name="text"/> and
	/// forwards the first valid one to SceneActions.  Game thread only.  The
	/// runtime re-checks the frozen allow-list, evidence, consent and speaker.
	/// </summary>
	private static void ConsumeSceneActionDirective(ref string text, int targetAgentIndex, string rawReply)
	{
		string source = text ?? "";
		if (!NpcReplyDirectiveTagV1.ContainsTag(source))
		{
			return;
		}
		bool valid = NpcReplyDirectiveTagV1.TryExtract(
			source,
			SceneActionFrameworkV4.LogicalActions.Select(entry => entry.IntentKey),
			out string value,
			out string remaining,
			out string error);
		text = (remaining ?? "").Trim();
		try
		{
			Mission mission = Mission.Current;
			Agent speaker = FindSceneActionAgent(mission, targetAgentIndex);
			if (!valid || speaker == null
				|| !TryGetSceneActionReplyCapture(mission, targetAgentIndex, out double capturedAt))
			{
				Logger.Log("ShoutBehavior", "[SceneActionDirective] dropped agent=" + targetAgentIndex
					+ " valid=" + valid + " speaker=" + (speaker != null) + " error=" + (error ?? ""));
				return;
			}
			bool submitted = SceneActionsRuntimeHost.SubmitNpcReplyDirective(
				mission,
				speaker,
				rawReply,
				value,
				capturedAt,
				mission.CurrentTime);
			Logger.Log("ShoutBehavior", "[SceneActionDirective] agent=" + targetAgentIndex
				+ " value=" + value + " submitted=" + submitted);
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[SceneActionDirective] dispatch failed open: " + ex.Message);
		}
	}
}
