namespace Bannerlord.ModuleManager.Models.Issues;

internal abstract record ModuleVersionMismatchIssue(ModuleInfoExtended Module, ModuleInfoExtended Dependency) : ModuleIssueV2(Module);
