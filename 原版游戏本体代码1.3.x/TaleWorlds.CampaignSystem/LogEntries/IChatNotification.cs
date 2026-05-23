using System;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.LogEntries
{
	// Token: 0x0200034E RID: 846
	public interface IChatNotification
	{
		// Token: 0x17000BF2 RID: 3058
		// (get) Token: 0x060031A6 RID: 12710
		bool IsVisibleNotification { get; }

		// Token: 0x17000BF3 RID: 3059
		// (get) Token: 0x060031A7 RID: 12711
		ChatNotificationType NotificationType { get; }

		// Token: 0x060031A8 RID: 12712
		TextObject GetNotificationText();
	}
}
