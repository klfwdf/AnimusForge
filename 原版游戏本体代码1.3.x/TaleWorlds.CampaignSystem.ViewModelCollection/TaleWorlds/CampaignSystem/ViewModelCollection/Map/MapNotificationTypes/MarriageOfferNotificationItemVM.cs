using System;
using TaleWorlds.CampaignSystem.MapNotificationTypes;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes
{
	// Token: 0x02000048 RID: 72
	public class MarriageOfferNotificationItemVM : MapNotificationItemBaseVM
	{
		// Token: 0x06000619 RID: 1561 RVA: 0x0001F740 File Offset: 0x0001D940
		public MarriageOfferNotificationItemVM(MarriageOfferMapNotification data)
			: base(data)
		{
			this._suitor = data.Suitor;
			this._maiden = data.Maiden;
			base.NotificationIdentifier = "marriage";
			this._onInspect = delegate()
			{
				CampaignEventDispatcher.Instance.OnMarriageOfferedToPlayer(this._suitor, this._maiden);
				this._playerInspectedNotification = true;
				Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
				base.ExecuteRemove();
			};
			CampaignEvents.OnMarriageOfferCanceledEvent.AddNonSerializedListener(this, new Action<Hero, Hero>(this.OnMarriageOfferCanceled));
		}

		// Token: 0x0600061A RID: 1562 RVA: 0x0001F7A0 File Offset: 0x0001D9A0
		private void OnMarriageOfferCanceled(Hero suitor, Hero maiden)
		{
			if (Campaign.Current.CampaignInformationManager.InformationDataExists<MarriageOfferMapNotification>((MarriageOfferMapNotification x) => x.Suitor == suitor && x.Maiden == maiden))
			{
				base.ExecuteRemove();
			}
		}

		// Token: 0x0600061B RID: 1563 RVA: 0x0001F7E4 File Offset: 0x0001D9E4
		public override void OnFinalize()
		{
			base.OnFinalize();
			CampaignEventDispatcher.Instance.RemoveListeners(this);
			if (!this._playerInspectedNotification)
			{
				CampaignEventDispatcher.Instance.OnMarriageOfferCanceled(this._suitor, this._maiden);
			}
		}

		// Token: 0x04000294 RID: 660
		private bool _playerInspectedNotification;

		// Token: 0x04000295 RID: 661
		private readonly Hero _suitor;

		// Token: 0x04000296 RID: 662
		private readonly Hero _maiden;
	}
}
