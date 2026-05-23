using System;

namespace System.Runtime.InteropServices.ComTypes
{
	// Token: 0x02000A4C RID: 2636
	[Flags]
	[__DynamicallyInvokable]
	[Serializable]
	public enum FUNCFLAGS : short
	{
		// Token: 0x04002DE7 RID: 11751
		[__DynamicallyInvokable]
		FUNCFLAG_FRESTRICTED = 1,
		// Token: 0x04002DE8 RID: 11752
		[__DynamicallyInvokable]
		FUNCFLAG_FSOURCE = 2,
		// Token: 0x04002DE9 RID: 11753
		[__DynamicallyInvokable]
		FUNCFLAG_FBINDABLE = 4,
		// Token: 0x04002DEA RID: 11754
		[__DynamicallyInvokable]
		FUNCFLAG_FREQUESTEDIT = 8,
		// Token: 0x04002DEB RID: 11755
		[__DynamicallyInvokable]
		FUNCFLAG_FDISPLAYBIND = 16,
		// Token: 0x04002DEC RID: 11756
		[__DynamicallyInvokable]
		FUNCFLAG_FDEFAULTBIND = 32,
		// Token: 0x04002DED RID: 11757
		[__DynamicallyInvokable]
		FUNCFLAG_FHIDDEN = 64,
		// Token: 0x04002DEE RID: 11758
		[__DynamicallyInvokable]
		FUNCFLAG_FUSESGETLASTERROR = 128,
		// Token: 0x04002DEF RID: 11759
		[__DynamicallyInvokable]
		FUNCFLAG_FDEFAULTCOLLELEM = 256,
		// Token: 0x04002DF0 RID: 11760
		[__DynamicallyInvokable]
		FUNCFLAG_FUIDEFAULT = 512,
		// Token: 0x04002DF1 RID: 11761
		[__DynamicallyInvokable]
		FUNCFLAG_FNONBROWSABLE = 1024,
		// Token: 0x04002DF2 RID: 11762
		[__DynamicallyInvokable]
		FUNCFLAG_FREPLACEABLE = 2048,
		// Token: 0x04002DF3 RID: 11763
		[__DynamicallyInvokable]
		FUNCFLAG_FIMMEDIATEBIND = 4096
	}
}
