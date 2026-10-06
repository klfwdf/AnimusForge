using TaleWorlds.CampaignSystem.Party;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public interface IParleyCampaignBehavior
{
	PartyBase GetParleyedParty();

	void StartParley(PartyBase partyBase);
}
