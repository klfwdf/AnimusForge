using System;
using System.Runtime.InteropServices;

namespace System.Threading.Tasks
{
	// Token: 0x02000552 RID: 1362
	[StructLayout(LayoutKind.Auto)]
	internal struct IndexRange
	{
		// Token: 0x04001AD1 RID: 6865
		internal long m_nFromInclusive;

		// Token: 0x04001AD2 RID: 6866
		internal long m_nToExclusive;

		// Token: 0x04001AD3 RID: 6867
		internal volatile Shared<long> m_nSharedCurrentIndexOffset;

		// Token: 0x04001AD4 RID: 6868
		internal int m_bRangeFinished;
	}
}
