using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AnimusForge.SiegeAftermathIntervention;

/// <summary>
/// Enforces the town postprocessor's one-primary-action contract after model output normalization.
/// </summary>
public static class TownPostprocessDecisionValidator
{
    private static readonly Regex TagPattern = new Regex(
        @"\[[^\r\n\]]+\]",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private const string MoodPrefix = "[ACTION:MOOD:";

    public static string KeepSinglePrimaryAction(string normalizedTags)
    {
        if (string.IsNullOrWhiteSpace(normalizedTags))
        {
            return string.Empty;
        }

        string primaryAction = string.Empty;
        string moodAction = string.Empty;
        bool hasConflictingPrimaryActions = false;
        foreach (Match match in TagPattern.Matches(normalizedTags))
        {
            string tag = (match.Value ?? string.Empty).Trim();
            if (tag.Length == 0)
            {
                continue;
            }

            if (tag.StartsWith(MoodPrefix, StringComparison.OrdinalIgnoreCase))
            {
                if (moodAction.Length == 0)
                {
                    moodAction = tag;
                }
                continue;
            }

            if (primaryAction.Length == 0)
            {
                primaryAction = tag;
            }
            else if (!string.Equals(primaryAction, tag, StringComparison.OrdinalIgnoreCase))
            {
                hasConflictingPrimaryActions = true;
            }
        }

        var accepted = new List<string>(2);
        if (primaryAction.Length > 0 && !hasConflictingPrimaryActions)
        {
            accepted.Add(primaryAction);
        }
        if (moodAction.Length > 0)
        {
            accepted.Add(moodAction);
        }
        return string.Join("\n", accepted);
    }
}
