using System;
using System.Collections.Generic;
using AnimusForge.SceneActions.Core;
using TaleWorlds.MountAndBlade;

namespace AnimusForge.XihaiAction
{
    public static class SceneActionsApiV2
    {
        public static IReadOnlyList<SceneActionContractEntryV2> GetLogicalActions()
        {
            return SceneActionFrameworkV2.LogicalActions;
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
