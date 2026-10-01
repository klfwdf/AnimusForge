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


using static AnimusForge.SceneMovementController;

public partial class ShoutBehavior
{
    private SceneMovementController _sceneMovementController;
    private SceneMovementController _sceneMovement => _sceneMovementController ?? (_sceneMovementController = new SceneMovementController(CreateSceneMovementPorts()));
    private SceneMovementPorts CreateSceneMovementPorts() => new SceneMovementPorts
    {
        AppendTargetedSceneNpcFact = AppendTargetedSceneNpcFact,
        BuildSceneNpcDataFromLocationCharacter = BuildSceneNpcDataFromLocationCharacter,
        ClearAgentSceneConversationFocus = ClearAgentSceneConversationFocus,
        ClearPendingSceneConversationAttentionRelease = ClearPendingSceneConversationAttentionRelease,
        ForceAgentFacePlayer = ForceAgentFacePlayer,
        CaptureCompactSceneReactionInputAsync = CaptureCompactSceneReactionInputAsync,
        ReleaseAgentFromSceneConversationLocks = ReleaseAgentFromSceneConversationLocks,
        ReleaseSceneConversationAttention = ReleaseSceneConversationAttention,
        RemoveSceneMovementSuppressionAgents = RemoveSceneMovementSuppressionAgents,
        ResolveHeroFromAgentIndex = ResolveHeroFromAgentIndex,
        RestoreAgentAutonomy = RestoreAgentAutonomy,
        TrackPlayerInteraction = (npc, count, timeout) => TrackPlayerInteraction(npc, count, timeout),
        TryInterruptAgentSceneUseForStare = TryInterruptAgentSceneUseForStare,
        TriggerImmediateSceneBehaviorReaction = (fact, index, persist, suppress, leave, skip, returnSummon) => TriggerImmediateSceneBehaviorReaction(fact, index, persist, suppress, leave, skip, returnSummon),
        EnqueueSpeechLine = (npc, text, participants, skip, suppress) => EnqueueSpeechLine(npc, text, participants, skip, suppress),
        CancelInteractionTimeoutArm = index => _pendingInteractionTimeoutArms.Remove(index),
        RemoveInteractionSession = index => _activeInteractionSessions.Remove(index),
        CancelAutonomyRestore = index => _pendingSceneAutonomyRestoresAfterSpeech.Remove(index),
        DetachStareAgent = index => { _staringAgents.RemoveAll(a => a == null || a.Index == index); _staringAgentAnchors.Remove(index); },
        ReleaseUseConversationSlot = index => _staringUseConversationAgents.Remove(index),
        ClearEmptyStareDeadline = () => { if (_staringAgents.Count == 0) _stopStaringTime = 0f; },
        RemoveAttentionRelease = index => { lock (_pendingSceneConversationAttentionReleaseLock) _pendingSceneConversationAttentionReleaseAgentIndices.Remove(index); },
    };
}
