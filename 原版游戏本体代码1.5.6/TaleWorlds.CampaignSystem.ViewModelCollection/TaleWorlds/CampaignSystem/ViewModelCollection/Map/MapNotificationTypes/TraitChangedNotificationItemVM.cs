using TaleWorlds.CampaignSystem.MapNotificationTypes;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes;

public class TraitChangedNotificationItemVM : MapNotificationItemBaseVM
{
	public TraitChangedNotificationItemVM(TraitChangedMapNotification data)
		: base(data)
	{
		int currentTraitLevel = data.CurrentTraitLevel;
		int previousTraitLevel = data.PreviousTraitLevel;
		if (currentTraitLevel == 0 && previousTraitLevel != 0)
		{
			base.NotificationIdentifier = "traitlost_" + data.Trait.StringId.ToLower() + "_by_" + ((previousTraitLevel > 0) ? "decrease" : "increase");
		}
		else
		{
			base.NotificationIdentifier = "traitgained_" + data.Trait.StringId.ToLower() + "_" + currentTraitLevel;
		}
		_onInspect = delegate
		{
			base.NavigationHandler?.OpenCharacterDeveloper();
			ExecuteRemove();
		};
	}
}
