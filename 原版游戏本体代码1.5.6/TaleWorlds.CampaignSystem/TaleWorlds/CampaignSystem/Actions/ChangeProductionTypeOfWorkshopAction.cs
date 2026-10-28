using TaleWorlds.CampaignSystem.Settlements.Workshops;

namespace TaleWorlds.CampaignSystem.Actions;

public static class ChangeProductionTypeOfWorkshopAction
{
	public static void Apply(Workshop workshop, WorkshopType newWorkshopType, bool ignoreCost = false)
	{
		int num = ((!ignoreCost) ? Campaign.Current.Models.WorkshopModel.GetConvertProductionCost(newWorkshopType) : 0);
		workshop.ChangeWorkshopProduction(newWorkshopType);
		if (num > 0)
		{
			GiveGoldAction.ApplyBetweenCharacters(workshop.Owner, null, num);
		}
		CampaignEventDispatcher.Instance.OnWorkshopTypeChanged(workshop);
	}
}
