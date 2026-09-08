using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Runtime;

namespace AnimusForge.Refactor.Adapters;

/// <summary>
/// Explicit ports for the existing AF rule/prompt/postprocess implementations.
/// The old channel owns these delegates; this type owns no module-private
/// state and can therefore be shared by SceneShout, NativeConversation and
/// Courier compositions.
/// </summary>
public sealed class LegacyInteractionPipelinePorts
{
    public LegacyInteractionPipelinePorts(
        Func<GameInteractionSnapshot, RuleSelection> selectRules,
        Func<InteractionEnvelope, RuleSelection, CapabilitySet, PromptPackage> composePrompt,
        Func<GameInteractionSnapshot, RuleSelection, CapabilitySet, PostprocessContext> buildPostprocessContext,
        Func<string, PostprocessContext, ActionPlan> parseActions,
        Func<string, IEnumerable<string>, string> normalizeVisibleReply,
        CapabilitySet capabilities,
        Func<InteractionEnvelope, RuleSelection, string, string, PostprocessContext, PromptPackage> composePostprocessPrompt = null)
        : this(selectRules, composePrompt, buildPostprocessContext, parseActions, normalizeVisibleReply,
            capabilities, composePostprocessPrompt, null, null)
    {
    }

    public LegacyInteractionPipelinePorts(
        Func<GameInteractionSnapshot, RuleSelection> selectRules,
        Func<InteractionEnvelope, RuleSelection, CapabilitySet, PromptPackage> composePrompt,
        Func<GameInteractionSnapshot, RuleSelection, CapabilitySet, PostprocessContext> buildPostprocessContext,
        Func<string, PostprocessContext, ActionPlan> parseActions,
        Func<string, IEnumerable<string>, string> normalizeVisibleReply,
        CapabilitySet capabilities,
        Func<InteractionEnvelope, RuleSelection, string, string, PostprocessContext, PromptPackage> composePostprocessPrompt,
        Func<InteractionEnvelope, RuleSelection, string, string, PostprocessContext, CancellationToken, Task<PromptPackage>> composePostprocessPromptAsync,
        Func<string, PostprocessContext, CancellationToken, Task<ActionPlan>> parseActionsAsync)
    {
        SelectRules = selectRules ?? throw new ArgumentNullException(nameof(selectRules));
        ComposePrompt = composePrompt ?? throw new ArgumentNullException(nameof(composePrompt));
        BuildPostprocessContext = buildPostprocessContext ?? throw new ArgumentNullException(nameof(buildPostprocessContext));
        ParseActions = parseActions ?? throw new ArgumentNullException(nameof(parseActions));
        NormalizeVisibleReply = normalizeVisibleReply ?? throw new ArgumentNullException(nameof(normalizeVisibleReply));
        Capabilities = capabilities ?? throw new ArgumentNullException(nameof(capabilities));
        ComposePostprocessPrompt = composePostprocessPrompt;
        ComposePostprocessPromptAsync = composePostprocessPromptAsync;
        ParseActionsAsync = parseActionsAsync;
        if (parseActionsAsync != null && composePostprocessPromptAsync == null)
        {
            throw new ArgumentException("An asynchronous action owner requires an asynchronous postprocess capture.", nameof(composePostprocessPromptAsync));
        }
    }

    public Func<GameInteractionSnapshot, RuleSelection> SelectRules { get; }
    public Func<InteractionEnvelope, RuleSelection, CapabilitySet, PromptPackage> ComposePrompt { get; }
    public Func<GameInteractionSnapshot, RuleSelection, CapabilitySet, PostprocessContext> BuildPostprocessContext { get; }
    public Func<string, PostprocessContext, ActionPlan> ParseActions { get; }
    public Func<string, IEnumerable<string>, string> NormalizeVisibleReply { get; }
    public CapabilitySet Capabilities { get; }
    public Func<InteractionEnvelope, RuleSelection, string, string, PostprocessContext, PromptPackage> ComposePostprocessPrompt { get; }
    public Func<InteractionEnvelope, RuleSelection, string, string, PostprocessContext, CancellationToken, Task<PromptPackage>> ComposePostprocessPromptAsync { get; }
    public Func<string, PostprocessContext, CancellationToken, Task<ActionPlan>> ParseActionsAsync { get; }
}

