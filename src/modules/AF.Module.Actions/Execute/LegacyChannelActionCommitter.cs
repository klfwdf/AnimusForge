using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge.Refactor.Runtime;

/// <summary>
/// Action-only commit boundary for legacy channel state machines. It gives
/// Native, Scene and Courier one parser, raw/plan authorization rule and
/// terminal execution receipt without taking ownership of their presentation,
/// history, relay or transport lifecycle.
/// </summary>
internal sealed class LegacyChannelActionCommitter
{
    private readonly LegacyActionTagParser _parser;
    private readonly PostprocessContext _context;

    internal LegacyChannelActionCommitter(
        IEnumerable<string> allowedTagFamilies = null,
        int maxActions = 64)
    {
        IReadOnlyList<string> allowed =
            (allowedTagFamilies ?? LegacyActionTagCatalog.DefaultAllowedTagFamilies)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList()
            .AsReadOnly();
        _parser = new LegacyActionTagParser(Math.Max(1, maxActions));
        _context = new PostprocessContext(
            Array.Empty<string>(),
            allowed,
            new CapabilitySet(new[] { "action.parse" }));
    }

    internal LegacyChannelActionCommitResult Commit(
        string rawPostprocessText,
        GameInteractionSnapshot snapshot,
        IActionPlanExecutor actionExecutor)
    {
        string raw = rawPostprocessText ?? string.Empty;
        if (_parser.ExceedsActionLimit(raw))
        {
            return LegacyChannelActionCommitResult.Rejected("action_limit_exceeded");
        }
        if (_parser.HasDisallowedProtocolTag(raw, _context))
        {
            return LegacyChannelActionCommitResult.Rejected("action_protocol_not_allowed");
        }

        ActionPlan plan = _parser.Parse(raw, _context);
        if (plan.Actions.Count == 0)
        {
            return new LegacyChannelActionCommitResult(
                plan,
                ActionExecutionCommitResult.NoActions());
        }
        if (snapshot == null || actionExecutor == null)
        {
            return new LegacyChannelActionCommitResult(
                plan,
                ActionExecutionCommitResult.Rejected(
                    InteractionStatus.RejectedByValidation,
                    "missing_action_execution_boundary"));
        }

        try
        {
            string requestId = InteractionResultCommitter.BuildCanonicalRequestId(snapshot);
            string actionFingerprint =
                InteractionResultCommitter.BuildCanonicalActionPlanFingerprint(plan);
            return new LegacyChannelActionCommitResult(
                plan,
                ActionExecutionCommitter.Execute(
                    plan,
                    snapshot,
                    actionExecutor,
                    requestId,
                    actionFingerprint));
        }
        catch
        {
            return new LegacyChannelActionCommitResult(
                plan,
                ActionExecutionCommitResult.Rejected(
                    InteractionStatus.RejectedByValidation,
                    "invalid_action_commit_identity"));
        }
    }
}

internal sealed class LegacyChannelActionCommitResult
{
    internal LegacyChannelActionCommitResult(
        ActionPlan actionPlan,
        ActionExecutionCommitResult execution)
    {
        ActionPlan = actionPlan
            ?? new ActionPlan(Array.Empty<ActionRequest>(), string.Empty);
        Execution = execution ?? ActionExecutionCommitResult.Rejected(
            InteractionStatus.NonRetryableFailure,
            "missing_action_execution_receipt");
    }

    internal ActionPlan ActionPlan { get; }
    internal ActionExecutionCommitResult Execution { get; }
    internal bool HasActions => ActionPlan.Actions.Count > 0;

    internal static LegacyChannelActionCommitResult Rejected(string errorCode)
        => new LegacyChannelActionCommitResult(
            new ActionPlan(Array.Empty<ActionRequest>(), string.Empty),
            ActionExecutionCommitResult.Rejected(
                InteractionStatus.RejectedByValidation,
                errorCode));
}
