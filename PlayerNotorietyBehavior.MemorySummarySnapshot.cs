using System;
using System.Linq;
using TaleWorlds.Library;

namespace AnimusForge;

public sealed partial class PlayerNotorietyBehavior
{
    // Capture once with the summary prompt. The returned renderer only closes over strings.
    internal static Func<string, string> CaptureMemorySummaryHistoryRenderer()
    {
        if (!TWParallel.IsMainThread())
        {
            throw new InvalidOperationException("Memory history names must be captured on the main thread.");
        }
        string playerName = BuildPlayerHistoryDisplayName();
        string[] aliases = BuildPlayerHistoryAnonymousAliases(playerName).ToArray();
        string publicName = NormalizeLine(MyBehavior.BuildPlayerPublicDisplayNameForExternal());
        return rawText =>
        {
            string text = RenderPlayerActionTextForPrompt(rawText, playerName);
            foreach (string alias in aliases)
            {
                text = text.Replace(alias, playerName);
            }
            text = StripPlayerInternalMarkers(text);
            if (!string.IsNullOrWhiteSpace(publicName) && !string.Equals(publicName, playerName, StringComparison.Ordinal))
            {
                text = text.Replace(publicName, playerName);
            }
            return text.Replace("玩家", playerName);
        };
    }
}
