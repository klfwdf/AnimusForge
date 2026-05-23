using System;

namespace Mono.Cecil
{
	// Token: 0x0200029B RID: 667
	[Flags]
	internal enum TypeDefinitionTreatment
	{
		// Token: 0x04000598 RID: 1432
		None = 0,
		// Token: 0x04000599 RID: 1433
		KindMask = 15,
		// Token: 0x0400059A RID: 1434
		NormalType = 1,
		// Token: 0x0400059B RID: 1435
		NormalAttribute = 2,
		// Token: 0x0400059C RID: 1436
		UnmangleWindowsRuntimeName = 3,
		// Token: 0x0400059D RID: 1437
		PrefixWindowsRuntimeName = 4,
		// Token: 0x0400059E RID: 1438
		RedirectToClrType = 5,
		// Token: 0x0400059F RID: 1439
		RedirectToClrAttribute = 6,
		// Token: 0x040005A0 RID: 1440
		RedirectImplementedMethods = 7,
		// Token: 0x040005A1 RID: 1441
		Abstract = 16,
		// Token: 0x040005A2 RID: 1442
		Internal = 32
	}
}
