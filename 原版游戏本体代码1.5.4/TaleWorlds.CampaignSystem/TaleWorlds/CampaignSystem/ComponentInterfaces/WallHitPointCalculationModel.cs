using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class WallHitPointCalculationModel : MBGameModel<WallHitPointCalculationModel>
{
	public abstract float CalculateMaximumWallHitPoint(Town town);
}
