using System;

namespace Mono.Cecil
{
	// Token: 0x0200026B RID: 619
	internal enum MethodCallingConvention : byte
	{
		// Token: 0x04000456 RID: 1110
		Default,
		// Token: 0x04000457 RID: 1111
		C,
		// Token: 0x04000458 RID: 1112
		StdCall,
		// Token: 0x04000459 RID: 1113
		ThisCall,
		// Token: 0x0400045A RID: 1114
		FastCall,
		// Token: 0x0400045B RID: 1115
		VarArg,
		// Token: 0x0400045C RID: 1116
		Unmanaged = 9,
		// Token: 0x0400045D RID: 1117
		Generic = 16
	}
}
