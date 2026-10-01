using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Runtime;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public partial class ShoutBehavior
{
    private sealed class NativeDetachedPostprocessSource
    {
        internal ShoutBehavior Behavior;
        internal ConversationManager Manager;
        internal int Token;
        internal Hero Hero;
        internal CharacterObject Character;
        internal int AgentIndex;
    }

    private sealed class NativeDetachedPostprocessCompletion
    {
        internal NativeDetachedPostprocessSource Source;
        internal InteractionEnvelope Envelope;
        internal SceneActionPostprocessWorkItem Work;
        internal PostprocessNetworkRequest Network;
    }

    private static readonly ConditionalWeakTable<InteractionEnvelope, NativeDetachedPostprocessSource>
        NativeDetachedPostprocessSources = new();

    // Capture occurs once at the existing snapshot boundary. Weak keys do not prolong a session.
    internal static InteractionEnvelope BindNativeDetachedPostprocessSource(InteractionEnvelope envelope)
    {
        ConversationActionPostprocessOwner.RequireMainThread();
        ShoutBehavior behavior = CurrentInstance;
        if (envelope == null || behavior == null
            || !TryResolveNativeConversationTarget(out Hero hero, out CharacterObject character, out _))
            return envelope;
        NativeDetachedPostprocessSources.Add(envelope, new NativeDetachedPostprocessSource
        {
            Behavior = behavior,
            Manager = Campaign.Current?.ConversationManager,
            Token = Campaign.Current?.ConversationManager?.ActiveToken ?? int.MinValue,
            Hero = hero,
            Character = character,
            AgentIndex = TryResolveNativeConversationAgentIndex(hero, character)
        });
        return envelope;
    }

    private static bool IsNativeDetachedPostprocessCurrent(
        NativeDetachedPostprocessSource source, InteractionEnvelope envelope)
    {
        ConversationActionPostprocessOwner.RequireMainThread();
        if (source == null || envelope?.Snapshot?.Identity == null
            || envelope.Snapshot.Identity.Channel != InteractionChannel.NativeConversation
            || !ReferenceEquals(CurrentInstance, source.Behavior)
            || !SaveRuntimeGuard.IsCurrentGeneration(envelope.Snapshot.Trace.RuntimeGeneration)
            || source.Manager == null || !ReferenceEquals(Campaign.Current?.ConversationManager, source.Manager)
            || source.Manager.ActiveToken != source.Token
            || !envelope.Snapshot.DetachedFacts.TryGetValue("native_conversation_token", out string token)
            || !int.TryParse(token, out int parsedToken) || parsedToken != source.Token
            || !TryResolveNativeConversationTarget(out Hero hero, out CharacterObject character, out _)
            || !ReferenceEquals(hero, source.Hero) || !ReferenceEquals(character, source.Character))
            return false;
        if (!TryGetNativeConversationPersistentHistoryTargetForExternal(out Hero persistentHero, out _, out string memoryId))
            return false;
        string subject = !string.IsNullOrWhiteSpace(memoryId) ? memoryId.Trim() : persistentHero?.StringId;
        return string.Equals(subject, envelope.Snapshot.Identity.SubjectId, StringComparison.Ordinal)
            && IsNativeConversationResponseTargetAvailableForActionDispatch(
                source.AgentIndex, source.Hero, source.Character, out _);
    }

    private static LegacyInteractionPipelinePorts CreateNativeConversationDetachedPortsCore(
        IEnumerable<string> allowedTagFamilies, int topN, int maxActions)
    {
        List<string> families = (allowedTagFamilies ?? Enumerable.Empty<string>())
            .Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var selector = CreateNativeConversationDetachedRuleSelectorForExternal(topN);
        var composer = new LegacyDetachedPromptComposer(model: "legacy-native");
        var parser = new LegacyActionTagParser(maxActions);
        var owners = new ConditionalWeakTable<PostprocessContext, NativeDetachedPostprocessCompletion>();
        var capabilities = new CapabilitySet(new[] { "llm.generate", "rule.select", "prompt.compose", "postprocess.compose", "action.parse" });
        return new LegacyInteractionPipelinePorts(
            snapshot =>
            {
                RuleSelection selection = selector.Select(snapshot);
                return selection != null && selection.RuleIds.Count > 0 ? selection
                    : new RuleSelection(new[] { "native_conversation" }, selection?.ExclusionReasons);
            },
            (envelope, selection, available) => composer.Compose(envelope, selection, available),
            (snapshot, selection, available) => new PostprocessContext(selection?.RuleIds, families, available),
            // No unqualified synchronous raw-parser escape hatch.
            (raw, context) => new ActionPlan(Array.Empty<ActionRequest>(), string.Empty),
            (raw, internalFamilies) => LlmVisibleReplyNormalizer.NormalizeComplete(raw),
            capabilities, null,
            (envelope, selection, visible, raw, context, cancellation) =>
                PrepareNativeDetachedPostprocessAsync(envelope, selection, raw, context, owners, cancellation),
            (raw, context, cancellation) => CompleteNativeDetachedPostprocessAsync(raw, context, owners, parser, cancellation),
            (context, cancellation) => CompleteNativeDetachedPostprocessAsync(null, context, owners, parser, cancellation));
    }

    private static async Task<PromptPackage> PrepareNativeDetachedPostprocessAsync(
        InteractionEnvelope envelope, RuleSelection selection, string rawReply, PostprocessContext context,
        ConditionalWeakTable<PostprocessContext, NativeDetachedPostprocessCompletion> owners,
        CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        if (envelope == null || context == null
            || !NativeDetachedPostprocessSources.TryGetValue(envelope, out var source))
            return null;
        NativeDetachedPostprocessCompletion owner = await source.Behavior.RunNativeConversationMainThreadFuncAsync(
            "native_detached_postprocess_prepare", envelope.Snapshot.Identity.SubjectId, source.AgentIndex, () =>
            {
                if (cancellation.IsCancellationRequested || !IsNativeDetachedPostprocessCurrent(source, envelope)) return null;
                SceneActionPostprocessWorkItem work = PrepareNativeDetachedPostprocessWork(source, envelope, selection, rawReply);
                return work == null ? null : new NativeDetachedPostprocessCompletion
                { Source = source, Envelope = envelope, Work = work, Network = work.NetworkRequest };
            }, (NativeDetachedPostprocessCompletion)null).ConfigureAwait(false);
        cancellation.ThrowIfCancellationRequested();
        if (owner == null) return null;
        owners.Add(context, owner);
        if (!owner.Work.RequiresNetwork) return null;
        PostprocessNetworkRequest network = owner.Network;
        return new PromptPackage(new[]
        {
            new PromptMessage("system", network.SystemPrompt),
            new PromptMessage("user", network.UserPrompt)
        }, 5000, "legacy-native-postprocess");
    }

    private static async Task<ActionPlan> CompleteNativeDetachedPostprocessAsync(
        string raw, PostprocessContext context,
        ConditionalWeakTable<PostprocessContext, NativeDetachedPostprocessCompletion> owners,
        LegacyActionTagParser parser, CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        if (context == null || !owners.TryGetValue(context, out var owner) || !owners.Remove(context))
            return new ActionPlan(Array.Empty<ActionRequest>(), string.Empty);
        return await owner.Source.Behavior.RunNativeConversationMainThreadFuncAsync(
            "native_detached_postprocess_complete", owner.Envelope.Snapshot.Identity.SubjectId, owner.Source.AgentIndex, () =>
            {
                if (cancellation.IsCancellationRequested || !IsNativeDetachedPostprocessCurrent(owner.Source, owner.Envelope))
                    return new ActionPlan(Array.Empty<ActionRequest>(), string.Empty);
                string normalized = CompleteSceneUnifiedActionPostprocess(owner.Work, true, raw, null);
                return parser.Parse(normalized, context);
            }, new ActionPlan(Array.Empty<ActionRequest>(), string.Empty)).ConfigureAwait(false);
    }

    private static SceneActionPostprocessWorkItem PrepareNativeDetachedPostprocessWork(
        NativeDetachedPostprocessSource source, InteractionEnvelope envelope, RuleSelection selection, string reply)
    {
        var hits = (selection?.RuleIds ?? Array.Empty<string>()).ToList();
        bool Hit(string id) => HasPreprocessRuleHit(hits, id);
        var npc = BuildNativeConversationNpcData(source.Hero, source.Character);
        npc.AgentIndex = source.AgentIndex;
        var targets = new List<NpcDataPacket> { npc };
        var heroes = new Dictionary<int, Hero>();
        if (source.AgentIndex >= 0 && source.Hero != null) heroes[source.AgentIndex] = source.Hero;
        var summon = source.AgentIndex >= 0 ? source.Behavior.BuildSceneSummonPromptTargets(targets, heroes) : null;
        int guideStart = (summon != null && summon.Count > 0 ? summon.Max(item => item?.PromptId ?? 0) : 0) + 1;
        Agent agent = source.AgentIndex >= 0 ? Mission.Current?.Agents?.FirstOrDefault(item => item != null && item.Index == source.AgentIndex) : null;
        var guide = source.AgentIndex >= 0 ? source.Behavior.BuildSceneGuidePromptTargets(agent, guideStart) : null;
        bool mechanism = Hit("scene_mechanism_actions") && CanUseSceneMechanismPostprocessForSpeaker(source.AgentIndex);
        var mechanismRules = mechanism ? source.Behavior.BuildRuntimeSceneMechanismPostprocessRulesForScene(npc, summon, guide) : null;
        var stakes = Hit("duel") && source.Hero != null ? RewardSystemBehavior.Instance?.BuildDuelStakeOptionsForAI(source.Hero) : null;
        string history = string.Join("\n", envelope.History.Select(message => message.Role + ": " + message.Content));
        return PrepareSceneUnifiedActionPostprocess(
            source.Hero, source.Character, source.AgentIndex, GetSceneNpcHistoryNameForPrompt(npc),
            envelope.Snapshot.PlayerText, history, reply,
            duelRuleInjected: Hit("duel"), rewardRuleInjected: Hit("reward"), loanRuleInjected: Hit("loan"),
            kingdomServiceRuleInjected: Hit("kingdom_service"), kingdomVassalageRuleInjected: Hit("kingdom_vassalage"),
            kingdomAnnexationRuleInjected: false, lordsHallRuleInjected: Hit("lords_hall_access"),
            meetingReleaseRuleInjected: Hit("encounter_release_player"), vanillaIssueRuleInjected: Hit("vanilla_issue"),
            heroJoinPartyRuleInjected: Hit("kingdom_service"), sceneMechanismRuleInjected: mechanism,
            partyTransferRuleInjected: Hit("party_transfer"), voteDealRuleInjected: Hit("kingdom_agenda"),
            diplomacyRuleInjected: Hit("diplomacy"), worldMapPartyCommandRuleInjected: Hit("worldmap_party_command"),
            marriageRuleInjected: Hit("marriage"), duelStakeOptions: stakes, kingdomServiceRules: null,
            sceneMechanismRules: mechanismRules, sceneSummonTargets: summon, sceneGuideTargets: guide,
            siegeInterventionRuleInjected: AfGcczShoutBridge.ShouldRunPostprocessForActiveScene(),
            replyIsDirectPlayerResponse: !string.IsNullOrWhiteSpace(envelope.Snapshot.PlayerText),
            preprocessRuleHits: hits, chainName: "native_conversation", detachedMainPromptSections: envelope.PromptSections,
            offerSceneActionDirective: true);
    }
}
