namespace Bannerlord.ModuleManager.Models.Issues;

internal sealed record ModuleMissingUnversionedDependencyIssue(ModuleInfoExtended Module, DependentModuleMetadata Dependency) : ModuleIssueV2(Module)
{
	public override string ToString()
	{
		return "Missing '" + Dependency.Id + "' module";
	}

	public override ModuleIssue ToLegacy()
	{
		return new ModuleIssue(base.Module, Dependency.Id, ModuleIssueType.MissingDependencies, ToString(), ApplicationVersionRange.Empty);
	}
}
