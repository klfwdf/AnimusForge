using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.CampaignSystem.SceneInformationPopupTypes;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes;

public class HeirComeOfAgeNotificationItemVM : MapNotificationItemBaseVM
{
	public HeirComeOfAgeNotificationItemVM(HeirComeOfAgeMapNotification data)
		: base(data)
	{
		HeirComeOfAgeNotificationItemVM heirComeOfAgeNotificationItemVM = this;
		base.NotificationIdentifier = "comeofage";
		_onInspect = delegate
		{
			heirComeOfAgeNotificationItemVM.OnInspect(data);
		};
	}

	private void OnInspect(HeirComeOfAgeMapNotification data)
	{
		SceneNotificationData data2 = ((!data.ComeOfAgeHero.IsFemale) ? ((SceneNotificationData)new HeirComingOfAgeSceneNotificationItem(data.MentorHero, data.ComeOfAgeHero, data.CreationTime)) : ((SceneNotificationData)new HeirComingOfAgeFemaleSceneNotificationItem(data.MentorHero, data.ComeOfAgeHero, data.CreationTime)));
		MBInformationManager.ShowSceneNotification(data2);
		ExecuteRemove();
	}
}
