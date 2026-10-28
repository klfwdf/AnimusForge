namespace Bannerlord.ModuleManager.Models.Issues;

internal abstract record ModuleVersionMismatchSpecificIssue(ModuleInfoExtended Module, ModuleInfoExtended Dependency, ApplicationVersion Version) : ModuleVersionMismatchIssue(Module, Dependency);
