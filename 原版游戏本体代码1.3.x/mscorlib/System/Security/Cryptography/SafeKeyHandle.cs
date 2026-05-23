using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
	// Token: 0x0200028F RID: 655
	[SecurityCritical]
	internal sealed class SafeKeyHandle : SafeHandleZeroOrMinusOneIsInvalid
	{
		// Token: 0x06002347 RID: 9031 RVA: 0x000802DF File Offset: 0x0007E4DF
		private SafeKeyHandle()
			: base(true)
		{
			base.SetHandle(IntPtr.Zero);
		}

		// Token: 0x06002348 RID: 9032 RVA: 0x000802F3 File Offset: 0x0007E4F3
		private SafeKeyHandle(IntPtr handle)
			: base(true)
		{
			base.SetHandle(handle);
		}

		// Token: 0x17000475 RID: 1141
		// (get) Token: 0x06002349 RID: 9033 RVA: 0x00080303 File Offset: 0x0007E503
		internal static SafeKeyHandle InvalidHandle
		{
			get
			{
				return new SafeKeyHandle();
			}
		}

		// Token: 0x0600234A RID: 9034
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void FreeKey(IntPtr pKeyCotext);

		// Token: 0x0600234B RID: 9035 RVA: 0x0008030A File Offset: 0x0007E50A
		[SecurityCritical]
		protected override bool ReleaseHandle()
		{
			SafeKeyHandle.FreeKey(this.handle);
			return true;
		}
	}
}
