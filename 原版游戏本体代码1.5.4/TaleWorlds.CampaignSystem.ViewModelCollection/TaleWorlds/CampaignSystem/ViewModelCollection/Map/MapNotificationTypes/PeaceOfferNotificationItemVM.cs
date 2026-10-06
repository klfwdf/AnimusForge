using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.MapNotificationTypes;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes;

public class PeaceOfferNotificationItemVM : MapNotificationItemBaseVM
{
	private bool _shouldDecisionBeCreatedOnClosed;

	private readonly IFaction _opponentFaction;

	private readonly int _tributeAmount;

	private readonly int _tributeDurationInDays;

	public PeaceOfferNotificationItemVM(PeaceOfferMapNotification data)
		: base(data)
	{
		PeaceOfferNotificationItemVM peaceOfferNotificationItemVM = this;
		_shouldDecisionBeCreatedOnClosed = true;
		_opponentFaction = data.OpponentFaction;
		_tributeAmount = data.TributeAmount;
		_tributeDurationInDays = data.TributeDurationInDays;
		_onInspect = delegate
		{
			CampaignEventDispatcher.Instance.OnPeaceOfferedToPlayer(data.OpponentFaction, data.TributeAmount, data.TributeDurationInDays);
			peaceOfferNotificationItemVM.RemovePeaceOfferNotification(shouldDecisionCreatedOnClosed: false);
		};
		CampaignEvents.OnPeaceOfferResolvedEvent.AddNonSerializedListener(this, OnPeaceOfferClosed);
		CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
		CampaignEvents.MakePeace.AddNonSerializedListener(this, OnMakePeace);
		base.NotificationIdentifier = "ransom";
	}

	private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
	{
		if (clan == Clan.PlayerClan)
		{
			RemovePeaceOfferNotification(shouldDecisionCreatedOnClosed: false);
		}
	}

	private void OnMakePeace(IFaction side1Faction, IFaction side2Faction, MakePeaceAction.MakePeaceDetail detail)
	{
		if ((side1Faction == Hero.MainHero.MapFaction && side2Faction == _opponentFaction) || (side2Faction == Hero.MainHero.MapFaction && side1Faction == _opponentFaction))
		{
			RemovePeaceOfferNotification(shouldDecisionCreatedOnClosed: false);
		}
	}

	private void OnPeaceOfferClosed(IFaction opponentFaction)
	{
		if (Campaign.Current.CampaignInformationManager.InformationDataExists((PeaceOfferMapNotification x) => x == base.Data))
		{
			RemovePeaceOfferNotification(shouldDecisionCreatedOnClosed: true);
		}
	}

	private void RemovePeaceOfferNotification(bool shouldDecisionCreatedOnClosed)
	{
		_shouldDecisionBeCreatedOnClosed = shouldDecisionCreatedOnClosed;
		ExecuteRemove();
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		CampaignEventDispatcher.Instance.RemoveListeners(this);
		if (!_shouldDecisionBeCreatedOnClosed || Hero.MainHero.MapFaction.Leader == Hero.MainHero)
		{
			return;
		}
		bool flag = false;
		foreach (KingdomDecision unresolvedDecision in ((Kingdom)Hero.MainHero.MapFaction).UnresolvedDecisions)
		{
			if (unresolvedDecision is MakePeaceKingdomDecision && ((MakePeaceKingdomDecision)unresolvedDecision).ProposerClan.MapFaction == Hero.MainHero.MapFaction && ((MakePeaceKingdomDecision)unresolvedDecision).FactionToMakePeaceWith == _opponentFaction)
			{
				flag = true;
			}
		}
		if (!flag)
		{
			MakePeaceKingdomDecision kingdomDecision = new MakePeaceKingdomDecision(Hero.MainHero.MapFaction.Leader.Clan, _opponentFaction, -_tributeAmount, _tributeDurationInDays, applyResults: true, isProposedByOpponent: true);
			((Kingdom)Hero.MainHero.MapFaction).AddDecision(kingdomDecision, ignoreInfluenceCost: true);
		}
	}
}
