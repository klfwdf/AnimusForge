using System;
using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes
{
	// Token: 0x02000051 RID: 81
	public class RebellionNotificationItemVM : MapNotificationItemBaseVM
	{
		// Token: 0x06000647 RID: 1607 RVA: 0x00020384 File Offset: 0x0001E584
		public RebellionNotificationItemVM(SettlementRebellionMapNotification data)
			: base(data)
		{
			this._settlement = data.RebelliousSettlement;
			this._onInspect = (this._onInspectAction = delegate()
			{
				base.GoToMapPosition(this._settlement.Position);
			});
			base.NotificationIdentifier = "rebellion";
		}

		// Token: 0x040002AD RID: 685
		private Settlement _settlement;

		// Token: 0x040002AE RID: 686
		protected Action _onInspectAction;
	}
}
