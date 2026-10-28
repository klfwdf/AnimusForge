namespace Bannerlord.ModuleManager.Models.Issues;

internal sealed record ModuleDependencyValidationIssue(ModuleInfoExtended Module, string DependencyId) : ModuleIssueV2(Module)
{
	public override string ToString()
	{
		return "Module '" + base.Module.Id + "': Dependency '" + DependencyId + "' has unresolved validation issues";
	}

	public override ModuleIssue ToLegacy()
	{
		return new ModuleIssue(base.Module, DependencyId, ModuleIssueType.DependencyValidationError, ToString(), ApplicationVersionRange.Empty);
	}
}
