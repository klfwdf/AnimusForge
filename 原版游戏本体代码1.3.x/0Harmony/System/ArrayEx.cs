using System;
using System.Runtime.CompilerServices;

namespace System
{
	// Token: 0x02000467 RID: 1127
	internal static class ArrayEx
	{
		// Token: 0x06001851 RID: 6225 RVA: 0x0004D065 File Offset: 0x0004B265
		[NullableContext(1)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T[] Empty<[Nullable(2)] T>()
		{
			return ArrayEx.TypeHolder<T>.Empty;
		}

		// Token: 0x170005A2 RID: 1442
		// (get) Token: 0x06001852 RID: 6226 RVA: 0x0004D06C File Offset: 0x0004B26C
		public static int MaxLength
		{
			get
			{
				return 1879048191;
			}
		}

		// Token: 0x02000468 RID: 1128
		private static class TypeHolder<[Nullable(2)] T>
		{
			// Token: 0x04001081 RID: 4225
			[Nullable(1)]
			public static readonly T[] Empty = new T[0];
		}
	}
}
