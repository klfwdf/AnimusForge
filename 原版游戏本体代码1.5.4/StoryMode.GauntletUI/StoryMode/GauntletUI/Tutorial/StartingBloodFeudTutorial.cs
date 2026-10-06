using SandBox.GauntletUI.Tutorial;
using TaleWorlds.CampaignSystem;

namespace StoryMode.GauntletUI.Tutorial;

[Tutorial("StartingBloodFeudTutorial")]
public class StartingBloodFeudTutorial : BloodFeudExecutionTutorialBase
{
	protected override bool IsRelevantToBloodFeudState(Hero victimHero)
	{
		if (victimHero.Clan != null)
		{
			return !victimHero.Clan.HasBloodFeudWithPlayer;
		}
		return false;
	}
}
