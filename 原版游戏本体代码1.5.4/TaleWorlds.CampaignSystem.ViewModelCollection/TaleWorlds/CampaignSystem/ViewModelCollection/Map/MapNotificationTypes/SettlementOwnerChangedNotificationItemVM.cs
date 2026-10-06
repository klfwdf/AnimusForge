using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes;

public class SettlementOwnerChangedNotificationItemVM : MapNotificationItemBaseVM
{
	private Settlement _settlement;

	private Hero _newOwner;

	public SettlementOwnerChangedNotificationItemVM(SettlementOwnerChangedMapNotification data)
		: base(data)
	{
		_settlement = data.Settlement;
		_newOwner = data.NewOwner;
		base.NotificationIdentifier = "settlementownerchanged";
		_onInspect = delegate
		{
			GoToMapPosition(_settlement.Position);
			ExecuteRemove();
		};
		CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
	}

	private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
	{
		if (settlement == _settlement && newOwner != _newOwner)
		{
			ExecuteRemove();
		}
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		CampaignEvents.OnSettlementOwnerChangedEvent.ClearListeners(this);
	}
}
