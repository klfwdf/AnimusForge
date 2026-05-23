using System;
using System.Runtime.CompilerServices;

namespace System.Text
{
	// Token: 0x02000484 RID: 1156
	internal static class StringBuilderExtensions
	{
		// Token: 0x060019B1 RID: 6577 RVA: 0x000546A6 File Offset: 0x000528A6
		[NullableContext(1)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static StringBuilder Clear(this StringBuilder builder)
		{
			ThrowHelper.ThrowIfArgumentNull(builder, "builder", null);
			return builder.Clear();
		}
	}
}
