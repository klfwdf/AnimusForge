using StoryMode.Extensions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;

namespace StoryMode.GameComponents;

public class StoryModeGenericXpModel : GenericXpModel
{
	public override float GetXpMultiplier(Hero hero)
	{
		if (hero?.CurrentSettlement != null && hero.CurrentSettlement.IsTrainingField())
		{
			return 0f;
		}
		return base.BaseModel.GetXpMultiplier(hero);
	}
}
