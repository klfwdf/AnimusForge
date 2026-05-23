using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics;

public class RetirementMissionLogic : MissionLogic
{
	public override void AfterStart()
	{
		base.AfterStart();
		SpawnHermit();
		_ = (LeaveMissionLogic)base.Mission.MissionLogics.FirstOrDefault((MissionLogic x) => x is LeaveMissionLogic);
	}

	private void SpawnHermit()
	{
		List<GameEntity> list = base.Mission.Scene.FindEntitiesWithTag("sp_hermit").ToList();
		MatrixFrame globalFrame = list[MBRandom.RandomInt(list.Count())].GetGlobalFrame();
		CharacterObject characterObject = Campaign.Current.ObjectManager.GetObject<CharacterObject>("sp_hermit");
		AgentBuildData agentBuildData = new AgentBuildData(characterObject).TroopOrigin(new SimpleAgentOrigin(characterObject)).Team(base.Mission.SpectatorTeam).InitialPosition(in globalFrame.origin)
			.InitialDirection(globalFrame.rotation.f.AsVec2.Normalized())
			.CivilianEquipment(civilianEquipment: true)
			.NoHorses(noHorses: true)
			.NoWeapons(noWeapons: true)
			.ClothingColor1(base.Mission.PlayerTeam.Color)
			.ClothingColor2(base.Mission.PlayerTeam.Color2);
		base.Mission.SpawnAgent(agentBuildData).SetMortalityState(Agent.MortalityState.Invulnerable);
	}
}
