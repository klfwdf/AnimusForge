namespace Bannerlord.ModuleManager.Models.Issues;

internal sealed record ModuleMissingVersionRangeDependencyIssue(ModuleInfoExtended Module, DependentModuleMetadata Dependency) : ModuleIssueV2(Module)
{
	public override string ToString()
	{
		return $"Missing '{Dependency.Id}' with required version range [{Dependency.VersionRange}]";
	}

	public override ModuleIssue ToLegacy()
	{
		return new ModuleIssue(base.Module, Dependency.Id, ModuleIssueType.MissingDependencies, ToString(), Dependency.VersionRange);
	}
}
