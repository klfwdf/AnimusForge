using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using NativeDetachedPostprocessSource = AnimusForge.ShoutBehavior.NativeDetachedPostprocessSource;
using NativeDetachedPostprocessCompletion = AnimusForge.ShoutBehavior.NativeDetachedPostprocessCompletion;

namespace AnimusForge;
internal delegate bool NativePostprocessHistoryTarget(out Hero hero,out string targetName,out string memoryId);
internal sealed class NativeDetachedPostprocessApplicationAdapter
{
    private static readonly ConditionalWeakTable<InteractionEnvelope,NativeDetachedPostprocessSource> NativeDetachedPostprocessSources=new();
    private readonly Func<bool> _isCurrentOwner;
    private readonly NativeAdmissionTargetResolver _resolveTarget;
    private readonly Func<Hero,CharacterObject,int> _resolveAgent;
    private readonly NativeAdmissionTargetGuard _targetAvailable;
    private readonly NativePostprocessHistoryTarget _resolveHistory;
        private readonly ConversationGameThreadDispatcher _dispatcher;
    private readonly Func<Hero,CharacterObject,int,InteractionEnvelope,RuleSelection,string,SceneActionPostprocessWorkItem> _prepareWork;
    private readonly Func<SceneActionPostprocessWorkItem,string,string> _completeWork;
    internal NativeDetachedPostprocessApplicationAdapter(Func<bool> isCurrentOwner,NativeAdmissionTargetResolver resolveTarget,
        Func<Hero,CharacterObject,int> resolveAgent,NativeAdmissionTargetGuard targetAvailable,
        NativePostprocessHistoryTarget resolveHistory,ConversationGameThreadDispatcher dispatcher,
        Func<Hero,CharacterObject,int,InteractionEnvelope,RuleSelection,string,SceneActionPostprocessWorkItem> prepareWork,
        Func<SceneActionPostprocessWorkItem,string,string> completeWork)
    { _isCurrentOwner=isCurrentOwner;_resolveTarget=resolveTarget;_resolveAgent=resolveAgent;_targetAvailable=targetAvailable;
      _resolveHistory=resolveHistory;_dispatcher=dispatcher;_prepareWork=prepareWork;_completeWork=completeWork; }
internal SceneActionPostprocessWorkItem CaptureWork(Hero hero, CharacterObject character, int agentIndex, InteractionEnvelope envelope, RuleSelection selection, string reply) => _prepareWork(hero, character, agentIndex, envelope, selection, reply);
internal InteractionEnvelope BindNativeDetachedPostprocessSource(InteractionEnvelope envelope)
    {
        ConversationActionPostprocessOwner.RequireMainThread();
        NativeDetachedPostprocessApplicationAdapter behavior = this;
        if (envelope == null || behavior == null
            || !_resolveTarget(out Hero hero, out CharacterObject character, out _))
            return envelope;
        NativeDetachedPostprocessSources.Add(envelope, new NativeDetachedPostprocessSource
        {
            Application = behavior,
            Manager = Campaign.Current?.ConversationManager,
            Token = Campaign.Current?.ConversationManager?.ActiveToken ?? int.MinValue,
            Hero = hero,
            Character = character,
            AgentIndex = _resolveAgent(hero, character)
        });
        return envelope;
    }
internal bool IsNativeDetachedPostprocessCurrent(
        NativeDetachedPostprocessSource source, InteractionEnvelope envelope)
    {
        ConversationActionPostprocessOwner.RequireMainThread();
        if (source == null || envelope?.Snapshot?.Identity == null
            || envelope.Snapshot.Identity.Channel != InteractionChannel.NativeConversation
            || !source.Application._isCurrentOwner()
            || !SaveRuntimeGuard.IsCurrentGeneration(envelope.Snapshot.Trace.RuntimeGeneration)
            || source.Manager == null || !ReferenceEquals(Campaign.Current?.ConversationManager, source.Manager)
            || source.Manager.ActiveToken != source.Token
            || !envelope.Snapshot.DetachedFacts.TryGetValue("native_conversation_token", out string token)
            || !int.TryParse(token, out int parsedToken) || parsedToken != source.Token
            || !_resolveTarget(out Hero hero, out CharacterObject character, out _)
            || !ReferenceEquals(hero, source.Hero) || !ReferenceEquals(character, source.Character))
            return false;
        if (!_resolveHistory(out Hero persistentHero, out _, out string memoryId))
            return false;
        string subject = !string.IsNullOrWhiteSpace(memoryId) ? memoryId.Trim() : persistentHero?.StringId;
        return string.Equals(subject, envelope.Snapshot.Identity.SubjectId, StringComparison.Ordinal)
            && _targetAvailable(
                source.AgentIndex, source.Hero, source.Character, out _);
    }
internal static LegacyInteractionPipelinePorts CreateNativeConversationDetachedPortsCore(
        IEnumerable<string> allowedTagFamilies, int topN, int maxActions, Func<int,LegacyDetachedRuleSelector> selectorFactory)
    {
        List<string> families = (allowedTagFamilies ?? Enumerable.Empty<string>())
            .Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var selector = selectorFactory(topN);
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
internal static async Task<PromptPackage> PrepareNativeDetachedPostprocessAsync(
        InteractionEnvelope envelope, RuleSelection selection, string rawReply, PostprocessContext context,
        ConditionalWeakTable<PostprocessContext, NativeDetachedPostprocessCompletion> owners,
        CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        if (envelope == null || context == null
            || !NativeDetachedPostprocessSources.TryGetValue(envelope, out var source))
            return null;
        NativeDetachedPostprocessCompletion owner = await source.Application._dispatcher.RunAsync(
            "native_detached_postprocess_prepare", envelope.Snapshot.Identity.SubjectId, source.AgentIndex, () =>
            {
                if (cancellation.IsCancellationRequested || !source.Application.IsNativeDetachedPostprocessCurrent(source, envelope)) return null;
                SceneActionPostprocessWorkItem work = source.Application._prepareWork(source.Hero,source.Character,source.AgentIndex,envelope,selection,rawReply);
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
internal static async Task<ActionPlan> CompleteNativeDetachedPostprocessAsync(
        string raw, PostprocessContext context,
        ConditionalWeakTable<PostprocessContext, NativeDetachedPostprocessCompletion> owners,
        LegacyActionTagParser parser, CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        if (context == null || !owners.TryGetValue(context, out var owner) || !owners.Remove(context))
            return new ActionPlan(Array.Empty<ActionRequest>(), string.Empty);
        return await owner.Source.Application._dispatcher.RunAsync(
            "native_detached_postprocess_complete", owner.Envelope.Snapshot.Identity.SubjectId, owner.Source.AgentIndex, () =>
            {
                if (cancellation.IsCancellationRequested || !owner.Source.Application.IsNativeDetachedPostprocessCurrent(owner.Source, owner.Envelope))
                    return new ActionPlan(Array.Empty<ActionRequest>(), string.Empty);
                string normalized = owner.Source.Application._completeWork(owner.Work,raw);
                return parser.Parse(normalized, context);
            }, new ActionPlan(Array.Empty<ActionRequest>(), string.Empty)).ConfigureAwait(false);
    }
}
