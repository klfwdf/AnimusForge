using SandBox.GauntletUI.Tutorial;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia;

namespace StoryMode.GauntletUI.Tutorial;

[Tutorial("EncyclopediaKingdomsTutorial")]
public class EncyclopediaKingdomsTutorial : EncyclopediaPageTutorialBase
{
	public EncyclopediaKingdomsTutorial()
		: base(EncyclopediaPages.Kingdom, EncyclopediaPages.ListKingdoms)
	{
	}
}
