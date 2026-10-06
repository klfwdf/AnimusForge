using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.MapNotificationTypes;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes;

public class WarNotificationItemVM : MapNotificationItemBaseVM
{
	private readonly IFaction _otherFaction;

	public WarNotificationItemVM(WarMapNotification data)
		: base(data)
	{
		base.NotificationIdentifier = "battle";
		CampaignEvents.MakePeace.AddNonSerializedListener(this, OnPeaceMade);
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
		CampaignEvents.MakePeace.ClearListeners(this);
		CampaignEvents.OnClanChangedKingdomEvent.ClearListeners(this);
	}

	private void OnPeaceMade(IFaction faction1, IFaction faction2, MakePeaceAction.MakePeaceDetail detail)
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
