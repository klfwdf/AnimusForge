using System;
using System.Runtime.InteropServices;

namespace System.Deployment.Internal.Isolation
{
	// Token: 0x02000679 RID: 1657
	internal struct STORE_ASSEMBLY_FILE
	{
		// Token: 0x040021F0 RID: 8688
		public uint Size;

		// Token: 0x040021F1 RID: 8689
		public uint Flags;

		// Token: 0x040021F2 RID: 8690
		[MarshalAs(UnmanagedType.LPWStr)]
		public string FileName;

		// Token: 0x040021F3 RID: 8691
		public uint FileStatusFlags;
	}
}
