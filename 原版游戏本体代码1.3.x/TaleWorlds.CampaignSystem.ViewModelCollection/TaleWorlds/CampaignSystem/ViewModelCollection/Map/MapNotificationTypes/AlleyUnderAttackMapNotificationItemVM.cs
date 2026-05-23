using System;
using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes
{
	// Token: 0x0200003D RID: 61
	public class AlleyUnderAttackMapNotificationItemVM : MapNotificationItemBaseVM
	{
		// Token: 0x060005D2 RID: 1490 RVA: 0x0001E9E0 File Offset: 0x0001CBE0
		public AlleyUnderAttackMapNotificationItemVM(AlleyUnderAttackMapNotification data)
			: base(data)
		{
			this._alley = data.Alley;
			base.NotificationIdentifier = "alley_under_attack";
			CampaignEvents.SettlementEntered.AddNonSerializedListener(this, new Action<MobileParty, Settlement, Hero>(this.OnSettlementEnter));
			this._onInspect = delegate()
			{
				base.GoToMapPosition(this._alley.Settlement.Position);
			};
		}

		// Token: 0x060005D3 RID: 1491 RVA: 0x0001EA34 File Offset: 0x0001CC34
		private void OnSettlementEnter(MobileParty party, Settlement settlement, Hero hero)
		{
			if (party != null && party.IsMainParty && settlement == this._alley.Settlement)
			{
				CampaignEventDispatcher.Instance.RemoveListeners(this);
				base.ExecuteRemove();
			}
		}

		// Token: 0x0400027A RID: 634
		private Alley _alley;
	}
}
