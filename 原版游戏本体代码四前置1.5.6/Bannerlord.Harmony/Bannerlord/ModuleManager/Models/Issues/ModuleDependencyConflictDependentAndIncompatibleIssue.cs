namespace Bannerlord.ModuleManager.Models.Issues;

internal sealed record ModuleDependencyConflictDependentAndIncompatibleIssue(ModuleInfoExtended Module, string ConflictingModuleId) : ModuleIssueV2(Module)
{
	public override string ToString()
	{
		return $"Module '{base.Module.Id}' has conflicting configuration: '{ConflictingModuleId}' is marked as both required and incompatible";
	}

	public override ModuleIssue ToLegacy()
	{
		return new ModuleIssue(base.Module, ConflictingModuleId, ModuleIssueType.DependencyConflictDependentAndIncompatible, ToString(), ApplicationVersionRange.Empty);
	}
}
