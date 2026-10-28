namespace Bannerlord.ModuleManager.Models.Issues;

internal sealed record ModuleIncompatibleIssue(ModuleInfoExtended Module, ModuleInfoExtended IncompatibleModule) : ModuleIssueV2(Module)
{
	public override string ToString()
	{
		return "'" + IncompatibleModule.Id + "' is incompatible with this module";
	}

	public override ModuleIssue ToLegacy()
	{
		return new ModuleIssue(base.Module, IncompatibleModule.Id, ModuleIssueType.Incompatible, ToString(), ApplicationVersionRange.Empty);
	}
}
