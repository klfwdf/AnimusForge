using System;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.MapNotificationTypes;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes
{
	// Token: 0x0200004C RID: 76
	public class PeaceNotificationItemVM : MapNotificationItemBaseVM
	{
		// Token: 0x0600062A RID: 1578 RVA: 0x0001FBF8 File Offset: 0x0001DDF8
		public PeaceNotificationItemVM(PeaceMapNotification data)
			: base(data)
		{
			base.NotificationIdentifier = "peace";
			CampaignEvents.WarDeclared.AddNonSerializedListener(this, new Action<IFaction, IFaction, DeclareWarAction.DeclareWarDetail>(this.OnWarDeclared));
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

		// Token: 0x0600062B RID: 1579 RVA: 0x0001FC92 File Offset: 0x0001DE92
		public override void OnFinalize()
		{
			base.OnFinalize();
			CampaignEvents.WarDeclared.ClearListeners(this);
			CampaignEvents.OnClanChangedKingdomEvent.ClearListeners(this);
		}

		// Token: 0x0600062C RID: 1580 RVA: 0x0001FCB0 File Offset: 0x0001DEB0
		private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
		{
			if ((faction1 == Hero.MainHero.Clan && this._otherFaction == faction2) || (faction2 == Hero.MainHero.Clan && this._otherFaction == faction1))
			{
				base.ExecuteRemove();
			}
		}

		// Token: 0x0600062D RID: 1581 RVA: 0x0001FCE4 File Offset: 0x0001DEE4
		private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
		{
			if (clan == Clan.PlayerClan)
			{
				base.ExecuteRemove();
			}
		}

		// Token: 0x0400029E RID: 670
		private readonly IFaction _otherFaction;
	}
}
