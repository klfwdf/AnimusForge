using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AnimusForge;

// Request-frequency pure main-chain message composition. Game/config reads belong to channel
// capture adapters; no provider, game object, history write or lifecycle state is held here.
internal static class MainPromptMessageAssemblyOwner
{
	internal static List<object> BuildCourierReplyMessages(string npcName, string playerName, string letterText, string extras, string deliveryFact, string history, IEnumerable<ConversationMessage> persistentMemoryRoleMessages, string npcRoleContext, string preprocessExcludedRuleBlock, string recentFacts, string senderIdentity, string senderRelationship, string currentLocationLine, string currentDateFact, string playerCustomRuleBlock)
	{
		string system = "你正在扮演 Mount & Blade II: Bannerlord 世界中的角色：" + npcName + "。\n"
			+ "这不是面对面对话。你刚刚通过信使收到" + playerName + "写给你的一封信。\n"
			+ "下面 messages 中 assistant 只代表你自己过去说过的话；role=user 包含来信、玩家发言、事实、旁听内容与规则。\n"
			+ "你必须根据来信者的公开身份选择合适称呼；如果来信者是君主或统治者，不要降格称为勋爵、领主或普通贵族。\n"
			+ "请只输出你要写在回信中的正文，不要写旁白、动作描写、系统说明或标签解释。\n"
			+ "如果你认为没有必要回信，可以完全空回复。\n"
			+ "如果你在回信中明确同意给玩家物品、部队、俘虏或固定资产，仍然按已注入的后处理规则在正文语义中表达，标签由后处理阶段生成。";
		if (!string.IsNullOrWhiteSpace(npcRoleContext))
		{
			system = system.TrimEnd() + "\n" + npcRoleContext.Trim();
		}
		if (!string.IsNullOrWhiteSpace(preprocessExcludedRuleBlock))
		{
			system = system.TrimEnd() + "\n" + preprocessExcludedRuleBlock.Trim();
		}
		system = JoinSystemSections(playerCustomRuleBlock, system);
		StringBuilder context = new StringBuilder();
		if (!string.IsNullOrWhiteSpace(senderIdentity))
		{
			context.AppendLine(senderIdentity.Trim());
		}
		if (!string.IsNullOrWhiteSpace(senderRelationship))
		{
			AppendCourierUserSection(context, "【来信者与你的关系】", senderRelationship);
		}
		if (!string.IsNullOrWhiteSpace(currentLocationLine))
		{
			AppendCourierUserSection(context, "【当前位置信息】", currentLocationLine);
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
			AppendCourierUserSection(context, "【本轮信件规则与补充】", extras);
		}
		List<object> messages = new List<object>
		{
			CreateCourierChatMessage("system", system)
		};
		string contextText = context.ToString().Trim();
		if (!string.IsNullOrWhiteSpace(contextText))
		{
			messages.Add(CreateCourierChatMessage("user", contextText));
		}
		AppendCourierPersistentMemoryRoleMessages(messages, persistentMemoryRoleMessages, npcName, playerName);
		StringBuilder current = new StringBuilder();
		current.AppendLine("【信件内容】");
		current.AppendLine(letterText ?? "");
		if (!string.IsNullOrWhiteSpace(deliveryFact))
		{
			current.AppendLine();
			current.AppendLine("【随信送达事实】");
			current.AppendLine("【当下行为】" + deliveryFact.Trim());
		}
		current.AppendLine();
		current.AppendLine("请以" + npcName + "的身份决定是否回信；如果回信，只输出信件正文。");
		messages.Add(CreateCourierChatMessage("user", current.ToString().Trim()));
		return messages;
	}

	internal static List<object> BuildInboundNpcLetterMessages(string npcName, string playerName, string seed, string extras, string fact, string history, IEnumerable<ConversationMessage> persistentMemoryRoleMessages, string npcRoleContext, string preprocessExcludedRuleBlock, string recentFacts, string playerIdentity, string playerRelationship, string currentLocationLine, string currentDateFact, int targetChars, string letterKind, string playerCustomRuleBlock)
	{
		string system = "你正在扮演 Mount & Blade II: Bannerlord 世界中的角色：" + npcName + "。\n"
			+ "这不是面对面对话。你决定主动通过信使给" + playerName + "写一封" + letterKind + "。\n"
			+ "下面 messages 中 assistant 只代表你自己过去说过的话；role=user 包含历史、事实、旁听内容、规则与本次写信意图。\n"
			+ "你必须根据收信人的公开身份选择合适称呼；如果收信人是君主或统治者，不要降格称为勋爵、领主或普通贵族。\n"
			+ "请只输出你要写在信中的正文，不要写旁白、动作描写、系统说明、标签解释或方括号动作标签。\n"
			+ "正文目标约" + targetChars + "字，只保留一个主要动机或一项具体请求。\n"
			+ "只能使用已提供的事实；不能宣布" + playerName + "已经同意，也不能把尚未发生的交易、承诺、外交或其他机制结果写成事实。";
		if (!string.IsNullOrWhiteSpace(npcRoleContext))
		{
			system = system.TrimEnd() + "\n" + npcRoleContext.Trim();
		}
		if (!string.IsNullOrWhiteSpace(preprocessExcludedRuleBlock))
		{
			system = system.TrimEnd() + "\n" + preprocessExcludedRuleBlock.Trim();
		}
		system = JoinSystemSections(playerCustomRuleBlock, system);
		StringBuilder context = new StringBuilder();
		if (!string.IsNullOrWhiteSpace(playerIdentity))
		{
			context.AppendLine(playerIdentity.Trim());
		}
		if (!string.IsNullOrWhiteSpace(playerRelationship))
		{
			AppendCourierUserSection(context, "【收信人与你的关系】", playerRelationship);
		}
		if (!string.IsNullOrWhiteSpace(currentLocationLine))
		{
			AppendCourierUserSection(context, "【当前位置信息】", currentLocationLine);
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
			AppendCourierUserSection(context, "【本轮信件规则与补充】", extras);
		}
		List<object> messages = new List<object>
		{
			CreateCourierChatMessage("system", system)
		};
		string contextText = context.ToString().Trim();
		if (!string.IsNullOrWhiteSpace(contextText))
		{
			messages.Add(CreateCourierChatMessage("user", contextText));
		}
		AppendCourierPersistentMemoryRoleMessages(messages, persistentMemoryRoleMessages, npcName, playerName);
		StringBuilder current = new StringBuilder();
		current.AppendLine("【本次 NPC 主动写信意图】");
		current.AppendLine(seed ?? "");
		if (!string.IsNullOrWhiteSpace(fact))
		{
			current.AppendLine();
			current.AppendLine("【送信事实】");
			current.AppendLine("【当下行为】" + fact.Trim());
		}
		current.AppendLine();
		current.AppendLine("请以" + npcName + "的身份写给" + playerName + "一封会由信使送达的" + letterKind + "，只输出信件正文。");
		messages.Add(CreateCourierChatMessage("user", current.ToString().Trim()));
		return messages;
	}

