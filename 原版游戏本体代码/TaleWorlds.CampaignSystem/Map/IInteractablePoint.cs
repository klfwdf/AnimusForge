using System;
using TaleWorlds.CampaignSystem.Party;

namespace TaleWorlds.CampaignSystem.Map
{
	// Token: 0x02000219 RID: 537
	public interface IInteractablePoint
	{
		// Token: 0x06002046 RID: 8262
		CampaignVec2 GetInteractionPosition(MobileParty interactingParty);

		// Token: 0x06002047 RID: 8263
		bool CanPartyInteract(MobileParty mobileParty, float dt);

		// Token: 0x06002048 RID: 8264
		void OnPartyInteraction(MobileParty mobileParty);
	}
}
