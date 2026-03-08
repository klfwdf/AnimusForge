using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Deployment.Internal.Isolation
{
	// Token: 0x020006A6 RID: 1702
	internal struct StoreOperationUninstallDeployment
	{
		// Token: 0x06004FD4 RID: 20436 RVA: 0x0011CA5F File Offset: 0x0011AC5F
		[SecuritySafeCritical]
		public StoreOperationUninstallDeployment(IDefinitionAppId appid, StoreApplicationReference AppRef)
		{
			this.Size = (uint)Marshal.SizeOf(typeof(StoreOperationUninstallDeployment));
			this.Flags = StoreOperationUninstallDeployment.OpFlags.Nothing;
			this.Application = appid;
			this.Reference = AppRef.ToIntPtr();
		}

		// Token: 0x06004FD5 RID: 20437 RVA: 0x0011CA91 File Offset: 0x0011AC91
		[SecurityCritical]
		public void Destroy()
		{
			StoreApplicationReference.Destroy(this.Reference);
		}

		// Token: 0x04002241 RID: 8769
		[MarshalAs(UnmanagedType.U4)]
		public uint Size;

		// Token: 0x04002242 RID: 8770
		[MarshalAs(UnmanagedType.U4)]
		public StoreOperationUninstallDeployment.OpFlags Flags;

		// Token: 0x04002243 RID: 8771
		[MarshalAs(UnmanagedType.Interface)]
		public IDefinitionAppId Application;

		// Token: 0x04002244 RID: 8772
		public IntPtr Reference;

		// Token: 0x02000C4F RID: 3151
		[Flags]
		public enum OpFlags
		{
			// Token: 0x04003782 RID: 14210
			Nothing = 0
		}

		// Token: 0x02000C50 RID: 3152
		public enum Disposition
		{
			// Token: 0x04003784 RID: 14212
			Failed,
			// Token: 0x04003785 RID: 14213
			DidNotExist,
			// Token: 0x04003786 RID: 14214
			Uninstalled
		}
	}
}
