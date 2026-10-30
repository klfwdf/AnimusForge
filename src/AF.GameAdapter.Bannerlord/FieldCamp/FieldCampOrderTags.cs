using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

/// <summary>
/// Scene-shout postprocess tags for camp orders and camp issue resolutions
/// (directive_tag_output_case). Rules are injected only while the camp mission is
/// running and only for a direct reply to the player, so no other scene ever sees
/// these tags. Frequency: built once per scene reply; at most 3 issues.
/// </summary>
internal static class FieldCampOrderTags
{
	internal const string AssembleTag = "[ACTION:SCENE_CAMP_ASSEMBLE]";
	internal const string DismissTag = "[ACTION:SCENE_CAMP_DISMISS]";
	private const string ResolvePrefix = "[ACTION:SCENE_CAMP_RESOLVE:";

	private static readonly Regex ResolveTagRegex = new Regex(@"\[ACTION:SCENE_CAMP_RESOLVE:\s*(\d+)\s*:\s*([A-Za-z_]+)\s*\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly FieldCampIssues.Resolution[] AllResolutions =
	{
		FieldCampIssues.Resolution.Encourage,
		FieldCampIssues.Resolution.Grant,
		FieldCampIssues.Resolution.Refuse,
		FieldCampIssues.Resolution.Discipline
	};

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
		IReadOnlyList<FieldCampIssues.Issue> issues = FieldCampBehavior.Instance?.State == FieldCampBehavior.CampState.Camped
			? FieldCampBehavior.Instance.Issues
			: null;
		for (int i = 0; issues != null && i < issues.Count; i++)
		{
			if (!issues[i].IsOpen)
			{
				continue;
			}
			HashSet<string> allowed = AllowedResolutions(issues[i]);
			if (allowed.Count == 0)
			{
				continue;
			}
			int number = i + 1;
			rules.Add(new PostprocessRuleEntry
			{
				Tag = ResolvePrefix + number + ":<RESOLUTION>]",
				Description = ConversationRuntimeTexts.FormatPrompt("AF_FC_Rule_Resolve",
					("INDEX", number),
					("TEXT", FieldCampText.IssuePromptText(issues[i])),
					("RES", string.Join("|", allowed))),
				RuntimeAllowedParameterValues = allowed
			});
		}
		return rules;
	}

	/// <summary>Resolutions the player can currently afford, as upper-case tag values.</summary>
	private static HashSet<string> AllowedResolutions(FieldCampIssues.Issue issue)
	{
		var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		int gold = Hero.MainHero?.Gold ?? 0;
		for (int i = 0; i < AllResolutions.Length; i++)
		{
			if (FieldCampBehavior.CostFor(issue, AllResolutions[i]) <= gold)
			{
				allowed.Add(AllResolutions[i].ToString().ToUpperInvariant());
			}
		}
		return allowed;
	}

	/// <summary>
	/// Main-prompt block listing the camp's issues (runtime context of a camp speaker).
	/// Empty outside the camp mission.
	/// </summary>
	internal static string BuildIssuePromptBlock()
	{
		if (FieldCampMissionLogic.Current == null || FieldCampBehavior.Instance?.State != FieldCampBehavior.CampState.Camped)
		{
			return "";
		}
		IReadOnlyList<FieldCampIssues.Issue> issues = FieldCampBehavior.Instance.Issues;
		var sb = new StringBuilder();
		for (int i = 0; i < issues.Count; i++)
		{
			if (!issues[i].IsOpen)
			{
				continue;
			}
			var options = new List<string>();
			for (int r = 0; r < AllResolutions.Length; r++)
			{
				string option = ConversationRuntimeTexts.Prompt("AF_FC_IssueOpt" + AllResolutions[r]);
				int cost = FieldCampBehavior.CostFor(issues[i], AllResolutions[r]);
				if (cost > 0)
				{
					option += "(" + ConversationRuntimeTexts.FormatPrompt("AF_FC_IssueCost", ("GOLD", cost)) + ")";
				}
				options.Add(option);
			}
			sb.AppendLine(ConversationRuntimeTexts.FormatPrompt("AF_FC_IssuePromptLine",
				("INDEX", i + 1),
				("TEXT", FieldCampText.IssuePromptText(issues[i])),
				("OPTIONS", string.Join(" / ", options))));
		}
		if (sb.Length == 0)
		{
			return ConversationRuntimeTexts.Prompt("AF_FC_IssuePromptNone");
		}
		return (ConversationRuntimeTexts.Prompt("AF_FC_IssuePromptHeader") + "\n" + sb.ToString()).Trim();
	}

