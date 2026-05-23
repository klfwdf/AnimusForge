using System;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography.X509Certificates
{
	// Token: 0x020002A8 RID: 680
	[SecurityCritical]
	internal sealed class SafeCertStoreHandle : SafeHandleZeroOrMinusOneIsInvalid
	{
		// Token: 0x06002408 RID: 9224 RVA: 0x0008249A File Offset: 0x0008069A
		private SafeCertStoreHandle()
			: base(true)
		{
		}

		// Token: 0x06002409 RID: 9225 RVA: 0x000824A3 File Offset: 0x000806A3
		internal SafeCertStoreHandle(IntPtr handle)
			: base(true)
		{
			base.SetHandle(handle);
		}

		// Token: 0x1700048B RID: 1163
		// (get) Token: 0x0600240A RID: 9226 RVA: 0x000824B4 File Offset: 0x000806B4
		internal static SafeCertStoreHandle InvalidHandle
		{
			get
			{
				SafeCertStoreHandle safeCertStoreHandle = new SafeCertStoreHandle(IntPtr.Zero);
				GC.SuppressFinalize(safeCertStoreHandle);
				return safeCertStoreHandle;
			}
		}

		// Token: 0x0600240B RID: 9227
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void _FreeCertStoreContext(IntPtr hCertStore);

		// Token: 0x0600240C RID: 9228 RVA: 0x000824D3 File Offset: 0x000806D3
		[SecurityCritical]
		protected override bool ReleaseHandle()
		{
			SafeCertStoreHandle._FreeCertStoreContext(this.handle);
			return true;
		}
	}
}
