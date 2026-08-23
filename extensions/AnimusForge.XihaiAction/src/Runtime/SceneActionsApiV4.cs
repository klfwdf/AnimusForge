using System;
using System.Collections.Generic;
using AnimusForge.SceneActions.Core;
using TaleWorlds.MountAndBlade;

namespace AnimusForge.XihaiAction
{
    public static class SceneActionsApiV4
    {
        public static IReadOnlyList<SceneActionContractEntryV4> GetLogicalActions()
        {
            return SceneActionFrameworkV4.LogicalActions;
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
