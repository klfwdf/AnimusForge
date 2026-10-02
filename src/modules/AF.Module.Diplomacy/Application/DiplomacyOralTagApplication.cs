using System;
using System.Text.RegularExpressions;

namespace AnimusForge;

internal interface IDiplomacyOralTagSource
{
    bool HasSpeaker { get; }
    bool IsAvailable { get; }
    string SpeakerHeroId { get; }
    string DeclareWar(string payload);
    string MakePeace(string payload);
    string IndependentClanPeace(string payload);
    string FormAlliance(string payload);
    string BreakAlliance(string payload);
    string MakeTrade(string payload);
    string CancelTrade(string payload);
    void Log(string message);
}

internal static class DiplomacyOralTagApplication
{
    private static readonly Regex TagRegex = new Regex(
        @"\[ACTION:DIPLOMACY:([A-Z_]+)(?::([^\]]+))?\]",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    internal static void Process<TSource>(TSource source, ref string responseText)
        where TSource : struct, IDiplomacyOralTagSource
    {
        if (!source.HasSpeaker || string.IsNullOrEmpty(responseText)) return;
        if (responseText.IndexOf("DIPLOMACY", StringComparison.OrdinalIgnoreCase) < 0) return;
        if (!source.IsAvailable)
        {
            source.Log("[Dispatch] Instance is null, abort.");
            return;
        }

        int matchCount = 0;
        responseText = TagRegex.Replace(responseText, match =>
        {
            matchCount++;
            return ProcessSingle(source, match.Groups[1].Value, match.Groups[2].Value);
        });
        if (matchCount > 0)
        {
            responseText = TagRegex.Replace(responseText, "");
            responseText = responseText.Trim();
        }
    }

    private static string ProcessSingle<TSource>(TSource source, string action, string payload)
        where TSource : struct, IDiplomacyOralTagSource
    {
        try
        {
            source.Log($"[Tag] action={action} payload={payload} npc={source.SpeakerHeroId}");
            switch (action.ToUpperInvariant())
            {
                case "DECLARE_WAR": return source.DeclareWar(payload);
                case "MAKE_PEACE": return source.MakePeace(payload);
                case "INDEPENDENT_CLAN_PEACE": return source.IndependentClanPeace(payload);
                case "FORM_ALLIANCE": return source.FormAlliance(payload);
                case "BREAK_ALLIANCE": return source.BreakAlliance(payload);
                case "MAKE_TRADE": return source.MakeTrade(payload);
                case "CANCEL_TRADE": return source.CancelTrade(payload);
                default:
                    source.Log($"[Tag] Unknown action: {action}");
                    return "";
            }
        }
        catch (Exception ex)
        {
            source.Log($"[Tag Error] action={action}: {ex.Message}");
            return "";
        }
    }
}
