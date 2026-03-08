using System;

namespace TaleWorlds.Engine
{
	// Token: 0x02000095 RID: 149
	public static class Time
	{
		// Token: 0x1700009D RID: 157
		// (get) Token: 0x06000D14 RID: 3348 RVA: 0x0000E9F8 File Offset: 0x0000CBF8
		public static float ApplicationTime
		{
			get
			{
				return EngineApplicationInterface.ITime.GetApplicationTime();
			}
		}
	}
}
