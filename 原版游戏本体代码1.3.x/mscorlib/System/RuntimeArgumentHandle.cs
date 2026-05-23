using System;
using System.Runtime.InteropServices;

namespace System
{
	// Token: 0x0200012F RID: 303
	[ComVisible(true)]
	public struct RuntimeArgumentHandle
	{
		// Token: 0x170001EC RID: 492
		// (get) Token: 0x060011BB RID: 4539 RVA: 0x00036BB1 File Offset: 0x00034DB1
		internal IntPtr Value
		{
			get
			{
				return this.m_ptr;
			}
		}

		// Token: 0x0400065E RID: 1630
		private IntPtr m_ptr;
	}
}
