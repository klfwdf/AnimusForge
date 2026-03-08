using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Win32.SafeHandles
{
	// Token: 0x0200001F RID: 31
	[SecurityCritical]
	public sealed class SafeRegistryHandle : SafeHandleZeroOrMinusOneIsInvalid
	{
		// Token: 0x06000168 RID: 360 RVA: 0x000047B4 File Offset: 0x000029B4
		[SecurityCritical]
		internal SafeRegistryHandle()
			: base(true)
		{
		}

		// Token: 0x06000169 RID: 361 RVA: 0x000047BD File Offset: 0x000029BD
		[SecurityCritical]
		public SafeRegistryHandle(IntPtr preexistingHandle, bool ownsHandle)
			: base(ownsHandle)
		{
			base.SetHandle(preexistingHandle);
		}

		// Token: 0x0600016A RID: 362 RVA: 0x000047CD File Offset: 0x000029CD
		[SecurityCritical]
		protected override bool ReleaseHandle()
		{
			return SafeRegistryHandle.RegCloseKey(this.handle) == 0;
		}

		// Token: 0x0600016B RID: 363
		[SuppressUnmanagedCodeSecurity]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[DllImport("advapi32.dll")]
		internal static extern int RegCloseKey(IntPtr hKey);
	}
}
