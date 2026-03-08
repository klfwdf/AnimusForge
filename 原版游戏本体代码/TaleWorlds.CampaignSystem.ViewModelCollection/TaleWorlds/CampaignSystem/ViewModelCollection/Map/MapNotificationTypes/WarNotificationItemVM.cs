using System;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.MapNotificationTypes;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes
{
	// Token: 0x02000057 RID: 87
	public class WarNotificationItemVM : MapNotificationItemBaseVM
	{
		// Token: 0x06000659 RID: 1625 RVA: 0x000206F0 File Offset: 0x0001E8F0
		public WarNotificationItemVM(WarMapNotification data)
			: base(data)
		{
			base.NotificationIdentifier = "battle";
			CampaignEvents.MakePeace.AddNonSerializedListener(this, new Action<IFaction, IFaction, MakePeaceAction.MakePeaceDetail>(this.OnPeaceMade));
			CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, new Action<Clan, Kingdom, Kingdom, ChangeKingdomAction.ChangeKingdomActionDetail, bool>(this.OnClanChangedKingdom));
			this._otherFaction = ((data.FirstFaction == Hero.MainHero.MapFaction) ? data.SecondFaction : data.FirstFaction);
			if (this._otherFaction.IsKingdomFaction)
			{
				this._onInspect = delegate()
				{
					INavigationHandler navigationHandler = base.NavigationHandler;
					if (navigationHandler == null)
					{
						return;
					}
					navigationHandler.OpenKingdom(this._otherFaction);
				};
				return;
			}
			this._onInspect = null;
		}

		// Token: 0x0600065A RID: 1626 RVA: 0x0002078A File Offset: 0x0001E98A
		public override void OnFinalize()
		{
			base.OnFinalize();
			CampaignEvents.MakePeace.ClearListeners(this);
			CampaignEvents.OnClanChangedKingdomEvent.ClearListeners(this);
		}

		// Token: 0x0600065B RID: 1627 RVA: 0x000207A8 File Offset: 0x0001E9A8
		private void OnPeaceMade(IFaction faction1, IFaction faction2, MakePeaceAction.MakePeaceDetail detail)
		{
			if ((faction1 == Hero.MainHero.Clan && this._otherFaction == faction2) || (faction2 == Hero.MainHero.Clan && this._otherFaction == faction1))
			{
				base.ExecuteRemove();
			}
		}

		// Token: 0x0600065C RID: 1628 RVA: 0x000207DC File Offset: 0x0001E9DC
		private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
		{
			if (clan == Clan.PlayerClan)
			{
				base.ExecuteRemove();
			}
		}

		// Token: 0x040002B3 RID: 691
		private readonly IFaction _otherFaction;
	}
}
