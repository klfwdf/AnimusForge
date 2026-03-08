using System;
using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace System.Runtime.ConstrainedExecution
{
	// Token: 0x02000729 RID: 1833
	[ComVisible(true)]
	[SecurityPermission(SecurityAction.InheritanceDemand, UnmanagedCode = true)]
	public abstract class CriticalFinalizerObject
	{
		// Token: 0x06005172 RID: 20850 RVA: 0x0011F1B6 File Offset: 0x0011D3B6
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		protected CriticalFinalizerObject()
		{
		}

		// Token: 0x06005173 RID: 20851 RVA: 0x0011F1C0 File Offset: 0x0011D3C0
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		~CriticalFinalizerObject()
		{
		}
	}
}
