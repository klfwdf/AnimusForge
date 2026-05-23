using System;

namespace System.Runtime.InteropServices.ComTypes
{
	// Token: 0x02000A41 RID: 2625
	[Flags]
	[__DynamicallyInvokable]
	[Serializable]
	public enum PARAMFLAG : short
	{
		// Token: 0x04002DAA RID: 11690
		[__DynamicallyInvokable]
		PARAMFLAG_NONE = 0,
		// Token: 0x04002DAB RID: 11691
		[__DynamicallyInvokable]
		PARAMFLAG_FIN = 1,
		// Token: 0x04002DAC RID: 11692
		[__DynamicallyInvokable]
		PARAMFLAG_FOUT = 2,
		// Token: 0x04002DAD RID: 11693
		[__DynamicallyInvokable]
		PARAMFLAG_FLCID = 4,
		// Token: 0x04002DAE RID: 11694
		[__DynamicallyInvokable]
		PARAMFLAG_FRETVAL = 8,
		// Token: 0x04002DAF RID: 11695
		[__DynamicallyInvokable]
		PARAMFLAG_FOPT = 16,
		// Token: 0x04002DB0 RID: 11696
		[__DynamicallyInvokable]
		PARAMFLAG_FHASDEFAULT = 32,
		// Token: 0x04002DB1 RID: 11697
		[__DynamicallyInvokable]
		PARAMFLAG_FHASCUSTDATA = 64
	}
}
