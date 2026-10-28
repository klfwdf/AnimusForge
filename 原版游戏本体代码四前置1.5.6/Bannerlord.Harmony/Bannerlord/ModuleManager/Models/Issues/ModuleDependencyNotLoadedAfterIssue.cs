namespace Bannerlord.ModuleManager.Models.Issues;

internal sealed record ModuleDependencyNotLoadedAfterIssue(ModuleInfoExtended Module, DependentModuleMetadata Dependency) : ModuleIssueV2(Module)
{
	public override string ToString()
	{
		return $"'{Dependency.Id}' should be loaded after '{base.Module.Id}'";
	}

	public override ModuleIssue ToLegacy()
	{
		return new ModuleIssue(base.Module, Dependency.Id, ModuleIssueType.DependencyNotLoadedAfterThis, ToString(), ApplicationVersionRange.Empty);
	}
}
