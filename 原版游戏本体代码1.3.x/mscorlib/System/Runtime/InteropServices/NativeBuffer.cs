using System;
using System.Runtime.CompilerServices;
using System.Security;

namespace System.Runtime.InteropServices
{
	// Token: 0x0200095B RID: 2395
	internal class NativeBuffer : IDisposable
	{
		// Token: 0x06006207 RID: 25095 RVA: 0x0014F5BB File Offset: 0x0014D7BB
		[SecuritySafeCritical]
		static NativeBuffer()
		{
			NativeBuffer.s_handleCache = new SafeHeapHandleCache(64UL, 2048UL, 0);
		}

		// Token: 0x06006208 RID: 25096 RVA: 0x0014F5DB File Offset: 0x0014D7DB
		public NativeBuffer(ulong initialMinCapacity = 0UL)
		{
			this.EnsureByteCapacity(initialMinCapacity);
		}

		// Token: 0x1700110C RID: 4364
		// (get) Token: 0x06006209 RID: 25097 RVA: 0x0014F5EC File Offset: 0x0014D7EC
		protected unsafe void* VoidPointer
		{
			[SecurityCritical]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				if (this._handle != null)
				{
					return this._handle.DangerousGetHandle().ToPointer();
				}
				return null;
			}
		}

		// Token: 0x1700110D RID: 4365
		// (get) Token: 0x0600620A RID: 25098 RVA: 0x0014F617 File Offset: 0x0014D817
		protected unsafe byte* BytePointer
		{
			[SecurityCritical]
			get
			{
				return (byte*)this.VoidPointer;
			}
		}

		// Token: 0x0600620B RID: 25099 RVA: 0x0014F61F File Offset: 0x0014D81F
		[SecuritySafeCritical]
		public SafeHandle GetHandle()
		{
			return this._handle ?? NativeBuffer.s_emptyHandle;
		}

		// Token: 0x1700110E RID: 4366
		// (get) Token: 0x0600620C RID: 25100 RVA: 0x0014F630 File Offset: 0x0014D830
		public ulong ByteCapacity
		{
			get
			{
				return this._capacity;
			}
		}

		// Token: 0x0600620D RID: 25101 RVA: 0x0014F638 File Offset: 0x0014D838
		[SecuritySafeCritical]
		public void EnsureByteCapacity(ulong minCapacity)
		{
			if (this._capacity < minCapacity)
			{
				this.Resize(minCapacity);
				this._capacity = minCapacity;
			}
		}

		// Token: 0x1700110F RID: 4367
		public unsafe byte this[ulong index]
		{
			[SecuritySafeCritical]
			get
			{
				if (index >= this._capacity)
				{
					throw new ArgumentOutOfRangeException();
				}
				return this.BytePointer[index];
			}
			[SecuritySafeCritical]
			set
			{
				if (index >= this._capacity)
				{
					throw new ArgumentOutOfRangeException();
				}
				this.BytePointer[index] = value;
			}
		}

		// Token: 0x06006210 RID: 25104 RVA: 0x0014F688 File Offset: 0x0014D888
		[SecuritySafeCritical]
		private void Resize(ulong byteLength)
		{
			if (byteLength == 0UL)
			{
				this.ReleaseHandle();
				return;
			}
			if (this._handle == null)
			{
				this._handle = NativeBuffer.s_handleCache.Acquire(byteLength);
				return;
			}
			this._handle.Resize(byteLength);
		}

		// Token: 0x06006211 RID: 25105 RVA: 0x0014F6BA File Offset: 0x0014D8BA
		[SecuritySafeCritical]
		private void ReleaseHandle()
		{
			if (this._handle != null)
			{
				NativeBuffer.s_handleCache.Release(this._handle);
				this._capacity = 0UL;
				this._handle = null;
			}
		}

		// Token: 0x06006212 RID: 25106 RVA: 0x0014F6E3 File Offset: 0x0014D8E3
		[SecuritySafeCritical]
		public virtual void Free()
		{
			this.ReleaseHandle();
		}

		// Token: 0x06006213 RID: 25107 RVA: 0x0014F6EB File Offset: 0x0014D8EB
		[SecuritySafeCritical]
		public void Dispose()
		{
			this.Free();
		}

		// Token: 0x04002B88 RID: 11144
		private static readonly SafeHeapHandleCache s_handleCache;

		// Token: 0x04002B89 RID: 11145
		[SecurityCritical]
		private static readonly SafeHandle s_emptyHandle = new NativeBuffer.EmptySafeHandle();

		// Token: 0x04002B8A RID: 11146
		[SecurityCritical]
		private SafeHeapHandle _handle;

		// Token: 0x04002B8B RID: 11147
		private ulong _capacity;

		// Token: 0x02000C98 RID: 3224
		[SecurityCritical]
		private sealed class EmptySafeHandle : SafeHandle
		{
			// Token: 0x06007117 RID: 28951 RVA: 0x0018544E File Offset: 0x0018364E
			public EmptySafeHandle()
				: base(IntPtr.Zero, true)
			{
			}

			// Token: 0x17001365 RID: 4965
			// (get) Token: 0x06007118 RID: 28952 RVA: 0x0018545C File Offset: 0x0018365C
			public override bool IsInvalid
			{
				[SecurityCritical]
				get
				{
					return true;
				}
			}

			// Token: 0x06007119 RID: 28953 RVA: 0x0018545F File Offset: 0x0018365F
			[SecurityCritical]
			protected override bool ReleaseHandle()
			{
				return true;
			}
		}
	}
}
