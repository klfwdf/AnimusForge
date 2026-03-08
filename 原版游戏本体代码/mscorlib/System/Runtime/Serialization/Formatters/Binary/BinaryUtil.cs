using System;
using System.Diagnostics;

namespace System.Runtime.Serialization.Formatters.Binary
{
	// Token: 0x0200077F RID: 1919
	internal static class BinaryUtil
	{
		// Token: 0x060053BF RID: 21439 RVA: 0x00126E7D File Offset: 0x0012507D
		[Conditional("_LOGGING")]
		public static void NVTraceI(string name, string value)
		{
			BCLDebug.CheckEnabled("BINARY");
		}

		// Token: 0x060053C0 RID: 21440 RVA: 0x00126E8A File Offset: 0x0012508A
		[Conditional("_LOGGING")]
		public static void NVTraceI(string name, object value)
		{
			BCLDebug.CheckEnabled("BINARY");
		}
	}
}
