using System;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes
{
	// Token: 0x02000052 RID: 82
	public class SettlementOwnerChangedNotificationItemVM : MapNotificationItemBaseVM
	{
		// Token: 0x06000649 RID: 1609 RVA: 0x000203E0 File Offset: 0x0001E5E0
		public SettlementOwnerChangedNotificationItemVM(SettlementOwnerChangedMapNotification data)
			: base(data)
		{
			this._settlement = data.Settlement;
			this._newOwner = data.NewOwner;
			base.NotificationIdentifier = "settlementownerchanged";
			this._onInspect = delegate()
			{
				base.GoToMapPosition(this._settlement.Position);
				base.ExecuteRemove();
			};
			CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, new Action<Settlement, bool, Hero, Hero, Hero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail>(this.OnSettlementOwnerChanged));
		}

		// Token: 0x0600064A RID: 1610 RVA: 0x00020440 File Offset: 0x0001E640
		private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
		{
			if (settlement == this._settlement && newOwner != this._newOwner)
			{
				base.ExecuteRemove();
			}
		}

		// Token: 0x0600064B RID: 1611 RVA: 0x0002045A File Offset: 0x0001E65A
		public override void OnFinalize()
		{
			base.OnFinalize();
			CampaignEvents.OnSettlementOwnerChangedEvent.ClearListeners(this);
		}

		// Token: 0x040002AF RID: 687
		private Settlement _settlement;

		// Token: 0x040002B0 RID: 688
		private Hero _newOwner;
	}
}
