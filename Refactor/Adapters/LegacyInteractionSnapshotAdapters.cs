using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Runtime;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace AnimusForge.Refactor.Adapters;

/// <summary>
/// Main-thread-only bridge from the current AF facades to detached interaction
/// contracts. These methods deliberately capture state but do not start an LLM
/// request, execute an action, or write persistence.
/// </summary>
public static class LegacyInteractionSnapshotAdapters
{
    private static long _sessionSequence;
    private static long _configurationSequence;
    private static readonly object RuntimeConfigurationStoreLock = new object();
    private static RuntimeConfigSnapshotStore _runtimeConfigurationStore;

    public const string NativeConversationModuleId = "conversation";
    public const string LegacyShoutNetworkProviderId = "legacy-shoutnetwork";

    /// <summary>
    /// Captures the non-secret legacy provider configuration needed by the
    /// opt-in coordinator. The endpoint is intentionally a non-network
    /// marker: LegacyShoutNetworkGateway continues to resolve the real
    /// endpoint and credentials through the existing ShoutNetwork authority.
    /// </summary>
    public static RuntimeConfigSnapshot CaptureNativeConversationRuntimeConfiguration()
    {
        return GetRuntimeConfigurationStore().Capture();
    }

    /// <summary>
    /// Publishes a new detached configuration snapshot for future requests.
    /// Existing requests keep their captured immutable instance. The legacy
    /// DuelSettings/MCM authority remains the source of the replacement.
    /// </summary>
    public static bool ReloadNativeConversationRuntimeConfigurationForExternal()
    {
        return GetRuntimeConfigurationStore().TryReload(out _);
    }

    private static RuntimeConfigSnapshotStore GetRuntimeConfigurationStore()
    {
        RuntimeConfigSnapshotStore store = Volatile.Read(ref _runtimeConfigurationStore);
        if (store != null)
        {
            return store;
        }
        lock (RuntimeConfigurationStoreLock)
        {
            store = _runtimeConfigurationStore;
            if (store == null)
            {
                store = new RuntimeConfigSnapshotStore(BuildLegacyRuntimeConfigurationSnapshot);
                Volatile.Write(ref _runtimeConfigurationStore, store);
            }
            return store;
        }
    }

    private static RuntimeConfigSnapshot BuildLegacyRuntimeConfigurationSnapshot()
    {
        string model = "legacy";
        try
        {
            string configuredModel = DuelSettings.GetSettings()?.ModelName;
            if (!string.IsNullOrWhiteSpace(configuredModel))
            {
                model = configuredModel.Trim();
            }
        }
        catch
        {
            // Configuration capture must degrade to the legacy provider.
        }

        Dictionary<string, bool> enabledModules = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
        {
            [NativeConversationModuleId] = true
        };
        Dictionary<string, LlmProviderSnapshot> providers = new Dictionary<string, LlmProviderSnapshot>(StringComparer.OrdinalIgnoreCase)
        {
            [LegacyShoutNetworkProviderId] = new LlmProviderSnapshot(
                LegacyShoutNetworkProviderId,
                "legacy://shoutnetwork",
                model,
                480000,
                4096)
        };
        return new RuntimeConfigSnapshot(
            "legacy-default",
            Interlocked.Increment(ref _configurationSequence),
            enabledModules,
            providers);
    }

    /// <summary>
    /// Production wiring helper. It only connects the concrete main-thread
    /// Native capture method; rule/prompt/action ports remain supplied by the
    /// channel owner so this helper cannot silently replace legacy behavior.
    /// </summary>
    public static LegacyNativeConversationFacade CreateNativeConversationFacade(
        LegacyInteractionPipelinePorts ports,
        ILlmGateway gateway)
    {
        return CreateNativeConversationFacade(ports, gateway, (Func<string, DetachedPromptSections>)null);
    }

