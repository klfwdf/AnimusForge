using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes;

public class AlleyUnderAttackMapNotificationItemVM : MapNotificationItemBaseVM
{
	private Alley _alley;

	public AlleyUnderAttackMapNotificationItemVM(AlleyUnderAttackMapNotification data)
		: base(data)
	{
		_alley = data.Alley;
		base.NotificationIdentifier = "alley_under_attack";
		CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEnter);
		_onInspect = delegate
		{
			GoToMapPosition(_alley.Settlement.Position);
		};
	}

	private void OnSettlementEnter(MobileParty party, Settlement settlement, Hero hero)
	{
		if (party != null && party.IsMainParty && settlement == _alley.Settlement)
		{
			CampaignEventDispatcher.Instance.RemoveListeners(this);
			ExecuteRemove();
		}
	}
}
