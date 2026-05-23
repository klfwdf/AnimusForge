using System;
using System.Collections;
using System.Collections.Generic;
using Mono.Cecil;

namespace Mono.Collections.Generic
{
	// Token: 0x020001DE RID: 478
	internal class Collection<T> : IList<!0>, ICollection<!0>, IEnumerable<!0>, IEnumerable, IList, ICollection
	{
		// Token: 0x17000215 RID: 533
		// (get) Token: 0x060008A0 RID: 2208 RVA: 0x0001BF24 File Offset: 0x0001A124
		public int Count
		{
			get
			{
				return this.size;
			}
		}

		// Token: 0x17000216 RID: 534
		public T this[int index]
		{
			get
			{
				if (index >= this.size)
				{
					throw new ArgumentOutOfRangeException();
				}
				return this.items[index];
			}
			set
			{
				this.CheckIndex(index);
				if (index == this.size)
				{
					throw new ArgumentOutOfRangeException();
				}
				this.OnSet(value, index);
				this.items[index] = value;
			}
		}

		// Token: 0x17000217 RID: 535
		// (get) Token: 0x060008A3 RID: 2211 RVA: 0x0001BF76 File Offset: 0x0001A176
		// (set) Token: 0x060008A4 RID: 2212 RVA: 0x0001BF80 File Offset: 0x0001A180
		public int Capacity
		{
			get
			{
				return this.items.Length;
			}
			set
			{
				if (value < 0 || value < this.size)
				{
					throw new ArgumentOutOfRangeException();
				}
				this.Resize(value);
			}
		}

		// Token: 0x17000218 RID: 536
		// (get) Token: 0x060008A5 RID: 2213 RVA: 0x0001B69F File Offset: 0x0001989F
		bool ICollection<!0>.IsReadOnly
		{
			get
			{
				return false;
			}
		}

		// Token: 0x17000219 RID: 537
		// (get) Token: 0x060008A6 RID: 2214 RVA: 0x0001B69F File Offset: 0x0001989F
		bool IList.IsFixedSize
		{
			get
			{
				return false;
			}
		}

		// Token: 0x1700021A RID: 538
		// (get) Token: 0x060008A7 RID: 2215 RVA: 0x0001B69F File Offset: 0x0001989F
		bool IList.IsReadOnly
		{
			get
			{
				return false;
			}
		}

		// Token: 0x1700021B RID: 539
		object IList.this[int index]
		{
			get
			{
				return this[index];
			}
			set
			{
				this.CheckIndex(index);
				try
				{
					this[index] = (T)((object)value);
					return;
				}
				catch (InvalidCastException)
				{
				}
				catch (NullReferenceException)
				{
				}
				throw new ArgumentException();
			}
		}

		// Token: 0x1700021C RID: 540
		// (get) Token: 0x060008AA RID: 2218 RVA: 0x0001BFF8 File Offset: 0x0001A1F8
		int ICollection.Count
		{
			get
			{
				return this.Count;
			}
		}

		// Token: 0x1700021D RID: 541
		// (get) Token: 0x060008AB RID: 2219 RVA: 0x0001B69F File Offset: 0x0001989F
		bool ICollection.IsSynchronized
		{
			get
			{
				return false;
			}
		}

		// Token: 0x1700021E RID: 542
		// (get) Token: 0x060008AC RID: 2220 RVA: 0x0001B6A2 File Offset: 0x000198A2
		object ICollection.SyncRoot
		{
			get
			{
				return this;
			}
		}

		// Token: 0x060008AD RID: 2221 RVA: 0x0001C000 File Offset: 0x0001A200
		public Collection()
		{
			this.items = Empty<T>.Array;
		}

		// Token: 0x060008AE RID: 2222 RVA: 0x0001C013 File Offset: 0x0001A213
		public Collection(int capacity)
		{
			if (capacity < 0)
			{
				throw new ArgumentOutOfRangeException();
			}
			this.items = ((capacity == 0) ? Empty<T>.Array : new T[capacity]);
		}

		// Token: 0x060008AF RID: 2223 RVA: 0x0001C03C File Offset: 0x0001A23C
		public Collection(ICollection<T> items)
		{
			if (items == null)
			{
				throw new ArgumentNullException("items");
			}
			this.items = new T[items.Count];
			items.CopyTo(this.items, 0);
			this.size = this.items.Length;
		}

		// Token: 0x060008B0 RID: 2224 RVA: 0x0001C08C File Offset: 0x0001A28C
		public void Add(T item)
		{
			if (this.size == this.items.Length)
			{
				this.Grow(1);
			}
			this.OnAdd(item, this.size);
			T[] array = this.items;
			int num = this.size;
			this.size = num + 1;
			array[num] = item;
			this.version++;
		}

		// Token: 0x060008B1 RID: 2225 RVA: 0x0001C0E8 File Offset: 0x0001A2E8
		public bool Contains(T item)
		{
			return this.IndexOf(item) != -1;
		}

