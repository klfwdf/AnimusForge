using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.Core;

namespace StoryMode;

public static class StoryModeHelpers
{
	public static void SetPlayerSiblingsSkillsIfNeeded(Hero hero)
	{
		bool flag = false;
		foreach (SkillObject item in Skills.All)
		{
			if (hero.GetSkillValue(item) == 0)
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			return;
		}
		List<(SkillObject, int)> defaultSkillsForHero = Campaign.Current.Models.HeroCreationModel.GetDefaultSkillsForHero(hero);
		hero.ClearSkills();
		foreach (var item2 in defaultSkillsForHero)
		{
			hero.HeroDeveloper.SetInitialSkillLevel(item2.Item1, item2.Item2);
		}
		hero.HeroDeveloper.InitializeHeroDeveloper();
	}
}
