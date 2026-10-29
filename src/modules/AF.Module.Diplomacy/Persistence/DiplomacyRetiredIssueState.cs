using System.Collections.Generic;

namespace AnimusForge;

// Read compatibility for the removed per-issue design. No scheduling rules live here.
public sealed partial class DiplomacyEventConvergence
{
    // Retired save-format fields, consumed only by BaselineMigration.
    public int IssueVersion { get; set; }
    public List<DiplomacyNegotiationIssue> Issues { get; set; } = new();
    public List<DiplomacyMaterialReaction> Reactions { get; set; } = new();
    public List<string> IssuePublicationIds { get; set; } = new();
    public List<string> TechnicalUnresolvedSpeakers { get; set; } = new();
    public bool IssuesFrozen { get; set; }
    public bool ShouldSerializeIssueVersion() => IssueVersion != 0;
    public bool ShouldSerializeIssues() => IssueVersion != 0;
    public bool ShouldSerializeReactions() => IssueVersion != 0;
    public bool ShouldSerializeIssuePublicationIds() => IssueVersion != 0;
    public bool ShouldSerializeTechnicalUnresolvedSpeakers() => IssueVersion != 0;
    public bool ShouldSerializeIssuesFrozen() => IssueVersion != 0;
}

// Persisted type identity retained for old save import only.
public sealed class DiplomacyNegotiationIssue
{
    public string Id { get; set; } = "";
    public string Owner { get; set; } = "";
    public string Respondent { get; set; } = "";
    public string Demand { get; set; } = "";
    public string OriginDocumentId { get; set; } = "";
    public string LastDocumentId { get; set; } = "";
    public string NextActor { get; set; } = "";
    public int CompletedRounds { get; set; }
    public string Status { get; set; } = "open";
    public string ResultDocumentId { get; set; } = "";
    public List<string> DocumentIds { get; set; } = new();

}

public sealed class DiplomacyMaterialReaction
{
    public string Id { get; set; } = "";
    public string KingdomId { get; set; } = "";
    public string Status { get; set; } = "pending";
    public List<string> ChangeDocumentIds { get; set; } = new();
    public List<string> IssueIds { get; set; } = new();
    public string JobId { get; set; } = "";
    public string DocumentId { get; set; } = "";
}
