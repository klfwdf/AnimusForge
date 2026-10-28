using Galaxy.Api;

namespace TaleWorlds.PlatformService.GOG;

public class UserStatsAndAchievementsRetrieveListener : GlobalUserStatsAndAchievementsRetrieveListener
{
	public delegate void UserStatsAndAchievementsRetrieved(GalaxyID userID, bool success, FailureReason? failureReason);

	public event UserStatsAndAchievementsRetrieved OnUserStatsAndAchievementsRetrieved;

	public override void OnUserStatsAndAchievementsRetrieveSuccess(GalaxyID userID)
	{
		OnUserStatsAndAchievementsRetrieved?.Invoke(userID, success: true, null);
	}

	public override void OnUserStatsAndAchievementsRetrieveFailure(GalaxyID userID, FailureReason failureReason)
	{
		OnUserStatsAndAchievementsRetrieved?.Invoke(userID, success: false, failureReason);
	}
}
