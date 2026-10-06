using System.Collections.Generic;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultClanMemberPartyRoleModel : ClanMemberPartyRoleModel
{
	public override int MaximumPartyRoleAssignmentCount => 2;

	public override IEnumerable<PartyRole> GetAssignablePartyRoles()
	{
		yield return PartyRole.Quartermaster;
		yield return PartyRole.Scout;
		yield return PartyRole.Surgeon;
		yield return PartyRole.Engineer;
	}

	public override SkillObject GetRelevantSkillForPartyRole(PartyRole role)
	{
		switch (role)
		{
		case PartyRole.Engineer:
			return DefaultSkills.Engineering;
		case PartyRole.Quartermaster:
			return DefaultSkills.Steward;
		case PartyRole.Scout:
			return DefaultSkills.Scouting;
		case PartyRole.Surgeon:
			return DefaultSkills.Medicine;
		default:
			Debug.FailedAssert($"Undefined clan role relevant skill {role}", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\GameComponents\\DefaultClanMemberRoleModel.cs", "GetRelevantSkillForPartyRole", 43);
			return null;
		}
	}

	public override bool IsHeroAssignableForPartyRole(Hero hero, PartyRole role, MobileParty party)
	{
		if (Campaign.Current.Models.ClanMemberPartyRoleModel.DoesHeroHaveEnoughSkillForPartyRole(hero, role, party))
		{
			return hero.CanBeGovernorOrHavePartyRole();
		}
		return false;
	}

	public override bool DoesHeroHaveEnoughSkillForPartyRole(Hero hero, PartyRole role, MobileParty party)
	{
		if (party.GetHeroPartyRoles(hero).Contains(role))
		{
			return true;
		}
		switch (role)
		{
		case PartyRole.Surgeon:
		case PartyRole.Engineer:
		case PartyRole.Scout:
		case PartyRole.Quartermaster:
			return Campaign.Current.Models.ClanMemberPartyRoleModel.IsHeroAssignableForPartyRoleInParty(role, hero, party);
		case PartyRole.None:
			return true;
		default:
			Debug.FailedAssert($"Undefined clan role is asked if assignable {role}", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\GameComponents\\DefaultClanMemberRoleModel.cs", "DoesHeroHaveEnoughSkillForPartyRole", 73);
			return false;
		}
	}

	public override bool IsHeroAssignableForPartyRoleInParty(PartyRole role, Hero hero, MobileParty party)
	{
		if (hero.PartyBelongedTo == party && hero != party.GetRoleHolder(role))
		{
			return hero.GetSkillValue(Campaign.Current.Models.ClanMemberPartyRoleModel.GetRelevantSkillForPartyRole(role)) >= 0;
		}
		return false;
	}
}