    /// <summary>
    /// Creates the Native opt-in facade with a main-thread-owned Prompt
    /// sections provider. The provider is called only by Capture; it must
    /// return strings built from the existing authoritative prompt helpers.
    /// </summary>
    public static LegacyNativeConversationFacade CreateNativeConversationFacade(
        LegacyInteractionPipelinePorts ports,
        ILlmGateway gateway,
        Func<string, DetachedPromptSections> promptSectionsProvider)
    {
        return new LegacyNativeConversationFacade(
            ports,
            gateway,
            SaveRuntimeGuard.CaptureGeneration,
            playerText => CaptureNativeConversation(
                playerText,
                promptSectionsProvider == null ? null : promptSectionsProvider(playerText)));
    }

    /// <summary>
    /// Atomic Native provider for both main and postprocess sections. The
    /// provider is invoked once per capture so both LLM stages use the same
    /// interaction-boundary snapshot.
    /// </summary>
    public static LegacyNativeConversationFacade CreateNativeConversationFacade(
        LegacyInteractionPipelinePorts ports,
        ILlmGateway gateway,
        Func<string, DetachedInteractionPromptSections> promptSectionsProvider)
    {
        return new LegacyNativeConversationFacade(
            ports,
            gateway,
            SaveRuntimeGuard.CaptureGeneration,
            playerText =>
            {
                DetachedInteractionPromptSections bundle = promptSectionsProvider == null
                    ? null
                    : promptSectionsProvider(playerText);
                return CaptureNativeConversation(
                    playerText,
                    bundle?.Main,
                    bundle?.Postprocess);
            });
    }

    /// <summary>
    /// Shared SceneShout lifecycle factory. Capture remains owned by the
    /// caller, so target Agent resolution and scene eligibility stay at the
    /// channel boundary.
    /// </summary>
    public static LegacyChannelInteractionFacade CreateSceneShoutInteractionFacade(
        LegacyInteractionPipelinePorts ports,
        ILlmGateway gateway,
        Func<string, InteractionEnvelope> capture)
    {
        return new LegacyChannelInteractionFacade(ports, gateway, SaveRuntimeGuard.CaptureGeneration, capture);
    }

    /// <summary>
    /// Shared Courier lifecycle factory. CourierDeliveryBehavior retains
    /// delivery/return timing and supplies the detached capture delegate.
    /// </summary>
    public static LegacyChannelInteractionFacade CreateCourierInteractionFacade(
        LegacyInteractionPipelinePorts ports,
        ILlmGateway gateway,
        Func<string, InteractionEnvelope> capture)
    {
        return new LegacyChannelInteractionFacade(ports, gateway, SaveRuntimeGuard.CaptureGeneration, capture);
    }

    /// <summary>
    /// Captures the currently selected Native conversation target and its
    /// existing MyBehavior memory. The returned envelope contains no Hero,
    /// CharacterObject, Campaign, or other live game object.
    /// </summary>
    public static InteractionEnvelope CaptureNativeConversation(string playerText)
    {
        return CaptureNativeConversation(playerText, null);
    }

    /// <summary>
    /// Captures Native state plus prompt fragments already assembled by the
    /// main-thread/channel owner. Fragments are copied into the envelope; no
    /// live game object crosses into the detached composer.
    /// </summary>
    public static InteractionEnvelope CaptureNativeConversation(
        string playerText,
        DetachedPromptSections promptSections)
    {
        return CaptureNativeConversation(playerText, promptSections, null);
    }

