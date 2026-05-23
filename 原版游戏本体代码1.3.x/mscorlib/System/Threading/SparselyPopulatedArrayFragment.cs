using System;

namespace System.Threading
{
	// Token: 0x0200054A RID: 1354
	internal class SparselyPopulatedArrayFragment<T> where T : class
	{
		// Token: 0x06003F73 RID: 16243 RVA: 0x000EC3CE File Offset: 0x000EA5CE
		internal SparselyPopulatedArrayFragment(int size)
			: this(size, null)
		{
		}

		// Token: 0x06003F74 RID: 16244 RVA: 0x000EC3D8 File Offset: 0x000EA5D8
		internal SparselyPopulatedArrayFragment(int size, SparselyPopulatedArrayFragment<T> prev)
		{
			this.m_elements = new T[size];
			this.m_freeCount = size;
			this.m_prev = prev;
		}

		// Token: 0x17000964 RID: 2404
		internal T this[int index]
		{
			get
			{
				return Volatile.Read<T>(ref this.m_elements[index]);
			}
		}

		// Token: 0x17000965 RID: 2405
		// (get) Token: 0x06003F76 RID: 16246 RVA: 0x000EC411 File Offset: 0x000EA611
		internal int Length
		{
			get
			{
				return this.m_elements.Length;
			}
		}

		// Token: 0x17000966 RID: 2406
		// (get) Token: 0x06003F77 RID: 16247 RVA: 0x000EC41B File Offset: 0x000EA61B
		internal SparselyPopulatedArrayFragment<T> Prev
		{
			get
			{
				return this.m_prev;
			}
		}

		// Token: 0x06003F78 RID: 16248 RVA: 0x000EC428 File Offset: 0x000EA628
		internal T SafeAtomicRemove(int index, T expectedElement)
		{
			T t = Interlocked.CompareExchange<T>(ref this.m_elements[index], default(T), expectedElement);
			if (t != null)
			{
				this.m_freeCount++;
			}
			return t;
		}

		// Token: 0x04001AB9 RID: 6841
		internal readonly T[] m_elements;

		// Token: 0x04001ABA RID: 6842
		internal volatile int m_freeCount;

		// Token: 0x04001ABB RID: 6843
		internal volatile SparselyPopulatedArrayFragment<T> m_next;

		// Token: 0x04001ABC RID: 6844
		internal volatile SparselyPopulatedArrayFragment<T> m_prev;
	}
}
