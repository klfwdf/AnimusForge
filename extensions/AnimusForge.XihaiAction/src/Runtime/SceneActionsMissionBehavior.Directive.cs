using System.Collections.Generic;
using AnimusForge.SceneActions.Core;
using TaleWorlds.MountAndBlade;

namespace AnimusForge.XihaiAction
{
    /// <summary>
    /// NPC reply actions decided by AF's unified postprocess instead of a
    /// separate SceneActions classifier request.  The directive only names
    /// logical V4 keys; it reuses the classifier result path so allow-list,
    /// performed-action evidence, consent and playback rules stay identical.
    /// Runs once per NPC reply on the Mission thread; no Tick scanning.
    /// </summary>
    internal sealed partial class SceneActionsMissionBehavior
    {
        // Rollback switch: false restores the separate NPC classifier request.
        internal const bool NpcReplyUsesPostprocessDirective = true;

        // A local implicit emotion inside this window after a directive for the
        // same speaker is treated as the same reply and not replayed.
        private const double DirectiveEchoWindowSeconds = 10d;

        // Per speaker Agent.Index: Mission time of the last NPC reply resolved
        // by the local parser/consent path, and of the last applied directive.
        private readonly Dictionary<int, double> _npcReplyLocalResolvedAt =
            new Dictionary<int, double>();
        private readonly Dictionary<int, double> _npcReplyDirectiveAppliedAt =
            new Dictionary<int, double>();

        /// <summary>
        /// Game-thread offer check used while AF prepares the postprocess
        /// request.  Returns the frozen allow-list only when the reply contains
        /// stage-direction prose the local parser cannot resolve, i.e. exactly
        /// the replies that previously triggered the classifier request.
        /// </summary>
        internal List<string> BuildNpcReplyDirectiveOffer(Agent speaker, string rawReply)
        {
            if (!IsSessionActive || speaker == null || !speaker.IsActive() ||
                !ReferenceEquals(speaker.Mission, Mission) ||
                string.IsNullOrWhiteSpace(rawReply))
            {
                return null;
            }
            double now = Mission.CurrentTime;
            if (TryGetPendingNpcConsent(speaker, now, out _, out _))
            {
                // Consent keeps its own frozen-request flow.
                return null;
            }
            ParseDecision decision = SceneActionsRuntimeHost.Parser.ParseNpcReplyText(
                rawReply,
                speaker.Name);
            if (decision.Status != ParseStatus.NoAction ||
                !decision.AiFallbackRequested ||
                SceneActionsRuntimeHost.Parser.TryBuildDeterministicNpcProgram(
                    decision.ClassifierText,
                    speaker.Name,
                    out _))
            {
                return null;
            }
            List<string> allowed = BuildEffectiveClassifierAllowList();
            return allowed.Count == 0 ? null : allowed;
        }

        private void ProcessNpcDirectiveEvent(CapturedSceneActionEvent captured, double now)
        {
            Agent speaker = captured.Speaker;
            if (speaker == null || !speaker.IsActive() ||
                !ReferenceEquals(speaker.Mission, Mission))
            {
                FinishAcceptedRequestWithoutTargets(
                    captured.EventId,
                    ExecutionResultCode.AgentInactive,
                    "Postprocess directive speaker is no longer active.");
                return;
            }
            if (_npcReplyLocalResolvedAt.TryGetValue(speaker.Index, out double localAt) &&
                localAt >= captured.ReplyCapturedAtMissionTime)
            {
                FinishNoAction(
                    captured.EventId,
                    "Reply was already resolved locally; postprocess directive ignored.");
                return;
            }
            if (TryGetPendingNpcConsent(speaker, now, out _, out _))
            {
                FinishNoAction(
                    captured.EventId,
                    "Speaker has a pending consent request; postprocess directive ignored.");
                return;
            }

            ParseDecision stage = SceneActionsRuntimeHost.Parser.ParseNpcReplyText(
                captured.RawText,
                speaker.Name);
            PendingClassification pending = new PendingClassification
            {
                Captured = captured,
                ClassifierText = stage.ClassifierText ?? string.Empty,
                AllowedIntentKeys = BuildEffectiveClassifierAllowList(),
                SessionGeneration = _sessionGeneration,
                ExpiresAtMissionTime = now
            };
            _npcReplyDirectiveAppliedAt[speaker.Index] = now;
            ApplyClassifierOutput(
                pending,
                NpcReplyDirectiveTagV1.ToClassifierProtocolLine(captured.DirectiveValue),
                ResolverSource.NpcPostprocessDirective,
                now,
                "Postprocess directive returned NONE.");
        }

        private void RecordNpcReplyLocalResolution(
            CapturedSceneActionEvent captured,
            ParseDecision decision,
            double now)
        {
            if (captured?.InputSource == SceneInputSource.NpcSceneShoutReply &&
                captured.DirectiveValue == null &&
                decision?.Resolver != ResolverSource.NpcPostprocessDirective &&
                captured.Speaker != null)
            {
                _npcReplyLocalResolvedAt[captured.Speaker.Index] = now;
            }
        }

        private bool IsDirectiveEcho(Agent speaker, double now)
        {
            return speaker != null &&
                   _npcReplyDirectiveAppliedAt.TryGetValue(speaker.Index, out double appliedAt) &&
                   now - appliedAt <= DirectiveEchoWindowSeconds;
        }

        private void ClearNpcReplyDirectiveLedgers()
        {
            _npcReplyLocalResolvedAt.Clear();
            _npcReplyDirectiveAppliedAt.Clear();
        }
    }
}
