using System;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics
{
	// Token: 0x0200007F RID: 127
	public class SandBoxBattleMissionSpawnHandler : SandBoxMissionSpawnHandler
	{
		// Token: 0x06000522 RID: 1314 RVA: 0x00022874 File Offset: 0x00020A74
		public override void AfterStart()
		{
			int numberOfInvolvedMen = this._mapEvent.GetNumberOfInvolvedMen(BattleSideEnum.Defender);
			int numberOfInvolvedMen2 = this._mapEvent.GetNumberOfInvolvedMen(BattleSideEnum.Attacker);
			int defenderInitialSpawn = numberOfInvolvedMen;
			int attackerInitialSpawn = numberOfInvolvedMen2;
			this._missionAgentSpawnLogic.SetSpawnHorses(BattleSideEnum.Defender, !this._mapEvent.IsSiegeAssault);
			this._missionAgentSpawnLogic.SetSpawnHorses(BattleSideEnum.Attacker, !this._mapEvent.IsSiegeAssault);
			MissionSpawnSettings missionSpawnSettings = SandBoxMissionSpawnHandler.CreateSandBoxBattleWaveSpawnSettings();
			this._missionAgentSpawnLogic.InitWithSinglePhase(numberOfInvolvedMen, numberOfInvolvedMen2, defenderInitialSpawn, attackerInitialSpawn, true, true, missionSpawnSettings);
		}
	}
}
