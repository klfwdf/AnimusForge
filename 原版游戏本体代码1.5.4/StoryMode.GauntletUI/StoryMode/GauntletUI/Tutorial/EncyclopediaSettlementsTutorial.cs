using SandBox.GauntletUI.Tutorial;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia;

namespace StoryMode.GauntletUI.Tutorial;

[Tutorial("EncyclopediaSettlementsTutorial")]
public class EncyclopediaSettlementsTutorial : EncyclopediaPageTutorialBase
{
	public EncyclopediaSettlementsTutorial()
		: base(EncyclopediaPages.Settlement, EncyclopediaPages.ListSettlements)
	{
	}
}
