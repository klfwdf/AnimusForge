namespace AnimusForge;

internal interface IDiplomacyCommitmentTagSource
{
    string SpeakerKingdomId { get; }
    string SubmitCommitment(string payload);
    string ControlCommitment(string payload);
    string SubmitLegacyCommitment(string action, string payload);
}
