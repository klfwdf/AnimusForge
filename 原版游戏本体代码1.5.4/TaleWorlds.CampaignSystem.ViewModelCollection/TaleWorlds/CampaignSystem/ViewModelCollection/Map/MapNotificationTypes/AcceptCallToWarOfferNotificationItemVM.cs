using System.Linq;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes;

public class AcceptCallToWarOfferNotificationItemVM : MapNotificationItemBaseVM
{
	private readonly Kingdom _offeringKingdom;

	private readonly Kingdom _kingdomToCallToWarAgainst;

	private bool _shouldDecisionBeCreatedOnClosed;

	public AcceptCallToWarOfferNotificationItemVM(AcceptCallToWarOfferMapNotification data)
		: base(data)
	{
		AcceptCallToWarOfferNotificationItemVM acceptCallToWarOfferNotificationItemVM = this;
		_shouldDecisionBeCreatedOnClosed = false;
		_offeringKingdom = data.OfferingKingdom;
		_kingdomToCallToWarAgainst = data.KingdomToCallToWarAgainst;
		_onInspect = delegate
		{
			bool flag = false;
			if (data != null && data.IsValid() && Clan.PlayerClan.Kingdom != null)
			{
				flag = new AcceptCallToWarAgreementDecision(Clan.PlayerClan, acceptCallToWarOfferNotificationItemVM._offeringKingdom, acceptCallToWarOfferNotificationItemVM._kingdomToCallToWarAgainst).CanMakeDecision(out var _);
			}
			if (flag)
			{
				Campaign.Current.GetCampaignBehavior<IAllianceCampaignBehavior>()?.OnCallToWarAgreementProposedToPlayer(data.OfferingKingdom, data.KingdomToCallToWarAgainst);
				acceptCallToWarOfferNotificationItemVM.RemoveAcceptCallToWarOfferNotification(shouldDecisionCreatedOnClosed: false);
			}
			else
			{
				InformationManager.ShowInquiry(new InquiryData("", new TextObject("{=oGgjuQav}This call to war offer is no longer relevant.").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, GameTexts.FindText("str_ok").ToString(), "", null, null));
				acceptCallToWarOfferNotificationItemVM.RemoveAcceptCallToWarOfferNotification(shouldDecisionCreatedOnClosed: false);
			}
		};
		CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
		CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
		CampaignEvents.MakePeace.AddNonSerializedListener(this, OnPeaceDeclared);
		CampaignEvents.KingdomDestroyedEvent.AddNonSerializedListener(this, OnKingdomDestroyed);
		CampaignEvents.OnAllianceEndedEvent.AddNonSerializedListener(this, OnAllianceEnded);
		base.NotificationIdentifier = "ransom";
	}

	private void OnPeaceDeclared(IFaction faction1, IFaction faction2, MakePeaceAction.MakePeaceDetail detail)
	{
		if ((faction1 == _offeringKingdom && faction2 == _kingdomToCallToWarAgainst) || (faction2 == _offeringKingdom && faction1 == _kingdomToCallToWarAgainst))
		{
			RemoveAcceptCallToWarOfferNotification(shouldDecisionCreatedOnClosed: false);
		}
	}

	private void OnAllianceEnded(Kingdom kingdom1, Kingdom kingdom2)
	{
		if ((kingdom1 == Clan.PlayerClan.Kingdom && kingdom2 == _offeringKingdom) || (kingdom2 == Clan.PlayerClan.Kingdom && kingdom1 == _offeringKingdom))
		{
			RemoveAcceptCallToWarOfferNotification(shouldDecisionCreatedOnClosed: false);
		}
	}

	private void OnKingdomDestroyed(Kingdom kingdom)
	{
		if (kingdom == Clan.PlayerClan.Kingdom || _offeringKingdom == kingdom || _kingdomToCallToWarAgainst == kingdom)
		{
			RemoveAcceptCallToWarOfferNotification(shouldDecisionCreatedOnClosed: false);
		}
	}

	private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
	{
		if (clan == Clan.PlayerClan)
		{
			RemoveAcceptCallToWarOfferNotification(shouldDecisionCreatedOnClosed: false);
		}
		else if (newKingdom == Clan.PlayerClan.Kingdom)
		{
			RemoveAcceptCallToWarOfferNotification(shouldDecisionCreatedOnClosed: true);
		}
	}

	private void OnWarDeclared(IFaction side1Faction, IFaction side2Faction, DeclareWarAction.DeclareWarDetail detail)
	{
		if ((side1Faction == Hero.MainHero.MapFaction && side2Faction == _kingdomToCallToWarAgainst) || (side2Faction == Hero.MainHero.MapFaction && side1Faction == _kingdomToCallToWarAgainst))
		{
			RemoveAcceptCallToWarOfferNotification(shouldDecisionCreatedOnClosed: false);
		}
	}

	private void RemoveAcceptCallToWarOfferNotification(bool shouldDecisionCreatedOnClosed)
	{
		_shouldDecisionBeCreatedOnClosed = shouldDecisionCreatedOnClosed;
		ExecuteRemove();
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		CampaignEventDispatcher.Instance.RemoveListeners(this);
		if (_shouldDecisionBeCreatedOnClosed && Clan.PlayerClan.Kingdom != null && Clan.PlayerClan.Kingdom.Clans.Count > 1 && Clan.PlayerClan.Kingdom.UnresolvedDecisions.FirstOrDefault((KingdomDecision s) => s is AcceptCallToWarAgreementDecision acceptCallToWarAgreementDecision2 && acceptCallToWarAgreementDecision2.CallingKingdom == _offeringKingdom) == null)
		{
			AcceptCallToWarAgreementDecision acceptCallToWarAgreementDecision = new AcceptCallToWarAgreementDecision(Clan.PlayerClan, _offeringKingdom, _kingdomToCallToWarAgainst);
			if (acceptCallToWarAgreementDecision.CanMakeDecision(out var _))
			{
				Clan.PlayerClan.Kingdom.AddDecision(acceptCallToWarAgreementDecision, ignoreInfluenceCost: true);
			}
		}
	}
}
