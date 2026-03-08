using System;

namespace System.Threading
{
	// Token: 0x0200053A RID: 1338
	internal static class TimeoutHelper
	{
		// Token: 0x06003EC4 RID: 16068 RVA: 0x000E9ACB File Offset: 0x000E7CCB
		public static uint GetTime()
		{
			return (uint)Environment.TickCount;
		}

		// Token: 0x06003EC5 RID: 16069 RVA: 0x000E9AD4 File Offset: 0x000E7CD4
		public static int UpdateTimeOut(uint startTime, int originalWaitMillisecondsTimeout)
		{
			uint num = TimeoutHelper.GetTime() - startTime;
			if (num > 2147483647U)
			{
				return 0;
			}
			int num2 = originalWaitMillisecondsTimeout - (int)num;
			if (num2 <= 0)
			{
				return 0;
			}
			return num2;
		}
	}
}
