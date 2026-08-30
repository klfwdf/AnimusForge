using System;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge.Refactor.Adapters;

/// <summary>
/// Domain boundary for the legacy Knowledge/RAG short-text generator.
/// Knowledge owns prompt construction, token capping, response parsing and
/// deterministic fallback; this adapter owns only provider transport policy.
/// RAG is a generation-only capability and must not accidentally run the
/// shared postprocess/action stage.
/// </summary>
public sealed class LegacyKnowledgeRagGateway : ILlmGateway
{
    private readonly ILlmGateway _configuredGateway;

    public LegacyKnowledgeRagGateway(
        Func<LlmProviderSnapshot, string> credentialResolver,
        float? temperature = null)
    {
        _configuredGateway = new LegacyConfiguredChatGateway(
            credentialResolver ?? throw new ArgumentNullException(nameof(credentialResolver)),
            temperature: temperature,
            disableThinking: true);
    }

    public Task<LlmGenerateResult> GenerateAsync(
        LlmGenerateRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }
        if (request.Stage != InteractionStage.MainReply)
        {
            return Task.FromResult(new LlmGenerateResult(
                LlmResultStatus.NonRetryableFailure,
                string.Empty,
                0,
                0,
                "knowledge_stage_not_supported"));
        }
        return _configuredGateway.GenerateAsync(request, cancellationToken);
    }
}
