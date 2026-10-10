using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.SceneActions.Core;
using Newtonsoft.Json;

namespace AnimusForge.XihaiAction
{
    internal sealed class AfV130AuxiliaryTextClassifier :
        IAuxiliaryTextClassifierV1,
        IAuxiliaryConsentClassifierV1,
        IBattleSpeechClassifierV2,
        IDisposable
    {
        private const int MaxClassifierInputChars = 1024;
        private const int MaxClassifierContextChars = 2048;
        private const int OutputTokenLimit = 32;
        private const int ConsentOutputTokenLimit = 8;
        private const int BattleSpeechTriggerOutputTokenLimit = 8;
        // Keep one request (and the closed-set parser), but size the response
        // budget from the frozen audience settings.  The old fixed 192-token
        // budget was only large enough for the original 24 short replies and
        // caused larger plans to be truncated and rejected by the parser.
        private const int BattleSpeechPlanMinimumOutputTokenLimit = 192;
        private const int BattleSpeechPlanMaximumOutputTokenLimit = 10000;

        private static readonly HashSet<string> LogicalIntentKeys =
            new HashSet<string>(
                SceneActionFrameworkV4.LogicalActions.Select(entry => entry.IntentKey),
                StringComparer.Ordinal);

        private readonly IAfClassifierTransport _transport;
        private readonly bool _useChinese;
        private readonly SceneActionClassifierPrompts _prompts;
        private readonly SemaphoreSlim _singleFlight = new SemaphoreSlim(1, 1);
        // Keep short speech trigger/plan calls out of the ordinary scene-action
        // and consent queue. The transport remains the AF entry point, but a
        // pending memory/action classifier no longer delays speech classification.
        private readonly SemaphoreSlim _battleSpeechFlight = new SemaphoreSlim(1, 1);
        private readonly CancellationTokenSource _lifetimeCancellation =
            new CancellationTokenSource();
        private int _disposed;

        public AfV130AuxiliaryTextClassifier(MethodInfo callApiWithMessages)
            : this(new AfV130CallApiTransport(callApiWithMessages))
        {
        }

        public AfV130AuxiliaryTextClassifier(MethodInfo callApiWithMessages, bool useChinese)
            : this(new AfV130CallApiTransport(callApiWithMessages), useChinese)
        {
        }

        internal AfV130AuxiliaryTextClassifier(IAfClassifierTransport transport)
            : this(transport, true)
        {
        }

        internal AfV130AuxiliaryTextClassifier(IAfClassifierTransport transport, bool useChinese)
        {
            _transport = transport ??
                         throw new ArgumentNullException(nameof(transport));
            _useChinese = useChinese;
            _prompts = new SceneActionClassifierPrompts(useChinese);
        }

        public async Task<string> ClassifyAsync(
            ClassifierRequest request,
            CancellationToken cancellationToken)
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                throw new ObjectDisposedException(nameof(AfV130AuxiliaryTextClassifier));
            }
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            string classifierText = (request.Text ?? string.Empty).Trim();
            if (classifierText.Length == 0 || classifierText.Length > MaxClassifierInputChars)
            {
                return "NONE";
            }

            List<string> allowed = (request.AllowedIntentKeys ?? Array.Empty<string>())
                .Where(key => !string.IsNullOrWhiteSpace(key))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(key => key, StringComparer.Ordinal)
                .ToList();
            if (allowed.Count == 0)
            {
                return "NONE";
            }
            if (allowed.Any(key => !LogicalIntentKeys.Contains(key)))
            {
                throw new InvalidOperationException(
                    "Classifier allow-list contains a key outside SceneActionFrameworkV4.");
            }

            List<string> implicitEmotionKeys =
                (request.ImplicitEmotionIntentKeys ?? Array.Empty<string>())
                .Where(key => !string.IsNullOrWhiteSpace(key))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(key => key, StringComparer.Ordinal)
                .ToList();
            if (implicitEmotionKeys.Any(key =>
                    !allowed.Contains(key) ||
                    !ImplicitEmotionInferenceV1.SupportedIntentKeys.Contains(
                        key,
                        StringComparer.Ordinal)))
            {
                throw new InvalidOperationException(
                    "Implicit emotion allow-list is outside the frozen request allow-list.");
            }
            if (request.InputSource != SceneInputSource.NpcSceneShoutReply)
            {
                implicitEmotionKeys.Clear();
            }

