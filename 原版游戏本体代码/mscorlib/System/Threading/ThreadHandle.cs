using System;

namespace System.Threading
{
	// Token: 0x02000515 RID: 1301
	internal struct ThreadHandle
	{
		// Token: 0x06003D22 RID: 15650 RVA: 0x000E62CC File Offset: 0x000E44CC
		internal ThreadHandle(IntPtr pThread)
		{
			this.m_ptr = pThread;
		}

		// Token: 0x040019E8 RID: 6632
		private IntPtr m_ptr;
	}
}
