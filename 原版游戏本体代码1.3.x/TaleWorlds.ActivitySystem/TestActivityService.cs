using System;
using System.Threading.Tasks;

namespace TaleWorlds.ActivitySystem
{
	// Token: 0x02000007 RID: 7
	public class TestActivityService : IActivityService
	{
		// Token: 0x06000019 RID: 25 RVA: 0x000020FA File Offset: 0x000002FA
		bool IActivityService.StartActivity(string activityId)
		{
			return true;
		}

		// Token: 0x0600001A RID: 26 RVA: 0x000020FD File Offset: 0x000002FD
		bool IActivityService.EndActivity(string activityId, ActivityOutcome outcome)
		{
			return true;
		}

		// Token: 0x0600001B RID: 27 RVA: 0x00002100 File Offset: 0x00000300
		Task<Activity> IActivityService.GetActivity(string activityId)
		{
			return Task.FromResult<Activity>(new Activity());
		}

		// Token: 0x0600001C RID: 28 RVA: 0x0000210C File Offset: 0x0000030C
		bool IActivityService.SetAvailability(string activityId, bool isAvailable)
		{
			return true;
		}

		// Token: 0x0600001D RID: 29 RVA: 0x0000210F File Offset: 0x0000030F
		bool IActivityService.IsInitializationCompleted()
		{
			return true;
		}

		// Token: 0x0600001E RID: 30 RVA: 0x00002112 File Offset: 0x00000312
		public ActivityTransition GetActivityTransition(string activityId)
		{
			return ActivityTransition.Singleplayer;
		}
	}
}
