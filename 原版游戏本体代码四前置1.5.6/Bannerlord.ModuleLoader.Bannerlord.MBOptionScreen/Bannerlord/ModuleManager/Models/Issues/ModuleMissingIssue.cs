namespace Bannerlord.ModuleManager.Models.Issues;

internal sealed record ModuleMissingIssue(ModuleInfoExtended Module, ApplicationVersionRange SourceVersion) : ModuleIssueV2(Module)
{
	public override string ToString()
	{
		return $"Module '{base.Module.Id}' {base.Module.Version} is missing from modules list";
	}

	public override ModuleIssue ToLegacy()
	{
		return new ModuleIssue(base.Module, base.Module.Id, ModuleIssueType.Missing, ToString(), SourceVersion);
	}
}
