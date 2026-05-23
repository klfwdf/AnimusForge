using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Threading
{
	// Token: 0x020004F0 RID: 1264
	[SecurityCritical]
	internal class SafeCompressedStackHandle : SafeHandle
	{
		// Token: 0x06003BAB RID: 15275 RVA: 0x000E298D File Offset: 0x000E0B8D
		public SafeCompressedStackHandle()
			: base(IntPtr.Zero, true)
		{
		}

		// Token: 0x17000908 RID: 2312
		// (get) Token: 0x06003BAC RID: 15276 RVA: 0x000E299B File Offset: 0x000E0B9B
		public override bool IsInvalid
		{
			[SecurityCritical]
			get
			{
				return this.handle == IntPtr.Zero;
			}
		}

		// Token: 0x06003BAD RID: 15277 RVA: 0x000E29AD File Offset: 0x000E0BAD
		[SecurityCritical]
		protected override bool ReleaseHandle()
		{
			CompressedStack.DestroyDelayedCompressedStack(this.handle);
			this.handle = IntPtr.Zero;
			return true;
		}
	}
}
