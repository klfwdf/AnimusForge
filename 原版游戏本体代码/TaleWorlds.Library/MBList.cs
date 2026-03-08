using System;
using System.Collections.Generic;

namespace TaleWorlds.Library
{
	// Token: 0x02000069 RID: 105
	public class MBList<T> : MBReadOnlyList<T>
	{
		// Token: 0x06000351 RID: 849 RVA: 0x0000C167 File Offset: 0x0000A367
		public MBList()
		{
		}

		// Token: 0x06000352 RID: 850 RVA: 0x0000C16F File Offset: 0x0000A36F
		public MBList(int capacity)
			: base(capacity)
		{
		}

		// Token: 0x06000353 RID: 851 RVA: 0x0000C178 File Offset: 0x0000A378
		public MBList(IEnumerable<T> collection)
			: base(collection)
		{
		}

		// Token: 0x06000354 RID: 852 RVA: 0x0000C181 File Offset: 0x0000A381
		public MBList(List<T> collection)
			: base(collection)
		{
		}
	}
}
