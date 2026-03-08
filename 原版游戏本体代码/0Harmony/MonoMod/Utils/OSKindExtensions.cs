using System;

namespace MonoMod.Utils
{
	// Token: 0x020008D7 RID: 2263
	internal static class OSKindExtensions
	{
		// Token: 0x06002F00 RID: 12032 RVA: 0x000A21A6 File Offset: 0x000A03A6
		public static bool Is(this OSKind operatingSystem, OSKind test)
		{
			return operatingSystem.Has(test);
		}

		// Token: 0x06002F01 RID: 12033 RVA: 0x000A21AF File Offset: 0x000A03AF
		public static OSKind GetKernel(this OSKind operatingSystem)
		{
			return operatingSystem & (OSKind)31;
		}

		// Token: 0x06002F02 RID: 12034 RVA: 0x000A21B5 File Offset: 0x000A03B5
		public static int GetSubtypeId(this OSKind operatingSystem)
		{
			return (int)(operatingSystem >> 5);
		}
	}
}
