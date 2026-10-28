namespace Bannerlord.ModuleManager.Models.Issues;

internal sealed record ModuleDependencyMissingIdIssue(ModuleInfoExtended Module) : ModuleIssueV2(Module)
{
	public override string ToString()
	{
		return "Module '" + base.Module.Id + "' has a dependency entry missing its Id field";
	}

	public override ModuleIssue ToLegacy()
	{
		return new ModuleIssue(base.Module, "UNKNOWN", ModuleIssueType.DependencyMissingModuleId, ToString(), ApplicationVersionRange.Empty);
	}
}
