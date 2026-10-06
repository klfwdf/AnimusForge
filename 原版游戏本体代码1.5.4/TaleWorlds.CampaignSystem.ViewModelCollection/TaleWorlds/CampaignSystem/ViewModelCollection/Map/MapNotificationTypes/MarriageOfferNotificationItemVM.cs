using TaleWorlds.CampaignSystem.MapNotificationTypes;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes;

public class MarriageOfferNotificationItemVM : MapNotificationItemBaseVM
{
	private bool _playerInspectedNotification;

	private readonly Hero _suitor;

	private readonly Hero _maiden;

	public MarriageOfferNotificationItemVM(MarriageOfferMapNotification data)
		: base(data)
	{
		_suitor = data.Suitor;
		_maiden = data.Maiden;
		base.NotificationIdentifier = "marriage";
		_onInspect = delegate
		{
			CampaignEventDispatcher.Instance.OnMarriageOfferedToPlayer(_suitor, _maiden);
			_playerInspectedNotification = true;
			Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
			ExecuteRemove();
		};
		CampaignEvents.OnMarriageOfferCanceledEvent.AddNonSerializedListener(this, OnMarriageOfferCanceled);
	}

	private void OnMarriageOfferCanceled(Hero suitor, Hero maiden)
	{
		if (Campaign.Current.CampaignInformationManager.InformationDataExists((MarriageOfferMapNotification x) => x.Suitor == suitor && x.Maiden == maiden))
		{
			ExecuteRemove();
		}
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		CampaignEventDispatcher.Instance.RemoveListeners(this);
		if (!_playerInspectedNotification)
		{
			CampaignEventDispatcher.Instance.OnMarriageOfferCanceled(_suitor, _maiden);
		}
	}
}
