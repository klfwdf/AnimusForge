using TaleWorlds.Library.EventSystem;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement;

public class ClanRoleAssignedThroughClanScreenEvent : EventBase
{
	public PartyRole Role { get; private set; }

	public Hero HeroObject { get; private set; }

	public ClanRoleAssignedThroughClanScreenEvent(PartyRole role, Hero heroObject)
	{
		Role = role;
		HeroObject = heroObject;
	}
}
