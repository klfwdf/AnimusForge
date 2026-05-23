using System;
using System.Runtime.CompilerServices;

namespace System.Buffers
{
	// Token: 0x02000028 RID: 40
	[NullableContext(1)]
	[Nullable(0)]
	public abstract class ArrayPool<[Nullable(2)] T>
	{
		// Token: 0x1700001B RID: 27
		// (get) Token: 0x060001A3 RID: 419 RVA: 0x00009D3C File Offset: 0x00007F3C
		public static ArrayPool<T> Shared
		{
			get
			{
				return ArrayPool<T>.s_shared;
			}
		}

		// Token: 0x060001A4 RID: 420 RVA: 0x00009D43 File Offset: 0x00007F43
		public static ArrayPool<T> Create()
		{
			return new ConfigurableArrayPool<T>();
		}

		// Token: 0x060001A5 RID: 421 RVA: 0x00009D4A File Offset: 0x00007F4A
		public static ArrayPool<T> Create(int maxArrayLength, int maxArraysPerBucket)
		{
			return new ConfigurableArrayPool<T>(maxArrayLength, maxArraysPerBucket);
		}

		// Token: 0x060001A6 RID: 422
		public abstract T[] Rent(int minimumLength);

		// Token: 0x060001A7 RID: 423
		public abstract void Return(T[] array, bool clearArray = false);

		// Token: 0x04000052 RID: 82
		private static readonly TlsOverPerCoreLockedStacksArrayPool<T> s_shared = new TlsOverPerCoreLockedStacksArrayPool<T>();
	}
}
