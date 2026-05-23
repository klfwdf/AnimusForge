using System;

namespace Bannerlord.ModuleManager
{
	// Token: 0x0200001C RID: 28
	internal enum ModuleIssueType
	{
		// Token: 0x04000062 RID: 98
		NONE,
		// Token: 0x04000063 RID: 99
		Missing,
		// Token: 0x04000064 RID: 100
		MissingDependencies,
		// Token: 0x04000065 RID: 101
		DependencyMissingDependencies,
		// Token: 0x04000066 RID: 102
		DependencyValidationError,
		// Token: 0x04000067 RID: 103
		VersionMismatchLessThanOrEqual,
		// Token: 0x04000068 RID: 104
		VersionMismatchLessThan,
		// Token: 0x04000069 RID: 105
		VersionMismatchGreaterThan,
		// Token: 0x0400006A RID: 106
		Incompatible,
		// Token: 0x0400006B RID: 107
		DependencyConflictDependentAndIncompatible,
		// Token: 0x0400006C RID: 108
		DependencyConflictDependentLoadBeforeAndAfter,
		// Token: 0x0400006D RID: 109
		DependencyConflictCircular,
		// Token: 0x0400006E RID: 110
		DependencyNotLoadedBeforeThis,
		// Token: 0x0400006F RID: 111
		DependencyNotLoadedAfterThis,
		// Token: 0x04000070 RID: 112
		MissingModuleId,
		// Token: 0x04000071 RID: 113
		MissingModuleName,
		// Token: 0x04000072 RID: 114
		DependencyIsNull,
		// Token: 0x04000073 RID: 115
		DependencyMissingModuleId,
		// Token: 0x04000074 RID: 116
		MissingBLSE
	}
}
