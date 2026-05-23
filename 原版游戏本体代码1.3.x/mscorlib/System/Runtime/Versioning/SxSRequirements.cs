using System;

namespace System.Runtime.Versioning
{
	// Token: 0x02000722 RID: 1826
	[Flags]
	internal enum SxSRequirements
	{
		// Token: 0x0400241C RID: 9244
		None = 0,
		// Token: 0x0400241D RID: 9245
		AppDomainID = 1,
		// Token: 0x0400241E RID: 9246
		ProcessID = 2,
		// Token: 0x0400241F RID: 9247
		CLRInstanceID = 4,
		// Token: 0x04002420 RID: 9248
		AssemblyName = 8,
		// Token: 0x04002421 RID: 9249
		TypeName = 16
	}
}