/// <summary>
/// Builds the same pipeline/coordinator for any channel. The caller supplies
/// the existing implementation ports, so this composition layer does not
/// invent a second rule set or a second action parser.
/// </summary>
public static class LegacyInteractionPipelineComposition
{
    public static InteractionRequestCoordinator Create(
        LegacyInteractionPipelinePorts ports,
        ILlmGateway gateway,
        Func<long> currentGeneration)
    {
        if (ports == null)
        {
            throw new ArgumentNullException(nameof(ports));
        }

        IInteractionPipeline pipeline;
        if (ports.ComposePostprocessPrompt == null && ports.ComposePostprocessPromptAsync == null)
        {
            pipeline = new InteractionPipeline(
                new DelegateRuleSelector(ports.SelectRules),
                new DelegatePromptComposer(ports.ComposePrompt),
                new DelegatePostprocessContextBuilder(ports.BuildPostprocessContext),
                gateway,
                new DelegateVisibleReplyNormalizer(ports.NormalizeVisibleReply),
                new DelegateActionPostprocessor(ports.ParseActions),
                ports.Capabilities);
        }
        else
        {
            pipeline = new FullInteractionPipeline(
                new DelegateRuleSelector(ports.SelectRules),
                new DelegatePromptComposer(ports.ComposePrompt),
                new DelegatePostprocessContextBuilder(ports.BuildPostprocessContext),
                new DelegatePostprocessPromptComposer(ports.ComposePostprocessPrompt, ports.ComposePostprocessPromptAsync),
                gateway,
                new DelegateVisibleReplyNormalizer(ports.NormalizeVisibleReply),
                new DelegateActionPostprocessor(ports.ParseActions, ports.ParseActionsAsync),
                ports.Capabilities);
        }
        return new InteractionRequestCoordinator(pipeline, currentGeneration);
    }

    private sealed class DelegateRuleSelector : IRuleSelector
    {
        private readonly Func<GameInteractionSnapshot, RuleSelection> _select;
        public DelegateRuleSelector(Func<GameInteractionSnapshot, RuleSelection> select) => _select = select;
        public RuleSelection Select(GameInteractionSnapshot snapshot) => _select(snapshot);
    }

    private sealed class DelegatePromptComposer : IPromptPackageComposer
    {
        private readonly Func<InteractionEnvelope, RuleSelection, CapabilitySet, PromptPackage> _compose;
        public DelegatePromptComposer(Func<InteractionEnvelope, RuleSelection, CapabilitySet, PromptPackage> compose) => _compose = compose;
        public PromptPackage Compose(InteractionEnvelope envelope, RuleSelection selection, CapabilitySet capabilities) => _compose(envelope, selection, capabilities);
    }

    private sealed class DelegatePostprocessContextBuilder : IPostprocessContextBuilder
    {
        private readonly Func<GameInteractionSnapshot, RuleSelection, CapabilitySet, PostprocessContext> _build;
        public DelegatePostprocessContextBuilder(Func<GameInteractionSnapshot, RuleSelection, CapabilitySet, PostprocessContext> build) => _build = build;
        public PostprocessContext Build(GameInteractionSnapshot snapshot, RuleSelection selection, CapabilitySet capabilities) => _build(snapshot, selection, capabilities);
    }

    private sealed class DelegateVisibleReplyNormalizer : IVisibleReplyNormalizer
    {
        private readonly Func<string, IEnumerable<string>, string> _normalize;
        public DelegateVisibleReplyNormalizer(Func<string, IEnumerable<string>, string> normalize) => _normalize = normalize;
        public string Normalize(string rawText, IEnumerable<string> internalTagFamilies) => _normalize(rawText, internalTagFamilies);
    }

    private sealed class DelegatePostprocessPromptComposer : IAsyncPostprocessPromptComposer
    {
        private readonly Func<InteractionEnvelope, RuleSelection, string, string, PostprocessContext, PromptPackage> _compose;
        private readonly Func<InteractionEnvelope, RuleSelection, string, string, PostprocessContext, CancellationToken, Task<PromptPackage>> _composeAsync;
        public DelegatePostprocessPromptComposer(
            Func<InteractionEnvelope, RuleSelection, string, string, PostprocessContext, PromptPackage> compose,
            Func<InteractionEnvelope, RuleSelection, string, string, PostprocessContext, CancellationToken, Task<PromptPackage>> composeAsync)
        {
            _compose = compose;
            _composeAsync = composeAsync;
        }
        public PromptPackage Compose(InteractionEnvelope envelope, RuleSelection selection, string visibleReply, string rawReply, PostprocessContext context) => _compose(envelope, selection, visibleReply, rawReply, context);
        public Task<PromptPackage> ComposeAsync(InteractionEnvelope envelope, RuleSelection selection, string visibleReply, string rawReply, PostprocessContext context, CancellationToken cancellationToken)
            => _composeAsync != null ? _composeAsync(envelope, selection, visibleReply, rawReply, context, cancellationToken)
                : Task.FromResult(Compose(envelope, selection, visibleReply, rawReply, context));
    }

    private sealed class DelegateActionPostprocessor : IAsyncActionPostprocessor
    {
        private readonly Func<string, PostprocessContext, ActionPlan> _parse;
        private readonly Func<string, PostprocessContext, CancellationToken, Task<ActionPlan>> _parseAsync;
        public DelegateActionPostprocessor(Func<string, PostprocessContext, ActionPlan> parse,
            Func<string, PostprocessContext, CancellationToken, Task<ActionPlan>> parseAsync = null)
        {
            _parse = parse;
            _parseAsync = parseAsync;
        }
        public ActionPlan Parse(string rawText, PostprocessContext context) => _parse(rawText, context);
        public Task<ActionPlan> ParseAsync(string rawText, PostprocessContext context, CancellationToken cancellationToken)
            => _parseAsync != null ? _parseAsync(rawText, context, cancellationToken) : Task.FromResult(Parse(rawText, context));
    }
}
