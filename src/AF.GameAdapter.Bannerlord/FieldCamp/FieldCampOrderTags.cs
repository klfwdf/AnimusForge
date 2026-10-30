using System;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

/// <summary>
/// Scene-shout postprocess tags for camp orders (directive_tag_output_case).
/// Rules are injected only while the camp mission is running and only for a
/// direct reply to the player, so no other scene ever sees these tags.
/// </summary>
internal static class FieldCampOrderTags
{
	internal const string AssembleTag = "[ACTION:SCENE_CAMP_ASSEMBLE]";
	internal const string DismissTag = "[ACTION:SCENE_CAMP_DISMISS]";

	private static bool InCampMission => Mission.Current?.GetMissionBehavior<FieldCampMissionLogic>() != null;

	internal static List<PostprocessRuleEntry> BuildRuntimePostprocessRules(bool replyIsDirectPlayerResponse)
	{
		var rules = new List<PostprocessRuleEntry>();
		FieldCampMissionLogic camp = replyIsDirectPlayerResponse ? FieldCampMissionLogic.Current : null;
		if (camp == null)
		{
			return rules;
		}
		// Offer only the order that can change the current state.
		if (!camp.IsAssembled)
		{
			rules.Add(new PostprocessRuleEntry { Tag = AssembleTag, Description = ConversationRuntimeTexts.Prompt("AF_FC_Rule_Assemble") });
		}
		else
		{
			rules.Add(new PostprocessRuleEntry { Tag = DismissTag, Description = ConversationRuntimeTexts.Prompt("AF_FC_Rule_Dismiss") });
		}
		return rules;
	}

	internal static string NormalizePostprocessTags(string content, List<PostprocessRuleEntry> allowedRules)
	{
		if (allowedRules == null || allowedRules.Count == 0 || string.IsNullOrEmpty(content))
		{
			return string.Empty;
		}
		for (int i = 0; i < allowedRules.Count; i++)
		{
			string tag = allowedRules[i]?.Tag;
			if (!string.IsNullOrEmpty(tag) && content.IndexOf(tag, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return tag;
			}
		}
		return string.Empty;
	}

	/// <summary>Strips camp tags from the reply and executes them (main thread, idempotent).</summary>
	internal static void Consume(bool allowPlayerDirectedActions, ref string content)
	{
		if (string.IsNullOrEmpty(content))
		{
			return;
		}
		bool assemble = content.IndexOf(AssembleTag, StringComparison.OrdinalIgnoreCase) >= 0;
		bool dismiss = content.IndexOf(DismissTag, StringComparison.OrdinalIgnoreCase) >= 0;
		if (!assemble && !dismiss)
		{
			return;
		}
		content = Strip(Strip(content, AssembleTag), DismissTag);
		if (!allowPlayerDirectedActions || !InCampMission)
		{
			return;
		}
		FieldCampMissionLogic camp = FieldCampMissionLogic.Current;
		bool executed = assemble ? camp.Assemble() : camp.Dismiss();
		Logger.Log("FieldCamp", "order tag " + (assemble ? "assemble" : "dismiss") + " executed=" + executed);
	}

	private static string Strip(string content, string tag)
	{
		int index;
		while ((index = content.IndexOf(tag, StringComparison.OrdinalIgnoreCase)) >= 0)
		{
			content = content.Remove(index, tag.Length);
		}
		return content;
	}
}
