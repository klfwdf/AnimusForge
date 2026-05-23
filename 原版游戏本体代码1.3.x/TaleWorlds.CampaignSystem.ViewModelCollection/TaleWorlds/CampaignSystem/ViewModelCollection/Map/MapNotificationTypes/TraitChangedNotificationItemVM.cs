using System;
using TaleWorlds.CampaignSystem.MapNotificationTypes;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes
{
	// Token: 0x02000054 RID: 84
	public class TraitChangedNotificationItemVM : MapNotificationItemBaseVM
	{
		// Token: 0x06000651 RID: 1617 RVA: 0x00020518 File Offset: 0x0001E718
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
				base.NotificationIdentifier = "traitgained_" + data.Trait.StringId.ToLower() + "_" + currentTraitLevel.ToString();
			}
			this._onInspect = delegate()
			{
				INavigationHandler navigationHandler = base.NavigationHandler;
				if (navigationHandler != null)
				{
					navigationHandler.OpenCharacterDeveloper();
				}
				base.ExecuteRemove();
			};
		}
	}
}
