using System;
using System.Collections.Generic;
using SandBox.Objects.Cinematics;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics.Hideout
{
	// Token: 0x02000091 RID: 145
	public class HideoutAmbushBossFightCinematicController : MissionLogic
	{
		// Token: 0x1400000C RID: 12
		// (add) Token: 0x060005AB RID: 1451 RVA: 0x00025B28 File Offset: 0x00023D28
		// (remove) Token: 0x060005AC RID: 1452 RVA: 0x00025B60 File Offset: 0x00023D60
		public event Action OnCinematicFinished;

		// Token: 0x1400000D RID: 13
		// (add) Token: 0x060005AD RID: 1453 RVA: 0x00025B98 File Offset: 0x00023D98
		// (remove) Token: 0x060005AE RID: 1454 RVA: 0x00025BD0 File Offset: 0x00023DD0
		public event Action<HideoutAmbushBossFightCinematicController.HideoutCinematicState> OnCinematicStateChanged;

		// Token: 0x1400000E RID: 14
		// (add) Token: 0x060005AF RID: 1455 RVA: 0x00025C08 File Offset: 0x00023E08
		// (remove) Token: 0x060005B0 RID: 1456 RVA: 0x00025C40 File Offset: 0x00023E40
		public event Action<HideoutAmbushBossFightCinematicController.HideoutCinematicState, float> OnCinematicTransition;

		// Token: 0x17000077 RID: 119
		// (get) Token: 0x060005B1 RID: 1457 RVA: 0x00025C75 File Offset: 0x00023E75
		// (set) Token: 0x060005B2 RID: 1458 RVA: 0x00025C7D File Offset: 0x00023E7D
		public HideoutAmbushBossFightCinematicController.HideoutCinematicState State { get; private set; }

		// Token: 0x17000078 RID: 120
		// (get) Token: 0x060005B3 RID: 1459 RVA: 0x00025C86 File Offset: 0x00023E86
		// (set) Token: 0x060005B4 RID: 1460 RVA: 0x00025C8E File Offset: 0x00023E8E
		public bool InStateTransition { get; private set; }

		// Token: 0x17000079 RID: 121
		// (get) Token: 0x060005B5 RID: 1461 RVA: 0x00025C97 File Offset: 0x00023E97
		public bool IsCinematicActive
		{
			get
			{
				return this.State > HideoutAmbushBossFightCinematicController.HideoutCinematicState.None;
			}
		}

		// Token: 0x1700007A RID: 122
		// (get) Token: 0x060005B6 RID: 1462 RVA: 0x00025CA2 File Offset: 0x00023EA2
		public float CinematicDuration
		{
			get
			{
				return this._cinematicDuration;
			}
		}

		// Token: 0x1700007B RID: 123
		// (get) Token: 0x060005B7 RID: 1463 RVA: 0x00025CAA File Offset: 0x00023EAA
		public float TransitionDuration
		{
			get
			{
				return this._transitionDuration;
			}
		}

		// Token: 0x1700007C RID: 124
		// (get) Token: 0x060005B8 RID: 1464 RVA: 0x00025CB2 File Offset: 0x00023EB2
		public override MissionBehaviorType BehaviorType
		{
			get
			{
				return MissionBehaviorType.Logic;
			}
		}

		// Token: 0x060005B9 RID: 1465 RVA: 0x00025CB8 File Offset: 0x00023EB8
		public HideoutAmbushBossFightCinematicController()
		{
			this.State = HideoutAmbushBossFightCinematicController.HideoutCinematicState.None;
			this.InStateTransition = false;
			this._isBehaviorInit = false;
		}

		// Token: 0x060005BA RID: 1466 RVA: 0x00025D24 File Offset: 0x00023F24
		public void StartCinematic(HideoutAmbushBossFightCinematicController.OnInitialFadeOutFinished initialFadeOutFinished, Action cinematicFinishedCallback, float transitionDuration = 0.4f, float stateDuration = 0.2f, float cinematicDuration = 8f, bool forceDismountAgents = false)
		{
			if (this._isBehaviorInit && this.State == HideoutAmbushBossFightCinematicController.HideoutCinematicState.None)
			{
				this.OnCinematicFinished += cinematicFinishedCallback;
				this._initialFadeOutFinished = initialFadeOutFinished;
				this._preCinematicPhase = HideoutAmbushBossFightCinematicController.HideoutPreCinematicPhase.InitializeFormations;
				this._postCinematicPhase = HideoutAmbushBossFightCinematicController.HideoutPostCinematicPhase.MoveAgents;
				this._transitionDuration = transitionDuration;
				this._stateDuration = stateDuration;
				this._cinematicDuration = cinematicDuration;
				this._remainingCinematicDuration = this._cinematicDuration;
				this.BeginStateTransition(HideoutAmbushBossFightCinematicController.HideoutCinematicState.InitialFadeOut);
				return;
			}
			if (!this._isBehaviorInit)
			{
				Debug.FailedAssert("Hideout cinematic controller is not initialized.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\Missions\\MissionLogics\\Hideout\\HideoutAmbushBossFightCinematicController.cs", "StartCinematic", 180);
				return;
			}
			if (this.State != HideoutAmbushBossFightCinematicController.HideoutCinematicState.None)
			{
				Debug.FailedAssert("There is already an ongoing cinematic.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\Missions\\MissionLogics\\Hideout\\HideoutAmbushBossFightCinematicController.cs", "StartCinematic", 184);
			}
		}

		// Token: 0x060005BB RID: 1467 RVA: 0x00025DCC File Offset: 0x00023FCC
		public void GetBossStandingEyePosition(out Vec3 eyePosition)
		{
			Agent agent = this._bossAgentInfo.Agent;
			if (((agent != null) ? agent.Monster : null) != null)
			{
				eyePosition = this._bossAgentInfo.InitialFrame.origin + Vec3.Up * (this._bossAgentInfo.Agent.AgentScale * this._bossAgentInfo.Agent.Monster.StandingEyeHeight);
				return;
			}
			eyePosition = Vec3.Zero;
			Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\Missions\\MissionLogics\\Hideout\\HideoutAmbushBossFightCinematicController.cs", "GetBossStandingEyePosition", 197);
		}

		// Token: 0x060005BC RID: 1468 RVA: 0x00025E64 File Offset: 0x00024064
		public void GetPlayerStandingEyePosition(out Vec3 eyePosition)
		{
			Agent agent = this._playerAgentInfo.Agent;
			if (((agent != null) ? agent.Monster : null) != null)
			{
				eyePosition = this._playerAgentInfo.InitialFrame.origin + Vec3.Up * (this._playerAgentInfo.Agent.AgentScale * this._playerAgentInfo.Agent.Monster.StandingEyeHeight);
				return;
			}
			eyePosition = Vec3.Zero;
			Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\Missions\\MissionLogics\\Hideout\\HideoutAmbushBossFightCinematicController.cs", "GetPlayerStandingEyePosition", 210);
		}

		// Token: 0x060005BD RID: 1469 RVA: 0x00025EFC File Offset: 0x000240FC
		public MatrixFrame GetBanditsInitialFrame()
		{
			MatrixFrame result;
			this._hideoutBossFightBehavior.GetBanditsInitialFrame(out result);
			return result;
		}

		// Token: 0x060005BE RID: 1470 RVA: 0x00025F18 File Offset: 0x00024118
		public void GetScenePrefabParameters(out float innerRadius, out float outerRadius, out float walkDistance)
		{
			innerRadius = 0f;
			outerRadius = 0f;
			walkDistance = 0f;
			if (this._hideoutBossFightBehavior != null)
			{
				innerRadius = this._hideoutBossFightBehavior.InnerRadius;
				outerRadius = this._hideoutBossFightBehavior.OuterRadius;
				walkDistance = this._hideoutBossFightBehavior.WalkDistance;
			}
		}

		// Token: 0x060005BF RID: 1471 RVA: 0x00025F6C File Offset: 0x0002416C
		public override void OnBehaviorInitialize()
		{
			base.OnBehaviorInitialize();
			GameEntity gameEntity = base.Mission.Scene.FindEntityWithTag("hideout_boss_fight");
			this._hideoutBossFightBehavior = ((gameEntity != null) ? gameEntity.GetFirstScriptOfType<HideoutBossFightBehavior>() : null);
			this._isBehaviorInit = gameEntity != null && this._hideoutBossFightBehavior != null;
		}

		// Token: 0x060005C0 RID: 1472 RVA: 0x00025FC4 File Offset: 0x000241C4
		public override void OnMissionTick(float dt)
		{
			if (this._isBehaviorInit && this.IsCinematicActive)
			{
				if (this.InStateTransition)
				{
					this.TickStateTransition(dt);
					return;
				}
				switch (this.State)
				{
				case HideoutAmbushBossFightCinematicController.HideoutCinematicState.InitialFadeOut:
					if (this.TickInitialFadeOut(dt))
					{
						this.BeginStateTransition(HideoutAmbushBossFightCinematicController.HideoutCinematicState.PreCinematic);
						return;
					}
					break;
				case HideoutAmbushBossFightCinematicController.HideoutCinematicState.PreCinematic:
					if (this.TickPreCinematic(dt))
					{
						this.BeginStateTransition(HideoutAmbushBossFightCinematicController.HideoutCinematicState.Cinematic);
						return;
					}
					break;
				case HideoutAmbushBossFightCinematicController.HideoutCinematicState.Cinematic:
					if (this.TickCinematic(dt))
					{
						this.BeginStateTransition(HideoutAmbushBossFightCinematicController.HideoutCinematicState.PostCinematic);
						return;
					}
					break;
				case HideoutAmbushBossFightCinematicController.HideoutCinematicState.PostCinematic:
					if (this.TickPostCinematic(dt))
					{
						this.BeginStateTransition(HideoutAmbushBossFightCinematicController.HideoutCinematicState.Completed);
						return;
					}
					break;
				case HideoutAmbushBossFightCinematicController.HideoutCinematicState.Completed:
				{
					Action onCinematicFinished = this.OnCinematicFinished;
					if (onCinematicFinished != null)
					{
						onCinematicFinished();
					}
					this.OnCinematicFinished = null;
					this.OnCinematicStateChanged = null;
					this.OnCinematicTransition = null;
					this.State = HideoutAmbushBossFightCinematicController.HideoutCinematicState.None;
					break;
				}
				default:
					return;
				}
			}
		}

		// Token: 0x060005C1 RID: 1473 RVA: 0x0002608C File Offset: 0x0002428C
		private void TickStateTransition(float dt)
		{
			this._remainingTransitionDuration -= dt;
			if (this._remainingTransitionDuration <= 0f)
			{
				this.InStateTransition = false;
				Action<HideoutAmbushBossFightCinematicController.HideoutCinematicState> onCinematicStateChanged = this.OnCinematicStateChanged;
				if (onCinematicStateChanged != null)
				{
					onCinematicStateChanged(this.State);
				}
				this._remainingStateDuration = this._stateDuration;
			}
		}

		// Token: 0x060005C2 RID: 1474 RVA: 0x000260E0 File Offset: 0x000242E0
		private bool TickInitialFadeOut(float dt)
		{
			this._remainingStateDuration -= dt;
			if (this._remainingStateDuration <= 0f)
			{
				Agent playerAgent = null;
				Agent bossAgent = null;
				List<Agent> playerCompanions = null;
				List<Agent> bossCompanions = null;
				float placementPerturbation = 0.25f;
				float placementAngle = 0.20943952f;
				HideoutAmbushBossFightCinematicController.OnInitialFadeOutFinished initialFadeOutFinished = this._initialFadeOutFinished;
				if (initialFadeOutFinished != null)
				{
					initialFadeOutFinished(ref playerAgent, ref playerCompanions, ref bossAgent, ref bossCompanions, ref placementPerturbation, ref placementAngle);
				}
				this.ComputeAgentFrames(playerAgent, playerCompanions, bossAgent, bossCompanions, placementPerturbation, placementAngle);
			}
			return this._remainingStateDuration <= 0f;
		}

		// Token: 0x060005C3 RID: 1475 RVA: 0x0002615C File Offset: 0x0002435C
		private bool TickPreCinematic(float dt)
		{
			Scene scene = base.Mission.Scene;
			this._remainingStateDuration -= dt;
			switch (this._preCinematicPhase)
			{
			case HideoutAmbushBossFightCinematicController.HideoutPreCinematicPhase.InitializeFormations:
			{
				this._playerAgentInfo.Agent.Controller = AgentControllerType.AI;
				bool isTeleportingAgents = base.Mission.IsTeleportingAgents;
				base.Mission.IsTeleportingAgents = true;
				MatrixFrame matrixFrame;
				this._hideoutBossFightBehavior.GetAlliesInitialFrame(out matrixFrame);
				foreach (Formation formation in base.Mission.Teams.Attacker.FormationsIncludingEmpty)
				{
					if (formation.CountOfUnits > 0)
					{
						WorldPosition position = new WorldPosition(scene, matrixFrame.origin);
						formation.SetMovementOrder(MovementOrder.MovementOrderMove(position));
					}
				}
				MatrixFrame matrixFrame2;
				this._hideoutBossFightBehavior.GetBanditsInitialFrame(out matrixFrame2);
				foreach (Formation formation2 in base.Mission.Teams.Defender.FormationsIncludingEmpty)
				{
					if (formation2.CountOfUnits > 0)
					{
						WorldPosition position2 = new WorldPosition(scene, matrixFrame2.origin);
						formation2.SetMovementOrder(MovementOrder.MovementOrderMove(position2));
					}
				}
				foreach (HideoutAmbushBossFightCinematicController.HideoutCinematicAgentInfo hideoutCinematicAgentInfo in this._hideoutAgentsInfo)
				{
					Agent agent = hideoutCinematicAgentInfo.Agent;
					Vec3 f = hideoutCinematicAgentInfo.InitialFrame.rotation.f;
					agent.LookDirection = f;
					Agent agent2 = agent;
					Vec2 vec = f.AsVec2;
					vec = vec.Normalized();
					agent2.SetMovementDirection(vec);
				}
				base.Mission.IsTeleportingAgents = isTeleportingAgents;
				this._preCinematicPhase = HideoutAmbushBossFightCinematicController.HideoutPreCinematicPhase.StopFormations;
				break;
			}
			case HideoutAmbushBossFightCinematicController.HideoutPreCinematicPhase.StopFormations:
				foreach (Formation formation3 in base.Mission.Teams.Attacker.FormationsIncludingEmpty)
				{
					if (formation3.CountOfUnits > 0)
					{
						formation3.SetMovementOrder(MovementOrder.MovementOrderStop);
					}
				}
				foreach (Formation formation4 in base.Mission.Teams.Defender.FormationsIncludingEmpty)
				{
					if (formation4.CountOfUnits > 0)
					{
						formation4.SetMovementOrder(MovementOrder.MovementOrderStop);
					}
				}
				this._preCinematicPhase = HideoutAmbushBossFightCinematicController.HideoutPreCinematicPhase.InitializeAgents;
				break;
			case HideoutAmbushBossFightCinematicController.HideoutPreCinematicPhase.InitializeAgents:
			{
				bool isTeleportingAgents2 = base.Mission.IsTeleportingAgents;
				base.Mission.IsTeleportingAgents = true;
				this._cachedAgentFormations = new List<Formation>();
				foreach (HideoutAmbushBossFightCinematicController.HideoutCinematicAgentInfo hideoutCinematicAgentInfo2 in this._hideoutAgentsInfo)
				{
					Agent agent3 = hideoutCinematicAgentInfo2.Agent;
					this._cachedAgentFormations.Add(agent3.Formation);
					agent3.Formation = null;
					MatrixFrame initialFrame = hideoutCinematicAgentInfo2.InitialFrame;
					WorldPosition worldPosition = new WorldPosition(scene, initialFrame.origin);
					Vec3 f2 = initialFrame.rotation.f;
					agent3.TeleportToPosition(worldPosition.GetGroundVec3());
					agent3.LookDirection = f2;
					Agent agent4 = agent3;
					Vec2 vec = f2.AsVec2;
					vec = vec.Normalized();
					agent4.SetMovementDirection(vec);
				}
				base.Mission.IsTeleportingAgents = isTeleportingAgents2;
				this._preCinematicPhase = HideoutAmbushBossFightCinematicController.HideoutPreCinematicPhase.MoveAgents;
				break;
			}
			case HideoutAmbushBossFightCinematicController.HideoutPreCinematicPhase.MoveAgents:
				foreach (HideoutAmbushBossFightCinematicController.HideoutCinematicAgentInfo hideoutCinematicAgentInfo3 in this._hideoutAgentsInfo)
				{
					Agent agent5 = hideoutCinematicAgentInfo3.Agent;
					MatrixFrame targetFrame = hideoutCinematicAgentInfo3.TargetFrame;
					WorldPosition worldPosition2 = new WorldPosition(scene, targetFrame.origin);
					agent5.SetMaximumSpeedLimit(0.65f, false);
					Agent agent6 = agent5;
					Vec2 vec = targetFrame.rotation.f.AsVec2;
					agent6.SetScriptedPositionAndDirection(ref worldPosition2, vec.RotationInRadians, true, Agent.AIScriptedFrameFlags.None);
				}
				this._preCinematicPhase = HideoutAmbushBossFightCinematicController.HideoutPreCinematicPhase.Completed;
				break;
			}
			return this._preCinematicPhase == HideoutAmbushBossFightCinematicController.HideoutPreCinematicPhase.Completed && this._remainingStateDuration <= 0f;
		}

		// Token: 0x060005C4 RID: 1476 RVA: 0x000265D4 File Offset: 0x000247D4
		private bool TickCinematic(float dt)
		{
			this._remainingCinematicDuration -= dt;
			this._remainingStateDuration -= dt;
			return this._remainingCinematicDuration <= 0f && this._remainingStateDuration <= 0f;
		}

		// Token: 0x060005C5 RID: 1477 RVA: 0x00026610 File Offset: 0x00024810
		private bool TickPostCinematic(float dt)
		{
			this._remainingStateDuration -= dt;
			HideoutAmbushBossFightCinematicController.HideoutPostCinematicPhase postCinematicPhase = this._postCinematicPhase;
			if (postCinematicPhase != HideoutAmbushBossFightCinematicController.HideoutPostCinematicPhase.MoveAgents)
			{
				if (postCinematicPhase == HideoutAmbushBossFightCinematicController.HideoutPostCinematicPhase.FinalizeAgents)
				{
					foreach (HideoutAmbushBossFightCinematicController.HideoutCinematicAgentInfo hideoutCinematicAgentInfo in this._hideoutAgentsInfo)
					{
						Agent agent = hideoutCinematicAgentInfo.Agent;
						agent.DisableScriptedMovement();
						agent.SetMaximumSpeedLimit(-1f, false);
					}
					this._postCinematicPhase = HideoutAmbushBossFightCinematicController.HideoutPostCinematicPhase.Completed;
				}
			}
			else
			{
				int num = 0;
				foreach (HideoutAmbushBossFightCinematicController.HideoutCinematicAgentInfo hideoutCinematicAgentInfo2 in this._hideoutAgentsInfo)
				{
					Agent agent2 = hideoutCinematicAgentInfo2.Agent;
					if (!hideoutCinematicAgentInfo2.HasReachedTarget(0.5f))
					{
						MatrixFrame targetFrame = hideoutCinematicAgentInfo2.TargetFrame;
						WorldPosition worldPosition = new WorldPosition(base.Mission.Scene, targetFrame.origin);
						agent2.TeleportToPosition(worldPosition.GetGroundVec3());
						Agent agent3 = agent2;
						Vec2 vec = targetFrame.rotation.f.AsVec2;
						vec = vec.Normalized();
						agent3.SetMovementDirection(vec);
					}
					agent2.Formation = this._cachedAgentFormations[num];
					num++;
				}
				this._postCinematicPhase = HideoutAmbushBossFightCinematicController.HideoutPostCinematicPhase.FinalizeAgents;
			}
			return this._postCinematicPhase == HideoutAmbushBossFightCinematicController.HideoutPostCinematicPhase.Completed && this._remainingStateDuration <= 0f;
		}

		// Token: 0x060005C6 RID: 1478 RVA: 0x00026788 File Offset: 0x00024988
		private void BeginStateTransition(HideoutAmbushBossFightCinematicController.HideoutCinematicState nextState)
		{
			this.State = nextState;
			this._remainingTransitionDuration = this._transitionDuration;
			this.InStateTransition = true;
			Action<HideoutAmbushBossFightCinematicController.HideoutCinematicState, float> onCinematicTransition = this.OnCinematicTransition;
			if (onCinematicTransition == null)
			{
				return;
			}
			onCinematicTransition(this.State, this._remainingTransitionDuration);
		}

		// Token: 0x060005C7 RID: 1479 RVA: 0x000267C0 File Offset: 0x000249C0
		private void ComputeAgentFrames(Agent playerAgent, List<Agent> playerCompanions, Agent bossAgent, List<Agent> bossCompanions, float placementPerturbation, float placementAngle)
		{
			this._hideoutAgentsInfo = new List<HideoutAmbushBossFightCinematicController.HideoutCinematicAgentInfo>();
			MatrixFrame matrixFrame;
			MatrixFrame matrixFrame2;
			this._hideoutBossFightBehavior.GetPlayerFrames(out matrixFrame, out matrixFrame2, placementPerturbation);
			this._playerAgentInfo = new HideoutAmbushBossFightCinematicController.HideoutCinematicAgentInfo(playerAgent, HideoutAmbushBossFightCinematicController.HideoutAgentType.Player, ref matrixFrame, ref matrixFrame2);
			this._hideoutAgentsInfo.Add(this._playerAgentInfo);
			List<MatrixFrame> list;
			List<MatrixFrame> list2;
			this.GetAllyFrames(out list, out list2, this._playerAgentInfo.InitialFrame, this._playerAgentInfo.TargetFrame, playerCompanions.Count, placementAngle);
			for (int i = 0; i < playerCompanions.Count; i++)
			{
				matrixFrame = list[i];
				matrixFrame2 = list2[i];
				this._hideoutAgentsInfo.Add(new HideoutAmbushBossFightCinematicController.HideoutCinematicAgentInfo(playerCompanions[i], HideoutAmbushBossFightCinematicController.HideoutAgentType.Ally, ref matrixFrame, ref matrixFrame2));
			}
			this._hideoutBossFightBehavior.GetBossFrames(out matrixFrame, out matrixFrame2, placementPerturbation);
			this._bossAgentInfo = new HideoutAmbushBossFightCinematicController.HideoutCinematicAgentInfo(bossAgent, HideoutAmbushBossFightCinematicController.HideoutAgentType.Boss, ref matrixFrame, ref matrixFrame2);
			this._hideoutAgentsInfo.Add(this._bossAgentInfo);
			this.GetBanditFrames(out list, out list2, this._bossAgentInfo.InitialFrame, this._bossAgentInfo.TargetFrame, bossCompanions.Count, placementAngle);
			for (int j = 0; j < bossCompanions.Count; j++)
			{
				matrixFrame = list[j];
				matrixFrame2 = list2[j];
				this._hideoutAgentsInfo.Add(new HideoutAmbushBossFightCinematicController.HideoutCinematicAgentInfo(bossCompanions[j], HideoutAmbushBossFightCinematicController.HideoutAgentType.Bandit, ref matrixFrame, ref matrixFrame2));
			}
		}

		// Token: 0x060005C8 RID: 1480 RVA: 0x0002691C File Offset: 0x00024B1C
		public void GetAllyFrames(out List<MatrixFrame> initialFrames, out List<MatrixFrame> targetFrames, MatrixFrame initialPlayerFrame, MatrixFrame targetPlayerFrame, int agentCount, float agentOffsetAngle)
		{
			initialFrames = new List<MatrixFrame>();
			targetFrames = new List<MatrixFrame>();
			MatrixFrame[] array = new MatrixFrame[this.GetSpineTroopCount(agentCount)];
			for (int i = 0; i < array.Length; i++)
			{
				int num = i + 1;
				MatrixFrame[] array2 = array;
				int num2 = i;
				Vec3 vec = new Vec3(initialPlayerFrame.origin.x, initialPlayerFrame.origin.y - 1.3f * (float)num, initialPlayerFrame.origin.z, -1f);
				array2[num2] = new MatrixFrame(ref initialPlayerFrame.rotation, ref vec);
			}
			for (int j = 0; j < array.Length; j++)
			{
				int num3 = j + 1;
				initialFrames.Add(array[j]);
				int num4 = num3;
				int num5 = num3;
				for (int k = 0; k < num4; k++)
				{
					List<MatrixFrame> list = initialFrames;
					MatrixFrame[] array3 = array;
					int num6 = j;
					Vec3 vec = new Vec3(array[j].origin.x - 1f * (float)(k + 1), array[j].origin.y, array[j].origin.z, -1f);
					list.Add(new MatrixFrame(ref array3[num6].rotation, ref vec));
				}
				for (int l = 0; l < num5; l++)
				{
					List<MatrixFrame> list2 = initialFrames;
					MatrixFrame[] array4 = array;
					int num7 = j;
					Vec3 vec = new Vec3(array[j].origin.x + 1f * (float)(l + 1), array[j].origin.y, array[j].origin.z, -1f);
					list2.Add(new MatrixFrame(ref array4[num7].rotation, ref vec));
				}
			}
			foreach (MatrixFrame matrixFrame in initialFrames)
			{
				List<MatrixFrame> list3 = targetFrames;
				Vec3 vec = new Vec3(matrixFrame.origin.x, matrixFrame.origin.y - 0.5f, matrixFrame.origin.z, -1f);
				list3.Add(new MatrixFrame(ref matrixFrame.rotation, ref vec));
			}
		}

		// Token: 0x060005C9 RID: 1481 RVA: 0x00026B4C File Offset: 0x00024D4C
		public int GetSpineTroopCount(int totalTroopCount)
		{
			int num = -totalTroopCount;
			int num2 = (int)((-2f + MathF.Sqrt((float)(4 - 4 * num))) / 2f);
			return num2 * num2 + 2 * num2;
		}

		// Token: 0x060005CA RID: 1482 RVA: 0x00026B7C File Offset: 0x00024D7C
		public void GetBanditFrames(out List<MatrixFrame> initialFrames, out List<MatrixFrame> targetFrames, MatrixFrame initialBossFrame, MatrixFrame targetBossFrame, int agentCount, float agentOffsetAngle)
		{
			initialFrames = new List<MatrixFrame>();
			targetFrames = new List<MatrixFrame>();
			MatrixFrame[] array = new MatrixFrame[this.GetSpineTroopCount(agentCount)];
			for (int i = 0; i < array.Length; i++)
			{
				int num = i + 1;
				MatrixFrame[] array2 = array;
				int num2 = i;
				Vec3 vec = new Vec3(initialBossFrame.origin.x, initialBossFrame.origin.y + 1.2f * (float)num, initialBossFrame.origin.z, -1f);
				array2[num2] = new MatrixFrame(ref initialBossFrame.rotation, ref vec);
			}
			for (int j = 0; j < array.Length; j++)
			{
				int num3 = j + 1;
				initialFrames.Add(array[j]);
				int num4 = num3;
				int num5 = num3;
				for (int k = 0; k < num4; k++)
				{
					List<MatrixFrame> list = initialFrames;
					MatrixFrame[] array3 = array;
					int num6 = j;
					Vec3 vec = new Vec3(array[j].origin.x - 1f * (float)(k + 1), array[j].origin.y, array[j].origin.z, -1f);
					list.Add(new MatrixFrame(ref array3[num6].rotation, ref vec));
				}
				for (int l = 0; l < num5; l++)
				{
					List<MatrixFrame> list2 = initialFrames;
					MatrixFrame[] array4 = array;
					int num7 = j;
					Vec3 vec = new Vec3(array[j].origin.x + 1f * (float)(l + 1), array[j].origin.y, array[j].origin.z, -1f);
					list2.Add(new MatrixFrame(ref array4[num7].rotation, ref vec));
				}
			}
			foreach (MatrixFrame matrixFrame in initialFrames)
			{
				List<MatrixFrame> list3 = targetFrames;
				Vec3 vec = new Vec3(matrixFrame.origin.x, matrixFrame.origin.y - 0.5f, matrixFrame.origin.z, -1f);
				list3.Add(new MatrixFrame(ref matrixFrame.rotation, ref vec));
			}
		}

		// Token: 0x040002F3 RID: 755
		private const float AgentTargetProximityThreshold = 0.5f;

		// Token: 0x040002F4 RID: 756
		private const float AgentMaxSpeedCinematicOverride = 0.65f;

		// Token: 0x040002F5 RID: 757
		public const string HideoutSceneEntityTag = "hideout_boss_fight";

		// Token: 0x040002F6 RID: 758
		public const float DefaultTransitionDuration = 0.4f;

		// Token: 0x040002F7 RID: 759
		public const float DefaultStateDuration = 0.2f;

		// Token: 0x040002F8 RID: 760
		public const float DefaultCinematicDuration = 8f;

		// Token: 0x040002F9 RID: 761
		public const float DefaultPlacementPerturbation = 0.25f;

		// Token: 0x040002FA RID: 762
		public const float DefaultPlacementAngle = 0.20943952f;

		// Token: 0x040002FB RID: 763
		private HideoutAmbushBossFightCinematicController.OnInitialFadeOutFinished _initialFadeOutFinished;

		// Token: 0x040002FC RID: 764
		private float _cinematicDuration = 8f;

		// Token: 0x040002FD RID: 765
		private float _stateDuration = 0.2f;

		// Token: 0x040002FE RID: 766
		private float _transitionDuration = 0.4f;

		// Token: 0x040002FF RID: 767
		private float _remainingCinematicDuration = 8f;

		// Token: 0x04000300 RID: 768
		private float _remainingStateDuration = 0.2f;

		// Token: 0x04000301 RID: 769
		private float _remainingTransitionDuration = 0.4f;

		// Token: 0x04000302 RID: 770
		private List<Formation> _cachedAgentFormations;

		// Token: 0x04000303 RID: 771
		private List<HideoutAmbushBossFightCinematicController.HideoutCinematicAgentInfo> _hideoutAgentsInfo;

		// Token: 0x04000304 RID: 772
		private HideoutAmbushBossFightCinematicController.HideoutCinematicAgentInfo _bossAgentInfo;

		// Token: 0x04000305 RID: 773
		private HideoutAmbushBossFightCinematicController.HideoutCinematicAgentInfo _playerAgentInfo;

		// Token: 0x04000306 RID: 774
		private bool _isBehaviorInit;

		// Token: 0x04000307 RID: 775
		private HideoutAmbushBossFightCinematicController.HideoutPreCinematicPhase _preCinematicPhase;

		// Token: 0x04000308 RID: 776
		private HideoutAmbushBossFightCinematicController.HideoutPostCinematicPhase _postCinematicPhase;

		// Token: 0x04000309 RID: 777
		private HideoutBossFightBehavior _hideoutBossFightBehavior;

		// Token: 0x0200018B RID: 395
		// (Invoke) Token: 0x06000EB9 RID: 3769
		public delegate void OnInitialFadeOutFinished(ref Agent playerAgent, ref List<Agent> playerCompanions, ref Agent bossAgent, ref List<Agent> bossCompanions, ref float placementPerturbation, ref float placementAngle);

		// Token: 0x0200018C RID: 396
		// (Invoke) Token: 0x06000EBD RID: 3773
		public delegate void OnHideoutCinematicFinished();

		// Token: 0x0200018D RID: 397
		public readonly struct HideoutCinematicAgentInfo
		{
			// Token: 0x06000EC0 RID: 3776 RVA: 0x00065DA7 File Offset: 0x00063FA7
			public HideoutCinematicAgentInfo(Agent agent, HideoutAmbushBossFightCinematicController.HideoutAgentType type, in MatrixFrame initialFrame, in MatrixFrame targetFrame)
			{
				this.Agent = agent;
				this.InitialFrame = initialFrame;
				this.TargetFrame = targetFrame;
				this.Type = type;
			}

			// Token: 0x06000EC1 RID: 3777 RVA: 0x00065DD0 File Offset: 0x00063FD0
			public bool HasReachedTarget(float proximityThreshold = 0.5f)
			{
				return this.Agent.Position.Distance(this.TargetFrame.origin) <= proximityThreshold;
			}

			// Token: 0x04000761 RID: 1889
			public readonly Agent Agent;

			// Token: 0x04000762 RID: 1890
			public readonly MatrixFrame InitialFrame;

			// Token: 0x04000763 RID: 1891
			public readonly MatrixFrame TargetFrame;

			// Token: 0x04000764 RID: 1892
			public readonly HideoutAmbushBossFightCinematicController.HideoutAgentType Type;
		}

		// Token: 0x0200018E RID: 398
		public enum HideoutCinematicState
		{
			// Token: 0x04000766 RID: 1894
			None,
			// Token: 0x04000767 RID: 1895
			InitialFadeOut,
			// Token: 0x04000768 RID: 1896
			PreCinematic,
			// Token: 0x04000769 RID: 1897
			Cinematic,
			// Token: 0x0400076A RID: 1898
			PostCinematic,
			// Token: 0x0400076B RID: 1899
			Completed
		}

		// Token: 0x0200018F RID: 399
		public enum HideoutAgentType
		{
			// Token: 0x0400076D RID: 1901
			Player,
			// Token: 0x0400076E RID: 1902
			Boss,
			// Token: 0x0400076F RID: 1903
			Ally,
			// Token: 0x04000770 RID: 1904
			Bandit
		}

		// Token: 0x02000190 RID: 400
		public enum HideoutPreCinematicPhase
		{
			// Token: 0x04000772 RID: 1906
			NotStarted,
			// Token: 0x04000773 RID: 1907
			InitializeFormations,
			// Token: 0x04000774 RID: 1908
			StopFormations,
			// Token: 0x04000775 RID: 1909
			InitializeAgents,
			// Token: 0x04000776 RID: 1910
			MoveAgents,
			// Token: 0x04000777 RID: 1911
			Completed
		}

		// Token: 0x02000191 RID: 401
		public enum HideoutPostCinematicPhase
		{
			// Token: 0x04000779 RID: 1913
			NotStarted,
			// Token: 0x0400077A RID: 1914
			MoveAgents,
			// Token: 0x0400077B RID: 1915
			FinalizeAgents,
			// Token: 0x0400077C RID: 1916
			Completed
		}
	}
}
