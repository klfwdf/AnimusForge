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
        return "【宗主主动释放臣属国】\n玩家是宗主国当前国王；当前NPC国家是其直属臣属国。玩家本轮明确决定释放时即可执行，无须NPC同意或臣属国独立外交权。"
            + "询问条件、假设、否定或NPC单方面请求不构成玩家决定。释放不会自动停战、解除其他同盟或建立新条约。\n"
            + "本轮释放标签=[ACTION:DIPLOMACY:RELEASE_SUBJECT:target=" + subjectId + ";agreement=" + token
            + "]；target与agreement必须原样复制，不得使用COMMIT、BreakAlliance或Alliance代替。";
    }

    internal string SubmitPlayerSubjectRelease(string rulerId, string npcKingdomId, string payload, DialogueInteractionOrigin origin)
    {
        if (!DiplomacySubjectReleasePayload.TryParse(payload, out string target, out string token))
            return "释放未执行：目标或条约凭证格式无效。";
        string player = _host.PlayerKingdomId();
        if (origin == null || string.IsNullOrWhiteSpace(origin.PlayerText)
            || target != npcKingdomId || !DialogueRulerIsCurrent(npcKingdomId, rulerId)
            || SubjectReleaseHost?.PlayerSubjectReleaseToken(player, target) != token)
        {
            _host.Log("subject release rejected channel=" + origin?.Channel + " target=" + target
                + " reason=source_authority_or_agreement_changed");
            return "释放未执行：缺少本轮玩家指令，或国王权限、目标、直属条约已经变化。";
        }
        string declaration = _host.PartyNameOrEmpty(player) + "作为宗主国，决定终止与"
            + _host.PartyNameOrEmpty(target) + "的臣属条约，释放该国并承认其独立。";
        var document = CreateDocument(player, target, "宗主释放臣属国", declaration,
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
            if (document != null) document.MechanicalResult = "释放未执行：国王权限或原臣属条约已经变化。";
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
