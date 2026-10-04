using System;
using System.Collections.Generic;
using AnimusForge.DiplomacyDialogue;
using Newtonsoft.Json;
namespace AnimusForge;

public sealed class WorldDiplomacyDialogueArrangement
{
    public string Fingerprint { get; set; } = "";
    public bool ExplicitlyDeferred { get; set; }
    public string ArrangementId { get; set; } = ""; public int Version { get; set; } = 1;
    public string SourceInteractionId { get; set; } = ""; public string RulerId { get; set; } = "";
    public string SourceChannel { get; set; } = "legacy_unknown"; public string SourceSessionId { get; set; } = "";
    public string SourcePlayerText { get; set; } = ""; public string SourceNpcText { get; set; } = "";
    public string SupersedesArrangementId { get; set; } = ""; public int SupersedesVersion { get; set; }
    public string SupersededByArrangementId { get; set; } = "";
    public int ControlSequence { get; set; }
    public string LastControlState { get; set; } = ""; public string LastControlReason { get; set; } = "";
    public List<WorldDiplomacyDialogueMemoryReceipt> MemoryReceipts { get; set; } = new List<WorldDiplomacyDialogueMemoryReceipt>();
    public string ActorKingdomId { get; set; } = ""; public string TargetKingdomId { get; set; } = "";
    public DialogueDiplomaticAction Action { get; set; } public DialogueDiplomaticMove Move { get; set; }
    public WorldDiplomacyDialogueTerms Terms { get; set; } = new WorldDiplomacyDialogueTerms();
    public string RoundId { get; set; } = ""; public string DocumentId { get; set; } = "";
    public string SourceDocumentId { get; set; } = ""; public string SourceActionId { get; set; } = "";
    public string Status { get; set; } = "accepted"; public string Reason { get; set; } = ""; public int CreatedDay { get; set; }
}

public sealed class WorldDiplomacyDialogueTerms
{
    public string ReceivingKingdomId { get; set; } = ""; public string JoiningKingdomId { get; set; } = "";
    public string TributePayerKingdomId { get; set; } = ""; public string TributeReceiverKingdomId { get; set; } = "";
    public int DailyTribute { get; set; } public int DurationDays { get; set; }
    public string CessionFromKingdomId { get; set; } = ""; public string CessionToKingdomId { get; set; } = "";
    public string CessionSettlementId { get; set; } = "";
    public DialogueDiplomaticTerms ToTerms() => new DialogueDiplomaticTerms(ReceivingKingdomId, JoiningKingdomId,
        TributePayerKingdomId, TributeReceiverKingdomId, DailyTribute, DurationDays, CessionFromKingdomId, CessionToKingdomId, CessionSettlementId);
    public static WorldDiplomacyDialogueTerms From(DialogueDiplomaticTerms t) => new WorldDiplomacyDialogueTerms {
        ReceivingKingdomId = t.ReceivingKingdomId, JoiningKingdomId = t.JoiningKingdomId, TributePayerKingdomId = t.TributePayerKingdomId,
        TributeReceiverKingdomId = t.TributeReceiverKingdomId, DailyTribute = t.DailyTribute, DurationDays = t.DurationDays,
        CessionFromKingdomId = t.CessionFromKingdomId, CessionToKingdomId = t.CessionToKingdomId, CessionSettlementId = t.CessionSettlementId };
    public WorldDiplomacyPeaceTerms ToPeaceTerms() => new WorldDiplomacyPeaceTerms { TributePayerKingdomId = TributePayerKingdomId,
        TributeReceiverKingdomId = TributeReceiverKingdomId, DailyTribute = DailyTribute, DurationDays = DurationDays,
        CessionFromKingdomId = CessionFromKingdomId, CessionToKingdomId = CessionToKingdomId, CessionSettlementId = CessionSettlementId };
}

public sealed class WorldDiplomacyDialogueMemoryReceipt
{
    public string SourceId { get; set; } = ""; public string RulerId { get; set; } = "";
    public string Fact { get; set; } = ""; public int Day { get; set; }
    public int Hour { get; set; } public string NpcName { get; set; } = "统治者";
    public string GameDate { get; set; } = "";
    public string LocationId { get; set; } = ""; public bool Delivered { get; set; }
    public string LastError { get; set; } = "";
    public string SourceInteractionId { get; set; } = ""; public string SourceChannel { get; set; } = "legacy_unknown";
    public string SourceSessionId { get; set; } = "";
}
