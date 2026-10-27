using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AnimusForge;

// Request-frequency pure main-chain message composition. Game/config reads belong to channel
// capture adapters; no provider, game object, history write or lifecycle state is held here.
internal static class MainPromptMessageAssemblyOwner
{
	internal static List<object> BuildCourierReplyMessages(string npcName, string playerName, string letterText, string extras, string deliveryFact, string history, IEnumerable<ConversationMessage> persistentMemoryRoleMessages, string npcRoleContext, string preprocessExcludedRuleBlock, string recentFacts, string senderIdentity, string senderRelationship, string currentLocationLine, string currentDateFact, string playerCustomRuleBlock, IEnumerable<ConversationMessage> recallMessages = null)
	{
		string system = PromptComposerTexts.Format("main_courier_reply_system", ("NPC", npcName), ("PLAYER", playerName));
		if (!string.IsNullOrWhiteSpace(npcRoleContext))
		{
			system = system.TrimEnd() + "\n" + npcRoleContext.Trim();
		}
		if (!string.IsNullOrWhiteSpace(preprocessExcludedRuleBlock))
		{
			system = system.TrimEnd() + "\n" + preprocessExcludedRuleBlock.Trim();
		}
		system = BuildConversationSystemPrompt(JoinSystemSections(playerCustomRuleBlock, system));
		StringBuilder context = new StringBuilder();
		if (!string.IsNullOrWhiteSpace(senderIdentity))
		{
			context.AppendLine(senderIdentity.Trim());
		}
		if (!string.IsNullOrWhiteSpace(senderRelationship))
		{
			AppendCourierUserSection(context, PromptComposerTexts.Get("main_courier_000"), senderRelationship);
		}
		if (!string.IsNullOrWhiteSpace(currentLocationLine))
		{
			AppendCourierUserSection(context, PromptComposerTexts.Get("main_courier_001"), currentLocationLine);
		}
		if (!string.IsNullOrWhiteSpace(currentDateFact))
		{
			AppendCourierRawUserSection(context, currentDateFact);
		}
		if (!string.IsNullOrWhiteSpace(history))
		{
			AppendCourierRawUserSection(context, history);
		}
		if (!string.IsNullOrWhiteSpace(recentFacts))
		{
			AppendCourierRawUserSection(context, recentFacts);
		}
		if (!string.IsNullOrWhiteSpace(extras))
		{
			AppendCourierUserSection(context, PromptComposerTexts.Get("main_courier_002"), extras);
		}
		List<object> messages = new List<object>
		{
			CreateCourierChatMessage("system", system)
		};
		string contextText = context.ToString().Trim();
		AppendCourierPersistentMemoryRoleMessages(messages, persistentMemoryRoleMessages, npcName, playerName);
		AppendCourierRecallMessage(messages, recallMessages, npcName, playerName);
		if (!string.IsNullOrWhiteSpace(contextText))
		{
			messages.Add(CreateCourierChatMessage("user", PromptRuleBlockText.ProjectHeadersForPrompt(contextText)));
		}
		StringBuilder current = new StringBuilder();
		current.AppendLine(PromptComposerTexts.Get("main_courier_003"));
		current.AppendLine(letterText ?? "");
		if (!string.IsNullOrWhiteSpace(deliveryFact))
		{
			current.AppendLine();
			current.AppendLine(PromptComposerTexts.Get("main_courier_004"));
			current.AppendLine(PromptComposerTexts.Get("history_section_scope_current") + MemorySystemFactProjection.ProjectNpcFact(deliveryFact.Trim()));
		}
		current.AppendLine();
		current.AppendLine(PromptComposerTexts.Get("main_courier_006") + npcName + PromptComposerTexts.Get("main_courier_007"));
		messages.Add(CreateCourierChatMessage("user", current.ToString().Trim()));
		return messages;
	}

