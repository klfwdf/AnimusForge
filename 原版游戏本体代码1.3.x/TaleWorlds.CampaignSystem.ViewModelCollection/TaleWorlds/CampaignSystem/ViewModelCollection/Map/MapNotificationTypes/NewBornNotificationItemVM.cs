using System;
using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.CampaignSystem.SceneInformationPopupTypes;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes
{
	// Token: 0x0200004A RID: 74
	public class NewBornNotificationItemVM : MapNotificationItemBaseVM
	{
		// Token: 0x06000621 RID: 1569 RVA: 0x0001F930 File Offset: 0x0001DB30
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
						this._notification = new NewBornFemaleHeroSceneNotificationItem(mother.Spouse, mother, data.CreationTime);
					}
					else
					{
						this._notification = new NewBornFemaleHeroSceneAlternateNotificationItem(mother.Spouse, mother, data.CreationTime);
					}
				}
				else
				{
					Hero spouse2 = mother.Spouse;
					if (spouse2 != null && spouse2.IsAlive)
					{
						this._notification = new NewBornSceneNotificationItem(mother.Spouse, mother, data.CreationTime);
					}
					else
					{
						this._notification = new NewBornFemaleHeroSceneAlternateNotificationItem(mother.Spouse, mother, data.CreationTime);
					}
				}
			}
			if (this._notification != null)
			{
				this._onInspect = delegate()
				{
					MBInformationManager.ShowSceneNotification(this._notification);
				};
				return;
			}
			this._onInspect = delegate()
			{
			};
		}

		// Token: 0x04000299 RID: 665
		private readonly SceneNotificationData _notification;
	}
}
