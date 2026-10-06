using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.Party;

public struct ShipTemplateStack
{
	public ShipHull ShipHull;

	public int MinValue;

	public int MaxValue;

	public ShipTemplateStack(ShipHull shipHull, int minValue, int maxValue)
	{
		ShipHull = shipHull;
		MinValue = minValue;
		MaxValue = maxValue;
	}
}
