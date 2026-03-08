using System;

namespace System.Runtime.CompilerServices
{
	// Token: 0x020008D4 RID: 2260
	internal struct ObjectHandleOnStack
	{
		// Token: 0x06005DC5 RID: 24005 RVA: 0x001499B2 File Offset: 0x00147BB2
		internal ObjectHandleOnStack(IntPtr pObject)
		{
			this.m_ptr = pObject;
		}

		// Token: 0x04002A36 RID: 10806
		private IntPtr m_ptr;
	}
}