            string systemPrompt = BuildClassifierSystemPrompt(allowed);
            string previousPlayerText = BoundContextText(request.PreviousPlayerText);
            string fullNpcReplyText = BoundContextText(request.FullNpcReplyText);

            string userPayload = JsonConvert.SerializeObject(new
            {
                inputSource = request.InputSource.ToString(),
                allowedIntentKeys = allowed,
                implicitEmotionIntentKeys = implicitEmotionKeys,
                previousPlayerText,
                untrustedText = classifierText,
                fullNpcReplyText
            });
            List<object> messages = new List<object>
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPayload }
            };

            return await InvokeProviderAsync(
                    messages,
                    OutputTokenLimit,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<string> ClassifyConsentAsync(
            ConsentClassifierRequest request,
            CancellationToken cancellationToken)
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                throw new ObjectDisposedException(nameof(AfV130AuxiliaryTextClassifier));
            }
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            string frozenExpression = request.FrozenProgram ?? request.FrozenIntentKey;
            if (!ActionProgramV4.TryParseExpression(
                    frozenExpression,
                    out ActionProgramV4 frozenProgram,
                    out _) ||
                frozenProgram.Steps.SelectMany(step => step.IntentKeys)
                    .Any(key => !LogicalIntentKeys.Contains(key)))
            {
                throw new InvalidOperationException(
                    "Frozen consent program is outside SceneActionFrameworkV4.");
            }

            string replyText = (request.ReplyText ?? string.Empty).Trim();
            if (replyText.Length == 0 || replyText.Length > MaxClassifierInputChars)
            {
                return "UNCLEAR";
            }

            string userPayload = JsonConvert.SerializeObject(new
            {
                frozenProgram = frozenProgram.ProtocolExpression,
                untrustedNpcReply = replyText
            });
            List<object> messages = new List<object>
            {
                new { role = "system", content = _prompts.ConsentSystemPrompt },
                new { role = "user", content = userPayload }
            };

            return await InvokeProviderAsync(
                    messages,
                    ConsentOutputTokenLimit,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<string> ClassifyBattleSpeechTriggerAsync(
            BattleSpeechTriggerClassifierRequestV2 request,
            CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            string playerText = (request.PlayerText ?? string.Empty).Trim();
            if (playerText.Length == 0 || playerText.Length > MaxClassifierInputChars)
            {
                return "NONE";
            }
            string payload = JsonConvert.SerializeObject(new
            {
                hasFrozenPrimaryNpcTarget = request.HasPrimaryNpcTarget,
                untrustedPlayerText = playerText
            });
            return await InvokeProviderAsync(
                    new List<object>
                    {
                        new { role = "system", content = _prompts.BattleSpeechTriggerSystemPrompt },
                        new { role = "user", content = payload }
                    },
                    BattleSpeechTriggerOutputTokenLimit,
                    cancellationToken,
                    _battleSpeechFlight)
                .ConfigureAwait(false);
        }

        public async Task<string> ClassifyBattleSpeechPlanAsync(
            BattleSpeechPlanClassifierRequestV2 request,
            CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            string speechText = (request.SpeechText ?? string.Empty).Trim();
            if (speechText.Length == 0 || speechText.Length > MaxClassifierContextChars)
            {
                return "ACTIONS NONE\nTACTIC NONE\nREPLIES NONE";
            }
            List<string> allowed = (request.AllowedIntentKeys ?? Array.Empty<string>())
                .Where(key => !string.IsNullOrWhiteSpace(key))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(key => key, StringComparer.Ordinal)
                .ToList();
            if (allowed.Count == 0 || allowed.Any(key => !LogicalIntentKeys.Contains(key)))
            {
                throw new InvalidOperationException(
                    "Battle speech allow-list is outside SceneActionFrameworkV4.");
            }
            string systemPrompt = _prompts.BattleSpeechPlanSystemPromptPreamble +
                                  SceneActionFrameworkV4.BuildClassifierDefinitionBlock(allowed, _useChinese);
            string payload = JsonConvert.SerializeObject(new
            {
                allowedIntentKeys = allowed,
                allowAdvance = request.AllowAdvance,
                audienceReplyCount = Math.Max(
                    0,
                    Math.Min(
                        BattleSpeechFrameworkV2.MaximumAudienceReplies,
                        request.AudienceReplyCount)),
                audienceReplyMinimumChars = request.AudienceReplyMinimumChars,
                audienceReplyMaximumChars = request.AudienceReplyMaximumChars,
                untrustedSpeechText = speechText
            });
            return await InvokeProviderAsync(
                    new List<object>
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = payload }
                    },
                    CalculateBattleSpeechPlanOutputTokenLimit(
                        request.AudienceReplyCount,
                        request.AudienceReplyMaximumChars),
                    cancellationToken,
                    _battleSpeechFlight)
                .ConfigureAwait(false);
        }

        internal static int CalculateBattleSpeechPlanOutputTokenLimit(
            int audienceReplyCount,
            int audienceReplyMaximumChars)
        {
            int boundedCount = Math.Max(
                0,
                Math.Min(
                    BattleSpeechFrameworkV2.MaximumAudienceReplies,
                    audienceReplyCount));
            int boundedChars = Math.Max(1, Math.Min(80, audienceReplyMaximumChars));

            // Chinese replies can consume roughly one to two output tokens per
            // character.  Reserve room for the body, protocol labels, and
            // separators while keeping a hard cap for provider safety.
            long estimate = 256L +
                            (long)boundedCount * (boundedChars * 2L + 8L);
            return (int)Math.Min(
                BattleSpeechPlanMaximumOutputTokenLimit,
                Math.Max(BattleSpeechPlanMinimumOutputTokenLimit, estimate));
        }

        private string BuildClassifierSystemPrompt(IEnumerable<string> allowedIntentKeys)
        {
            return _prompts.SystemPromptPreamble +
                   SceneActionFrameworkV4.BuildClassifierDefinitionBlock(allowedIntentKeys, _useChinese);
        }

        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                throw new ObjectDisposedException(nameof(AfV130AuxiliaryTextClassifier));
            }
        }

        private static string BoundContextText(string text)
        {
            string value = (text ?? string.Empty).Trim();
            return value.Length <= MaxClassifierContextChars
                ? value
                : value.Substring(0, MaxClassifierContextChars);
        }

        private async Task<string> InvokeProviderAsync(
            List<object> messages,
            int outputTokenLimit,
            CancellationToken cancellationToken,
            SemaphoreSlim flight = null)
        {

            using (CancellationTokenSource linkedCancellation =
                CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    _lifetimeCancellation.Token))
            {
                CancellationToken effectiveToken = linkedCancellation.Token;
                SemaphoreSlim gate = flight ?? _singleFlight;
                await gate.WaitAsync(effectiveToken).ConfigureAwait(false);
                try
                {
                    effectiveToken.ThrowIfCancellationRequested();
                    return await _transport.SendAsync(
                            messages,
                            outputTokenLimit,
                            effectiveToken)
                        .ConfigureAwait(false);
                }
                finally
                {
                    try
                    {
                        gate.Release();
                    }
                    catch (ObjectDisposedException)
                    {
                        // Dispose may race an already acquired flight gate. The
                        // lifetime token has already cancelled the operation;
                        // releasing a disposed semaphore is otherwise harmless.
                    }
                }
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }
            try
            {
                _lifetimeCancellation.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
            try
            {
                _transport.Dispose();
            }
            catch (Exception)
            {
                // Transport disposal is a best-effort lifecycle cleanup.
            }
            _battleSpeechFlight.Dispose();
            _singleFlight.Dispose();
            _lifetimeCancellation.Dispose();
        }
    }
}
