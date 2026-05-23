using System;
using System.Runtime.CompilerServices;

namespace System.Buffers
{
	// Token: 0x02000495 RID: 1173
	internal abstract class MemoryManager<[Nullable(2)] T> : IMemoryOwner<T>, IDisposable, IPinnable
	{
		// Token: 0x170005C3 RID: 1475
		// (get) Token: 0x06001A1C RID: 6684 RVA: 0x00055348 File Offset: 0x00053548
		[Nullable(new byte[] { 0, 1 })]
		public virtual Memory<T> Memory
		{
			[return: Nullable(new byte[] { 0, 1 })]
			get
			{
				return new Memory<T>(this, this.GetSpan().Length);
			}
		}

		// Token: 0x06001A1D RID: 6685
		[return: Nullable(new byte[] { 0, 1 })]
		public abstract Span<T> GetSpan();

		// Token: 0x06001A1E RID: 6686
		public abstract MemoryHandle Pin(int elementIndex = 0);

		// Token: 0x06001A1F RID: 6687
		public abstract void Unpin();

		// Token: 0x06001A20 RID: 6688 RVA: 0x00055369 File Offset: 0x00053569
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: Nullable(new byte[] { 0, 1 })]
		protected Memory<T> CreateMemory(int length)
		{
			return new Memory<T>(this, length);
		}

		// Token: 0x06001A21 RID: 6689 RVA: 0x00055372 File Offset: 0x00053572
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: Nullable(new byte[] { 0, 1 })]
		protected Memory<T> CreateMemory(int start, int length)
		{
			return new Memory<T>(this, start, length);
		}

		// Token: 0x06001A22 RID: 6690 RVA: 0x0005537C File Offset: 0x0005357C
		protected internal virtual bool TryGetArray([Nullable(new byte[] { 0, 1 })] out ArraySegment<T> segment)
		{
			segment = default(ArraySegment<T>);
			return false;
		}

		// Token: 0x06001A23 RID: 6691 RVA: 0x00055386 File Offset: 0x00053586
		void IDisposable.Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		// Token: 0x06001A24 RID: 6692
		protected abstract void Dispose(bool disposing);
	}
}
