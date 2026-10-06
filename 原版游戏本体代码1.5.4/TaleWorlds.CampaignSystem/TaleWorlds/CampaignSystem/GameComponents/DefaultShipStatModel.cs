using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Naval;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultShipStatModel : ShipStatModel
{
	public override float GetShipFlagshipScore(Ship ship)
	{
		return 0f;
	}
}
