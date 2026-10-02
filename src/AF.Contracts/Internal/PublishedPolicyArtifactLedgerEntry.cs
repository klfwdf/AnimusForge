namespace AnimusForge;

internal sealed class PublishedPolicyArtifactLedgerEntry
{
	internal PublishedPolicyArtifactLedgerEntry(
		long sequence,
		long revision,
		string policyId,
		string eventKind,
		int occurredDay,
		string gameDate,
		long createdUtcTicks,
		string scopeKind,
		string kingdomId,
		string kingdomName,
		string policyName,
		string publishedText,
		string contentHash)
	{
		Sequence = sequence;
		Revision = revision;
		PolicyId = policyId ?? string.Empty;
		EventKind = eventKind ?? string.Empty;
		OccurredDay = occurredDay;
		GameDate = gameDate ?? string.Empty;
		CreatedUtcTicks = createdUtcTicks;
		ScopeKind = scopeKind ?? string.Empty;
		KingdomId = kingdomId ?? string.Empty;
		KingdomName = kingdomName ?? string.Empty;
		PolicyName = policyName ?? string.Empty;
		PublishedText = publishedText ?? string.Empty;
		ContentHash = contentHash ?? string.Empty;
	}

	public long Sequence { get; }
	public long Revision { get; }
	public string PolicyId { get; }
	public string EventKind { get; }
	public int OccurredDay { get; }
	public string GameDate { get; }
	public long CreatedUtcTicks { get; }
	public string ScopeKind { get; }
	public string KingdomId { get; }
	public string KingdomName { get; }
	public string PolicyName { get; }
	public string PublishedText { get; }
	public string ContentHash { get; }
}
