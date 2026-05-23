using System;
using System.Runtime.CompilerServices;

namespace System.Buffers
{
	// Token: 0x0200048D RID: 1165
	[NullableContext(1)]
	[Nullable(0)]
	internal abstract class ArrayPool<[Nullable(2)] T>
	{
		// Token: 0x170005BE RID: 1470
		// (get) Token: 0x060019FC RID: 6652 RVA: 0x00054DF5 File Offset: 0x00052FF5
		public static ArrayPool<T> Shared
		{
			get
			{
				return ArrayPool<T>.s_shared;
			}
		}

		// Token: 0x060019FD RID: 6653 RVA: 0x00054DFC File Offset: 0x00052FFC
		public static ArrayPool<T> Create()
		{
			return new ConfigurableArrayPool<T>();
		}

		// Token: 0x060019FE RID: 6654 RVA: 0x00054E03 File Offset: 0x00053003
		public static ArrayPool<T> Create(int maxArrayLength, int maxArraysPerBucket)
		{
			return new ConfigurableArrayPool<T>(maxArrayLength, maxArraysPerBucket);
		}

		// Token: 0x060019FF RID: 6655
		public abstract T[] Rent(int minimumLength);

		// Token: 0x06001A00 RID: 6656
		public abstract void Return(T[] array, bool clearArray = false);

		// Token: 0x040010D1 RID: 4305
		private static readonly TlsOverPerCoreLockedStacksArrayPool<T> s_shared = new TlsOverPerCoreLockedStacksArrayPool<T>();
	}
}
