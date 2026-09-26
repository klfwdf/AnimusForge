using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using Helpers;
using Newtonsoft.Json;
using SandBox;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Modules;
using AnimusForge.Refactor.Runtime;

namespace AnimusForge;

public sealed partial class CourierDeliveryBehavior
{
	private static List<object> BuildCourierReplyMessages(Hero recipient, CourierSession session, string extras, string deliveryFactForPrompt = null, string prebuiltHistory = null, IEnumerable<ConversationMessage> persistentMemoryRoleMessages = null, string npcRoleContext = null, string preprocessExcludedRuleBlock = null)
	{
		string npcName = recipient?.Name?.ToString() ?? "NPC";
		string playerName = MyBehavior.BuildPlayerPublicDisplayNameForExternal(recipient);
		if (string.IsNullOrWhiteSpace(playerName))
		{
			playerName = Hero.MainHero?.Name?.ToString() ?? "玩家";
		}
		string deliveryFact = string.IsNullOrWhiteSpace(deliveryFactForPrompt) ? (session?.DeliveryFactText ?? "") : deliveryFactForPrompt;
		string history = prebuiltHistory ?? MyBehavior.BuildHistoryContextForExternal(recipient, DuelSettings.GetDailyConversationHistoryLineLimitForExternal(), session.LetterText, deliveryFact);
		string recentFacts = MyBehavior.BuildRecentNpcFactContextForExternal(recipient, 6);
		string senderIdentity = MyBehavior.BuildPlayerCourierSenderIdentityForExternal(recipient);
		string senderRelationship = MyBehavior.BuildNpcPlayerKinshipPromptLineForExternal(recipient);
		string currentLocationLine = BuildCourierCurrentLocationLine(recipient);
		string currentDateFact = MyBehavior.BuildCurrentDateFactForExternal();
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
		system = MyBehavior.AppendPlayerCustomPromptRuleToSystemPromptForExternal(system);
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
		current.AppendLine(session.LetterText ?? "");
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

	private static List<object> BuildInboundNpcLetterMessages(Hero sender, CourierSession session, string seed, string extras, string factForPrompt = null, string prebuiltHistory = null, IEnumerable<ConversationMessage> persistentMemoryRoleMessages = null, string npcRoleContext = null, string preprocessExcludedRuleBlock = null)
	{
		string npcName = sender?.Name?.ToString() ?? session?.SenderName ?? "NPC";
		string playerName = MyBehavior.BuildPlayerPublicDisplayNameForExternal(sender);
		if (string.IsNullOrWhiteSpace(playerName))
		{
			playerName = Hero.MainHero?.Name?.ToString() ?? "玩家";
		}
		string fact = string.IsNullOrWhiteSpace(factForPrompt) ? (session?.DeliveryFactText ?? "") : factForPrompt;
		string history = prebuiltHistory ?? MyBehavior.BuildHistoryContextForExternal(sender, DuelSettings.GetDailyConversationHistoryLineLimitForExternal(), null, fact);
		string recentFacts = MyBehavior.BuildRecentNpcFactContextForExternal(sender, 6);
		string playerIdentity = MyBehavior.BuildPlayerCourierRecipientIdentityForExternal(sender);
		string playerRelationship = MyBehavior.BuildNpcPlayerKinshipPromptLineForExternal(sender);
		string currentLocationLine = BuildCourierCurrentLocationLine(sender);
		string currentDateFact = MyBehavior.BuildCurrentDateFactForExternal();
		int targetChars = ClampInt(DuelSettings.GetSettings()?.NpcInitiatedLetterTargetChars ?? 220, 80, 1000);
		string letterKind = GetInboundLetterKindDisplayText(session);
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
		system = MyBehavior.AppendPlayerCustomPromptRuleToSystemPromptForExternal(system);
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

	private static string BuildCourierCurrentLocationLine(Hero recipient)
	{
		try
		{
			MobileParty party = ResolveCourierPromptParty(recipient);
			Settlement currentSettlement = recipient?.CurrentSettlement ?? party?.CurrentSettlement;
			if (currentSettlement != null)
			{
				return "你当前位于" + FormatCourierSettlementNameWithType(currentSettlement) + "。";
			}
			if (party == null)
			{
				return "";
			}
			if (MapSeaContextGuard.IsMobilePartyAtSeaOrOnWater(party))
			{
				Settlement nearest = FindNearestSettlementForCourierPrompt(party);
				string nearestName = FormatCourierSettlementNameWithType(nearest);
				string locationLine = string.IsNullOrWhiteSpace(nearestName)
					? "你正位于海上。"
					: "你正位于" + nearestName + "附近的海上。";
				string shipText = MapSeaContextGuard.BuildMobilePartyShipPromptText(party);
				if (!string.IsNullOrWhiteSpace(shipText))
				{
					locationLine += "舰船：" + shipText + "。";
				}
				return locationLine;
			}
			string terrainLabel = MapSeaContextGuard.BuildMobilePartyLandTerrainPromptLabel(party);
			if (string.IsNullOrWhiteSpace(terrainLabel))
			{
				terrainLabel = "野外";
			}
			Settlement landNearest = FindNearestSettlementForCourierPrompt(party);
			string landNearestName = FormatCourierSettlementNameWithType(landNearest);
			return string.IsNullOrWhiteSpace(landNearestName)
				? "你当前位于" + terrainLabel + "。"
				: "你当前位于" + landNearestName + "附近的" + terrainLabel + "。";
		}
		catch
		{
			return "";
		}
	}

	private static MobileParty ResolveCourierPromptParty(Hero hero)
	{
		try
		{
			if (hero?.PartyBelongedTo != null && hero.PartyBelongedTo.IsActive)
			{
				return hero.PartyBelongedTo;
			}
		}
		catch
		{
		}
		try
		{
			PartyBase prisonerParty = hero?.PartyBelongedToAsPrisoner;
			if (prisonerParty?.MobileParty != null && prisonerParty.MobileParty.IsActive)
			{
				return prisonerParty.MobileParty;
			}
		}
		catch
		{
		}
		return null;
	}

	private static Settlement FindNearestSettlementForCourierPrompt(MobileParty party)
	{
		return MapSeaContextGuard.FindNearestSettlementForPrompt(party);
	}

	private static string FormatCourierSettlementNameWithType(Settlement settlement)
	{
		return MapSeaContextGuard.FormatSettlementNameWithTypeForPrompt(settlement);
	}

	private static object CreateCourierChatMessage(string role, string content)
	{
		return new
		{
			role = role ?? "",
			content = content ?? ""
		};
	}

	private static void AppendCourierRawUserSection(StringBuilder builder, string content)
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

	private static void AppendCourierUserSection(StringBuilder builder, string title, string content)
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

	private static void AppendCourierPersistentMemoryRoleMessages(List<object> messages, IEnumerable<ConversationMessage> historyMessages, string npcName, string playerName)
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

	private static bool TryConvertCourierMemoryMessageToChatMessage(ConversationMessage message, string npcName, string playerName, out object chatMessage)
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
		if (role.Equals("assistant", StringComparison.OrdinalIgnoreCase) && IsCourierMemorySpeakerRecipient(speaker, npcName))
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

	private static bool IsCourierMemorySpeakerRecipient(string speaker, string npcName)
	{
		string left = (speaker ?? "").Trim();
		string right = (npcName ?? "").Trim();
		return !string.IsNullOrWhiteSpace(left) && !string.IsNullOrWhiteSpace(right) && string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
	}

	private static string BuildCourierMemoryMetadataPrefix(ConversationMessage message, string fallbackSpeaker)
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

	private static string StripCourierPromptScopeLabel(string text)
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

	private static string StripCourierSpeakerPrefix(string content, string speaker)
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

	private static string BuildDeliveryFactText(CourierSession session, bool delivered, Hero recipient = null, bool commitPlayerCraftInspection = false)
	{
		if (session == null)
		{
			return "";
		}
		string playerName = MyBehavior.BuildPlayerPublicDisplayNameForExternal(recipient);
		if (string.IsNullOrWhiteSpace(playerName))
		{
			playerName = Hero.MainHero?.Name?.ToString() ?? "玩家";
		}
		StringBuilder sb = new StringBuilder();
		if (delivered)
		{
			sb.Append("[AFEF玩家行为补充] ").Append(playerName).Append("通过信使向你寄来一封信。");
		}
		else
		{
			sb.Append("[AFEF玩家行为补充] ").Append(playerName).Append("已安排信使携带一封信出发。");
		}
		string crewSummary = BuildCourierCrewSummaryForPrompt(session);
		if (!string.IsNullOrWhiteSpace(crewSummary))
		{
			sb.Append("\n[AFEF玩家行为补充] ").Append(playerName)
				.Append(delivered ? "派来送达这封信的信使队成员为：" : "安排的送信队伍成员为：")
				.Append(crewSummary)
				.Append("。这些成员只负责送信与护送回信，不属于随信赠与或转移给你的物资或部队。");
		}
		foreach (CourierCargoEntry entry in session.Entries ?? new List<CourierCargoEntry>())
		{
			if (entry == null || entry.Amount <= 0)
			{
				continue;
			}
			string verb = delivered ? "通过信使" : "准备通过信使";
			string factEntryName = (entry.Kind == "item" || entry.Kind == "show_item")
				? GetCourierLetterTransferFactDescriptionForExternal(entry.Id, 0u, entry.Name)
				: entry.Name;
			if (delivered
				&& entry.Delivered
				&& (entry.Kind == "item" || entry.Kind == "show_item")
				&& !string.IsNullOrWhiteSpace(entry.Id))
			{
				string observerKey = (recipient?.StringId ?? "").Trim();
				if (string.IsNullOrWhiteSpace(observerKey))
				{
					observerKey = MyBehavior.BuildRuleTargetKeyForExternal(recipient, recipient?.CharacterObject, -1);
				}
				factEntryName = RewardSystemBehavior.DecoratePlayerCraftedAfefItemNameForExternal(
					entry.Id,
					0u,
					factEntryName,
					recipient,
					recipient?.CharacterObject,
					observerKey,
					entry.Kind == "show_item" ? "show" : "give",
					commitPlayerCraftInspection);
			}
			if (entry.Kind == "gold")
			{
				sb.Append("\n[AFEF玩家行为补充] ").Append(playerName).Append(verb).Append("转移了 ").Append(entry.Amount).Append(" 第纳尔").Append(delivered ? BuildCourierCargoValueSuffix(entry) : "").Append("。");
			}
			else if (entry.Kind == "item")
			{
				sb.Append("\n[AFEF玩家行为补充] ").Append(playerName).Append(verb).Append("转移了 ").Append(entry.Amount).Append(" 个 ").Append(factEntryName).Append(delivered ? BuildCourierCargoValueSuffix(entry) : "").Append("。");
			}
			else if (entry.Kind == "show_gold")
			{
				sb.Append("\n[AFEF玩家行为补充] ").Append(playerName).Append(verb).Append("展示了 ").Append(entry.Amount).Append(" 第纳尔，但没有转移所有权。");
			}
			else if (entry.Kind == "show_item")
			{
				sb.Append("\n[AFEF玩家行为补充] ").Append(playerName).Append(verb).Append("展示了 ").Append(entry.Amount).Append(" 个 ").Append(factEntryName).Append("，但没有转移所有权。");
			}
			else if (entry.Kind == "troop")
			{
				sb.Append("\n[AFEF玩家行为补充] ").Append(playerName).Append(verb).Append("转移了 ").Append(entry.Amount).Append(" 名 ").Append(entry.Name).Append(delivered ? BuildCourierCargoValueSuffix(entry) : "").Append("。");
			}
			else if (entry.Kind == "prisoner")
			{
				sb.Append("\n[AFEF玩家行为补充] ").Append(playerName).Append(verb).Append(entry.IsHero ? "转移了俘虏 " : "转移了俘虏 ").Append(entry.IsHero ? entry.Name : (entry.Amount + " 名 " + entry.Name)).Append(delivered ? BuildCourierCargoValueSuffix(entry) : "").Append("。");
			}
			else if (entry.Kind == "settlement")
			{
				sb.Append("\n[AFEF玩家行为补充] ").Append(playerName).Append(verb).Append("转移了固定资产 ").Append(entry.Name).Append(delivered ? BuildCourierCargoValueSuffix(entry) : "").Append("。");
			}
		}
		return sb.ToString().Trim();
	}

	private static string BuildInboundDeliveryFactText(CourierSession session, bool delivered, Hero sender = null)
	{
		if (session == null)
		{
			return "";
		}
		string senderName = string.IsNullOrWhiteSpace(session.SenderName) ? (sender?.Name?.ToString() ?? "NPC") : session.SenderName.Trim();
		string playerName = MyBehavior.BuildPlayerPublicDisplayNameForExternal(sender);
		if (string.IsNullOrWhiteSpace(playerName))
		{
			playerName = Hero.MainHero?.Name?.ToString() ?? "玩家";
		}
		string letterKind = GetInboundLetterKindDisplayText(session);
		string fact = delivered
			? "[AFEF NPC行为补充] " + senderName + "通过信使给" + playerName + "送来一封" + letterKind + "。"
			: "[AFEF NPC行为补充] " + senderName + "已经派出信使，准备把一封" + letterKind + "送给" + playerName + "。";
		if (!string.IsNullOrWhiteSpace(session.InboundIntentFact))
		{
			fact += "\n" + session.InboundIntentFact.Trim();
		}
		try
		{
			IFaction senderFaction = sender?.MapFaction;
			IFaction playerFaction = Hero.MainHero?.MapFaction;
			if (senderFaction != null && playerFaction != null && senderFaction.IsAtWarWith(playerFaction))
			{
				fact += "\n[AFEF NPC行为补充] " + senderName + "所属势力与" + playerName + "所属势力当前仍处于战争状态；这封信属于私人或秘密通信，不代表停战、议和已经成立或敌对关系已经解除。";
			}
		}
		catch
		{
		}
		string crewSummary = BuildCourierCrewSummaryForPrompt(session);
		if (!string.IsNullOrWhiteSpace(crewSummary))
		{
			fact += "\n[AFEF NPC行为补充] " + senderName + "派出的信使队成员为：" + crewSummary + "。这些成员只负责送信，不属于随信赠与或转移给" + playerName + "的物资或部队。";
		}
		return fact;
	}

	private static string GetInboundLetterKindDisplayText(CourierSession session)
	{
		string kind = (session?.InboundLetterKind ?? "").Trim();
		if (string.Equals(kind, InboundLetterKindNeedRequest, StringComparison.OrdinalIgnoreCase)) return "请求信";
		if (string.Equals(kind, InboundLetterKindDiplomacy, StringComparison.OrdinalIgnoreCase)) return "外交信";
		if (string.Equals(kind, InboundLetterKindPersonal, StringComparison.OrdinalIgnoreCase)) return "私人来信";
		return "来信";
	}

	private static string BuildCourierCrewSummaryForPrompt(CourierSession session)
	{
		List<string> parts = new List<string>();
		foreach (CourierCargoEntry entry in session?.CrewEntries ?? new List<CourierCargoEntry>())
		{
			if (entry == null || entry.Amount <= 0)
			{
				continue;
			}
			if (!string.Equals(entry.Kind ?? "", "crew", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			string name = string.IsNullOrWhiteSpace(entry.Name) ? (entry.Id ?? "").Trim() : entry.Name.Trim();
			if (string.IsNullOrWhiteSpace(name))
			{
				continue;
			}
			string role = entry.IsHero ? "角色" : "士兵";
			parts.Add(name + "×" + Math.Max(1, entry.Amount) + "（" + role + "）");
		}
		return parts.Count == 0 ? "" : string.Join("，", parts);
	}

	private static string BuildCourierCargoValueSuffix(CourierCargoEntry entry)
	{
		long value = EstimateCourierCargoEntryTotalValue(entry);
		return value > 0L ? ("（估值约 " + value + " 第纳尔）") : "";
	}

	private static long EstimateCourierCargoEntryTotalValue(CourierCargoEntry entry)
	{
		if (entry == null || entry.Amount <= 0)
		{
			return 0L;
		}
		string kind = (entry.Kind ?? "").Trim();
		if (string.Equals(kind, "gold", StringComparison.OrdinalIgnoreCase))
		{
			return Math.Max(0, entry.Amount);
		}
		int unitValue = Math.Max(0, entry.GuidePriceDenars);
		if (unitValue <= 0 && string.Equals(kind, "item", StringComparison.OrdinalIgnoreCase))
		{
			unitValue = EstimateCourierItemUnitValue(ResolveItem(entry.Id));
		}
		if (unitValue <= 0)
		{
			return 0L;
		}
		int amount = string.Equals(kind, "settlement", StringComparison.OrdinalIgnoreCase) || entry.IsHero ? 1 : Math.Max(1, entry.Amount);
		return (long)amount * unitValue;
	}

	private static string BuildPendingPayloadSummary(PendingCourierFlow flow)
	{
		if (flow == null)
		{
			return "";
		}
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("信使成员：");
		sb.AppendLine("  " + RosterSummary(flow.CrewRoster));
		if (flow.Mode == CourierPayloadMode.Normal || flow.SelectedEntries.Count == 0)
		{
			sb.AppendLine("随信内容：仅信件。");
			return sb.ToString().Trim();
		}
		sb.AppendLine("随信内容：");
		foreach (CourierCargoEntry entry in flow.SelectedEntries)
		{
			if (entry == null)
			{
				continue;
			}
			sb.Append("  · ");
			if (entry.Kind == "gold")
			{
				sb.Append("发送 ").Append(entry.Amount).Append(" 第纳尔");
			}
			else if (entry.Kind == "show_gold")
			{
				sb.Append("展示 ").Append(entry.Amount).Append(" 第纳尔");
			}
			else if (entry.Kind == "item")
			{
				sb.Append("发送 ").Append(entry.Amount).Append(" 个 ").Append(entry.Name);
			}
			else if (entry.Kind == "show_item")
			{
				sb.Append("展示 ").Append(entry.Amount).Append(" 个 ").Append(entry.Name);
			}
			else if (entry.Kind == "troop")
			{
				sb.Append("转移 ").Append(entry.Amount).Append(" 名 ").Append(entry.Name);
			}
			else if (entry.Kind == "prisoner")
			{
				sb.Append(entry.IsHero ? ("转移俘虏 " + entry.Name) : ("转移 " + entry.Amount + " 名 " + entry.Name + " 俘虏"));
			}
			else if (entry.Kind == "settlement")
			{
				sb.Append("转移固定资产 ").Append(entry.Name);
			}
			sb.AppendLine();
		}
		return sb.ToString().Trim();
	}
}
