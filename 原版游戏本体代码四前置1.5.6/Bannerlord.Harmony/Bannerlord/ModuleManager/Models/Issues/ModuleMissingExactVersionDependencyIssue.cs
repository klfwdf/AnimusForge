namespace Bannerlord.ModuleManager.Models.Issues;

internal sealed record ModuleMissingExactVersionDependencyIssue(ModuleInfoExtended Module, DependentModuleMetadata Dependency) : ModuleIssueV2(Module)
{
	public override string ToString()
	{
		return $"Missing '{Dependency.Id}' with required version {Dependency.Version}";
	}

	public override ModuleIssue ToLegacy()
	{
		return new ModuleIssue(base.Module, Dependency.Id, ModuleIssueType.MissingDependencies, ToString(), new ApplicationVersionRange(Dependency.Version, Dependency.Version));
	}
}
