using TaleWorlds.CampaignSystem.Party;

namespace TaleWorlds.CampaignSystem.Map;

public interface IInteractablePoint
{
	CampaignVec2 GetInteractionPosition(MobileParty interactingParty);

	bool CanPartyInteract(MobileParty mobileParty, float dt);

	void OnPartyInteraction(MobileParty mobileParty);
}
