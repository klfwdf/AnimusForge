using System;

namespace System.Runtime.InteropServices.ComTypes
{
	// Token: 0x02000A4D RID: 2637
	[Flags]
	[__DynamicallyInvokable]
	[Serializable]
	public enum VARFLAGS : short
	{
		// Token: 0x04002DF5 RID: 11765
		[__DynamicallyInvokable]
		VARFLAG_FREADONLY = 1,
		// Token: 0x04002DF6 RID: 11766
		[__DynamicallyInvokable]
		VARFLAG_FSOURCE = 2,
		// Token: 0x04002DF7 RID: 11767
		[__DynamicallyInvokable]
		VARFLAG_FBINDABLE = 4,
		// Token: 0x04002DF8 RID: 11768
		[__DynamicallyInvokable]
		VARFLAG_FREQUESTEDIT = 8,
		// Token: 0x04002DF9 RID: 11769
		[__DynamicallyInvokable]
		VARFLAG_FDISPLAYBIND = 16,
		// Token: 0x04002DFA RID: 11770
		[__DynamicallyInvokable]
		VARFLAG_FDEFAULTBIND = 32,
		// Token: 0x04002DFB RID: 11771
		[__DynamicallyInvokable]
		VARFLAG_FHIDDEN = 64,
		// Token: 0x04002DFC RID: 11772
		[__DynamicallyInvokable]
		VARFLAG_FRESTRICTED = 128,
		// Token: 0x04002DFD RID: 11773
		[__DynamicallyInvokable]
		VARFLAG_FDEFAULTCOLLELEM = 256,
		// Token: 0x04002DFE RID: 11774
		[__DynamicallyInvokable]
		VARFLAG_FUIDEFAULT = 512,
		// Token: 0x04002DFF RID: 11775
		[__DynamicallyInvokable]
		VARFLAG_FNONBROWSABLE = 1024,
		// Token: 0x04002E00 RID: 11776
		[__DynamicallyInvokable]
		VARFLAG_FREPLACEABLE = 2048,
		// Token: 0x04002E01 RID: 11777
		[__DynamicallyInvokable]
		VARFLAG_FIMMEDIATEBIND = 4096
	}
}
