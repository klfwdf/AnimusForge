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

using static AnimusForge.SceneMovementController;

namespace AnimusForge;
public partial class ShoutBehavior {
private void RecordExtraFactToSceneHistoryCaptured(string extraFact, List<NpcDataPacket> nearbyData)
	{
		if (string.IsNullOrWhiteSpace(extraFact) || nearbyData == null)
		{
			return;
		}
		string text = NormalizeSceneExtraFactForHistory(extraFact);
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		List<int> visibleAgentIndices = BuildVisibleAgentSnapshot(nearbyData);
		long eventSequence = NextConversationEventSequence();
		lock (_historyLock)
		{
			SceneHistoryOwner.AppendPublic(StampConversationMessageWithCurrentMemoryContext(new ConversationMessage
			{
				EventSequence = eventSequence,
				Role = "system",
				Content = text,
				SpeakerName = "系统",
				SpeakerAgentIndex = -1,
				VisibleAgentIndices = visibleAgentIndices
			}));
			foreach (NpcDataPacket nearbyDatum in nearbyData)
			{
				int agentIndex = nearbyDatum.AgentIndex;
				SceneHistoryOwner.AppendNpc(agentIndex, StampConversationMessageWithCurrentMemoryContext(new ConversationMessage
				{
					EventSequence = eventSequence,
					Role = "system",
					Content = text,
					SpeakerName = "系统",
					SpeakerAgentIndex = -1,
					VisibleAgentIndices = visibleAgentIndices
				}));
			}
		}
		AppendSceneEventToNativeSharedHistoryForTargets(nearbyData, "系统", text, "fact", eventSequence);
	}
private void AppendNativeConversationSessionLineToSceneHistoryCaptured(Hero targetHero, CharacterObject targetCharacter, string npcName, string speaker, string text, string kind, long eventSequence = 0L, int targetAgentIndexOverride = -1, NpcDataPacket targetNpc = null, int playerTargetAgentIndexOverride = -1, string playerTargetNameOverride = null)
	{
		text = (text ?? "").Replace("\r", "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		try
		{
			int targetAgentIndex = targetAgentIndexOverride >= 0 ? targetAgentIndexOverride : TryResolveNativeConversationAgentIndex(targetHero, targetCharacter);
			if (targetAgentIndex < 0)
			{
				return;
			}
			string targetName = (npcName ?? "").Trim();
			try
			{
				NpcDataPacket npc = targetNpc;
				if (npc == null)
				{
					Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == targetAgentIndex && a.IsActive());
					npc = ShoutUtils.ExtractNpcData(agent);
				}
				string sceneName = (npc != null) ? GetSceneNpcHistoryNameForPrompt(npc) : "";
				if (!string.IsNullOrWhiteSpace(sceneName))
				{
					targetName = sceneName;
				}
			}
			catch
			{
			}
			if (string.IsNullOrWhiteSpace(targetName))
			{
				targetName = (targetHero?.Name?.ToString() ?? targetCharacter?.Name?.ToString() ?? "NPC").Trim();
			}
			string normalizedKind = (kind ?? "").Trim().ToLowerInvariant();
			int actualPlayerTargetAgentIndex = playerTargetAgentIndexOverride >= 0 ? playerTargetAgentIndexOverride : targetAgentIndex;
			string actualPlayerTargetName = (playerTargetNameOverride ?? "").Trim();
			if (string.IsNullOrWhiteSpace(actualPlayerTargetName))
			{
				actualPlayerTargetName = targetName;
			}
            float distance = normalizedKind == "npc" || normalizedKind == "fact" ? -1f : GetPlayerDistanceToAgentForScenePrompt(actualPlayerTargetAgentIndex);
            ConversationMessage message = StampConversationMessageWithCurrentMemoryContext(SceneHistoryProjectionOwner.BuildNativeSceneBridgeMessage(
                text, normalizedKind, speaker, targetName, targetAgentIndex, eventSequence, actualPlayerTargetAgentIndex, actualPlayerTargetName, distance,
                new ConversationSpeechTextOptions(IsDetailedSceneSpeechPromptEnabled(), ShouldPreserveSceneAsteriskActions())));
			if (string.IsNullOrWhiteSpace(message.Content))
			{
				return;
			}
			lock (_historyLock)
			{
				SceneHistoryOwner.AppendPublic(message);
				SceneHistoryOwner.AppendNpc(targetAgentIndex, message);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("NativeConversation", "[WARN] scene-history bridge failed: " + ex.Message);
		}
	}
private void RemoveNativeConversationSessionHistoryEventFromSceneHistoryCaptured(long eventSequence)
	{
		if (eventSequence <= 0L)
		{
			return;
		}
		try
		{
			lock (_historyLock)
			{
				SceneHistoryOwner.RollbackPlayerEvent(eventSequence);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("NativeConversation", "[WARN] scene-history rollback failed: " + ex.Message);
		}
	}
private void AppendActionAfefFactToSceneHistoryInOrderCaptured(int targetAgentIndex, string fact, bool mirrorToNativeSharedHistory = true)
	{
		if (targetAgentIndex < 0)
		{
			return;
		}
		string factLine = NormalizeNativeConversationFactLineForPrompt(fact, "玩家动作");
		if (string.IsNullOrWhiteSpace(factLine))
		{
			return;
		}
		long eventSequence = NextConversationEventSequence();
		ConversationMessage publicMessage = StampConversationMessageWithCurrentMemoryContext(new ConversationMessage
		{
			EventSequence = eventSequence,
			Role = "system",
			Content = factLine,
			SpeakerName = "系统",
			SpeakerAgentIndex = -1,
			VisibleAgentIndices = new List<int> { targetAgentIndex }
		});
		ConversationMessage privateMessage = StampConversationMessageWithCurrentMemoryContext(new ConversationMessage
		{
			EventSequence = eventSequence,
			Role = "system",
			Content = factLine,
			SpeakerName = "系统",
			SpeakerAgentIndex = -1,
			VisibleAgentIndices = new List<int> { targetAgentIndex }
		});
		lock (_historyLock)
		{
			SceneHistoryOwner.AppendPair(targetAgentIndex, publicMessage, privateMessage);
		}
		if (mirrorToNativeSharedHistory)
		{
			AppendSceneEventToNativeSharedHistory(ResolveSceneNpcDataForSharedHistory(targetAgentIndex), "玩家动作", factLine, "fact", eventSequence);
		}
	}
private bool RecordPlayerMessageCaptured(string text, List<NpcDataPacket> nearbyData, int primaryTargetAgentIndex = -1, string primaryTargetName = "", Dictionary<int, Agent> audienceAgentsByIndex = null, bool requireMemoryReceipt = false)
	{
		// Must happen first so the dynamic AFEF fact sits directly above the player's current scene utterance.
		TryInjectSceneFirstMeetingFactsBeforePlayerMessage(nearbyData);
		TryInjectSceneRevisitFactsBeforePlayerMessage(nearbyData);
		List<int> visibleAgentIndices = BuildVisibleAgentSnapshot(nearbyData);
		string text2 = ResolveSceneTargetNameForPrompt(primaryTargetAgentIndex, primaryTargetName, nearbyData);
		float playerDistanceMeters = GetPlayerDistanceToAgentForScenePrompt(primaryTargetAgentIndex);
		long eventSequence = NextConversationEventSequence();
		lock (_historyLock)
		{
			SceneHistoryOwner.AppendPublic(StampConversationMessageWithCurrentMemoryContext(new ConversationMessage
			{
				EventSequence = eventSequence,
				Role = "user",
				Content = text,
				SpeakerName = "你",
				SpeakerAgentIndex = -1,
				TargetAgentIndex = primaryTargetAgentIndex,
				TargetName = text2,
				PlayerDistanceMeters = playerDistanceMeters,
				VisibleAgentIndices = visibleAgentIndices
			}));
			foreach (NpcDataPacket nearbyDatum in nearbyData)
			{
				int agentIndex = nearbyDatum.AgentIndex;
				SceneHistoryOwner.AppendNpc(agentIndex, StampConversationMessageWithCurrentMemoryContext(new ConversationMessage
				{
					EventSequence = eventSequence,
					Role = "user",
					Content = text,
					SpeakerName = "你",
					SpeakerAgentIndex = -1,
					TargetAgentIndex = primaryTargetAgentIndex,
					TargetName = text2,
					PlayerDistanceMeters = playerDistanceMeters,
					VisibleAgentIndices = visibleAgentIndices
				}));
			}
		}
		AppendSceneEventToNativeSharedHistoryForTargets(nearbyData, GetPlayerDisplayNameForShout(), text, "player", eventSequence, primaryTargetAgentIndex, text2, audienceAgentsByIndex);
		try
		{
			return PersistPlayerMessageToNamedHeroes(text, nearbyData, primaryTargetAgentIndex, text2, audienceAgentsByIndex, requireMemoryReceipt);
		}
		catch
		{
			return false;
		}
	}
private void PromotePersonalizedExtraFactInScenePrivateHistoryCaptured(
		string personalizedExtraFact,
		int personalizedAgentIndex)
	{
		if (personalizedAgentIndex < 0
			|| !ContainsPlayerCraftedAfefInspectionSuffix(personalizedExtraFact))
		{
			return;
		}
		string personalizedLine = NormalizeSceneExtraFactForHistory(personalizedExtraFact);
		string sharedLine = NormalizeSceneExtraFactForHistory(
			StripPlayerCraftedAfefInspectionSuffix(personalizedExtraFact));
		if (string.IsNullOrWhiteSpace(personalizedLine)
			|| string.IsNullOrWhiteSpace(sharedLine)
			|| string.Equals(personalizedLine, sharedLine, StringComparison.Ordinal))
		{
			return;
		}
		lock (_historyLock)
		{
            if (!SceneHistoryOwner.CapturePrivateFactForPromotion(personalizedAgentIndex, sharedLine, out int index, out ConversationMessage message)) return;
            message.Content = personalizedLine;
            message.TargetAgentIndex = personalizedAgentIndex;
            message.TargetHeroId = "";
            message.VisibleAgentIndices = new List<int> { personalizedAgentIndex };
            message.VisibleHeroIds = new List<string>();
            FillSceneMessageHeroIdentity(message);
            SceneHistoryOwner.ApplyPrivateFactPromotion(personalizedAgentIndex, index, sharedLine, message);
		}
	}
private bool RecordResponseForAllNearbySafeCaptured(List<NpcDataPacket> nearbyData, int speakerAgentIndex, string speakerName, string response, bool requireMemoryReceipt = false)
	{
		List<int> visibleAgentIndices = BuildVisibleAgentSnapshot(nearbyData);
		string text = ResolveSceneHistorySpeakerNameForPrompt(speakerAgentIndex, speakerName, nearbyData);
		long eventSequence = NextConversationEventSequence();
		lock (_historyLock)
		{
			foreach (NpcDataPacket nearbyDatum in nearbyData)
			{
				int agentIndex = nearbyDatum.AgentIndex;
				SceneHistoryOwner.AppendNpc(agentIndex, StampConversationMessageWithCurrentMemoryContext(new ConversationMessage
				{
					EventSequence = eventSequence,
					Role = "assistant",
					Content = "[" + text + "]: " + response,
					SpeakerName = text,
					SpeakerAgentIndex = speakerAgentIndex,
					VisibleAgentIndices = visibleAgentIndices
				}));
				SceneHistoryOwner.PruneNpcFacts(agentIndex);
			}
			SceneHistoryOwner.AppendPublic(StampConversationMessageWithCurrentMemoryContext(new ConversationMessage
			{
				EventSequence = eventSequence,
				Role = "assistant",
				Content = response,
				SpeakerName = text,
				SpeakerAgentIndex = speakerAgentIndex,
				VisibleAgentIndices = visibleAgentIndices
			}));
			SceneHistoryOwner.PrunePublicFacts();
		}
		NpcDataPacket speakerNpc = nearbyData?.FirstOrDefault((NpcDataPacket x) => x != null && x.AgentIndex == speakerAgentIndex) ?? ResolveSceneNpcDataForSharedHistory(speakerAgentIndex, speakerName);
		AfGcczShoutBridge.RecordOrdinarySpeakerUtterance(speakerNpc, response);
		AppendSceneEventToNativeSharedHistory(speakerNpc, text, response, "npc", eventSequence);
		return FlushPendingHeroHistoryExtraFactAfterSceneReply(requireMemoryReceipt);
	}
private void RecordSystemFactForNearbySafeCaptured(List<NpcDataPacket> nearbyData, string factText)
	{
		string text = (factText ?? "").Replace("\r", "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		bool isAfefFact = TryNormalizeAfefFactLineForPrompt(text, out var afefFactLine);
		List<int> visibleAgentIndices = BuildVisibleAgentSnapshot(nearbyData);
		List<NpcDataPacket> afefPendingTargets = isAfefFact ? new List<NpcDataPacket>() : null;
		long eventSequence = NextConversationEventSequence();
		lock (_historyLock)
		{
			SceneHistoryOwner.AppendPublic(StampConversationMessageWithCurrentMemoryContext(new ConversationMessage
			{
				EventSequence = eventSequence,
				Role = "system",
				Content = text,
				SpeakerName = "system",
				SpeakerAgentIndex = -1,
				VisibleAgentIndices = visibleAgentIndices
			}));
			if (nearbyData == null)
			{
				return;
			}
			foreach (NpcDataPacket nearbyDatum in nearbyData)
			{
				if (nearbyDatum == null)
				{
					continue;
				}
				int agentIndex = nearbyDatum.AgentIndex;
				SceneHistoryOwner.AppendNpc(agentIndex, StampConversationMessageWithCurrentMemoryContext(new ConversationMessage
				{
					EventSequence = eventSequence,
					Role = "system",
					Content = text,
					SpeakerName = "system",
					SpeakerAgentIndex = -1,
					VisibleAgentIndices = visibleAgentIndices
				}));
				if (isAfefFact)
				{
					afefPendingTargets.Add(nearbyDatum);
				}
			}
		}
		if (afefPendingTargets != null)
		{
			foreach (NpcDataPacket target in afefPendingTargets)
			{
				if (target == null)
				{
					continue;
				}
				QueuePendingCurrentAfefFactForAgent(target.AgentIndex, afefFactLine);
				QueuePendingCurrentNativeAfefFactForSceneTarget(target, afefFactLine);
			}
		}
		AppendSceneEventToNativeSharedHistoryForTargets(nearbyData, "系统", text, "fact", eventSequence);
		try
		{
			if (nearbyData != null)
			{
				HashSet<string> persistedNonHeroIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				foreach (NpcDataPacket nearbyDatum in nearbyData)
				{
					if (nearbyDatum != null && !nearbyDatum.IsHero && TryResolveWildernessNonHeroMemory(nearbyDatum, null, null, nearbyDatum.AgentIndex, out var memoryId, out var memoryName) && persistedNonHeroIds.Add(memoryId))
					{
						MyBehavior.AppendExternalNonHeroSceneDialogueHistory(memoryId, memoryName, null, null, text, _sceneHistorySessionId);
					}
				}
			}
		}
		catch
		{
		}
	}
}
