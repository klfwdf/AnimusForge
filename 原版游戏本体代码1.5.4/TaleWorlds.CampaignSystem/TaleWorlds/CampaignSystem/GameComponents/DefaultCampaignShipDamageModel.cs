using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultCampaignShipDamageModel : CampaignShipDamageModel
{
	public override int GetHourlyShipDamage(MobileParty owner, Ship ship)
	{
		return 0;
	}

	public override float GetEstimatedSafeSailDuration(MobileParty mobileParty)
	{
		return 0f;
	}

	public override float GetShipDamage(Ship ship, Ship rammingShip, float rawDamage)
	{
		return rawDamage;
	}
}
