using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Buffers
{
	// Token: 0x0200002E RID: 46
	public struct MemoryHandle : IDisposable
	{
		// Token: 0x060001BC RID: 444 RVA: 0x0000A0F9 File Offset: 0x000082F9
		[CLSCompliant(false)]
		public unsafe MemoryHandle(void* pointer, GCHandle handle = default(GCHandle), [Nullable(2)] IPinnable pinnable = null)
		{
			this._pointer = pointer;
			this._handle = handle;
			this._pinnable = pinnable;
		}

		// Token: 0x1700001E RID: 30
		// (get) Token: 0x060001BD RID: 445 RVA: 0x0000A110 File Offset: 0x00008310
		[CLSCompliant(false)]
		public unsafe void* Pointer
		{
			get
			{
				return this._pointer;
			}
		}

		// Token: 0x060001BE RID: 446 RVA: 0x0000A118 File Offset: 0x00008318
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

		// Token: 0x04000056 RID: 86
		private unsafe void* _pointer;

		// Token: 0x04000057 RID: 87
		private GCHandle _handle;

		// Token: 0x04000058 RID: 88
		[Nullable(2)]
		private IPinnable _pinnable;
	}
}
