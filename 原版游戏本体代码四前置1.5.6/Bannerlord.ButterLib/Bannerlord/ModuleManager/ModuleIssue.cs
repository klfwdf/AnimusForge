namespace Bannerlord.ModuleManager;

internal sealed record ModuleIssue
{
	public ModuleInfoExtended Target { get; set; }

	public string SourceId { get; set; }

	public ModuleIssueType Type { get; set; }

	public string Reason { get; set; }

	public ApplicationVersionRange SourceVersion { get; set; }

	public ModuleIssue()
	{
		Target = new ModuleInfoExtended();
		SourceId = string.Empty;
		Type = ModuleIssueType.NONE;
		Reason = string.Empty;
		SourceVersion = ApplicationVersionRange.Empty;
	}

	public ModuleIssue(ModuleInfoExtended target, string sourceId, ModuleIssueType type)
	{
		Target = target;
		SourceId = sourceId;
		Type = type;
		Reason = string.Empty;
		SourceVersion = ApplicationVersionRange.Empty;
	}

	public ModuleIssue(ModuleInfoExtended target, string sourceId, ModuleIssueType type, string reason, ApplicationVersionRange sourceVersion)
		: this(target, sourceId, type)
	{
		Reason = reason;
		SourceVersion = sourceVersion;
	}
}
