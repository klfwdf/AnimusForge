namespace Bannerlord.ModuleManager.Models.Issues;

internal sealed record ModuleMissingBLSEDependencyIssue(ModuleInfoExtended Module, DependentModuleMetadata Dependency) : ModuleIssueV2(Module)
{
	public override string ToString()
	{
		return "Missing Bannerlord Software Extender";
	}

	public override ModuleIssue ToLegacy()
	{
		return new ModuleIssue(base.Module, Dependency.Id, ModuleIssueType.MissingBLSE, ToString(), ApplicationVersionRange.Empty);
	}
}
