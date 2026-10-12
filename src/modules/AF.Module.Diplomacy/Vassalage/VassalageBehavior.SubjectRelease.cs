using AnimusForge;
using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;

namespace AnimusForge;

internal sealed partial class VassalageBehavior
{
    // These queries run at prompt/document/action boundaries, never from Tick.
    internal string GetPlayerSubjectReleaseToken(Kingdom suzerain, Kingdom subject)
    {
        if (!IsValidKingdom(suzerain) || !IsValidKingdom(subject) || suzerain == subject
            || suzerain != GetPlayerKingdom() || Hero.MainHero?.IsAlive != true
            || suzerain.RulingClan?.Leader != Hero.MainHero) return "";
        var agreement = GetAnyVassalAgreement(subject);
        return agreement?.ResolveSuzerain() == suzerain
            && !string.IsNullOrWhiteSpace(agreement.ReleaseIdentity) ? agreement.ReleaseIdentity : "";
    }

    internal Dictionary<string, string> CapturePlayerSubjectReleaseTokens(Kingdom suzerain)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!IsValidKingdom(suzerain) || suzerain != GetPlayerKingdom()
            || Hero.MainHero?.IsAlive != true || suzerain.RulingClan?.Leader != Hero.MainHero) return result;
        foreach (var agreement in _agreementsByVassalId.Values)
        {
            if (agreement == null || !agreement.IsValid() || agreement.SuzerainKingdomId != suzerain.StringId) continue;
            var subject = agreement.ResolveVassal();
            string token = GetPlayerSubjectReleaseToken(suzerain, subject);
            if (!string.IsNullOrEmpty(token)) result[subject.StringId] = token;
        }
        return result;
    }

    internal bool TryReleasePlayerSubject(Kingdom suzerain, Kingdom subject, string expectedToken, out string reason)
    {
        string current = GetPlayerSubjectReleaseToken(suzerain, subject);
        if (string.IsNullOrWhiteSpace(expectedToken) || current != expectedToken)
        {
            reason = DiplomacyMiscPromptTexts.Get("SubjectRelease_PreconditionsChanged", "Release not executed: player ruler authority, direct vassal relationship, or treaty token has changed.");
            Logger.Log("Vassalage", "subject release rejected suzerain=" + suzerain?.StringId
                + " subject=" + subject?.StringId + " reason=authority_or_agreement_changed");
            return false;
        }
        var agreement = GetAnyVassalAgreement(subject);
        // Preserve native wars and other agreements. The existing owner ends all
        // tribute, independence, policy and pending protection obligations.
        reason = string.Format(DiplomacyMiscPromptTexts.Get("SubjectRelease_VassalageTerminatedFormat", "{0} has actively released {1}; the vassalage treaty is terminated and the kingdom restores independence."), GetKingdomDisplayName(suzerain, DiplomacyMiscPromptTexts.Get("Vassalage_SuzerainKingdom", "suzerain kingdom")), GetKingdomDisplayName(subject, DiplomacyMiscPromptTexts.Get("Vassalage_SubjectKingdom", "subject kingdom")));

        BreakAgreement(agreement, "suzerain_released_subject", reason);
        bool released = GetAnyVassalAgreement(subject) == null;
        if (!released) reason = DiplomacyMiscPromptTexts.Get("SubjectRelease_UnconfirmedStillExists", "Release unconfirmed: vassalage relationship still exists.");
        return released;
    }

    private bool IsSubjectWarSyncCurrent(string reason, Kingdom suzerain, Kingdom enemy)
    {
        const string prefix = "agreement_sync_subject_war@";
        if ((reason ?? "").StartsWith(prefix, StringComparison.Ordinal))
        {
            string[] source = reason.Substring(prefix.Length).Split('@');
            return source.Length == 2 && _agreementsByVassalId.TryGetValue(source[0], out var agreement)
                && agreement != null && agreement.ReleaseIdentity == source[1]
                && agreement.ResolveSuzerain() == suzerain && IsAtWar(agreement.ResolveVassal(), enemy);
        }
        if (reason != "agreement_sync_subject_war") return true;
        // Legacy jobs lack a source identity: require a still-active subject war.
        foreach (var agreement in _agreementsByVassalId.Values)
            if (agreement != null && agreement.IsValid() && agreement.ResolveSuzerain() == suzerain
                && IsAtWar(agreement.ResolveVassal(), enemy)) return true;
        return false;
    }
}
