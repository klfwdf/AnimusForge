using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.LogEntries;

public interface IChatNotification
{
	bool IsVisibleNotification { get; }

	ChatNotificationType NotificationType { get; }

	TextObject GetNotificationText();
}
