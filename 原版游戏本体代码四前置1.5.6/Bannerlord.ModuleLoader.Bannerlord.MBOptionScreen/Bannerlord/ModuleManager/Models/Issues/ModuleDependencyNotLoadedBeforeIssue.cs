namespace Bannerlord.ModuleManager.Models.Issues;

internal sealed record ModuleDependencyNotLoadedBeforeIssue(ModuleInfoExtended Module, DependentModuleMetadata Dependency) : ModuleIssueV2(Module)
{
	public override string ToString()
	{
		return "'" + Dependency.Id + "' should be loaded before '" + base.Module.Id + "'";
	}

	public override ModuleIssue ToLegacy()
	{
		return new ModuleIssue(base.Module, Dependency.Id, ModuleIssueType.DependencyNotLoadedBeforeThis, ToString(), ApplicationVersionRange.Empty);
	}
}
