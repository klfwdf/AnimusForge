using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class SiegeStrategyActionModel : MBGameModel<SiegeStrategyActionModel>
{
	public enum SiegeAction
	{
		ConstructNewSiegeEngine,
		DeploySiegeEngineFromReserve,
		MoveSiegeEngineToReserve,
		RemoveDeployedSiegeEngine,
		Hold
	}

	public abstract void GetLogicalActionForStrategy(ISiegeEventSide side, out SiegeAction siegeAction, out SiegeEngineType siegeEngineType, out int deploymentIndex, out int reserveIndex);
}
