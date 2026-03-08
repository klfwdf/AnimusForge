using System;

namespace System.Runtime.CompilerServices
{
	// Token: 0x020008D5 RID: 2261
	internal struct StackCrawlMarkHandle
	{
		// Token: 0x06005DC6 RID: 24006 RVA: 0x001499BB File Offset: 0x00147BBB
		internal StackCrawlMarkHandle(IntPtr stackMark)
		{
			this.m_ptr = stackMark;
		}

		// Token: 0x04002A37 RID: 10807
		private IntPtr m_ptr;
	}
}
