using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class ClanMemberPartyRoleModel : MBGameModel<ClanMemberPartyRoleModel>
{
	public abstract int MaximumPartyRoleAssignmentCount { get; }

	public abstract IEnumerable<PartyRole> GetAssignablePartyRoles();

	public abstract SkillObject GetRelevantSkillForPartyRole(PartyRole role);

	public abstract bool IsHeroAssignableForPartyRole(Hero hero, PartyRole role, MobileParty party);

	public abstract bool DoesHeroHaveEnoughSkillForPartyRole(Hero hero, PartyRole role, MobileParty party);

	public abstract bool IsHeroAssignableForPartyRoleInParty(PartyRole role, Hero hero, MobileParty party);
}
