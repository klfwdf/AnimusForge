using TaleWorlds.MountAndBlade;

namespace NavalDLC.Storyline.MissionControllers;

public class BlockedEstuaryBattleEndLogic : NavalBattleEndLogic
{
	private BlockedEstuaryMissionController _controller;

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		_controller = base.Mission.GetMissionBehavior<BlockedEstuaryMissionController>();
	}

	public override void OnMissionTick(float dt)
	{
		if (_controller.CanEndBattleNatively)
		{
			base.OnMissionTick(dt);
		}
	}
}
