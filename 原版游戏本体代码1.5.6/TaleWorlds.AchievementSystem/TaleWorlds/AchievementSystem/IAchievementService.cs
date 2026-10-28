using System.Threading.Tasks;

namespace TaleWorlds.AchievementSystem;

public interface IAchievementService
{
	bool SetStat(string name, int value);

	Task<int> GetStat(string name);

	Task<int[]> GetStats(string[] names);

	bool IsInitializationCompleted();
}
