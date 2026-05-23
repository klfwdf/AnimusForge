using System;

namespace Mono.Cecil
{
	// Token: 0x0200029D RID: 669
	[Flags]
	internal enum MethodDefinitionTreatment
	{
		// Token: 0x040005A9 RID: 1449
		None = 0,
		// Token: 0x040005AA RID: 1450
		Abstract = 2,
		// Token: 0x040005AB RID: 1451
		Private = 4,
		// Token: 0x040005AC RID: 1452
		Public = 8,
		// Token: 0x040005AD RID: 1453
		Runtime = 16,
		// Token: 0x040005AE RID: 1454
		InternalCall = 32
	}
}
