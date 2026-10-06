using NavalDLC.Storyline.MissionControllers;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.Objectives;

namespace NavalDLC.Storyline.Objectives.Quest3;

internal class SwimToShoreObjective : MissionObjective
{
	private BlockedEstuaryMissionController _controller;

	public override string UniqueId => "naval_storyline_quest_3_reach_horses_objective";

	public override TextObject Name => new TextObject("{=h8HcPYjn}Swim to shore");

	public override TextObject Description => new TextObject("{=dBQj9VSX}Swim to shore and reach your horses.");

	internal SwimToShoreObjective(Mission mission, Agent gunnarAgent)
		: base(mission)
	{
		_controller = base.Mission.GetMissionBehavior<BlockedEstuaryMissionController>();
		foreach (Agent allAgent in base.Mission.AllAgents)
		{
			if (allAgent.IsActive() && allAgent.IsMount)
			{
				AddTarget(new AgentObjectiveTarget(allAgent));
			}
		}
		if (gunnarAgent != null && gunnarAgent.IsActive())
		{
			AddTarget(new AgentObjectiveTarget(gunnarAgent));
		}
	}

	protected override bool IsActivationRequirementsMet()
	{
		return _controller.CurrentPhase == BlockedEstuaryMissionController.BattlePhase.Phase2;
	}

	protected override bool IsCompletionRequirementsMet()
	{
		return _controller.CurrentPhase != BlockedEstuaryMissionController.BattlePhase.Phase2;
	}
}
