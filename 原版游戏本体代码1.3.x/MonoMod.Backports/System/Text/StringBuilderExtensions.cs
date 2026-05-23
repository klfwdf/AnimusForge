using System;
using System.Runtime.CompilerServices;

namespace System.Text
{
	// Token: 0x02000022 RID: 34
	public static class StringBuilderExtensions
	{
		// Token: 0x06000160 RID: 352 RVA: 0x000095FE File Offset: 0x000077FE
		[NullableContext(1)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static StringBuilder Clear(this StringBuilder builder)
		{
			ThrowHelper.ThrowIfArgumentNull(builder, "builder", null);
			return builder.Clear();
		}
	}
}