    /// <summary>
    /// Captures Native state with atomic main/postprocess detached sections.
    /// </summary>
    public static InteractionEnvelope CaptureNativeConversation(
        string playerText,
        DetachedPromptSections promptSections,
        DetachedPostprocessPromptSections postprocessPromptSections)
    {
        Hero targetHero = null;
        string targetName = string.Empty;
        string memoryId = string.Empty;
        try
        {
            ShoutBehavior.TryGetNativeConversationPersistentHistoryTargetForExternal(
                out targetHero,
                out targetName,
                out memoryId);
        }
        catch
        {
            // A conversation can close between the UI event and this capture.
        }

        string subjectId = FirstNonEmpty(memoryId, targetHero?.StringId, "native:unknown");
        List<ConversationMessage> history = targetHero == null
            ? new List<ConversationMessage>()
            : MyBehavior.BuildUncompressedMemoryRoleMessagesForExternal(
                targetHero,
                targetAgentIndex: -1,
                includeCurrentActiveSceneSession: true);

        Dictionary<string, string> facts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["target_name"] = targetName ?? string.Empty,
            ["memory_id"] = subjectId,
            ["rule_runtime_context"] = FirstNonEmpty(targetName, CurrentLocationId(), "native_conversation"),
            ["excluded_rule_ids"] = string.Empty
        };
        return CreateEnvelope(
            InteractionChannel.NativeConversation,
            subjectId,
            playerText,
            CurrentLocationId(),
            history,
            null,
            facts,
            null,
            promptSections,
            postprocessPromptSections);
    }

    /// <summary>
    /// Captures one scene target using the existing Agent index. Agent lookup
    /// is intentionally performed only at an interaction boundary, never from
    /// a tick or background worker.
    /// </summary>
    public static InteractionEnvelope CaptureSceneShout(string playerText, int targetAgentIndex)
    {
        return CaptureSceneShout(playerText, targetAgentIndex, null, null);
    }

    /// <summary>
    /// SceneShout capture overload for the shared detached Prompt composer.
    /// Target resolution still happens only at this interaction boundary.
    /// </summary>
    public static InteractionEnvelope CaptureSceneShout(
        string playerText,
        int targetAgentIndex,
        DetachedPromptSections promptSections)
    {
        return CaptureSceneShout(playerText, targetAgentIndex, promptSections, null);
    }

    /// <summary>
    /// Captures the main and postprocess sections atomically for one SceneShout
    /// turn. Both providers are evaluated by the channel owner at the same
    /// interaction boundary; the detached worker receives only copied strings.
    /// </summary>
    public static InteractionEnvelope CaptureSceneShout(
        string playerText,
        int targetAgentIndex,
        DetachedPromptSections promptSections,
        DetachedPostprocessPromptSections postprocessPromptSections)
    {
        List<InteractionCandidate> candidates = new List<InteractionCandidate>();
        Hero targetHero = null;
        CharacterObject targetCharacter = null;
        NpcDataPacket targetNpc = null;
        string nonHeroMemoryId = string.Empty;
        string nonHeroMemoryName = string.Empty;
        try
        {
            Agent agent = Mission.Current?.Agents?.FirstOrDefault(x => x != null && x.Index == targetAgentIndex);
            if (agent != null)
            {
                targetCharacter = agent.Character as CharacterObject;
                targetHero = targetCharacter?.HeroObject;
                targetNpc = ShoutUtils.ExtractNpcData(agent);
                string stableId = FirstNonEmpty(targetHero?.StringId, targetCharacter?.StringId, "agent:" + targetAgentIndex);
                string displayName = targetCharacter?.Name?.ToString() ?? string.Empty;
                candidates.Add(new InteractionCandidate(stableId, displayName, targetAgentIndex, agent.IsActive()));
                if (targetHero == null)
                {
                    ShoutBehavior.TryResolveWildernessNonHeroMemoryForExternal(
                        targetNpc,
                        null,
                        targetCharacter,
                        targetAgentIndex,
                        out nonHeroMemoryId,
                        out nonHeroMemoryName);
                }
            }
        }
        catch
        {
            // A mission may be unloading while an input event is dispatched.
        }

        List<ConversationMessage> history = new List<ConversationMessage>();
        try
        {
            if (targetHero != null)
            {
                history = MyBehavior.BuildUncompressedMemoryRoleMessagesForExternal(
                    targetHero,
                    targetAgentIndex,
                    includeCurrentActiveSceneSession: true)
                    ?? new List<ConversationMessage>();
            }
            else if (!string.IsNullOrWhiteSpace(nonHeroMemoryId))
            {
                history = MyBehavior.BuildNonHeroUncompressedMemoryRoleMessagesForExternal(
                    nonHeroMemoryId,
                    string.IsNullOrWhiteSpace(nonHeroMemoryName) ? targetCharacter?.Name?.ToString() : nonHeroMemoryName,
                    targetAgentIndex,
                    includeCurrentActiveSceneSession: true)
                    ?? new List<ConversationMessage>();
            }
        }
        catch
        {
            // Memory capture is best effort; the immutable envelope remains
            // valid and the detached path can proceed with an empty history.
        }

        Dictionary<string, string> facts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["scene_session_id"] = ShoutBehavior.GetCurrentSceneHistorySessionIdForExternal().ToString(),
            ["target_agent_index"] = targetAgentIndex.ToString(),
            ["memory_id"] = FirstNonEmpty(targetHero?.StringId, nonHeroMemoryId, "agent:" + targetAgentIndex),
            ["memory_kind"] = targetHero != null ? "hero" : (string.IsNullOrWhiteSpace(nonHeroMemoryId) ? "unresolved" : "nonhero")
        };
        return CreateEnvelope(
            InteractionChannel.SceneShout,
            FirstNonEmpty(targetHero?.StringId, nonHeroMemoryId, "agent:" + targetAgentIndex),
            playerText,
            CurrentLocationId(),
            history,
            candidates,
            facts,
            null,
            promptSections,
            postprocessPromptSections);
    }

    /// <summary>
    /// Captures the courier prompt boundary while preserving CourierDelivery's
    /// existing delivery/reply state machine. The letter itself is supplied by
    /// the existing flow; this adapter does not inspect or mutate a session.
    /// </summary>
    public static InteractionEnvelope CaptureCourier(
        Hero recipient,
        string letterText,
        string sessionId = null,
        string deliveryFact = null)
    {
        return CaptureCourier(recipient, letterText, sessionId, deliveryFact, null);
    }

    /// <summary>
    /// Courier capture overload for the shared detached Prompt composer.
    /// Delivery and return timing remain owned by CourierDeliveryBehavior.
    /// </summary>
    public static InteractionEnvelope CaptureCourier(
        Hero recipient,
        string letterText,
        string sessionId,
        string deliveryFact,
        DetachedPromptSections promptSections)
    {
        string subjectId = FirstNonEmpty(recipient?.StringId, "courier:unknown");
        List<ConversationMessage> history = recipient == null
            ? new List<ConversationMessage>()
            : MyBehavior.BuildUncompressedMemoryRoleMessagesForExternal(
                recipient,
                targetAgentIndex: -1,
                includeCurrentActiveSceneSession: false);

        bool hasActiveCourier = false;
        try
        {
            hasActiveCourier = recipient != null && CourierDeliveryBehavior.HasActiveCourierForHeroForExternal(recipient);
        }
        catch
        {
        }

        Dictionary<string, string> facts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["recipient_name"] = recipient?.Name?.ToString() ?? string.Empty,
            ["delivery_fact"] = deliveryFact ?? string.Empty,
            ["has_active_courier"] = hasActiveCourier ? "true" : "false"
        };
        return CreateEnvelope(
            InteractionChannel.Courier,
            subjectId,
            letterText,
            CurrentLocationId(recipient),
            history,
            null,
            facts,
            sessionId,
            promptSections);
    }

    /// <summary>
    /// Captures a Courier turn from the legacy message list already assembled
    /// by CourierDeliveryBehavior. The first system message becomes the
    /// detached system section and every remaining role/content message is
    /// copied into the immutable envelope history, preserving the exact
    /// legacy message order without carrying legacy objects across threads.
    /// </summary>
    public static InteractionEnvelope CaptureCourierFromPromptPackage(
        Hero recipient,
        string letterText,
        string sessionId,
        string deliveryFact,
        PromptPackage promptPackage,
        DetachedPostprocessPromptSections postprocessPromptSections = null,
        IDictionary<string, string> detachedFacts = null)
    {
        List<PromptMessage> promptHistory = new List<PromptMessage>();
        string system = string.Empty;
        if (promptPackage != null)
        {
            for (int i = 0; i < promptPackage.Messages.Count; i++)
            {
                PromptMessage message = promptPackage.Messages[i];
                if (i == 0 && string.Equals(message?.Role, "system", StringComparison.OrdinalIgnoreCase))
                {
                    system = message.Content ?? string.Empty;
                    continue;
                }
                if (message != null && !string.IsNullOrWhiteSpace(message.Content))
                {
                    promptHistory.Add(new PromptMessage(message.Role, message.Content));
                }
            }
        }
        Dictionary<string, string> facts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["recipient_name"] = recipient?.Name?.ToString() ?? string.Empty,
            ["delivery_fact"] = deliveryFact ?? string.Empty,
            ["has_active_courier"] = "true",
            ["courier_prompt_model"] = promptPackage?.Model ?? string.Empty,
            ["courier_selected_rule_ids"] = string.Empty
        };
        if (detachedFacts != null)
        {
            foreach (KeyValuePair<string, string> pair in detachedFacts)
            {
                if (!string.IsNullOrWhiteSpace(pair.Key))
                {
                    facts[pair.Key.Trim()] = pair.Value ?? string.Empty;
                }
            }
        }
        DetachedPromptSections main = new DetachedPromptSections(
            new[] { system },
            Array.Empty<string>(),
            Array.Empty<string>(),
            appendCurrentPlayerInput: false);
        List<ConversationMessage> history = promptHistory
            .Select(message => new ConversationMessage
            {
                Role = message.Role,
                Content = message.Content
            })
            .ToList();
        return CreateEnvelope(
            InteractionChannel.Courier,
            FirstNonEmpty(recipient?.StringId, "courier:unknown"),
            letterText,
            CurrentLocationId(recipient),
            history,
            null,
            facts,
            sessionId,
            main,
            postprocessPromptSections);
    }

    private static InteractionEnvelope CreateEnvelope(
        InteractionChannel channel,
        string subjectId,
        string playerText,
        string locationId,
        IEnumerable<ConversationMessage> history,
        IEnumerable<InteractionCandidate> candidates,
        IDictionary<string, string> facts,
        string sessionId = null,
        DetachedPromptSections promptSections = null,
        DetachedPostprocessPromptSections postprocessPromptSections = null)
    {
        string normalizedSubjectId = FirstNonEmpty(subjectId, channel.ToString().ToLowerInvariant() + ":unknown");
        string normalizedSessionId = FirstNonEmpty(
            sessionId,
            "af-" + channel.ToString().ToLowerInvariant() + "-" + Interlocked.Increment(ref _sessionSequence));
        string apiLine;
#if BANNERLORD_1_4_OR_GREATER
        apiLine = "1.4";
#else
        apiLine = "1.3";
#endif
        long generation = SaveRuntimeGuard.CurrentGeneration;
        TraceContext trace = new TraceContext(
            "af-trace-" + normalizedSessionId,
            generation,
            generation,
            "default",
            apiLine);
        InteractionIdentity identity = new InteractionIdentity(normalizedSessionId, channel, normalizedSubjectId);
        GameInteractionSnapshot snapshot = new GameInteractionSnapshot(
            identity,
            trace,
            playerText,
            locationId,
            CurrentDay(),
            MyBehavior.GetCurrentMemoryGameHourForExternal(),
            candidates,
            ExtractVisibleHeroIds(candidates),
            facts);
        return new InteractionEnvelope(snapshot, CopyHistory(history), promptSections, postprocessPromptSections);
    }

    private static IReadOnlyList<PromptMessage> CopyHistory(IEnumerable<ConversationMessage> history)
    {
        List<PromptMessage> result = new List<PromptMessage>();
        foreach (ConversationMessage message in history ?? Enumerable.Empty<ConversationMessage>())
        {
            if (message == null || string.IsNullOrWhiteSpace(message.Content))
            {
                continue;
            }

            string role = (message.Role ?? string.Empty).Trim().ToLowerInvariant();
            if (role != "system" && role != "assistant" && role != "user")
            {
                role = "user";
            }
            result.Add(new PromptMessage(role, message.Content));
        }
        return result.AsReadOnly();
    }

    private static IReadOnlyList<string> ExtractVisibleHeroIds(IEnumerable<InteractionCandidate> candidates)
    {
        return (candidates ?? Enumerable.Empty<InteractionCandidate>())
            .Where(x => x != null && !string.IsNullOrWhiteSpace(x.StableId) && x.StableId.IndexOf("agent:", StringComparison.OrdinalIgnoreCase) != 0)
            .Select(x => x.StableId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList()
            .AsReadOnly();
    }

    private static string CurrentLocationId(Hero perspectiveHero = null)
    {
        try
        {
            return FirstNonEmpty(
                Mission.Current?.SceneName,
                perspectiveHero?.CurrentSettlement?.StringId,
                Settlement.CurrentSettlement?.StringId,
                MobileParty.MainParty?.CurrentSettlement?.StringId,
                "worldmap");
        }
        catch
        {
            return "unknown";
        }
    }

    private static int CurrentDay()
    {
        try
        {
            return Math.Max(0, (int)Math.Floor(CampaignTime.Now.ToDays));
        }
        catch
        {
            return 0;
        }
    }

    private static string FirstNonEmpty(params string[] values)
    {
        foreach (string value in values ?? Array.Empty<string>())
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }
        return string.Empty;
    }
}

