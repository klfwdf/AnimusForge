using System;

namespace System.Threading
{
	// Token: 0x02000539 RID: 1337
	internal static class PlatformHelper
	{
		// Token: 0x17000944 RID: 2372
		// (get) Token: 0x06003EC2 RID: 16066 RVA: 0x000E9A7C File Offset: 0x000E7C7C
		internal static int ProcessorCount
		{
			get
			{
				int tickCount = Environment.TickCount;
				int num = PlatformHelper.s_processorCount;
				if (num == 0 || tickCount - PlatformHelper.s_lastProcessorCountRefreshTicks >= 30000)
				{
					num = (PlatformHelper.s_processorCount = Environment.ProcessorCount);
					PlatformHelper.s_lastProcessorCountRefreshTicks = tickCount;
				}
				return num;
			}
		}

		// Token: 0x17000945 RID: 2373
		// (get) Token: 0x06003EC3 RID: 16067 RVA: 0x000E9AC1 File Offset: 0x000E7CC1
		internal static bool IsSingleProcessor
		{
			get
			{
				return PlatformHelper.ProcessorCount == 1;
			}
		}

		// Token: 0x04001A67 RID: 6759
		private const int PROCESSOR_COUNT_REFRESH_INTERVAL_MS = 30000;

		// Token: 0x04001A68 RID: 6760
		private static volatile int s_processorCount;

		// Token: 0x04001A69 RID: 6761
		private static volatile int s_lastProcessorCountRefreshTicks;
	}
}
