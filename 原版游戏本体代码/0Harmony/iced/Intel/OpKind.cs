using System;

namespace Iced.Intel
{
	// Token: 0x02000661 RID: 1633
	internal enum OpKind
	{
		// Token: 0x04003314 RID: 13076
		Register,
		// Token: 0x04003315 RID: 13077
		NearBranch16,
		// Token: 0x04003316 RID: 13078
		NearBranch32,
		// Token: 0x04003317 RID: 13079
		NearBranch64,
		// Token: 0x04003318 RID: 13080
		FarBranch16,
		// Token: 0x04003319 RID: 13081
		FarBranch32,
		// Token: 0x0400331A RID: 13082
		Immediate8,
		// Token: 0x0400331B RID: 13083
		Immediate8_2nd,
		// Token: 0x0400331C RID: 13084
		Immediate16,
		// Token: 0x0400331D RID: 13085
		Immediate32,
		// Token: 0x0400331E RID: 13086
		Immediate64,
		// Token: 0x0400331F RID: 13087
		Immediate8to16,
		// Token: 0x04003320 RID: 13088
		Immediate8to32,
		// Token: 0x04003321 RID: 13089
		Immediate8to64,
		// Token: 0x04003322 RID: 13090
		Immediate32to64,
		// Token: 0x04003323 RID: 13091
		MemorySegSI,
		// Token: 0x04003324 RID: 13092
		MemorySegESI,
		// Token: 0x04003325 RID: 13093
		MemorySegRSI,
		// Token: 0x04003326 RID: 13094
		MemorySegDI,
		// Token: 0x04003327 RID: 13095
		MemorySegEDI,
		// Token: 0x04003328 RID: 13096
		MemorySegRDI,
		// Token: 0x04003329 RID: 13097
		MemoryESDI,
		// Token: 0x0400332A RID: 13098
		MemoryESEDI,
		// Token: 0x0400332B RID: 13099
		MemoryESRDI,
		// Token: 0x0400332C RID: 13100
		Memory
	}
}
