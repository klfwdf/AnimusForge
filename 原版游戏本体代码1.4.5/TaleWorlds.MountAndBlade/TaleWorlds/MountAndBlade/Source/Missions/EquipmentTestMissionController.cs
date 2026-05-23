using TaleWorlds.Core;
using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade.Source.Missions;

public class EquipmentTestMissionController : MissionLogic
{
	public override void AfterStart()
	{
		base.AfterStart();
		WeakGameEntity entity = base.Mission.Scene.FindWeakEntityWithTag("spawnpoint_player");
		base.Mission.SpawnAgent(new AgentBuildData(Game.Current.PlayerTroop).Team(base.Mission.AttackerTeam).InitialFrameFromSpawnPointEntity(entity).CivilianEquipment(civilianEquipment: false)
			.Controller(AgentControllerType.Player));
	}
}
