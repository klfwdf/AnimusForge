using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Deployment.Internal.Isolation
{
	// Token: 0x020006A9 RID: 1705
	internal struct StoreOperationSetCanonicalizationContext
	{
		// Token: 0x06004FDD RID: 20445 RVA: 0x0011CD0E File Offset: 0x0011AF0E
		[SecurityCritical]
		public StoreOperationSetCanonicalizationContext(string Bases, string Exports)
		{
			this.Size = (uint)Marshal.SizeOf(typeof(StoreOperationSetCanonicalizationContext));
			this.Flags = StoreOperationSetCanonicalizationContext.OpFlags.Nothing;
			this.BaseAddressFilePath = Bases;
			this.ExportsFilePath = Exports;
		}

		// Token: 0x06004FDE RID: 20446 RVA: 0x0011CD3A File Offset: 0x0011AF3A
		public void Destroy()
		{
		}

		// Token: 0x04002251 RID: 8785
		[MarshalAs(UnmanagedType.U4)]
		public uint Size;

		// Token: 0x04002252 RID: 8786
		[MarshalAs(UnmanagedType.U4)]
		public StoreOperationSetCanonicalizationContext.OpFlags Flags;

		// Token: 0x04002253 RID: 8787
		[MarshalAs(UnmanagedType.LPWStr)]
		public string BaseAddressFilePath;

		// Token: 0x04002254 RID: 8788
		[MarshalAs(UnmanagedType.LPWStr)]
		public string ExportsFilePath;

		// Token: 0x02000C53 RID: 3155
		[Flags]
		public enum OpFlags
		{
			// Token: 0x0400378D RID: 14221
			Nothing = 0
		}
	}
}
