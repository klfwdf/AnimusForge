using System.Collections.Generic;
using System.Linq;
using AnimusForge.SceneActions.Core;
using TaleWorlds.MountAndBlade;

namespace AnimusForge.XihaiAction
{
    /// <summary>
    /// Bounded one-turn player context and implicit-emotion inference. Context remains
    /// Mission-generation scoped and is consumed once by the addressed NPC.
    /// </summary>
    internal sealed partial class SceneActionsMissionBehavior
    {
        private void RememberRecentPlayerContext(
            CapturedSceneActionEvent captured,
            double now)
        {
            string text = (captured?.RawText ?? string.Empty).Trim();
            if (text.Length == 0)
            {
                return;
            }
            if (text.Length > MaxRecentPlayerContextChars)
            {
                text = text.Substring(0, MaxRecentPlayerContextChars);
            }

            List<Agent> targets = new List<Agent>();
            if (captured.FramedTargets != null)
            {
                targets.AddRange(captured.FramedTargets);
            }
            if (captured.PrimaryTarget != null)
            {
                targets.Add(captured.PrimaryTarget);
            }
            foreach (Agent target in targets
                         .Where(agent => agent != null)
                         .GroupBy(agent => agent.Index)
                         .Select(group => group.First()))
            {
                if (ReferenceEquals(target, captured.Player) ||
                    !ReferenceEquals(target.Mission, Mission))
                {
                    continue;
                }
                _recentPlayerContexts[target.Index] = new RecentPlayerContext
                {
                    Agent = target,
                    Text = text,
                    ExpiresAtMissionTime = now + RecentPlayerContextTtlSeconds
                };
            }
        }

        private string ConsumeRecentPlayerContext(Agent speaker, double now)
        {
            if (speaker == null ||
                !_recentPlayerContexts.TryGetValue(
                    speaker.Index,
                    out RecentPlayerContext context))
            {
                return string.Empty;
            }
            _recentPlayerContexts.Remove(speaker.Index);
            if (!ReferenceEquals(context.Agent, speaker) ||
                !ReferenceEquals(speaker.Mission, Mission) ||
                now > context.ExpiresAtMissionTime)
            {
                return string.Empty;
            }
            return context.Text ?? string.Empty;
        }

        private void CleanupRecentPlayerContexts(double now)
        {
            foreach (KeyValuePair<int, RecentPlayerContext> entry in
                     _recentPlayerContexts.ToArray())
            {
                RecentPlayerContext context = entry.Value;
                if (context == null ||
                    context.Agent == null ||
                    !ReferenceEquals(context.Agent.Mission, Mission) ||
                    now > context.ExpiresAtMissionTime)
                {
                    _recentPlayerContexts.Remove(entry.Key);
                }
            }
        }

        private void RemoveRecentPlayerContext(Agent agent)
        {
            if (agent != null &&
                _recentPlayerContexts.TryGetValue(agent.Index, out RecentPlayerContext context) &&
                ReferenceEquals(context.Agent, agent))
            {
                _recentPlayerContexts.Remove(agent.Index);
            }
        }

        private bool TryResolveImplicitEmotion(
            CapturedSceneActionEvent captured,
            ParseDecision fallback,
            string previousPlayerText,
            out ParseDecision decision)
        {
            decision = null;
            List<string> allowed = BuildEffectiveClassifierAllowList();
            if (!ImplicitEmotionInferenceV1.TryInfer(
                    previousPlayerText,
                    fallback?.ClassifierText,
                    captured?.RawText,
                    allowed,
                    out ImplicitEmotionDecisionV1 inferred))
            {
                return false;
            }

            decision = ParseDecision.MatchV4(
                inferred.IntentKey,
                null,
                ResolverSource.ImplicitEmotionInference);
            SceneActionsLog.Info(
                "INFERENCE",
                "RequestId=" + captured.EventId.ToString("N") +
                " SessionGeneration=" + _sessionGeneration +
                " Agent=" + (captured.Speaker?.Index ?? -1) +
                " Intent=" + inferred.IntentKey +
                " Score=" + inferred.Score +
                " Evidence=" + string.Join(",", inferred.Evidence));
            return true;
        }
    }
}
