using TaleWorlds.MountAndBlade;

namespace SandBox.AI;

public class UsablePlaceAI : UsableMachineAIBase
{
	public UsablePlaceAI(UsableMachine usableMachine)
		: base(usableMachine)
	{
	}

	protected override Agent.AIScriptedFrameFlags GetScriptedFrameFlags(Agent agent)
	{
		if (!UsableMachine.GameEntity.HasTag("quest_wanderer_target"))
		{
			return Agent.AIScriptedFrameFlags.DoNotRun;
		}
		return Agent.AIScriptedFrameFlags.None;
	}
}
