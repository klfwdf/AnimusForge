using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.SceneActions.Core;
using HarmonyLib;
using TaleWorlds.MountAndBlade;

namespace AnimusForge.XihaiAction
{
    internal static class AfCompatV130
    {
        internal const string ClassifierProviderId = "animusforge.main.v130";

        private const string HarmonyId = "animusforge.sceneactions.compat.af130";
        private static Harmony _harmony;
        private static MethodInfo _patchedMethod;
        private static MethodInfo _pauseGameMethod;
        private static MethodInfo _resumeGameMethod;
        private static MethodInfo _recordPlayerMessageMethod;
        private static MethodInfo _queuedNpcReplyMethod;
        private static MethodInfo _shownNpcReplyMethod;
        private static MethodInfo _replyPromptMethod;
        private static MethodInfo _strictSceneMessagesSystemPromptMethod;
        private static FieldInfo _shownVisualDurationField;
        private static MethodInfo _classifierApiMethod;
        private static MethodInfo _getAgentsMethod;
        private static MethodInfo _buildTargetingContextMethod;
        private static MethodInfo _extractNpcDataMethod;
        private static MethodInfo _speechPopupShowMethod;
        private static MethodInfo _passiveNpcResponseMethod;
        private static MethodInfo _sceneDescriptionMethod;
        private static FieldInfo _contextField;
        private static FieldInfo _conversationEpochField;
        private static FieldInfo _primaryIndexField;
        private static PropertyInfo _currentInstanceProperty;
        private static Type _npcDataPacketType;
        private static FieldInfo _npcAgentIndexField;
        private static PropertyInfo _npcAgentIndexProperty;
        private static IDisposable _classifierRegistration;
        private static AfV130AuxiliaryTextClassifier _classifierProvider;
        private static readonly object ReplyObservationSync = new object();
        private static readonly List<QueuedReplyObservation> QueuedReplyObservations =
            new List<QueuedReplyObservation>();
        private const double QueuedReplyObservationTtlSeconds = 30d;
        private static bool _installed;
        private static int _replayDepth;
        private static int _suppressRecordedObservationDepth;
        private static int _audienceReplyDepth;
        private static object _behaviorInstance;

