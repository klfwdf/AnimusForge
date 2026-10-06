using TaleWorlds.CampaignSystem.MapNotificationTypes;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes;

public class VassalOfferMapNotificationItemVM : MapNotificationItemBaseVM
{
	private readonly Kingdom _offeredKingdom;

	public VassalOfferMapNotificationItemVM(VassalOfferMapNotification data)
		: base(data)
	{
		_offeredKingdom = data.OfferedKingdom;
		base.NotificationIdentifier = "vote";
		_onInspect = delegate
		{
			CampaignEventDispatcher.Instance.OnVassalOrMercenaryServiceOfferedToPlayer(_offeredKingdom);
			ExecuteRemove();
		};
		CampaignEvents.OnVassalOrMercenaryServiceOfferCanceledEvent.AddNonSerializedListener(this, OnVassalOrMercenaryServiceOfferCanceled);
	}

	private void OnVassalOrMercenaryServiceOfferCanceled(Kingdom offeredKingdom)
	{
		if (Campaign.Current.CampaignInformationManager.InformationDataExists((VassalOfferMapNotification x) => x.OfferedKingdom == offeredKingdom))
		{
			ExecuteRemove();
		}
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		CampaignEventDispatcher.Instance.RemoveListeners(this);
	}
}
