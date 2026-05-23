using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Source.Missions;

public class SimpleMountedPlayerMissionController : MissionLogic
{
	private const string TestPlayerSpawnPoint = "spawnpoint_player_test";

	private const string PlayerSpawnPoint = "spawnpoint_player";

	private readonly Game _game = Game.Current;

	public override void EarlyStart()
	{
		base.EarlyStart();
		foreach (MissionObject item in base.Mission.ActiveMissionObjects.ToList())
		{
			item.SetDisabled(isParentObject: true);
		}
	}

	public override void AfterStart()
	{
		BasicCharacterObject troop = _game.ObjectManager.GetObject<BasicCharacterObject>("aserai_tribal_horseman");
		WeakGameEntity weakGameEntity = Mission.Current.Scene.FindWeakEntityWithTag("spawnpoint_player_test");
		if (!weakGameEntity.IsValid)
		{
			weakGameEntity = Mission.Current.Scene.FindWeakEntityWithTag("spawnpoint_player");
		}
		MatrixFrame matrixFrame = (weakGameEntity.IsValid ? weakGameEntity.GetGlobalFrame() : MatrixFrame.Identity);
		AgentBuildData agentBuildData = new AgentBuildData(new BasicBattleAgentOrigin(troop));
		agentBuildData.InitialPosition(in matrixFrame.origin).InitialDirection(matrixFrame.rotation.f.AsVec2.Normalized()).Controller(AgentControllerType.Player);
		base.Mission.SpawnAgent(agentBuildData).WieldInitialWeapons();
	}

	public override bool MissionEnded(ref MissionResult missionResult)
	{
		return base.Mission.InputManager.IsGameKeyPressed(4);
	}
}