/// <summary>
/// Main-thread facade for the existing MyBehavior dialogue/AFEF boundary.
/// It stores only stable identity and is intended to be used at the commit
/// boundary; no live Hero crosses into detached generation.
/// </summary>
public sealed class MyBehaviorMemoryFacade : IInteractionMemory, IInteractionMemoryBatchCommitter
{
    private readonly string _heroId;
    private readonly string _nonHeroMemoryId;
    private readonly string _nonHeroName;

    public MyBehaviorMemoryFacade(Hero hero)
    {
        if (hero == null || string.IsNullOrWhiteSpace(hero.StringId))
        {
            throw new ArgumentException("A stable hero id is required.", nameof(hero));
        }
        _heroId = hero.StringId.Trim();
    }

    public MyBehaviorMemoryFacade(string nonHeroMemoryId, string nonHeroName)
    {
        if (string.IsNullOrWhiteSpace(nonHeroMemoryId))
        {
            throw new ArgumentException("Memory id is required.", nameof(nonHeroMemoryId));
        }
        _nonHeroMemoryId = nonHeroMemoryId.Trim();
        _nonHeroName = string.IsNullOrWhiteSpace(nonHeroName) ? "NPC" : nonHeroName.Trim();
    }

    public IReadOnlyList<PromptMessage> Read(string subjectId, int maxItems)
    {
        Hero hero = ResolveHeroOnInteractionBoundary();
        IEnumerable<ConversationMessage> messages;
        if (!string.IsNullOrWhiteSpace(_heroId))
        {
            messages = hero == null
                ? Enumerable.Empty<ConversationMessage>()
                : MyBehavior.BuildUncompressedMemoryRoleMessagesForExternal(hero, -1, false);
        }
        else
        {
            messages = MyBehavior.BuildNonHeroUncompressedMemoryRoleMessagesForExternal(_nonHeroMemoryId, _nonHeroName, -1, false);
        }
        int limit = Math.Max(0, maxItems);
        return CopyAndLimit(messages, limit);
    }

