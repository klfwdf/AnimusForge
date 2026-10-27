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
			playerName = Hero.MainHero?.Name?.ToString() ?? ConversationRuntimeTexts.Prompt("AF_CONV_Prompt_0530");
		}
		string deliveryFact = string.IsNullOrWhiteSpace(deliveryFactForPrompt) ? (session?.DeliveryFactText ?? "") : deliveryFactForPrompt;
        persistentMemoryRoleMessages = ConversationContextWindow.SelectWindow(persistentMemoryRoleMessages, session?.LetterText, ConversationContextSettingsCapture.Capture(), out var recallMessages);
		string history = prebuiltHistory ?? MyBehavior.BuildHistoryContextForExternal(recipient, DuelSettings.GetDailyConversationHistoryLineLimitForExternal(), session.LetterText, deliveryFact);
		string recentFacts = MyBehavior.BuildRecentNpcFactContextForExternal(recipient, 6);
		string senderIdentity = MyBehavior.BuildPlayerCourierSenderIdentityForExternal(recipient);
		string senderRelationship = MyBehavior.BuildNpcPlayerKinshipPromptLineForExternal(recipient);
		string currentLocationLine = BuildCourierCurrentLocationLine(recipient);
		string currentDateFact = MyBehavior.BuildCurrentDateFactForExternal();
        var messages = MainPromptMessageAssemblyOwner.BuildCourierReplyMessages(npcName, playerName, session.LetterText, extras, deliveryFact, history, persistentMemoryRoleMessages, npcRoleContext, preprocessExcludedRuleBlock, recentFacts, senderIdentity, senderRelationship, currentLocationLine, currentDateFact, MyBehavior.AppendPlayerCustomPromptRuleToSystemPromptForExternal(string.Empty), recallMessages);
        return messages;
	}

	private static List<object> BuildInboundNpcLetterMessages(Hero sender, CourierSession session, string seed, string extras, string factForPrompt = null, string prebuiltHistory = null, IEnumerable<ConversationMessage> persistentMemoryRoleMessages = null, string npcRoleContext = null, string preprocessExcludedRuleBlock = null)
	{
		string npcName = sender?.Name?.ToString() ?? session?.SenderName ?? "NPC";
		string playerName = MyBehavior.BuildPlayerPublicDisplayNameForExternal(sender);
		if (string.IsNullOrWhiteSpace(playerName))
		{
			playerName = Hero.MainHero?.Name?.ToString() ?? ConversationRuntimeTexts.Prompt("AF_CONV_Prompt_0531");
		}
		string fact = string.IsNullOrWhiteSpace(factForPrompt) ? (session?.DeliveryFactText ?? "") : factForPrompt;
        persistentMemoryRoleMessages = ConversationContextWindow.SelectWindow(persistentMemoryRoleMessages, seed, ConversationContextSettingsCapture.Capture(), out var recallMessages);
		string history = prebuiltHistory ?? MyBehavior.BuildHistoryContextForExternal(sender, DuelSettings.GetDailyConversationHistoryLineLimitForExternal(), null, fact);
		string recentFacts = MyBehavior.BuildRecentNpcFactContextForExternal(sender, 6);
		string playerIdentity = MyBehavior.BuildPlayerCourierRecipientIdentityForExternal(sender);
		string playerRelationship = MyBehavior.BuildNpcPlayerKinshipPromptLineForExternal(sender);
		string currentLocationLine = BuildCourierCurrentLocationLine(sender);
		string currentDateFact = MyBehavior.BuildCurrentDateFactForExternal();
		int targetChars = ClampInt(DuelSettings.GetSettings()?.NpcInitiatedLetterTargetChars ?? 220, 80, 1000);
		string letterKind = GetInboundLetterKindDisplayText(session);
        var messages = MainPromptMessageAssemblyOwner.BuildInboundNpcLetterMessages(npcName, playerName, seed, extras, fact, history, persistentMemoryRoleMessages, npcRoleContext, preprocessExcludedRuleBlock, recentFacts, playerIdentity, playerRelationship, currentLocationLine, currentDateFact, targetChars, letterKind, MyBehavior.AppendPlayerCustomPromptRuleToSystemPromptForExternal(string.Empty), recallMessages);
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
				return ConversationRuntimeTexts.Prompt("AF_CONV_Prompt_0532") + FormatCourierSettlementNameWithType(currentSettlement) + "。";
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
					? ConversationRuntimeTexts.Prompt("AF_CONV_Prompt_0533")
					: ConversationRuntimeTexts.Prompt("AF_CONV_Prompt_0534") + nearestName + ConversationRuntimeTexts.Prompt("AF_CONV_Prompt_0535");
				string shipText = MapSeaContextGuard.BuildMobilePartyShipPromptText(party);
				if (!string.IsNullOrWhiteSpace(shipText))
				{
					locationLine += ConversationRuntimeTexts.Prompt("AF_CONV_Prompt_0536") + shipText + "。";
				}
				return locationLine;
			}
			string terrainLabel = MapSeaContextGuard.BuildMobilePartyLandTerrainPromptLabel(party);
			if (string.IsNullOrWhiteSpace(terrainLabel))
			{
				terrainLabel = ConversationRuntimeTexts.Prompt("AF_CONV_Prompt_0537");
			}
			Settlement landNearest = FindNearestSettlementForCourierPrompt(party);
			string landNearestName = FormatCourierSettlementNameWithType(landNearest);
			return string.IsNullOrWhiteSpace(landNearestName)
				? ConversationRuntimeTexts.Prompt("AF_CONV_Prompt_0538") + terrainLabel + "。"
				: ConversationRuntimeTexts.Prompt("AF_CONV_Prompt_0539") + landNearestName + ConversationRuntimeTexts.Prompt("AF_CONV_Prompt_0540") + terrainLabel + "。";
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

















	private static string BuildDeliveryFactText(CourierSession session, bool delivered, Hero recipient = null, bool commitPlayerCraftInspection = false)
	{
		if (session == null)
		{
			return "";
		}
		string playerName = MyBehavior.BuildPlayerPublicDisplayNameForExternal(recipient);
		if (string.IsNullOrWhiteSpace(playerName))
		{
			playerName = Hero.MainHero?.Name?.ToString() ?? ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0541");
		}
		StringBuilder sb = new StringBuilder();
		if (delivered)
		{
			sb.Append(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0542")).Append(playerName).Append(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0543"));
		}
		else
		{
			sb.Append(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0544")).Append(playerName).Append(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0545"));
		}
		string crewSummary = BuildCourierCrewSummaryForPrompt(session);
		if (!string.IsNullOrWhiteSpace(crewSummary))
		{
			sb.Append(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0546")).Append(playerName)
				.Append(delivered ? ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0547") : ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0548"))
				.Append(crewSummary)
				.Append(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0549"));
		}
		foreach (CourierCargoEntry entry in session.Entries ?? new List<CourierCargoEntry>())
		{
			if (entry == null || entry.Amount <= 0)
			{
				continue;
			}
			string verb = delivered ? ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0550") : ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0551");
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
				sb.Append(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0552")).Append(playerName).Append(verb).Append(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0553")).Append(entry.Amount).Append(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0554")).Append(delivered ? BuildCourierCargoValueSuffix(entry) : "").Append("。");
			}
			else if (entry.Kind == "item")
			{
				sb.Append(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0555")).Append(playerName).Append(verb).Append(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0556")).Append(entry.Amount).Append(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0557")).Append(factEntryName).Append(delivered ? BuildCourierCargoValueSuffix(entry) : "").Append("。");
			}
			else if (entry.Kind == "show_gold")
			{
				sb.Append(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0558")).Append(playerName).Append(verb).Append(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0559")).Append(entry.Amount).Append(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0560"));
			}
			else if (entry.Kind == "show_item")
			{
				sb.Append(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0561")).Append(playerName).Append(verb).Append(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0562")).Append(entry.Amount).Append(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0563")).Append(factEntryName).Append(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0564"));
			}
			else if (entry.Kind == "troop")
			{
				sb.Append(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0565")).Append(playerName).Append(verb).Append(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0566")).Append(entry.Amount).Append(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0567")).Append(entry.Name).Append(delivered ? BuildCourierCargoValueSuffix(entry) : "").Append("。");
			}
			else if (entry.Kind == "prisoner")
			{
				sb.Append(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0568")).Append(playerName).Append(verb).Append(entry.IsHero ? ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0569") : ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0570")).Append(entry.IsHero ? entry.Name : (entry.Amount + ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0571") + entry.Name)).Append(delivered ? BuildCourierCargoValueSuffix(entry) : "").Append("。");
			}
			else if (entry.Kind == "settlement")
			{
				sb.Append(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0572")).Append(playerName).Append(verb).Append(ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0573")).Append(entry.Name).Append(delivered ? BuildCourierCargoValueSuffix(entry) : "").Append("。");
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
			playerName = Hero.MainHero?.Name?.ToString() ?? ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0574");
		}
		string letterKind = GetInboundLetterKindDisplayText(session);
		string fact = delivered
			? ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0575") + senderName + ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0576") + playerName + ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0577") + letterKind + "。"
			: ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0578") + senderName + ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0579") + letterKind + ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0580") + playerName + "。";
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
				fact += ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0581") + senderName + ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0582") + playerName + ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0583");
			}
		}
		catch
		{
		}
		string crewSummary = BuildCourierCrewSummaryForPrompt(session);
		if (!string.IsNullOrWhiteSpace(crewSummary))
		{
			fact += ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0584") + senderName + ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0585") + crewSummary + ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0586") + playerName + ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0587");
		}
		return fact;
	}

	private static string GetInboundLetterKindDisplayText(CourierSession session)
	{
		string kind = (session?.InboundLetterKind ?? "").Trim();
		if (string.Equals(kind, InboundLetterKindNeedRequest, StringComparison.OrdinalIgnoreCase)) return ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0588");
		if (string.Equals(kind, InboundLetterKindDiplomacy, StringComparison.OrdinalIgnoreCase)) return ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0589");
		if (string.Equals(kind, InboundLetterKindPersonal, StringComparison.OrdinalIgnoreCase)) return ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0590");
		return ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0591");
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
			string role = entry.IsHero ? ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0592") : ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0593");
			parts.Add(name + "×" + Math.Max(1, entry.Amount) + "（" + role + "）");
		}
		return parts.Count == 0 ? "" : string.Join("，", parts);
	}

	private static string BuildCourierCargoValueSuffix(CourierCargoEntry entry)
	{
		long value = EstimateCourierCargoEntryTotalValue(entry);
		return value > 0L ? (ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0594") + value + ConversationRuntimeTexts.Raw("AF_CONV_Runtime_0595")) : "";
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
		sb.AppendLine(ConversationUiLocalization.Text("AF_CONV_UI_Runtime_0596", "Courier party members:"));
		sb.AppendLine("  " + RosterSummary(flow.CrewRoster));
		if (flow.Mode == CourierPayloadMode.Normal || flow.SelectedEntries.Count == 0)
		{
			sb.AppendLine(ConversationUiLocalization.Text("AF_CONV_UI_Runtime_0597", "Enclosed: letter only."));
			return sb.ToString().Trim();
		}
		sb.AppendLine(ConversationUiLocalization.Text("AF_CONV_UI_Runtime_0598", "Enclosed:"));
		foreach (CourierCargoEntry entry in flow.SelectedEntries)
		{
			if (entry == null)
			{
				continue;
			}
			sb.Append("  · ");
			if (entry.Kind == "gold")
			{
				sb.Append(ConversationUiLocalization.Text("AF_CONV_UI_Runtime_0599", "Send ")).Append(entry.Amount).Append(ConversationUiLocalization.Text("AF_CONV_UI_Runtime_0600", " denars"));
			}
			else if (entry.Kind == "show_gold")
			{
				sb.Append(ConversationUiLocalization.Text("AF_CONV_UI_Runtime_0601", "Show ")).Append(entry.Amount).Append(ConversationUiLocalization.Text("AF_CONV_UI_Runtime_0600", " denars"));
			}
			else if (entry.Kind == "item")
			{
				sb.Append(ConversationUiLocalization.Text("AF_CONV_UI_Runtime_0599", "Send ")).Append(entry.Amount).Append(ConversationUiLocalization.Text("AF_CONV_UI_Runtime_0604", " × ")).Append(entry.Name);
			}
			else if (entry.Kind == "show_item")
			{
				sb.Append(ConversationUiLocalization.Text("AF_CONV_UI_Runtime_0601", "Show ")).Append(entry.Amount).Append(ConversationUiLocalization.Text("AF_CONV_UI_Runtime_0604", " × ")).Append(entry.Name);
			}
			else if (entry.Kind == "troop")
			{
				sb.Append(ConversationUiLocalization.Text("AF_CONV_UI_Runtime_0607", "Transfer ")).Append(entry.Amount).Append(ConversationUiLocalization.Text("AF_CONV_UI_Runtime_0608", " × ")).Append(entry.Name);
			}
			else if (entry.Kind == "prisoner")
			{
				sb.Append(entry.IsHero ? (ConversationUiLocalization.Text("AF_CONV_UI_Runtime_0609", "Transfer prisoner ") + entry.Name) : (ConversationUiLocalization.Text("AF_CONV_UI_Runtime_0607", "Transfer ") + entry.Amount + ConversationUiLocalization.Text("AF_CONV_UI_Runtime_0608", " × ") + entry.Name + ConversationUiLocalization.Text("AF_CONV_UI_Runtime_0612", " prisoners")));
			}
			else if (entry.Kind == "settlement")
			{
				sb.Append(ConversationUiLocalization.Text("AF_CONV_UI_Runtime_0613", "Transfer fixed asset ")).Append(entry.Name);
			}
			sb.AppendLine();
		}
		return sb.ToString().Trim();
	}
}
