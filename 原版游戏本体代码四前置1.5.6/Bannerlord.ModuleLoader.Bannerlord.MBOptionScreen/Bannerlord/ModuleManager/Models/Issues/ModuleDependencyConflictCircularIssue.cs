namespace Bannerlord.ModuleManager.Models.Issues;

internal sealed record ModuleDependencyConflictCircularIssue(ModuleInfoExtended Module, ModuleInfoExtended CircularDependency) : ModuleIssueV2(Module)
{
	public override string ToString()
	{
		return "Module '" + base.Module.Id + "' and '" + CircularDependency.Id + "' have circular dependencies";
	}

	public override ModuleIssue ToLegacy()
	{
		return new ModuleIssue(base.Module, CircularDependency.Id, ModuleIssueType.DependencyConflictCircular, ToString(), ApplicationVersionRange.Empty);
	}
}
