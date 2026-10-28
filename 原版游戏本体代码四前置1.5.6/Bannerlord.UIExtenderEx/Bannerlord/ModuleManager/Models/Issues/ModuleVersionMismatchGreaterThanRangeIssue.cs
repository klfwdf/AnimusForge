namespace Bannerlord.ModuleManager.Models.Issues;

internal sealed record ModuleVersionMismatchGreaterThanRangeIssue(ModuleInfoExtended Module, ModuleInfoExtended Dependency, ApplicationVersionRange VersionRange) : ModuleVersionMismatchRangeIssue(Module, Dependency, VersionRange)
{
	public override string ToString()
	{
		return $"The module '{base.Module.Id}' requires '{base.Dependency.Id}' version {base.VersionRange}, but version {base.Dependency.Version} is installed (above maximum)";
	}

	public override ModuleIssue ToLegacy()
	{
		return new ModuleIssue(base.Module, base.Dependency.Id, ModuleIssueType.VersionMismatchGreaterThan, ToString(), base.VersionRange);
	}
}
