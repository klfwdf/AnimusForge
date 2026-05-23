using System;
using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.CampaignSystem.SceneInformationPopupTypes;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes
{
	// Token: 0x02000044 RID: 68
	public class KingdomDestroyedNotificationItemVM : MapNotificationItemBaseVM
	{
		// Token: 0x060005EC RID: 1516 RVA: 0x0001F17C File Offset: 0x0001D37C
		public KingdomDestroyedNotificationItemVM(KingdomDestroyedMapNotification data)
			: base(data)
		{
			KingdomDestroyedNotificationItemVM <>4__this = this;
			base.NotificationIdentifier = "kingdomdestroyed";
			this._onInspect = delegate()
			{
				<>4__this.OnInspect(data);
			};
		}

		// Token: 0x060005ED RID: 1517 RVA: 0x0001F1C6 File Offset: 0x0001D3C6
		private void OnInspect(KingdomDestroyedMapNotification data)
		{
			MBInformationManager.ShowSceneNotification(new KingdomDestroyedSceneNotificationItem(data.DestroyedKingdom, data.CreationTime));
			base.ExecuteRemove();
		}
	}
}
