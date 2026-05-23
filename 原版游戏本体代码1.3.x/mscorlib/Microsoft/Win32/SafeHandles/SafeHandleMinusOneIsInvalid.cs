using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;

namespace Microsoft.Win32.SafeHandles
{
	// Token: 0x02000023 RID: 35
	[SecurityCritical]
	[SecurityPermission(SecurityAction.InheritanceDemand, UnmanagedCode = true)]
	public abstract class SafeHandleMinusOneIsInvalid : SafeHandle
	{
		// Token: 0x06000174 RID: 372 RVA: 0x00004869 File Offset: 0x00002A69
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		protected SafeHandleMinusOneIsInvalid(bool ownsHandle)
			: base(new IntPtr(-1), ownsHandle)
		{
		}

		// Token: 0x17000009 RID: 9
		// (get) Token: 0x06000175 RID: 373 RVA: 0x00004878 File Offset: 0x00002A78
		public override bool IsInvalid
		{
			[SecurityCritical]
			get
			{
				return this.handle == new IntPtr(-1);
			}
		}
	}
}
