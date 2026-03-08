using System;
using System.Threading.Tasks;

namespace TaleWorlds.AchievementSystem
{
	// Token: 0x02000005 RID: 5
	public class TestAchievementService : IAchievementService
	{
		// Token: 0x0600001D RID: 29 RVA: 0x00002199 File Offset: 0x00000399
		bool IAchievementService.SetStat(string name, int value)
		{
			return true;
		}

		// Token: 0x0600001E RID: 30 RVA: 0x0000219C File Offset: 0x0000039C
		Task<int> IAchievementService.GetStat(string name)
		{
			return Task.FromResult<int>(0);
		}

		// Token: 0x0600001F RID: 31 RVA: 0x000021A4 File Offset: 0x000003A4
		Task<int[]> IAchievementService.GetStats(string[] names)
		{
			return Task.FromResult<int[]>(new int[names.Length]);
		}

		// Token: 0x06000020 RID: 32 RVA: 0x000021B3 File Offset: 0x000003B3
		bool IAchievementService.IsInitializationCompleted()
		{
			return true;
		}
	}
}
