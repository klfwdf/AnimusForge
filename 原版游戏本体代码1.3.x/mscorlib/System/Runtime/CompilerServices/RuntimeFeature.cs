using System;

namespace System.Runtime.CompilerServices
{
	// Token: 0x020008AC RID: 2220
	public static class RuntimeFeature
	{
		// Token: 0x06005D97 RID: 23959 RVA: 0x001494BC File Offset: 0x001476BC
		public static bool IsSupported(string feature)
		{
			return feature == "PortablePdb" && !AppContextSwitches.IgnorePortablePDBsInStackTraces;
		}

		// Token: 0x04002A11 RID: 10769
		public const string PortablePdb = "PortablePdb";
	}
}
