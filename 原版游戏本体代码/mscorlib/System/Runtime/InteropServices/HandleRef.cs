using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x0200094A RID: 2378
	[ComVisible(true)]
	public struct HandleRef
	{
		// Token: 0x060060A6 RID: 24742 RVA: 0x0014CB15 File Offset: 0x0014AD15
		public HandleRef(object wrapper, IntPtr handle)
		{
			this.m_wrapper = wrapper;
			this.m_handle = handle;
		}

		// Token: 0x170010FB RID: 4347
		// (get) Token: 0x060060A7 RID: 24743 RVA: 0x0014CB25 File Offset: 0x0014AD25
		public object Wrapper
		{
			get
			{
				return this.m_wrapper;
			}
		}

		// Token: 0x170010FC RID: 4348
		// (get) Token: 0x060060A8 RID: 24744 RVA: 0x0014CB2D File Offset: 0x0014AD2D
		public IntPtr Handle
		{
			get
			{
				return this.m_handle;
			}
		}

		// Token: 0x060060A9 RID: 24745 RVA: 0x0014CB35 File Offset: 0x0014AD35
		public static explicit operator IntPtr(HandleRef value)
		{
			return value.m_handle;
		}

		// Token: 0x060060AA RID: 24746 RVA: 0x0014CB3D File Offset: 0x0014AD3D
		public static IntPtr ToIntPtr(HandleRef value)
		{
			return value.m_handle;
		}

		// Token: 0x04002B53 RID: 11091
		internal object m_wrapper;

		// Token: 0x04002B54 RID: 11092
		internal IntPtr m_handle;
	}
}
