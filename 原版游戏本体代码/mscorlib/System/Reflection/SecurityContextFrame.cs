using System;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Security;

namespace System.Reflection
{
	// Token: 0x020005DC RID: 1500
	internal struct SecurityContextFrame
	{
		// Token: 0x0600456E RID: 17774
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern void Push(RuntimeAssembly assembly);

		// Token: 0x0600456F RID: 17775
		[SecurityCritical]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern void Pop();

		// Token: 0x04001C87 RID: 7303
		private IntPtr m_GSCookie;

		// Token: 0x04001C88 RID: 7304
		private IntPtr __VFN_table;

		// Token: 0x04001C89 RID: 7305
		private IntPtr m_Next;

		// Token: 0x04001C8A RID: 7306
		private IntPtr m_Assembly;
	}
}
