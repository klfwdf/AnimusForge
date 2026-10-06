using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class FerryModel : MBGameModel<FerryModel>
{
	public abstract int MaximumFerryCapacityForPassengers { get; }

	public abstract ExplainedNumber GetFerryCost(Settlement departureVillage, bool includeReason = false);
}
