using System.Threading.Tasks;

namespace TaleWorlds.ActivitySystem;

public class TestActivityService : IActivityService
{
	bool IActivityService.StartActivity(string activityId)
	{
		return true;
	}

	bool IActivityService.EndActivity(string activityId, ActivityOutcome outcome)
	{
		return true;
	}

	Task<Activity> IActivityService.GetActivity(string activityId)
	{
		return Task.FromResult(new Activity());
	}

	bool IActivityService.SetAvailability(string activityId, bool isAvailable)
	{
		return true;
	}

	bool IActivityService.IsInitializationCompleted()
	{
		return true;
	}

	public ActivityTransition GetActivityTransition(string activityId)
	{
		return ActivityTransition.Singleplayer;
	}
}
