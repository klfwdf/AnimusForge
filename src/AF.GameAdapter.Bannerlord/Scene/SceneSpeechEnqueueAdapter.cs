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

using static AnimusForge.ShoutBehavior;
namespace AnimusForge;

internal sealed class SceneSpeechEnqueueAdapter
{
    private readonly SceneSpeechEnqueueAdapterPorts _ports;
    internal SceneSpeechEnqueueAdapter(SceneSpeechEnqueueAdapterPorts ports) { _ports = ports ?? throw new ArgumentNullException(nameof(ports)); }

	internal void EnqueueSpeechLineWithOptions(NpcDataPacket npc, string content, List<NpcDataPacket> allNpcData, bool commitHistory, bool suppressStare, bool allowPlayerDirectedActions, int requiredConversationEpoch, List<SceneSummonPromptTarget> sceneSummonTargets = null, List<SceneGuidePromptTarget> sceneGuideTargets = null, string afterSpeechInfoMessage = null, TaskCompletionSource<bool> completionSource = null, float interactionTimeoutSeconds = -1f, int interactionParticipantCount = 1, Func<bool> canStillPublish = null, string playerDirectedActionText = null, string playerDirectedNpcReplyText = null)
	{
		if (npc == null || string.IsNullOrWhiteSpace(content))
		{
			completionSource?.TrySetResult(false);
			return;
		}
		Agent agent = Mission.Current?.Agents?.FirstOrDefault((Agent a) => a != null && a.Index == npc.AgentIndex);
		if (!CanAgentParticipateInSceneSpeech(agent))
		{
			completionSource?.TrySetResult(false);
			return;
		}
		NpcDataPacket value = CloneNpcDataPacket(npc);
		List<NpcDataPacket> value2 = CloneNpcDataSnapshot(allNpcData);
		ApplySceneLocalDisambiguatedNames(value2);
		NpcDataPacket npcDataPacket = value2.FirstOrDefault((NpcDataPacket x) => x != null && x.AgentIndex == value.AgentIndex);
		if (npcDataPacket != null)
		{
			value.Name = npcDataPacket.Name;
			value.PromptGivenName = npcDataPacket.PromptGivenName;
			value.PromptDisplayName = npcDataPacket.PromptDisplayName;
		}
		SceneSpeechQueueItem queuedItem = new SceneSpeechQueueItem
		{
			SourceMission = Mission.Current,
            RuntimeGeneration = SaveRuntimeGuard.CaptureGeneration(),
            Npc = value,
			Content = content,
            DiplomacyOrigin = DiplomacyDialogueSourceScope.Capture(content, "scene", SceneConversationHistoryOwner.SessionId.ToString(),
                playerDirectedActionText, playerDirectedNpcReplyText ?? StripActionTagsForSceneSpeech(content)),
			ContextSnapshot = value2,
			SceneSummonTargets = SceneMovementController.CloneSceneSummonPromptTargets(sceneSummonTargets),
			SceneGuideTargets = SceneMovementController.CloneSceneGuidePromptTargets(sceneGuideTargets),
			CommitHistory = commitHistory,
			SuppressStare = suppressStare,
			AllowPlayerDirectedActions = allowPlayerDirectedActions,
			PlayerDirectedActionText = playerDirectedActionText,
			PlayerDirectedNpcReplyText = playerDirectedNpcReplyText,
			RequiredConversationEpoch = requiredConversationEpoch,
			AfterSpeechInfoMessage = afterSpeechInfoMessage,
			CompletionSource = completionSource,
			InteractionTimeoutSeconds = interactionTimeoutSeconds,
			InteractionParticipantCount = Math.Max(1, interactionParticipantCount),
			CanStillPublish = canStillPublish
		};
		_sceneSpeechQueueOwner.Enqueue(queuedItem);
	}

    private SceneSpeechExecutionRuntime<SceneSpeechQueueItem> _sceneSpeechQueueOwner { get => _ports.Get_sceneSpeechQueueOwner(); }
}

internal sealed class SceneSpeechEnqueueAdapterPorts
{
    internal Func<SceneSpeechExecutionRuntime<SceneSpeechQueueItem>> Get_sceneSpeechQueueOwner;
}
