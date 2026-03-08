using System;
using System.Collections.Generic;

namespace TaleWorlds.Library
{
	// Token: 0x0200006E RID: 110
	public class MBReadOnlyList<T> : List<T>
	{
		// Token: 0x060003DF RID: 991 RVA: 0x0000DD08 File Offset: 0x0000BF08
		public MBReadOnlyList()
		{
		}

		// Token: 0x060003E0 RID: 992 RVA: 0x0000DD10 File Offset: 0x0000BF10
		public MBReadOnlyList(int capacity)
			: base(capacity)
		{
		}

		// Token: 0x060003E1 RID: 993 RVA: 0x0000DD19 File Offset: 0x0000BF19
		public MBReadOnlyList(IEnumerable<T> collection)
			: base(collection)
		{
		}
	}
}
