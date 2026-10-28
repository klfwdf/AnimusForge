using SandBox.GauntletUI.Tutorial;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia;

namespace StoryMode.GauntletUI.Tutorial;

[Tutorial("EncyclopediaClansTutorial")]
public class EncyclopediaClansTutorial : EncyclopediaPageTutorialBase
{
	public EncyclopediaClansTutorial()
		: base(EncyclopediaPages.Clan, EncyclopediaPages.ListClans)
	{
	}
}
