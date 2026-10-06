using TaleWorlds.CampaignSystem.Party;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public interface IDisbandPartyCampaignBehavior : ICampaignBehavior
{
	bool IsPartyWaitingForDisband(MobileParty party);
}
