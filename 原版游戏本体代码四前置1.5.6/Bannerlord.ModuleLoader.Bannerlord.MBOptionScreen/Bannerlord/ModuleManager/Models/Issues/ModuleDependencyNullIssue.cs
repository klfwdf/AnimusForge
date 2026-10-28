namespace Bannerlord.ModuleManager.Models.Issues;

internal sealed record ModuleDependencyNullIssue(ModuleInfoExtended Module) : ModuleIssueV2(Module)
{
	public override string ToString()
	{
		return "Module '" + base.Module.Id + "' has a null dependency entry";
	}

	public override ModuleIssue ToLegacy()
	{
		return new ModuleIssue(base.Module, "UNKNOWN", ModuleIssueType.DependencyIsNull, ToString(), ApplicationVersionRange.Empty);
	}
}
