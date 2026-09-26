namespace AnimusForge;

internal sealed class WorldDiplomacyPolicySignalSnapshot
{
	public WorldDiplomacyPolicySignalSnapshot(string signalKey, string policyId, string policyKind, string policyName, string policySummary, string issuerKingdomId, string issuerKingdomName, string targetKingdomId, string targetKingdomName, string directEffect, int publishedDay)
	{
		SignalKey = signalKey;
		PolicyId = policyId;
		PolicyKind = policyKind;
		PolicyName = policyName;
		PolicySummary = policySummary;
		IssuerKingdomId = issuerKingdomId;
		IssuerKingdomName = issuerKingdomName;
		TargetKingdomId = targetKingdomId;
		TargetKingdomName = targetKingdomName;
		DirectEffect = directEffect;
		PublishedDay = publishedDay;
	}
	public string SignalKey { get; }
	public string PolicyId { get; }
	public string PolicyKind { get; }
	public string PolicyName { get; }
	public string PolicySummary { get; }
	public string IssuerKingdomId { get; }
	public string IssuerKingdomName { get; }
	public string TargetKingdomId { get; }
	public string TargetKingdomName { get; }
	public string DirectEffect { get; }
	public int PublishedDay { get; }
}
