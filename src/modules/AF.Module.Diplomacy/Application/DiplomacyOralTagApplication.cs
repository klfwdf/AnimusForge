using AnimusForge;
using System;
using System.Collections.Generic;
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

internal interface IDiplomacySubjectReleaseTagSource
{
    string ReleaseSubject(string payload);
}

internal interface IDiplomacyBilateralTagSource
{
    bool TryProcessBilateral(string action, string payload);
}

internal static class DiplomacyOralTagApplication
{
    private static readonly Regex TagRegex = new Regex(
        @"\[ACTION:DIPLOMACY:([A-Z_]+)(?::([^\]]+))?\]",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex HiddenTagRegex = new Regex(
        @"\[ACTION:DIPLOMACY[^\]\r\n]*\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    internal static void Process<TSource>(TSource source, ref string responseText, List<string> receipts = null)
        where TSource : struct, IDiplomacyOralTagSource
    {
        bool reviewing = false;
        try
        {
            reviewing = source.HasSpeaker && source.IsAvailable && source is IDiplomacyCommitmentReviewSource review
                && review.CaptureCommitmentReview(responseText);
        }
        catch (Exception ex)
        {
            // Isolate the retained complex review owner; its failure must not
            // suppress independent lobbyist/direct items or enable its fallback.
            reviewing = true;
            source.Log("[SpeechReview] capture error=" + ex.GetType().Name);
        }
        if (string.IsNullOrEmpty(responseText) || responseText.IndexOf("DIPLOMACY", StringComparison.OrdinalIgnoreCase) < 0) return;
        if (!source.HasSpeaker || !source.IsAvailable)
        {
            source.Log("[Dispatch] Instance is null, abort.");
            responseText = HiddenTagRegex.Replace(responseText, "").Trim();
            return;
        }

        responseText = TagRegex.Replace(responseText, match =>
        {
            // The review owns remaining complex self-country matters. Immediate actions pass
            // through their existing authority/consent checks in the executor.
            string action = match.Groups[1].Value;
            // Country-based ownership and lossless format normalization precede
            // suppression by the retained complex-matter review.
            if (source is IDiplomacyLobbyistTagSource lobbyist)
            {
                try { if (lobbyist.TryProcessLobbyist(action, match.Groups[2].Value)) return ""; }
                catch (Exception ex)
                { source.Log("[Lobbyist] action=" + action + " error=" + ex.GetType().Name); return ""; }
            }
            if (source is IDiplomacyBilateralTagSource bilateral)
            {
                try { if (bilateral.TryProcessBilateral(action, match.Groups[2].Value)) return ""; }
                catch (Exception ex)
                {
                    // An effect may already have a durable receipt. Never fall
                    // through to another executor or abandon remaining tags.
                    source.Log("[Bilateral] action=" + action + " error=" + ex.GetType().Name);
                    return "";
                }
            }
            bool immediate = string.Equals(action, "RELEASE_SUBJECT", StringComparison.OrdinalIgnoreCase)
                || string.Equals(action, "INDEPENDENT_CLAN_PEACE", StringComparison.OrdinalIgnoreCase)
                || (string.Equals(action, "DECLARE_WAR", StringComparison.OrdinalIgnoreCase)
                    && source is IDiplomacyCommitmentTagSource actor
                    && !string.Equals(match.Groups[2].Value.Split(':')[0].Trim(), actor.SpeakerKingdomId, StringComparison.OrdinalIgnoreCase));
            if (reviewing && !immediate)
            { source.Log("[Dispatch] not executed by immediate owners; retained review boundary action=" + action); return ""; }
            string status = ProcessSingle(source, match.Groups[1].Value, match.Groups[2].Value);
            if (!string.IsNullOrWhiteSpace(status)) source.Log("[Outcome] " + status);
            // All modes keep outcomes in diagnostics/owned facts and notifications.
            // Never populate the legacy prose receipt list, including rejected/late tags.
            return "";
        });
        // Malformed/unrecognized directives are hidden without attempting effects.
        responseText = HiddenTagRegex.Replace(responseText, "").Trim();
    }

    // Signature retained for old callers and saved DTOs. Receipts are never NPC prose;
    // in particular, reopening an old letter must not append its saved receipts again.
    internal static string ComposeReceiptText(string body, IReadOnlyList<string> receipts, Func<string, string> project = null)
    {
        return DiplomacyReplyTextRules.CleanNpcText(body);
    }

    private static string ProcessSingle<TSource>(TSource source, string action, string payload)
        where TSource : struct, IDiplomacyOralTagSource
    {
        try
        {
            source.Log($"[Tag] action={action} payload={payload} npc={source.SpeakerHeroId}");
            var formal = source is IDiplomacyCommitmentTagSource candidate && candidate.UseFormalCommitments ? candidate : null;
            switch (action.ToUpperInvariant())
            {
                case "RELEASE_SUBJECT": return source is IDiplomacySubjectReleaseTagSource release
                    ? release.ReleaseSubject(payload) : DiplomacyMiscPromptTexts.Get("OralTag_VassalageUnavailable", "Release not executed: vassalage system is unavailable.");
                case "COMMIT": return formal != null ? formal.SubmitCommitment(payload)
                    : source is IDiplomacyCommitmentTagSource direct
                        ? DiplomacyNativeCommitmentApplication.Execute(source, direct.SpeakerKingdomId, payload) : "";
                case "COMMITMENT": return formal != null ? formal.ControlCommitment(payload)
                    : DiplomacyMiscPromptTexts.Get("OralTag_AiDisabled", "AI diplomacy is disabled; existing document agreements cannot be modified in the current mode.");
                case "DECLARE_WAR":
                    return formal != null
                        && string.Equals((payload ?? "").Split(':')[0].Trim(), formal.SpeakerKingdomId, StringComparison.OrdinalIgnoreCase)
                        ? formal.SubmitLegacyCommitment("DeclareWar", payload) : source.DeclareWar(payload);
                case "MAKE_PEACE": return formal != null ? formal.SubmitLegacyCommitment("Peace", payload) : source.MakePeace(payload);
                case "INDEPENDENT_CLAN_PEACE": return source.IndependentClanPeace(payload);
                case "FORM_ALLIANCE": return formal != null ? formal.SubmitLegacyCommitment("Alliance", payload) : source.FormAlliance(payload);
                case "BREAK_ALLIANCE": return formal != null ? formal.SubmitLegacyCommitment("BreakAlliance", payload) : source.BreakAlliance(payload);
                case "MAKE_TRADE": return formal != null ? formal.SubmitLegacyCommitment("Trade", payload) : source.MakeTrade(payload);
                case "CANCEL_TRADE": return formal != null ? formal.SubmitLegacyCommitment("CancelTrade", payload) : source.CancelTrade(payload);
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
