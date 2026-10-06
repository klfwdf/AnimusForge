using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.Objectives;

namespace SandBox.Missions.MissionLogics.Towns.Objectives;

public class EscapeThePrisonObjective : MissionObjective
{
	public override string UniqueId => "prison_break_escape_the_prison_objective";

	public override TextObject Name => new TextObject("{=LLZCYIzm}Escape the Prison");

	public override TextObject Description => new TextObject("{=ibdGpCkR}Reach the exit and escape with the prisoner.");

	public EscapeThePrisonObjective(Mission mission)
		: base(mission)
	{
	}
}
