using System;
using System.Collections.Generic;
using System.Linq;
using SandBox.Missions.MissionLogics;
using SandBox.Tournaments.AgentControllers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.Tournaments.MissionLogics
{
	// Token: 0x0200002C RID: 44
	public class TournamentArcheryMissionController : MissionLogic, ITournamentGameBehavior
	{
		// Token: 0x1700000F RID: 15
		// (get) Token: 0x06000148 RID: 328 RVA: 0x000082A4 File Offset: 0x000064A4
		public IEnumerable<ArcheryTournamentAgentController> AgentControllers
		{
			get
			{
				return this._agentControllers;
			}
		}

		// Token: 0x06000149 RID: 329 RVA: 0x000082AC File Offset: 0x000064AC
		public TournamentArcheryMissionController(CultureObject culture)
		{
			this._culture = culture;
			this.ShootingPositions = new List<GameEntity>();
			this._agentControllers = new List<ArcheryTournamentAgentController>();
			this._archeryEquipment = new Equipment();
			this._archeryEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.WeaponItemBeginSlot, new EquipmentElement(Game.Current.ObjectManager.GetObject<ItemObject>("nordic_shortbow"), null, null, false));
			this._archeryEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Weapon1, new EquipmentElement(Game.Current.ObjectManager.GetObject<ItemObject>("blunt_arrows"), null, null, false));
			this._archeryEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Body, new EquipmentElement(Game.Current.ObjectManager.GetObject<ItemObject>("desert_lamellar"), null, null, false));
			this._archeryEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Gloves, new EquipmentElement(Game.Current.ObjectManager.GetObject<ItemObject>("reinforced_mail_mitten"), null, null, false));
			this._archeryEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Leg, new EquipmentElement(Game.Current.ObjectManager.GetObject<ItemObject>("leather_cavalier_boots"), null, null, false));
		}

		// Token: 0x0600014A RID: 330 RVA: 0x000083B0 File Offset: 0x000065B0
		public override void AfterStart()
		{
			TournamentBehavior.DeleteTournamentSetsExcept(base.Mission.Scene.FindEntityWithTag("tournament_archery"));
			this._spawnPoints = base.Mission.Scene.FindEntitiesWithTag("sp_arena").ToList<GameEntity>();
			base.Mission.SetMissionMode(MissionMode.Battle, true);
			this._targets = (from x in base.Mission.ActiveMissionObjects.FindAllWithType<DestructableComponent>()
				where x.GameEntity.HasTag("archery_target")
				select x).ToList<DestructableComponent>();
			foreach (DestructableComponent destructableComponent in this._targets)
			{
				destructableComponent.OnDestroyed += new DestructableComponent.OnHitTakenAndDestroyedDelegate(this.OnTargetDestroyed);
			}
		}

		// Token: 0x0600014B RID: 331 RVA: 0x00008494 File Offset: 0x00006694
		public void StartMatch(TournamentMatch match, bool isLastRound)
		{
			this._match = match;
			this.ResetTargets();
			int count = this._spawnPoints.Count;
			int num = 0;
			int num2 = 0;
			foreach (TournamentTeam tournamentTeam in this._match.Teams)
			{
				Team team = base.Mission.Teams.Add(BattleSideEnum.None, MissionAgentHandler.GetRandomTournamentTeamColor(num2), uint.MaxValue, null, true, false, true);
				foreach (TournamentParticipant tournamentParticipant in tournamentTeam.Participants)
				{
					tournamentParticipant.MatchEquipment = this._archeryEquipment.Clone(false);
					MatrixFrame globalFrame = this._spawnPoints[num % count].GetGlobalFrame();
					globalFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
					this.SetItemsAndSpawnCharacter(tournamentParticipant, team, globalFrame);
					num++;
				}
				num2++;
			}
		}

		// Token: 0x0600014C RID: 332 RVA: 0x000085A4 File Offset: 0x000067A4
		public void SkipMatch(TournamentMatch match)
		{
			this._match = match;
			this.Simulate();
		}

		// Token: 0x0600014D RID: 333 RVA: 0x000085B4 File Offset: 0x000067B4
		private void Simulate()
		{
			this._isSimulated = false;
			List<TournamentParticipant> list = this._match.Participants.ToList<TournamentParticipant>();
			int i = this._targets.Count;
			while (i > 0)
			{
				foreach (TournamentParticipant tournamentParticipant in list)
				{
					if (i == 0)
					{
						break;
					}
					if (MBRandom.RandomFloat < this.GetDeadliness(tournamentParticipant))
					{
						tournamentParticipant.AddScore(1);
						i--;
					}
				}
			}
			this._isSimulated = true;
		}

		// Token: 0x0600014E RID: 334 RVA: 0x0000864C File Offset: 0x0000684C
		public bool IsMatchEnded()
		{
			if (this._isSimulated || this._match == null)
			{
				return true;
			}
			if (this._endTimer != null && this._endTimer.ElapsedTime > 6f)
			{
				this._endTimer = null;
				return true;
			}
			if (this._endTimer == null && (!this.IsThereAnyTargetLeft() || !this.IsThereAnyArrowLeft()))
			{
				this._endTimer = new BasicMissionTimer();
			}
			return false;
		}

		// Token: 0x0600014F RID: 335 RVA: 0x000086B4 File Offset: 0x000068B4
		public void OnMatchEnded()
		{
			SandBoxHelpers.MissionHelper.FadeOutAgents(base.Mission.Agents, true, false);
			base.Mission.ClearCorpses(false);
			base.Mission.Teams.Clear();
			base.Mission.RemoveSpawnedItemsAndMissiles();
			this._match = null;
			this._endTimer = null;
			this._isSimulated = false;
		}

		// Token: 0x06000150 RID: 336 RVA: 0x00008710 File Offset: 0x00006910
		private void ResetTargets()
		{
			foreach (DestructableComponent destructableComponent in this._targets)
			{
				destructableComponent.Reset();
			}
		}

		// Token: 0x06000151 RID: 337 RVA: 0x00008760 File Offset: 0x00006960
		private void SetItemsAndSpawnCharacter(TournamentParticipant participant, Team team, MatrixFrame frame)
		{
			AgentBuildData agentBuildData = new AgentBuildData(new SimpleAgentOrigin(participant.Character, -1, null, participant.Descriptor)).Team(team).Equipment(participant.MatchEquipment).InitialPosition(frame.origin);
			Vec2 vec = frame.rotation.f.AsVec2;
			vec = vec.Normalized();
			AgentBuildData agentBuildData2 = agentBuildData.InitialDirection(vec).Controller(participant.Character.IsPlayerCharacter ? AgentControllerType.Player : AgentControllerType.AI);
			Agent agent = base.Mission.SpawnAgent(agentBuildData2, false);
			agent.Health = agent.HealthLimit;
			ArcheryTournamentAgentController archeryTournamentAgentController = agent.AddController(typeof(ArcheryTournamentAgentController)) as ArcheryTournamentAgentController;
			archeryTournamentAgentController.SetTargets(this._targets);
			this._agentControllers.Add(archeryTournamentAgentController);
			if (participant.Character.IsPlayerCharacter)
			{
				agent.WieldInitialWeapons(Agent.WeaponWieldActionType.InstantAfterPickUp, Equipment.InitialWeaponEquipPreference.Any);
				base.Mission.PlayerTeam = team;
				return;
			}
			agent.SetWatchState(Agent.WatchState.Alarmed);
		}

		// Token: 0x06000152 RID: 338 RVA: 0x0000884C File Offset: 0x00006A4C
		public void OnTargetDestroyed(DestructableComponent destroyedComponent, Agent destroyerAgent, in MissionWeapon attackerWeapon, ScriptComponentBehavior attackerScriptComponentBehavior, int inflictedDamage)
		{
			foreach (ArcheryTournamentAgentController archeryTournamentAgentController in this.AgentControllers)
			{
				archeryTournamentAgentController.OnTargetHit(destroyerAgent, destroyedComponent);
				this._match.GetParticipant(destroyerAgent.Origin.UniqueSeed).AddScore(1);
			}
		}

		// Token: 0x06000153 RID: 339 RVA: 0x000088B8 File Offset: 0x00006AB8
		public override void OnMissionTick(float dt)
		{
			base.OnMissionTick(dt);
			if (!this.IsMatchEnded())
			{
				foreach (Agent agent in base.Mission.Agents)
				{
					ArcheryTournamentAgentController controller = agent.GetController<ArcheryTournamentAgentController>();
					if (controller != null)
					{
						controller.OnTick();
					}
				}
			}
		}

		// Token: 0x06000154 RID: 340 RVA: 0x00008928 File Offset: 0x00006B28
		public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon attackerWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
		{
			base.Mission.EndMission();
		}

		// Token: 0x06000155 RID: 341 RVA: 0x00008935 File Offset: 0x00006B35
		private bool IsThereAnyTargetLeft()
		{
			return this._targets.Any((DestructableComponent e) => !e.IsDestroyed);
		}

		// Token: 0x06000156 RID: 342 RVA: 0x00008961 File Offset: 0x00006B61
		private bool IsThereAnyArrowLeft()
		{
			return base.Mission.Agents.Any((Agent agent) => agent.Equipment.GetAmmoAmount(EquipmentIndex.WeaponItemBeginSlot) > 0);
		}

		// Token: 0x06000157 RID: 343 RVA: 0x00008992 File Offset: 0x00006B92
		private float GetDeadliness(TournamentParticipant participant)
		{
			return 0.01f + (float)participant.Character.GetSkillValue(DefaultSkills.Bow) / 300f * 0.19f;
		}

		// Token: 0x0400005D RID: 93
		private readonly List<ArcheryTournamentAgentController> _agentControllers;

		// Token: 0x0400005E RID: 94
		private TournamentMatch _match;

		// Token: 0x0400005F RID: 95
		private BasicMissionTimer _endTimer;

		// Token: 0x04000060 RID: 96
		private List<GameEntity> _spawnPoints;

		// Token: 0x04000061 RID: 97
		private bool _isSimulated;

		// Token: 0x04000062 RID: 98
		private CultureObject _culture;

		// Token: 0x04000063 RID: 99
		private List<DestructableComponent> _targets;

		// Token: 0x04000064 RID: 100
		public List<GameEntity> ShootingPositions;

		// Token: 0x04000065 RID: 101
		private readonly Equipment _archeryEquipment;
	}
}
