using System;
using System.Threading.Tasks;

namespace TaleWorlds.ActivitySystem
{
	// Token: 0x02000005 RID: 5
	public class ActivityManager
	{
		// Token: 0x17000005 RID: 5
		// (get) Token: 0x0600000B RID: 11 RVA: 0x000020A0 File Offset: 0x000002A0
		// (set) Token: 0x0600000C RID: 12 RVA: 0x000020A7 File Offset: 0x000002A7
		public static IActivityService ActivityService { get; set; } = new TestActivityService();

		// Token: 0x0600000D RID: 13 RVA: 0x000020AF File Offset: 0x000002AF
		public static bool StartActivity(string activityId)
		{
			return ActivityManager.ActivityService.StartActivity(activityId);
		}

		// Token: 0x0600000E RID: 14 RVA: 0x000020BC File Offset: 0x000002BC
		public static bool EndActivity(string activityId, ActivityOutcome outcome)
		{
			return ActivityManager.ActivityService.EndActivity(activityId, outcome);
		}

		// Token: 0x0600000F RID: 15 RVA: 0x000020CA File Offset: 0x000002CA
		public static bool SetActivityAvailability(string activityId, bool isAvailable)
		{
			return ActivityManager.ActivityService.SetAvailability(activityId, isAvailable);
		}

		// Token: 0x06000010 RID: 16 RVA: 0x000020D8 File Offset: 0x000002D8
		public static Task<Activity> GetActivity(string activityId)
		{
			return ActivityManager.ActivityService.GetActivity(activityId);
		}

		// Token: 0x06000011 RID: 17 RVA: 0x000020E5 File Offset: 0x000002E5
		public static ActivityTransition GetActivityTransition(string activityId)
		{
			return ActivityManager.ActivityService.GetActivityTransition(activityId);
		}
	}
}
