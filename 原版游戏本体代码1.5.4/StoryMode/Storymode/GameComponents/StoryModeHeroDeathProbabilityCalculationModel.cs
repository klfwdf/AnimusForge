using StoryMode.StoryModeObjects;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;

namespace StoryMode.GameComponents;

public class StoryModeHeroDeathProbabilityCalculationModel : HeroDeathProbabilityCalculationModel
{
	public override float CalculateHeroDeathProbability(Hero hero)
	{
		if (hero == StoryModeHeroes.ElderBrother && !StoryModeManager.Current.MainStoryLine.IsCompleted)
		{
			return 0f;
		}
		return base.BaseModel.CalculateHeroDeathProbability(hero);
	}
}
