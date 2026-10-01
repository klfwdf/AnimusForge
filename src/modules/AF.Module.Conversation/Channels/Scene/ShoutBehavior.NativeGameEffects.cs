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

public partial class ShoutBehavior
{
    private NativeConversationGameEffectsRuntime _nativeGameEffectsRuntime;
    private NativeConversationGameEffectsRuntime _nativeGameEffects => _nativeGameEffectsRuntime ?? (_nativeGameEffectsRuntime = new NativeConversationGameEffectsRuntime(CreateNativeGameEffectPorts(), _pendingMainThreadFunctions));
    private NativeConversationGameEffectPorts CreateNativeGameEffectPorts() => new NativeConversationGameEffectPorts
    {
        TryTriggerNativeConversationOpenLordsHallAction = TryTriggerNativeConversationOpenLordsHallAction,
        TryProcessSetsOwnedSettlementMassacreActionTags = TryProcessSetsOwnedSettlementMassacreActionTags,
        QueueNativeConversationNpcSurrender = QueueNativeConversationNpcSurrender,
        RecordGeneratedNpcAfefFactsForNativeConversation = RecordGeneratedNpcAfefFactsForNativeConversation,
        TryQueueNativeSceneMechanismActionAfterConversationExit = TryQueueNativeSceneMechanismActionAfterConversationExit,
        TrySpeakNativeConversationReplyWithTts = TrySpeakNativeConversationReplyWithTts,
        IsNativeConversationAdmissionCurrent = IsNativeConversationAdmissionCurrent,
        IsNativeConversationContextCurrent = IsNativeConversationContextCurrent,
        IsPresentationCurrent = revision => _nativeAdmissionOwner.IsPresentationCurrent(revision),
        PostMainThread = action => _mainThreadActions.Enqueue(action),
        RollbackPendingPlayerHistory = (admission, key, sequence, reason) => RollbackNativeConversationPendingPlayerHistory(this, admission, key, sequence, reason),
    };
}
