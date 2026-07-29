using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AnimusForge;

internal enum SceneActionTargetKind
{
	FramedNpcs,
	Player
}

internal sealed class SceneActionIntent
{
	internal string ActionKey { get; }

	internal SceneActionTargetKind TargetKind { get; }

	internal bool IsForced { get; }

	internal SceneActionIntent(string actionKey, SceneActionTargetKind targetKind, bool isForced)
	{
		ActionKey = actionKey ?? "";
		TargetKind = targetKind;
		IsForced = isForced;
	}
}

internal static class SceneActionIntentResolver
{
	private static readonly Regex DirectNpcCommandRegex = new Regex(
		"^(?:(?:你|你们|大家|所有人|都|全都|全部)(?:都)?(?:给我)?|给我|都给我|全都给我|全部给我)?(?:马上|现在|立刻|赶紧)?(?<action>跪下|下跪|跪着|站起来|站起|起身|起来)$",
		RegexOptions.Compiled | RegexOptions.CultureInvariant);

	private static readonly Regex DirectPlayerCommandRegex = new Regex(
		"^(?:我|我自己)(?:要|来|现在|马上)?(?<action>跪下|下跪|跪着|站起来|站起|起身|起来)$",
		RegexOptions.Compiled | RegexOptions.CultureInvariant);

	private static readonly Dictionary<string, string> ActionAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
	{
		["跪下"] = "kneel",
		["下跪"] = "kneel",
		["跪着"] = "kneel",
		["kneel"] = "kneel",
		["kneel_loop"] = "kneel",
		["act_main_story_conspirator_kneel_down_1_continue"] = "kneel",
		["站起来"] = "standup",
		["站起"] = "standup",
		["起身"] = "standup",
		["起来"] = "standup",
		["standup"] = "standup",
		["getup"] = "standup",
		["act_stand_up_floor_1"] = "standup",
		["西海"] = "xihai",
		["xihai"] = "xihai",
		["af_xihai"] = "xihai",
		["act_af_xihai"] = "xihai"
	};

	internal static bool TryResolvePlayerInput(string input, out SceneActionIntent intent)
	{
		intent = null;
		string text = NormalizeSentence(input);
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		if (text[0] == '*')
		{
			string forcedToken = NormalizeSentence(text.Substring(1));
			if (!TryNormalizeActionKey(forcedToken, out var forcedActionKey))
			{
				return false;
			}
			intent = new SceneActionIntent(forcedActionKey, SceneActionTargetKind.FramedNpcs, isForced: true);
			return true;
		}
		Match playerMatch = DirectPlayerCommandRegex.Match(RemoveCommandWhitespace(text));
		if (playerMatch.Success && TryNormalizeActionKey(playerMatch.Groups["action"].Value, out var playerActionKey))
		{
			intent = new SceneActionIntent(playerActionKey, SceneActionTargetKind.Player, isForced: false);
			return true;
		}
		Match npcMatch = DirectNpcCommandRegex.Match(RemoveCommandWhitespace(text));
		if (!npcMatch.Success || !TryNormalizeActionKey(npcMatch.Groups["action"].Value, out var npcActionKey))
		{
			return false;
		}
		intent = new SceneActionIntent(npcActionKey, SceneActionTargetKind.FramedNpcs, isForced: false);
		return true;
	}

	private static bool TryNormalizeActionKey(string token, out string actionKey)
	{
		actionKey = "";
		string text = NormalizeSentence(token);
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		if (ActionAliases.TryGetValue(text, out var alias))
		{
			actionKey = alias;
			return true;
		}
		if (!Regex.IsMatch(text, "^act_[a-z0-9_]+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
		{
			return false;
		}
		actionKey = text.ToLowerInvariant();
		return true;
	}

	private static string NormalizeSentence(string input)
	{
		return (input ?? "")
			.Replace("\r", " ")
			.Replace("\n", " ")
			.Trim()
			.TrimEnd('。', '！', '？', '.', '!', '?', '；', ';', '，', ',')
			.Trim();
	}

	private static string RemoveCommandWhitespace(string input)
	{
		return Regex.Replace(input ?? "", "\\s+", "");
	}
}
