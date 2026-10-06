using TaleWorlds.CampaignSystem.MapNotificationTypes;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes;

public class RansomNotificationItemVM : MapNotificationItemBaseVM
{
	private bool _playerInspectedNotification;

	private Hero _hero;

	public RansomNotificationItemVM(RansomOfferMapNotification data)
		: base(data)
	{
		RansomNotificationItemVM ransomNotificationItemVM = this;
		_hero = data.CaptiveHero;
		_onInspect = delegate
		{
			ransomNotificationItemVM._playerInspectedNotification = true;
			CampaignEventDispatcher.Instance.OnRansomOfferedToPlayer(data.CaptiveHero);
			ransomNotificationItemVM.ExecuteRemove();
		};
		CampaignEvents.OnRansomOfferCancelledEvent.AddNonSerializedListener(this, OnRansomOfferCancelled);
		base.NotificationIdentifier = "ransom";
	}

	private void OnRansomOfferCancelled(Hero captiveHero)
	{
		if (captiveHero == _hero)
		{
			ExecuteRemove();
		}
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		CampaignEvents.OnRansomOfferCancelledEvent.ClearListeners(this);
		if (!_playerInspectedNotification)
		{
			CampaignEventDispatcher.Instance.OnRansomOfferCancelled(_hero);
		}
	}
}