		// Token: 0x060008B2 RID: 2226 RVA: 0x0001C0F7 File Offset: 0x0001A2F7
		public int IndexOf(T item)
		{
			return Array.IndexOf<T>(this.items, item, 0, this.size);
		}

		// Token: 0x060008B3 RID: 2227 RVA: 0x0001C10C File Offset: 0x0001A30C
		public void Insert(int index, T item)
		{
			this.CheckIndex(index);
			if (this.size == this.items.Length)
			{
				this.Grow(1);
			}
			this.OnInsert(item, index);
			this.Shift(index, 1);
			this.items[index] = item;
			this.version++;
		}

		// Token: 0x060008B4 RID: 2228 RVA: 0x0001C164 File Offset: 0x0001A364
		public void RemoveAt(int index)
		{
			if (index < 0 || index >= this.size)
			{
				throw new ArgumentOutOfRangeException();
			}
			T item = this.items[index];
			this.OnRemove(item, index);
			this.Shift(index, -1);
			this.version++;
		}

		// Token: 0x060008B5 RID: 2229 RVA: 0x0001C1B0 File Offset: 0x0001A3B0
		public bool Remove(T item)
		{
			int index = this.IndexOf(item);
			if (index == -1)
			{
				return false;
			}
			this.OnRemove(item, index);
			this.Shift(index, -1);
			this.version++;
			return true;
		}

		// Token: 0x060008B6 RID: 2230 RVA: 0x0001C1EA File Offset: 0x0001A3EA
		public void Clear()
		{
			this.OnClear();
			Array.Clear(this.items, 0, this.size);
			this.size = 0;
			this.version++;
		}

		// Token: 0x060008B7 RID: 2231 RVA: 0x0001C219 File Offset: 0x0001A419
		public void CopyTo(T[] array, int arrayIndex)
		{
			Array.Copy(this.items, 0, array, arrayIndex, this.size);
		}

		// Token: 0x060008B8 RID: 2232 RVA: 0x0001C230 File Offset: 0x0001A430
		public T[] ToArray()
		{
			T[] array = new T[this.size];
			Array.Copy(this.items, 0, array, 0, this.size);
			return array;
		}

		// Token: 0x060008B9 RID: 2233 RVA: 0x0001C25E File Offset: 0x0001A45E
		private void CheckIndex(int index)
		{
			if (index < 0 || index > this.size)
			{
				throw new ArgumentOutOfRangeException();
			}
		}

		// Token: 0x060008BA RID: 2234 RVA: 0x0001C274 File Offset: 0x0001A474
		private void Shift(int start, int delta)
		{
			if (delta < 0)
			{
				start -= delta;
			}
			if (start < this.size)
			{
				Array.Copy(this.items, start, this.items, start + delta, this.size - start);
			}
			this.size += delta;
			if (delta < 0)
			{
				Array.Clear(this.items, this.size, -delta);
			}
		}

		// Token: 0x060008BB RID: 2235 RVA: 0x0001B842 File Offset: 0x00019A42
		protected virtual void OnAdd(T item, int index)
		{
		}

		// Token: 0x060008BC RID: 2236 RVA: 0x0001B842 File Offset: 0x00019A42
		protected virtual void OnInsert(T item, int index)
		{
		}

		// Token: 0x060008BD RID: 2237 RVA: 0x0001B842 File Offset: 0x00019A42
		protected virtual void OnSet(T item, int index)
		{
		}

		// Token: 0x060008BE RID: 2238 RVA: 0x0001B842 File Offset: 0x00019A42
		protected virtual void OnRemove(T item, int index)
		{
		}

		// Token: 0x060008BF RID: 2239 RVA: 0x0001B842 File Offset: 0x00019A42
		protected virtual void OnClear()
		{
		}

		// Token: 0x060008C0 RID: 2240 RVA: 0x0001C2D8 File Offset: 0x0001A4D8
		internal virtual void Grow(int desired)
		{
			int new_size = this.size + desired;
			if (new_size <= this.items.Length)
			{
				return;
			}
			new_size = Math.Max(Math.Max(this.items.Length * 2, 4), new_size);
			this.Resize(new_size);
		}

		// Token: 0x060008C1 RID: 2241 RVA: 0x0001C318 File Offset: 0x0001A518
		protected void Resize(int new_size)
		{
			if (new_size == this.size)
			{
				return;
			}
			if (new_size < this.size)
			{
				throw new ArgumentOutOfRangeException();
			}
			this.items = this.items.Resize(new_size);
		}

		// Token: 0x060008C2 RID: 2242 RVA: 0x0001C348 File Offset: 0x0001A548
		int IList.Add(object value)
		{
			try
			{
				this.Add((T)((object)value));
				return this.size - 1;
			}
			catch (InvalidCastException)
			{
			}
			catch (NullReferenceException)
			{
			}
			throw new ArgumentException();
		}

		// Token: 0x060008C3 RID: 2243 RVA: 0x0001C398 File Offset: 0x0001A598
		void IList.Clear()
		{
			this.Clear();
		}