	internal static List<object> BuildInboundNpcLetterMessages(string npcName, string playerName, string seed, string extras, string fact, string history, IEnumerable<ConversationMessage> persistentMemoryRoleMessages, string npcRoleContext, string preprocessExcludedRuleBlock, string recentFacts, string playerIdentity, string playerRelationship, string currentLocationLine, string currentDateFact, int targetChars, string letterKind, string playerCustomRuleBlock, IEnumerable<ConversationMessage> recallMessages = null)
	{
		string system = PromptComposerTexts.Format("main_courier_inbound_system", ("NPC", npcName), ("PLAYER", playerName), ("KIND", letterKind), ("CHARS", targetChars));
		if (!string.IsNullOrWhiteSpace(npcRoleContext))
		{
			system = system.TrimEnd() + "\n" + npcRoleContext.Trim();
		}
		if (!string.IsNullOrWhiteSpace(preprocessExcludedRuleBlock))
		{
			system = system.TrimEnd() + "\n" + preprocessExcludedRuleBlock.Trim();
		}
		system = BuildConversationSystemPrompt(JoinSystemSections(playerCustomRuleBlock, system));
		StringBuilder context = new StringBuilder();
		if (!string.IsNullOrWhiteSpace(playerIdentity))
		{
			context.AppendLine(playerIdentity.Trim());
		}
		if (!string.IsNullOrWhiteSpace(playerRelationship))
		{
			AppendCourierUserSection(context, PromptComposerTexts.Get("main_courier_008"), playerRelationship);
		}
		if (!string.IsNullOrWhiteSpace(currentLocationLine))
		{
			AppendCourierUserSection(context, PromptComposerTexts.Get("main_courier_009"), currentLocationLine);
		}
		if (!string.IsNullOrWhiteSpace(currentDateFact))
		{
			AppendCourierRawUserSection(context, currentDateFact);
		}
		if (!string.IsNullOrWhiteSpace(history))
		{
			AppendCourierRawUserSection(context, history);
		}
		if (!string.IsNullOrWhiteSpace(recentFacts))
		{
			AppendCourierRawUserSection(context, recentFacts);
		}
		if (!string.IsNullOrWhiteSpace(extras))
		{
			AppendCourierUserSection(context, PromptComposerTexts.Get("main_courier_010"), extras);
		}
		List<object> messages = new List<object>
		{
			CreateCourierChatMessage("system", system)
		};
		string contextText = context.ToString().Trim();
		AppendCourierPersistentMemoryRoleMessages(messages, persistentMemoryRoleMessages, npcName, playerName);
		AppendCourierRecallMessage(messages, recallMessages, npcName, playerName);
		if (!string.IsNullOrWhiteSpace(contextText))
		{
			messages.Add(CreateCourierChatMessage("user", PromptRuleBlockText.ProjectHeadersForPrompt(contextText)));
		}
		StringBuilder current = new StringBuilder();
		current.AppendLine(PromptComposerTexts.Get("main_courier_011"));
		current.AppendLine(seed ?? "");
		if (!string.IsNullOrWhiteSpace(fact))
		{
			current.AppendLine();
			current.AppendLine(PromptComposerTexts.Get("main_courier_012"));
			current.AppendLine(PromptComposerTexts.Get("history_section_scope_current") + MemorySystemFactProjection.ProjectNpcFact(fact.Trim()));
		}
		current.AppendLine();
		current.AppendLine(PromptComposerTexts.Get("main_courier_014") + npcName + PromptComposerTexts.Get("main_courier_015") + playerName + PromptComposerTexts.Get("main_courier_016") + letterKind + PromptComposerTexts.Get("main_courier_017"));
		messages.Add(CreateCourierChatMessage("user", current.ToString().Trim()));
		return messages;
	}

	internal static object CreateCourierChatMessage(string role, string content)
	{
        return new ConversationPromptMessage(role ?? "", content ?? "");
	}

