using System;

namespace System.Security.Cryptography.X509Certificates
{
	// Token: 0x020002D3 RID: 723
	internal struct ReadOnlySpan<T>
	{
		// Token: 0x06002577 RID: 9591 RVA: 0x00088E0B File Offset: 0x0008700B
		public ReadOnlySpan(ArraySegment<T> segment)
		{
			this._Segment = segment;
		}

		// Token: 0x06002578 RID: 9592 RVA: 0x00088E14 File Offset: 0x00087014
		public ReadOnlySpan(T[] array, int offset, int count)
		{
			this = new ReadOnlySpan<T>((array != null || offset != 0 || count != 0) ? new ArraySegment<T>(array, offset, count) : default(ArraySegment<T>));
		}

		// Token: 0x06002579 RID: 9593 RVA: 0x00088E44 File Offset: 0x00087044
		public ReadOnlySpan(T[] array)
		{
			this = new ReadOnlySpan<T>((array != null) ? new ArraySegment<T>(array) : default(ArraySegment<T>));
		}

		// Token: 0x170004A1 RID: 1185
		public T this[int index]
		{
			get
			{
				if (index < 0 || index >= this._Segment.Count)
				{
					throw new ArgumentOutOfRangeException("index");
				}
				return this._Segment.Array[index + this._Segment.Offset];
			}
		}

		// Token: 0x170004A2 RID: 1186
		// (get) Token: 0x0600257B RID: 9595 RVA: 0x00088EA7 File Offset: 0x000870A7
		public bool IsEmpty
		{
			get
			{
				return this._Segment.Count == 0;
			}
		}

		// Token: 0x170004A3 RID: 1187
		// (get) Token: 0x0600257C RID: 9596 RVA: 0x00088EB7 File Offset: 0x000870B7
		public bool IsNull
		{
			get
			{
				return this._Segment.Array == null;
			}
		}

		// Token: 0x170004A4 RID: 1188
		// (get) Token: 0x0600257D RID: 9597 RVA: 0x00088EC7 File Offset: 0x000870C7
		public int Length
		{
			get
			{
				return this._Segment.Count;
			}
		}

		// Token: 0x0600257E RID: 9598 RVA: 0x00088ED4 File Offset: 0x000870D4
		public ReadOnlySpan<T> Slice(int start)
		{
			if (start < 0)
			{
				throw new ArgumentOutOfRangeException();
			}
			return new ReadOnlySpan<T>(this._Segment.Array, this._Segment.Offset + start, this._Segment.Count - start);
		}

		// Token: 0x0600257F RID: 9599 RVA: 0x00088F0A File Offset: 0x0008710A
		public ReadOnlySpan<T> Slice(int start, int length)
		{
			if (start < 0)
			{
				throw new InvalidOperationException();
			}
			if (length > this._Segment.Count - start)
			{
				throw new InvalidOperationException();
			}
			return new ReadOnlySpan<T>(this._Segment.Array, this._Segment.Offset + start, length);
		}

		// Token: 0x06002580 RID: 9600 RVA: 0x00088F4C File Offset: 0x0008714C
		public T[] ToArray()
		{
			T[] array = new T[this._Segment.Count];
			if (!this.IsEmpty)
			{
				Array.Copy(this._Segment.Array, this._Segment.Offset, array, 0, this._Segment.Count);
			}
			return array;
		}

		// Token: 0x06002581 RID: 9601 RVA: 0x00088F9C File Offset: 0x0008719C
		public void CopyTo(Span<T> destination)
		{
			if (destination.Length < this.Length)
			{
				throw new InvalidOperationException("Destination too short");
			}
			if (!this.IsEmpty)
			{
				ArraySegment<T> arraySegment = destination.DangerousGetArraySegment();
				Array.Copy(this._Segment.Array, this._Segment.Offset, arraySegment.Array, arraySegment.Offset, this._Segment.Count);
			}
		}

		// Token: 0x06002582 RID: 9602 RVA: 0x00089008 File Offset: 0x00087208
		public bool Overlaps(ReadOnlySpan<T> destination)
		{
			int num;
			return this.Overlaps(destination, out num);
		}

		// Token: 0x06002583 RID: 9603 RVA: 0x00089020 File Offset: 0x00087220
		public bool Overlaps(ReadOnlySpan<T> destination, out int elementOffset)
		{
			elementOffset = 0;
			if (this.IsEmpty || destination.IsEmpty)
			{
				return false;
			}
			if (this._Segment.Array != destination._Segment.Array)
			{
				return false;
			}
			elementOffset = destination._Segment.Offset - this._Segment.Offset;
			if (elementOffset >= this._Segment.Count || elementOffset <= -destination._Segment.Count)
			{
				elementOffset = 0;
				return false;
			}
			return true;
		}

		// Token: 0x06002584 RID: 9604 RVA: 0x0008909E File Offset: 0x0008729E
		public ArraySegment<T> DangerousGetArraySegment()
		{
			return this._Segment;
		}

		// Token: 0x06002585 RID: 9605 RVA: 0x000890A6 File Offset: 0x000872A6
		public static implicit operator ReadOnlySpan<T>(T[] array)
		{
			return new ReadOnlySpan<T>(array);
		}

		// Token: 0x04000E24 RID: 3620
		public static readonly Span<T> Empty;

		// Token: 0x04000E25 RID: 3621
		private ArraySegment<T> _Segment;
	}
}
