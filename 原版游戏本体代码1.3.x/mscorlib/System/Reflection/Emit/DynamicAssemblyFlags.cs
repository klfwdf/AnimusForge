using System;

namespace System.Reflection.Emit
{
	// Token: 0x02000628 RID: 1576
	[Flags]
	internal enum DynamicAssemblyFlags
	{
		// Token: 0x04001E44 RID: 7748
		None = 0,
		// Token: 0x04001E45 RID: 7749
		AllCritical = 1,
		// Token: 0x04001E46 RID: 7750
		Aptca = 2,
		// Token: 0x04001E47 RID: 7751
		Critical = 4,
		// Token: 0x04001E48 RID: 7752
		Transparent = 8,
		// Token: 0x04001E49 RID: 7753
		TreatAsSafe = 16
	}
}
