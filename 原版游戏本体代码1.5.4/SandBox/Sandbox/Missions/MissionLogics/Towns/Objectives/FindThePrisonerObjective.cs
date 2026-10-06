using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.Objectives;

namespace SandBox.Missions.MissionLogics.Towns.Objectives;

public class FindThePrisonerObjective : MissionObjective
{
	public override string UniqueId => "prison_break_find_the_prisoner_objective";

	public override TextObject Name => new TextObject("{=nxkYh5Ut}Find the Prisoner");

	public override TextObject Description => new TextObject("{=R7z9qNqS}Find and talk to the prisoner without alerting the guards.");

	public FindThePrisonerObjective(Mission mission)
		: base(mission)
	{
	}
}
