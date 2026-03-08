using System;
using System.Collections.Generic;

namespace TaleWorlds.Library
{
	// Token: 0x0200006F RID: 111
	public class MBReadOnlyQueue<T> : Queue<T>
	{
		// Token: 0x060003E2 RID: 994 RVA: 0x0000DD22 File Offset: 0x0000BF22
		public MBReadOnlyQueue()
		{
		}

		// Token: 0x060003E3 RID: 995 RVA: 0x0000DD2A File Offset: 0x0000BF2A
		public MBReadOnlyQueue(int capacity)
			: base(capacity)
		{
		}

		// Token: 0x060003E4 RID: 996 RVA: 0x0000DD33 File Offset: 0x0000BF33
		public MBReadOnlyQueue(Queue<T> queue)
			: base(queue)
		{
		}

		// Token: 0x060003E5 RID: 997 RVA: 0x0000DD3C File Offset: 0x0000BF3C
		public MBReadOnlyQueue(IEnumerable<T> collection)
			: base(collection)
		{
		}
	}
}
