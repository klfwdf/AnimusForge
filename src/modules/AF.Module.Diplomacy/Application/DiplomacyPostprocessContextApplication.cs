using System;
using System.Collections.Generic;
using System.Text;

namespace AnimusForge;

internal readonly struct DiplomacyIndependentPeaceContextSnapshot
{
    internal DiplomacyIndependentPeaceContextSnapshot(string playerClanName, int settlementCount, string targetName)
    {
        PlayerClanName = playerClanName;
        SettlementCount = settlementCount;
        TargetName = targetName;
    }
    internal string PlayerClanName { get; }
    internal int SettlementCount { get; }
    internal string TargetName { get; }
}

internal readonly struct DiplomacyKingdomSummary
{
    internal DiplomacyKingdomSummary(string id, string name, bool eliminated)
    { Id = id; Name = name; Eliminated = eliminated; }
    internal string Id { get; }
    internal string Name { get; }
    internal bool Eliminated { get; }
}

internal readonly struct DiplomacyPostprocessKingdomSnapshot
{
    internal DiplomacyPostprocessKingdomSnapshot(bool npcKingdomExists, string npcId, string npcName,
        bool playerKingdomExists, bool playerKingdomEliminated, string playerId, string playerName,
        bool playerIsRuler, bool kingdomsDiffer, IReadOnlyList<DiplomacyKingdomSummary> kingdoms)
    {
        NpcKingdomExists = npcKingdomExists;
        NpcId = npcId;
        NpcName = npcName;
        PlayerKingdomExists = playerKingdomExists;
        PlayerKingdomEliminated = playerKingdomEliminated;
        PlayerId = playerId;
        PlayerName = playerName;
        PlayerIsRuler = playerIsRuler;
        KingdomsDiffer = kingdomsDiffer;
        Kingdoms = kingdoms;
    }
    internal bool NpcKingdomExists { get; }
    internal string NpcId { get; }
    internal string NpcName { get; }
    internal bool PlayerKingdomExists { get; }
    internal bool PlayerKingdomEliminated { get; }
    internal string PlayerId { get; }
    internal string PlayerName { get; }
    internal bool PlayerIsRuler { get; }
    internal bool KingdomsDiffer { get; }
    internal IReadOnlyList<DiplomacyKingdomSummary> Kingdoms { get; }
}

internal interface IDiplomacyPostprocessContextSource
{
    bool HasSpeaker { get; }
    bool TryCaptureIndependentPeace(out DiplomacyIndependentPeaceContextSnapshot snapshot);
    DiplomacyConversationEligibilitySnapshot CaptureEligibility();
    DiplomacyPostprocessKingdomSnapshot CaptureKingdoms();
    string GetAnnexationHint();
    bool ArePlayerAndNpcAtWar();
    int CalculateDailyTribute(bool npcPays);
    void LogFailure(string message);
}

internal interface IDiplomacySubjectReleaseContextSource
{
    string ReleaseSubjectContext();
}

internal static class DiplomacyPostprocessContextApplication
{
    internal static string Build<TSource>(ref TSource source) where TSource : struct, IDiplomacyPostprocessContextSource
    {
        try
        {
            if (!source.HasSpeaker) return "";
            if (source.TryCaptureIndependentPeace(out DiplomacyIndependentPeaceContextSnapshot independent))
                return BuildIndependent(independent);
            DiplomacyConversationEligibilitySnapshot eligibility = source.CaptureEligibility();
            bool allowFullDiplomacy = DiplomacyConversationEligibilityApplication.CanUseFull(eligibility);
            bool allowNpcDeclareWar = DiplomacyConversationEligibilityApplication.CanUseNpcDeclareWar(eligibility);
            if (!allowFullDiplomacy && !allowNpcDeclareWar) return "";
            DiplomacyPostprocessKingdomSnapshot kingdoms = source.CaptureKingdoms();
            if (!kingdoms.NpcKingdomExists) return "";
            string royal = BuildRoyal(ref source, kingdoms, allowFullDiplomacy);
            if (source is IDiplomacyOralPostprocessSource oral && oral.UseFormalCommitments)
                royal += "\n" + oral.OralArrangementContext();
            if (source is IDiplomacySubjectReleaseContextSource release) royal += "\n" + release.ReleaseSubjectContext();
            return royal;
        }
        catch (Exception ex)
        {
            source.LogFailure(ex.Message);
            return "";
        }
    }

    private static string BuildIndependent(DiplomacyIndependentPeaceContextSnapshot value)
    {
        var independent = new StringBuilder();
        independent.AppendLine(DiplomacyPromptTexts.Get("AF_DIP_IndependentPeaceFacts", "[Independent clan peace runtime facts]"));
        independent.AppendLine(DiplomacyPromptTexts.Get("AF_DIP_IndependentClanPrefix", "The player's clan \"") + value.PlayerClanName + DiplomacyPromptTexts.Get("AF_DIP_IndependentClanSuffix", "\" is independent; settlement count: ") + value.SettlementCount);
        independent.AppendLine(DiplomacyPromptTexts.Get("AF_DIP_IndependentEnemyPrefix", "You are the current monarch of the hostile kingdom \"") + value.TargetName + DiplomacyPromptTexts.Get("AF_DIP_IndependentEnemySuffix", "\"."));
        independent.AppendLine(DiplomacyPromptTexts.Get("AF_DIP_IndependentHostility", "The two sides are currently hostile: yes"));
        return independent.ToString().TrimEnd();
    }

