using System;
using System.Runtime.ConstrainedExecution;
using System.Security;

namespace Microsoft.Win32.SafeHandles
{
	// Token: 0x02000021 RID: 33
	[SecurityCritical]
	[__DynamicallyInvokable]
	public sealed class SafeWaitHandle : SafeHandleZeroOrMinusOneIsInvalid
	{
		// Token: 0x0600016F RID: 367 RVA: 0x00004813 File Offset: 0x00002A13
		private SafeWaitHandle()
			: base(true)
		{
		}

		// Token: 0x06000170 RID: 368 RVA: 0x0000481C File Offset: 0x00002A1C
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		public SafeWaitHandle(IntPtr existingHandle, bool ownsHandle)
			: base(ownsHandle)
		{
			base.SetHandle(existingHandle);
		}

		// Token: 0x06000171 RID: 369 RVA: 0x0000482C File Offset: 0x00002A2C
		[SecurityCritical]
		protected override bool ReleaseHandle()
		{
			return Win32Native.CloseHandle(this.handle);
		}
	}
}
