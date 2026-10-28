using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Naval;

namespace NavalDLC.CampaignBehaviors;

public interface IFleetManagementCampaignBehavior
{
	void SendShipToClan(Ship ship, Clan clan);
}
