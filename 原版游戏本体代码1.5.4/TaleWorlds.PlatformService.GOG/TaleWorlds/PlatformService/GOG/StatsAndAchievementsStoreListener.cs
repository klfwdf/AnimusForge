using Galaxy.Api;

namespace TaleWorlds.PlatformService.GOG;

public class StatsAndAchievementsStoreListener : GlobalStatsAndAchievementsStoreListener
{
	public delegate void UserStatsAndAchievementsStored(bool success, FailureReason? failureReason);

	public event UserStatsAndAchievementsStored OnUserStatsAndAchievementsStored;

	public override void OnUserStatsAndAchievementsStoreFailure(FailureReason failureReason)
	{
		this.OnUserStatsAndAchievementsStored?.Invoke(success: false, failureReason);
	}

	public override void OnUserStatsAndAchievementsStoreSuccess()
	{
		this.OnUserStatsAndAchievementsStored?.Invoke(success: true, null);
	}
}
