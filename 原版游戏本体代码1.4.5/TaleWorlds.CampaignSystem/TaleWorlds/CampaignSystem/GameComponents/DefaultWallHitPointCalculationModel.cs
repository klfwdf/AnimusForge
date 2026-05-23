using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultWallHitPointCalculationModel : WallHitPointCalculationModel
{
	public override float CalculateMaximumWallHitPoint(Town town)
	{
		if (town == null)
		{
			return 0f;
		}
		return CalculateMaximumWallHitPointInternal(town);
	}

	private float CalculateMaximumWallHitPointInternal(Town town)
	{
		float num = 0f;
		switch (town.GetWallLevel())
		{
		case 1:
			num += 30000f;
			break;
		case 2:
			num += 50000f;
			break;
		case 3:
			num += 67000f;
			break;
		default:
			Debug.FailedAssert(string.Concat("Settlement \"", town.Name, "\" has a wrong wall level set."), "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\GameComponents\\DefaultWallHitPointCalculationModel.cs", "CalculateMaximumWallHitPointInternal", 35);
			num += -1f;
			break;
		}
		Hero governor = town.Governor;
		if (governor != null && governor.GetPerkValue(DefaultPerks.Engineering.EngineeringGuilds))
		{
			num += num * DefaultPerks.Engineering.EngineeringGuilds.SecondaryBonus;
		}
		return num;
	}
}
