using AnimusForge;
using System;
using System.Collections.Generic;
using AnimusForge.DiplomacyDialogue;
using AnimusForge.Refactor.Domain;

namespace AnimusForge;

// Optional narrow internal port; independent NPC diplomacy keeps its own gates.
internal interface IWorldDiplomacySubjectReleaseHost
{
    Dictionary<string, string> CapturePlayerSubjectReleaseTokens(string suzerainId);
    string PlayerSubjectReleaseToken(string suzerainId, string subjectId);
    WorldDiplomacyImmediateActionReceipt ReleasePlayerSubject(string suzerainId, string subjectId, string token);
}

internal sealed partial class WorldDiplomacyOrchestration
{
    private IWorldDiplomacySubjectReleaseHost SubjectReleaseHost => _host as IWorldDiplomacySubjectReleaseHost;

    internal bool CanReleasePlayerSubject(string author, string target) => author != null && target != null
        && author != target && !string.IsNullOrEmpty(SubjectReleaseHost?.PlayerSubjectReleaseToken(author, target));

    internal string BuildPlayerSubjectReleaseContext(string rulerId, string subjectId)
    {
        if (!DialogueRulerIsCurrent(subjectId, rulerId)) return "";
        string player = _host.PlayerKingdomId();
        string token = SubjectReleaseHost?.PlayerSubjectReleaseToken(player, subjectId);
        if (string.IsNullOrEmpty(token)) return "";
        return string.Format(DiplomacyMiscPromptTexts.Get("SubjectRelease_PromptInstructionFormat", "[Suzerain Actively Releases Subject Kingdom]\nThe player is currently the ruler of the suzerain kingdom; the current NPC kingdom is its direct subject kingdom. Executable immediately when the player explicitly decides to release this round, requiring neither NPC consent nor subject independent diplomacy. Asking conditions, hypotheses, negations, or unilateral NPC requests do not constitute player decisions. Release does not automatically make peace, dissolve other alliances, or establish new treaties.\nThis round release tag=[ACTION:DIPLOMACY:RELEASE_SUBJECT:target={0};agreement={1}]; target and agreement must be copied verbatim without substituting COMMIT, BreakAlliance, or Alliance."), subjectId, token);
    }

    internal string SubmitPlayerSubjectRelease(string rulerId, string npcKingdomId, string payload, DialogueInteractionOrigin origin)
    {
        if (!DiplomacySubjectReleasePayload.TryParse(payload, out string target, out string token))
            return DiplomacyMiscPromptTexts.Get("SubjectRelease_InvalidFormat", "Release not executed: target or treaty token format is invalid.");
        string player = _host.PlayerKingdomId();
        if (origin == null || string.IsNullOrWhiteSpace(origin.PlayerText)
            || target != npcKingdomId || !DialogueRulerIsCurrent(npcKingdomId, rulerId)
            || SubjectReleaseHost?.PlayerSubjectReleaseToken(player, target) != token)
        {
            _host.Log("subject release rejected channel=" + origin?.Channel + " target=" + target
                + " reason=source_authority_or_agreement_changed");
            return DiplomacyMiscPromptTexts.Get("SubjectRelease_PreconditionsChanged", "Release not executed: missing player command this round, or ruler authority, target, or direct treaty has changed.");
        }
        string declaration = string.Format(DiplomacyMiscPromptTexts.Get("SubjectRelease_DeclarationBodyFormat", "{0}, as the suzerain kingdom, has decided to terminate the vassalage treaty with {1}, releasing the kingdom and recognizing its independence."), _host.PartyNameOrEmpty(player), _host.PartyNameOrEmpty(target));
        var document = CreateDocument(player, target, DiplomacyMiscPromptTexts.Get("SubjectRelease_DeclarationTitle", "Suzerain releases subject kingdom"), declaration,
            "player_subject_release", true, false, "");
        document.Intent = "release_subject"; document.Commitment = "binding";
        document.AnalysisStatus = "success"; document.RequiresResponse = false;
        AddDocument(document);
        // Deterministic action: no NPC formal consent and no second model request.
        var receipt = ExecutePlayerSubjectRelease(player, target, document);
        try { PublishPlayerAuthoredDocumentImmediately(document); }
        catch (Exception ex) { _host.Log("subject release publication callback failed document=" + document.DocumentId + " error=" + ex.Message); }
        document.IsReadyForPublication = true;
        document.AnalysisStatus = "success";
        try { AppendCanonicalDocumentEvents(document); }
        catch (Exception ex) { ScheduleDeferredCanonicalHistoryRetry(document.DocumentId); _host.Log("subject release history deferred: " + ex.Message); }
        if (receipt.Applied)
        {
            try { DeliverDialogueDeclarationToCounterparty(document, target); }
            catch (Exception ex) { _host.Log("subject release court receipt deferred: " + ex.Message); }
            try
            {
                var memory = DialogueHost?.CommitFact(rulerId, document.DocumentId + ":subject_release", receipt.Message,
                    _host.CurrentDay(), "", npcName: _host.PartyRulerName(target));
                _host.Log("subject release personal memory document=" + document.DocumentId + " receipt=" + memory?.Status);
            }
            catch (Exception ex) { _host.Log("subject release personal memory failed: " + ex.Message); }
        }
        _host.Log("subject release channel=" + origin.Channel + " document=" + document.DocumentId
            + " target=" + target + " applied=" + receipt.Applied + " result=" + receipt.Message);
        return receipt.Message;
    }

    internal bool ValidatePlayerSubjectRelease(WorldDiplomacyDocument document, string author, string target, out string reason)
    {
        reason = "subject_release_authority_or_agreement_changed";
        if (document?.IsPlayerAuthored != true || document.AuthorKingdomId != author
            || document.AuthorRulerId != _host.PartyRulerId(author)
            || document.SubjectReleaseTokens == null || target == null
            || !document.SubjectReleaseTokens.TryGetValue(target, out string token) || string.IsNullOrEmpty(token)
            || SubjectReleaseHost?.PlayerSubjectReleaseToken(author, target) != token) return false;
        reason = ""; return true;
    }

    private WorldDiplomacyImmediateActionReceipt ExecutePlayerSubjectRelease(string author, string target, WorldDiplomacyDocument document)
    {
        if (!ValidatePlayerSubjectRelease(document, author, target, out string reason))
        {
            if (document != null) document.MechanicalResult = DiplomacyMiscPromptTexts.Get("SubjectRelease_AuthorityOrTreatyChanged", "Release not executed: ruler authority or original vassalage treaty has changed.");
            _host.Log("subject release blocked document=" + document?.DocumentId + " reason=" + reason);
            return new WorldDiplomacyImmediateActionReceipt(false, document?.MechanicalResult ?? reason);
        }
        var receipt = SubjectReleaseHost.ReleasePlayerSubject(author, target, document.SubjectReleaseTokens[target]);
        document.MechanicalResult = receipt.Message;
        if (receipt.Applied) document.ChangedDiplomaticState = true;
        if (!string.IsNullOrEmpty(receipt.Diagnostic)) _host.Log(receipt.Diagnostic);
        return receipt;
    }
}
