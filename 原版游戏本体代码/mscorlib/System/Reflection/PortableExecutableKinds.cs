using System;
using System.Runtime.InteropServices;

namespace System.Reflection
{
	// Token: 0x0200060B RID: 1547
	[Flags]
	[ComVisible(true)]
	[Serializable]
	public enum PortableExecutableKinds
	{
		// Token: 0x04001DB0 RID: 7600
		NotAPortableExecutableImage = 0,
		// Token: 0x04001DB1 RID: 7601
		ILOnly = 1,
		// Token: 0x04001DB2 RID: 7602
		Required32Bit = 2,
		// Token: 0x04001DB3 RID: 7603
		PE32Plus = 4,
		// Token: 0x04001DB4 RID: 7604
		Unmanaged32Bit = 8,
		// Token: 0x04001DB5 RID: 7605
		[ComVisible(false)]
		Preferred32Bit = 16
	}
}
