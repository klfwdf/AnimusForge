using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem;

public class CampaignTutorial
{
	public readonly string TutorialTypeId;

	public readonly int Priority;

	public TextObject Description => GameTexts.FindText("str_campaign_tutorial_description", TutorialTypeId);

	public TextObject Title => GameTexts.FindText("str_campaign_tutorial_title", TutorialTypeId);

	public CampaignTutorial(string tutorialType, int priority)
	{
		TutorialTypeId = tutorialType;
		Priority = priority;
	}
}
