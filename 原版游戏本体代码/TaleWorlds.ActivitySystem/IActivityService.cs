using System;
using System.Threading.Tasks;

namespace TaleWorlds.ActivitySystem
{
	// Token: 0x02000006 RID: 6
	public interface IActivityService
	{
		// Token: 0x06000013 RID: 19
		bool StartActivity(string activityId);

		// Token: 0x06000014 RID: 20
		bool EndActivity(string activityId, ActivityOutcome outcome);

		// Token: 0x06000015 RID: 21
		Task<Activity> GetActivity(string activityId);

		// Token: 0x06000016 RID: 22
		bool SetAvailability(string activityId, bool isAvailable);

		// Token: 0x06000017 RID: 23
		bool IsInitializationCompleted();

		// Token: 0x06000018 RID: 24
		ActivityTransition GetActivityTransition(string activityId);
	}
}
