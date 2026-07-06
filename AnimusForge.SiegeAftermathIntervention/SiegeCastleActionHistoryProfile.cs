using System;
using System.Collections.Generic;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Dependency-free runtime prompt history for castle aftermath actions.
/// The bridge supplies the mission-local applied action history; this profile turns it into a compact
/// ordered context line so later LLM postprocess rounds can react to earlier prisoner treatment.
/// </summary>
public static class SiegeCastleActionHistoryProfile
{
    public const string RuntimeHistoryHeader = "【城堡已执行军务】";

    public const string NoActionText = "暂无；如果玩家尚未下令，不要主动编造已完成军务。";

    public const string DuplicateAvoidanceText = "后续回应不得重复输出已执行标签，除非玩家明确修改命令；重复标签不会再次叠加状态；必须承认标签触发顺序，例如先劳役后收编时，战俘会记得曾被派去劳役。";

    public static string BuildRuntimeHistoryLine(IEnumerable<SiegeCastleAftermathActionKind> appliedActions)
    {
        string list = BuildActionList(appliedActions);
        if (string.IsNullOrWhiteSpace(list))
        {
            list = NoActionText;
        }

        return RuntimeHistoryHeader + list + "。" + DuplicateAvoidanceText;
    }

    public static string BuildActionList(IEnumerable<SiegeCastleAftermathActionKind> appliedActions)
    {
        if (appliedActions == null)
        {
            return string.Empty;
        }

        var seen = new HashSet<SiegeCastleAftermathActionKind>();
        var labels = new List<string>();
        foreach (SiegeCastleAftermathActionKind action in appliedActions)
        {
            if (action == SiegeCastleAftermathActionKind.Unknown || !seen.Add(action))
            {
                continue;
            }

            labels.Add(SiegeCastleAftermathProfile.GetActionLabel(action));
        }

        return labels.Count == 0 ? string.Empty : string.Join("、", labels);
    }
}
