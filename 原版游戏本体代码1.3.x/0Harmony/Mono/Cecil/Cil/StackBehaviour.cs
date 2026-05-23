using System;

namespace Mono.Cecil.Cil
{
	// Token: 0x020002F8 RID: 760
	internal enum StackBehaviour
	{
		// Token: 0x040008E8 RID: 2280
		Pop0,
		// Token: 0x040008E9 RID: 2281
		Pop1,
		// Token: 0x040008EA RID: 2282
		Pop1_pop1,
		// Token: 0x040008EB RID: 2283
		Popi,
		// Token: 0x040008EC RID: 2284
		Popi_pop1,
		// Token: 0x040008ED RID: 2285
		Popi_popi,
		// Token: 0x040008EE RID: 2286
		Popi_popi8,
		// Token: 0x040008EF RID: 2287
		Popi_popi_popi,
		// Token: 0x040008F0 RID: 2288
		Popi_popr4,
		// Token: 0x040008F1 RID: 2289
		Popi_popr8,
		// Token: 0x040008F2 RID: 2290
		Popref,
		// Token: 0x040008F3 RID: 2291
		Popref_pop1,
		// Token: 0x040008F4 RID: 2292
		Popref_popi,
		// Token: 0x040008F5 RID: 2293
		Popref_popi_popi,
		// Token: 0x040008F6 RID: 2294
		Popref_popi_popi8,
		// Token: 0x040008F7 RID: 2295
		Popref_popi_popr4,
		// Token: 0x040008F8 RID: 2296
		Popref_popi_popr8,
		// Token: 0x040008F9 RID: 2297
		Popref_popi_popref,
		// Token: 0x040008FA RID: 2298
		PopAll,
		// Token: 0x040008FB RID: 2299
		Push0,
		// Token: 0x040008FC RID: 2300
		Push1,
		// Token: 0x040008FD RID: 2301
		Push1_push1,
		// Token: 0x040008FE RID: 2302
		Pushi,
		// Token: 0x040008FF RID: 2303
		Pushi8,
		// Token: 0x04000900 RID: 2304
		Pushr4,
		// Token: 0x04000901 RID: 2305
		Pushr8,
		// Token: 0x04000902 RID: 2306
		Pushref,
		// Token: 0x04000903 RID: 2307
		Varpop,
		// Token: 0x04000904 RID: 2308
		Varpush
	}
}
