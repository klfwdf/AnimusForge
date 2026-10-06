using SandBox.GauntletUI.Tutorial;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia;

namespace StoryMode.GauntletUI.Tutorial;

[Tutorial("EncyclopediaTroopsTutorial")]
public class EncyclopediaTroopsTutorial : EncyclopediaPageTutorialBase
{
	public EncyclopediaTroopsTutorial()
		: base(EncyclopediaPages.Unit, EncyclopediaPages.ListUnits)
	{
	}
}
