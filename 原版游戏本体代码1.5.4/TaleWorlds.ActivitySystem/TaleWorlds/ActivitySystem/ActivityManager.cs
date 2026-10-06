using System.Threading.Tasks;

namespace TaleWorlds.ActivitySystem;

public class ActivityManager
{
	public static IActivityService ActivityService { get; set; }

	static ActivityManager()
	{
		ActivityService = new TestActivityService();
	}

	public static bool StartActivity(string activityId)
	{
		return ActivityService.StartActivity(activityId);
	}

	public static bool EndActivity(string activityId, ActivityOutcome outcome)
	{
		return ActivityService.EndActivity(activityId, outcome);
	}

	public static bool SetActivityAvailability(string activityId, bool isAvailable)
	{
		return ActivityService.SetAvailability(activityId, isAvailable);
	}

	public static Task<Activity> GetActivity(string activityId)
	{
		return ActivityService.GetActivity(activityId);
	}

	public static ActivityTransition GetActivityTransition(string activityId)
	{
		return ActivityService.GetActivityTransition(activityId);
	}
}
