using System;

namespace Bannerlord.ModuleManager
{
	// Token: 0x0200005E RID: 94
	internal enum ModuleIssueType
	{
		// Token: 0x04000132 RID: 306
		NONE,
		// Token: 0x04000133 RID: 307
		Missing,
		// Token: 0x04000134 RID: 308
		MissingDependencies,
		// Token: 0x04000135 RID: 309
		DependencyMissingDependencies,
		// Token: 0x04000136 RID: 310
		DependencyValidationError,
		// Token: 0x04000137 RID: 311
		VersionMismatchLessThanOrEqual,
		// Token: 0x04000138 RID: 312
		VersionMismatchLessThan,
		// Token: 0x04000139 RID: 313
		VersionMismatchGreaterThan,
		// Token: 0x0400013A RID: 314
		Incompatible,
		// Token: 0x0400013B RID: 315
		DependencyConflictDependentAndIncompatible,
		// Token: 0x0400013C RID: 316
		DependencyConflictDependentLoadBeforeAndAfter,
		// Token: 0x0400013D RID: 317
		DependencyConflictCircular,
		// Token: 0x0400013E RID: 318
		DependencyNotLoadedBeforeThis,
		// Token: 0x0400013F RID: 319
		DependencyNotLoadedAfterThis
	}
}
