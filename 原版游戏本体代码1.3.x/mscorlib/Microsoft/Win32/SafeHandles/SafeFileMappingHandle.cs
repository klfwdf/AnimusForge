using System;
using System.Security;

namespace Microsoft.Win32.SafeHandles
{
	// Token: 0x0200001B RID: 27
	[SecurityCritical]
	internal sealed class SafeFileMappingHandle : SafeHandleZeroOrMinusOneIsInvalid
	{
		// Token: 0x0600015C RID: 348 RVA: 0x00004725 File Offset: 0x00002925
		[SecurityCritical]
		internal SafeFileMappingHandle()
			: base(true)
		{
		}

		// Token: 0x0600015D RID: 349 RVA: 0x0000472E File Offset: 0x0000292E
		[SecurityCritical]
		internal SafeFileMappingHandle(IntPtr handle, bool ownsHandle)
			: base(ownsHandle)
		{
			base.SetHandle(handle);
		}

		// Token: 0x0600015E RID: 350 RVA: 0x0000473E File Offset: 0x0000293E
		[SecurityCritical]
		protected override bool ReleaseHandle()
		{
			return Win32Native.CloseHandle(this.handle);
		}
	}
}
