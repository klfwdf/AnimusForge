using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics;

public class SandboxGeneralsAndCaptainsAssignmentLogic : GeneralsAndCaptainsAssignmentLogic
{
	public SandboxGeneralsAndCaptainsAssignmentLogic(TextObject attackerGeneralName, TextObject defenderGeneralName, TextObject attackerAllyGeneralName = null, TextObject defenderAllyGeneralName = null, bool createBodyguard = true)
		: base(attackerGeneralName, defenderGeneralName, attackerAllyGeneralName, defenderAllyGeneralName, createBodyguard)
	{
	}

	protected override void SortCaptainsByPriority(Team team, ref List<Agent> captains)
	{
		EncounterModel encounterModel = Campaign.Current.Models.EncounterModel;
		if (encounterModel != null)
		{
			captains = captains.OrderByDescending((Agent captain) => (captain != team.GeneralAgent) ? ((float)((captain.Character is CharacterObject { HeroObject: not null } characterObject) ? encounterModel.GetCharacterSergeantScore(characterObject.HeroObject) : 0)) : float.MaxValue).ToList();
		}
		else
		{
			base.SortCaptainsByPriority(team, ref captains);
		}
	}
}
