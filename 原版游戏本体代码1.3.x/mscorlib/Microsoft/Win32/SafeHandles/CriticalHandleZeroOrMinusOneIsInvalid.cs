using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;

namespace Microsoft.Win32.SafeHandles
{
	// Token: 0x02000024 RID: 36
	[SecurityCritical]
	[SecurityPermission(SecurityAction.InheritanceDemand, UnmanagedCode = true)]
	public abstract class CriticalHandleZeroOrMinusOneIsInvalid : CriticalHandle
	{
		// Token: 0x06000176 RID: 374 RVA: 0x0000488B File Offset: 0x00002A8B
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		protected CriticalHandleZeroOrMinusOneIsInvalid()
			: base(IntPtr.Zero)
		{
		}

		// Token: 0x1700000A RID: 10
		// (get) Token: 0x06000177 RID: 375 RVA: 0x00004898 File Offset: 0x00002A98
		public override bool IsInvalid
		{
			[SecurityCritical]
			get
			{
				return this.handle.IsNull() || this.handle == new IntPtr(-1);
			}
		}
	}
}
