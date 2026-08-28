using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.SceneActions.Core;

namespace AnimusForge.XihaiAction
{
    /// <summary>
    /// AF classifier task launch boundary. Completion application remains on the Mission
    /// thread so action state and target authority are never mutated from a worker task.
    /// </summary>
    internal sealed partial class SceneActionsMissionBehavior
    {
        private static void ObserveLateClassifierFailure(Task<string> providerTask)
        {
            _ = providerTask.ContinueWith(
                task =>
                {
                    _ = task.Exception;
                },
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted |
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

        private void DrainClassifierCompletions(double now)
        {
            while (_classifierCompletions.TryDequeue(out ClassifierCompletion completion))
            {
                if (!_pendingClassifications.TryGetValue(
                    completion.RequestId,
                    out PendingClassification pending))
                {
                    continue;
                }
                _pendingClassifications.Remove(completion.RequestId);
                if (completion.SessionGeneration != _sessionGeneration ||
                    Volatile.Read(ref _closed) != 0)
                {
                    FinishAcceptedRequestWithoutTargets(
                        completion.RequestId,
                        ExecutionResultCode.MissionChanged,
                        "Classifier result belongs to a closed Mission.");
                    continue;
                }
                if (now > pending.ExpiresAtMissionTime)
                {
                    FinishAcceptedRequestWithoutTargets(
                        completion.RequestId,
                        ExecutionResultCode.Expired,
                        "Classifier result arrived after Mission-time TTL.");
                    continue;
                }
                if (completion.Failure.HasValue)
                {
                    FinishAcceptedRequestWithoutTargets(
                        completion.RequestId,
                        completion.Failure.Value,
                        completion.Error);
                    continue;
                }

                ParseDecision decision = pending.Captured.InputSource ==
                                         SceneInputSource.NpcSceneShoutReply
                    ? SceneActionsRuntimeHost.Parser.ParseNpcReplyClassifierOutput(
                        completion.Output)
                    : SceneActionsRuntimeHost.Parser.ParseClassifierOutput(completion.Output);
                if (decision.Status == ParseStatus.NoAction)
                {
                    if (pending.FallbackToConsent &&
                        ResolvePendingNpcConsent(pending.Captured, now))
                    {
                        continue;
                    }
                    FinishNoAction(
                        completion.RequestId,
                        "Classifier returned NONE.");
                    continue;
                }
                if (decision.Status != ParseStatus.Matched ||
                    decision.ProgramV4 == null ||
                    decision.ProgramV4.Steps.SelectMany(step => step.IntentKeys)
                        .Any(key => !pending.AllowedIntentKeys.Contains(
                            key,
                            StringComparer.Ordinal)))
                {
                    FinishAcceptedRequestWithoutTargets(
                        completion.RequestId,
                        ExecutionResultCode.InvalidClassifierOutput,
                        decision.Error ?? "Classifier selected a key outside the frozen allow-list.");
                    continue;
                }
                if (pending.Captured.InputSource == SceneInputSource.NpcSceneShoutReply &&
                    !SceneActionFrameworkV4.ValidateNpcClassifierProgramEvidence(
                        pending.ClassifierText,
                        decision.ProgramV4,
                        out string evidenceError))
                {
                    SceneActionsLog.Warning(
                        "CLASSIFIER",
                        "NPC classifier action rejected because current reply lacked " +
                        "performed-action evidence. RequestId=" +
                        completion.RequestId.ToString("N") +
                        " Program=" + decision.ProgramV4.ProtocolExpression +
                        " Reason=" + (evidenceError ?? "unknown"));
                    FinishAcceptedRequestWithoutTargets(
                        completion.RequestId,
                        ExecutionResultCode.InvalidClassifierOutput,
                        evidenceError);
                    continue;
                }
                ParseDecision targetedDecision = ParseDecision.MatchProgramV4(
                    decision.ProgramV4,
                    pending.TargetOverride,
                    ResolverSource.AiClassifier,
                    pending.BypassNpcConsent);
                if (pending.Captured.InputSource == SceneInputSource.PlayerSceneShout)
                {
                    RouteResolvedPlayerIntent(pending.Captured, targetedDecision, now);
                }
                else
                {
                    ConsumePendingConsentForSpeaker(
                        pending.Captured.Speaker,
                        now,
                        "AI resolved an actual NPC action description.");
                    BuildAndQueuePlans(pending.Captured, targetedDecision, now);
                }
            }
        }

        private void DrainConsentClassifierCompletions(double now)
        {
            while (_consentClassifierCompletions.TryDequeue(
                out ConsentClassifierCompletion completion))
            {
                if (!_pendingConsentClassifications.TryGetValue(
                    completion.RequestId,
                    out PendingConsentClassification pending))
                {
                    continue;
                }
                _pendingConsentClassifications.Remove(completion.RequestId);
                if (completion.SessionGeneration != _sessionGeneration ||
                    Volatile.Read(ref _closed) != 0)
                {
                    FinishAcceptedRequestWithoutTargets(
                        completion.RequestId,
                        ExecutionResultCode.MissionChanged,
                        "Consent result belongs to a closed Mission.");
                    continue;
                }
                if (now > pending.ExpiresAtMissionTime)
                {
                    FinishAcceptedRequestWithoutTargets(
                        completion.RequestId,
                        ExecutionResultCode.Expired,
                        "Consent result arrived after the reply-event Mission-time TTL.");
                    continue;
                }
                if (completion.Failure.HasValue)
                {
                    FinishAcceptedRequestWithoutTargets(
                        completion.RequestId,
                        completion.Failure.Value,
                        completion.Error);
                    continue;
                }
                if (!ConsentReplyInterpreter.TryParseClassifierOutput(
                    completion.Output,
                    out ConsentDecision decision))
                {
                    FinishAcceptedRequestWithoutTargets(
                        completion.RequestId,
                        ExecutionResultCode.InvalidClassifierOutput,
                        "Consent classifier output was outside ACCEPT/REFUSE/UNCLEAR.");
                    continue;
                }

                ApplyConsentDecision(
                    pending.Captured,
                    pending.FrozenRequest,
                    decision,
                    ResolverSource.NpcConsentClassifier,
                    now);
            }
        }

        private void StartClassification(
            CapturedSceneActionEvent captured,
            ParseDecision fallback,
            double now,
            bool fallbackToConsent,
            string previousPlayerText)
        {
            SceneActionSettings settings = SceneActionsRuntimeHost.Settings;
            List<string> allowed = BuildEffectiveClassifierAllowList();
            if (allowed.Count == 0)
            {
                FinishAcceptedRequestWithoutTargets(
                    captured.EventId,
                    ExecutionResultCode.ClassifierUnavailable,
                    "No validated and runtime-ready classifier-selectable intents.");
                return;
            }
            if (!SceneActionsRuntimeHost.TryGetClassifier(
                settings.AiClassifierProviderId,
                out IAuxiliaryTextClassifierV1 classifier))
            {
                FinishAcceptedRequestWithoutTargets(
                    captured.EventId,
                    ExecutionResultCode.ClassifierUnavailable,
                    "Configured classifier provider is not registered.");
                return;
            }

            PendingClassification pending = new PendingClassification
            {
                Captured = captured,
                ClassifierText = fallback.ClassifierText ?? string.Empty,
                AllowedIntentKeys = allowed,
                TargetOverride = fallback.TargetOverride,
                FallbackToConsent = fallbackToConsent,
                BypassNpcConsent = fallback.BypassNpcConsent,
                SessionGeneration = _sessionGeneration,
                ExpiresAtMissionTime = captured.SubmittedAtMissionTime +
                                       (settings.RequestTtlMs / 1000d)
            };
            _pendingClassifications.Add(captured.EventId, pending);
            ClassifierRequest request = new ClassifierRequest
            {
                RequestId = captured.EventId,
                InputSource = captured.InputSource,
                Text = fallback.ClassifierText ?? string.Empty,
                PreviousPlayerText = previousPlayerText ?? string.Empty,
                FullNpcReplyText = captured.InputSource == SceneInputSource.NpcSceneShoutReply
                    ? captured.RawText ?? string.Empty
                    : string.Empty,
                AllowedIntentKeys = allowed.ToArray(),
                ImplicitEmotionIntentKeys = captured.InputSource ==
                                            SceneInputSource.NpcSceneShoutReply
                    ? ImplicitEmotionInferenceV1.SupportedIntentKeys
                        .Where(key => allowed.Contains(key, StringComparer.Ordinal))
                        .ToArray()
                    : Array.Empty<string>()
            };

            ConcurrentQueue<ClassifierCompletion> output = _classifierCompletions;
            CancellationToken sessionToken = _sessionCancellation.Token;
            int timeoutMs = settings.ClassifierTimeoutMs;
            int maxChars = settings.ClassifierMaxOutputChars;
            long generation = _sessionGeneration;
            _ = Task.Run(async () =>
            {
                ClassifierCompletion completion = new ClassifierCompletion
                {
                    RequestId = request.RequestId,
                    SessionGeneration = generation
                };
                using (CancellationTokenSource providerCancellation =
                    CancellationTokenSource.CreateLinkedTokenSource(sessionToken))
                {
                    try
                    {
                        Task<string> providerTask = classifier.ClassifyAsync(
                            request,
                            providerCancellation.Token);
                        if (providerTask == null)
                        {
                            completion.Failure = ExecutionResultCode.ClassifierUnavailable;
                            completion.Error = "Classifier returned a null Task.";
                        }
                        else
                        {
                            Task timeoutTask = Task.Delay(timeoutMs, sessionToken);
                            Task winner = await Task.WhenAny(providerTask, timeoutTask)
                                .ConfigureAwait(false);
                            if (!ReferenceEquals(winner, providerTask))
                            {
                                providerCancellation.Cancel();
                                ObserveLateClassifierFailure(providerTask);
                                completion.Failure = sessionToken.IsCancellationRequested
                                    ? ExecutionResultCode.Cancelled
                                    : ExecutionResultCode.ClassifierTimeout;
                            }
                            else
                            {
                                string result = await providerTask.ConfigureAwait(false);
                                if (result != null && result.Length > maxChars)
                                {
                                    completion.Failure =
                                        ExecutionResultCode.InvalidClassifierOutput;
                                    completion.Error = "Classifier output exceeded maxOutputChars.";
                                }
                                else
                                {
                                    completion.Output = result;
                                }
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        completion.Failure = sessionToken.IsCancellationRequested
                            ? ExecutionResultCode.Cancelled
                            : ExecutionResultCode.ClassifierTimeout;
                    }
                    catch (Exception ex)
                    {
                        completion.Failure = ExecutionResultCode.ClassifierUnavailable;
                        completion.Error = ex.GetType().Name + ": " + ex.Message;
                    }
                }
                output.Enqueue(completion);
            });
        }

        private void StartConsentClassification(
            CapturedSceneActionEvent captured,
            FrozenConsentRequest frozen,
            IAuxiliaryConsentClassifierV1 classifier)
        {
            SceneActionSettings settings = SceneActionsRuntimeHost.Settings;
            PendingConsentClassification pending = new PendingConsentClassification
            {
                Captured = captured,
                FrozenRequest = frozen,
                SessionGeneration = _sessionGeneration,
                ExpiresAtMissionTime = captured.SubmittedAtMissionTime +
                                       (settings.RequestTtlMs / 1000d)
            };
            _pendingConsentClassifications.Add(captured.EventId, pending);
            ConsentClassifierRequest request = new ConsentClassifierRequest
            {
                RequestId = captured.EventId,
                FrozenIntentKey = frozen.IntentKey,
                FrozenProgram = frozen.ProgramExpression,
                ReplyText = captured.RawText ?? string.Empty
            };

            ConcurrentQueue<ConsentClassifierCompletion> output =
                _consentClassifierCompletions;
            CancellationToken sessionToken = _sessionCancellation.Token;
            int timeoutMs = settings.ClassifierTimeoutMs;
            int maxChars = settings.ClassifierMaxOutputChars;
            long generation = _sessionGeneration;
            _ = Task.Run(async () =>
            {
                ConsentClassifierCompletion completion =
                    new ConsentClassifierCompletion
                    {
                        RequestId = request.RequestId,
                        SessionGeneration = generation
                    };
                using (CancellationTokenSource providerCancellation =
                    CancellationTokenSource.CreateLinkedTokenSource(sessionToken))
                {
                    try
                    {
                        Task<string> providerTask = classifier.ClassifyConsentAsync(
                            request,
                            providerCancellation.Token);
                        if (providerTask == null)
                        {
                            completion.Failure = ExecutionResultCode.ClassifierUnavailable;
                            completion.Error = "Consent classifier returned a null Task.";
                        }
                        else
                        {
                            Task timeoutTask = Task.Delay(timeoutMs, sessionToken);
                            Task winner = await Task.WhenAny(providerTask, timeoutTask)
                                .ConfigureAwait(false);
                            if (!ReferenceEquals(winner, providerTask))
                            {
                                providerCancellation.Cancel();
                                ObserveLateClassifierFailure(providerTask);
                                completion.Failure = sessionToken.IsCancellationRequested
                                    ? ExecutionResultCode.Cancelled
                                    : ExecutionResultCode.ClassifierTimeout;
                            }
                            else
                            {
                                string result = await providerTask.ConfigureAwait(false);
                                if (result != null && result.Length > maxChars)
                                {
                                    completion.Failure =
                                        ExecutionResultCode.InvalidClassifierOutput;
                                    completion.Error =
                                        "Consent classifier output exceeded maxOutputChars.";
                                }
                                else
                                {
                                    completion.Output = result;
                                }
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        completion.Failure = sessionToken.IsCancellationRequested
                            ? ExecutionResultCode.Cancelled
                            : ExecutionResultCode.ClassifierTimeout;
                    }
                    catch (Exception ex)
                    {
                        completion.Failure = ExecutionResultCode.ClassifierUnavailable;
                        completion.Error = ex.GetType().Name + ": " + ex.Message;
                    }
                }
                output.Enqueue(completion);
            });
        }
    }
}
