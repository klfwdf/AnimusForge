using TaleWorlds.CampaignSystem.MapNotificationTypes;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes;

public class ArmyDispersionItemVM : MapNotificationItemBaseVM
{
	public ArmyDispersionItemVM(ArmyDispersionMapNotification data)
		: base(data)
	{
		ArmyDispersionItemVM armyDispersionItemVM = this;
		base.NotificationIdentifier = "armydispersion";
		_onInspect = delegate
		{
			armyDispersionItemVM.NavigationHandler?.OpenKingdom(data.DispersedArmy);
			armyDispersionItemVM.ExecuteRemove();
		};
	}
}