    private static string BuildRoyal<TSource>(ref TSource source, DiplomacyPostprocessKingdomSnapshot value,
        bool allowFullDiplomacy) where TSource : struct, IDiplomacyPostprocessContextSource
    {
        var sb = new StringBuilder();
        sb.AppendLine(); sb.AppendLine(DiplomacyPromptTexts.Get("AF_DIP_PostprocessTagsHeader", "[Diplomacy postprocess tags]"));
        sb.AppendLine(DiplomacyPromptTexts.Get("AF_DIP_PostprocessCalendar", "Time units: 21 days = one quarter, 84 days = one year; there are no months or weeks."));
        sb.AppendLine(DiplomacyPromptTexts.Get("AF_DIP_KingdomIdDirectory", "[Kingdom ID reference]"));
        if (value.Kingdoms != null)
            foreach (DiplomacyKingdomSummary kingdom in value.Kingdoms)
                if (!kingdom.Eliminated) sb.AppendLine($"  {kingdom.Id} = {kingdom.Name}");
        sb.AppendLine();
        sb.AppendLine(DiplomacyPromptTexts.Get("AF_DIP_KeyIdentities", "[Key identities]"));
        sb.AppendLine(string.Format(DiplomacyPromptTexts.Get("AF_DIP_NpcKingdomIdentity", "  Your kingdom ID: {0} ({1})"), value.NpcId, value.NpcName));
        if (value.PlayerKingdomExists && !value.PlayerKingdomEliminated)
            sb.AppendLine(string.Format(DiplomacyPromptTexts.Get("AF_DIP_PlayerKingdomIdentity", "  Player kingdom ID: {0} ({1})"), value.PlayerId, value.PlayerName) + (value.PlayerIsRuler ? DiplomacyPromptTexts.Get("AF_DIP_PlayerIsRulerSuffix", "; the player is the monarch") : ""));
        else sb.AppendLine(DiplomacyPromptTexts.Get("AF_DIP_PlayerNoKingdom", "  The player currently has no kingdom to represent."));
        if (allowFullDiplomacy && (!(source is IDiplomacyOralPostprocessSource mode) || mode.UseFormalCommitments))
        {
            string annexationHint = source.GetAnnexationHint();
            if (!string.IsNullOrWhiteSpace(annexationHint))
            {
                sb.AppendLine();
                sb.AppendLine(DiplomacyPromptTexts.Get("AF_DIP_AnnexationConstraintsHeader", "[Kingdom annexation constraints]"));
                sb.AppendLine(annexationHint);
            }
        }

        if (source is IDiplomacyOralPostprocessSource native && !native.UseFormalCommitments)
        {
            sb.AppendLine(native.NativeActionInstruction());
            return sb.ToString().TrimEnd();
        }
        if (allowFullDiplomacy && value.PlayerIsRuler && value.PlayerKingdomExists && value.KingdomsDiffer)
        {
            sb.AppendLine(DiplomacyPromptTexts.Get("Bilateral_Postprocess", ""));
            return sb.ToString().TrimEnd();
        }
        sb.AppendLine(DiplomacyPromptTexts.Get("AF_DIP_CommitInstruction", "Use [ACTION:DIPLOMACY:COMMIT:action=Peace;move=NewMatter;target=KINGDOM_ID] only when the NPC explicitly gives final agreement; action may be Peace/Alliance/Trade/DeclareWar/BreakAlliance/CancelTrade/Annexation/Tributary/Garrison/Vassal. Do not output it for consideration, quotes, or hypotheticals. For move=AcceptProposal or RejectProposal, include source_document=ORIGINAL_DOCUMENT_ID;source_action=ORIGINAL_ACTION_ID (the action ID may be empty for a single-action document without one). If the original proposal is ambiguous, first ask the player to choose. Tribute requires an explicit amount and duration; clarify missing terms first. Peace without tribute has duration 0. Peace may include payer/receiver/tribute/days/cession_from/cession_to/settlement; copy the original terms explicitly agreed to and do not modify accepted proposal terms. Annexation or vassalage requires receiving=RECEIVING_KINGDOM_OR_SUZERAIN_ID;joining=JOINING_KINGDOM_OR_VASSAL_ID. Acceptance may omit terms to mean full acceptance of the original proposal, but must not omit its source. New terms require NewMatter, not a false acceptance. A player who is not a contracting party may still persuade you to respond to a third kingdom's proposal. Do not publish declarations on the player's behalf. For an NPC's oral agreement to declare war, use COMMIT; war begins only after the NPC formally publishes the declaration."));
        sb.AppendLine(DiplomacyPromptTexts.Get("AF_DIP_CommitmentInstruction", "When the NPC explicitly decides to defer, cancel, or resume their own publication commitment, use [ACTION:DIPLOMACY:COMMITMENT:arrangement=ARRANGEMENT_ID;state=deferred|cancelled|accepted;reason=EXPLICIT_REASON]. Copy the ID from the commitment context. Their own published proposal that has not yet been accepted may only be cancelled; the system publishes a separate formal withdrawal. Actions already in effect cannot be undone; use COMMIT:NewMatter separately to change terms."));
        if (value.PlayerIsRuler && value.PlayerKingdomExists && value.KingdomsDiffer)
            sb.AppendLine(DiplomacyPromptTexts.Get("AF_DIP_PlayerWarPrefix", "If the player personally and explicitly declares war on your kingdom as monarch, use [ACTION:DIPLOMACY:DECLARE_WAR:") + value.PlayerId + ":" + value.NpcId + DiplomacyPromptTexts.Get("AF_DIP_PlayerWarSuffix", "]; this takes effect immediately. Do not treat persuasion, threats, or hypotheticals as declarations of war."));
        sb.AppendLine(AnimusForge.DiplomacyDialogue.DialoguePeaceClarificationRules.PostprocessInstruction);
        return sb.ToString().TrimEnd();
    }
}

internal interface IDiplomacyOralPostprocessSource
{
    bool UseFormalCommitments { get; }
    string NativeActionInstruction();
    string OralArrangementContext();
}
