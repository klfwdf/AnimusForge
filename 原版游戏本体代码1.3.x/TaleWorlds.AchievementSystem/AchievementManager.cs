using System;
using System.Threading.Tasks;

namespace TaleWorlds.AchievementSystem
{
	// Token: 0x02000003 RID: 3
	public class AchievementManager
	{
		// Token: 0x17000009 RID: 9
		// (get) Token: 0x06000013 RID: 19 RVA: 0x000020E4 File Offset: 0x000002E4
		// (set) Token: 0x06000014 RID: 20 RVA: 0x000020EB File Offset: 0x000002EB
		public static IAchievementService AchievementService { get; set; } = new TestAchievementService();

		// Token: 0x06000015 RID: 21 RVA: 0x000020F3 File Offset: 0x000002F3
		public static bool SetStat(string name, int value)
		{
			return AchievementManager.AchievementService.SetStat(name, value);
		}

		// Token: 0x06000016 RID: 22 RVA: 0x00002104 File Offset: 0x00000304
		public static async Task<int> GetStat(string name)
		{
			return await AchievementManager.AchievementService.GetStat(name);
		}

		// Token: 0x06000017 RID: 23 RVA: 0x0000214C File Offset: 0x0000034C
		public static async Task<int[]> GetStats(string[] names)
		{
			return await AchievementManager.AchievementService.GetStats(names);
		}
	}
}
