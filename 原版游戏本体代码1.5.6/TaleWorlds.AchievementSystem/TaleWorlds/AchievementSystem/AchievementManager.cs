using System.Threading.Tasks;

namespace TaleWorlds.AchievementSystem;

public class AchievementManager
{
	public static IAchievementService AchievementService { get; set; }

	static AchievementManager()
	{
		AchievementService = new TestAchievementService();
	}

	public static bool SetStat(string name, int value)
	{
		return AchievementService.SetStat(name, value);
	}

	public static async Task<int> GetStat(string name)
	{
		return await AchievementService.GetStat(name);
	}

	public static async Task<int[]> GetStats(string[] names)
	{
		return await AchievementService.GetStats(names);
	}
}
