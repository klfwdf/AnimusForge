using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultFerryModel : FerryModel
{
	public override int MaximumFerryCapacityForPassengers => 0;

	public override ExplainedNumber GetFerryCost(Settlement departureVillage, bool includeReason = false)
	{
		return new ExplainedNumber(0f, includeReason);
	}
}
