using System;
using System.Collections.Generic;

namespace TaleWorlds.Library
{
	// Token: 0x0200006C RID: 108
	public class MBQueue<T> : MBReadOnlyQueue<T>, IMBCollection
	{
		// Token: 0x060003CC RID: 972 RVA: 0x0000DACE File Offset: 0x0000BCCE
		public MBQueue()
		{
		}

		// Token: 0x060003CD RID: 973 RVA: 0x0000DAD6 File Offset: 0x0000BCD6
		public MBQueue(int capacity)
			: base(capacity)
		{
		}

		// Token: 0x060003CE RID: 974 RVA: 0x0000DADF File Offset: 0x0000BCDF
		public MBQueue(Queue<T> queue)
			: base(queue)
		{
		}

		// Token: 0x060003CF RID: 975 RVA: 0x0000DAE8 File Offset: 0x0000BCE8
		public MBQueue(IEnumerable<T> collection)
			: base(collection)
		{
		}

		// Token: 0x060003D0 RID: 976 RVA: 0x0000DAF4 File Offset: 0x0000BCF4
		public bool Remove(T item)
		{
			EqualityComparer<T> @default = EqualityComparer<T>.Default;
			int count = base.Count;
			bool flag = false;
			for (int i = 0; i < count; i++)
			{
				T t = base.Dequeue();
				if (!flag && @default.Equals(t, item))
				{
					flag = true;
				}
				else
				{
					base.Enqueue(t);
				}
			}
			return flag;
		}

		// Token: 0x060003D1 RID: 977 RVA: 0x0000DB3F File Offset: 0x0000BD3F
		void IMBCollection.Clear()
		{
			base.Clear();
		}
	}
}
