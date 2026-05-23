using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System
{
	// Token: 0x020007EF RID: 2031
	internal static class string2
	{
		// Token: 0x060026FB RID: 9979 RVA: 0x0008741B File Offset: 0x0008561B
		[NullableContext(2)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsNullOrEmpty([<b37590d4-39fb-478a-88de-d293f3364852>NotNullWhen(false)] string value)
		{
			return string.IsNullOrEmpty(value);
		}
	}
}
