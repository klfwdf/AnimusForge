using System.Threading.Tasks;
using TaleWorlds.AchievementSystem;

namespace TaleWorlds.PlatformService.GOG;

public class GOGAchievementService : IAchievementService
{
	private GOGPlatformServices _gogPlatformServices;

	public GOGAchievementService(GOGPlatformServices epicPlatformServices)
	{
		_gogPlatformServices = epicPlatformServices;
	}

	bool IAchievementService.SetStat(string name, int value)
	{
		return _gogPlatformServices.SetStat(name, value);
	}

	async Task<int> IAchievementService.GetStat(string name)
	{
		return await _gogPlatformServices.GetStat(name);
	}

	async Task<int[]> IAchievementService.GetStats(string[] names)
	{
		return await _gogPlatformServices.GetStats(names);
	}

	bool IAchievementService.IsInitializationCompleted()
	{
		return true;
	}
}
