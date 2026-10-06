using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library.EventSystem;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ArmyManagement;

public class PartyAddedToArmyByPlayerEvent : EventBase
{
	public MobileParty AddedParty { get; private set; }

	public PartyAddedToArmyByPlayerEvent(MobileParty addedParty)
	{
		AddedParty = addedParty;
	}
}
