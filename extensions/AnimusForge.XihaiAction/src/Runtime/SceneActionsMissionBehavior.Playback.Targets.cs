using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AnimusForge.SceneActions.Core;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge.XihaiAction
{
    internal sealed partial class SceneActionsMissionBehavior
    {
        private static bool ShouldStaggerBatch(
            ParseDecision decision,
            int requestedCount,
            SceneActionSettings settings)
        {
            return SceneActionPermissionRouter.ShouldStaggerNpcBatch(
                decision,
                requestedCount,
                settings);
        }
        private List<Agent> ResolveTargets(CapturedSceneActionEvent captured, TargetMode mode)
        {
            IEnumerable<Agent> source;
            if (captured.InputSource == SceneInputSource.NpcSceneShoutReply)
            {
                source = new[] { captured.Speaker };
            }
            else if (mode == TargetMode.Player)
            {
                source = new[] { captured.Player };
            }
            else if (mode == TargetMode.Primary)
            {
                source = new[] { captured.PrimaryTarget };
            }
            else
            {
                source = captured.FramedTargets ?? Array.Empty<Agent>();
            }

            List<Agent> result = new List<Agent>();
            foreach (Agent agent in source)
            {
                if (agent == null ||
                    result.Any(existing => ReferenceEquals(existing, agent)) ||
                    (captured.InputSource == SceneInputSource.PlayerSceneShout &&
                     mode != TargetMode.Player &&
                     ReferenceEquals(agent, captured.Player)))
                {
                    continue;
                }
                result.Add(agent);
            }
            return result;
        }
    }
}