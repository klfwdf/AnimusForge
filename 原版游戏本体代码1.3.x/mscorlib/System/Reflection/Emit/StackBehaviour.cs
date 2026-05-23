using System;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit
{
	// Token: 0x02000658 RID: 1624
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public enum StackBehaviour
	{
		// Token: 0x04002151 RID: 8529
		[__DynamicallyInvokable]
		Pop0,
		// Token: 0x04002152 RID: 8530
		[__DynamicallyInvokable]
		Pop1,
		// Token: 0x04002153 RID: 8531
		[__DynamicallyInvokable]
		Pop1_pop1,
		// Token: 0x04002154 RID: 8532
		[__DynamicallyInvokable]
		Popi,
		// Token: 0x04002155 RID: 8533
		[__DynamicallyInvokable]
		Popi_pop1,
		// Token: 0x04002156 RID: 8534
		[__DynamicallyInvokable]
		Popi_popi,
		// Token: 0x04002157 RID: 8535
		[__DynamicallyInvokable]
		Popi_popi8,
		// Token: 0x04002158 RID: 8536
		[__DynamicallyInvokable]
		Popi_popi_popi,
		// Token: 0x04002159 RID: 8537
		[__DynamicallyInvokable]
		Popi_popr4,
		// Token: 0x0400215A RID: 8538
		[__DynamicallyInvokable]
		Popi_popr8,
		// Token: 0x0400215B RID: 8539
		[__DynamicallyInvokable]
		Popref,
		// Token: 0x0400215C RID: 8540
		[__DynamicallyInvokable]
		Popref_pop1,
		// Token: 0x0400215D RID: 8541
		[__DynamicallyInvokable]
		Popref_popi,
		// Token: 0x0400215E RID: 8542
		[__DynamicallyInvokable]
		Popref_popi_popi,
		// Token: 0x0400215F RID: 8543
		[__DynamicallyInvokable]
		Popref_popi_popi8,
		// Token: 0x04002160 RID: 8544
		[__DynamicallyInvokable]
		Popref_popi_popr4,
		// Token: 0x04002161 RID: 8545
		[__DynamicallyInvokable]
		Popref_popi_popr8,
		// Token: 0x04002162 RID: 8546
		[__DynamicallyInvokable]
		Popref_popi_popref,
		// Token: 0x04002163 RID: 8547
		[__DynamicallyInvokable]
		Push0,
		// Token: 0x04002164 RID: 8548
		[__DynamicallyInvokable]
		Push1,
		// Token: 0x04002165 RID: 8549
		[__DynamicallyInvokable]
		Push1_push1,
		// Token: 0x04002166 RID: 8550
		[__DynamicallyInvokable]
		Pushi,
		// Token: 0x04002167 RID: 8551
		[__DynamicallyInvokable]
		Pushi8,
		// Token: 0x04002168 RID: 8552
		[__DynamicallyInvokable]
		Pushr4,
		// Token: 0x04002169 RID: 8553
		[__DynamicallyInvokable]
		Pushr8,
		// Token: 0x0400216A RID: 8554
		[__DynamicallyInvokable]
		Pushref,
		// Token: 0x0400216B RID: 8555
		[__DynamicallyInvokable]
		Varpop,
		// Token: 0x0400216C RID: 8556
		[__DynamicallyInvokable]
		Varpush,
		// Token: 0x0400216D RID: 8557
		[__DynamicallyInvokable]
		Popref_popi_pop1
	}
}
