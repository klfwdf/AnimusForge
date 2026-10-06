using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.MapNotificationTypes;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes;

public class MercenaryOfferMapNotificationItemVM : MapNotificationItemBaseVM
{
	private bool _playerInspectedNotification;

	private readonly Kingdom _offeredKingdom;

	public MercenaryOfferMapNotificationItemVM(MercenaryOfferMapNotification data)
		: base(data)
	{
		_offeredKingdom = data.OfferedKingdom;
		base.NotificationIdentifier = "vote";
		_onInspect = delegate
		{
			CampaignEventDispatcher.Instance.OnVassalOrMercenaryServiceOfferedToPlayer(_offeredKingdom);
			_playerInspectedNotification = true;
			ExecuteRemove();
		};
		CampaignEvents.OnVassalOrMercenaryServiceOfferCanceledEvent.AddNonSerializedListener(this, OnVassalOrMercenaryServiceOfferCanceled);
	}

	private void OnVassalOrMercenaryServiceOfferCanceled(Kingdom offeredKingdom)
	{
		if (Campaign.Current.CampaignInformationManager.InformationDataExists((MercenaryOfferMapNotification x) => x.OfferedKingdom == offeredKingdom))
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
			Campaign.Current.GetCampaignBehavior<IVassalAndMercenaryOfferCampaignBehavior>()?.CancelVassalOrMercenaryServiceOffer(_offeredKingdom);
		}
	}
}
