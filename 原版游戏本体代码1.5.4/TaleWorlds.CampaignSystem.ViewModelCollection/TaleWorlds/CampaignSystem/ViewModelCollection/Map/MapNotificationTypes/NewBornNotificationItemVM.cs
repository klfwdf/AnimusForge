using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.CampaignSystem.SceneInformationPopupTypes;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes;

public class NewBornNotificationItemVM : MapNotificationItemBaseVM
{
	private readonly SceneNotificationData _notification;

	public NewBornNotificationItemVM(ChildBornMapNotification data)
		: base(data)
	{
		base.NotificationIdentifier = "newborn";
		if (data.NewbornHero != null)
		{
			Hero mother = data.NewbornHero.Mother;
			if (mother.Spouse == Hero.MainHero)
			{
				Hero spouse = mother.Spouse;
				if (spouse != null && spouse.IsAlive)
				{
					_notification = new NewBornFemaleHeroSceneNotificationItem(mother.Spouse, mother, data.CreationTime);
				}
				else
				{
					_notification = new NewBornFemaleHeroSceneAlternateNotificationItem(mother.Spouse, mother, data.CreationTime);
				}
			}
			else
			{
				Hero spouse2 = mother.Spouse;
				if (spouse2 != null && spouse2.IsAlive)
				{
					_notification = new NewBornSceneNotificationItem(mother.Spouse, mother, data.CreationTime);
				}
				else
				{
					_notification = new NewBornFemaleHeroSceneAlternateNotificationItem(mother.Spouse, mother, data.CreationTime);
				}
			}
		}
		if (_notification != null)
		{
			_onInspect = delegate
			{
				MBInformationManager.ShowSceneNotification(_notification);
			};
		}
		else
		{
			_onInspect = delegate
			{
			};
		}
	}
}
