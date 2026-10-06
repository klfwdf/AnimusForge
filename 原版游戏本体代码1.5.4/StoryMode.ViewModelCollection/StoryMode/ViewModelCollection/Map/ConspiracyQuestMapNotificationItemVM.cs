using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes;

namespace StoryMode.ViewModelCollection.Map;

public class ConspiracyQuestMapNotificationItemVM : MapNotificationItemBaseVM
{
	public QuestBase Quest { get; }

	public ConspiracyQuestMapNotificationItemVM(ConspiracyQuestMapNotification data)
		: base(data)
	{
		ConspiracyQuestMapNotificationItemVM conspiracyQuestMapNotificationItemVM = this;
		base.NotificationIdentifier = "conspiracyquest";
		Quest = data.ConspiracyQuest;
		_onInspect = delegate
		{
			conspiracyQuestMapNotificationItemVM.NavigationHandler?.OpenQuests(data.ConspiracyQuest);
		};
	}
}
