using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultDailyTroopXpBonusModel : DailyTroopXpBonusModel
{
	public override int CalculateDailyTroopXpBonus(Town town)
	{
		return CalculateTroopXpBonusInternal(town);
	}

	private int CalculateTroopXpBonusInternal(Town town)
	{
		ExplainedNumber result = new ExplainedNumber(0f, includeDescriptions: false, null);
		town.AddEffectOfBuildings(BuildingEffectEnum.ExperiencePerDay, ref result);
		PerkHelper.AddPerkBonusForTown(DefaultPerks.Leadership.RaiseTheMeek, town, ref result);
		PerkHelper.AddPerkBonusForTown(DefaultPerks.TwoHanded.ProjectileDeflection, town, ref result);
		return (int)result.ResultNumber;
	}

	public override float CalculateGarrisonXpBonusMultiplier(Town town)
	{
		return 1f;
	}
}
