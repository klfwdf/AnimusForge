using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultLocationModel : LocationModel
{
	public override int GetSettlementUpgradeLevel(LocationEncounter locationEncounter)
	{
		return locationEncounter.Settlement.Town.GetWallLevel();
	}

	public override string GetCivilianSceneLevel(Settlement settlement)
	{
		string text = "civilian";
		if (settlement.IsFortification)
		{
			string upgradeLevelTag = GetUpgradeLevelTag(settlement.Town.GetWallLevel());
			if (!upgradeLevelTag.IsEmpty())
			{
				text = text + " " + upgradeLevelTag;
			}
		}
		return text;
	}

	public override string GetCivilianUpgradeLevelTag(int upgradeLevel)
	{
		if (upgradeLevel == 0)
		{
			return "";
		}
		string text = "civilian";
		string upgradeLevelTag = GetUpgradeLevelTag(upgradeLevel);
		if (!upgradeLevelTag.IsEmpty())
		{
			text = text + " " + upgradeLevelTag;
		}
		return text;
	}

	public override string GetUpgradeLevelTag(int upgradeLevel)
	{
		return upgradeLevel switch
		{
			1 => "level_1", 
			2 => "level_2", 
			3 => "level_3", 
			_ => "", 
		};
	}
}
