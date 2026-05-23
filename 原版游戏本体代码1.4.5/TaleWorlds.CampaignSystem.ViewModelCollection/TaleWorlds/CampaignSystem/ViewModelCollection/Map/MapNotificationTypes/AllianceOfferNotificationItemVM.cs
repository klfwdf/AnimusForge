using System.Linq;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes;

public class AllianceOfferNotificationItemVM : MapNotificationItemBaseVM
{
	private readonly Kingdom _offeringKingdom;

	private bool _shouldDecisionBeCreatedOnClosed;

	public AllianceOfferNotificationItemVM(AllianceOfferMapNotification data)
		: base(data)
	{
		AllianceOfferNotificationItemVM allianceOfferNotificationItemVM = this;
		_shouldDecisionBeCreatedOnClosed = false;
		_offeringKingdom = data.OfferingKingdom;
		_onInspect = delegate
		{
			bool flag = false;
			if (data != null && data.IsValid() && Clan.PlayerClan.Kingdom != null)
			{
				flag = new StartAllianceDecision(Clan.PlayerClan, allianceOfferNotificationItemVM._offeringKingdom).CanMakeDecision(out var _);
			}
			if (flag)
			{
				Campaign.Current.GetCampaignBehavior<IAllianceCampaignBehavior>()?.OnAllianceOfferedToPlayer(data.OfferingKingdom);
				allianceOfferNotificationItemVM.RemoveAllianceOfferNotification(shouldDecisionCreatedOnClosed: false);
			}
			else
			{
				InformationManager.ShowInquiry(new InquiryData("", new TextObject("{=4vPm9bFW}This alliance offer is no longer relevant.").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, GameTexts.FindText("str_ok").ToString(), "", null, null));
				allianceOfferNotificationItemVM.RemoveAllianceOfferNotification(shouldDecisionCreatedOnClosed: false);
			}
		};
		CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
		CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
		CampaignEvents.KingdomDestroyedEvent.AddNonSerializedListener(this, OnKingdomDestroyed);
		CampaignEvents.OnAllianceStartedEvent.AddNonSerializedListener(this, OnAllianceStarted);
		base.NotificationIdentifier = "ransom";
	}

	private void OnAllianceStarted(Kingdom kingdom1, Kingdom kingdom2)
	{
		if ((kingdom1 == Clan.PlayerClan.Kingdom && kingdom2 == _offeringKingdom) || (kingdom2 == Clan.PlayerClan.Kingdom && kingdom1 == _offeringKingdom))
		{
			RemoveAllianceOfferNotification(shouldDecisionCreatedOnClosed: false);
		}
	}

	private void OnKingdomDestroyed(Kingdom kingdom)
	{
		if (kingdom == Clan.PlayerClan.Kingdom || _offeringKingdom == kingdom)
		{
			RemoveAllianceOfferNotification(shouldDecisionCreatedOnClosed: false);
		}
	}

	private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
	{
		if (clan == Clan.PlayerClan)
		{
			RemoveAllianceOfferNotification(shouldDecisionCreatedOnClosed: false);
		}
		else if (newKingdom == Clan.PlayerClan.Kingdom)
		{
			RemoveAllianceOfferNotification(shouldDecisionCreatedOnClosed: true);
		}
	}

	private void OnWarDeclared(IFaction side1Faction, IFaction side2Faction, DeclareWarAction.DeclareWarDetail detail)
	{
		if ((side1Faction == Hero.MainHero.MapFaction && side2Faction == _offeringKingdom) || (side2Faction == Hero.MainHero.MapFaction && side1Faction == _offeringKingdom))
		{
			RemoveAllianceOfferNotification(shouldDecisionCreatedOnClosed: false);
		}
	}

	private void RemoveAllianceOfferNotification(bool shouldDecisionCreatedOnClosed)
	{
		_shouldDecisionBeCreatedOnClosed = shouldDecisionCreatedOnClosed;
		ExecuteRemove();
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		CampaignEventDispatcher.Instance.RemoveListeners(this);
		if (_shouldDecisionBeCreatedOnClosed && Clan.PlayerClan.Kingdom != null && Clan.PlayerClan.Kingdom.Clans.Count > 1 && Clan.PlayerClan.Kingdom.UnresolvedDecisions.FirstOrDefault((KingdomDecision s) => s is StartAllianceDecision startAllianceDecision2 && startAllianceDecision2.KingdomToStartAllianceWith == _offeringKingdom) == null)
		{
			StartAllianceDecision startAllianceDecision = new StartAllianceDecision(Clan.PlayerClan, _offeringKingdom);
			if (startAllianceDecision.CanMakeDecision(out var _))
			{
				Clan.PlayerClan.Kingdom.AddDecision(startAllianceDecision, ignoreInfluenceCost: true);
			}
		}
	}
}
