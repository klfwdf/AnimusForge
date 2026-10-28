namespace Bannerlord.ModuleManager;

internal sealed record ModuleIssue
{
	public Bannerlord.ModuleManager.ModuleInfoExtended Target { get; set; }

	public string SourceId { get; set; }

	public Bannerlord.ModuleManager.ModuleIssueType Type { get; set; }

	public string Reason { get; set; }

	public Bannerlord.ModuleManager.ApplicationVersionRange SourceVersion { get; set; }

	public ModuleIssue()
	{
		Target = new Bannerlord.ModuleManager.ModuleInfoExtended();
		SourceId = string.Empty;
		Type = Bannerlord.ModuleManager.ModuleIssueType.NONE;
		Reason = string.Empty;
		SourceVersion = Bannerlord.ModuleManager.ApplicationVersionRange.Empty;
	}

	public ModuleIssue(Bannerlord.ModuleManager.ModuleInfoExtended target, string sourceId, Bannerlord.ModuleManager.ModuleIssueType type)
	{
		Target = target;
		SourceId = sourceId;
		Type = type;
		Reason = string.Empty;
		SourceVersion = Bannerlord.ModuleManager.ApplicationVersionRange.Empty;
	}

	public ModuleIssue(Bannerlord.ModuleManager.ModuleInfoExtended target, string sourceId, Bannerlord.ModuleManager.ModuleIssueType type, string reason, Bannerlord.ModuleManager.ApplicationVersionRange sourceVersion)
		: this(target, sourceId, type)
	{
		Reason = reason;
		SourceVersion = sourceVersion;
	}
}
