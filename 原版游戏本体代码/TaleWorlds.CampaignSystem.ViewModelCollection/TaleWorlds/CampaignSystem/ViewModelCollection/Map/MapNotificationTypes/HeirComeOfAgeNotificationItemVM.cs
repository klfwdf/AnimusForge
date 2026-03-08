using System;
using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.CampaignSystem.SceneInformationPopupTypes;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes
{
	// Token: 0x02000043 RID: 67
	public class HeirComeOfAgeNotificationItemVM : MapNotificationItemBaseVM
	{
		// Token: 0x060005EA RID: 1514 RVA: 0x0001F0D8 File Offset: 0x0001D2D8
		public HeirComeOfAgeNotificationItemVM(HeirComeOfAgeMapNotification data)
			: base(data)
		{
			HeirComeOfAgeNotificationItemVM <>4__this = this;
			base.NotificationIdentifier = "comeofage";
			this._onInspect = delegate()
			{
				<>4__this.OnInspect(data);
			};
		}

		// Token: 0x060005EB RID: 1515 RVA: 0x0001F124 File Offset: 0x0001D324
		private void OnInspect(HeirComeOfAgeMapNotification data)
		{
			SceneNotificationData data2;
			if (data.ComeOfAgeHero.IsFemale)
			{
				data2 = new HeirComingOfAgeFemaleSceneNotificationItem(data.MentorHero, data.ComeOfAgeHero, data.CreationTime);
			}
			else
			{
				data2 = new HeirComingOfAgeSceneNotificationItem(data.MentorHero, data.ComeOfAgeHero, data.CreationTime);
			}
			MBInformationManager.ShowSceneNotification(data2);
			base.ExecuteRemove();
		}
	}
}
