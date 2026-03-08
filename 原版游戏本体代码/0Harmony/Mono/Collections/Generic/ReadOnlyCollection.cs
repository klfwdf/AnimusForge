using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Mono.Collections.Generic
{
	// Token: 0x020001E0 RID: 480
	internal sealed class ReadOnlyCollection<T> : Collection<T>, ICollection<!0>, IEnumerable<!0>, IEnumerable, IList, ICollection
	{
		// Token: 0x17000221 RID: 545
		// (get) Token: 0x060008D4 RID: 2260 RVA: 0x0001C598 File Offset: 0x0001A798
		public static ReadOnlyCollection<T> Empty
		{
			get
			{
				if (ReadOnlyCollection<T>.empty != null)
				{
					return ReadOnlyCollection<T>.empty;
				}
				Interlocked.CompareExchange<ReadOnlyCollection<T>>(ref ReadOnlyCollection<T>.empty, new ReadOnlyCollection<T>(), null);
				return ReadOnlyCollection<T>.empty;
			}
		}

		// Token: 0x17000222 RID: 546
		// (get) Token: 0x060008D5 RID: 2261 RVA: 0x0001B6C7 File Offset: 0x000198C7
		bool ICollection<!0>.IsReadOnly
		{
			get
			{
				return true;
			}
		}

		// Token: 0x17000223 RID: 547
		// (get) Token: 0x060008D6 RID: 2262 RVA: 0x0001B6C7 File Offset: 0x000198C7
		bool IList.IsFixedSize
		{
			get
			{
				return true;
			}
		}

		// Token: 0x17000224 RID: 548
		// (get) Token: 0x060008D7 RID: 2263 RVA: 0x0001B6C7 File Offset: 0x000198C7
		bool IList.IsReadOnly
		{
			get
			{
				return true;
			}
		}

		// Token: 0x060008D8 RID: 2264 RVA: 0x0001C5BD File Offset: 0x0001A7BD
		private ReadOnlyCollection()
		{
		}

		// Token: 0x060008D9 RID: 2265 RVA: 0x0001C5C5 File Offset: 0x0001A7C5
		public ReadOnlyCollection(T[] array)
		{
			if (array == null)
			{
				throw new ArgumentNullException();
			}
			this.Initialize(array, array.Length);
		}

		// Token: 0x060008DA RID: 2266 RVA: 0x0001C5E0 File Offset: 0x0001A7E0
		public ReadOnlyCollection(Collection<T> collection)
		{
			if (collection == null)
			{
				throw new ArgumentNullException();
			}
			this.Initialize(collection.items, collection.size);
		}

		// Token: 0x060008DB RID: 2267 RVA: 0x0001C603 File Offset: 0x0001A803
		private void Initialize(T[] items, int size)
		{
			this.items = new T[size];
			Array.Copy(items, 0, this.items, 0, size);
			this.size = size;
		}

		// Token: 0x060008DC RID: 2268 RVA: 0x0001C627 File Offset: 0x0001A827
		internal override void Grow(int desired)
		{
			throw new InvalidOperationException();
		}

		// Token: 0x060008DD RID: 2269 RVA: 0x0001C627 File Offset: 0x0001A827
		protected override void OnAdd(T item, int index)
		{
			throw new InvalidOperationException();
		}

		// Token: 0x060008DE RID: 2270 RVA: 0x0001C627 File Offset: 0x0001A827
		protected override void OnClear()
		{
			throw new InvalidOperationException();
		}

		// Token: 0x060008DF RID: 2271 RVA: 0x0001C627 File Offset: 0x0001A827
		protected override void OnInsert(T item, int index)
		{
			throw new InvalidOperationException();
		}

		// Token: 0x060008E0 RID: 2272 RVA: 0x0001C627 File Offset: 0x0001A827
		protected override void OnRemove(T item, int index)
		{
			throw new InvalidOperationException();
		}

		// Token: 0x060008E1 RID: 2273 RVA: 0x0001C627 File Offset: 0x0001A827
		protected override void OnSet(T item, int index)
		{
			throw new InvalidOperationException();
		}

		// Token: 0x040002EC RID: 748
		private static ReadOnlyCollection<T> empty;
	}
}
