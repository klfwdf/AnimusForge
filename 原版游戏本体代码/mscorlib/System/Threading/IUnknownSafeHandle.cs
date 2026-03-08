using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Threading
{
	// Token: 0x020004FC RID: 1276
	[SecurityCritical]
	internal class IUnknownSafeHandle : SafeHandle
	{
		// Token: 0x06003C45 RID: 15429 RVA: 0x000E4076 File Offset: 0x000E2276
		public IUnknownSafeHandle()
			: base(IntPtr.Zero, true)
		{
		}

		// Token: 0x17000919 RID: 2329
		// (get) Token: 0x06003C46 RID: 15430 RVA: 0x000E4084 File Offset: 0x000E2284
		public override bool IsInvalid
		{
			[SecurityCritical]
			get
			{
				return this.handle == IntPtr.Zero;
			}
		}

		// Token: 0x06003C47 RID: 15431 RVA: 0x000E4096 File Offset: 0x000E2296
		[SecurityCritical]
		protected override bool ReleaseHandle()
		{
			HostExecutionContextManager.ReleaseHostSecurityContext(this.handle);
			return true;
		}

		// Token: 0x06003C48 RID: 15432 RVA: 0x000E40A8 File Offset: 0x000E22A8
		internal object Clone()
		{
			IUnknownSafeHandle unknownSafeHandle = new IUnknownSafeHandle();
			if (!this.IsInvalid)
			{
				HostExecutionContextManager.CloneHostSecurityContext(this, unknownSafeHandle);
			}
			return unknownSafeHandle;
		}
	}
}
