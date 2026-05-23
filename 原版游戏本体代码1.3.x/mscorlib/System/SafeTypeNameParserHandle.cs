using System;
using System.Runtime.InteropServices;
using System.Security;
using Microsoft.Win32.SafeHandles;

namespace System
{
	// Token: 0x0200014A RID: 330
	[SecurityCritical]
	internal class SafeTypeNameParserHandle : SafeHandleZeroOrMinusOneIsInvalid
	{
		// Token: 0x060014B2 RID: 5298
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void _ReleaseTypeNameParser(IntPtr pTypeNameParser);

		// Token: 0x060014B3 RID: 5299 RVA: 0x0003D54D File Offset: 0x0003B74D
		public SafeTypeNameParserHandle()
			: base(true)
		{
		}

		// Token: 0x060014B4 RID: 5300 RVA: 0x0003D556 File Offset: 0x0003B756
		[SecurityCritical]
		protected override bool ReleaseHandle()
		{
			SafeTypeNameParserHandle._ReleaseTypeNameParser(this.handle);
			this.handle = IntPtr.Zero;
			return true;
		}
	}
}
