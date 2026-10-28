using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class CampaignShipDamageModel : MBGameModel<CampaignShipDamageModel>
{
	public abstract int GetHourlyShipDamage(MobileParty owner, Ship ship);

	public abstract float GetEstimatedSafeSailDuration(MobileParty mobileParty);

	public abstract float GetShipDamage(Ship ship, Ship rammingShip, float rawDamage);
}