	internal static string NormalizePostprocessTags(string content, List<PostprocessRuleEntry> allowedRules)
	{
		if (allowedRules == null || allowedRules.Count == 0 || string.IsNullOrEmpty(content))
		{
			return string.Empty;
		}
		var result = new List<string>();
		for (int i = 0; i < allowedRules.Count; i++)
		{
			string tag = allowedRules[i]?.Tag;
			if (string.IsNullOrEmpty(tag))
			{
				continue;
			}
			if (tag.StartsWith(ResolvePrefix, StringComparison.OrdinalIgnoreCase))
			{
				string canonical = MatchResolveRule(content, allowedRules[i]);
				if (canonical.Length > 0)
				{
					result.Add(canonical);
				}
			}
			else if (content.IndexOf(tag, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				result.Add(tag);
			}
		}
		return string.Join("\n", result);
	}

	/// <summary>First tag in the content whose index and resolution this rule allows; one per issue.</summary>
	private static string MatchResolveRule(string content, PostprocessRuleEntry rule)
	{
		string number = rule.Tag.Substring(ResolvePrefix.Length).Split(':')[0];
		foreach (Match match in ResolveTagRegex.Matches(content))
		{
			string resolution = match.Groups[2].Value.ToUpperInvariant();
			if (match.Groups[1].Value == number && rule.RuntimeAllowedParameterValues != null && rule.RuntimeAllowedParameterValues.Contains(resolution))
			{
				return ResolvePrefix + number + ":" + resolution + "]";
			}
		}
		return string.Empty;
	}

	/// <summary>Strips camp tags from the reply and executes them (main thread, idempotent per issue).</summary>
	internal static void Consume(bool allowPlayerDirectedActions, ref string content)
	{
		if (string.IsNullOrEmpty(content))
		{
			return;
		}
		bool assemble = content.IndexOf(AssembleTag, StringComparison.OrdinalIgnoreCase) >= 0;
		bool dismiss = content.IndexOf(DismissTag, StringComparison.OrdinalIgnoreCase) >= 0;
		MatchCollection resolves = content.IndexOf(ResolvePrefix, StringComparison.OrdinalIgnoreCase) >= 0 ? ResolveTagRegex.Matches(content) : null;
		bool hasResolve = resolves != null && resolves.Count > 0;
		if (!assemble && !dismiss && !hasResolve)
		{
			return;
		}
		content = Strip(Strip(content, AssembleTag), DismissTag);
		if (hasResolve)
		{
			content = ResolveTagRegex.Replace(content, "");
		}
		if (!allowPlayerDirectedActions || !InCampMission)
		{
			return;
		}
		if (assemble || dismiss)
		{
			FieldCampMissionLogic camp = FieldCampMissionLogic.Current;
			bool executed = assemble ? camp.Assemble() : camp.Dismiss();
			Logger.Log("FieldCamp", "order tag " + (assemble ? "assemble" : "dismiss") + " executed=" + executed);
		}
		for (int i = 0; hasResolve && i < resolves.Count; i++)
		{
			ExecuteResolve(resolves[i]);
		}
	}

	private static void ExecuteResolve(Match match)
	{
		FieldCampBehavior behavior = FieldCampBehavior.Instance;
		if (behavior == null
			|| !int.TryParse(match.Groups[1].Value, out int number)
			|| !Enum.TryParse(match.Groups[2].Value, true, out FieldCampIssues.Resolution resolution))
		{
			return;
		}
		int index = number - 1;
		IReadOnlyList<FieldCampIssues.Issue> issues = behavior.Issues;
		FieldCampIssues.Issue issue = index >= 0 && index < issues.Count ? issues[index] : null;
		string fact = behavior.Resolve(index, resolution);
		Logger.Log("FieldCamp", "resolve tag issue=" + number + " resolution=" + resolution + " executed=" + (fact != null));
		if (fact == null || issue == null)
		{
			return;
		}
		if (issue.IsCompanionIssue)
		{
			RecordHeroFact(issue.Subject, fact);
			RecordHeroFact(issue.Subject2, fact);
			return;
		}
		RecordFact(issue.Subject, fact);
		if (issue.Kind == FieldCampIssues.Kind.CultureFriction)
		{
			RecordFact(issue.Subject2, fact);
		}
	}

	/// <summary>AFEF fact into a companion's own hero memory.</summary>
	private static void RecordHeroFact(string heroId, string fact)
	{
		try
		{
			Hero hero = string.IsNullOrEmpty(heroId) ? null : Hero.Find(heroId);
			if (hero != null)
			{
				MyBehavior.AppendExternalDialogueHistory(hero, null, null, fact);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("FieldCamp", "[WARN] issue fact write failed hero=" + heroId + " error=" + ex.Message);
		}
	}

	/// <summary>AFEF fact into the troop-type memory of the main party (same store as all three channels).</summary>
	private static void RecordFact(string troopId, string fact)
	{
		if (string.IsNullOrEmpty(troopId))
		{
			return;
		}
		try
		{
			CharacterObject troop = CharacterObject.Find(troopId);
			if (troop != null && MainPartyTroopMemory.TryResolve(null, troop, -1, out string memoryId, out string memoryName))
			{
				MyBehavior.AppendExternalNonHeroDialogueHistory(memoryId, memoryName, null, null, fact);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("FieldCamp", "[WARN] issue fact write failed troop=" + troopId + " error=" + ex.Message);
		}
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