    public void Append(string subjectId, PromptMessage message, IEnumerable<FactRecord> confirmedFacts)
    {
        if (message == null || string.IsNullOrWhiteSpace(message.Content))
        {
            return;
        }

        string role = (message.Role ?? string.Empty).Trim();
        string playerText = role.Equals("assistant", StringComparison.OrdinalIgnoreCase) ? null : message.Content;
        string aiText = role.Equals("assistant", StringComparison.OrdinalIgnoreCase) ? message.Content : null;
        string fact = string.Join("\n", (confirmedFacts ?? Enumerable.Empty<FactRecord>())
            .Where(x => x != null && !string.IsNullOrWhiteSpace(x.Text))
            .Select(x => x.Text.Trim()));

        Hero hero = ResolveHeroOnInteractionBoundary();
        if (!string.IsNullOrWhiteSpace(_heroId) && hero != null)
        {
            MyBehavior.AppendExternalDialogueHistory(hero, playerText, aiText, EmptyToNull(fact));
        }
        else if (!string.IsNullOrWhiteSpace(_nonHeroMemoryId))
        {
            MyBehavior.AppendExternalNonHeroDialogueHistory(_nonHeroMemoryId, _nonHeroName, playerText, aiText, EmptyToNull(fact));
        }
    }

    /// <summary>
    /// Commits one detached interaction through the existing MyBehavior entry
    /// point. This keeps the legacy history/AFEF storage, keys and types as
    /// the sole persistence authority while making the three-channel write
    /// atomic at the facade boundary and idempotent for a repeated callback.
    /// </summary>
    public MemoryCommitResult Commit(InteractionMemoryCommit commit)
    {
        if (commit == null)
        {
            return new MemoryCommitResult(MemoryCommitStatus.Rejected, "missing_memory_commit");
        }

        string expectedSubjectId = !string.IsNullOrWhiteSpace(_heroId) ? _heroId : _nonHeroMemoryId;
        if (!string.Equals(expectedSubjectId, commit.SubjectId, StringComparison.OrdinalIgnoreCase))
        {
            return new MemoryCommitResult(MemoryCommitStatus.Rejected, "memory_subject_mismatch");
        }

        Hero hero = ResolveHeroOnInteractionBoundary();
        if (!string.IsNullOrWhiteSpace(_heroId))
        {
            if (hero == null)
            {
                return new MemoryCommitResult(MemoryCommitStatus.Rejected, "memory_target_missing");
            }
        }
        if (MemoryCommitReceiptCache.Contains(commit.CommitId))
        {
            return new MemoryCommitResult(MemoryCommitStatus.Duplicate);
        }

        string facts = string.Join("\n", (commit.ConfirmedFacts ?? Array.Empty<FactRecord>())
            .Where(x => x != null && !string.IsNullOrWhiteSpace(x.Text))
            .Select(x => x.Text.Trim()));
        string userText = EmptyToNull(commit.UserText);
        string assistantText = EmptyToNull(commit.AssistantText);
        try
        {
            if (!string.IsNullOrWhiteSpace(_heroId))
            {
                MyBehavior.AppendExternalDialogueHistory(hero, userText, assistantText, EmptyToNull(facts));
            }
            else
            {
                MyBehavior.AppendExternalNonHeroDialogueHistory(_nonHeroMemoryId, _nonHeroName, userText, assistantText, EmptyToNull(facts));
            }
        }
        catch
        {
            // Do not retain a receipt when the legacy persistence owner did
            // not accept the write. The caller may retry the same commit.
            return new MemoryCommitResult(MemoryCommitStatus.Failed, "legacy_memory_append_failed");
        }
        MemoryCommitReceiptCache.TryAccept(commit.CommitId);
        return new MemoryCommitResult(MemoryCommitStatus.Applied);
    }

