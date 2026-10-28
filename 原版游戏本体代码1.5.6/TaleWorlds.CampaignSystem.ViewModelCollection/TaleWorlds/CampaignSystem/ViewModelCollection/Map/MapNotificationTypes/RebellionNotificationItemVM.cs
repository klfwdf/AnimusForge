using System;
using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes;

public class RebellionNotificationItemVM : MapNotificationItemBaseVM
{
	private Settlement _settlement;

	protected Action _onInspectAction;

	public RebellionNotificationItemVM(SettlementRebellionMapNotification data)
		: base(data)
	{
		_settlement = data.RebelliousSettlement;
		_onInspect = (_onInspectAction = delegate
		{
			GoToMapPosition(_settlement.Position);
		});
		base.NotificationIdentifier = "rebellion";
	}
}
