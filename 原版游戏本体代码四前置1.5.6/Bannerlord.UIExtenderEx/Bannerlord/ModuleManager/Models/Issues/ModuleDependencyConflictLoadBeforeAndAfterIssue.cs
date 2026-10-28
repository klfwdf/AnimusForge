namespace Bannerlord.ModuleManager.Models.Issues;

internal sealed record ModuleDependencyConflictLoadBeforeAndAfterIssue(ModuleInfoExtended Module, DependentModuleMetadata ConflictingModule) : ModuleIssueV2(Module)
{
	public override string ToString()
	{
		return "Module '" + base.Module.Id + "' has conflicting load order requirements with '" + ConflictingModule.Id + "' (both LoadBefore and LoadAfter)";
	}

	public override ModuleIssue ToLegacy()
	{
		return new ModuleIssue(base.Module, ConflictingModule.Id, ModuleIssueType.DependencyConflictDependentLoadBeforeAndAfter, ToString(), ApplicationVersionRange.Empty);
	}
}
