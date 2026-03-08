using System;
using System.Runtime.CompilerServices;

namespace System
{
	// Token: 0x020007ED RID: 2029
	internal static class Array2
	{
		// Token: 0x060026F9 RID: 9977 RVA: 0x00087407 File Offset: 0x00085607
		[NullableContext(1)]
		public static T[] Empty<[Nullable(2)] T>()
		{
			return Array2.EmptyClass<T>.Empty;
		}

		// Token: 0x020007EE RID: 2030
		private static class EmptyClass<T>
		{
			// Token: 0x040039B2 RID: 14770
			public static readonly T[] Empty = new T[0];
		}
	}
}