	internal static void AppendCourierRawUserSection(StringBuilder builder, string content)
	{
		if (builder == null || string.IsNullOrWhiteSpace(content))
		{
			return;
		}
		if (builder.Length > 0)
		{
			builder.AppendLine();
		}
		builder.AppendLine(content.Trim());
	}

	internal static void AppendCourierUserSection(StringBuilder builder, string title, string content)
	{
		if (builder == null || string.IsNullOrWhiteSpace(content))
		{
			return;
		}
		if (builder.Length > 0)
		{
			builder.AppendLine();
		}
		if (!string.IsNullOrWhiteSpace(title))
		{
			builder.AppendLine(title.Trim());
		}
		builder.AppendLine(content.Trim());
	}

	internal static void AppendCourierPersistentMemoryRoleMessages(List<object> messages, IEnumerable<ConversationMessage> historyMessages, string npcName, string playerName)
	{
		if (messages == null || historyMessages == null)
		{
			return;
		}
        var history = historyMessages.Where(x => x != null && !string.IsNullOrWhiteSpace(x.Content)).ToList();
        int totalGroups = 0;
        foreach (var speech in history)
            if (speech.Role != "system" && (totalGroups == 0 || speech.Role == "user")) totalGroups++;
        int group = 0;
		foreach (ConversationMessage message in history)
		{
            if (message.Role != "system" && (group == 0 || message.Role == "user")) group++;
			if (TryConvertCourierMemoryMessageToChatMessage(message, npcName, playerName, out var chatMessage))
			{
                if (message.Role != "system" && group < totalGroups && chatMessage is ConversationPromptMessage speech)
                    chatMessage = new ConversationPromptMessage(speech.role, speech.content, group);
				messages.Add(chatMessage);
			}
		}
	}

	// Recall varies with each letter; one quoted user block after the stable transcript keeps
	// the history prefix cacheable and cannot be mistaken for the latest assistant reply.
	internal static void AppendCourierRecallMessage(List<object> messages, IEnumerable<ConversationMessage> recallMessages, string npcName, string playerName)
	{
		if (messages == null || recallMessages == null) return;
		var lines = new List<string>();
		foreach (ConversationMessage message in recallMessages)
			if (TryConvertCourierMemoryMessageToChatMessage(message, npcName, playerName, out var chatMessage)
				&& chatMessage is ConversationPromptMessage quoted && !string.IsNullOrWhiteSpace(quoted.content))
				lines.Add(quoted.content.Trim());
		if (lines.Count > 0)
			messages.Add(CreateCourierChatMessage("user", PromptComposerTexts.Format("scene_prefix_recall", ("TEXT", string.Join("\n", lines)))));
	}

	internal static bool TryConvertCourierMemoryMessageToChatMessage(ConversationMessage message, string npcName, string playerName, out object chatMessage)
	{
		chatMessage = null;
		if (message == null)
		{
			return false;
		}
		string content = (message.Role ?? "").Trim().Equals("assistant", StringComparison.OrdinalIgnoreCase)
            ? DiplomacyReplyTextRules.CleanNpcText(message.Content ?? "", message.SpeakerName)
            : message.Content ?? "";
		content = content.Replace("\r", "").Trim();
		if (string.IsNullOrWhiteSpace(content))
		{
			return false;
		}
		string role = (message.Role ?? "").Trim();
		string speaker = (message.SpeakerName ?? "").Trim();
		string metadata = BuildCourierMemoryMetadataPrefix(message, string.IsNullOrWhiteSpace(speaker) ? PromptComposerTexts.Get("residual_record") : speaker);
		if (ConversationRoleClassificationOwner.IsViewerAssistant(message, npcName, null, -1, useStableIdentity: false))
		{
			chatMessage = CreateCourierChatMessage("assistant", metadata + StripCourierSpeakerPrefix(content, npcName));
			return true;
		}
		if (role.Equals("system", StringComparison.OrdinalIgnoreCase))
		{
			chatMessage = CreateCourierChatMessage("user", metadata + PromptComposerTexts.Get("history_section_scope_historical") + MemorySystemFactProjection.ProjectNpcFact(StripCourierPromptScopeLabel(content)));
			return true;
		}
		if (role.Equals("assistant", StringComparison.OrdinalIgnoreCase))
		{
			string otherSpeaker = string.IsNullOrWhiteSpace(speaker) ? PromptComposerTexts.Get("residual_other_npc") : speaker;
			chatMessage = CreateCourierChatMessage("user", metadata + PromptComposerTexts.Get("residual_past_heard") + otherSpeaker + PromptComposerTexts.Get("residual_says") + StripCourierSpeakerPrefix(content, otherSpeaker));
			return true;
		}
		string player = string.IsNullOrWhiteSpace(playerName) ? PromptComposerTexts.Raw("player_fallback") : playerName.Trim();
		chatMessage = CreateCourierChatMessage("user", metadata + StripCourierSpeakerPrefix(content, player));
		return true;
	}

