using System;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.MapNotificationTypes;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes
{
	// Token: 0x02000049 RID: 73
	public class MercenaryOfferMapNotificationItemVM : MapNotificationItemBaseVM
	{
		// Token: 0x0600061D RID: 1565 RVA: 0x0001F848 File Offset: 0x0001DA48
		public MercenaryOfferMapNotificationItemVM(MercenaryOfferMapNotification data)
			: base(data)
		{
			this._offeredKingdom = data.OfferedKingdom;
			base.NotificationIdentifier = "vote";
			this._onInspect = delegate()
			{
				CampaignEventDispatcher.Instance.OnVassalOrMercenaryServiceOfferedToPlayer(this._offeredKingdom);
				this._playerInspectedNotification = true;
				base.ExecuteRemove();
			};
			CampaignEvents.OnVassalOrMercenaryServiceOfferCanceledEvent.AddNonSerializedListener(this, new Action<Kingdom>(this.OnVassalOrMercenaryServiceOfferCanceled));
		}

		// Token: 0x0600061E RID: 1566 RVA: 0x0001F89C File Offset: 0x0001DA9C
		private void OnVassalOrMercenaryServiceOfferCanceled(Kingdom offeredKingdom)
		{
			if (Campaign.Current.CampaignInformationManager.InformationDataExists<MercenaryOfferMapNotification>((MercenaryOfferMapNotification x) => x.OfferedKingdom == offeredKingdom))
			{
				base.ExecuteRemove();
			}
		}

		// Token: 0x0600061F RID: 1567 RVA: 0x0001F8D9 File Offset: 0x0001DAD9
		public override void OnFinalize()
		{
			base.OnFinalize();
			CampaignEventDispatcher.Instance.RemoveListeners(this);
			if (!this._playerInspectedNotification)
			{
				IVassalAndMercenaryOfferCampaignBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<IVassalAndMercenaryOfferCampaignBehavior>();
				if (campaignBehavior == null)
				{
					return;
				}
				campaignBehavior.CancelVassalOrMercenaryServiceOffer(this._offeredKingdom);
			}
		}

		// Token: 0x04000297 RID: 663
		private bool _playerInspectedNotification;

		// Token: 0x04000298 RID: 664
		private readonly Kingdom _offeredKingdom;
	}
}
