using System;
using System.Runtime.InteropServices;

namespace System.Deployment.Internal.Isolation
{
	// Token: 0x020006AF RID: 1711
	internal struct IStore_BindingResult_BoundVersion
	{
		// Token: 0x04002269 RID: 8809
		[MarshalAs(UnmanagedType.U2)]
		public ushort Revision;

		// Token: 0x0400226A RID: 8810
		[MarshalAs(UnmanagedType.U2)]
		public ushort Build;

		// Token: 0x0400226B RID: 8811
		[MarshalAs(UnmanagedType.U2)]
		public ushort Minor;

		// Token: 0x0400226C RID: 8812
		[MarshalAs(UnmanagedType.U2)]
		public ushort Major;
	}
}
