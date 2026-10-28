using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.Party;

public struct ShipTemplateStack(ShipHull shipHull, int minValue, int maxValue)
{
	public ShipHull ShipHull = shipHull;

	public int MinValue = minValue;

	public int MaxValue = maxValue;
}
