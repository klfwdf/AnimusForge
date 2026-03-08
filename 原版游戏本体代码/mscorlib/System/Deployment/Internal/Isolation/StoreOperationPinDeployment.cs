using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Deployment.Internal.Isolation
{
	// Token: 0x020006A3 RID: 1699
	internal struct StoreOperationPinDeployment
	{
		// Token: 0x06004FCC RID: 20428 RVA: 0x0011C959 File Offset: 0x0011AB59
		[SecuritySafeCritical]
		public StoreOperationPinDeployment(IDefinitionAppId AppId, StoreApplicationReference Ref)
		{
			this.Size = (uint)Marshal.SizeOf(typeof(StoreOperationPinDeployment));
			this.Flags = StoreOperationPinDeployment.OpFlags.NeverExpires;
			this.Application = AppId;
			this.Reference = Ref.ToIntPtr();
			this.ExpirationTime = 0L;
		}

		// Token: 0x06004FCD RID: 20429 RVA: 0x0011C993 File Offset: 0x0011AB93
		public StoreOperationPinDeployment(IDefinitionAppId AppId, DateTime Expiry, StoreApplicationReference Ref)
		{
			this = new StoreOperationPinDeployment(AppId, Ref);
			this.Flags |= StoreOperationPinDeployment.OpFlags.NeverExpires;
		}

		// Token: 0x06004FCE RID: 20430 RVA: 0x0011C9AB File Offset: 0x0011ABAB
		[SecurityCritical]
		public void Destroy()
		{
			StoreApplicationReference.Destroy(this.Reference);
		}

		// Token: 0x04002234 RID: 8756
		[MarshalAs(UnmanagedType.U4)]
		public uint Size;

		// Token: 0x04002235 RID: 8757
		[MarshalAs(UnmanagedType.U4)]
		public StoreOperationPinDeployment.OpFlags Flags;

		// Token: 0x04002236 RID: 8758
		[MarshalAs(UnmanagedType.Interface)]
		public IDefinitionAppId Application;

		// Token: 0x04002237 RID: 8759
		[MarshalAs(UnmanagedType.I8)]
		public long ExpirationTime;

		// Token: 0x04002238 RID: 8760
		public IntPtr Reference;

		// Token: 0x02000C49 RID: 3145
		[Flags]
		public enum OpFlags
		{
			// Token: 0x04003770 RID: 14192
			Nothing = 0,
			// Token: 0x04003771 RID: 14193
			NeverExpires = 1
		}

		// Token: 0x02000C4A RID: 3146
		public enum Disposition
		{
			// Token: 0x04003773 RID: 14195
			Failed,
			// Token: 0x04003774 RID: 14196
			Pinned
		}
	}
}
