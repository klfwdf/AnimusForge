using System;
using System.Threading.Tasks;

namespace TaleWorlds.AchievementSystem
{
	// Token: 0x02000004 RID: 4
	public interface IAchievementService
	{
		// Token: 0x06000019 RID: 25
		bool SetStat(string name, int value);

		// Token: 0x0600001A RID: 26
		Task<int> GetStat(string name);

		// Token: 0x0600001B RID: 27
		Task<int[]> GetStats(string[] names);

		// Token: 0x0600001C RID: 28
		bool IsInitializationCompleted();
	}
}
