using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics
{
	// Token: 0x0200007E RID: 126
	public class RetirementMissionLogic : MissionLogic
	{
		// Token: 0x0600051F RID: 1311 RVA: 0x0002271C File Offset: 0x0002091C
		public override void AfterStart()
		{
			base.AfterStart();
			this.SpawnHermit();
			LeaveMissionLogic leaveMissionLogic = (LeaveMissionLogic)base.Mission.MissionLogics.FirstOrDefault((MissionLogic x) => x is LeaveMissionLogic);
		}

		// Token: 0x06000520 RID: 1312 RVA: 0x0002276C File Offset: 0x0002096C
		private void SpawnHermit()
		{
			List<GameEntity> list = base.Mission.Scene.FindEntitiesWithTag("sp_hermit").ToList<GameEntity>();
			MatrixFrame globalFrame = list[MBRandom.RandomInt(list.Count<GameEntity>())].GetGlobalFrame();
			CharacterObject @object = Campaign.Current.ObjectManager.GetObject<CharacterObject>("sp_hermit");
			AgentBuildData agentBuildData = new AgentBuildData(@object).TroopOrigin(new SimpleAgentOrigin(@object, -1, null, default(UniqueTroopDescriptor))).Team(base.Mission.SpectatorTeam).InitialPosition(globalFrame.origin);
			Vec2 vec = globalFrame.rotation.f.AsVec2;
			vec = vec.Normalized();
			AgentBuildData agentBuildData2 = agentBuildData.InitialDirection(vec).CivilianEquipment(true).NoHorses(true)
				.NoWeapons(true)
				.ClothingColor1(base.Mission.PlayerTeam.Color)
				.ClothingColor2(base.Mission.PlayerTeam.Color2);
			base.Mission.SpawnAgent(agentBuildData2, false).SetMortalityState(Agent.MortalityState.Invulnerable);
		}
	}
}