	internal static string BuildCourierMemoryMetadataPrefix(ConversationMessage message, string fallbackSpeaker)
	{
		string date = string.IsNullOrWhiteSpace(message?.GameDate) ? (PromptComposerTexts.Get("residual_day_prefix") + Math.Max(0, message?.GameDayIndex ?? 0) + PromptComposerTexts.Get("residual_day_suffix")) : message.GameDate.Trim();
		int hour = Math.Max(0, Math.Min(23, message?.GameHour ?? 0));
		string scene = (message?.Scene ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		string speaker = string.IsNullOrWhiteSpace(fallbackSpeaker) ? PromptComposerTexts.Get("residual_record") : fallbackSpeaker.Trim();
		if (string.IsNullOrWhiteSpace(scene))
		{
			return "[" + date + " " + hour + PromptComposerTexts.Get("residual_hour_separator") + speaker + "] ";
		}
		return "[" + date + " " + hour + PromptComposerTexts.Get("residual_hour_separator") + scene + "｜" + speaker + "] ";
	}

	internal static string StripCourierPromptScopeLabel(string text)
	{
		string value = (text ?? "").Trim();
		bool changed;
		do
		{
			changed = false;
			string[] prefixes = new string[4] { PromptComposerTexts.Raw("history_section_scope_current"), PromptComposerTexts.Raw("main_history_raw_019"), PromptComposerTexts.Raw("main_history_raw_031"), PromptComposerTexts.Raw("main_history_raw_032") };
			foreach (string prefix in prefixes)
			{
				if (value.StartsWith(prefix, StringComparison.Ordinal))
				{
					value = value.Substring(prefix.Length).Trim();
					changed = true;
				}
			}
		}
		while (changed);
		return value;
	}

	internal static string StripCourierSpeakerPrefix(string content, string speaker)
	{
		string text = (content ?? "").Trim();
		string name = (speaker ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(name))
		{
			return text;
		}
		string[] prefixes = new string[2] { name + ": ", name + "：" };
		foreach (string prefix in prefixes)
		{
			if (text.StartsWith(prefix, StringComparison.Ordinal))
			{
				return text.Substring(prefix.Length).Trim();
			}
		}
		return text;
	}

	internal static string BuildSceneCompositeUserBlock(string sceneHistoryUserBlock, params string[] extraSections)
{
	List<string> list = new List<string>();
	if (!string.IsNullOrWhiteSpace(sceneHistoryUserBlock))
	{
		list.Add(sceneHistoryUserBlock.Trim());
	}
	if (extraSections != null)
	{
		for (int i = 0; i < extraSections.Length; i++)
		{
			string text = (extraSections[i] ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text))
			{
				list.Add(text);
			}
		}
	}
	return string.Join("\n\n", list.Where((string x) => !string.IsNullOrWhiteSpace(x))).Trim();
}

