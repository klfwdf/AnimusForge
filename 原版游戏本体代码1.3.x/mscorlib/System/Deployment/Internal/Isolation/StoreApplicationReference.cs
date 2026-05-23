using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Deployment.Internal.Isolation
{
	// Token: 0x020006A2 RID: 1698
	internal struct StoreApplicationReference
	{
		// Token: 0x06004FC9 RID: 20425 RVA: 0x0011C8D5 File Offset: 0x0011AAD5
		public StoreApplicationReference(Guid RefScheme, string Id, string NcData)
		{
			this.Size = (uint)Marshal.SizeOf(typeof(StoreApplicationReference));
			this.Flags = StoreApplicationReference.RefFlags.Nothing;
			this.GuidScheme = RefScheme;
			this.Identifier = Id;
			this.NonCanonicalData = NcData;
		}

		// Token: 0x06004FCA RID: 20426 RVA: 0x0011C908 File Offset: 0x0011AB08
		[SecurityCritical]
		public IntPtr ToIntPtr()
		{
			IntPtr intPtr = Marshal.AllocCoTaskMem(Marshal.SizeOf<StoreApplicationReference>(this));
			Marshal.StructureToPtr<StoreApplicationReference>(this, intPtr, false);
			return intPtr;
		}

		// Token: 0x06004FCB RID: 20427 RVA: 0x0011C934 File Offset: 0x0011AB34
		[SecurityCritical]
		public static void Destroy(IntPtr ip)
		{
			if (ip != IntPtr.Zero)
			{
				Marshal.DestroyStructure(ip, typeof(StoreApplicationReference));
				Marshal.FreeCoTaskMem(ip);
			}
		}

		// Token: 0x0400222F RID: 8751
		[MarshalAs(UnmanagedType.U4)]
		public uint Size;

		// Token: 0x04002230 RID: 8752
		[MarshalAs(UnmanagedType.U4)]
		public StoreApplicationReference.RefFlags Flags;

		// Token: 0x04002231 RID: 8753
		public Guid GuidScheme;

		// Token: 0x04002232 RID: 8754
		[MarshalAs(UnmanagedType.LPWStr)]
		public string Identifier;

		// Token: 0x04002233 RID: 8755
		[MarshalAs(UnmanagedType.LPWStr)]
		public string NonCanonicalData;

		// Token: 0x02000C48 RID: 3144
		[Flags]
		public enum RefFlags
		{
			// Token: 0x0400376E RID: 14190
			Nothing = 0
		}
	}
}
