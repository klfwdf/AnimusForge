using System;
using System.Runtime.CompilerServices;

namespace System.Buffers
{
	// Token: 0x0200002F RID: 47
	public abstract class MemoryManager<[Nullable(2)] T> : IMemoryOwner<T>, IDisposable, IPinnable
	{
		// Token: 0x1700001F RID: 31
		// (get) Token: 0x060001BF RID: 447 RVA: 0x0000A154 File Offset: 0x00008354
		[Nullable(new byte[] { 0, 1 })]
		public virtual Memory<T> Memory
		{
			[return: Nullable(new byte[] { 0, 1 })]
			get
			{
				return new Memory<T>(this, this.GetSpan().Length);
			}
		}

		// Token: 0x060001C0 RID: 448
		[return: Nullable(new byte[] { 0, 1 })]
		public abstract Span<T> GetSpan();

		// Token: 0x060001C1 RID: 449
		public abstract MemoryHandle Pin(int elementIndex = 0);

		// Token: 0x060001C2 RID: 450
		public abstract void Unpin();

		// Token: 0x060001C3 RID: 451 RVA: 0x0000A175 File Offset: 0x00008375
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: Nullable(new byte[] { 0, 1 })]
		protected Memory<T> CreateMemory(int length)
		{
			return new Memory<T>(this, length);
		}

		// Token: 0x060001C4 RID: 452 RVA: 0x0000A17E File Offset: 0x0000837E
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[return: Nullable(new byte[] { 0, 1 })]
		protected Memory<T> CreateMemory(int start, int length)
		{
			return new Memory<T>(this, start, length);
		}

		// Token: 0x060001C5 RID: 453 RVA: 0x0000A188 File Offset: 0x00008388
		protected internal virtual bool TryGetArray([Nullable(new byte[] { 0, 1 })] out ArraySegment<T> segment)
		{
			segment = default(ArraySegment<T>);
			return false;
		}

		// Token: 0x060001C6 RID: 454 RVA: 0x0000A192 File Offset: 0x00008392
		void IDisposable.Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		// Token: 0x060001C7 RID: 455
		protected abstract void Dispose(bool disposing);
	}
}
