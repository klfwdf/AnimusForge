using TaleWorlds.CampaignSystem.Settlements.Workshops;

namespace TaleWorlds.CampaignSystem.Actions;

public static class InitializeWorkshopAction
{
	public static void ApplyByNewGame(Workshop workshop, Hero workshopOwner, WorkshopType workshopType)
	{
		workshop.InitializeWorkshop(workshopOwner, workshopType);
		NameGenerator.Current.GenerateHeroNameAndHeroFullName(workshopOwner, out var firstName, out var fullName);
		workshopOwner.SetName(fullName, firstName);
		CampaignEventDispatcher.Instance.OnWorkshopInitialized(workshop);
	}
}
