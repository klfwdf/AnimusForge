using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.SceneActions.Core;
using AnimusForge.XihaiAction;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public partial class ShoutBehavior
{
internal static void RecordSceneActionReplyCapture(int agentIndex) => SceneActionDirectiveController.RecordSceneActionReplyCapture(agentIndex);

internal static IReadOnlyList<string> TryOfferSceneActionDirective(int targetAgentIndex, string rawReply) => SceneActionDirectiveController.TryOfferSceneActionDirective(targetAgentIndex, rawReply);

internal static List<PostprocessRuleEntry> BuildSceneActionDirectiveRules(IReadOnlyList<string> offeredKeys) => SceneActionDirectiveController.BuildSceneActionDirectiveRules(offeredKeys);

internal static string NormalizeSceneActionDirectiveTag(string raw, IReadOnlyList<string> offeredKeys) => SceneActionDirectiveController.NormalizeSceneActionDirectiveTag(raw, offeredKeys);

internal static string ExtractSceneActionDirective(ref string text, int targetAgentIndex) => SceneActionDirectiveController.ExtractSceneActionDirective(ref text, targetAgentIndex);

internal static void SubmitSceneActionDirective(string value, int targetAgentIndex, string rawReply) => SceneActionDirectiveController.SubmitSceneActionDirective(value, targetAgentIndex, rawReply);
}
