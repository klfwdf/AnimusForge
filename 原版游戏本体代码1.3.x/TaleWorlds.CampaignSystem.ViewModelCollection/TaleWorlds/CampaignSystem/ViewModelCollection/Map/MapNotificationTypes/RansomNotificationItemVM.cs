using System;
using TaleWorlds.CampaignSystem.MapNotificationTypes;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes
{
	// Token: 0x02000050 RID: 80
	public class RansomNotificationItemVM : MapNotificationItemBaseVM
	{
		// Token: 0x06000644 RID: 1604 RVA: 0x000202D4 File Offset: 0x0001E4D4
		public RansomNotificationItemVM(RansomOfferMapNotification data)
			: base(data)
		{
			RansomNotificationItemVM <>4__this = this;
			this._hero = data.CaptiveHero;
			this._onInspect = delegate()
			{
				<>4__this._playerInspectedNotification = true;
				CampaignEventDispatcher.Instance.OnRansomOfferedToPlayer(data.CaptiveHero);
				<>4__this.ExecuteRemove();
			};
			CampaignEvents.OnRansomOfferCancelledEvent.AddNonSerializedListener(this, new Action<Hero>(this.OnRansomOfferCancelled));
			base.NotificationIdentifier = "ransom";
		}

		// Token: 0x06000645 RID: 1605 RVA: 0x00020346 File Offset: 0x0001E546
		private void OnRansomOfferCancelled(Hero captiveHero)
		{
			if (captiveHero == this._hero)
			{
				base.ExecuteRemove();
			}
		}

		// Token: 0x06000646 RID: 1606 RVA: 0x00020357 File Offset: 0x0001E557
		public override void OnFinalize()
		{
			base.OnFinalize();
			CampaignEvents.OnRansomOfferCancelledEvent.ClearListeners(this);
			if (!this._playerInspectedNotification)
			{
				CampaignEventDispatcher.Instance.OnRansomOfferCancelled(this._hero);
			}
		}

		// Token: 0x040002AB RID: 683
		private bool _playerInspectedNotification;

		// Token: 0x040002AC RID: 684
		private Hero _hero;
	}
}
