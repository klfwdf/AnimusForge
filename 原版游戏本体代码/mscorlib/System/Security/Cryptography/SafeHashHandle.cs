using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
	// Token: 0x02000290 RID: 656
	[SecurityCritical]
	internal sealed class SafeHashHandle : SafeHandleZeroOrMinusOneIsInvalid
	{
		// Token: 0x0600234C RID: 9036 RVA: 0x00080318 File Offset: 0x0007E518
		private SafeHashHandle()
			: base(true)
		{
			base.SetHandle(IntPtr.Zero);
		}

		// Token: 0x0600234D RID: 9037 RVA: 0x0008032C File Offset: 0x0007E52C
		private SafeHashHandle(IntPtr handle)
			: base(true)
		{
			base.SetHandle(handle);
		}

		// Token: 0x17000476 RID: 1142
		// (get) Token: 0x0600234E RID: 9038 RVA: 0x0008033C File Offset: 0x0007E53C
		internal static SafeHashHandle InvalidHandle
		{
			get
			{
				return new SafeHashHandle();
			}
		}

		// Token: 0x0600234F RID: 9039
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void FreeHash(IntPtr pHashContext);

		// Token: 0x06002350 RID: 9040 RVA: 0x00080343 File Offset: 0x0007E543
		[SecurityCritical]
		protected override bool ReleaseHandle()
		{
			SafeHashHandle.FreeHash(this.handle);
			return true;
		}
	}
}
