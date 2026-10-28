using System.Collections.Generic;
using NavalDLC.CharacterDevelopment;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace NavalDLC.GameComponents;

public class NavalDLCClanMemberPartyRoleModel : ClanMemberPartyRoleModel
{
	public override int MaximumPartyRoleAssignmentCount => 2;

	public override IEnumerable<PartyRole> GetAssignablePartyRoles()
	{
		yield return PartyRole.Quartermaster;
		yield return PartyRole.Scout;
		yield return PartyRole.Surgeon;
		yield return PartyRole.Engineer;
		yield return PartyRole.FirstMate;
		yield return PartyRole.Navigator;
	}

	public override SkillObject GetRelevantSkillForPartyRole(PartyRole role)
	{
		return role switch
		{
			PartyRole.FirstMate => NavalSkills.Boatswain, 
			PartyRole.Navigator => NavalSkills.Shipmaster, 
			_ => base.BaseModel.GetRelevantSkillForPartyRole(role), 
		};
	}

	public override bool IsHeroAssignableForPartyRole(Hero hero, PartyRole role, MobileParty party)
	{
		return base.BaseModel.IsHeroAssignableForPartyRole(hero, role, party);
	}

	public override bool DoesHeroHaveEnoughSkillForPartyRole(Hero hero, PartyRole role, MobileParty party)
	{
		if (party.GetHeroPartyRoles(hero).Contains(role))
		{
			return true;
		}
		if (role == PartyRole.FirstMate || role == PartyRole.Navigator)
		{
			return Campaign.Current.Models.ClanMemberPartyRoleModel.IsHeroAssignableForPartyRoleInParty(role, hero, party);
		}
		return base.BaseModel.DoesHeroHaveEnoughSkillForPartyRole(hero, role, party);
	}

	public override bool IsHeroAssignableForPartyRoleInParty(PartyRole role, Hero hero, MobileParty party)
	{
		return base.BaseModel.IsHeroAssignableForPartyRoleInParty(role, hero, party);
	}
}
