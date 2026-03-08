using System;
using System.Linq;
using SandBox.Tournaments.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics.Arena
{
	// Token: 0x02000097 RID: 151
	public class ArenaDuelMissionController : MissionLogic
	{
		// Token: 0x06000639 RID: 1593 RVA: 0x0002A867 File Offset: 0x00028A67
		public ArenaDuelMissionController(CharacterObject duelCharacter, bool requireCivilianEquipment, bool spawnBothSideWithHorses, Action<CharacterObject> onDuelEnd, float customAgentHealth)
		{
			this._duelCharacter = duelCharacter;
			this._requireCivilianEquipment = requireCivilianEquipment;
			this._spawnBothSideWithHorses = spawnBothSideWithHorses;
			this._customAgentHealth = customAgentHealth;
			ArenaDuelMissionController._onDuelEnd = onDuelEnd;
		}

		// Token: 0x0600063A RID: 1594 RVA: 0x0002A894 File Offset: 0x00028A94
		public override void AfterStart()
		{
			this._duelHasEnded = false;
			this._duelEndTimer = new BasicMissionTimer();
			this.DeactivateOtherTournamentSets();
			this.InitializeMissionTeams();
			this._initialSpawnFrames = (from e in base.Mission.Scene.FindEntitiesWithTag("sp_arena")
				select e.GetGlobalFrame()).ToMBList<MatrixFrame>();
			for (int i = 0; i < this._initialSpawnFrames.Count; i++)
			{
				MatrixFrame value = this._initialSpawnFrames[i];
				value.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
				this._initialSpawnFrames[i] = value;
			}
			MatrixFrame randomElement = this._initialSpawnFrames.GetRandomElement<MatrixFrame>();
			this._initialSpawnFrames.Remove(randomElement);
			MatrixFrame randomElement2 = this._initialSpawnFrames.GetRandomElement<MatrixFrame>();
			this.SpawnAgent(CharacterObject.PlayerCharacter, randomElement);
			this._duelAgent = this.SpawnAgent(this._duelCharacter, randomElement2);
			this._duelAgent.Defensiveness = 1f;
		}

		// Token: 0x0600063B RID: 1595 RVA: 0x0002A994 File Offset: 0x00028B94
		private void InitializeMissionTeams()
		{
			base.Mission.Teams.Add(BattleSideEnum.Defender, Hero.MainHero.MapFaction.Color, Hero.MainHero.MapFaction.Color2, null, true, false, true);
			base.Mission.Teams.Add(BattleSideEnum.Attacker, this._duelCharacter.Culture.Color, this._duelCharacter.Culture.Color2, null, true, false, true);
			base.Mission.PlayerTeam = base.Mission.Teams.Defender;
		}

		// Token: 0x0600063C RID: 1596 RVA: 0x0002AA26 File Offset: 0x00028C26
		private void DeactivateOtherTournamentSets()
		{
			TournamentBehavior.DeleteTournamentSetsExcept(base.Mission.Scene.FindEntityWithTag("tournament_fight"));
		}

		// Token: 0x0600063D RID: 1597 RVA: 0x0002AA44 File Offset: 0x00028C44
		private Agent SpawnAgent(CharacterObject character, MatrixFrame spawnFrame)
		{
			AgentBuildData agentBuildData = new AgentBuildData(character);
			agentBuildData.BodyProperties(character.GetBodyPropertiesMax(false));
			Mission mission = base.Mission;
			AgentBuildData agentBuildData2 = agentBuildData.Team((character == CharacterObject.PlayerCharacter) ? base.Mission.PlayerTeam : base.Mission.PlayerEnemyTeam).InitialPosition(spawnFrame.origin);
			Vec2 vec = spawnFrame.rotation.f.AsVec2;
			vec = vec.Normalized();
			Agent agent = mission.SpawnAgent(agentBuildData2.InitialDirection(vec).NoHorses(!this._spawnBothSideWithHorses).Equipment(this._requireCivilianEquipment ? character.FirstCivilianEquipment : character.FirstBattleEquipment)
				.TroopOrigin(new SimpleAgentOrigin(character, -1, null, default(UniqueTroopDescriptor))), false);
			agent.FadeIn();
			if (character == CharacterObject.PlayerCharacter)
			{
				agent.Controller = AgentControllerType.Player;
			}
			if (agent.IsAIControlled)
			{
				agent.SetWatchState(Agent.WatchState.Alarmed);
			}
			agent.Health = this._customAgentHealth;
			agent.BaseHealthLimit = this._customAgentHealth;
			agent.HealthLimit = this._customAgentHealth;
			return agent;
		}

		// Token: 0x0600063E RID: 1598 RVA: 0x0002AB50 File Offset: 0x00028D50
		public override void OnMissionTick(float dt)
		{
			if (this._duelHasEnded && this._duelEndTimer.ElapsedTime > 4f)
			{
				GameTexts.SetVariable("leave_key", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("Generic", 4), 1f));
				MBInformationManager.AddQuickInformation(GameTexts.FindText("str_duel_has_ended", null), 0, null, null, "");
				this._duelEndTimer.Reset();
			}
		}

		// Token: 0x0600063F RID: 1599 RVA: 0x0002ABBC File Offset: 0x00028DBC
		public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
		{
			if (ArenaDuelMissionController._onDuelEnd != null)
			{
				ArenaDuelMissionController._onDuelEnd((affectedAgent == this._duelAgent) ? CharacterObject.PlayerCharacter : this._duelCharacter);
				ArenaDuelMissionController._onDuelEnd = null;
				this._duelHasEnded = true;
				this._duelEndTimer.Reset();
			}
		}

		// Token: 0x06000640 RID: 1600 RVA: 0x0002AC08 File Offset: 0x00028E08
		public override InquiryData OnEndMissionRequest(out bool canPlayerLeave)
		{
			canPlayerLeave = true;
			if (!this._duelHasEnded)
			{
				canPlayerLeave = false;
				MBInformationManager.AddQuickInformation(GameTexts.FindText("str_can_not_retreat_duel_ongoing", null), 0, null, null, "");
			}
			return null;
		}

		// Token: 0x04000355 RID: 853
		private CharacterObject _duelCharacter;

		// Token: 0x04000356 RID: 854
		private bool _requireCivilianEquipment;

		// Token: 0x04000357 RID: 855
		private bool _spawnBothSideWithHorses;

		// Token: 0x04000358 RID: 856
		private bool _duelHasEnded;

		// Token: 0x04000359 RID: 857
		private Agent _duelAgent;

		// Token: 0x0400035A RID: 858
		private float _customAgentHealth;

		// Token: 0x0400035B RID: 859
		private BasicMissionTimer _duelEndTimer;

		// Token: 0x0400035C RID: 860
		private MBList<MatrixFrame> _initialSpawnFrames;

		// Token: 0x0400035D RID: 861
		private static Action<CharacterObject> _onDuelEnd;
	}
}
