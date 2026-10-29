namespace AnimusForge;

public sealed class DiplomacyAidRequest
{
    public string Id { get; set; } = "";
    public string Identity { get; set; } = "";
    public string PreviousRequestId { get; set; } = "";
    public string MaterialFingerprint { get; set; } = "";
    public int Revision { get; set; }
    public string CallingKingdomId { get; set; } = "";
    public string CalledKingdomId { get; set; } = "";
    public string EnemyKingdomId { get; set; } = "";
    public string WarIdentity { get; set; } = "";
    public string Status { get; set; } = "requested";
    public string RoundId { get; set; } = "";
    public string RequestDocumentId { get; set; } = "";
    public string AnswerDocumentId { get; set; } = "";
    public string AcceptedRulerId { get; set; } = "";
    public string SourceKind { get; set; } = "native_request";
    public int Day { get; set; }
    public int LastAttemptDay { get; set; } = -1;
    public int LegacyCost { get; set; }
    public bool LegacyPlayerPaying { get; set; }
    public string LegacyCostState { get; set; } = "none";
    public string Reason { get; set; } = "";
}

public sealed class DiplomacyAidDecision
{
    public string RequestId { get; set; } = "";
    public string Decision { get; set; } = "";
}
