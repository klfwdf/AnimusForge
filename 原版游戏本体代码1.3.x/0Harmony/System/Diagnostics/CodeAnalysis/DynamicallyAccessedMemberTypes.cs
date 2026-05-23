using System;

namespace System.Diagnostics.CodeAnalysis
{
	// Token: 0x020004C9 RID: 1225
	[Flags]
	internal enum DynamicallyAccessedMemberTypes
	{
		// Token: 0x0400113B RID: 4411
		None = 0,
		// Token: 0x0400113C RID: 4412
		PublicParameterlessConstructor = 1,
		// Token: 0x0400113D RID: 4413
		PublicConstructors = 3,
		// Token: 0x0400113E RID: 4414
		NonPublicConstructors = 4,
		// Token: 0x0400113F RID: 4415
		PublicMethods = 8,
		// Token: 0x04001140 RID: 4416
		NonPublicMethods = 16,
		// Token: 0x04001141 RID: 4417
		PublicFields = 32,
		// Token: 0x04001142 RID: 4418
		NonPublicFields = 64,
		// Token: 0x04001143 RID: 4419
		PublicNestedTypes = 128,
		// Token: 0x04001144 RID: 4420
		NonPublicNestedTypes = 256,
		// Token: 0x04001145 RID: 4421
		PublicProperties = 512,
		// Token: 0x04001146 RID: 4422
		NonPublicProperties = 1024,
		// Token: 0x04001147 RID: 4423
		PublicEvents = 2048,
		// Token: 0x04001148 RID: 4424
		NonPublicEvents = 4096,
		// Token: 0x04001149 RID: 4425
		All = -1
	}
}
