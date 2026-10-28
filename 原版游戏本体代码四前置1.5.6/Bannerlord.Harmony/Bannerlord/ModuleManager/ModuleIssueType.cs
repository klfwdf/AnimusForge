namespace Bannerlord.ModuleManager;

internal enum ModuleIssueType
{
	NONE,
	Missing,
	MissingDependencies,
	DependencyMissingDependencies,
	DependencyValidationError,
	VersionMismatchLessThanOrEqual,
	VersionMismatchLessThan,
	VersionMismatchGreaterThan,
	Incompatible,
	DependencyConflictDependentAndIncompatible,
	DependencyConflictDependentLoadBeforeAndAfter,
	DependencyConflictCircular,
	DependencyNotLoadedBeforeThis,
	DependencyNotLoadedAfterThis,
	MissingModuleId,
	MissingModuleName,
	DependencyIsNull,
	DependencyMissingModuleId,
	MissingBLSE
}
