using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultNotableSpawnModel : NotableSpawnModel
{
	public override int GetTargetNotableCountForSettlement(Settlement settlement, Occupation occupation)
	{
		int result = 0;
		if (settlement.IsTown)
		{
			result = occupation switch
			{
				Occupation.Merchant => 2, 
				Occupation.GangLeader => 2, 
				Occupation.Artisan => 1, 
				_ => 0, 
			};
		}
		else if (settlement.IsVillage)
		{
			switch (occupation)
			{
			case Occupation.Headman:
				result = 1;
				break;
			case Occupation.RuralNotable:
				result = 2;
				break;
			}
		}
		return result;
	}
}
