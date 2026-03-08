using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
	// Token: 0x0200028E RID: 654
	[SecurityCritical]
	internal sealed class SafeProvHandle : SafeHandleZeroOrMinusOneIsInvalid
	{
		// Token: 0x06002342 RID: 9026 RVA: 0x000802A6 File Offset: 0x0007E4A6
		private SafeProvHandle()
			: base(true)
		{
			base.SetHandle(IntPtr.Zero);
		}

		// Token: 0x06002343 RID: 9027 RVA: 0x000802BA File Offset: 0x0007E4BA
		private SafeProvHandle(IntPtr handle)
			: base(true)
		{
			base.SetHandle(handle);
		}

		// Token: 0x17000474 RID: 1140
		// (get) Token: 0x06002344 RID: 9028 RVA: 0x000802CA File Offset: 0x0007E4CA
		internal static SafeProvHandle InvalidHandle
		{
			get
			{
				return new SafeProvHandle();
			}
		}

		// Token: 0x06002345 RID: 9029
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void FreeCsp(IntPtr pProviderContext);

		// Token: 0x06002346 RID: 9030 RVA: 0x000802D1 File Offset: 0x0007E4D1
		[SecurityCritical]
		protected override bool ReleaseHandle()
		{
			SafeProvHandle.FreeCsp(this.handle);
			return true;
		}
	}
}
