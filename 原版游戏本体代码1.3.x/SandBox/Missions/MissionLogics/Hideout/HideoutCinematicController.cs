using System;
using System.Collections.Generic;
using SandBox.Objects.Cinematics;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics.Hideout
{
	// Token: 0x02000093 RID: 147
	public class HideoutCinematicController : MissionLogic
	{
		// Token: 0x1400000F RID: 15
		// (add) Token: 0x060005F4 RID: 1524 RVA: 0x000289D0 File Offset: 0x00026BD0
		// (remove) Token: 0x060005F5 RID: 1525 RVA: 0x00028A08 File Offset: 0x00026C08
		public event Action OnCinematicFinished;

		// Token: 0x14000010 RID: 16
		// (add) Token: 0x060005F6 RID: 1526 RVA: 0x00028A40 File Offset: 0x00026C40
		// (remove) Token: 0x060005F7 RID: 1527 RVA: 0x00028A78 File Offset: 0x00026C78
		public event Action<HideoutCinematicController.HideoutCinematicState> OnCinematicStateChanged;

		// Token: 0x14000011 RID: 17
		// (add) Token: 0x060005F8 RID: 1528 RVA: 0x00028AB0 File Offset: 0x00026CB0
		// (remove) Token: 0x060005F9 RID: 1529 RVA: 0x00028AE8 File Offset: 0x00026CE8
		public event Action<HideoutCinematicController.HideoutCinematicState, float> OnCinematicTransition;

		// Token: 0x1700007E RID: 126
		// (get) Token: 0x060005FA RID: 1530 RVA: 0x00028B1D File Offset: 0x00026D1D
		// (set) Token: 0x060005FB RID: 1531 RVA: 0x00028B25 File Offset: 0x00026D25
		public HideoutCinematicController.HideoutCinematicState State { get; private set; }

		// Token: 0x1700007F RID: 127
		// (get) Token: 0x060005FC RID: 1532 RVA: 0x00028B2E File Offset: 0x00026D2E
		// (set) Token: 0x060005FD RID: 1533 RVA: 0x00028B36 File Offset: 0x00026D36
		public bool InStateTransition { get; private set; }

		// Token: 0x17000080 RID: 128
		// (get) Token: 0x060005FE RID: 1534 RVA: 0x00028B3F File Offset: 0x00026D3F
		public bool IsCinematicActive
		{
			get
			{
				return this.State > HideoutCinematicController.HideoutCinematicState.None;
			}
		}

		// Token: 0x17000081 RID: 129
		// (get) Token: 0x060005FF RID: 1535 RVA: 0x00028B4A File Offset: 0x00026D4A
		public float CinematicDuration
		{
			get
			{
				return this._cinematicDuration;
			}
		}

		// Token: 0x17000082 RID: 130
		// (get) Token: 0x06000600 RID: 1536 RVA: 0x00028B52 File Offset: 0x00026D52
		public float TransitionDuration
		{
			get
			{
				return this._transitionDuration;
			}
		}

		// Token: 0x17000083 RID: 131
		// (get) Token: 0x06000601 RID: 1537 RVA: 0x00028B5A File Offset: 0x00026D5A
		public override MissionBehaviorType BehaviorType
		{
			get
			{
				return MissionBehaviorType.Logic;
			}
		}

		// Token: 0x06000602 RID: 1538 RVA: 0x00028B60 File Offset: 0x00026D60
		public HideoutCinematicController()
		{
			this.State = HideoutCinematicController.HideoutCinematicState.None;
			this.InStateTransition = false;
			this._isBehaviorInit = false;
		}

		// Token: 0x06000603 RID: 1539 RVA: 0x00028BCC File Offset: 0x00026DCC
		public void StartCinematic(HideoutCinematicController.OnInitialFadeOutFinished initialFadeOutFinished, Action cinematicFinishedCallback, float transitionDuration = 0.4f, float stateDuration = 0.2f, float cinematicDuration = 8f, bool forceDismountAgents = false)
		{
			if (this._isBehaviorInit && this.State == HideoutCinematicController.HideoutCinematicState.None)
			{
				this.OnCinematicFinished += cinematicFinishedCallback;
				this._initialFadeOutFinished = initialFadeOutFinished;
				this._preCinematicPhase = HideoutCinematicController.HideoutPreCinematicPhase.InitializeFormations;
				this._postCinematicPhase = HideoutCinematicController.HideoutPostCinematicPhase.MoveAgents;
				this._transitionDuration = transitionDuration;
				this._stateDuration = stateDuration;
				this._cinematicDuration = cinematicDuration;
				this._remainingCinematicDuration = this._cinematicDuration;
				this.BeginStateTransition(HideoutCinematicController.HideoutCinematicState.InitialFadeOut);
				return;
			}
			if (!this._isBehaviorInit)
			{
				Debug.FailedAssert("Hideout cinematic controller is not initialized.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\Missions\\MissionLogics\\Hideout\\HideoutCinematicController.cs", "StartCinematic", 178);
				return;
			}
			if (this.State != HideoutCinematicController.HideoutCinematicState.None)
			{
				Debug.FailedAssert("There is already an ongoing cinematic.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\Missions\\MissionLogics\\Hideout\\HideoutCinematicController.cs", "StartCinematic", 182);
			}
		}

		// Token: 0x06000604 RID: 1540 RVA: 0x00028C74 File Offset: 0x00026E74
		public void GetBossStandingEyePosition(out Vec3 eyePosition)
		{
			Agent agent = this._bossAgentInfo.Agent;
			if (((agent != null) ? agent.Monster : null) != null)
			{
				eyePosition = this._bossAgentInfo.InitialFrame.origin + Vec3.Up * (this._bossAgentInfo.Agent.AgentScale * this._bossAgentInfo.Agent.Monster.StandingEyeHeight);
				return;
			}
			eyePosition = Vec3.Zero;
			Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\Missions\\MissionLogics\\Hideout\\HideoutCinematicController.cs", "GetBossStandingEyePosition", 195);
		}

		// Token: 0x06000605 RID: 1541 RVA: 0x00028D0C File Offset: 0x00026F0C
		public void GetPlayerStandingEyePosition(out Vec3 eyePosition)
		{
			Agent agent = this._playerAgentInfo.Agent;
			if (((agent != null) ? agent.Monster : null) != null)
			{
				eyePosition = this._playerAgentInfo.InitialFrame.origin + Vec3.Up * (this._playerAgentInfo.Agent.AgentScale * this._playerAgentInfo.Agent.Monster.StandingEyeHeight);
				return;
			}
			eyePosition = Vec3.Zero;
			Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\Missions\\MissionLogics\\Hideout\\HideoutCinematicController.cs", "GetPlayerStandingEyePosition", 208);
		}

		// Token: 0x06000606 RID: 1542 RVA: 0x00028DA4 File Offset: 0x00026FA4
		public MatrixFrame GetBanditsInitialFrame()
		{
			MatrixFrame result;
			this._hideoutBossFightBehavior.GetBanditsInitialFrame(out result);
			return result;
		}

		// Token: 0x06000607 RID: 1543 RVA: 0x00028DC0 File Offset: 0x00026FC0
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

		// Token: 0x06000608 RID: 1544 RVA: 0x00028E14 File Offset: 0x00027014
		public override void OnBehaviorInitialize()
		{
			base.OnBehaviorInitialize();
			GameEntity gameEntity = base.Mission.Scene.FindEntityWithTag("hideout_boss_fight");
			this._hideoutBossFightBehavior = ((gameEntity != null) ? gameEntity.GetFirstScriptOfType<HideoutBossFightBehavior>() : null);
			this._isBehaviorInit = gameEntity != null && this._hideoutBossFightBehavior != null;
		}

		// Token: 0x06000609 RID: 1545 RVA: 0x00028E6C File Offset: 0x0002706C
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
				case HideoutCinematicController.HideoutCinematicState.InitialFadeOut:
					if (this.TickInitialFadeOut(dt))
					{
						this.BeginStateTransition(HideoutCinematicController.HideoutCinematicState.PreCinematic);
						return;
					}
					break;
				case HideoutCinematicController.HideoutCinematicState.PreCinematic:
					if (this.TickPreCinematic(dt))
					{
						this.BeginStateTransition(HideoutCinematicController.HideoutCinematicState.Cinematic);
						return;
					}
					break;
				case HideoutCinematicController.HideoutCinematicState.Cinematic:
					if (this.TickCinematic(dt))
					{
						this.BeginStateTransition(HideoutCinematicController.HideoutCinematicState.PostCinematic);
						return;
					}
					break;
				case HideoutCinematicController.HideoutCinematicState.PostCinematic:
					if (this.TickPostCinematic(dt))
					{
						this.BeginStateTransition(HideoutCinematicController.HideoutCinematicState.Completed);
						return;
					}
					break;
				case HideoutCinematicController.HideoutCinematicState.Completed:
				{
					Action onCinematicFinished = this.OnCinematicFinished;
					if (onCinematicFinished != null)
					{
						onCinematicFinished();
					}
					this.OnCinematicFinished = null;
					this.OnCinematicStateChanged = null;
					this.OnCinematicTransition = null;
					this.State = HideoutCinematicController.HideoutCinematicState.None;
					break;
				}
				default:
					return;
				}
			}
		}

		// Token: 0x0600060A RID: 1546 RVA: 0x00028F34 File Offset: 0x00027134
		private void TickStateTransition(float dt)
		{
			this._remainingTransitionDuration -= dt;
			if (this._remainingTransitionDuration <= 0f)
			{
				this.InStateTransition = false;
				Action<HideoutCinematicController.HideoutCinematicState> onCinematicStateChanged = this.OnCinematicStateChanged;
				if (onCinematicStateChanged != null)
				{
					onCinematicStateChanged(this.State);
				}
				this._remainingStateDuration = this._stateDuration;
			}
		}

		// Token: 0x0600060B RID: 1547 RVA: 0x00028F88 File Offset: 0x00027188
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
				HideoutCinematicController.OnInitialFadeOutFinished initialFadeOutFinished = this._initialFadeOutFinished;
				if (initialFadeOutFinished != null)
				{
					initialFadeOutFinished(ref playerAgent, ref playerCompanions, ref bossAgent, ref bossCompanions, ref placementPerturbation, ref placementAngle);
				}
				this.ComputeAgentFrames(playerAgent, playerCompanions, bossAgent, bossCompanions, placementPerturbation, placementAngle);
			}
			return this._remainingStateDuration <= 0f;
		}

		// Token: 0x0600060C RID: 1548 RVA: 0x00029004 File Offset: 0x00027204
		private bool TickPreCinematic(float dt)
		{
			Scene scene = base.Mission.Scene;
			this._remainingStateDuration -= dt;
			switch (this._preCinematicPhase)
			{
			case HideoutCinematicController.HideoutPreCinematicPhase.InitializeFormations:
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
				foreach (HideoutCinematicController.HideoutCinematicAgentInfo hideoutCinematicAgentInfo in this._hideoutAgentsInfo)
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
				this._preCinematicPhase = HideoutCinematicController.HideoutPreCinematicPhase.StopFormations;
				break;
			}
			case HideoutCinematicController.HideoutPreCinematicPhase.StopFormations:
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
				this._preCinematicPhase = HideoutCinematicController.HideoutPreCinematicPhase.InitializeAgents;
				break;
			case HideoutCinematicController.HideoutPreCinematicPhase.InitializeAgents:
			{
				bool isTeleportingAgents2 = base.Mission.IsTeleportingAgents;
				base.Mission.IsTeleportingAgents = true;
				this._cachedAgentFormations = new List<Formation>();
				foreach (HideoutCinematicController.HideoutCinematicAgentInfo hideoutCinematicAgentInfo2 in this._hideoutAgentsInfo)
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
				this._preCinematicPhase = HideoutCinematicController.HideoutPreCinematicPhase.MoveAgents;
				break;
			}
			case HideoutCinematicController.HideoutPreCinematicPhase.MoveAgents:
				foreach (HideoutCinematicController.HideoutCinematicAgentInfo hideoutCinematicAgentInfo3 in this._hideoutAgentsInfo)
				{
					Agent agent5 = hideoutCinematicAgentInfo3.Agent;
					MatrixFrame targetFrame = hideoutCinematicAgentInfo3.TargetFrame;
					WorldPosition worldPosition2 = new WorldPosition(scene, targetFrame.origin);
					agent5.SetMaximumSpeedLimit(0.65f, false);
					Agent agent6 = agent5;
					Vec2 vec = targetFrame.rotation.f.AsVec2;
					agent6.SetScriptedPositionAndDirection(ref worldPosition2, vec.RotationInRadians, true, Agent.AIScriptedFrameFlags.None);
				}
				this._preCinematicPhase = HideoutCinematicController.HideoutPreCinematicPhase.Completed;
				break;
			}
			return this._preCinematicPhase == HideoutCinematicController.HideoutPreCinematicPhase.Completed && this._remainingStateDuration <= 0f;
		}

		// Token: 0x0600060D RID: 1549 RVA: 0x0002947C File Offset: 0x0002767C
		private bool TickCinematic(float dt)
		{
			this._remainingCinematicDuration -= dt;
			this._remainingStateDuration -= dt;
			return this._remainingCinematicDuration <= 0f && this._remainingStateDuration <= 0f;
		}

		// Token: 0x0600060E RID: 1550 RVA: 0x000294B8 File Offset: 0x000276B8
		private bool TickPostCinematic(float dt)
		{
			this._remainingStateDuration -= dt;
			HideoutCinematicController.HideoutPostCinematicPhase postCinematicPhase = this._postCinematicPhase;
			if (postCinematicPhase != HideoutCinematicController.HideoutPostCinematicPhase.MoveAgents)
			{
				if (postCinematicPhase == HideoutCinematicController.HideoutPostCinematicPhase.FinalizeAgents)
				{
					foreach (HideoutCinematicController.HideoutCinematicAgentInfo hideoutCinematicAgentInfo in this._hideoutAgentsInfo)
					{
						Agent agent = hideoutCinematicAgentInfo.Agent;
						agent.DisableScriptedMovement();
						agent.SetMaximumSpeedLimit(-1f, false);
					}
					this._postCinematicPhase = HideoutCinematicController.HideoutPostCinematicPhase.Completed;
				}
			}
			else
			{
				int num = 0;
				foreach (HideoutCinematicController.HideoutCinematicAgentInfo hideoutCinematicAgentInfo2 in this._hideoutAgentsInfo)
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
				this._postCinematicPhase = HideoutCinematicController.HideoutPostCinematicPhase.FinalizeAgents;
			}
			return this._postCinematicPhase == HideoutCinematicController.HideoutPostCinematicPhase.Completed && this._remainingStateDuration <= 0f;
		}

		// Token: 0x0600060F RID: 1551 RVA: 0x00029630 File Offset: 0x00027830
		private void BeginStateTransition(HideoutCinematicController.HideoutCinematicState nextState)
		{
			this.State = nextState;
			this._remainingTransitionDuration = this._transitionDuration;
			this.InStateTransition = true;
			Action<HideoutCinematicController.HideoutCinematicState, float> onCinematicTransition = this.OnCinematicTransition;
			if (onCinematicTransition == null)
			{
				return;
			}
			onCinematicTransition(this.State, this._remainingTransitionDuration);
		}

		// Token: 0x06000610 RID: 1552 RVA: 0x00029668 File Offset: 0x00027868
		private bool CheckNavMeshValidity(ref Vec3 initial, ref Vec3 target)
		{
			Scene scene = base.Mission.Scene;
			bool result = false;
			bool flag = scene.GetNavigationMeshForPosition(initial) != UIntPtr.Zero;
			bool flag2 = scene.GetNavigationMeshForPosition(target) != UIntPtr.Zero;
			if (flag && flag2)
			{
				WorldPosition position = new WorldPosition(scene, initial);
				WorldPosition destination = new WorldPosition(scene, target);
				result = scene.DoesPathExistBetweenPositions(position, destination);
			}
			return result;
		}

		// Token: 0x06000611 RID: 1553 RVA: 0x000296D4 File Offset: 0x000278D4
		private void ComputeAgentFrames(Agent playerAgent, List<Agent> playerCompanions, Agent bossAgent, List<Agent> bossCompanions, float placementPerturbation, float placementAngle)
		{
			this._hideoutAgentsInfo = new List<HideoutCinematicController.HideoutCinematicAgentInfo>();
			MatrixFrame matrixFrame;
			MatrixFrame matrixFrame2;
			this._hideoutBossFightBehavior.GetPlayerFrames(out matrixFrame, out matrixFrame2, placementPerturbation);
			this._playerAgentInfo = new HideoutCinematicController.HideoutCinematicAgentInfo(playerAgent, HideoutCinematicController.HideoutAgentType.Player, ref matrixFrame, ref matrixFrame2);
			this._hideoutAgentsInfo.Add(this._playerAgentInfo);
			List<MatrixFrame> list;
			List<MatrixFrame> list2;
			this._hideoutBossFightBehavior.GetAllyFrames(out list, out list2, playerCompanions.Count, placementAngle, placementPerturbation);
			for (int i = 0; i < playerCompanions.Count; i++)
			{
				matrixFrame = list[i];
				matrixFrame2 = list2[i];
				this._hideoutAgentsInfo.Add(new HideoutCinematicController.HideoutCinematicAgentInfo(playerCompanions[i], HideoutCinematicController.HideoutAgentType.Ally, ref matrixFrame, ref matrixFrame2));
			}
			this._hideoutBossFightBehavior.GetBossFrames(out matrixFrame, out matrixFrame2, placementPerturbation);
			this._bossAgentInfo = new HideoutCinematicController.HideoutCinematicAgentInfo(bossAgent, HideoutCinematicController.HideoutAgentType.Boss, ref matrixFrame, ref matrixFrame2);
			this._hideoutAgentsInfo.Add(this._bossAgentInfo);
			this._hideoutBossFightBehavior.GetBanditFrames(out list, out list2, bossCompanions.Count, placementAngle, placementPerturbation);
			for (int j = 0; j < bossCompanions.Count; j++)
			{
				matrixFrame = list[j];
				matrixFrame2 = list2[j];
				this._hideoutAgentsInfo.Add(new HideoutCinematicController.HideoutCinematicAgentInfo(bossCompanions[j], HideoutCinematicController.HideoutAgentType.Bandit, ref matrixFrame, ref matrixFrame2));
			}
		}

		// Token: 0x04000326 RID: 806
		private const float AgentTargetProximityThreshold = 0.5f;

		// Token: 0x04000327 RID: 807
		private const float AgentMaxSpeedCinematicOverride = 0.65f;

		// Token: 0x04000328 RID: 808
		public const string HideoutSceneEntityTag = "hideout_boss_fight";

		// Token: 0x04000329 RID: 809
		public const float DefaultTransitionDuration = 0.4f;

		// Token: 0x0400032A RID: 810
		public const float DefaultStateDuration = 0.2f;

		// Token: 0x0400032B RID: 811
		public const float DefaultCinematicDuration = 8f;

		// Token: 0x0400032C RID: 812
		public const float DefaultPlacementPerturbation = 0.25f;

		// Token: 0x0400032D RID: 813
		public const float DefaultPlacementAngle = 0.20943952f;

		// Token: 0x0400032E RID: 814
		private HideoutCinematicController.OnInitialFadeOutFinished _initialFadeOutFinished;

		// Token: 0x0400032F RID: 815
		private float _cinematicDuration = 8f;

		// Token: 0x04000330 RID: 816
		private float _stateDuration = 0.2f;

		// Token: 0x04000331 RID: 817
		private float _transitionDuration = 0.4f;

		// Token: 0x04000332 RID: 818
		private float _remainingCinematicDuration = 8f;

		// Token: 0x04000333 RID: 819
		private float _remainingStateDuration = 0.2f;

		// Token: 0x04000334 RID: 820
		private float _remainingTransitionDuration = 0.4f;

		// Token: 0x04000335 RID: 821
		private List<Formation> _cachedAgentFormations;

		// Token: 0x04000336 RID: 822
		private List<HideoutCinematicController.HideoutCinematicAgentInfo> _hideoutAgentsInfo;

		// Token: 0x04000337 RID: 823
		private HideoutCinematicController.HideoutCinematicAgentInfo _bossAgentInfo;

		// Token: 0x04000338 RID: 824
		private HideoutCinematicController.HideoutCinematicAgentInfo _playerAgentInfo;

		// Token: 0x04000339 RID: 825
		private bool _isBehaviorInit;

		// Token: 0x0400033A RID: 826
		private HideoutCinematicController.HideoutPreCinematicPhase _preCinematicPhase;

		// Token: 0x0400033B RID: 827
		private HideoutCinematicController.HideoutPostCinematicPhase _postCinematicPhase;

		// Token: 0x0400033C RID: 828
		private HideoutBossFightBehavior _hideoutBossFightBehavior;

		// Token: 0x02000195 RID: 405
		// (Invoke) Token: 0x06000EC7 RID: 3783
		public delegate void OnInitialFadeOutFinished(ref Agent playerAgent, ref List<Agent> playerCompanions, ref Agent bossAgent, ref List<Agent> bossCompanions, ref float placementPerturbation, ref float placementAngle);

		// Token: 0x02000196 RID: 406
		// (Invoke) Token: 0x06000ECB RID: 3787
		public delegate void OnHideoutCinematicFinished();

		// Token: 0x02000197 RID: 407
		public readonly struct HideoutCinematicAgentInfo
		{
			// Token: 0x06000ECE RID: 3790 RVA: 0x00065E38 File Offset: 0x00064038
			public HideoutCinematicAgentInfo(Agent agent, HideoutCinematicController.HideoutAgentType type, in MatrixFrame initialFrame, in MatrixFrame targetFrame)
			{
				this.Agent = agent;
				this.InitialFrame = initialFrame;
				this.TargetFrame = targetFrame;
				this.Type = type;
			}

			// Token: 0x06000ECF RID: 3791 RVA: 0x00065E64 File Offset: 0x00064064
			public bool HasReachedTarget(float proximityThreshold = 0.5f)
			{
				return this.Agent.Position.Distance(this.TargetFrame.origin) <= proximityThreshold;
			}

			// Token: 0x0400078B RID: 1931
			public readonly Agent Agent;

			// Token: 0x0400078C RID: 1932
			public readonly MatrixFrame InitialFrame;

			// Token: 0x0400078D RID: 1933
			public readonly MatrixFrame TargetFrame;

			// Token: 0x0400078E RID: 1934
			public readonly HideoutCinematicController.HideoutAgentType Type;
		}

		// Token: 0x02000198 RID: 408
		public enum HideoutCinematicState
		{
			// Token: 0x04000790 RID: 1936
			None,
			// Token: 0x04000791 RID: 1937
			InitialFadeOut,
			// Token: 0x04000792 RID: 1938
			PreCinematic,
			// Token: 0x04000793 RID: 1939
			Cinematic,
			// Token: 0x04000794 RID: 1940
			PostCinematic,
			// Token: 0x04000795 RID: 1941
			Completed
		}

		// Token: 0x02000199 RID: 409
		public enum HideoutAgentType
		{
			// Token: 0x04000797 RID: 1943
			Player,
			// Token: 0x04000798 RID: 1944
			Boss,
			// Token: 0x04000799 RID: 1945
			Ally,
			// Token: 0x0400079A RID: 1946
			Bandit
		}

		// Token: 0x0200019A RID: 410
		public enum HideoutPreCinematicPhase
		{
			// Token: 0x0400079C RID: 1948
			NotStarted,
			// Token: 0x0400079D RID: 1949
			InitializeFormations,
			// Token: 0x0400079E RID: 1950
			StopFormations,
			// Token: 0x0400079F RID: 1951
			InitializeAgents,
			// Token: 0x040007A0 RID: 1952
			MoveAgents,
			// Token: 0x040007A1 RID: 1953
			Completed
		}

		// Token: 0x0200019B RID: 411
		public enum HideoutPostCinematicPhase
		{
			// Token: 0x040007A3 RID: 1955
			NotStarted,
			// Token: 0x040007A4 RID: 1956
			MoveAgents,
			// Token: 0x040007A5 RID: 1957
			FinalizeAgents,
			// Token: 0x040007A6 RID: 1958
			Completed
		}
	}
}
