using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using Newtonsoft.Json.Linq;

namespace AnimusForge.XihaiAction
{
    /// <summary>
    /// Shared-Gateway transport for the auxiliary SceneActions classifiers.
    /// The transport resolves the current auxiliary settings at request time;
    /// no credential is retained in the classifier or in a contract snapshot.
    /// </summary>
    internal sealed class AfV130ConfiguredGatewayTransport : IAfClassifierTransport
    {
        private const int RequestTimeoutMilliseconds = 60000;
        private int _disposed;

        public static bool TryCreate(out IAfClassifierTransport transport)
        {
            transport = null;
            try
            {
                DuelSettings settings = DuelSettings.GetSettings();
                string endpoint = DuelSettings.GetEffectiveApiUrl(settings?.AuxiliaryApiUrl ?? string.Empty);
                string apiKey = (settings?.AuxiliaryApiKey ?? string.Empty).Trim();
                string model = settings?.GetEffectiveAuxiliaryModelName() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(endpoint) ||
                    string.IsNullOrWhiteSpace(apiKey) ||
                    string.IsNullOrWhiteSpace(model))
                {
                    return false;
                }
                transport = new AfV130ConfiguredGatewayTransport();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> SendAsync(
            List<object> messages,
            int outputTokenLimit,
            CancellationToken cancellationToken)
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                throw new ObjectDisposedException(nameof(AfV130ConfiguredGatewayTransport));
            }
            cancellationToken.ThrowIfCancellationRequested();
            DuelSettings settings = DuelSettings.GetSettings();
            string endpoint = DuelSettings.GetEffectiveApiUrl(settings?.AuxiliaryApiUrl ?? string.Empty);
            string apiKey = (settings?.AuxiliaryApiKey ?? string.Empty).Trim();
            string model = settings?.GetEffectiveAuxiliaryModelName() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(endpoint) ||
                string.IsNullOrWhiteSpace(apiKey) ||
                string.IsNullOrWhiteSpace(model))
            {
                throw new InvalidOperationException("Auxiliary classifier Gateway configuration is incomplete.");
            }

            List<PromptMessage> copiedMessages = new List<PromptMessage>();
            foreach (object message in messages ?? new List<object>())
            {
                JObject item = message == null ? null : JObject.FromObject(message);
                copiedMessages.Add(new PromptMessage(
                    item?["role"]?.ToString() ?? "user",
                    item?["content"]?.ToString() ?? string.Empty));
            }
            PromptPackage prompt = new PromptPackage(
                copiedMessages,
                Math.Max(1, outputTokenLimit),
                model);
#if BANNERLORD_1_4_OR_GREATER
            string apiLine = "1.4";
#else
            string apiLine = "1.3";
#endif
            long generation = SaveRuntimeGuard.CaptureGeneration();
            TraceContext trace = new TraceContext(
                "af-scene-actions-classifier-" + Guid.NewGuid().ToString("N"),
                generation,
                generation,
                "auxiliary-classifier",
                apiLine);
            LlmProviderSnapshot provider = new LlmProviderSnapshot(
                "scene-actions-auxiliary",
                endpoint,
                model,
                RequestTimeoutMilliseconds,
                Math.Max(1, outputTokenLimit));
            LlmGenerateResult result = await new LegacyConfiguredChatGateway(
                    _ => apiKey,
                    temperature: 0f,
                    disableThinking: true)
                .GenerateAsync(
                    new LlmGenerateRequest(trace, provider, prompt, InteractionStage.MainReply),
                    cancellationToken)
                .ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (result == null || result.Status != LlmResultStatus.Succeeded)
            {
                throw new InvalidOperationException(
                    "Auxiliary classifier Gateway request failed: " +
                    (result?.ErrorCode ?? "empty_result"));
            }
            return result.RawText ?? string.Empty;
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref _disposed, 1);
        }
    }
}
