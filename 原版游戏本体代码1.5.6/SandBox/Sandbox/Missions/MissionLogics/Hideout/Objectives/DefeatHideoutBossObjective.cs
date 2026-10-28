using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.Objectives;

namespace SandBox.Missions.MissionLogics.Hideout.Objectives;

internal class DefeatHideoutBossObjective : MissionObjective
{
	private readonly TextObject _name;

	private readonly TextObject _description;

	public override string UniqueId => "hideout_mission_defeat_hideout_boss_objective";

	public override TextObject Name => _name;

	public override TextObject Description => _description;

	public DefeatHideoutBossObjective(Mission mission, bool isDuel)
		: base(mission)
	{
		_name = (isDuel ? new TextObject("{=QEynMlwL}Win the Duel") : new TextObject("{=0sPTRh6L}Win the Fight"));
		_description = (isDuel ? new TextObject("{=t13oVKkw}Win the duel against the bandit boss.") : new TextObject("{=7vqW1CsE}Eliminate the bandit boss and his troops."));
	}
}