    /// <summary>
    /// Memory read/write is an interaction-boundary operation, not a tick
    /// operation. Resolve by stable id only when the caller is already on the
    /// game thread; no live Hero is retained across an async request.
    /// </summary>
    private Hero ResolveHeroOnInteractionBoundary()
    {
        if (string.IsNullOrWhiteSpace(_heroId))
        {
            return null;
        }
        try
        {
            return Hero.Find(_heroId)
                ?? Hero.FindFirst(x => x != null && string.Equals((x.StringId ?? "").Trim(), _heroId, StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return null;
        }
    }

    private IReadOnlyList<PromptMessage> CopyAndLimit(IEnumerable<ConversationMessage> messages, int maxItems)
    {
        List<PromptMessage> copy = new List<PromptMessage>();
        foreach (ConversationMessage message in messages ?? Enumerable.Empty<ConversationMessage>())
        {
            if (message == null || string.IsNullOrWhiteSpace(message.Content))
            {
                continue;
            }
            string role = (message.Role ?? "user").Trim().ToLowerInvariant();
            if (role != "system" && role != "assistant" && role != "user")
            {
                role = "user";
            }
            copy.Add(new PromptMessage(role, message.Content));
        }
        if (maxItems <= 0 || copy.Count <= maxItems)
        {
            return copy.AsReadOnly();
        }
        return copy.Skip(copy.Count - maxItems).ToList().AsReadOnly();
    }

    private static string EmptyToNull(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
