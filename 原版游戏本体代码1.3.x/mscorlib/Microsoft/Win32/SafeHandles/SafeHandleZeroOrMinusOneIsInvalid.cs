using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;

namespace Microsoft.Win32.SafeHandles
{
	// Token: 0x02000022 RID: 34
	[SecurityCritical]
	[SecurityPermission(SecurityAction.InheritanceDemand, UnmanagedCode = true)]
	public abstract class SafeHandleZeroOrMinusOneIsInvalid : SafeHandle
	{
		// Token: 0x06000172 RID: 370 RVA: 0x00004839 File Offset: 0x00002A39
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		protected SafeHandleZeroOrMinusOneIsInvalid(bool ownsHandle)
			: base(IntPtr.Zero, ownsHandle)
		{
		}

		// Token: 0x17000008 RID: 8
		// (get) Token: 0x06000173 RID: 371 RVA: 0x00004847 File Offset: 0x00002A47
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