		// Token: 0x060008C4 RID: 2244 RVA: 0x0001C3A0 File Offset: 0x0001A5A0
		bool IList.Contains(object value)
		{
			return ((IList)this).IndexOf(value) > -1;
		}

		// Token: 0x060008C5 RID: 2245 RVA: 0x0001C3AC File Offset: 0x0001A5AC
		int IList.IndexOf(object value)
		{
			try
			{
				return this.IndexOf((T)((object)value));
			}
			catch (InvalidCastException)
			{
			}
			catch (NullReferenceException)
			{
			}
			return -1;
		}

		// Token: 0x060008C6 RID: 2246 RVA: 0x0001C3F0 File Offset: 0x0001A5F0
		void IList.Insert(int index, object value)
		{
			this.CheckIndex(index);
			try
			{
				this.Insert(index, (T)((object)value));
				return;
			}
			catch (InvalidCastException)
			{
			}
			catch (NullReferenceException)
			{
			}
			throw new ArgumentException();
		}

		// Token: 0x060008C7 RID: 2247 RVA: 0x0001C43C File Offset: 0x0001A63C
		void IList.Remove(object value)
		{
			try
			{
				this.Remove((T)((object)value));
			}
			catch (InvalidCastException)
			{
			}
			catch (NullReferenceException)
			{
			}
		}

		// Token: 0x060008C8 RID: 2248 RVA: 0x0001C47C File Offset: 0x0001A67C
		void IList.RemoveAt(int index)
		{
			this.RemoveAt(index);
		}

		// Token: 0x060008C9 RID: 2249 RVA: 0x0001C219 File Offset: 0x0001A419
		void ICollection.CopyTo(Array array, int index)
		{
			Array.Copy(this.items, 0, array, index, this.size);
		}

		// Token: 0x060008CA RID: 2250 RVA: 0x0001C485 File Offset: 0x0001A685
		public Collection<T>.Enumerator GetEnumerator()
		{
			return new Collection<T>.Enumerator(this);
		}

		// Token: 0x060008CB RID: 2251 RVA: 0x0001C48D File Offset: 0x0001A68D
		IEnumerator IEnumerable.GetEnumerator()
		{
			return new Collection<T>.Enumerator(this);
		}

		// Token: 0x060008CC RID: 2252 RVA: 0x0001C48D File Offset: 0x0001A68D
		IEnumerator<T> IEnumerable<!0>.GetEnumerator()
		{
			return new Collection<T>.Enumerator(this);
		}

		// Token: 0x040002E5 RID: 741
		internal T[] items;

		// Token: 0x040002E6 RID: 742
		internal int size;

		// Token: 0x040002E7 RID: 743
		private int version;

		// Token: 0x020001DF RID: 479
		public struct Enumerator : IEnumerator<!0>, IDisposable, IEnumerator
		{
			// Token: 0x1700021F RID: 543
			// (get) Token: 0x060008CD RID: 2253 RVA: 0x0001C49A File Offset: 0x0001A69A
			public T Current
			{
				get
				{
					return this.current;
				}
			}

			// Token: 0x17000220 RID: 544
			// (get) Token: 0x060008CE RID: 2254 RVA: 0x0001C4A2 File Offset: 0x0001A6A2
			object IEnumerator.Current
			{
				get
				{
					this.CheckState();
					if (this.next <= 0)
					{
						throw new InvalidOperationException();
					}
					return this.current;
				}
			}

			// Token: 0x060008CF RID: 2255 RVA: 0x0001C4C4 File Offset: 0x0001A6C4
			internal Enumerator(Collection<T> collection)
			{
				this = default(Collection<T>.Enumerator);
				this.collection = collection;
				this.version = collection.version;
			}

			// Token: 0x060008D0 RID: 2256 RVA: 0x0001C4E0 File Offset: 0x0001A6E0
			public bool MoveNext()
			{
				this.CheckState();
				if (this.next < 0)
				{
					return false;
				}
				if (this.next < this.collection.size)
				{
					T[] items = this.collection.items;
					int num = this.next;
					this.next = num + 1;
					this.current = items[num];
					return true;
				}
				this.next = -1;
				return false;
			}

			// Token: 0x060008D1 RID: 2257 RVA: 0x0001C542 File Offset: 0x0001A742
			public void Reset()
			{
				this.CheckState();
				this.next = 0;
			}

			// Token: 0x060008D2 RID: 2258 RVA: 0x0001C551 File Offset: 0x0001A751
			private void CheckState()
			{
				if (this.collection == null)
				{
					throw new ObjectDisposedException(base.GetType().FullName);
				}
				if (this.version != this.collection.version)
				{
					throw new InvalidOperationException();
				}
			}

			// Token: 0x060008D3 RID: 2259 RVA: 0x0001C58F File Offset: 0x0001A78F
			public void Dispose()
			{
				this.collection = null;
			}

			// Token: 0x040002E8 RID: 744
			private Collection<T> collection;

			// Token: 0x040002E9 RID: 745
			private T current;

			// Token: 0x040002EA RID: 746
			private int next;

			// Token: 0x040002EB RID: 747
			private readonly int version;
		}
	}
}
