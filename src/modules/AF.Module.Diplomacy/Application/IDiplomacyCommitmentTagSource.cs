namespace AnimusForge;

internal interface IDiplomacyCommitmentTagSource
{
    bool UseFormalCommitments { get; }
    string SpeakerKingdomId { get; }
    string SubmitCommitment(string payload);
    string ControlCommitment(string payload);
    string SubmitLegacyCommitment(string action, string payload);
}

internal interface IDiplomacyCommitmentReviewSource
{
    bool CaptureCommitmentReview(string tagHints);
}

internal interface IDiplomacyLobbyistTagSource
{
    bool TryProcessLobbyist(string action, string payload);
}
