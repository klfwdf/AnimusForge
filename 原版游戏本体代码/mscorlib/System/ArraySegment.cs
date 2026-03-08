using System;
using System.Collections;
using System.Collections.Generic;

namespace System
{
	// Token: 0x02000057 RID: 87
	[__DynamicallyInvokable]
	[Serializable]
	public struct ArraySegment<T> : IList<T>, ICollection<T>, IEnumerable<T>, IEnumerable, IReadOnlyList<T>, IReadOnlyCollection<T>
	{
		// Token: 0x0600031B RID: 795 RVA: 0x00007D1E File Offset: 0x00005F1E
		[__DynamicallyInvokable]
		public ArraySegment(T[] array)
		{
			if (array == null)
			{
				throw new ArgumentNullException("array");
			}
			this._array = array;
			this._offset = 0;
			this._count = array.Length;
		}

		// Token: 0x0600031C RID: 796 RVA: 0x00007D48 File Offset: 0x00005F48
		[__DynamicallyInvokable]
		public ArraySegment(T[] array, int offset, int count)
		{
			if (array == null)
			{
				throw new ArgumentNullException("array");
			}
			if (offset < 0)
			{
				throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (array.Length - offset < count)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
			}
			this._array = array;
			this._offset = offset;
			this._count = count;
		}

		// Token: 0x17000038 RID: 56
		// (get) Token: 0x0600031D RID: 797 RVA: 0x00007DC2 File Offset: 0x00005FC2
		[__DynamicallyInvokable]
		public T[] Array
		{
			[__DynamicallyInvokable]
			get
			{
				return this._array;
			}
		}

		// Token: 0x17000039 RID: 57
		// (get) Token: 0x0600031E RID: 798 RVA: 0x00007DCA File Offset: 0x00005FCA
		[__DynamicallyInvokable]
		public int Offset
		{
			[__DynamicallyInvokable]
			get
			{
				return this._offset;
			}
		}

		// Token: 0x1700003A RID: 58
		// (get) Token: 0x0600031F RID: 799 RVA: 0x00007DD2 File Offset: 0x00005FD2
		[__DynamicallyInvokable]
		public int Count
		{
			[__DynamicallyInvokable]
			get
			{
				return this._count;
			}
		}

		// Token: 0x06000320 RID: 800 RVA: 0x00007DDA File Offset: 0x00005FDA
		[__DynamicallyInvokable]
		public override int GetHashCode()
		{
			if (this._array != null)
			{
				return this._array.GetHashCode() ^ this._offset ^ this._count;
			}
			return 0;
		}

		// Token: 0x06000321 RID: 801 RVA: 0x00007DFF File Offset: 0x00005FFF
		[__DynamicallyInvokable]
		public override bool Equals(object obj)
		{
			return obj is ArraySegment<T> && this.Equals((ArraySegment<T>)obj);
		}

		// Token: 0x06000322 RID: 802 RVA: 0x00007E17 File Offset: 0x00006017
		[__DynamicallyInvokable]
		public bool Equals(ArraySegment<T> obj)
		{
			return obj._array == this._array && obj._offset == this._offset && obj._count == this._count;
		}

		// Token: 0x06000323 RID: 803 RVA: 0x00007E45 File Offset: 0x00006045
		[__DynamicallyInvokable]
		public static bool operator ==(ArraySegment<T> a, ArraySegment<T> b)
		{
			return a.Equals(b);
		}

		// Token: 0x06000324 RID: 804 RVA: 0x00007E4F File Offset: 0x0000604F
		[__DynamicallyInvokable]
		public static bool operator !=(ArraySegment<T> a, ArraySegment<T> b)
		{
			return !(a == b);
		}

