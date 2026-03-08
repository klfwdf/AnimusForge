using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Deployment.Internal.Isolation
{
	// Token: 0x020006A4 RID: 1700
	internal struct StoreOperationUnpinDeployment
	{
		// Token: 0x06004FCF RID: 20431 RVA: 0x0011C9B8 File Offset: 0x0011ABB8
		[SecuritySafeCritical]
		public StoreOperationUnpinDeployment(IDefinitionAppId app, StoreApplicationReference reference)
		{
			this.Size = (uint)Marshal.SizeOf(typeof(StoreOperationUnpinDeployment));
			this.Flags = StoreOperationUnpinDeployment.OpFlags.Nothing;
			this.Application = app;
			this.Reference = reference.ToIntPtr();
		}

		// Token: 0x06004FD0 RID: 20432 RVA: 0x0011C9EA File Offset: 0x0011ABEA
		[SecurityCritical]
		public void Destroy()
		{
			StoreApplicationReference.Destroy(this.Reference);
		}

		// Token: 0x04002239 RID: 8761
		[MarshalAs(UnmanagedType.U4)]
		public uint Size;

		// Token: 0x0400223A RID: 8762
		[MarshalAs(UnmanagedType.U4)]
		public StoreOperationUnpinDeployment.OpFlags Flags;

		// Token: 0x0400223B RID: 8763
		[MarshalAs(UnmanagedType.Interface)]
		public IDefinitionAppId Application;

		// Token: 0x0400223C RID: 8764
		public IntPtr Reference;

		// Token: 0x02000C4B RID: 3147
		[Flags]
		public enum OpFlags
		{
			// Token: 0x04003776 RID: 14198
			Nothing = 0
		}

		// Token: 0x02000C4C RID: 3148
		public enum Disposition
		{
			// Token: 0x04003778 RID: 14200
			Failed,
			// Token: 0x04003779 RID: 14201
			Unpinned
		}
	}
}
