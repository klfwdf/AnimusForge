using System.Collections.Generic;

namespace AnimusForge;

// Indices and unadopted leads only. Published facts remain in the event archive.
public sealed class DiplomacyMaterialInvestigation
{
    public long Sequence { get; set; }
    public int Day { get; set; } = -1;
    public string SourceVersion { get; set; } = "";
}

public sealed class DiplomacyMaterialLead
{
    public Dictionary<string, string> SourceVersions { get; set; } = new();
    public string Id { get; set; } = "";
    public string EventId { get; set; } = "";
    public string Candidate { get; set; } = "";
    public string SourceVersion { get; set; } = "";
    public string Status { get; set; } = "organized";
    public int Day { get; set; }
}

public sealed class DiplomacyCountryParticipation
{
    public int LastExpressionDay { get; set; } = -1;
    public string LastDocumentId { get; set; } = "";
    public string LastEventId { get; set; } = "";
    public string ExpressionKind { get; set; } = "";
}

public sealed class DiplomacyEventWait
{
    public string Kind { get; set; } = "";
    public string SourceId { get; set; } = "";
    public string SourceActionId { get; set; } = "";
    public string KingdomId { get; set; } = "";
    public string Reason { get; set; } = "";
    public int UntilDay { get; set; }
    public string WorldFingerprint { get; set; } = "";
}

public sealed class DiplomacyEventAnnouncement
{
    public string Theme { get; set; } = "";
    public string Id { get; set; } = "";
    public string EventId { get; set; } = "";
    public string Stage { get; set; } = "started";
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public string Scene { get; set; } = "Negotiation";
    public string ImageId { get; set; } = "";
    public string State { get; set; } = "pending";
    public int Day { get; set; }
    public bool Acknowledged { get; set; }
    public bool CanonicalHistoryRecorded { get; set; }
    public List<string> ParticipantIds { get; set; } = new();
    public List<string> EffectIds { get; set; } = new();
    public List<string> RemovedEffectIds { get; set; } = new();
    public List<string> ConfirmedDocumentIds { get; set; } = new();
    public List<string> ProgressEvidenceIds { get; set; } = new();
    public List<DiplomacyEventNarrativeFact> Narrative { get; set; } = new();
    // Compact story identity survives completed-round display retention.
    public List<string> MaterialKeys { get; set; } = new();
    public string ConceptFingerprint { get; set; } = "";
}

public sealed class DiplomacyEventNarrativeFact
{
    public List<string> HeroIds { get; set; } = new();
    public string Id { get; set; } = "";
    public string Kind { get; set; } = "public_fact";
    public string Source { get; set; } = "";
    public string Text { get; set; } = "";
    public string SubjectKingdomId { get; set; } = "";
    public List<string> KnownKingdomIds { get; set; } = new();
    public bool Public { get; set; }
}

public sealed class DiplomacyEventEffectState
{
    public string Id { get; set; } = "";
    public string EventId { get; set; } = "";
    public string Stage { get; set; } = "";
    public string Kind { get; set; } = "";
    public string KingdomId { get; set; } = "";
    public string OtherKingdomId { get; set; } = "";
    public string RulerId { get; set; } = "";
    public string OtherRulerId { get; set; } = "";
    public string SettlementId { get; set; } = "";
    public string Reason { get; set; } = "";
    public float Delta { get; set; }
    public int StartedDay { get; set; }
    public int ExpiresDay { get; set; }
    public int LastAppliedDay { get; set; } = -1;
    public float AppliedTotal { get; set; }
    public string Status { get; set; } = "pending";
    public string Failure { get; set; } = "";
}
