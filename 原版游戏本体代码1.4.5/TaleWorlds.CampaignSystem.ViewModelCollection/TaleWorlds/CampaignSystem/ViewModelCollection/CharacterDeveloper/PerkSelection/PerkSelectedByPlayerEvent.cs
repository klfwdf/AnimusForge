using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Library.EventSystem;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper.PerkSelection;

public class PerkSelectedByPlayerEvent : EventBase
{
	public PerkObject SelectedPerk { get; private set; }

	public PerkSelectedByPlayerEvent(PerkObject selectedPerk)
	{
		SelectedPerk = selectedPerk;
	}
}
