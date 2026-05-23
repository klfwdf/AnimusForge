using System;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.MapNotificationTypes;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes
{
	// Token: 0x0200004D RID: 77
	public class PeaceOfferNotificationItemVM : MapNotificationItemBaseVM
	{
		// Token: 0x0600062F RID: 1583 RVA: 0x0001FD0C File Offset: 0x0001DF0C
		public PeaceOfferNotificationItemVM(PeaceOfferMapNotification data)
			: base(data)
		{
			PeaceOfferNotificationItemVM <>4__this = this;
			this._shouldDecisionBeCreatedOnClosed = true;
			this._opponentFaction = data.OpponentFaction;
			this._tributeAmount = data.TributeAmount;
			this._tributeDurationInDays = data.TributeDurationInDays;
			this._onInspect = delegate()
			{
				CampaignEventDispatcher.Instance.OnPeaceOfferedToPlayer(data.OpponentFaction, data.TributeAmount, data.TributeDurationInDays);
				<>4__this.RemovePeaceOfferNotification(false);
			};
			CampaignEvents.OnPeaceOfferResolvedEvent.AddNonSerializedListener(this, new Action<IFaction>(this.OnPeaceOfferClosed));
			CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, new Action<Clan, Kingdom, Kingdom, ChangeKingdomAction.ChangeKingdomActionDetail, bool>(this.OnClanChangedKingdom));
			CampaignEvents.MakePeace.AddNonSerializedListener(this, new Action<IFaction, IFaction, MakePeaceAction.MakePeaceDetail>(this.OnMakePeace));
			base.NotificationIdentifier = "ransom";
		}

		// Token: 0x06000630 RID: 1584 RVA: 0x0001FDD5 File Offset: 0x0001DFD5
		private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
		{
			if (clan == Clan.PlayerClan)
			{
				this.RemovePeaceOfferNotification(false);
			}
		}

		// Token: 0x06000631 RID: 1585 RVA: 0x0001FDE6 File Offset: 0x0001DFE6
		private void OnMakePeace(IFaction side1Faction, IFaction side2Faction, MakePeaceAction.MakePeaceDetail detail)
		{
			if ((side1Faction == Hero.MainHero.MapFaction && side2Faction == this._opponentFaction) || (side2Faction == Hero.MainHero.MapFaction && side1Faction == this._opponentFaction))
			{
				this.RemovePeaceOfferNotification(false);
			}
		}

		// Token: 0x06000632 RID: 1586 RVA: 0x0001FE1B File Offset: 0x0001E01B
		private void OnPeaceOfferClosed(IFaction opponentFaction)
		{
			if (Campaign.Current.CampaignInformationManager.InformationDataExists<PeaceOfferMapNotification>((PeaceOfferMapNotification x) => x == base.Data))
			{
				this.RemovePeaceOfferNotification(true);
			}
		}

		// Token: 0x06000633 RID: 1587 RVA: 0x0001FE41 File Offset: 0x0001E041
		private void RemovePeaceOfferNotification(bool shouldDecisionCreatedOnClosed)
		{
			this._shouldDecisionBeCreatedOnClosed = shouldDecisionCreatedOnClosed;
			base.ExecuteRemove();
		}

		// Token: 0x06000634 RID: 1588 RVA: 0x0001FE50 File Offset: 0x0001E050
		public override void OnFinalize()
		{
			base.OnFinalize();
			CampaignEventDispatcher.Instance.RemoveListeners(this);
			if (this._shouldDecisionBeCreatedOnClosed && Hero.MainHero.MapFaction.Leader != Hero.MainHero)
			{
				bool flag = false;
				foreach (KingdomDecision kingdomDecision in ((Kingdom)Hero.MainHero.MapFaction).UnresolvedDecisions)
				{
					if (kingdomDecision is MakePeaceKingdomDecision && ((MakePeaceKingdomDecision)kingdomDecision).ProposerClan.MapFaction == Hero.MainHero.MapFaction && ((MakePeaceKingdomDecision)kingdomDecision).FactionToMakePeaceWith == this._opponentFaction)
					{
						flag = true;
					}
				}
				if (!flag)
				{
					MakePeaceKingdomDecision kingdomDecision2 = new MakePeaceKingdomDecision(Hero.MainHero.MapFaction.Leader.Clan, this._opponentFaction, -this._tributeAmount, this._tributeDurationInDays, true, true);
					((Kingdom)Hero.MainHero.MapFaction).AddDecision(kingdomDecision2, false);
				}
			}
		}

		// Token: 0x0400029F RID: 671
		private bool _shouldDecisionBeCreatedOnClosed;

		// Token: 0x040002A0 RID: 672
		private readonly IFaction _opponentFaction;

		// Token: 0x040002A1 RID: 673
		private readonly int _tributeAmount;

		// Token: 0x040002A2 RID: 674
		private readonly int _tributeDurationInDays;
	}
}
