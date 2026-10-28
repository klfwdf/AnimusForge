using System.Linq;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes;

public class ProposeCallToWarOfferNotificationItemVM : MapNotificationItemBaseVM
{
	private readonly Kingdom _offeredKingdom;

	private readonly Kingdom _kingdomToCallToWarAgainst;

	private bool _shouldDecisionBeCreatedOnClosed;

	public ProposeCallToWarOfferNotificationItemVM(ProposeCallToWarOfferMapNotification data)
		: base(data)
	{
		ProposeCallToWarOfferNotificationItemVM proposeCallToWarOfferNotificationItemVM = this;
		_shouldDecisionBeCreatedOnClosed = false;
		_offeredKingdom = data.OfferedKingdom;
		_kingdomToCallToWarAgainst = data.KingdomToCallToWarAgainst;
		_onInspect = delegate
		{
			bool flag = false;
			if (data != null && data.IsValid() && Clan.PlayerClan.Kingdom != null)
			{
				flag = new ProposeCallToWarAgreementDecision(Clan.PlayerClan, proposeCallToWarOfferNotificationItemVM._offeredKingdom, proposeCallToWarOfferNotificationItemVM._kingdomToCallToWarAgainst).CanMakeDecision(out var _);
			}
			if (flag)
			{
				Campaign.Current.GetCampaignBehavior<IAllianceCampaignBehavior>()?.OnCallToWarAgreementProposedByPlayer(data.OfferedKingdom, data.KingdomToCallToWarAgainst);
				proposeCallToWarOfferNotificationItemVM.RemoveProposeCallToWarOfferNotification(shouldDecisionCreatedOnClosed: false);
			}
			else
			{
				InformationManager.ShowInquiry(new InquiryData("", new TextObject("{=oGgjuQav}This call to war offer is no longer relevant.").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, GameTexts.FindText("str_ok").ToString(), "", null, null));
				proposeCallToWarOfferNotificationItemVM.RemoveProposeCallToWarOfferNotification(shouldDecisionCreatedOnClosed: false);
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
		if ((faction1 == _offeredKingdom && faction2 == _kingdomToCallToWarAgainst) || (faction2 == _offeredKingdom && faction1 == _kingdomToCallToWarAgainst))
		{
			RemoveProposeCallToWarOfferNotification(shouldDecisionCreatedOnClosed: false);
		}
	}

	private void OnAllianceEnded(Kingdom kingdom1, Kingdom kingdom2)
	{
		if ((kingdom1 == Clan.PlayerClan.Kingdom && kingdom2 == _offeredKingdom) || (kingdom2 == Clan.PlayerClan.Kingdom && kingdom1 == _offeredKingdom))
		{
			RemoveProposeCallToWarOfferNotification(shouldDecisionCreatedOnClosed: false);
		}
	}

	private void OnKingdomDestroyed(Kingdom kingdom)
	{
		if (kingdom == Clan.PlayerClan.Kingdom || _offeredKingdom == kingdom || _kingdomToCallToWarAgainst == kingdom)
		{
			RemoveProposeCallToWarOfferNotification(shouldDecisionCreatedOnClosed: false);
		}
	}

	private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
	{
		if (clan == Clan.PlayerClan)
		{
			RemoveProposeCallToWarOfferNotification(shouldDecisionCreatedOnClosed: false);
		}
		else if (newKingdom == Clan.PlayerClan.Kingdom)
		{
			RemoveProposeCallToWarOfferNotification(shouldDecisionCreatedOnClosed: true);
		}
	}

	private void OnWarDeclared(IFaction side1Faction, IFaction side2Faction, DeclareWarAction.DeclareWarDetail detail)
	{
		if ((side1Faction == Hero.MainHero.MapFaction && side2Faction == _kingdomToCallToWarAgainst) || (side2Faction == Hero.MainHero.MapFaction && side1Faction == _kingdomToCallToWarAgainst))
		{
			RemoveProposeCallToWarOfferNotification(shouldDecisionCreatedOnClosed: false);
		}
	}

	private void RemoveProposeCallToWarOfferNotification(bool shouldDecisionCreatedOnClosed)
	{
		_shouldDecisionBeCreatedOnClosed = shouldDecisionCreatedOnClosed;
		ExecuteRemove();
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		CampaignEventDispatcher.Instance.RemoveListeners(this);
		if (_shouldDecisionBeCreatedOnClosed && Clan.PlayerClan.Kingdom != null && Clan.PlayerClan.Kingdom.Clans.Count > 1 && Clan.PlayerClan.Kingdom.UnresolvedDecisions.FirstOrDefault((KingdomDecision s) => s is ProposeCallToWarAgreementDecision proposeCallToWarAgreementDecision2 && proposeCallToWarAgreementDecision2.CalledKingdom == _offeredKingdom) == null)
		{
			ProposeCallToWarAgreementDecision proposeCallToWarAgreementDecision = new ProposeCallToWarAgreementDecision(Clan.PlayerClan, _offeredKingdom, _kingdomToCallToWarAgainst);
			if (proposeCallToWarAgreementDecision.CanMakeDecision(out var _))
			{
				Clan.PlayerClan.Kingdom.AddDecision(proposeCallToWarAgreementDecision, ignoreInfluenceCost: true);
			}
		}
	}
}
