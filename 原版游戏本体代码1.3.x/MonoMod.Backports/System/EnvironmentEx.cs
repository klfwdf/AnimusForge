using System;

namespace System
{
	// Token: 0x0200000F RID: 15
	public static class EnvironmentEx
	{
		// Token: 0x17000002 RID: 2
		// (get) Token: 0x06000013 RID: 19 RVA: 0x00002168 File Offset: 0x00000368
		public static int CurrentManagedThreadId
		{
			get
			{
				return Environment.CurrentManagedThreadId;
			}
		}
	}
}
