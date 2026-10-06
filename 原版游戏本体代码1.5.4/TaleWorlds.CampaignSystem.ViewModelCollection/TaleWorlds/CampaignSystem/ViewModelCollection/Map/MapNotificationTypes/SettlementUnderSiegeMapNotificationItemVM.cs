using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes;

public class SettlementUnderSiegeMapNotificationItemVM : MapNotificationItemBaseVM
{
	private Settlement _settlement;

	public SettlementUnderSiegeMapNotificationItemVM(SettlementUnderSiegeMapNotification data)
		: base(data)
	{
		_settlement = data.BesiegedSettlement;
		base.NotificationIdentifier = "settlementundersiege";
		_onInspect = delegate
		{
			GoToMapPosition(_settlement.Position);
		};
		CampaignEvents.OnSiegeEventEndedEvent.AddNonSerializedListener(this, OnSiegeEventEnded);
	}

	private void OnSiegeEventEnded(SiegeEvent obj)
	{
		if (obj.BesiegedSettlement == _settlement)
		{
			ExecuteRemove();
		}
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		CampaignEvents.OnSiegeEventEndedEvent.ClearListeners(this);
	}
}
