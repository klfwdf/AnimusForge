using System.Threading.Tasks;
using TaleWorlds.AchievementSystem;

namespace TaleWorlds.PlatformService.Epic;

internal class EpicAchievementService : IAchievementService
{
	private EpicPlatformServices _epicPlatformServices;

	public EpicAchievementService(EpicPlatformServices epicPlatformServices)
	{
		_epicPlatformServices = epicPlatformServices;
	}

	bool IAchievementService.SetStat(string name, int value)
	{
		return _epicPlatformServices.SetStat(name, value);
	}

	Task<int> IAchievementService.GetStat(string name)
	{
		return _epicPlatformServices.GetStat(name);
	}

	Task<int[]> IAchievementService.GetStats(string[] names)
	{
		return _epicPlatformServices.GetStats(names);
	}

	bool IAchievementService.IsInitializationCompleted()
	{
		return true;
	}
}
