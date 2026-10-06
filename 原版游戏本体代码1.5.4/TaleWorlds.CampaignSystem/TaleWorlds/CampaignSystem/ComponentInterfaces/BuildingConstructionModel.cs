using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class BuildingConstructionModel : MBGameModel<BuildingConstructionModel>
{
	public abstract int TownBoostCost { get; }

	public abstract int TownBoostBonus { get; }

	public abstract int CastleBoostCost { get; }

	public abstract int CastleBoostBonus { get; }

	public abstract ExplainedNumber CalculateDailyConstructionPower(Town town, bool includeDescriptions = false);

	public abstract int CalculateDailyConstructionPowerWithoutBoost(Town town);

	public abstract int GetBoostCost(Town town);

	public abstract int GetBoostAmount(Town town);
}
