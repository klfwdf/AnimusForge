using System;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography.X509Certificates
{
	// Token: 0x020002A7 RID: 679
	[SecurityCritical]
	internal sealed class SafeCertContextHandle : SafeHandleZeroOrMinusOneIsInvalid
	{
		// Token: 0x06002402 RID: 9218 RVA: 0x0008242E File Offset: 0x0008062E
		private SafeCertContextHandle()
			: base(true)
		{
		}

		// Token: 0x06002403 RID: 9219 RVA: 0x00082437 File Offset: 0x00080637
		internal SafeCertContextHandle(IntPtr handle)
			: base(true)
		{
			base.SetHandle(handle);
		}

		// Token: 0x17000489 RID: 1161
		// (get) Token: 0x06002404 RID: 9220 RVA: 0x00082448 File Offset: 0x00080648
		internal static SafeCertContextHandle InvalidHandle
		{
			get
			{
				SafeCertContextHandle safeCertContextHandle = new SafeCertContextHandle(IntPtr.Zero);
				GC.SuppressFinalize(safeCertContextHandle);
				return safeCertContextHandle;
			}
		}

		// Token: 0x1700048A RID: 1162
		// (get) Token: 0x06002405 RID: 9221 RVA: 0x00082467 File Offset: 0x00080667
		internal IntPtr pCertContext
		{
			get
			{
				if (this.handle == IntPtr.Zero)
				{
					return IntPtr.Zero;
				}
				return Marshal.ReadIntPtr(this.handle);
			}
		}

		// Token: 0x06002406 RID: 9222
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[MethodImpl(MethodImplOptions.InternalCall)]
		private static extern void _FreePCertContext(IntPtr pCert);

		// Token: 0x06002407 RID: 9223 RVA: 0x0008248C File Offset: 0x0008068C
		[SecurityCritical]
		protected override bool ReleaseHandle()
		{
			SafeCertContextHandle._FreePCertContext(this.handle);
			return true;
		}
	}
}
