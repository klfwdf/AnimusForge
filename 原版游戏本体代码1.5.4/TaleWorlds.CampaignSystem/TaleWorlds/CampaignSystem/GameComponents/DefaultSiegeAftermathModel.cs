using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultSiegeAftermathModel : SiegeAftermathModel
{
	public override int GetSiegeAftermathTraitXpChangeForPlayer(TraitObject trait, Settlement devastatedSettlement, SiegeAftermathAction.SiegeAftermath aftermathType)
	{
		int result = 0;
		if (trait == DefaultTraits.Mercy)
		{
			switch (aftermathType)
			{
			case SiegeAftermathAction.SiegeAftermath.Devastate:
				result = ((!devastatedSettlement.IsTown) ? (-30) : (-50));
				break;
			case SiegeAftermathAction.SiegeAftermath.ShowMercy:
				result = ((!devastatedSettlement.IsTown) ? 10 : 20);
				break;
			}
		}
		return result;
	}
}
