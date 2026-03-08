using System;

namespace System.Security.Cryptography.X509Certificates
{
	// Token: 0x020002D4 RID: 724
	internal struct Span<T>
	{
		// Token: 0x06002587 RID: 9607 RVA: 0x000890B0 File Offset: 0x000872B0
		public Span(ArraySegment<T> segment)
		{
			this._Segment = segment;
		}

		// Token: 0x06002588 RID: 9608 RVA: 0x000890BC File Offset: 0x000872BC
		public Span(T[] array, int offset, int count)
		{
			this = new Span<T>((array != null || offset != 0 || count != 0) ? new ArraySegment<T>(array, offset, count) : default(ArraySegment<T>));
		}

		// Token: 0x06002589 RID: 9609 RVA: 0x000890EC File Offset: 0x000872EC
		public Span(T[] array)
		{
			this = new Span<T>((array != null) ? new ArraySegment<T>(array) : default(ArraySegment<T>));
		}

		// Token: 0x170004A5 RID: 1189
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
			set
			{
				if (index < 0 || index >= this._Segment.Count)
				{
					throw new ArgumentOutOfRangeException("index");
				}
				this._Segment.Array[index + this._Segment.Offset] = value;
			}
		}

		// Token: 0x170004A6 RID: 1190
		// (get) Token: 0x0600258C RID: 9612 RVA: 0x0008918C File Offset: 0x0008738C
		public bool IsEmpty
		{
			get
			{
				return this._Segment.Count == 0;
			}
		}

		// Token: 0x170004A7 RID: 1191
		// (get) Token: 0x0600258D RID: 9613 RVA: 0x0008919C File Offset: 0x0008739C
		public int Length
		{
			get
			{
				return this._Segment.Count;
			}
		}

		// Token: 0x0600258E RID: 9614 RVA: 0x000891A9 File Offset: 0x000873A9
		public Span<T> Slice(int start)
		{
			if (start < 0)
			{
				throw new ArgumentOutOfRangeException();
			}
			return new Span<T>(this._Segment.Array, this._Segment.Offset + start, this._Segment.Count - start);
		}

		// Token: 0x0600258F RID: 9615 RVA: 0x000891DF File Offset: 0x000873DF
		public Span<T> Slice(int start, int length)
		{
			if (start < 0 || length > this._Segment.Count - start)
			{
				throw new ArgumentOutOfRangeException();
			}
			return new Span<T>(this._Segment.Array, this._Segment.Offset + start, length);
		}

		// Token: 0x06002590 RID: 9616 RVA: 0x0008921C File Offset: 0x0008741C
		public void Fill(T value)
		{
			for (int i = this._Segment.Offset; i < this._Segment.Count - this._Segment.Offset; i++)
			{
				this._Segment.Array[i] = value;
			}
		}

		// Token: 0x06002591 RID: 9617 RVA: 0x00089268 File Offset: 0x00087468
		public void Clear()
		{
			for (int i = this._Segment.Offset; i < this._Segment.Count - this._Segment.Offset; i++)
			{
				this._Segment.Array[i] = default(T);
			}
		}

		// Token: 0x06002592 RID: 9618 RVA: 0x000892BC File Offset: 0x000874BC
		public T[] ToArray()
		{
			T[] array = new T[this._Segment.Count];
			if (!this.IsEmpty)
			{
				Array.Copy(this._Segment.Array, this._Segment.Offset, array, 0, this._Segment.Count);
			}
			return array;
		}

		// Token: 0x06002593 RID: 9619 RVA: 0x0008930C File Offset: 0x0008750C
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

		// Token: 0x06002594 RID: 9620 RVA: 0x00089378 File Offset: 0x00087578
		public bool Overlaps(ReadOnlySpan<T> destination, out int elementOffset)
		{
			return this.Overlaps(destination, out elementOffset);
		}

		// Token: 0x06002595 RID: 9621 RVA: 0x0008939A File Offset: 0x0008759A
		public ArraySegment<T> DangerousGetArraySegment()
		{
			return this._Segment;
		}

		// Token: 0x06002596 RID: 9622 RVA: 0x000893A2 File Offset: 0x000875A2
		public static implicit operator Span<T>(T[] array)
		{
			return new Span<T>(array);
		}

		// Token: 0x06002597 RID: 9623 RVA: 0x000893AA File Offset: 0x000875AA
		public static implicit operator ReadOnlySpan<T>(Span<T> span)
		{
			return new ReadOnlySpan<T>(span._Segment);
		}

		// Token: 0x06002598 RID: 9624 RVA: 0x000893B7 File Offset: 0x000875B7
		public T[] DangerousGetArrayForPinning()
		{
			return this._Segment.Array;
		}

		// Token: 0x04000E26 RID: 3622
		public static readonly Span<T> Empty;

		// Token: 0x04000E27 RID: 3623
		private ArraySegment<T> _Segment;
	}
}
