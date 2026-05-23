using System;
using System.Diagnostics.CodeAnalysis;

namespace System.Diagnostics
{
	// Token: 0x020007F1 RID: 2033
	internal static class Debug2
	{
		// Token: 0x060026FC RID: 9980 RVA: 0x0001B842 File Offset: 0x00019A42
		[Conditional("DEBUG")]
		public static void Assert([<b37590d4-39fb-478a-88de-d293f3364852>DoesNotReturnIf(false)] bool condition)
		{
		}
	}
}
