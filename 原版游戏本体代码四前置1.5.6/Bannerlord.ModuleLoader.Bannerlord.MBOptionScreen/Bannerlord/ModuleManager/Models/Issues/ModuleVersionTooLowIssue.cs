namespace Bannerlord.ModuleManager.Models.Issues;

internal sealed record ModuleVersionTooLowIssue(ModuleInfoExtended Module, ModuleInfoExtended Dependency, ApplicationVersion Version) : ModuleVersionMismatchSpecificIssue(Module, Dependency, Version)
{
	public override string ToString()
	{
		return $"The module '{base.Module.Id}' requires version {base.Version} or higher of '{base.Dependency.Id}', but version {base.Dependency.Version} is installed";
	}

	public override ModuleIssue ToLegacy()
	{
		return new ModuleIssue(base.Module, base.Dependency.Id, ModuleIssueType.VersionMismatchLessThanOrEqual, ToString(), new ApplicationVersionRange(base.Version, base.Version));
	}
}
