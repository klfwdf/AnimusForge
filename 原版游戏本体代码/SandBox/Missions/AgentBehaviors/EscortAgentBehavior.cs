using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.AgentBehaviors
{
	// Token: 0x020000A3 RID: 163
	public class EscortAgentBehavior : AgentBehavior
	{
		// Token: 0x17000094 RID: 148
		// (get) Token: 0x060006DE RID: 1758 RVA: 0x0002ECCF File Offset: 0x0002CECF
		public Agent EscortedAgent
		{
			get
			{
				return this._escortedAgent;
			}
		}

		// Token: 0x17000095 RID: 149
		// (get) Token: 0x060006DF RID: 1759 RVA: 0x0002ECD7 File Offset: 0x0002CED7
		public Agent TargetAgent
		{
			get
			{
				return this._targetAgent;
			}
		}

		// Token: 0x060006E0 RID: 1760 RVA: 0x0002ECDF File Offset: 0x0002CEDF
		public EscortAgentBehavior(AgentBehaviorGroup behaviorGroup)
			: base(behaviorGroup)
		{
			this._targetAgent = null;
			this._escortedAgent = null;
			this._myLastStateWasRunning = false;
			this._initialMaxSpeedLimit = 1f;
		}

		// Token: 0x060006E1 RID: 1761 RVA: 0x0002ED08 File Offset: 0x0002CF08
		public void Initialize(Agent escortedAgent, Agent targetAgent, EscortAgentBehavior.OnTargetReachedDelegate onTargetReached = null)
		{
			this._escortedAgent = escortedAgent;
			this._targetAgent = targetAgent;
			this._targetMachine = null;
			this._targetPosition = null;
			this._onTargetReached = onTargetReached;
			this._escortFinished = false;
			this._initialMaxSpeedLimit = base.OwnerAgent.GetMaximumSpeedLimit();
			this._state = EscortAgentBehavior.State.Escorting;
		}

		// Token: 0x060006E2 RID: 1762 RVA: 0x0002ED5C File Offset: 0x0002CF5C
		public void Initialize(Agent escortedAgent, UsableMachine targetMachine, EscortAgentBehavior.OnTargetReachedDelegate onTargetReached = null)
		{
			this._escortedAgent = escortedAgent;
			this._targetAgent = null;
			this._targetMachine = targetMachine;
			this._targetPosition = null;
			this._onTargetReached = onTargetReached;
			this._escortFinished = false;
			this._initialMaxSpeedLimit = base.OwnerAgent.GetMaximumSpeedLimit();
			this._state = EscortAgentBehavior.State.Escorting;
		}

		// Token: 0x060006E3 RID: 1763 RVA: 0x0002EDB0 File Offset: 0x0002CFB0
		public void Initialize(Agent escortedAgent, Vec3? targetPosition, EscortAgentBehavior.OnTargetReachedDelegate onTargetReached = null)
		{
			this._escortedAgent = escortedAgent;
			this._targetAgent = null;
			this._targetMachine = null;
			this._targetPosition = targetPosition;
			this._onTargetReached = onTargetReached;
			this._escortFinished = false;
			this._initialMaxSpeedLimit = base.OwnerAgent.GetMaximumSpeedLimit();
			this._state = EscortAgentBehavior.State.Escorting;
		}

		// Token: 0x060006E4 RID: 1764 RVA: 0x0002EE00 File Offset: 0x0002D000
		public override void Tick(float dt, bool isSimulation)
		{
			if (this._escortedAgent == null || !this._escortedAgent.IsActive() || this._targetAgent == null || !this._targetAgent.IsActive())
			{
				this._state = EscortAgentBehavior.State.NotEscorting;
			}
			if (this._escortedAgent != null && this._state != EscortAgentBehavior.State.NotEscorting)
			{
				this.ControlMovement();
			}
		}

		// Token: 0x060006E5 RID: 1765 RVA: 0x0002EE54 File Offset: 0x0002D054
		public bool IsEscortFinished()
		{
			return this._escortFinished;
		}

		// Token: 0x060006E6 RID: 1766 RVA: 0x0002EE5C File Offset: 0x0002D05C
		private void ControlMovement()
		{
			int nearbyEnemyAgentCount = base.Mission.GetNearbyEnemyAgentCount(this._escortedAgent.Team, this._escortedAgent.Position.AsVec2, 5f);
			if (this._state != EscortAgentBehavior.State.NotEscorting && nearbyEnemyAgentCount > 0)
			{
				this._state = EscortAgentBehavior.State.NotEscorting;
				base.OwnerAgent.ResetLookAgent();
				base.Navigator.ClearTarget();
				base.OwnerAgent.DisableScriptedMovement();
				base.OwnerAgent.SetMaximumSpeedLimit(this._initialMaxSpeedLimit, false);
				Debug.Print("[Escort agent behavior] Escorted agent got into a fight... Disable!", 0, Debug.DebugColor.White, 17592186044416UL);
				return;
			}
			float rangeThreshold = (base.OwnerAgent.HasMount ? 2.2f : 1.2f);
			float num = base.OwnerAgent.Position.DistanceSquared(this._escortedAgent.Position);
			float num2;
			WorldPosition targetPosition;
			float targetRotation;
			if (this._targetAgent != null)
			{
				num2 = base.OwnerAgent.Position.DistanceSquared(this._targetAgent.Position);
				targetPosition = this._targetAgent.GetWorldPosition();
				MatrixFrame frame = this._targetAgent.Frame;
				targetRotation = frame.rotation.f.AsVec2.RotationInRadians;
			}
			else if (this._targetMachine != null)
			{
				MatrixFrame globalFrame = this._targetMachine.GameEntity.GetGlobalFrame();
				num2 = base.OwnerAgent.Position.DistanceSquared(globalFrame.origin);
				targetPosition = globalFrame.origin.ToWorldPosition();
				targetRotation = globalFrame.rotation.f.AsVec2.RotationInRadians;
			}
			else if (this._targetPosition != null)
			{
				num2 = base.OwnerAgent.Position.DistanceSquared(this._targetPosition.Value);
				targetPosition = this._targetPosition.Value.ToWorldPosition();
				targetRotation = (this._targetPosition.Value - base.OwnerAgent.Position).AsVec2.RotationInRadians;
			}
			else
			{
				Debug.FailedAssert("At least one target must be specified for the escort behavior.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\Missions\\AgentBehaviors\\EscortAgentBehavior.cs", "ControlMovement", 160);
				num2 = 0f;
				targetPosition = base.OwnerAgent.GetWorldPosition();
				targetRotation = 0f;
			}
			if (this._escortFinished)
			{
				bool flag = false;
				base.OwnerAgent.SetMaximumSpeedLimit(this._initialMaxSpeedLimit, false);
				if (this._onTargetReached != null)
				{
					flag = this._onTargetReached(base.OwnerAgent, ref this._escortedAgent, ref this._targetAgent, ref this._targetMachine, ref this._targetPosition);
				}
				if (flag && this._escortedAgent != null && (this._targetAgent != null || this._targetMachine != null || this._targetPosition != null))
				{
					this._state = EscortAgentBehavior.State.Escorting;
				}
				else
				{
					this._state = EscortAgentBehavior.State.NotEscorting;
				}
			}
			switch (this._state)
			{
			case EscortAgentBehavior.State.ReturnToEscortedAgent:
				if (num < 25f)
				{
					this._state = EscortAgentBehavior.State.Wait;
				}
				else
				{
					WorldPosition worldPosition = this._escortedAgent.GetWorldPosition();
					MatrixFrame frame = this._escortedAgent.Frame;
					this.SetMovePos(worldPosition, frame.rotation.f.AsVec2.RotationInRadians, rangeThreshold);
				}
				break;
			case EscortAgentBehavior.State.Wait:
				if (num < 25f)
				{
					this._state = EscortAgentBehavior.State.Escorting;
					Debug.Print("[Escort agent behavior] Escorting!", 0, Debug.DebugColor.White, 17592186044416UL);
				}
				else if (num > 100f)
				{
					this._state = EscortAgentBehavior.State.ReturnToEscortedAgent;
					Debug.Print("[Escort agent behavior] Escorted agent is too far away! Return to escorted agent!", 0, Debug.DebugColor.White, 17592186044416UL);
				}
				else
				{
					WorldPosition worldPosition2 = base.OwnerAgent.GetWorldPosition();
					MatrixFrame frame = base.OwnerAgent.Frame;
					this.SetMovePos(worldPosition2, frame.rotation.f.AsVec2.RotationInRadians, 0f);
				}
				break;
			case EscortAgentBehavior.State.Escorting:
				if (num >= 25f)
				{
					this._state = EscortAgentBehavior.State.Wait;
					Debug.Print("[Escort agent behavior] Stop walking! Wait", 0, Debug.DebugColor.White, 17592186044416UL);
				}
				else
				{
					this.SetMovePos(targetPosition, targetRotation, 3f);
				}
				break;
			}
			if (this._state == EscortAgentBehavior.State.Escorting && num2 < 16f && num < 16f && !Campaign.Current.ConversationManager.IsConversationInProgress)
			{
				this._escortFinished = true;
			}
		}

		// Token: 0x060006E7 RID: 1767 RVA: 0x0002F298 File Offset: 0x0002D498
		private void SetMovePos(WorldPosition targetPosition, float targetRotation, float rangeThreshold)
		{
			Agent.AIScriptedFrameFlags aiscriptedFrameFlags = Agent.AIScriptedFrameFlags.NoAttack;
			if (base.Navigator.CharacterHasVisiblePrefabs)
			{
				this._myLastStateWasRunning = false;
			}
			else
			{
				float num = base.OwnerAgent.Position.AsVec2.Distance(targetPosition.AsVec2);
				float length = this._escortedAgent.Velocity.AsVec2.Length;
				if (num - rangeThreshold <= 0.5f * (this._myLastStateWasRunning ? 1f : 1.2f) && length <= base.OwnerAgent.Monster.WalkingSpeedLimit * (this._myLastStateWasRunning ? 1f : 1.2f))
				{
					this._myLastStateWasRunning = false;
				}
				else
				{
					base.OwnerAgent.SetMaximumSpeedLimit(num - rangeThreshold + length, false);
					this._myLastStateWasRunning = true;
				}
			}
			if (!this._myLastStateWasRunning)
			{
				aiscriptedFrameFlags |= Agent.AIScriptedFrameFlags.DoNotRun;
			}
			base.Navigator.SetTargetFrame(targetPosition, targetRotation, rangeThreshold, -10f, aiscriptedFrameFlags, false);
		}

		// Token: 0x060006E8 RID: 1768 RVA: 0x0002F38B File Offset: 0x0002D58B
		public override float GetAvailability(bool isSimulation)
		{
			return (float)((this._state == EscortAgentBehavior.State.NotEscorting) ? 0 : 1);
		}

		// Token: 0x060006E9 RID: 1769 RVA: 0x0002F39C File Offset: 0x0002D59C
		protected override void OnDeactivate()
		{
			this._escortedAgent = null;
			this._targetAgent = null;
			this._targetMachine = null;
			this._targetPosition = null;
			this._onTargetReached = null;
			this._state = EscortAgentBehavior.State.NotEscorting;
			base.OwnerAgent.DisableScriptedMovement();
			base.OwnerAgent.ResetLookAgent();
			base.Navigator.ClearTarget();
		}

		// Token: 0x060006EA RID: 1770 RVA: 0x0002F3FC File Offset: 0x0002D5FC
		public override string GetDebugInfo()
		{
			return string.Concat(new object[]
			{
				"Escort ",
				this._escortedAgent.Name,
				" (id:",
				this._escortedAgent.Index,
				")",
				(this._targetAgent != null) ? string.Concat(new object[]
				{
					" to ",
					this._targetAgent.Name,
					" (id:",
					this._targetAgent.Index,
					")"
				}) : ((this._targetMachine != null) ? string.Concat(new object[]
				{
					" to ",
					this._targetMachine,
					"(id:",
					this._targetMachine.Id,
					")"
				}) : ((this._targetPosition != null) ? (" to position: " + this._targetPosition.Value) : " to NO TARGET"))
			});
		}

		// Token: 0x060006EB RID: 1771 RVA: 0x0002F51C File Offset: 0x0002D71C
		public static void AddEscortAgentBehavior(Agent ownerAgent, Agent targetAgent, EscortAgentBehavior.OnTargetReachedDelegate onTargetReached)
		{
			AgentNavigator agentNavigator = ownerAgent.GetComponent<CampaignAgentComponent>().AgentNavigator;
			InterruptingBehaviorGroup interruptingBehaviorGroup = ((agentNavigator != null) ? agentNavigator.GetBehaviorGroup<InterruptingBehaviorGroup>() : null);
			if (interruptingBehaviorGroup == null)
			{
				return;
			}
			bool flag = interruptingBehaviorGroup.GetBehavior<EscortAgentBehavior>() == null;
			EscortAgentBehavior escortAgentBehavior = interruptingBehaviorGroup.GetBehavior<EscortAgentBehavior>() ?? interruptingBehaviorGroup.AddBehavior<EscortAgentBehavior>();
			if (flag)
			{
				interruptingBehaviorGroup.SetScriptedBehavior<EscortAgentBehavior>();
			}
			escortAgentBehavior.Initialize(Agent.Main, targetAgent, onTargetReached);
		}

		// Token: 0x060006EC RID: 1772 RVA: 0x0002F574 File Offset: 0x0002D774
		public static void RemoveEscortBehaviorOfAgent(Agent ownerAgent)
		{
			AgentNavigator agentNavigator = ownerAgent.GetComponent<CampaignAgentComponent>().AgentNavigator;
			InterruptingBehaviorGroup interruptingBehaviorGroup = ((agentNavigator != null) ? agentNavigator.GetBehaviorGroup<InterruptingBehaviorGroup>() : null);
			if (interruptingBehaviorGroup == null)
			{
				return;
			}
			if (interruptingBehaviorGroup.GetBehavior<EscortAgentBehavior>() != null)
			{
				interruptingBehaviorGroup.RemoveBehavior<EscortAgentBehavior>();
			}
		}

		// Token: 0x060006ED RID: 1773 RVA: 0x0002F5AC File Offset: 0x0002D7AC
		public static bool CheckIfAgentIsEscortedBy(Agent ownerAgent, Agent escortedAgent)
		{
			AgentNavigator agentNavigator = ownerAgent.GetComponent<CampaignAgentComponent>().AgentNavigator;
			InterruptingBehaviorGroup interruptingBehaviorGroup = ((agentNavigator != null) ? agentNavigator.GetBehaviorGroup<InterruptingBehaviorGroup>() : null);
			EscortAgentBehavior escortAgentBehavior = ((interruptingBehaviorGroup != null) ? interruptingBehaviorGroup.GetBehavior<EscortAgentBehavior>() : null);
			return escortAgentBehavior != null && escortAgentBehavior.EscortedAgent == escortedAgent;
		}

		// Token: 0x0400039A RID: 922
		private const float StartWaitingDistanceSquared = 25f;

		// Token: 0x0400039B RID: 923
		private const float ReturnToEscortedAgentDistanceSquared = 100f;

		// Token: 0x0400039C RID: 924
		private const float EscortFinishedDistanceSquared = 16f;

		// Token: 0x0400039D RID: 925
		private const float TargetProximityThreshold = 3f;

		// Token: 0x0400039E RID: 926
		private const float MountedMoveProximityThreshold = 2.2f;

		// Token: 0x0400039F RID: 927
		private const float OnFootMoveProximityThreshold = 1.2f;

		// Token: 0x040003A0 RID: 928
		private EscortAgentBehavior.State _state;

		// Token: 0x040003A1 RID: 929
		private Agent _escortedAgent;

		// Token: 0x040003A2 RID: 930
		private Agent _targetAgent;

		// Token: 0x040003A3 RID: 931
		private UsableMachine _targetMachine;

		// Token: 0x040003A4 RID: 932
		private Vec3? _targetPosition;

		// Token: 0x040003A5 RID: 933
		private bool _myLastStateWasRunning;

		// Token: 0x040003A6 RID: 934
		private float _initialMaxSpeedLimit;

		// Token: 0x040003A7 RID: 935
		private EscortAgentBehavior.OnTargetReachedDelegate _onTargetReached;

		// Token: 0x040003A8 RID: 936
		private bool _escortFinished;

		// Token: 0x020001A4 RID: 420
		// (Invoke) Token: 0x06000EF0 RID: 3824
		public delegate bool OnTargetReachedDelegate(Agent agent, ref Agent escortedAgent, ref Agent targetAgent, ref UsableMachine targetMachine, ref Vec3? targetPosition);

		// Token: 0x020001A5 RID: 421
		private enum State
		{
			// Token: 0x040007C8 RID: 1992
			NotEscorting,
			// Token: 0x040007C9 RID: 1993
			ReturnToEscortedAgent,
			// Token: 0x040007CA RID: 1994
			Wait,
			// Token: 0x040007CB RID: 1995
			Escorting
		}
	}
}
