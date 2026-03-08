using System;

namespace System.Runtime.CompilerServices
{
	// Token: 0x020008D3 RID: 2259
	internal struct StringHandleOnStack
	{
		// Token: 0x06005DC4 RID: 24004 RVA: 0x001499A9 File Offset: 0x00147BA9
		internal StringHandleOnStack(IntPtr pString)
		{
			this.m_ptr = pString;
		}

		// Token: 0x04002A35 RID: 10805
		private IntPtr m_ptr;
	}
}
