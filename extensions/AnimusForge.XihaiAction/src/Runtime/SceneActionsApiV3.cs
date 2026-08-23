using System;
using System.Collections.Generic;
using AnimusForge.SceneActions.Core;
using TaleWorlds.MountAndBlade;

namespace AnimusForge.XihaiAction
{
    public static class SceneActionsApiV3
    {
        public static IReadOnlyList<SceneActionContractEntryV3> GetLogicalActions()
        {
            return SceneActionFrameworkV3.LogicalActions;
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
            return SceneActionsApiV1.SubmitNpcReply(
                replyId,
                mission,
                speaker,
                semanticText,
                submittedAtMissionTime);
        }
    }
}
