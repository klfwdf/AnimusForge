using System;
using System.Collections.Generic;
using AnimusForge.SceneActions.Core;
using TaleWorlds.MountAndBlade;

namespace AnimusForge.XihaiAction
{
    public static class SceneActionsApiV1
    {
        public static IReadOnlyList<SceneActionContractEntryV1> GetLogicalActions()
        {
            return SceneActionFrameworkV1.LogicalActions;
        }

        public static IDisposable RegisterClassifier(
            string providerId,
            IAuxiliaryTextClassifierV1 classifier)
        {
            return SceneActionsRuntimeHost.RegisterClassifier(providerId, classifier);
        }

        public static bool SubmitNpcReply(
            Guid replyId,
            Mission mission,
            Agent speaker,
            string semanticText,
            double submittedAtMissionTime)
        {
            if (replyId == Guid.Empty ||
                mission == null ||
                speaker == null ||
                double.IsNaN(submittedAtMissionTime) ||
                double.IsInfinity(submittedAtMissionTime) ||
                submittedAtMissionTime < 0d)
            {
                return false;
            }
            return SceneActionsRuntimeHost.SubmitNpcReply(
                replyId,
                mission,
                speaker,
                semanticText,
                submittedAtMissionTime);
        }
    }
}
