using Galaxy.Api;

namespace TaleWorlds.PlatformService.GOG;

public class StatsAndAchievementsStoreListener : GlobalStatsAndAchievementsStoreListener
{
	public delegate void UserStatsAndAchievementsStored(bool success, FailureReason? failureReason);

	public event UserStatsAndAchievementsStored OnUserStatsAndAchievementsStored;

	public override void OnUserStatsAndAchievementsStoreFailure(FailureReason failureReason)
	{
		OnUserStatsAndAchievementsStored?.Invoke(success: false, failureReason);
	}

	public override void OnUserStatsAndAchievementsStoreSuccess()
	{
		OnUserStatsAndAchievementsStored?.Invoke(success: true, null);
	}
}
