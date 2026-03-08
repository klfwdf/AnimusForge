using System;
using System.Collections.Generic;
using System.Linq;
using SandBox.Missions.MissionLogics.Arena;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.Issues.IssueQuestTasks
{
	// Token: 0x020000B7 RID: 183
	public class ArenaDuelQuestTask : QuestTaskBase
	{
		// Token: 0x06000797 RID: 1943 RVA: 0x00033A34 File Offset: 0x00031C34
		public ArenaDuelQuestTask(CharacterObject duelOpponentCharacter, Settlement settlement, Action onSucceededAction, Action onFailedAction, DialogFlow dialogFlow = null)
			: base(dialogFlow, onSucceededAction, onFailedAction, null)
		{
			this._opponentCharacter = duelOpponentCharacter;
			this._settlement = settlement;
		}

		// Token: 0x06000798 RID: 1944 RVA: 0x00033A50 File Offset: 0x00031C50
		public void AfterStart(IMission mission)
		{
			if (Mission.Current.HasMissionBehavior<ArenaDuelMissionBehavior>() && PlayerEncounter.LocationEncounter.Settlement == this._settlement)
			{
				this.InitializeTeams();
				List<MatrixFrame> list = (from e in Mission.Current.Scene.FindEntitiesWithTag("sp_arena_respawn")
					select e.GetGlobalFrame()).ToList<MatrixFrame>();
				MatrixFrame matrixFrame = list[MBRandom.RandomInt(list.Count)];
				float maxValue = float.MaxValue;
				MatrixFrame frame = matrixFrame;
				foreach (MatrixFrame matrixFrame2 in list)
				{
					if ((matrixFrame) != (matrixFrame2))
					{
						Vec3 origin = matrixFrame2.origin;
						if (origin.DistanceSquared(matrixFrame.origin) < maxValue)
						{
							frame = matrixFrame2;
						}
					}
				}
				matrixFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
				frame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
				this._playerAgent = this.SpawnArenaAgent(CharacterObject.PlayerCharacter, Mission.Current.PlayerTeam, matrixFrame);
				this._opponentAgent = this.SpawnArenaAgent(this._opponentCharacter, Mission.Current.PlayerEnemyTeam, frame);
			}
		}

		// Token: 0x06000799 RID: 1945 RVA: 0x00033B90 File Offset: 0x00031D90
		public override void SetReferences()
		{
			CampaignEvents.AfterMissionStarted.AddNonSerializedListener(this, new Action<IMission>(this.AfterStart));
			CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, new Action<MenuCallbackArgs>(this.OnGameMenuOpened));
			CampaignEvents.MissionTickEvent.AddNonSerializedListener(this, new Action<float>(this.MissionTick));
		}

		// Token: 0x0600079A RID: 1946 RVA: 0x00033BE2 File Offset: 0x00031DE2
		public void OnGameMenuOpened(MenuCallbackArgs args)
		{
			if (Hero.MainHero.CurrentSettlement == this._settlement)
			{
				if (this._duelStarted)
				{
					if (this._opponentAgent.IsActive())
					{
						base.Finish(QuestTaskBase.FinishStates.Fail);
						return;
					}
					base.Finish(QuestTaskBase.FinishStates.Success);
					return;
				}
				else
				{
					this.OpenArenaDuelMission();
				}
			}
		}

		// Token: 0x0600079B RID: 1947 RVA: 0x00033C24 File Offset: 0x00031E24
		public void MissionTick(float dt)
		{
			if (Mission.Current.HasMissionBehavior<ArenaDuelMissionBehavior>() && PlayerEncounter.LocationEncounter.Settlement == this._settlement && ((this._playerAgent != null && !this._playerAgent.IsActive()) || (this._opponentAgent != null && !this._opponentAgent.IsActive())))
			{
				if (this._missionEndTimer != null && this._missionEndTimer.ElapsedTime > 4f)
				{
					Mission.Current.EndMission();
					return;
				}
				if (this._missionEndTimer == null && ((this._playerAgent != null && !this._playerAgent.IsActive()) || (this._opponentAgent != null && !this._opponentAgent.IsActive())))
				{
					this._missionEndTimer = new BasicMissionTimer();
				}
			}
		}

		// Token: 0x0600079C RID: 1948 RVA: 0x00033CE4 File Offset: 0x00031EE4
		private void OpenArenaDuelMission()
		{
			Location locationWithId = this._settlement.LocationComplex.GetLocationWithId("arena");
			int upgradeLevel = (this._settlement.IsTown ? this._settlement.Town.GetWallLevel() : 1);
			SandBoxMissions.OpenArenaDuelMission(locationWithId.GetSceneName(upgradeLevel), locationWithId);
			this._duelStarted = true;
		}

		// Token: 0x0600079D RID: 1949 RVA: 0x00033D40 File Offset: 0x00031F40
		private void InitializeTeams()
		{
			Mission.Current.Teams.Add(BattleSideEnum.Defender, Hero.MainHero.MapFaction.Color, Hero.MainHero.MapFaction.Color2, null, true, false, true);
			Mission.Current.Teams.Add(BattleSideEnum.Attacker, Hero.MainHero.MapFaction.Color2, Hero.MainHero.MapFaction.Color, null, true, false, true);
			Mission.Current.PlayerTeam = Mission.Current.DefenderTeam;
		}

		// Token: 0x0600079E RID: 1950 RVA: 0x00033DC8 File Offset: 0x00031FC8
		private Agent SpawnArenaAgent(CharacterObject character, Team team, MatrixFrame frame)
		{
			if (team == Mission.Current.PlayerTeam)
			{
				character = CharacterObject.PlayerCharacter;
			}
			Equipment randomElement = this._settlement.Culture.DuelPresetEquipmentRoster.AllEquipments.GetRandomElement<Equipment>();
			Mission mission = Mission.Current;
			AgentBuildData agentBuildData = new AgentBuildData(character).Team(team).ClothingColor1(team.Color).ClothingColor2(team.Color2)
				.InitialPosition(frame.origin);
			Vec2 vec = frame.rotation.f.AsVec2;
			vec = vec.Normalized();
			Agent agent = mission.SpawnAgent(agentBuildData.InitialDirection(vec).NoHorses(true).Equipment(randomElement)
				.TroopOrigin(new SimpleAgentOrigin(character, -1, null, default(UniqueTroopDescriptor)))
				.Controller((character == CharacterObject.PlayerCharacter) ? AgentControllerType.Player : AgentControllerType.AI), false);
			if (agent.IsAIControlled)
			{
				agent.SetWatchState(Agent.WatchState.Alarmed);
			}
			return agent;
		}

		// Token: 0x04000407 RID: 1031
		private Settlement _settlement;

		// Token: 0x04000408 RID: 1032
		private CharacterObject _opponentCharacter;

		// Token: 0x04000409 RID: 1033
		private Agent _playerAgent;

		// Token: 0x0400040A RID: 1034
		private Agent _opponentAgent;

		// Token: 0x0400040B RID: 1035
		private bool _duelStarted;

		// Token: 0x0400040C RID: 1036
		private BasicMissionTimer _missionEndTimer;
	}
}
