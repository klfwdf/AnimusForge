using System;
using System.Runtime.CompilerServices;

namespace System
{
	// Token: 0x0200000D RID: 13
	public static class ArrayEx
	{
		// Token: 0x0600000B RID: 11 RVA: 0x000020DC File Offset: 0x000002DC
		[NullableContext(1)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T[] Empty<[Nullable(2)] T>()
		{
			return ArrayEx.TypeHolder<T>.Empty;
		}

		// Token: 0x17000001 RID: 1
		// (get) Token: 0x0600000C RID: 12 RVA: 0x000020E3 File Offset: 0x000002E3
		public static int MaxLength
		{
			get
			{
				return 1879048191;
			}
		}

		// Token: 0x02000057 RID: 87
		private static class TypeHolder<[Nullable(2)] T>
		{
			// Token: 0x040000A0 RID: 160
			[Nullable(1)]
			public static readonly T[] Empty = new T[0];
		}
	}
}
