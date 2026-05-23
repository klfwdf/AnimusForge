using System.Threading.Tasks;

namespace TaleWorlds.AchievementSystem;

public class TestAchievementService : IAchievementService
{
	bool IAchievementService.SetStat(string name, int value)
	{
		return true;
	}

	Task<int> IAchievementService.GetStat(string name)
	{
		return Task.FromResult(0);
	}

	Task<int[]> IAchievementService.GetStats(string[] names)
	{
		return Task.FromResult(new int[names.Length]);
	}

	bool IAchievementService.IsInitializationCompleted()
	{
		return true;
	}
}
