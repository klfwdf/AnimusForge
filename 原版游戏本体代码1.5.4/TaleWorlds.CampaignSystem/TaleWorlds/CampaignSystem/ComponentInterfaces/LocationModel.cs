using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class LocationModel : MBGameModel<LocationModel>
{
	public abstract int GetSettlementUpgradeLevel(LocationEncounter locationEncounter);

	public abstract string GetCivilianSceneLevel(Settlement settlement);

	public abstract string GetCivilianUpgradeLevelTag(int upgradeLevel);

	public abstract string GetUpgradeLevelTag(int upgradeLevel);
}
