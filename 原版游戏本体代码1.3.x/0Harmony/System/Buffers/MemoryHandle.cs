using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Buffers
{
	// Token: 0x02000494 RID: 1172
	internal struct MemoryHandle : IDisposable
	{
		// Token: 0x06001A19 RID: 6681 RVA: 0x000552EC File Offset: 0x000534EC
		[CLSCompliant(false)]
		public unsafe MemoryHandle(void* pointer, GCHandle handle = default(GCHandle), [Nullable(2)] IPinnable pinnable = null)
		{
			this._pointer = pointer;
			this._handle = handle;
			this._pinnable = pinnable;
		}

		// Token: 0x170005C2 RID: 1474
		// (get) Token: 0x06001A1A RID: 6682 RVA: 0x00055303 File Offset: 0x00053503
		[CLSCompliant(false)]
		public unsafe void* Pointer
		{
			get
			{
				return this._pointer;
			}
		}

		// Token: 0x06001A1B RID: 6683 RVA: 0x0005530B File Offset: 0x0005350B
		public void Dispose()
		{
			if (this._handle.IsAllocated)
			{
				this._handle.Free();
			}
			if (this._pinnable != null)
			{
				this._pinnable.Unpin();
				this._pinnable = null;
			}
			this._pointer = null;
		}

		// Token: 0x040010DA RID: 4314
		private unsafe void* _pointer;

		// Token: 0x040010DB RID: 4315
		private GCHandle _handle;

		// Token: 0x040010DC RID: 4316
		[Nullable(2)]
		private IPinnable _pinnable;
	}
}
