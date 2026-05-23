using System;
using System.Security;

namespace Microsoft.Win32.SafeHandles
{
	// Token: 0x0200001A RID: 26
	[SecurityCritical]
	public sealed class SafeFileHandle : SafeHandleZeroOrMinusOneIsInvalid
	{
		// Token: 0x06000159 RID: 345 RVA: 0x000046FF File Offset: 0x000028FF
		private SafeFileHandle()
			: base(true)
		{
		}

		// Token: 0x0600015A RID: 346 RVA: 0x00004708 File Offset: 0x00002908
		public SafeFileHandle(IntPtr preexistingHandle, bool ownsHandle)
			: base(ownsHandle)
		{
			base.SetHandle(preexistingHandle);
		}

		// Token: 0x0600015B RID: 347 RVA: 0x00004718 File Offset: 0x00002918
		[SecurityCritical]
		protected override bool ReleaseHandle()
		{
			return Win32Native.CloseHandle(this.handle);
		}
	}
}
