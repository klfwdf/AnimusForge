using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AnimusForge.Refactor.Contracts;

/// <summary>
/// Shared LLM pipeline seam. It intentionally stops before game mutation and
/// persistence so channel adapters can revalidate and execute on the main thread.
/// </summary>
public sealed class InteractionPipeline : IInteractionPipeline
{
    private readonly IRuleSelector _ruleSelector;
    private readonly IPromptPackageComposer _promptComposer;
    private readonly IPostprocessContextBuilder _postprocessContextBuilder;
    private readonly ILlmGateway _llmGateway;
    private readonly IVisibleReplyNormalizer _visibleReplyNormalizer;
    private readonly IActionPostprocessor _actionPostprocessor;
    private readonly CapabilitySet _capabilities;

    public InteractionPipeline(
        IRuleSelector ruleSelector,
        IPromptPackageComposer promptComposer,
        IPostprocessContextBuilder postprocessContextBuilder,
        ILlmGateway llmGateway,
        IVisibleReplyNormalizer visibleReplyNormalizer,
        IActionPostprocessor actionPostprocessor,
        CapabilitySet capabilities)
    {
        _ruleSelector = ruleSelector ?? throw new ArgumentNullException(nameof(ruleSelector));
        _promptComposer = promptComposer ?? throw new ArgumentNullException(nameof(promptComposer));
        _postprocessContextBuilder = postprocessContextBuilder ?? throw new ArgumentNullException(nameof(postprocessContextBuilder));
        _llmGateway = llmGateway ?? throw new ArgumentNullException(nameof(llmGateway));
        _visibleReplyNormalizer = visibleReplyNormalizer ?? throw new ArgumentNullException(nameof(visibleReplyNormalizer));
        _actionPostprocessor = actionPostprocessor ?? throw new ArgumentNullException(nameof(actionPostprocessor));
        _capabilities = capabilities ?? throw new ArgumentNullException(nameof(capabilities));
    }

    public async Task<InteractionResult> GenerateAsync(
        InteractionEnvelope envelope,
        LlmProviderSnapshot provider,
        CancellationToken cancellationToken)
    {
        if (envelope == null)
        {
            throw new ArgumentNullException(nameof(envelope));
        }

        if (provider == null)
        {
            throw new ArgumentNullException(nameof(provider));
        }

        RuleSelection selection = _ruleSelector.Select(envelope.Snapshot);
        if (selection == null || selection.RuleIds.Count == 0)
        {
            return new InteractionResult(
                InteractionStatus.SkippedByEligibility,
                string.Empty,
                new ActionPlan(Array.Empty<ActionRequest>(), string.Empty),
                Array.Empty<FactRecord>(),
                "no_eligible_rule");
        }

        PromptPackage prompt = _promptComposer.Compose(envelope, selection, _capabilities);
        LlmGenerateResult generated = await _llmGateway.GenerateAsync(
            new LlmGenerateRequest(envelope.Snapshot.Trace, provider, prompt),
            cancellationToken).ConfigureAwait(false);

        if (generated == null)
        {
            return Failure(InteractionStatus.NonRetryableFailure, "null_llm_result");
        }

        if (generated.Status != LlmResultStatus.Succeeded)
        {
            return Failure(MapStatus(generated.Status), generated.ErrorCode);
        }

        PostprocessContext postprocessContext = _postprocessContextBuilder.Build(envelope.Snapshot, selection, _capabilities);
        ActionPlan actionPlan = _actionPostprocessor.Parse(generated.RawText, postprocessContext)
            ?? new ActionPlan(Array.Empty<ActionRequest>(), string.Empty);
        string visibleReply = _visibleReplyNormalizer.Normalize(generated.RawText, postprocessContext.AllowedTagFamilies);

        return new InteractionResult(
            InteractionStatus.Succeeded,
            visibleReply,
            actionPlan,
            Array.Empty<FactRecord>(),
            string.Empty,
            generated.RawText,
            string.Empty);
    }

    private static InteractionResult Failure(InteractionStatus status, string errorCode)
    {
        return new InteractionResult(
            status,
            string.Empty,
            new ActionPlan(Array.Empty<ActionRequest>(), string.Empty),
            Array.Empty<FactRecord>(),
            errorCode ?? string.Empty);
    }

    private static InteractionStatus MapStatus(LlmResultStatus status)
    {
        switch (status)
        {
            case LlmResultStatus.Cancelled:
                return InteractionStatus.CancelledAsStale;
            case LlmResultStatus.RetryableFailure:
                return InteractionStatus.RetryableFailure;
            default:
                return InteractionStatus.NonRetryableFailure;
        }
    }
}
