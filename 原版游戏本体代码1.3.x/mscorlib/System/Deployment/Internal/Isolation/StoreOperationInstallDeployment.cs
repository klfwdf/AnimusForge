using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Deployment.Internal.Isolation
{
	// Token: 0x020006A5 RID: 1701
	internal struct StoreOperationInstallDeployment
	{
		// Token: 0x06004FD1 RID: 20433 RVA: 0x0011C9F7 File Offset: 0x0011ABF7
		public StoreOperationInstallDeployment(IDefinitionAppId App, StoreApplicationReference reference)
		{
			this = new StoreOperationInstallDeployment(App, true, reference);
		}

		// Token: 0x06004FD2 RID: 20434 RVA: 0x0011CA04 File Offset: 0x0011AC04
		[SecuritySafeCritical]
		public StoreOperationInstallDeployment(IDefinitionAppId App, bool UninstallOthers, StoreApplicationReference reference)
		{
			this.Size = (uint)Marshal.SizeOf(typeof(StoreOperationInstallDeployment));
			this.Flags = StoreOperationInstallDeployment.OpFlags.Nothing;
			this.Application = App;
			if (UninstallOthers)
			{
				this.Flags |= StoreOperationInstallDeployment.OpFlags.UninstallOthers;
			}
			this.Reference = reference.ToIntPtr();
		}

		// Token: 0x06004FD3 RID: 20435 RVA: 0x0011CA52 File Offset: 0x0011AC52
		[SecurityCritical]
		public void Destroy()
		{
			StoreApplicationReference.Destroy(this.Reference);
		}

		// Token: 0x0400223D RID: 8765
		[MarshalAs(UnmanagedType.U4)]
		public uint Size;

		// Token: 0x0400223E RID: 8766
		[MarshalAs(UnmanagedType.U4)]
		public StoreOperationInstallDeployment.OpFlags Flags;

		// Token: 0x0400223F RID: 8767
		[MarshalAs(UnmanagedType.Interface)]
		public IDefinitionAppId Application;

		// Token: 0x04002240 RID: 8768
		public IntPtr Reference;

		// Token: 0x02000C4D RID: 3149
		[Flags]
		public enum OpFlags
		{
			// Token: 0x0400377B RID: 14203
			Nothing = 0,
			// Token: 0x0400377C RID: 14204
			UninstallOthers = 1
		}

		// Token: 0x02000C4E RID: 3150
		public enum Disposition
		{
			// Token: 0x0400377E RID: 14206
			Failed,
			// Token: 0x0400377F RID: 14207
			AlreadyInstalled,
			// Token: 0x04003780 RID: 14208
			Installed
		}
	}
}