        public static bool TryInstall(out string reason)
        {
            reason = null;
            if (_installed)
            {
                reason = "already installed";
                return true;
            }

            try
            {
                List<Assembly> afAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(assembly =>
                        string.Equals(
                            assembly.GetName().Name,
                            "AnimusForge",
                            StringComparison.Ordinal))
                    .ToList();
                if (afAssemblies.Count != 1)
                {
                    reason = "expected exactly one loaded AnimusForge assembly, found " +
                             afAssemblies.Count;
                    return false;
                }
                Assembly afAssembly = afAssemblies[0];
                Type behaviorType = afAssembly.GetType("AnimusForge.ShoutBehavior", false, false);
                if (behaviorType == null)
                {
                    reason = "AnimusForge.ShoutBehavior is missing";
                    return false;
                }
                const BindingFlags instanceFlags =
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                _npcDataPacketType = afAssembly.GetType(
                    "AnimusForge.NpcDataPacket",
                    false,
                    false);
                if (_npcDataPacketType == null)
                {
                    reason = "AnimusForge.NpcDataPacket is missing";
                    return false;
                }
                Type shoutUtilsType = afAssembly.GetType(
                    "AnimusForge.ShoutUtils",
                    false,
                    false);
                _extractNpcDataMethod = shoutUtilsType?.GetMethod(
                    "ExtractNpcData",
                    BindingFlags.Static | BindingFlags.Public,
                    null,
                    new[] { typeof(Agent) },
                    null);
                if (_extractNpcDataMethod == null ||
                    _extractNpcDataMethod.ReturnType != _npcDataPacketType)
                {
                    reason = "AF NPC packet extractor signature drifted";
                    return false;
                }
                _npcAgentIndexField = _npcDataPacketType.GetField(
                    "AgentIndex",
                    instanceFlags);
                _npcAgentIndexProperty = _npcDataPacketType.GetProperty(
                    "AgentIndex",
                    instanceFlags);
                if (_npcAgentIndexField?.FieldType != typeof(int))
                {
                    _npcAgentIndexField = null;
                }
                if (_npcAgentIndexProperty?.PropertyType != typeof(int) ||
                    _npcAgentIndexProperty.GetGetMethod(true) == null)
                {
                    _npcAgentIndexProperty = null;
                }
                if (_npcAgentIndexField == null && _npcAgentIndexProperty == null)
                {
                    reason = "NPC packet AgentIndex contract drifted";
                    return false;
                }
                _contextField = behaviorType.GetField(
                    "_activeShoutTargetingContext",
                    instanceFlags);
                if (_contextField == null)
                {
                    reason = "targeting context field is missing";
                    return false;
                }
                _conversationEpochField = behaviorType.GetField(
                    "_sceneConversationEpoch",
                    instanceFlags);
                if (_conversationEpochField?.FieldType != typeof(int))
                {
                    reason = "conversation epoch field is missing";
                    return false;
                }
                Type contextType = _contextField.FieldType;
                _currentInstanceProperty = behaviorType.GetProperty(
                    "CurrentInstance",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                _buildTargetingContextMethod = behaviorType.GetMethod(
                    "BuildCurrentShoutTargetingContext",
                    instanceFlags,
                    null,
                    Type.EmptyTypes,
                    null);
                _primaryIndexField = contextType.GetField("PrimaryAgentIndex", instanceFlags);
                if (_primaryIndexField?.FieldType != typeof(int))
                {
                    reason = "targeting context signature drifted";
                    return false;
                }

                _getAgentsMethod = behaviorType.GetMethod(
                    "GetAgentsForShoutTargetingContext",
                    instanceFlags,
                    null,
                    new[] { contextType },
                    null);
                if (_getAgentsMethod == null ||
                    _getAgentsMethod.ReturnType != typeof(List<Agent>))
                {
                    reason = "framed-agent resolver signature drifted";
                    return false;
                }

                // The popup is an optional UI capability. The dedicated Y-menu route
                // falls back to TextInquiryData when an older AF build does not
                // expose this stable public helper, so this must not disable the
                // rest of the extension.
                Type speechPopupType = afAssembly.GetType(
                    "AnimusForge.ShoutTextInputPopup",
                    false,
                    false);
                _speechPopupShowMethod = speechPopupType?.GetMethod(
                    "Show",
                    BindingFlags.Static | BindingFlags.Public,
                    null,
                    new[]
                    {
                        typeof(string), typeof(string), typeof(string), typeof(string),
                        typeof(Action<string>), typeof(Action), typeof(Action)
                    },
                    null);

                _passiveNpcResponseMethod = behaviorType.GetMethods(instanceFlags)
                    .Where(method => string.Equals(
                        method.Name,
                        "GetPassiveNpcResponse",
                        StringComparison.Ordinal))
                    .SingleOrDefault(method =>
                    {
                        ParameterInfo[] parameters = method.GetParameters();
                        return parameters.Length == 6 &&
                               parameters[0].ParameterType == _npcDataPacketType &&
                               parameters[1].ParameterType == typeof(string) &&
                               parameters[2].ParameterType == typeof(string) &&
                               parameters[3].ParameterType == typeof(string) &&
                               parameters[4].ParameterType.IsGenericType &&
                               parameters[5].ParameterType == typeof(Dictionary<int, TaleWorlds.CampaignSystem.Hero>) &&
                               typeof(Task<string>).IsAssignableFrom(method.ReturnType);
                    });
                if (_passiveNpcResponseMethod == null)
                {
                    reason = "AF passive NPC response signature drifted";
                    return false;
                }
                Type afShoutUtilsType = afAssembly.GetType(
                    "AnimusForge.ShoutUtils",
                    false,
                    false);
                _sceneDescriptionMethod = afShoutUtilsType?.GetMethod(
                    "GetCurrentSceneDescription",
                    BindingFlags.Static | BindingFlags.Public,
                    null,
                    Type.EmptyTypes,
                    null);

                _patchedMethod = behaviorType.GetMethod(
                    "OnShoutConfirmedWithContext",
                    instanceFlags,
                    null,
                    new[] { typeof(string), typeof(string), typeof(int?) },
                    null);
                if (_patchedMethod == null || _patchedMethod.ReturnType != typeof(void))
                {
                    reason = "player shout submission signature drifted";
                    return false;
                }

                MethodInfo[] recordPlayerCandidates = behaviorType.GetMethods(instanceFlags)
                    .Where(method => string.Equals(
                        method.Name,
                        "RecordPlayerMessage",
                        StringComparison.Ordinal))
                    .ToArray();
                if (recordPlayerCandidates.Length != 1 ||
                    !IsExpectedRecordedPlayerMessageMethod(
                        recordPlayerCandidates[0],
                        _npcDataPacketType))
                {
                    reason = "accepted player-message publication signature drifted";
                    return false;
                }
                _recordPlayerMessageMethod = recordPlayerCandidates[0];

                MethodInfo[] enqueueCandidates = behaviorType.GetMethods(instanceFlags)
                    .Where(method => string.Equals(
                        method.Name,
                        "EnqueueSpeechLineWithOptions",
                        StringComparison.Ordinal))
                    .ToArray();
                if (enqueueCandidates.Length != 1 ||
                    !IsExpectedQueuedReplyMethod(enqueueCandidates[0], _npcDataPacketType))
                {
                    reason = "NPC queued-reply publication signature drifted";
                    return false;
                }
                _queuedNpcReplyMethod = enqueueCandidates[0];

                MethodInfo[] shownCandidates = behaviorType.GetMethods(instanceFlags)
                    .Where(method => string.Equals(
                        method.Name,
                        "ShowNpcSpeechOutput",
                        StringComparison.Ordinal))
                    .ToArray();
                if (shownCandidates.Length != 1 ||
                    !IsExpectedShownReplyMethod(shownCandidates[0], _npcDataPacketType))
                {
                    reason = "NPC shown-reply publication signature drifted";
                    return false;
                }
                _shownNpcReplyMethod = shownCandidates[0];
                _shownVisualDurationField = _shownNpcReplyMethod.ReturnType.GetField(
                    "VisualDurationSeconds",
                    instanceFlags);
                if (_shownVisualDurationField?.FieldType != typeof(float))
                {
                    reason = "NPC shown-reply playback result signature drifted";
                    return false;
                }

                MethodInfo[] promptCandidates = behaviorType.GetMethods(
                        BindingFlags.Static | BindingFlags.NonPublic)
                    .Where(method => string.Equals(
                        method.Name,
                        "BuildSceneSingleNpcTaskSystemBlock",
                        StringComparison.Ordinal))
                    .ToArray();
                if (promptCandidates.Length != 1 ||
                    !IsExpectedReplyPromptMethod(promptCandidates[0]))
                {
                    reason = "AF single-NPC reply prompt signature drifted";
                    return false;
                }
                _replyPromptMethod = promptCandidates[0];

                MethodInfo[] strictPromptCandidates = behaviorType.GetMethods(
                        BindingFlags.Static | BindingFlags.NonPublic)
                    .Where(method => string.Equals(
                        method.Name,
                        "BuildStrictSceneMessagesSystemPrompt",
                        StringComparison.Ordinal))
                    .ToArray();
                if (strictPromptCandidates.Length != 1 ||
                    !IsExpectedStrictSceneMessagesSystemPromptMethod(strictPromptCandidates[0]))
                {
                    reason = "AF strict scene system prompt signature drifted";
                    return false;
                }
                _strictSceneMessagesSystemPromptMethod = strictPromptCandidates[0];

                _resumeGameMethod = behaviorType.GetMethod(
                    "ResumeGame",
                    instanceFlags,
                    null,
                    Type.EmptyTypes,
                    null);
                if (_resumeGameMethod == null || _resumeGameMethod.ReturnType != typeof(void))
                {
                    reason = "AF ResumeGame signature drifted";
                    return false;
                }
                _pauseGameMethod = behaviorType.GetMethod(
                    "PauseGame",
                    instanceFlags,
                    null,
                    Type.EmptyTypes,
                    null);
                if (_pauseGameMethod == null || _pauseGameMethod.ReturnType != typeof(void))
                {
                    reason = "AF PauseGame signature drifted";
                    return false;
                }

                Type networkType = afAssembly.GetType(
                    "AnimusForge.ShoutNetwork",
                    false,
                    false);
                MethodInfo[] classifierCandidates = networkType?
                    .GetMethods(BindingFlags.Static | BindingFlags.Public)
                    .Where(method => string.Equals(
                        method.Name,
                        "CallApiWithMessages",
                        StringComparison.Ordinal))
                    .ToArray() ?? Array.Empty<MethodInfo>();
                if (classifierCandidates.Length != 1 ||
                    !IsExpectedClassifierMethod(classifierCandidates[0]))
                {
                    reason = "AF primary classifier API signature drifted";
                    return false;
                }
                _classifierApiMethod = classifierCandidates[0];

                MethodInfo prefix = typeof(AfCompatV130).GetMethod(
                    nameof(ObserveAcceptedPlayerShout),
                    BindingFlags.Static | BindingFlags.NonPublic);
                MethodInfo recordedPlayerPostfix = typeof(AfCompatV130).GetMethod(
                    nameof(ObserveRecordedPlayerMessage),
                    BindingFlags.Static | BindingFlags.NonPublic);
                MethodInfo queuedPostfix = typeof(AfCompatV130).GetMethod(
                    nameof(ObserveQueuedNpcReply),
                    BindingFlags.Static | BindingFlags.NonPublic);
                MethodInfo shownPostfix = typeof(AfCompatV130).GetMethod(
                    nameof(ObserveShownNpcReply),
                    BindingFlags.Static | BindingFlags.NonPublic);
                MethodInfo shownPrefix = typeof(AfCompatV130).GetMethod(
                    nameof(DeferBattleSpeechReply),
                    BindingFlags.Static | BindingFlags.NonPublic);
                MethodInfo promptPrefix = typeof(AfCompatV130).GetMethod(
                    nameof(ReplaceBattleSpeechReplyPrompt),
                    BindingFlags.Static | BindingFlags.NonPublic);
                MethodInfo strictPromptPrefix = typeof(AfCompatV130).GetMethod(
                    nameof(ReplaceBattleSpeechOrdinaryTaskPreamble),
                    BindingFlags.Static | BindingFlags.NonPublic);
                _harmony = new Harmony(HarmonyId);
                _harmony.Patch(_patchedMethod, prefix: new HarmonyMethod(prefix));
                _harmony.Patch(
                    _recordPlayerMessageMethod,
                    postfix: new HarmonyMethod(recordedPlayerPostfix));
                _harmony.Patch(
                    _queuedNpcReplyMethod,
                    postfix: new HarmonyMethod(queuedPostfix));
                _harmony.Patch(
                    _shownNpcReplyMethod,
                    prefix: new HarmonyMethod(shownPrefix),
                    postfix: new HarmonyMethod(shownPostfix));
                _harmony.Patch(
                    _replyPromptMethod,
                    prefix: new HarmonyMethod(promptPrefix));
                _harmony.Patch(
                    _strictSceneMessagesSystemPromptMethod,
                    prefix: new HarmonyMethod(strictPromptPrefix));
                _classifierProvider =
                    new AfV130AuxiliaryTextClassifier(_classifierApiMethod);
                _classifierRegistration = SceneActionsRuntimeHost.RegisterClassifier(
                    ClassifierProviderId,
                    _classifierProvider);
                _installed = true;
                reason =
                    "AF player-shout, NPC-reply, deferred speech and classifier structural contracts matched";
                return true;
            }
            catch (Exception ex)
            {
                reason = ex.GetType().Name + ": " + ex.Message;
                Uninstall();
                return false;
            }
        }

        public static void Uninstall()
        {
            try
            {
                _classifierRegistration?.Dispose();
            }
            catch (Exception ex)
            {
                SceneActionsLog.Error("COMPAT", "Classifier unregister failed.", ex);
            }
            finally
            {
                _classifierRegistration = null;
            }

            try
            {
                _classifierProvider?.Dispose();
            }
            catch (Exception ex)
            {
                SceneActionsLog.Error("COMPAT", "Classifier disposal failed.", ex);
            }
            finally
            {
                _classifierProvider = null;
            }

            try
            {
                _harmony?.UnpatchAll(HarmonyId);
            }
            catch (Exception ex)
            {
                SceneActionsLog.Error("COMPAT", "Harmony unpatch failed.", ex);
            }
            finally
            {
                _installed = false;
                _harmony = null;
                _patchedMethod = null;
                _pauseGameMethod = null;
                _resumeGameMethod = null;
                _recordPlayerMessageMethod = null;
                _queuedNpcReplyMethod = null;
                _shownNpcReplyMethod = null;
                _replyPromptMethod = null;
                _strictSceneMessagesSystemPromptMethod = null;
                _shownVisualDurationField = null;
                _classifierApiMethod = null;
                _getAgentsMethod = null;
                _buildTargetingContextMethod = null;
                _extractNpcDataMethod = null;
                _speechPopupShowMethod = null;
                _passiveNpcResponseMethod = null;
                _sceneDescriptionMethod = null;
                _contextField = null;
                _conversationEpochField = null;
                _primaryIndexField = null;
                _currentInstanceProperty = null;
                _npcDataPacketType = null;
                _npcAgentIndexField = null;
                _npcAgentIndexProperty = null;
                _replayDepth = 0;
                _suppressRecordedObservationDepth = 0;
                _audienceReplyDepth = 0;
                _behaviorInstance = null;
                lock (ReplyObservationSync)
                {
                    QueuedReplyObservations.Clear();
                }
            }
        }

        private static void ObserveRecordedPlayerMessage(
            object __instance,
            string __0,
            object __1,
            int __2,
            string __3,
            object __4)
        {
            try
            {
                CaptureBehaviorInstance(__instance);
                if (!_installed || string.IsNullOrWhiteSpace(__0))
                {
                    return;
                }
                if (Volatile.Read(ref _suppressRecordedObservationDepth) > 0)
                {
                    return;
                }
                Mission mission = Mission.Current;
                Agent player = Agent.Main;
                if (mission == null || player == null)
                {
                    return;
                }
                Agent primary = mission.Agents.FirstOrDefault(agent =>
                    agent != null &&
                    agent.Index == __2 &&
                    agent.IsActive());
                List<Agent> framed = new List<Agent>();
                IDictionary dictionary = __4 as IDictionary;
                if (dictionary != null)
                {
                    foreach (DictionaryEntry entry in dictionary)
                    {
                        Agent agent = entry.Value as Agent;
                        if (agent == null ||
                            ReferenceEquals(agent, player) ||
                            framed.Any(existing => ReferenceEquals(existing, agent)))
                        {
                            continue;
                        }
                        framed.Add(agent);
                    }
                }
                if (primary != null &&
                    !framed.Any(agent => ReferenceEquals(agent, primary)))
                {
                    framed.Insert(0, primary);
                }
                int conversationEpoch = (int)_conversationEpochField.GetValue(__instance);
                BattleSpeechRuntimeHost.SubmitPlayerShout(
                    mission,
                    __0,
                    player,
                    primary,
                    framed,
                    conversationEpoch,
                    mission.CurrentTime);
            }
            catch (Exception ex)
            {
                SceneActionsLog.Error(
                    "BATTLE_SPEECH",
                    "AF accepted player-message observer failed closed.",
                    ex);
            }
        }
        private static bool ObserveAcceptedPlayerShout(
            object __instance,
            string shoutText,
            string extraFact,
            int? forcedPrimaryAgentIndex)
        {
            try
            {
                CaptureBehaviorInstance(__instance);
                if (!_installed || __instance == null || string.IsNullOrWhiteSpace(shoutText))
                {
                    return true;
                }
                // Reflection replay is used only after the lightweight route has
                // classified a message as ordinary AF or needs AF to generate an
                // NPC body. Do not recursively claim that replay as a new speech.
                if (Volatile.Read(ref _replayDepth) > 0)
                {
                    return true;
                }
                Mission mission = Mission.Current;
                Agent player = Agent.Main;
                if (mission == null || player == null)
                {
                    return true;
                }

                object context = _contextField.GetValue(__instance);
                List<Agent> framed = new List<Agent>();
                Agent primary = null;
                if (context != null)
                {
                    object rawAgents = _getAgentsMethod.Invoke(__instance, new[] { context });
                    IEnumerable enumerable = rawAgents as IEnumerable;
                    if (enumerable != null)
                    {
                        foreach (object item in enumerable)
                        {
                            Agent agent = item as Agent;
                            if (agent == null || ReferenceEquals(agent, player) ||
                                framed.Any(existing => ReferenceEquals(existing, agent)))
                            {
                                continue;
                            }
                            framed.Add(agent);
                        }
                    }
                    int primaryIndex = forcedPrimaryAgentIndex ??
                                       (int)_primaryIndexField.GetValue(context);
                    primary = framed.FirstOrDefault(agent => agent.Index == primaryIndex);
                }
                int conversationEpoch = _conversationEpochField != null
                    ? (int)_conversationEpochField.GetValue(__instance)
                    : 0;

                BattleSpeechTriggerDecisionV2 localDecision =
                    BattleSpeechFrameworkV2.ParsePlayerShout(shoutText);

                // Explicit "演讲：..." is a closed, forced command. Route it
                // before touching AF's context so a missing/late target context
                // cannot send the player's speech back to ordinary self-talk.
                if (localDecision.Force &&
                    BattleSpeechRuntimeHost.TryPreRouteForcedPlayerShout(
                        mission,
                        shoutText,
                        player,
                        primary,
                        framed,
                        conversationEpoch,
                        mission.CurrentTime,
                        out bool allowForcedOriginal))
                {
                    if (allowForcedOriginal)
                    {
                        return true;
                    }
                    ResumeAfShoutUi(__instance);
                    return false;
                }

                bool naturalCandidate = !localDecision.Force &&
                    (localDecision.Kind == BattleSpeechTriggerKindV2.DeliverPlayerSpeech ||
                     localDecision.Kind == BattleSpeechTriggerKindV2.ArmPlayerSpeech ||
                     localDecision.Kind == BattleSpeechTriggerKindV2.Cancel ||
                     localDecision.Kind == BattleSpeechTriggerKindV2.NeedsClassifier);
                if (naturalCandidate)
                {
                    if (BattleSpeechRuntimeHost.TryPreRouteNaturalPlayerShout(
                        __instance,
                        mission,
                        shoutText,
                        extraFact,
                        forcedPrimaryAgentIndex,
                        player,
                        primary,
                        framed,
                        conversationEpoch,
                        mission.CurrentTime,
                        out bool allowNaturalOriginal))
                    {
                        if (allowNaturalOriginal)
                        {
                            return true;
                        }
                        ResumeAfShoutUi(__instance);
                        return false;
                    }
                }

                // A null context makes AF fall back to its own broad nearby scan;
                // this adapter deliberately refuses to invent a framed target.
                if (context == null)
                {
                    return true;
                }
                if (!localDecision.Force &&
                    localDecision.Kind == BattleSpeechTriggerKindV2.RequestNpcSpeech &&
                    BattleSpeechRuntimeHost.TryPreRouteNaturalPlayerShout(
                        __instance,
                        mission,
                        shoutText,
                        extraFact,
                        forcedPrimaryAgentIndex,
                        player,
                        primary,
                        framed,
                        conversationEpoch,
                        mission.CurrentTime,
                        out bool allowNpcOriginal))
                {
                    // Natural NPC speech intentionally runs the AF body generator;
                    // its recorded-message observer will create the frozen session.
                    return allowNpcOriginal;
                }
                SceneActionsRuntimeHost.SubmitPlayerSceneShout(
                    Guid.NewGuid(),
                    mission,
                    shoutText,
                    player,
                    primary,
                    framed,
                    mission.CurrentTime);
                return true;
            }
            catch (Exception ex)
            {
                // This prefix is an observer. It must never propagate into AF's original chain.
                SceneActionsLog.Error("COMPAT", "AF observer callback failed closed.", ex);
                return true;
            }
        }

        internal static void CompleteDedicatedSpeechMenuInput()
        {
            try
            {
                object behavior = _currentInstanceProperty?.GetValue(null, null) ??
                                  _behaviorInstance;
                ResumeAfShoutUi(behavior);
            }
            catch (Exception ex)
            {
                SceneActionsLog.Warning(
                    "BATTLE_SPEECH_UI",
                    "Failed to resume AF after Y-menu speech input: " +
                    ex.GetType().Name + ": " + ex.Message);
            }
        }

        internal static bool BeginDedicatedSpeechMenuInput()
        {
            try
            {
                object behavior = _currentInstanceProperty?.GetValue(null, null) ??
                                  _behaviorInstance;
                if (behavior == null || _pauseGameMethod == null)
                {
                    return false;
                }
                _pauseGameMethod.Invoke(behavior, null);
                return true;
            }
            catch (Exception ex)
            {
                SceneActionsLog.Warning(
                    "BATTLE_SPEECH_UI",
                    "Failed to pause AF after Y-menu speech selection: " +
                    ex.GetType().Name + ": " + ex.Message);
                return false;
            }
        }

        internal static bool TryOpenDedicatedSpeechInput(
            string title,
            string subtitle,
            string inputHint,
            Action<string> onSubmit,
            Action onCancel)
        {
            if (_speechPopupShowMethod == null || onSubmit == null || onCancel == null)
            {
                return false;
            }
            try
            {
                object result = _speechPopupShowMethod.Invoke(
                    null,
                    new object[]
                    {
                        title ?? string.Empty,
                        subtitle ?? string.Empty,
                        inputHint ?? string.Empty,
                        string.Empty,
                        onSubmit,
                        onCancel,
                        null
                    });
                return result is bool opened && opened;
            }
            catch (Exception ex)
            {
                SceneActionsLog.Warning(
                    "BATTLE_SPEECH_UI",
                    "AF speech popup bridge failed; caller should use the fallback input. " +
                    ex.GetType().Name + ": " + ex.Message);
                return false;
            }
        }

        internal static async Task<DedicatedNpcSpeechResultV1> GenerateDedicatedNpcSpeechAsync(
            Agent speaker,
            IReadOnlyList<Agent> framedTargets,
            string topic)
        {
            DedicatedNpcSpeechResultV1 failure = new DedicatedNpcSpeechResultV1
            {
                Speaker = speaker,
                Error = "AF dedicated NPC speech generation is unavailable."
            };
            if (!_installed || _passiveNpcResponseMethod == null ||
                _extractNpcDataMethod == null || speaker == null || !speaker.IsActive())
            {
                return failure;
            }
            try
            {
                object behavior = _currentInstanceProperty?.GetValue(null, null) ?? _behaviorInstance;
                if (behavior == null)
                {
                    failure.Error = "AF ShoutBehavior instance is unavailable.";
                    return failure;
                }
                object primaryPacket = _extractNpcDataMethod.Invoke(
                    null,
                    new object[] { speaker });
                if (primaryPacket == null)
                {
                    failure.Error = "AF could not build the NPC speech packet.";
                    return failure;
                }
                Type listType = typeof(List<>).MakeGenericType(_npcDataPacketType);
                IList packetList = (IList)Activator.CreateInstance(listType);
                packetList.Add(primaryPacket);
                if (framedTargets != null)
                {
                    for (int i = 0; i < framedTargets.Count; i++)
                    {
                        Agent target = framedTargets[i];
                        if (target == null || !target.IsActive() || target.Index == speaker.Index)
                        {
                            continue;
                        }
                        object packet = _extractNpcDataMethod.Invoke(
                            null,
                            new object[] { target });
                        if (packet != null)
                        {
                            packetList.Add(packet);
                        }
                    }
                }
                string sceneDescription = "战场";
                if (_sceneDescriptionMethod != null)
                {
                    sceneDescription = _sceneDescriptionMethod.Invoke(null, null) as string ?? sceneDescription;
                }
                object responseTask = _passiveNpcResponseMethod.Invoke(
                    behavior,
                    new object[]
                    {
                        primaryPacket,
                        sceneDescription,
                        topic ?? string.Empty,
                        string.Empty,
                        packetList,
                        new Dictionary<int, TaleWorlds.CampaignSystem.Hero>()
                    });
                if (!(responseTask is Task<string> task))
                {
                    failure.Error = "AF returned an unexpected NPC speech task.";
                    return failure;
                }
                string response = (await task.ConfigureAwait(false) ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(response) ||
                    response.StartsWith("（错误", StringComparison.Ordinal) ||
                    response.StartsWith("（API请求失败", StringComparison.Ordinal))
                {
                    failure.Error = "AF returned an empty or failed NPC speech body.";
                    return failure;
                }
                return new DedicatedNpcSpeechResultV1
                {
                    Speaker = speaker,
                    AfBehavior = behavior,
                    AfNpcPacket = primaryPacket,
                    Content = response
                };
            }
            catch (TargetInvocationException ex)
            {
                failure.Error = (ex.InnerException ?? ex).Message;
                return failure;
            }
            catch (Exception ex)
            {
                failure.Error = ex.GetType().Name + ": " + ex.Message;
                return failure;
            }
        }

        private static void ObserveQueuedNpcReply(
            object __instance,
            object __0,
            string __1,
            int __6,
            Func<bool> __13,
            string __14,
            string __15)
        {
            try
            {
                CaptureBehaviorInstance(__instance);
                if (__13 != null && !__13())
                {
                    return;
                }
                if (string.IsNullOrWhiteSpace(__14) || string.IsNullOrWhiteSpace(__15))
                {
                    return;
                }
                Mission mission = Mission.Current;
                if (mission == null || !TryResolveNpcSpeaker(mission, __0, null, out Agent speaker))
                {
                    return;
                }
                if (!BattleSpeechReplyBindingV1.ReplyMatches(__15, __1))
                {
                    return;
                }
                BattleSpeechRuntimeHost.SubmitQueuedNpcReplyCandidate(
                    mission,
                    speaker,
                    __1,
                    __6,
                    __14,
                    __15,
                    mission.CurrentTime);
                ObserveNpcReply(__0, null, __1, isQueuedPublication: true);
            }
            catch (Exception ex)
            {
                SceneActionsLog.Error(
                    "COMPAT",
                    "AF queued NPC reply observer failed closed.",
                    ex);
            }
        }

        private static void ObserveShownNpcReply(
            object __instance,
            object __0,
            Agent __1,
            string __2,
            object __result)
        {
            try
            {
                CaptureBehaviorInstance(__instance);
                if (Volatile.Read(ref _audienceReplyDepth) > 0)
                {
                    return;
                }
                if (__result == null || _shownVisualDurationField == null)
                {
                    return;
                }
                object rawDuration = _shownVisualDurationField.GetValue(__result);
                if (!(rawDuration is float duration) || duration <= 0f)
                {
                    return;
                }
                ObserveNpcReply(__0, __1, __2, isQueuedPublication: false);
                Mission mission = Mission.Current;
                if (mission != null && __1 != null && __1.IsActive())
                {
                    int conversationEpoch = _conversationEpochField != null && __instance != null
                        ? (int)_conversationEpochField.GetValue(__instance)
                        : 0;
                    BattleSpeechRuntimeHost.SubmitShownNpcReply(
                        mission,
                        __1,
                        __2,
                        conversationEpoch,
                        mission.CurrentTime);
                }
            }
            catch (Exception ex)
            {
                SceneActionsLog.Error(
                    "COMPAT",
                    "AF shown NPC reply observer failed closed.",
                    ex);
            }
        }

        private static bool DeferBattleSpeechReply(
            object __instance,
            object __0,
            Agent __1,
            string __2,
            bool __3,
            bool __4,
            bool __5,
            ref object __result)
        {
            CaptureBehaviorInstance(__instance);
            if (Volatile.Read(ref _replayDepth) > 0)
            {
                return true;
            }
            try
            {
                int conversationEpoch = _conversationEpochField != null && __instance != null
                    ? (int)_conversationEpochField.GetValue(__instance)
                    : 0;
                if (!BattleSpeechRuntimeHost.TryDeferShownNpcReply(
                    __instance,
                    __0,
                    __1,
                    __2,
                    __3,
                    __4,
                    __5,
                    conversationEpoch,
                    out bool duplicateSuppressed))
                {
                    return true;
                }
                __result = Activator.CreateInstance(_shownNpcReplyMethod.ReturnType);
                if (duplicateSuppressed)
                {
                    SceneActionsLog.Info(
                        "BATTLE_SPEECH_COMPAT",
                        "Suppressed duplicate claimed AF speech reply. Agent=" +
                        (__1?.Index ?? -1));
                }
                else
                {
                    SceneActionsLog.Info(
                        "BATTLE_SPEECH_COMPAT",
                        "Deferred AF reply until the NPC reaches the speech line. Agent=" +
                        (__1?.Index ?? -1));
                }
                return false;
            }
            catch (Exception ex)
            {
                SceneActionsLog.Error(
                    "BATTLE_SPEECH_COMPAT",
                    "Reply deferral failed open; AF will show the original reply.",
                    ex);
                return true;
            }
        }

        private static bool ReplaceBattleSpeechReplyPrompt(
            string __0,
            bool __1,
            int __2,
            int __3,
            string __4,
            ref string __result)
        {
            try
            {
                if (!BattleSpeechRuntimeHost.TryGetReplyPromptInstruction(
                        __0,
                        out string instruction))
                {
                    return true;
                }
                // AF's original block contains the ordinary-NPC contract
                // (actions, inner thoughts and player-facing replies). Appending
                // our text left those instructions active and caused the model
                // to produce a normal reply which was later rejected. Replace
                // the whole block only for the currently claimed speech NPC.
                __result = instruction;
                SceneActionsLog.Info(
                    "BATTLE_SPEECH_COMPAT",
                    "Replaced AF ordinary NPC prompt with dedicated battle-speech prompt. " +
                    "Npc=" + (__0 ?? string.Empty));
                return false;
            }
            catch (Exception ex)
            {
                SceneActionsLog.Error(
                    "BATTLE_SPEECH_COMPAT",
                    "Battle-speech prompt replacement failed open.",
                    ex);
                return true;
            }
        }

        private static bool ReplaceBattleSpeechOrdinaryTaskPreamble(
            string __0,
            bool __1,
            ref string __result)
        {
            try
            {
                if (!BattleSpeechRuntimeHost.HasActiveNpcSpeechClaim() ||
                    string.IsNullOrWhiteSpace(__0))
                {
                    return true;
                }

                const string ordinaryPreamble =
                    "你是【站在你旁边的人】中的NPC角色,可能是多个人。你们的唯一任务是：根据下方提供的角色信息、场景信息和对话历史，以NPC身份直接回复";
                const string ordinaryPreambleEnd =
                    "\n禁止生成任何【】章节标题或格式说明。";
                int start = __0.IndexOf(ordinaryPreamble, StringComparison.Ordinal);
                if (start < 0)
                {
                    return true;
                }
                int end = __0.IndexOf(
                    ordinaryPreambleEnd,
                    start + ordinaryPreamble.Length,
                    StringComparison.Ordinal);
                if (end < 0)
                {
                    return true;
                }

                const string battleSpeechPreamble =
                    "你是当前阵前演讲中的NPC演讲者。你的唯一任务是：根据下方提供的角色身份、战场处境和对话历史，面向己方士兵发表战前动员。你不是在回复玩家，也不是在进行普通闲聊。";
                __result = __0.Substring(0, start) +
                           battleSpeechPreamble +
                           __0.Substring(end + ordinaryPreambleEnd.Length);
                SceneActionsLog.Info(
                    "BATTLE_SPEECH_COMPAT",
                    "Removed AF ordinary player-reply task preamble from speech system prompt.");
                return false;
            }
            catch (Exception ex)
            {
                SceneActionsLog.Error(
                    "BATTLE_SPEECH_COMPAT",
                    "Battle-speech task-preamble replacement failed open.",
                    ex);
                return true;
            }
        }

        internal static bool TryReplayOriginalPlayerShout(
            BattleSpeechCapturedInputV1 input,
            bool observeForBattleSpeech,
            out string error)
        {
            error = null;
            if (!_installed || input == null || _patchedMethod == null ||
                string.IsNullOrWhiteSpace(input.RawText))
            {
                error = "AF original shout replay is unavailable.";
                return false;
            }
            object behavior = input.OriginalAfBehavior ?? _behaviorInstance;
            if (behavior == null || !_patchedMethod.DeclaringType.IsInstanceOfType(behavior))
            {
                error = "AF ShoutBehavior instance is unavailable for replay.";
                return false;
            }

            Interlocked.Increment(ref _replayDepth);
            if (!observeForBattleSpeech)
            {
                Interlocked.Increment(ref _suppressRecordedObservationDepth);
            }
            try
            {
                _patchedMethod.Invoke(
                    behavior,
                    new object[]
                    {
                        input.RawText,
                        input.OriginalExtraFact,
                        input.OriginalForcedPrimaryAgentIndex
                    });
                return true;
            }
            catch (TargetInvocationException ex)
            {
                error = (ex.InnerException ?? ex).Message;
                return false;
            }
            catch (Exception ex)
            {
                error = ex.GetType().Name + ": " + ex.Message;
                return false;
            }
            finally
            {
                if (!observeForBattleSpeech)
                {
                    Interlocked.Decrement(ref _suppressRecordedObservationDepth);
                }
                Interlocked.Decrement(ref _replayDepth);
            }
        }

        internal static bool TryReplayDeferredReply(
            DeferredNpcReplyV2 deferred,
            out string error)
        {
            error = null;
            if (!_installed || deferred == null || _shownNpcReplyMethod == null ||
                deferred.Behavior == null || deferred.NpcPacket == null ||
                deferred.Speaker == null || !deferred.Speaker.IsActive())
            {
                error = "Deferred AF reply is no longer replayable.";
                return false;
            }
            Interlocked.Increment(ref _replayDepth);
            try
            {
                object result = _shownNpcReplyMethod.Invoke(
                    deferred.Behavior,
                    new object[]
                    {
                        deferred.NpcPacket,
                        deferred.Speaker,
                        deferred.Content,
                        deferred.AllowTts,
                        deferred.AttachTtsToSceneAgent,
                        deferred.SuppressInteractionTimeoutArm
                    });
                if (result == null)
                {
                    error = "AF replay returned no playback result.";
                    return false;
                }
                return true;
            }
            catch (TargetInvocationException ex)
            {
                error = (ex.InnerException ?? ex).Message;
                return false;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
            finally
            {
                Interlocked.Decrement(ref _replayDepth);
            }
        }

        internal static bool TryShowAudienceReply(
            Agent speaker,
            string content,
            out float visualDurationSeconds,
            out string error)
        {
            visualDurationSeconds = 0f;
            error = null;
            object behavior = _behaviorInstance;
            if (!_installed || behavior == null || _shownNpcReplyMethod == null ||
                _extractNpcDataMethod == null || speaker == null ||
                !speaker.IsActive() || string.IsNullOrWhiteSpace(content))
            {
                error = "AF audience-reply playback is unavailable.";
                return false;
            }
            Interlocked.Increment(ref _replayDepth);
            Interlocked.Increment(ref _audienceReplyDepth);
            try
            {
                object npcPacket = _extractNpcDataMethod.Invoke(
                    null,
                    new object[] { speaker });
                if (npcPacket == null || !_npcDataPacketType.IsInstanceOfType(npcPacket))
                {
                    error = "AF could not build an audience NPC packet.";
                    return false;
                }
                object result = _shownNpcReplyMethod.Invoke(
                    behavior,
                    new object[] { npcPacket, speaker, content, true, true, true });
                if (result == null)
                {
                    error = "AF returned no audience-reply playback result.";
                    return false;
                }
                object rawDuration = _shownVisualDurationField?.GetValue(result);
                if (rawDuration is float duration)
                {
                    visualDurationSeconds = Math.Max(0f, duration);
                }
                return true;
            }
            catch (TargetInvocationException ex)
            {
                error = (ex.InnerException ?? ex).Message;
                return false;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
            finally
            {
                Interlocked.Decrement(ref _audienceReplyDepth);
                Interlocked.Decrement(ref _replayDepth);
            }
        }

        private static void CaptureBehaviorInstance(object behavior)
        {
            if (_installed && behavior != null && _patchedMethod != null &&
                _patchedMethod.DeclaringType.IsInstanceOfType(behavior))
            {
                _behaviorInstance = behavior;
            }
        }

        private static void ResumeAfShoutUi(object behavior)
        {
            try
            {
                if (behavior != null && _resumeGameMethod != null)
                {
                    _resumeGameMethod.Invoke(behavior, null);
                }
            }
            catch (Exception ex)
            {
                SceneActionsLog.Error(
                    "BATTLE_SPEECH_COMPAT",
                    "Failed to resume AF shout UI after forced speech claim.",
                    ex);
            }
        }

        private static void ObserveNpcReply(
            object npcPacket,
            Agent knownSpeaker,
            string content,
            bool isQueuedPublication)
        {
            if (!_installed ||
                !SceneActionsRuntimeHost.IsInitialized ||
                string.IsNullOrWhiteSpace(content) ||
                !TryReadNpcAgentIndex(npcPacket, out int agentIndex))
            {
                return;
            }


            Mission mission = Mission.Current;
            if (mission == null)
            {
                return;
            }
            if (!TryResolveNpcSpeaker(mission, npcPacket, knownSpeaker, out Agent speaker) ||
                speaker.Index != agentIndex)
            {
                return;
            }
            if (BattleSpeechRuntimeHost.IsClaimedNpcReply(mission, speaker, content))
            {
                SceneActionsLog.Info(
                    "BATTLE_SPEECH_COMPAT",
                    "Claimed speech reply bypassed ordinary SceneActions. Agent=" + agentIndex);
                return;
            }

            ParseDecision decision = SceneActionsRuntimeHost.Parser.ParseNpcReplyText(
                content,
                speaker.Name);

            double now = mission.CurrentTime;
            string fingerprint = BuildReplyFingerprint(content);
            string resolutionKey = BuildReplyResolutionKey(decision);
            if (!isQueuedPublication && TryConsumeQueuedReplyObservation(
                mission,
                agentIndex,
                resolutionKey,
                fingerprint,
                now))
            {
                return;
            }

            bool submitted = SceneActionsRuntimeHost.SubmitNpcReply(
                Guid.NewGuid(),
                mission,
                speaker,
                content,
                now);
            if (submitted && isQueuedPublication)
            {
                RecordQueuedReplyObservation(
                    mission,
                    agentIndex,
                    resolutionKey,
                    fingerprint,
                    now);
            }
        }

        private static bool TryResolveNpcSpeaker(
            Mission mission,
            object npcPacket,
            Agent knownSpeaker,
            out Agent speaker)
        {
            speaker = knownSpeaker;
            if (speaker == null || !speaker.IsActive() ||
                (TryReadNpcAgentIndex(npcPacket, out int knownIndex) && speaker.Index != knownIndex))
            {
                speaker = null;
            }
            if (speaker == null &&
                TryReadNpcAgentIndex(npcPacket, out int agentIndex))
            {
                speaker = mission?.Agents?.FirstOrDefault(agent =>
                    agent != null && agent.Index == agentIndex);
            }
            return speaker != null &&
                   !ReferenceEquals(speaker, Agent.Main) &&
                   speaker.IsActive();
        }

        private static bool TryReadNpcAgentIndex(object npcPacket, out int agentIndex)
        {
            agentIndex = -1;
            if (npcPacket == null ||
                _npcDataPacketType == null ||
                !_npcDataPacketType.IsInstanceOfType(npcPacket))
            {
                return false;
            }
            object raw = _npcAgentIndexField != null
                ? _npcAgentIndexField.GetValue(npcPacket)
                : _npcAgentIndexProperty?.GetValue(npcPacket, null);
            if (!(raw is int value) || value < 0)
            {
                return false;
            }
            agentIndex = value;
            return true;
        }

        private static string BuildReplyFingerprint(string content)
        {
            string normalized = CommandParser.Normalize(content ?? string.Empty);
            return string.Join(
                " ",
                normalized.Split(
                    new[] { ' ', '\t', '\r', '\n' },
                 StringSplitOptions.RemoveEmptyEntries));
        }

        private static string BuildReplyResolutionKey(ParseDecision decision)
        {
            if (decision == null)
            {
                return "<consent-reply>";
            }
            if (decision.Status == ParseStatus.Matched)
            {
                return decision.IntentKey;
            }
            if (decision.AiFallbackRequested)
            {
                return "<ai-fallback>";
            }
            return decision.Status == ParseStatus.Invalid
                ? "<invalid-stage>"
                : "<consent-reply>";
        }

        private static void RecordQueuedReplyObservation(
            Mission mission,
            int agentIndex,
            string intentKey,
            string fingerprint,
            double now)
        {
            lock (ReplyObservationSync)
            {
                CleanupQueuedReplyObservations(mission, now);
                QueuedReplyObservations.Add(new QueuedReplyObservation
                {
                    Mission = mission,
                    AgentIndex = agentIndex,
                    IntentKey = intentKey,
                    Fingerprint = fingerprint,
                    ObservedAtMissionTime = now
                });
            }
        }

        private static bool TryConsumeQueuedReplyObservation(
            Mission mission,
            int agentIndex,
            string intentKey,
            string fingerprint,
            double now)
        {
            lock (ReplyObservationSync)
            {
                CleanupQueuedReplyObservations(mission, now);
                int index = QueuedReplyObservations.FindIndex(observation =>
                    ReferenceEquals(observation.Mission, mission) &&
                    observation.AgentIndex == agentIndex &&
                    string.Equals(observation.IntentKey, intentKey, StringComparison.Ordinal) &&
                    string.Equals(
                        observation.Fingerprint,
                        fingerprint,
                        StringComparison.Ordinal));
                if (index < 0)
                {
                    return false;
                }
                QueuedReplyObservations.RemoveAt(index);
                return true;
            }
        }

        private static void CleanupQueuedReplyObservations(Mission mission, double now)
        {
            QueuedReplyObservations.RemoveAll(observation =>
                observation == null ||
                !ReferenceEquals(observation.Mission, mission) ||
                double.IsNaN(now) ||
                now < observation.ObservedAtMissionTime ||
                now - observation.ObservedAtMissionTime > QueuedReplyObservationTtlSeconds);
        }

        private static bool IsExpectedRecordedPlayerMessageMethod(
            MethodInfo method,
            Type npcType)
        {
            ParameterInfo[] parameters = method?.GetParameters();
            return method != null &&
                   method.ReturnType == typeof(void) &&
                   parameters.Length == 5 &&
                   parameters[0].ParameterType == typeof(string) &&
                   IsListOf(parameters[1].ParameterType, npcType) &&
                   parameters[2].ParameterType == typeof(int) &&
                   parameters[3].ParameterType == typeof(string) &&
                   IsDictionaryOfIntAgent(parameters[4].ParameterType);
        }
        private static bool IsExpectedQueuedReplyMethod(MethodInfo method, Type npcType)
        {
            ParameterInfo[] parameters = method?.GetParameters();
            return method != null &&
                   method.ReturnType == typeof(void) &&
                   parameters.Length == 16 &&
                   parameters[0].ParameterType == npcType &&
                   parameters[1].ParameterType == typeof(string) &&
                   IsListOf(parameters[2].ParameterType, npcType) &&
                   parameters[3].ParameterType == typeof(bool) &&
                   parameters[4].ParameterType == typeof(bool) &&
                   parameters[5].ParameterType == typeof(bool) &&
                   parameters[6].ParameterType == typeof(int) &&
                   IsList(parameters[7].ParameterType) &&
                   IsList(parameters[8].ParameterType) &&
                   parameters[9].ParameterType == typeof(string) &&
                   parameters[10].ParameterType == typeof(TaskCompletionSource<bool>) &&
                   parameters[11].ParameterType == typeof(float) &&
                   parameters[12].ParameterType == typeof(int) &&
                   parameters[13].ParameterType == typeof(Func<bool>) &&
                   parameters[14].ParameterType == typeof(string) &&
                   parameters[15].ParameterType == typeof(string);
        }

        private static bool IsExpectedShownReplyMethod(MethodInfo method, Type npcType)
        {
            ParameterInfo[] parameters = method?.GetParameters();
            return method != null &&
                   method.ReturnType != typeof(void) &&
                   parameters.Length == 6 &&
                   parameters[0].ParameterType == npcType &&
                   parameters[1].ParameterType == typeof(Agent) &&
                   parameters[2].ParameterType == typeof(string) &&
                   parameters[3].ParameterType == typeof(bool) &&
                   parameters[4].ParameterType == typeof(bool) &&
                   parameters[5].ParameterType == typeof(bool);
        }

        private static bool IsExpectedReplyPromptMethod(MethodInfo method)
        {
            ParameterInfo[] parameters = method?.GetParameters();
            return method != null &&
                   method.IsStatic &&
                   method.ReturnType == typeof(string) &&
                   parameters.Length == 5 &&
                   parameters[0].ParameterType == typeof(string) &&
                   parameters[1].ParameterType == typeof(bool) &&
                   parameters[2].ParameterType == typeof(int) &&
                   parameters[3].ParameterType == typeof(int) &&
                   parameters[4].ParameterType == typeof(string);
        }

        private static bool IsExpectedStrictSceneMessagesSystemPromptMethod(MethodInfo method)
        {
            ParameterInfo[] parameters = method?.GetParameters();
            return method != null &&
                   method.IsStatic &&
                   method.ReturnType == typeof(string) &&
                   parameters.Length == 2 &&
                   parameters[0].ParameterType == typeof(string) &&
                   parameters[1].ParameterType == typeof(bool);
        }

        private static bool IsExpectedClassifierMethod(MethodInfo method)
        {
            ParameterInfo[] parameters = method?.GetParameters();
            return method != null &&
                   method.IsPublic &&
                   method.IsStatic &&
                   method.ReturnType == typeof(Task<string>) &&
                   parameters.Length == 8 &&
                   parameters[0].ParameterType == typeof(List<object>) &&
                   parameters[1].ParameterType == typeof(int) &&
                   parameters[2].ParameterType == typeof(bool) &&
                   parameters[3].ParameterType == typeof(int?) &&
                   parameters[4].ParameterType == typeof(bool) &&
                   parameters[5].ParameterType == typeof(bool) &&
                   parameters[6].ParameterType == typeof(CancellationToken) &&
                   parameters[7].ParameterType == typeof(float?);
        }

        private static bool IsDictionaryOfIntAgent(Type candidate)
        {
            return candidate != null &&
                   candidate.IsGenericType &&
                   candidate.GetGenericTypeDefinition() == typeof(Dictionary<,>) &&
                   candidate.GetGenericArguments()[0] == typeof(int) &&
                   candidate.GetGenericArguments()[1] == typeof(Agent);
        }
        private static bool IsListOf(Type candidate, Type elementType)
        {
            return IsList(candidate) &&
                   candidate.GetGenericArguments()[0] == elementType;
        }

        private static bool IsList(Type candidate)
        {
            return candidate != null &&
                   candidate.IsGenericType &&
                   candidate.GetGenericTypeDefinition() == typeof(List<>);
        }

        private sealed class QueuedReplyObservation
        {
            public Mission Mission { get; set; }
            public int AgentIndex { get; set; }
            public string IntentKey { get; set; }
            public string Fingerprint { get; set; }
            public double ObservedAtMissionTime { get; set; }
        }
    }
}
