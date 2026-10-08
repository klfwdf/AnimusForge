using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.SceneActions.Core;
using AnimusForge.SiegeAftermathIntervention;
using AnimusForge.XihaiAction;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Modules;
using AnimusForge.Refactor.Runtime;
using RichExecutions.Core;
using RichExecutions.Scene;
using SandBox;
using SandBox.Missions.AgentBehaviors;
using SandBox.Missions.MissionLogics;
using SandBox.Missions.MissionLogics.Towns;
using SandBox.Objects.AnimationPoints;
using SandBox.Objects.Usables;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions;

namespace AnimusForge;


public partial class ShoutBehavior
{
    internal List<string> CaptureVisibleSceneHistoryLinesForPrompt(int viewerAgentIndex, string viewerName, bool useDistanceLabels)
    {
        return SceneHistoryPromptCapture.CaptureVisibleSceneHistoryLinesForPrompt(viewerAgentIndex, viewerName, useDistanceLabels);
    }
	private static List<string> KeepAfefFactsAndRecentHistoryLines(List<string> lines, int maxConversationLines)
	{

        int limit = Math.Max(DuelSettings.DailyConversationHistoryLineLimitMin, Math.Min(DuelSettings.DailyConversationHistoryLineLimitMax, maxConversationLines));
        return SceneHistoryMessageAssemblyOwner.KeepAfefFactsAndRecentHistoryLines(lines, limit);
	}

	private List<object> BuildStrictSceneMessagesForNpc(int npcAgentIndex, string systemPrompt, IEnumerable<string> prefixUserSections, IEnumerable<string> suffixUserSections = null, bool currentInputAlreadyRecorded = true, string currentPlayerInput = null, int maxHistoryMessages = 0, bool suppressReplyFormatInstruction = false, IEnumerable<ConversationMessage> injectedHistoryMessages = null, bool includeSceneHistory = true, IEnumerable<ConversationMessage> persistentHistoryMessages = null, IEnumerable<ConversationMessage> pendingCurrentAfefFactMessages = null, bool useSceneDistanceSpeechLabels = true)
	{ return SceneHistoryMessageAssemblyOwner.Build(CaptureStrictSceneMessageInputForNpc(npcAgentIndex, systemPrompt, prefixUserSections, suffixUserSections, currentInputAlreadyRecorded, currentPlayerInput, maxHistoryMessages, suppressReplyFormatInstruction, injectedHistoryMessages, includeSceneHistory, persistentHistoryMessages, pendingCurrentAfefFactMessages, useSceneDistanceSpeechLabels)); }

	internal SceneHistoryMessageAssemblyInput CaptureStrictSceneMessageInputForNpc(int npcAgentIndex, string systemPrompt, IEnumerable<string> prefixUserSections, IEnumerable<string> suffixUserSections = null, bool currentInputAlreadyRecorded = true, string currentPlayerInput = null, int maxHistoryMessages = 0, bool suppressReplyFormatInstruction = false, IEnumerable<ConversationMessage> injectedHistoryMessages = null, bool includeSceneHistory = true, IEnumerable<ConversationMessage> persistentHistoryMessages = null, IEnumerable<ConversationMessage> pendingCurrentAfefFactMessages = null, bool useSceneDistanceSpeechLabels = true)
	{
        return SceneHistoryPromptCapture.CaptureStrictSceneMessageInputForNpc(npcAgentIndex, systemPrompt, prefixUserSections, suffixUserSections, currentInputAlreadyRecorded, currentPlayerInput, maxHistoryMessages, suppressReplyFormatInstruction, injectedHistoryMessages, includeSceneHistory, persistentHistoryMessages, pendingCurrentAfefFactMessages, useSceneDistanceSpeechLabels);
    }
	private static string BuildConversationMessageDedupeKey(ConversationMessage msg)
	{
		return SceneHistoryMessageAssemblyOwner.BuildConversationMessageDedupeKey(msg, new ConversationSpeechTextOptions(IsDetailedSceneSpeechPromptEnabled(), ShouldPreserveSceneAsteriskActions()));
	}

