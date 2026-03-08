using System;
using TaleWorlds.CampaignSystem.MapNotificationTypes;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes
{
	// Token: 0x02000055 RID: 85
	public class TributeFinishedMapNotificationVM : MapNotificationItemBaseVM
	{
		// Token: 0x06000653 RID: 1619 RVA: 0x000205D0 File Offset: 0x0001E7D0
		public TributeFinishedMapNotificationVM(TributeFinishedMapNotification data)
			: base(data)
		{
			TributeFinishedMapNotificationVM <>4__this = this;
			base.NotificationIdentifier = "ransom";
			this._onInspect = delegate()
			{
				<>4__this.OnInspect(data.RelatedFaction);
			};
		}

		// Token: 0x06000654 RID: 1620 RVA: 0x0002061A File Offset: 0x0001E81A
		private void OnInspect(IFaction relatedFaction)
		{
			INavigationHandler navigationHandler = base.NavigationHandler;
			if (navigationHandler != null)
			{
				navigationHandler.OpenKingdom(relatedFaction);
			}
			base.ExecuteRemove();
		}
	}
}
