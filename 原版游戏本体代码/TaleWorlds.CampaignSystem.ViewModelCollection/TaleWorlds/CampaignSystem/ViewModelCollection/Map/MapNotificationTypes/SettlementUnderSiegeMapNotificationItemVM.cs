using System;
using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes
{
	// Token: 0x02000053 RID: 83
	public class SettlementUnderSiegeMapNotificationItemVM : MapNotificationItemBaseVM
	{
		// Token: 0x0600064D RID: 1613 RVA: 0x00020488 File Offset: 0x0001E688
		public SettlementUnderSiegeMapNotificationItemVM(SettlementUnderSiegeMapNotification data)
			: base(data)
		{
			this._settlement = data.BesiegedSettlement;
			base.NotificationIdentifier = "settlementundersiege";
			this._onInspect = delegate()
			{
				base.GoToMapPosition(this._settlement.Position);
			};
			CampaignEvents.OnSiegeEventEndedEvent.AddNonSerializedListener(this, new Action<SiegeEvent>(this.OnSiegeEventEnded));
		}

		// Token: 0x0600064E RID: 1614 RVA: 0x000204DC File Offset: 0x0001E6DC
		private void OnSiegeEventEnded(SiegeEvent obj)
		{
			if (obj.BesiegedSettlement == this._settlement)
			{
				base.ExecuteRemove();
			}
		}

		// Token: 0x0600064F RID: 1615 RVA: 0x000204F2 File Offset: 0x0001E6F2
		public override void OnFinalize()
		{
			base.OnFinalize();
			CampaignEvents.OnSiegeEventEndedEvent.ClearListeners(this);
		}

		// Token: 0x040002B1 RID: 689
		private Settlement _settlement;
	}
}
