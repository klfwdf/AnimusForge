using System;
using System.Security;
using System.Security.Permissions;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.Win32
{
	// Token: 0x02000019 RID: 25
	[SecurityCritical]
	[HostProtection(SecurityAction.LinkDemand, MayLeakOnAbort = true)]
	internal sealed class SafeLibraryHandle : SafeHandleZeroOrMinusOneIsInvalid
	{
		// Token: 0x06000157 RID: 343 RVA: 0x000046E9 File Offset: 0x000028E9
		internal SafeLibraryHandle()
			: base(true)
		{
		}

		// Token: 0x06000158 RID: 344 RVA: 0x000046F2 File Offset: 0x000028F2
		[SecurityCritical]
		protected override bool ReleaseHandle()
		{
			return UnsafeNativeMethods.FreeLibrary(this.handle);
		}
	}
}
