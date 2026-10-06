using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.Objectives;

namespace NavalDLC.Storyline.Objectives.Quest4;

public class DefeatTheEnemyCrewObjective : MissionObjective
{
	public override string UniqueId => "naval_storyline_quest_4_defeat_the_enemy_crew_objective";

	public override TextObject Name => new TextObject("{=7OeuYDQS}Defeat the Enemy Crew");

	public override TextObject Description => new TextObject("{=aImP2qRA}Defeat Crusas’ men in the battle aboard the floating fortress");

	public DefeatTheEnemyCrewObjective(Mission mission)
		: base(mission)
	{
	}

	protected override bool IsActivationRequirementsMet()
	{
		return true;
	}

	protected override bool IsCompletionRequirementsMet()
	{
		return base.Mission.PlayerEnemyTeam.ActiveAgents.Count == 0;
	}
}
