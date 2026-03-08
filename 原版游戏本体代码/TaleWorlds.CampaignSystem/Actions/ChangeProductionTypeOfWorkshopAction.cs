using System;
using TaleWorlds.CampaignSystem.Settlements.Workshops;

namespace TaleWorlds.CampaignSystem.Actions
{
	// Token: 0x0200049D RID: 1181
	public static class ChangeProductionTypeOfWorkshopAction
	{
		// Token: 0x06004973 RID: 18803 RVA: 0x00171EA8 File Offset: 0x001700A8
		public static void Apply(Workshop workshop, WorkshopType newWorkshopType, bool ignoreCost = false)
		{
			int num = (ignoreCost ? 0 : Campaign.Current.Models.WorkshopModel.GetConvertProductionCost(newWorkshopType));
			workshop.ChangeWorkshopProduction(newWorkshopType);
			if (num > 0)
			{
				GiveGoldAction.ApplyBetweenCharacters(workshop.Owner, null, num, false);
			}
			CampaignEventDispatcher.Instance.OnWorkshopTypeChanged(workshop);
		}
	}
}