	internal static object CreateCourierChatMessage(string role, string content)
	{
		return new
		{
			role = role ?? "",
			content = content ?? ""
		};
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
		foreach (ConversationMessage message in historyMessages.Where((ConversationMessage x) => x != null && !string.IsNullOrWhiteSpace(x.Content)))
		{
			if (TryConvertCourierMemoryMessageToChatMessage(message, npcName, playerName, out var chatMessage))
			{
				messages.Add(chatMessage);
			}
		}
	}

	internal static bool TryConvertCourierMemoryMessageToChatMessage(ConversationMessage message, string npcName, string playerName, out object chatMessage)
	{
		chatMessage = null;
		if (message == null)
		{
			return false;
		}
		string content = (message.Content ?? "").Replace("\r", "").Trim();
		if (string.IsNullOrWhiteSpace(content))
		{
			return false;
		}
		string role = (message.Role ?? "").Trim();
		string speaker = (message.SpeakerName ?? "").Trim();
		string metadata = BuildCourierMemoryMetadataPrefix(message, string.IsNullOrWhiteSpace(speaker) ? "记录" : speaker);
		if (ConversationRoleClassificationOwner.IsViewerAssistant(message, npcName, null, -1, useStableIdentity: false))
		{
			chatMessage = CreateCourierChatMessage("assistant", metadata + StripCourierSpeakerPrefix(content, npcName));
			return true;
		}
		if (role.Equals("system", StringComparison.OrdinalIgnoreCase))
		{
			chatMessage = CreateCourierChatMessage("user", metadata + "【过往行为】" + StripCourierPromptScopeLabel(content));
			return true;
		}
		if (role.Equals("assistant", StringComparison.OrdinalIgnoreCase))
		{
			string otherSpeaker = string.IsNullOrWhiteSpace(speaker) ? "某NPC" : speaker;
			chatMessage = CreateCourierChatMessage("user", metadata + "【过往听闻】" + otherSpeaker + "说：" + StripCourierSpeakerPrefix(content, otherSpeaker));
			return true;
		}
		string player = string.IsNullOrWhiteSpace(playerName) ? "玩家" : playerName.Trim();
		chatMessage = CreateCourierChatMessage("user", metadata + StripCourierSpeakerPrefix(content, player));
		return true;
	}

	internal static string BuildCourierMemoryMetadataPrefix(ConversationMessage message, string fallbackSpeaker)
	{
		string date = string.IsNullOrWhiteSpace(message?.GameDate) ? ("第" + Math.Max(0, message?.GameDayIndex ?? 0) + "日") : message.GameDate.Trim();
		int hour = Math.Max(0, Math.Min(23, message?.GameHour ?? 0));
		string scene = (message?.Scene ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		string speaker = string.IsNullOrWhiteSpace(fallbackSpeaker) ? "记录" : fallbackSpeaker.Trim();
		if (string.IsNullOrWhiteSpace(scene))
		{
			return "[" + date + " " + hour + "时｜" + speaker + "] ";
		}
		return "[" + date + " " + hour + "时｜" + scene + "｜" + speaker + "] ";
	}

	internal static string StripCourierPromptScopeLabel(string text)
	{
		string value = (text ?? "").Trim();
		bool changed;
		do
		{
			changed = false;
			string[] prefixes = new string[4] { "【当下行为】", "【过往行为】", "[当下行为]", "[过往行为]" };
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
				messages.Add(CreateCourierChatMessage("user", text));
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

    private static string JoinSystemSections(string playerCustomRuleBlock, string system)
    {
        var sections = new List<string>(2);
        if (!string.IsNullOrWhiteSpace(playerCustomRuleBlock)) sections.Add(playerCustomRuleBlock.Trim());
        if (!string.IsNullOrWhiteSpace(system)) sections.Add(system.Trim());
        return string.Join("\n", sections);
    }
}
