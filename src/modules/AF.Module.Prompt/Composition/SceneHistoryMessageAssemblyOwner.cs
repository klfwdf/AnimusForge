using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AnimusForge;

// Pure request-frequency history text projection; adapters capture all live identity/config.
internal static class SceneHistoryMessageAssemblyOwner
{
internal static string NormalizeSceneHistoryPromptLineContent(string content)
	{
		string text = (content ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		string[] array = text.Split('\n');
		StringBuilder stringBuilder = new StringBuilder(text.Length);
		for (int i = 0; i < array.Length; i++)
		{
			string text2 = (array[i] ?? "").Trim();
			text2 = ShoutUtils.StripConversationMetadataPrefix(text2);
			if (string.IsNullOrWhiteSpace(text2) || ConversationSpeechTextRules.IsLeakedPromptLineForShout(text2))
			{
				continue;
			}
			if (stringBuilder.Length > 0)
			{
				stringBuilder.Append(' ');
			}
			stringBuilder.Append(text2);
		}
		return Regex.Replace(stringBuilder.ToString(), "[ \\t]{2,}", " ").Trim();
	}

internal static string NormalizeStrictSceneAssistantContent(string content, string speakerName)
	{
		string text = (content ?? "").Replace("\r", "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		text = ShoutUtils.StripConversationMetadataPrefix(text);
		int num = -1;
		if (text.StartsWith("[", StringComparison.Ordinal))
		{
			num = text.IndexOf("]: ", StringComparison.Ordinal);
			if (num > 0 && num + 3 < text.Length)
			{
				text = text.Substring(num + 3).Trim();
			}
		}
		string text2 = (speakerName ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text2))
		{
			string[] array = new string[4] { text2 + ": ", text2 + "：", "[" + text2 + "]: ", "[" + text2 + "]：" };
			foreach (string value in array)
			{
				if (text.StartsWith(value, StringComparison.Ordinal))
				{
					text = text.Substring(value.Length).Trim();
					break;
				}
			}
		}
		return ShoutUtils.StripConversationMetadataPrefix(text);
	}

internal static string FormatScenePlayerDirectSpeechLabel(string playerName, float playerDistanceMeters)
	{
		string text = (playerName ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = PromptComposerTexts.Get("residual_player");
		}
		if (float.IsNaN(playerDistanceMeters) || float.IsInfinity(playerDistanceMeters) || playerDistanceMeters < 0f)
		{
			return PromptComposerTexts.Format("residual_player_says", ("NAME", text));
		}
		int num = Math.Max(0, (int)Math.Ceiling(playerDistanceMeters));
		if (playerDistanceMeters > 50f)
		{
			return PromptComposerTexts.Format("residual_player_shouts_loudly", ("NAME", text), ("DISTANCE", num));
		}
		if (playerDistanceMeters > 5f)
		{
			return PromptComposerTexts.Format("residual_player_shouts", ("NAME", text), ("DISTANCE", num));
		}
		return PromptComposerTexts.Format("residual_player_says", ("NAME", text));
	}

internal static bool TryNormalizeAfefFactLineForPrompt(string text, out string factLine)
	{
		string value = ConversationSpeechTextRules.StripAfefPromptScopeLabel((text ?? "").Replace("\r", " ").Replace("\n", " ").Trim());
        if (value.StartsWith("[AFEF NPC_ACTION]", StringComparison.Ordinal))
            value = PromptComposerTexts.Raw("afef_npc_action") + value.Substring("[AFEF NPC_ACTION]".Length);
        else if (value.StartsWith("[AFEF PLAYER_ACTION]", StringComparison.Ordinal))
            value = PromptComposerTexts.Raw("afef_player_action") + value.Substring("[AFEF PLAYER_ACTION]".Length);
		if (value.StartsWith(PromptComposerTexts.Raw("afef_player_action"), StringComparison.Ordinal) || value.StartsWith(PromptComposerTexts.Raw("afef_npc_action"), StringComparison.Ordinal) || value.StartsWith(PromptComposerTexts.Raw("afef_player_action_bracket"), StringComparison.Ordinal) || value.StartsWith(PromptComposerTexts.Raw("afef_npc_action_bracket"), StringComparison.Ordinal))
		{
			factLine = NormalizeAfefFactLineForPrompt(value);
			return true;
		}
		factLine = "";
		return false;
	}

internal static string NormalizeAfefFactLineForPrompt(string text, string fallbackPrefix = null)
	{
		string value = ConversationSpeechTextRules.StripAfefPromptScopeLabel((text ?? "").Replace("\r", " ").Replace("\n", " ").Trim());
        if (value.StartsWith("[AFEF NPC_ACTION]", StringComparison.Ordinal))
            value = PromptComposerTexts.Raw("afef_npc_action") + value.Substring("[AFEF NPC_ACTION]".Length);
        else if (value.StartsWith("[AFEF PLAYER_ACTION]", StringComparison.Ordinal))
            value = PromptComposerTexts.Raw("afef_player_action") + value.Substring("[AFEF PLAYER_ACTION]".Length);
		if (string.IsNullOrWhiteSpace(value))
		{
			return "";
		}
		if (value.StartsWith(PromptComposerTexts.Raw("afef_player_action"), StringComparison.Ordinal) || value.StartsWith(PromptComposerTexts.Raw("afef_npc_action"), StringComparison.Ordinal))
		{
			return value;
		}
		if (value.StartsWith(PromptComposerTexts.Raw("afef_player_action_bracket"), StringComparison.Ordinal))
		{
			return PromptComposerTexts.Raw("afef_player_action") + " " + value.Substring(PromptComposerTexts.Raw("afef_player_action_bracket").Length).Trim();
		}
		if (value.StartsWith(PromptComposerTexts.Raw("afef_npc_action_bracket"), StringComparison.Ordinal))
		{
			return PromptComposerTexts.Raw("afef_npc_action") + " " + value.Substring(PromptComposerTexts.Raw("afef_npc_action_bracket").Length).Trim();
		}
		string prefix = string.IsNullOrWhiteSpace(fallbackPrefix) ? PromptComposerTexts.Raw("afef_player_action") : fallbackPrefix.Trim();
		return prefix + " " + value;
	}

internal static string BuildScopedAfefFactLineForPrompt(string text, bool isCurrent)
	{
        // Project individual courier facts before the legacy normalizer flattens line breaks.
		string factLine = NormalizeAfefFactLineForPrompt(MemorySystemFactProjection.ProjectNpcFact(text));
        factLine = MemorySystemFactProjection.ProjectNpcFact(factLine);
		if (string.IsNullOrWhiteSpace(factLine))
		{
			return "";
		}
		return (isCurrent ? PromptComposerTexts.Get("history_section_scope_current") : PromptComposerTexts.Get("history_section_scope_historical")) + factLine;
	}

// Currentness is event identity, never equality of the fact's prose.
internal static string BuildCurrentFactIdentity(ConversationMessage message)
{
    if (message == null || message.EventSequence <= 0) return "";
    return message.EventSequence.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|"
        + message.GameDayIndex + "|" + message.GameHour + "|" + (message.PromptMemorySessionKey ?? "")
        + "|" + (message.Scene ?? "") + "|" + (message.Role ?? "");
}

internal static string BuildConversationMessageDedupeKey(ConversationMessage msg, ConversationSpeechTextOptions options)
	{
		if (msg == null)
		{
			return "";
		}
		string role = (msg.Role ?? "").Trim().ToLowerInvariant();
		string rawContent = ConversationSpeechTextRules.StripAfefPromptScopeLabel(NormalizeSceneHistoryPromptLineContent(msg.Content));
		if (TryNormalizeAfefFactLineForPrompt(rawContent, out var afefFactLine))
		{
			return "afef|" + afefFactLine;
		}
		string content = ConversationSpeechTextRules.StripAfefPromptScopeLabel(ConversationSpeechTextRules.NormalizeNativeConversationVisibleTextKey(msg.Content, options));
		if (role == "system")
		{
			return "fact|" + content;
		}
		string speaker = (msg.SpeakerName ?? "").Trim().ToLowerInvariant();
		string target = (msg.TargetName ?? "").Trim().ToLowerInvariant();
		return role + "|" + speaker + "|" + msg.SpeakerAgentIndex + "|" + msg.TargetAgentIndex + "|" + target + "|" + content;
	}

internal static void AppendConversationMessages(List<ConversationMessage> target, IEnumerable<ConversationMessage> source)
	{ SceneHistoryProjectionOwner.AppendConversationMessages(target, source); }

internal static List<ConversationMessage> SortConversationMessagesByEventSequence(List<ConversationMessage> messages)
	{ return SceneHistoryProjectionOwner.SortConversationMessagesByEventSequence(messages); }

internal static bool IsAfefConversationMessage(ConversationMessage msg)
	{
		if (msg == null)
		{
			return false;
		}
		return TryNormalizeAfefFactLineForPrompt(NormalizeSceneHistoryPromptLineContent(msg.Content), out var _);
	}

internal static List<ConversationMessage> KeepAfefFactsAndRecentConversationMessages(List<ConversationMessage> messages, int maxConversationMessages)
	{ return SceneHistoryProjectionOwner.KeepAfefFactsAndRecentConversationMessages(messages, maxConversationMessages); }

internal static bool TryConvertSceneMessageToStrictChatMessage(ConversationMessage msg, SceneHistoryMessageContext context, out object chatMessage, HashSet<string> currentAfefFactKeys = null)
	{
		chatMessage = null;
		if (msg == null)
		{
			return false;
		}
		string text = (msg.Role ?? "").Trim();
		string text2 = NormalizeSceneHistoryPromptLineContent(text.Equals("assistant", StringComparison.OrdinalIgnoreCase)
            ? DiplomacyReplyTextRules.CleanNpcText(msg.Content, msg.SpeakerName) : msg.Content);
		if (string.IsNullOrWhiteSpace(text2) || ConversationSpeechTextRules.IsLeakedPromptLineForShout(text2))
		{
			return false;
		}
		int npcAgentIndex = context.ViewerAgentIndex;
		string npcHeroId = context.ViewerHeroId;
		bool isAfefFact = TryNormalizeAfefFactLineForPrompt(text2, out var afefFactLine);
		bool isAfefNpcFact = afefFactLine.StartsWith(PromptComposerTexts.Raw("afef_npc_action"), StringComparison.Ordinal);
		bool isAfefPlayerFact = afefFactLine.StartsWith(PromptComposerTexts.Raw("afef_player_action"), StringComparison.Ordinal);
		bool isCurrentAfefFact = isAfefFact && (msg.PromptFactScopeCaptured
            ? msg.PromptIsCurrentFact
            : currentAfefFactKeys != null && currentAfefFactKeys.Contains(BuildCurrentFactIdentity(msg)));
		if (text.Equals("assistant", StringComparison.OrdinalIgnoreCase))
		{
			string text3 = NormalizeStrictSceneAssistantContent(text2, msg.SpeakerName);
			if (string.IsNullOrWhiteSpace(text3))
			{
				return false;
			}
			if (ConversationRoleClassificationOwner.IsViewerAssistant(msg, null, npcHeroId, npcAgentIndex, useStableIdentity: true))
			{
				chatMessage = MainPromptMessageAssemblyOwner.CreateCourierChatMessage("assistant", PrefixConversationMessageForPrompt(context, msg, string.IsNullOrWhiteSpace(msg.SpeakerName) ? "NPC" : msg.SpeakerName.Trim(), text3));
				return true;
			}
			string text4 = string.IsNullOrWhiteSpace(msg.SpeakerName) ? PromptComposerTexts.Get("residual_other_npc") : msg.SpeakerName.Trim();
			chatMessage = MainPromptMessageAssemblyOwner.CreateCourierChatMessage("user", PrefixConversationMessageForPrompt(context, msg, text4, PromptComposerTexts.Format("residual_heard_speech", ("NAME", text4), ("TEXT", text3))));
			return true;
		}
		if (text.Equals("user", StringComparison.OrdinalIgnoreCase))
		{
			string text5 = context.PlayerName;
			if (msg.TargetAgentIndex == npcAgentIndex || SameHero(msg.TargetHeroId, npcHeroId))
			{
				float promptDistanceMeters = context.UseDistanceLabels ? msg.PlayerDistanceMeters : -1f;
				chatMessage = MainPromptMessageAssemblyOwner.CreateCourierChatMessage("user", PrefixConversationMessageForPrompt(context, msg, text5, "【" + FormatScenePlayerDirectSpeechLabel(text5, promptDistanceMeters) + "】" + text2));
				return true;
			}
			if (msg.TargetAgentIndex >= 0)
			{
				string text6 = string.IsNullOrWhiteSpace(msg.TargetName) ? PromptComposerTexts.Get("residual_other_person") : msg.TargetName.Trim();
				chatMessage = MainPromptMessageAssemblyOwner.CreateCourierChatMessage("user", PrefixConversationMessageForPrompt(context, msg, text5, PromptComposerTexts.Format("residual_heard_target_speech", ("NAME", text5), ("TARGET", text6), ("TEXT", text2))));
				return true;
			}
			chatMessage = MainPromptMessageAssemblyOwner.CreateCourierChatMessage("user", PrefixConversationMessageForPrompt(context, msg, text5, PromptComposerTexts.Format("residual_heard_speech", ("NAME", text5), ("TEXT", text2))));
			return true;
		}
		if (text.Equals("system", StringComparison.OrdinalIgnoreCase))
		{
			if (isAfefFact && isAfefNpcFact)
			{
				chatMessage = MainPromptMessageAssemblyOwner.CreateCourierChatMessage("user", PrefixConversationMessageForPrompt(context, msg, "AFEF", BuildScopedAfefFactLineForPrompt(afefFactLine, isCurrentAfefFact)));
				return true;
			}
			string text6 = (isAfefFact && isAfefPlayerFact) ? BuildScopedAfefFactLineForPrompt(afefFactLine, isCurrentAfefFact) : (PromptComposerTexts.Get("residual_scene_fact") + text2);
			chatMessage = MainPromptMessageAssemblyOwner.CreateCourierChatMessage("user", PrefixConversationMessageForPrompt(context, msg, isAfefPlayerFact ? "AFEF" : PromptComposerTexts.Get("residual_system"), text6));
			return true;
		}
		chatMessage = MainPromptMessageAssemblyOwner.CreateCourierChatMessage("user", PrefixConversationMessageForPrompt(context, msg, string.IsNullOrWhiteSpace(msg.SpeakerName) ? PromptComposerTexts.Get("residual_record") : msg.SpeakerName.Trim(), text2));
		return true;
	}
internal static string PrefixConversationMessageForPrompt(SceneHistoryMessageContext context, ConversationMessage msg, string fallbackSpeaker, string content)
	{
		string text = (content ?? "").Replace("\r", "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		return BuildConversationMessageMetadataPrefix(context, msg, fallbackSpeaker) + text;
	}
internal static string BuildConversationMessageMetadataPrefix(SceneHistoryMessageContext context, ConversationMessage msg, string fallbackSpeaker)
	{
		string date = (msg?.GameDate ?? "").Trim();
		if (string.IsNullOrWhiteSpace(date)) date = context.GameDate;
		int hour = msg?.GameHour ?? -1;
		hour = hour < 0 ? context.GameHour : Math.Min(23, hour);
		string scene = (msg?.Scene ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		if (string.IsNullOrWhiteSpace(scene))
		{
			scene = context.Scene;
		}
		string speaker = (fallbackSpeaker ?? "").Trim();
		if (string.IsNullOrWhiteSpace(speaker))
		{
			speaker = (msg?.SpeakerName ?? "").Trim();
		}
		if (string.IsNullOrWhiteSpace(speaker))
		{
			speaker = PromptComposerTexts.Get("residual_record");
		}
		return "[" + date + " " + hour + PromptComposerTexts.Get("residual_hour_separator") + scene + "｜" + speaker + "] ";
	}

internal static List<string> KeepAfefFactsAndRecentHistoryLines(List<string> lines, int maxConversationLines)
	{ return SceneHistoryProjectionOwner.KeepAfefFactsAndRecentHistoryLines(lines, maxConversationLines); }

    private static bool SameHero(string left, string right) => !string.IsNullOrWhiteSpace(left)
        && !string.IsNullOrWhiteSpace(right) && string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);

    internal static List<object> Build(SceneHistoryMessageAssemblyInput input)
    {
        var messages = new List<object> { MainPromptMessageAssemblyOwner.CreateCourierChatMessage("system", MainPromptMessageAssemblyOwner.BuildConversationSystemPrompt(input.SystemPrompt)) };
        string revision = input.PersistentHistory?.Select(x => x?.PromptContextRevision).FirstOrDefault(x => !string.IsNullOrEmpty(x)) ?? "";
        if (revision.Length > 0 && messages[0] is ConversationPromptMessage first)
            messages[0] = new ConversationPromptMessage(first.role, first.content, revision: revision);
        AppendSections(messages, input.PrefixUserSections);
        var pending = (input.PendingCurrentFacts ?? Enumerable.Empty<ConversationMessage>())
            .Where(x => x != null && IsAfefConversationMessage(x)).Select(BuildCurrentFactIdentity);
        var keys = new HashSet<string>(pending.Where(x => !string.IsNullOrWhiteSpace(x)), StringComparer.Ordinal);
        var scene = new List<ConversationMessage>();
        AppendConversationMessages(scene, input.SceneHistory);
        AppendConversationMessages(scene, input.InjectedHistory);
        scene = SortConversationMessagesByEventSequence(scene);
        var history = new List<ConversationMessage>();
        AppendConversationMessages(history, input.PersistentHistory);
        AppendConversationMessages(history, scene);
        // Only request copies receive scope metadata; persistent history is never rewritten.
        history = SceneHistoryProjectionOwner.CapturePromptMessages(history);
        var present = new HashSet<string>(StringComparer.Ordinal);
        var unsequencedCurrent = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var fact in history)
        {
            if (!IsAfefConversationMessage(fact)) continue;
            string identity = BuildCurrentFactIdentity(fact);
            if (!string.IsNullOrEmpty(identity)) present.Add(identity);
            if (string.IsNullOrEmpty(identity) && fact.PromptFactScopeCaptured && fact.PromptIsCurrentFact)
            {
                string contentKey = BuildConversationMessageDedupeKey(fact, input.Context.SpeechTextOptions);
                unsequencedCurrent.TryGetValue(contentKey, out int count);
                unsequencedCurrent[contentKey] = count + 1;
            }
            if (!fact.PromptFactScopeCaptured)
            {
                fact.PromptFactScopeCaptured = true;
                fact.PromptIsCurrentFact = !string.IsNullOrEmpty(identity) && keys.Contains(identity);
            }
        }
        foreach (var fact in SceneHistoryProjectionOwner.CapturePromptMessages(input.PendingCurrentFacts))
        {
            if (!IsAfefConversationMessage(fact)) continue;
            string identity = BuildCurrentFactIdentity(fact);
            if (!string.IsNullOrEmpty(identity) && !present.Add(identity)) continue;
            if (string.IsNullOrEmpty(identity))
            {
                string contentKey = BuildConversationMessageDedupeKey(fact, input.Context.SpeechTextOptions);
                if (unsequencedCurrent.TryGetValue(contentKey, out int count) && count > 0)
                { unsequencedCurrent[contentKey] = count - 1; continue; }
            }
            fact.PromptFactScopeCaptured = fact.PromptIsCurrentFact = true;
            history.Add(fact);
        }
        var recalled = history.Where(x => x.PromptIsRecall).ToList();
        history = KeepAfefFactsAndRecentConversationMessages(history.Where(x => !x.PromptIsRecall).ToList(), input.HistoryLineLimit);
        history.InsertRange(0, recalled);
        string query = input.CurrentInputMessage?.Content ?? history.LastOrDefault(x => x.Role == "user" && !x.PromptIsRecall)?.Content ?? "";
        history = ConversationContextWindow.Select(history, query, input.WindowOptions);
        int group = 0;
        int lastGroup = ConversationContextWindow.Group(history).Count;
        foreach (var entry in history)
        {
            if (entry.Role != "system" && (group == 0 || entry.Role == "user")) group++;
            if (TryConvertSceneMessageToStrictChatMessage(entry, input.Context, out var message, keys))
            {
                if (message is ConversationPromptMessage speech && entry.Role != "system" && group < lastGroup)
                    message = new ConversationPromptMessage(speech.role, speech.content, group);
                messages.Add(message);
            }
        }
        AppendSections(messages, input.SuffixUserSections);
        if (input.CurrentInputMessage != null && TryConvertSceneMessageToStrictChatMessage(input.CurrentInputMessage, input.Context, out var current))
            messages.Add(current);
        return messages;
    }

    private static void AppendSections(List<object> messages, IEnumerable<string> sections)
    {
        if (sections == null) return;
        foreach (string section in sections)
            if (!string.IsNullOrWhiteSpace(section)) messages.Add(MainPromptMessageAssemblyOwner.CreateCourierChatMessage("user", section.Trim()));
    }
}

internal sealed class SceneHistoryMessageContext
{
    internal int ViewerAgentIndex;
    internal string ViewerHeroId, PlayerName, GameDate, Scene;
    internal int GameHour;
    internal bool UseDistanceLabels;
    internal ConversationSpeechTextOptions SpeechTextOptions;
}

internal sealed class SceneHistoryMessageAssemblyInput
{
    internal string SystemPrompt;
    internal IEnumerable<string> PrefixUserSections, SuffixUserSections;
    internal IEnumerable<ConversationMessage> PersistentHistory, SceneHistory, InjectedHistory, PendingCurrentFacts;
    internal ConversationMessage CurrentInputMessage;
    internal int HistoryLineLimit;
    internal ConversationContextOptions WindowOptions;
    internal SceneHistoryMessageContext Context;
}
