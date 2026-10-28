namespace Bannerlord.ModuleManager.Models.Issues;

internal sealed record ModuleMissingIdIssue(ModuleInfoExtended Module) : ModuleIssueV2(Module)
{
	public override string ToString()
	{
		return "Module with Name '" + base.Module.Name + "' is missing its Id field";
	}

	public override ModuleIssue ToLegacy()
	{
		return new ModuleIssue(base.Module, "UNKNOWN", ModuleIssueType.MissingModuleId, ToString(), ApplicationVersionRange.Empty);
	}
}