	private static void AppendConversationMessages(List<ConversationMessage> target, IEnumerable<ConversationMessage> source)
	{
		SceneHistoryMessageAssemblyOwner.AppendConversationMessages(target, source);
	}

	private static List<ConversationMessage> SortConversationMessagesByEventSequence(List<ConversationMessage> messages)
	{
		return SceneHistoryMessageAssemblyOwner.SortConversationMessagesByEventSequence(messages);
	}

	private static bool IsAfefConversationMessage(ConversationMessage msg)
	{
		return SceneHistoryMessageAssemblyOwner.IsAfefConversationMessage(msg);
	}

	private static List<ConversationMessage> KeepAfefFactsAndRecentConversationMessages(List<ConversationMessage> messages, int maxConversationMessages)
	{
		return SceneHistoryMessageAssemblyOwner.KeepAfefFactsAndRecentConversationMessages(messages, maxConversationMessages);
	}

	private static bool TryConvertSceneMessageToStrictChatMessage(ConversationMessage msg, int npcAgentIndex, out object chatMessage)
	{
		return TryConvertSceneMessageToStrictChatMessage(msg, npcAgentIndex, out chatMessage, null);
	}

	private static bool TryConvertSceneMessageToStrictChatMessage(ConversationMessage msg, int npcAgentIndex, out object chatMessage, HashSet<string> currentAfefFactKeys, bool useSceneDistanceSpeechLabels = true)
	{
		return SceneHistoryMessageAssemblyOwner.TryConvertSceneMessageToStrictChatMessage(msg, CaptureSceneHistoryMessageContext(npcAgentIndex, useSceneDistanceSpeechLabels), out chatMessage, currentAfefFactKeys);
	}

	private static string NormalizeSceneHistoryPromptLineContent(string content)
	{
		return SceneHistoryMessageAssemblyOwner.NormalizeSceneHistoryPromptLineContent(content);
	}

	private static string NormalizeStrictSceneAssistantContent(string content, string speakerName)
	{
		return SceneHistoryMessageAssemblyOwner.NormalizeStrictSceneAssistantContent(content, speakerName);
	}

	private static string FormatScenePlayerDirectSpeechLabel(string playerName, float playerDistanceMeters)
	{
		return SceneHistoryMessageAssemblyOwner.FormatScenePlayerDirectSpeechLabel(playerName, playerDistanceMeters);
	}

	private static bool TryNormalizeAfefFactLineForPrompt(string text, out string factLine)
	{
		return SceneHistoryMessageAssemblyOwner.TryNormalizeAfefFactLineForPrompt(text, out factLine);
	}

	private static string NormalizeAfefFactLineForPrompt(string text, string fallbackPrefix = "[AFEF玩家行为补充]")
	{
		return SceneHistoryMessageAssemblyOwner.NormalizeAfefFactLineForPrompt(text, fallbackPrefix);
	}

	private static string BuildScopedAfefFactLineForPrompt(string text, bool isCurrent)
	{
		return SceneHistoryMessageAssemblyOwner.BuildScopedAfefFactLineForPrompt(text, isCurrent);
	}

	private static string BuildConversationMessageMetadataPrefix(ConversationMessage msg, string fallbackSpeaker)
	{
		return SceneHistoryMessageAssemblyOwner.BuildConversationMessageMetadataPrefix(CaptureSceneHistoryMessageContext(-1, true), msg, fallbackSpeaker);
	}

	private static string PrefixConversationMessageForPrompt(ConversationMessage msg, string fallbackSpeaker, string content)
	{
		return SceneHistoryMessageAssemblyOwner.PrefixConversationMessageForPrompt(CaptureSceneHistoryMessageContext(-1, true), msg, fallbackSpeaker, content);
	}

    private static SceneHistoryMessageContext CaptureSceneHistoryMessageContext(int npcAgentIndex, bool useDistance)
    {
        return SceneHistoryPromptCaptureAdapter.CaptureSceneHistoryMessageContext(npcAgentIndex, useDistance);
    }
}
