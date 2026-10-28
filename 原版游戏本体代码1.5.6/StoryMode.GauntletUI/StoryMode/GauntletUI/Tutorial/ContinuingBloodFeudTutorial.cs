using SandBox.GauntletUI.Tutorial;
using TaleWorlds.CampaignSystem;

namespace StoryMode.GauntletUI.Tutorial;

[Tutorial("ContinuingBloodFeudTutorial")]
public class ContinuingBloodFeudTutorial : BloodFeudExecutionTutorialBase
{
	protected override bool IsRelevantToBloodFeudState(Hero victimHero)
	{
		if (victimHero.Clan != null)
		{
			return victimHero.Clan.HasBloodFeudWithPlayer;
		}
		return false;
	}
}
