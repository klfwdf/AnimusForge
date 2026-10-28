namespace Bannerlord.ModuleManager.Models.Issues;

internal sealed record ModuleMissingNameIssue(ModuleInfoExtended Module) : ModuleIssueV2(Module)
{
	public override string ToString()
	{
		return "Module with Id '" + base.Module.Id + "' is missing its Name field";
	}

	public override ModuleIssue ToLegacy()
	{
		return new ModuleIssue(base.Module, base.Module.Id, ModuleIssueType.MissingModuleName, ToString(), ApplicationVersionRange.Empty);
	}
}
