using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AnimusForge.Refactor.Contracts;

/// <summary>
/// Three-stage interaction pipeline: rule/preprocess selection, main reply,
/// then postprocess generation and ActionPlan parsing. It shares the same
/// immutable envelope and gateway but keeps postprocess failure from erasing a
/// valid visible main reply.
/// </summary>
public sealed class FullInteractionPipeline : IInteractionPipeline
{
    private readonly IRuleSelector _ruleSelector;
    private readonly IPromptPackageComposer _mainPromptComposer;
    private readonly IPostprocessContextBuilder _postprocessContextBuilder;
    private readonly IPostprocessPromptComposer _postprocessPromptComposer;
    private readonly ILlmGateway _llmGateway;
    private readonly IVisibleReplyNormalizer _visibleReplyNormalizer;
    private readonly IActionPostprocessor _actionPostprocessor;
    private readonly CapabilitySet _capabilities;

    public FullInteractionPipeline(
        IRuleSelector ruleSelector,
        IPromptPackageComposer mainPromptComposer,
        IPostprocessContextBuilder postprocessContextBuilder,
        IPostprocessPromptComposer postprocessPromptComposer,
        ILlmGateway llmGateway,
        IVisibleReplyNormalizer visibleReplyNormalizer,
        IActionPostprocessor actionPostprocessor,
        CapabilitySet capabilities)
    {
        _ruleSelector = ruleSelector ?? throw new ArgumentNullException(nameof(ruleSelector));
        _mainPromptComposer = mainPromptComposer ?? throw new ArgumentNullException(nameof(mainPromptComposer));
        _postprocessContextBuilder = postprocessContextBuilder ?? throw new ArgumentNullException(nameof(postprocessContextBuilder));
        _postprocessPromptComposer = postprocessPromptComposer ?? throw new ArgumentNullException(nameof(postprocessPromptComposer));
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
            return Result(InteractionStatus.SkippedByEligibility, "no_eligible_rule", null, null, null);
        }

        PromptPackage mainPrompt = _mainPromptComposer.Compose(envelope, selection, _capabilities);
        LlmGenerateResult main = await GenerateStageAsync(
            envelope,
            provider,
            mainPrompt,
            InteractionStage.MainReply,
            cancellationToken).ConfigureAwait(false);
        if (main == null)
        {
            return Result(InteractionStatus.NonRetryableFailure, "null_main_result", null, null, null);
        }
        if (main.Status != LlmResultStatus.Succeeded)
        {
            return Result(MapStatus(main.Status), main.ErrorCode, null, null, null);
        }

        PostprocessContext context = _postprocessContextBuilder.Build(envelope.Snapshot, selection, _capabilities)
            ?? new PostprocessContext(Array.Empty<string>(), Array.Empty<string>(), _capabilities);
        string visibleReply = _visibleReplyNormalizer.Normalize(
            main.RawText,
            context?.AllowedTagFamilies ?? Array.Empty<string>());
        PromptPackage postprocessPrompt = _postprocessPromptComposer.Compose(
            envelope,
            selection,
            visibleReply,
            main.RawText,
            context);
        if (postprocessPrompt == null)
        {
            return Result(InteractionStatus.Succeeded, string.Empty, visibleReply, main.RawText, null);
        }

        LlmGenerateResult postprocess = await GenerateStageAsync(
            envelope,
            provider,
            postprocessPrompt,
            InteractionStage.Postprocess,
            cancellationToken).ConfigureAwait(false);
        if (postprocess == null)
        {
            return Result(InteractionStatus.Succeeded, "postprocess_null_result", visibleReply, main.RawText, null);
        }
        if (postprocess.Status != LlmResultStatus.Succeeded)
        {
            return Result(
                InteractionStatus.Succeeded,
                PrefixPostprocessError(postprocess.ErrorCode),
                visibleReply,
                main.RawText,
                null);
        }

        ActionPlan actionPlan = _actionPostprocessor.Parse(postprocess.RawText, context)
            ?? new ActionPlan(Array.Empty<ActionRequest>(), string.Empty);
        return Result(InteractionStatus.Succeeded, string.Empty, visibleReply, main.RawText, actionPlan, postprocess.RawText);
    }

    private Task<LlmGenerateResult> GenerateStageAsync(
        InteractionEnvelope envelope,
        LlmProviderSnapshot provider,
        PromptPackage prompt,
        InteractionStage stage,
        CancellationToken cancellationToken)
    {
        if (prompt == null)
        {
            throw new InvalidOperationException("Prompt package is required for " + stage + ".");
        }
        return _llmGateway.GenerateAsync(
            new LlmGenerateRequest(envelope.Snapshot.Trace, provider, prompt, stage),
            cancellationToken);
    }

    private static InteractionResult Result(
        InteractionStatus status,
        string errorCode,
        string visibleReply,
        string rawReply,
        ActionPlan actionPlan = null,
        string rawPostprocessReply = null)
    {
        return new InteractionResult(
            status,
            visibleReply ?? string.Empty,
            actionPlan ?? new ActionPlan(Array.Empty<ActionRequest>(), string.Empty),
            Array.Empty<FactRecord>(),
            errorCode ?? string.Empty,
            rawReply,
            rawPostprocessReply);
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

    private static string PrefixPostprocessError(string errorCode)
    {
        return string.IsNullOrWhiteSpace(errorCode) ? "postprocess_failed" : "postprocess_" + errorCode.Trim();
    }
}
