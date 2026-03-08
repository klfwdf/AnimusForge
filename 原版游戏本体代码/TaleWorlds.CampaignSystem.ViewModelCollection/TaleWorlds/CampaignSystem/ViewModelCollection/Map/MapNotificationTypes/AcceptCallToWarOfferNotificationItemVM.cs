using System;
using System.Linq;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes
{
	// Token: 0x0200003B RID: 59
	public class AcceptCallToWarOfferNotificationItemVM : MapNotificationItemBaseVM
	{
		// Token: 0x060005C6 RID: 1478 RVA: 0x0001E630 File Offset: 0x0001C830
		public AcceptCallToWarOfferNotificationItemVM(AcceptCallToWarOfferMapNotification data)
			: base(data)
		{
			AcceptCallToWarOfferNotificationItemVM <>4__this = this;
			this._shouldDecisionBeCreatedOnClosed = false;
			this._offeringKingdom = data.OfferingKingdom;
			this._kingdomToCallToWarAgainst = data.KingdomToCallToWarAgainst;
			this._onInspect = delegate()
			{
				bool flag = false;
				if (data != null && data.IsValid() && Clan.PlayerClan.Kingdom != null)
				{
					TextObject textObject;
					flag = new AcceptCallToWarAgreementDecision(Clan.PlayerClan, <>4__this._offeringKingdom, <>4__this._kingdomToCallToWarAgainst).CanMakeDecision(out textObject, false);
				}
				if (flag)
				{
					Campaign.Current.GetCampaignBehavior<IAllianceCampaignBehavior>().OnCallToWarAgreementProposedToPlayer(data.OfferingKingdom, data.KingdomToCallToWarAgainst);
					<>4__this.RemoveAcceptCallToWarOfferNotification(false);
					return;
				}
				InformationManager.ShowInquiry(new InquiryData("", new TextObject("{=oGgjuQav}This call to war offer is no longer relevant.", null).ToString(), true, false, GameTexts.FindText("str_ok", null).ToString(), "", null, null, "", 0f, null, null, null), false, false);
				<>4__this.RemoveAcceptCallToWarOfferNotification(false);
			};
			CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, new Action<Clan, Kingdom, Kingdom, ChangeKingdomAction.ChangeKingdomActionDetail, bool>(this.OnClanChangedKingdom));
			CampaignEvents.WarDeclared.AddNonSerializedListener(this, new Action<IFaction, IFaction, DeclareWarAction.DeclareWarDetail>(this.OnWarDeclared));
			CampaignEvents.MakePeace.AddNonSerializedListener(this, new Action<IFaction, IFaction, MakePeaceAction.MakePeaceDetail>(this.OnPeaceDeclared));
			CampaignEvents.KingdomDestroyedEvent.AddNonSerializedListener(this, new Action<Kingdom>(this.OnKingdomDestroyed));
			CampaignEvents.OnAllianceEndedEvent.AddNonSerializedListener(this, new Action<Kingdom, Kingdom>(this.OnAllianceEnded));
			base.NotificationIdentifier = "ransom";
		}

		// Token: 0x060005C7 RID: 1479 RVA: 0x0001E716 File Offset: 0x0001C916
		private void OnPeaceDeclared(IFaction faction1, IFaction faction2, MakePeaceAction.MakePeaceDetail detail)
		{
			if ((faction1 == this._offeringKingdom && faction2 == this._kingdomToCallToWarAgainst) || (faction2 == this._offeringKingdom && faction1 == this._kingdomToCallToWarAgainst))
			{
				this.RemoveAcceptCallToWarOfferNotification(false);
			}
		}

		// Token: 0x060005C8 RID: 1480 RVA: 0x0001E743 File Offset: 0x0001C943
		private void OnAllianceEnded(Kingdom kingdom1, Kingdom kingdom2)
		{
			if ((kingdom1 == Clan.PlayerClan.Kingdom && kingdom2 == this._offeringKingdom) || (kingdom2 == Clan.PlayerClan.Kingdom && kingdom1 == this._offeringKingdom))
			{
				this.RemoveAcceptCallToWarOfferNotification(false);
			}
		}

		// Token: 0x060005C9 RID: 1481 RVA: 0x0001E778 File Offset: 0x0001C978
		private void OnKingdomDestroyed(Kingdom kingdom)
		{
			if (kingdom == Clan.PlayerClan.Kingdom || this._offeringKingdom == kingdom || this._kingdomToCallToWarAgainst == kingdom)
			{
				this.RemoveAcceptCallToWarOfferNotification(false);
			}
		}

		// Token: 0x060005CA RID: 1482 RVA: 0x0001E7A0 File Offset: 0x0001C9A0
		private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
		{
			if (clan == Clan.PlayerClan)
			{
				this.RemoveAcceptCallToWarOfferNotification(false);
				return;
			}
			if (newKingdom == Clan.PlayerClan.Kingdom)
			{
				this.RemoveAcceptCallToWarOfferNotification(true);
			}
		}

		// Token: 0x060005CB RID: 1483 RVA: 0x0001E7C6 File Offset: 0x0001C9C6
		private void OnWarDeclared(IFaction side1Faction, IFaction side2Faction, DeclareWarAction.DeclareWarDetail detail)
		{
			if ((side1Faction == Hero.MainHero.MapFaction && side2Faction == this._kingdomToCallToWarAgainst) || (side2Faction == Hero.MainHero.MapFaction && side1Faction == this._kingdomToCallToWarAgainst))
			{
				this.RemoveAcceptCallToWarOfferNotification(false);
			}
		}

		// Token: 0x060005CC RID: 1484 RVA: 0x0001E7FB File Offset: 0x0001C9FB
		private void RemoveAcceptCallToWarOfferNotification(bool shouldDecisionCreatedOnClosed)
		{
			this._shouldDecisionBeCreatedOnClosed = shouldDecisionCreatedOnClosed;
			base.ExecuteRemove();
		}

		// Token: 0x060005CD RID: 1485 RVA: 0x0001E80C File Offset: 0x0001CA0C
		public override void OnFinalize()
		{
			base.OnFinalize();
			CampaignEventDispatcher.Instance.RemoveListeners(this);
			if (this._shouldDecisionBeCreatedOnClosed && Clan.PlayerClan.Kingdom != null && Clan.PlayerClan.Kingdom.Clans.Count > 1 && Clan.PlayerClan.Kingdom.UnresolvedDecisions.FirstOrDefault(delegate(KingdomDecision s)
			{
				AcceptCallToWarAgreementDecision acceptCallToWarAgreementDecision2;
				return (acceptCallToWarAgreementDecision2 = s as AcceptCallToWarAgreementDecision) != null && acceptCallToWarAgreementDecision2.CallingKingdom == this._offeringKingdom;
			}) == null)
			{
				AcceptCallToWarAgreementDecision acceptCallToWarAgreementDecision = new AcceptCallToWarAgreementDecision(Clan.PlayerClan, this._offeringKingdom, this._kingdomToCallToWarAgainst);
				TextObject textObject;
				if (acceptCallToWarAgreementDecision.CanMakeDecision(out textObject, false))
				{
					Clan.PlayerClan.Kingdom.AddDecision(acceptCallToWarAgreementDecision, true);
				}
			}
		}

		// Token: 0x04000276 RID: 630
		private readonly Kingdom _offeringKingdom;

		// Token: 0x04000277 RID: 631
		private readonly Kingdom _kingdomToCallToWarAgainst;

		// Token: 0x04000278 RID: 632
		private bool _shouldDecisionBeCreatedOnClosed;
	}
}
