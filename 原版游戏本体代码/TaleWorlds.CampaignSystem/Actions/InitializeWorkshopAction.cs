using System;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Actions
{
	// Token: 0x020004B6 RID: 1206
	public static class InitializeWorkshopAction
	{
		// Token: 0x060049E3 RID: 18915 RVA: 0x001737D4 File Offset: 0x001719D4
		public static void ApplyByNewGame(Workshop workshop, Hero workshopOwner, WorkshopType workshopType)
		{
			workshop.InitializeWorkshop(workshopOwner, workshopType);
			TextObject firstName;
			TextObject fullName;
			NameGenerator.Current.GenerateHeroNameAndHeroFullName(workshopOwner, out firstName, out fullName, true);
			workshopOwner.SetName(fullName, firstName);
			CampaignEventDispatcher.Instance.OnWorkshopInitialized(workshop);
		}
	}
}
