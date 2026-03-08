using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System
{
	// Token: 0x02000137 RID: 311
	[StructLayout(LayoutKind.Sequential)]
	internal class RuntimeFieldInfoStub : IRuntimeFieldInfo
	{
		// Token: 0x06001286 RID: 4742 RVA: 0x00037838 File Offset: 0x00035A38
		[SecuritySafeCritical]
		public RuntimeFieldInfoStub(IntPtr methodHandleValue, object keepalive)
		{
			this.m_keepalive = keepalive;
			this.m_fieldHandle = new RuntimeFieldHandleInternal(methodHandleValue);
		}

		// Token: 0x170001F8 RID: 504
		// (get) Token: 0x06001287 RID: 4743 RVA: 0x00037853 File Offset: 0x00035A53
		RuntimeFieldHandleInternal IRuntimeFieldInfo.Value
		{
			get
			{
				return this.m_fieldHandle;
			}
		}

		// Token: 0x0400066D RID: 1645
		private object m_keepalive;

		// Token: 0x0400066E RID: 1646
		private object m_c;

		// Token: 0x0400066F RID: 1647
		private object m_d;

		// Token: 0x04000670 RID: 1648
		private int m_b;

		// Token: 0x04000671 RID: 1649
		private object m_e;

		// Token: 0x04000672 RID: 1650
		private object m_f;

		// Token: 0x04000673 RID: 1651
		private RuntimeFieldHandleInternal m_fieldHandle;
	}
}