	internal static void AppendStrictSceneUserSections(List<object> messages, IEnumerable<string> sections)
	{
		if (messages == null || sections == null)
		{
			return;
		}
		foreach (string section in sections)
		{
			string text = (section ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text))
			{
				messages.Add(CreateCourierChatMessage("user", PromptRuleBlockText.ProjectHeadersForPrompt(text)));
			}
		}
	}
    // These are the existing channel message groupings, not new prompt policy.
    // Preserve grouping as well as text: provider history must not be flattened or reordered.
    internal enum SceneSingleSpeakerLayout { GroupFallback, Passive, GroupTurn, ImmediateReaction, CompactArrival }

    internal static string[] BuildSceneSingleSpeakerPrefixSections(SceneSingleSpeakerLayout layout,
        string privateRecent, string persistedHistory, string runtime, string local,
        string currentFact, string trust, string misc, string patience, string knowledge, string rules)
    {
        string ruleBlock = BuildSceneCompositeUserBlock("", knowledge, rules);
        switch (layout)
        {
            case SceneSingleSpeakerLayout.GroupFallback:
                return new[] { privateRecent, persistedHistory, runtime, local, currentFact, trust, misc, patience, ruleBlock };
            case SceneSingleSpeakerLayout.Passive:
                return new[] { privateRecent, persistedHistory, runtime, local, trust, misc, patience, ruleBlock };
            case SceneSingleSpeakerLayout.GroupTurn:
                return new[] { privateRecent, persistedHistory,
                    BuildSceneCompositeUserBlock("", runtime, local, currentFact, trust, misc, patience), ruleBlock };
            case SceneSingleSpeakerLayout.ImmediateReaction:
            case SceneSingleSpeakerLayout.CompactArrival:
                string reaction = BuildSceneCompositeUserBlock("", local, trust, misc);
                reaction = BuildSceneCompositeUserBlock("", reaction, currentFact);
                return layout == SceneSingleSpeakerLayout.ImmediateReaction
                    ? new[] { privateRecent, persistedHistory, BuildSceneCompositeUserBlock("", runtime, knowledge, reaction) }
                    : new[] { privateRecent, persistedHistory, BuildSceneCompositeUserBlock("", runtime, knowledge), reaction };
            default:
                throw new ArgumentOutOfRangeException(nameof(layout));
        }
    }

    internal static string BuildSceneSharedSystemPrompt(string publicRules)
        => BuildSceneCompositeUserBlock("", publicRules, PromptComposerTexts.Get("scene_prefix_system"));

    internal static string BuildSceneReactionSystemPrompt(string identityOverride, string ruleBlock, string customRoleIntro)
        => BuildSceneCompositeUserBlock("", BuildSceneCompositeUserBlock("", identityOverride, ruleBlock), customRoleIntro);

    internal static AnimusForge.Refactor.Contracts.DetachedInteractionPromptSections BuildExternalScenePromptSections(
        string mainSystem, string mainHistory, string postSystem, string postHistory, string postUser)
    {
        var main = new AnimusForge.Refactor.Contracts.DetachedPromptSections(
            new[] { mainSystem }, new[] { mainHistory }, Array.Empty<string>(), appendCurrentPlayerInput: true);
        var postprocess = new AnimusForge.Refactor.Contracts.DetachedPostprocessPromptSections(
            new[] { postSystem }, new[] { postHistory }, new[] { postUser }, appendLatestVisibleReply: true);
        return new AnimusForge.Refactor.Contracts.DetachedInteractionPromptSections(main, postprocess);
    }

    // Preserve the composed system; package edition does not lock reply language.
    internal static string BuildConversationSystemPrompt(string system)
        => PromptComposerTexts.ApplyOutputLanguage(system);

    private static string JoinSystemSections(string playerCustomRuleBlock, string system)
    {
        var sections = new List<string>(2);
        if (!string.IsNullOrWhiteSpace(playerCustomRuleBlock)) sections.Add(playerCustomRuleBlock.Trim());
        if (!string.IsNullOrWhiteSpace(system)) sections.Add(system.Trim());
        return string.Join("\n", sections);
    }
}
