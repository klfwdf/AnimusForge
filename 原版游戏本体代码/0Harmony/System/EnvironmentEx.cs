using System;

namespace System
{
	// Token: 0x0200046A RID: 1130
	internal static class EnvironmentEx
	{
		// Token: 0x170005A3 RID: 1443
		// (get) Token: 0x0600185A RID: 6234 RVA: 0x0004D0FC File Offset: 0x0004B2FC
		public static int CurrentManagedThreadId
		{
			get
			{
				return Environment.CurrentManagedThreadId;
			}
		}
	}
}
