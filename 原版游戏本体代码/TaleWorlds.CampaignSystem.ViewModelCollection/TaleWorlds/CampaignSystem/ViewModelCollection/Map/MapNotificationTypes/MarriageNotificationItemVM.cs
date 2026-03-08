using System;
using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.CampaignSystem.SceneInformationPopupTypes;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes
{
	// Token: 0x02000047 RID: 71
	public class MarriageNotificationItemVM : MapNotificationItemBaseVM
	{
		// Token: 0x170001B1 RID: 433
		// (get) Token: 0x06000614 RID: 1556 RVA: 0x0001F6B0 File Offset: 0x0001D8B0
		// (set) Token: 0x06000615 RID: 1557 RVA: 0x0001F6B8 File Offset: 0x0001D8B8
		public Hero Suitor { get; private set; }

		// Token: 0x170001B2 RID: 434
		// (get) Token: 0x06000616 RID: 1558 RVA: 0x0001F6C1 File Offset: 0x0001D8C1
		// (set) Token: 0x06000617 RID: 1559 RVA: 0x0001F6C9 File Offset: 0x0001D8C9
		public Hero Maiden { get; private set; }

		// Token: 0x06000618 RID: 1560 RVA: 0x0001F6D4 File Offset: 0x0001D8D4
		public MarriageNotificationItemVM(MarriageMapNotification data)
			: base(data)
		{
			MarriageNotificationItemVM <>4__this = this;
			this.Suitor = data.Suitor;
			this.Maiden = data.Maiden;
			base.NotificationIdentifier = "marriage";
			this._onInspect = delegate()
			{
				MBInformationManager.ShowSceneNotification(new MarriageSceneNotificationItem(data.Suitor, data.Maiden, data.CreationTime, SceneNotificationData.RelevantContextType.Any));
				<>4__this.ExecuteRemove();
			};
		}
	}
}
