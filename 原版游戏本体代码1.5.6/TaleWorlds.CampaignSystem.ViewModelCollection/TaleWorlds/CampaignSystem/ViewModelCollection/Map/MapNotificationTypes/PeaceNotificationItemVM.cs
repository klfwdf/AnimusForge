using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.MapNotificationTypes;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes;

public class PeaceNotificationItemVM : MapNotificationItemBaseVM
{
	private readonly IFaction _otherFaction;

	public PeaceNotificationItemVM(PeaceMapNotification data)
		: base(data)
	{
		base.NotificationIdentifier = "peace";
		CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
		CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
		_otherFaction = ((data.FirstFaction == Hero.MainHero.MapFaction) ? data.SecondFaction : data.FirstFaction);
		if (_otherFaction.IsKingdomFaction)
		{
			_onInspect = delegate
			{
				base.NavigationHandler?.OpenKingdom(_otherFaction);
			};
		}
		else
		{
			_onInspect = null;
		}
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		CampaignEvents.WarDeclared.ClearListeners(this);
		CampaignEvents.OnClanChangedKingdomEvent.ClearListeners(this);
	}

	private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
	{
		if ((faction1 == Hero.MainHero.Clan && _otherFaction == faction2) || (faction2 == Hero.MainHero.Clan && _otherFaction == faction1))
		{
			ExecuteRemove();
		}
	}

	private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
	{
		if (clan == Clan.PlayerClan)
		{
			ExecuteRemove();
		}
	}
}
