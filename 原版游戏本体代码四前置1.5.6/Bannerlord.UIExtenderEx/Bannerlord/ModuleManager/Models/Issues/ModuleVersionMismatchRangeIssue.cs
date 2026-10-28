namespace Bannerlord.ModuleManager.Models.Issues;

internal abstract record ModuleVersionMismatchRangeIssue(ModuleInfoExtended Module, ModuleInfoExtended Dependency, ApplicationVersionRange VersionRange) : ModuleVersionMismatchIssue(Module, Dependency);
