using System.Threading.Tasks;

namespace TaleWorlds.ActivitySystem;

public interface IActivityService
{
	bool StartActivity(string activityId);

	bool EndActivity(string activityId, ActivityOutcome outcome);

	Task<Activity> GetActivity(string activityId);

	bool SetAvailability(string activityId, bool isAvailable);

	bool IsInitializationCompleted();

	ActivityTransition GetActivityTransition(string activityId);
}
