namespace Bannerlord.ModuleManager.Models.Issues;

internal abstract record ModuleIssueV2(ModuleInfoExtended Module)
{
	public abstract ModuleIssue ToLegacy();
}