		// Token: 0x1700003B RID: 59
		[__DynamicallyInvokable]
		T IList<!0>.this[int index]
		{
			[__DynamicallyInvokable]
			get
			{
				if (this._array == null)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NullArray"));
				}
				if (index < 0 || index >= this._count)
				{
					throw new ArgumentOutOfRangeException("index");
				}
				return this._array[this._offset + index];
			}
			[__DynamicallyInvokable]
			set
			{
				if (this._array == null)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NullArray"));
				}
				if (index < 0 || index >= this._count)
				{
					throw new ArgumentOutOfRangeException("index");
				}
				this._array[this._offset + index] = value;
			}
		}

		// Token: 0x06000327 RID: 807 RVA: 0x00007F00 File Offset: 0x00006100
		[__DynamicallyInvokable]
		int IList<!0>.IndexOf(T item)
		{
			if (this._array == null)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NullArray"));
			}
			int num = System.Array.IndexOf<T>(this._array, item, this._offset, this._count);
			if (num < 0)
			{
				return -1;
			}
			return num - this._offset;
		}

		// Token: 0x06000328 RID: 808 RVA: 0x00007F4C File Offset: 0x0000614C
		[__DynamicallyInvokable]
		void IList<!0>.Insert(int index, T item)
		{
			throw new NotSupportedException();
		}

		// Token: 0x06000329 RID: 809 RVA: 0x00007F53 File Offset: 0x00006153
		[__DynamicallyInvokable]
		void IList<!0>.RemoveAt(int index)
		{
			throw new NotSupportedException();
		}

		// Token: 0x1700003C RID: 60
		[__DynamicallyInvokable]
		T IReadOnlyList<!0>.this[int index]
		{
			[__DynamicallyInvokable]
			get
			{
				if (this._array == null)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NullArray"));
				}
				if (index < 0 || index >= this._count)
				{
					throw new ArgumentOutOfRangeException("index");
				}
				return this._array[this._offset + index];
			}
		}

		// Token: 0x1700003D RID: 61
		// (get) Token: 0x0600032B RID: 811 RVA: 0x00007FAC File Offset: 0x000061AC
		[__DynamicallyInvokable]
		bool ICollection<!0>.IsReadOnly
		{
			[__DynamicallyInvokable]
			get
			{
				return true;
			}
		}

		// Token: 0x0600032C RID: 812 RVA: 0x00007FAF File Offset: 0x000061AF
		[__DynamicallyInvokable]
		void ICollection<!0>.Add(T item)
		{
			throw new NotSupportedException();
		}

		// Token: 0x0600032D RID: 813 RVA: 0x00007FB6 File Offset: 0x000061B6
		[__DynamicallyInvokable]
		void ICollection<!0>.Clear()
		{
			throw new NotSupportedException();
		}

		// Token: 0x0600032E RID: 814 RVA: 0x00007FC0 File Offset: 0x000061C0
		[__DynamicallyInvokable]
		bool ICollection<!0>.Contains(T item)
		{
			if (this._array == null)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NullArray"));
			}
			int num = System.Array.IndexOf<T>(this._array, item, this._offset, this._count);
			return num >= 0;
		}

		// Token: 0x0600032F RID: 815 RVA: 0x00008005 File Offset: 0x00006205
		[__DynamicallyInvokable]
		void ICollection<!0>.CopyTo(T[] array, int arrayIndex)
		{
			if (this._array == null)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NullArray"));
			}
			System.Array.Copy(this._array, this._offset, array, arrayIndex, this._count);
		}

		// Token: 0x06000330 RID: 816 RVA: 0x00008038 File Offset: 0x00006238
		[__DynamicallyInvokable]
		bool ICollection<!0>.Remove(T item)
		{
			throw new NotSupportedException();
		}

		// Token: 0x06000331 RID: 817 RVA: 0x0000803F File Offset: 0x0000623F
		[__DynamicallyInvokable]
		IEnumerator<T> IEnumerable<!0>.GetEnumerator()
		{
			if (this._array == null)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NullArray"));
			}
			return new ArraySegment<T>.ArraySegmentEnumerator(this);
		}

		// Token: 0x06000332 RID: 818 RVA: 0x00008064 File Offset: 0x00006264
		[__DynamicallyInvokable]
		IEnumerator IEnumerable.GetEnumerator()
		{
			if (this._array == null)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NullArray"));
			}
			return new ArraySegment<T>.ArraySegmentEnumerator(this);
		}

		// Token: 0x040001F0 RID: 496
		private T[] _array;

		// Token: 0x040001F1 RID: 497
		private int _offset;

		// Token: 0x040001F2 RID: 498
		private int _count;

		// Token: 0x02000AC6 RID: 2758
		[Serializable]
		private sealed class ArraySegmentEnumerator : IEnumerator<T>, IDisposable, IEnumerator
		{
			// Token: 0x060069C5 RID: 27077 RVA: 0x0016CB90 File Offset: 0x0016AD90
			internal ArraySegmentEnumerator(ArraySegment<T> arraySegment)
			{
				this._array = arraySegment._array;
				this._start = arraySegment._offset;
				this._end = this._start + arraySegment._count;
				this._current = this._start - 1;
			}

			// Token: 0x060069C6 RID: 27078 RVA: 0x0016CBDC File Offset: 0x0016ADDC
			public bool MoveNext()
			{
				if (this._current < this._end)
				{
					this._current++;
					return this._current < this._end;
				}
				return false;
			}

			// Token: 0x170011EB RID: 4587
			// (get) Token: 0x060069C7 RID: 27079 RVA: 0x0016CC0C File Offset: 0x0016AE0C
			public T Current
			{
				get
				{
					if (this._current < this._start)
					{
						throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumNotStarted"));
					}
					if (this._current >= this._end)
					{
						throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumEnded"));
					}
					return this._array[this._current];
				}
			}

			// Token: 0x170011EC RID: 4588
			// (get) Token: 0x060069C8 RID: 27080 RVA: 0x0016CC66 File Offset: 0x0016AE66
			object IEnumerator.Current
			{
				get
				{
					return this.Current;
				}
			}

			// Token: 0x060069C9 RID: 27081 RVA: 0x0016CC73 File Offset: 0x0016AE73
			void IEnumerator.Reset()
			{
				this._current = this._start - 1;
			}

			// Token: 0x060069CA RID: 27082 RVA: 0x0016CC83 File Offset: 0x0016AE83
			public void Dispose()
			{
			}

			// Token: 0x040030D9 RID: 12505
			private T[] _array;

			// Token: 0x040030DA RID: 12506
			private int _start;

			// Token: 0x040030DB RID: 12507
			private int _end;

			// Token: 0x040030DC RID: 12508
			private int _current;
		}
	}
}
