namespace Bannerlord.ModuleManager.Models.Issues;

internal sealed record ModuleDependencyMissingDependenciesIssue(ModuleInfoExtended Module, string DependencyId) : ModuleIssueV2(Module)
{
	public override string ToString()
	{
		return "Module '" + base.Module.Id + "': Required dependency '" + DependencyId + "' is missing its own dependencies";
	}

	public override ModuleIssue ToLegacy()
	{
		return new ModuleIssue(base.Module, DependencyId, ModuleIssueType.DependencyMissingDependencies, ToString(), ApplicationVersionRange.Empty);
	}
}
