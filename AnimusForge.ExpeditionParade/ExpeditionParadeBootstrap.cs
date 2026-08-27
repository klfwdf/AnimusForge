using AnimusForge.ExpeditionParade.Campaign;
using TaleWorlds.CampaignSystem;

namespace AnimusForge.ExpeditionParade;

internal static class ExpeditionParadeBootstrap
{
	internal static void AddCampaignBehaviors(CampaignGameStarter starter)
	{
		if (starter == null)
		{
			return;
		}
		starter.AddBehavior(new ParadeCampaignBehavior());
	}
}
