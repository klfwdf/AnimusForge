using System;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.AgentBehaviors
{
	// Token: 0x020000AC RID: 172
	public class ScriptBehavior : AgentBehavior
	{
		// Token: 0x06000734 RID: 1844 RVA: 0x000319AD File Offset: 0x0002FBAD
		public ScriptBehavior(AgentBehaviorGroup behaviorGroup)
			: base(behaviorGroup)
		{
		}

		// Token: 0x06000735 RID: 1845 RVA: 0x000319CC File Offset: 0x0002FBCC
		public static void AddUsableMachineTarget(Agent ownerAgent, UsableMachine targetUsableMachine)
		{
			DailyBehaviorGroup behaviorGroup = ownerAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<DailyBehaviorGroup>();
			ScriptBehavior scriptBehavior = behaviorGroup.GetBehavior<ScriptBehavior>() ?? behaviorGroup.AddBehavior<ScriptBehavior>();
			bool flag = behaviorGroup.ScriptedBehavior != scriptBehavior;
			scriptBehavior._targetUsableMachine = targetUsableMachine;
			scriptBehavior._state = ScriptBehavior.State.GoToUsableMachine;
			scriptBehavior._sentToTarget = false;
			if (flag)
			{
				behaviorGroup.SetScriptedBehavior<ScriptBehavior>();
			}
		}

		// Token: 0x06000736 RID: 1846 RVA: 0x00031A24 File Offset: 0x0002FC24
		public static void AddAgentTarget(Agent ownerAgent, Agent targetAgent)
		{
			DailyBehaviorGroup behaviorGroup = ownerAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<DailyBehaviorGroup>();
			ScriptBehavior scriptBehavior = behaviorGroup.GetBehavior<ScriptBehavior>() ?? behaviorGroup.AddBehavior<ScriptBehavior>();
			bool flag = behaviorGroup.ScriptedBehavior != scriptBehavior;
			scriptBehavior._targetAgent = targetAgent;
			scriptBehavior._state = ScriptBehavior.State.GoToAgent;
			scriptBehavior._sentToTarget = false;
			if (flag)
			{
				behaviorGroup.SetScriptedBehavior<ScriptBehavior>();
			}
		}

		// Token: 0x06000737 RID: 1847 RVA: 0x00031A7C File Offset: 0x0002FC7C
		public static void AddWorldFrameTarget(Agent ownerAgent, WorldFrame targetWorldFrame)
		{
			DailyBehaviorGroup behaviorGroup = ownerAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<DailyBehaviorGroup>();
			ScriptBehavior scriptBehavior = behaviorGroup.GetBehavior<ScriptBehavior>() ?? behaviorGroup.AddBehavior<ScriptBehavior>();
			bool flag = behaviorGroup.ScriptedBehavior != scriptBehavior;
			scriptBehavior._targetFrame = targetWorldFrame;
			scriptBehavior._state = ScriptBehavior.State.GoToTargetFrame;
			scriptBehavior._sentToTarget = false;
			if (flag)
			{
				behaviorGroup.SetScriptedBehavior<ScriptBehavior>();
			}
		}

		// Token: 0x06000738 RID: 1848 RVA: 0x00031AD4 File Offset: 0x0002FCD4
		public static void AddTargetWithDelegate(Agent ownerAgent, ScriptBehavior.SelectTargetDelegate selectTargetDelegate, ScriptBehavior.OnTargetReachedWaitDelegate onTargetReachWaitDelegate, ScriptBehavior.OnTargetReachedDelegate onTargetReachedDelegate, float initialWaitInSeconds = 0f)
		{
			DailyBehaviorGroup behaviorGroup = ownerAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<DailyBehaviorGroup>();
			ScriptBehavior scriptBehavior = behaviorGroup.GetBehavior<ScriptBehavior>() ?? behaviorGroup.AddBehavior<ScriptBehavior>();
			bool flag = behaviorGroup.ScriptedBehavior != scriptBehavior;
			scriptBehavior._selectTargetDelegate = selectTargetDelegate;
			scriptBehavior._onTargetReachedDelegate = onTargetReachedDelegate;
			scriptBehavior._onTargetReachWaitDelegate = onTargetReachWaitDelegate;
			scriptBehavior._initialWaitInSeconds = initialWaitInSeconds;
			scriptBehavior._isInitiallyWaiting = initialWaitInSeconds > 0f;
			scriptBehavior._state = ScriptBehavior.State.NoTarget;
			scriptBehavior._sentToTarget = false;
			if (flag)
			{
				behaviorGroup.SetScriptedBehavior<ScriptBehavior>();
			}
		}

		// Token: 0x06000739 RID: 1849 RVA: 0x00031B51 File Offset: 0x0002FD51
		public bool IsNearTarget(Agent targetAgent)
		{
			return this._targetAgent == targetAgent && (this._state == ScriptBehavior.State.NearAgent || this._state == ScriptBehavior.State.NearStationaryTarget);
		}

		// Token: 0x0600073A RID: 1850 RVA: 0x00031B74 File Offset: 0x0002FD74
		public override void Tick(float dt, bool isSimulation)
		{
			if (this._isInitiallyWaiting)
			{
				if (this._waitTimer == null)
				{
					this._waitTimer = new MissionTimer(this._initialWaitInSeconds);
					return;
				}
				if (this._waitTimer.Check(false))
				{
					this._isInitiallyWaiting = false;
					this._waitTimer = null;
					return;
				}
			}
			else
			{
				if (this._state == ScriptBehavior.State.NoTarget)
				{
					if (this._selectTargetDelegate == null)
					{
						if (this.BehaviorGroup.ScriptedBehavior == this)
						{
							this.BehaviorGroup.DisableScriptedBehavior();
						}
						return;
					}
					this.SearchForNewTarget();
				}
				switch (this._state)
				{
				case ScriptBehavior.State.GoToUsableMachine:
					if (!this._sentToTarget)
					{
						base.Navigator.SetTarget(this._targetUsableMachine, false, Agent.AIScriptedFrameFlags.None);
						this._sentToTarget = true;
						return;
					}
					if (base.OwnerAgent.IsUsingGameObject && base.OwnerAgent.Position.DistanceSquared(this._targetUsableMachine.GameEntity.GetGlobalFrame().origin) < 1f)
					{
						if (this.CheckForSearchNewTarget(ScriptBehavior.State.NearStationaryTarget))
						{
							base.OwnerAgent.StopUsingGameObject(false, Agent.StopUsingGameObjectFlags.AutoAttachAfterStoppingUsingGameObject);
							return;
						}
						this.RemoveTargets();
						return;
					}
					break;
				case ScriptBehavior.State.GoToAgent:
				{
					float interactionDistanceToUsable = base.OwnerAgent.GetInteractionDistanceToUsable(this._targetAgent);
					if (base.OwnerAgent.Position.DistanceSquared(this._targetAgent.Position) >= interactionDistanceToUsable * interactionDistanceToUsable)
					{
						AgentNavigator navigator = base.Navigator;
						WorldPosition worldPosition = this._targetAgent.GetWorldPosition();
						MatrixFrame frame = this._targetAgent.Frame;
						navigator.SetTargetFrame(worldPosition, frame.rotation.f.AsVec2.RotationInRadians, this._customTargetReachedRangeThreshold, this._customTargetReachedRotationThreshold, Agent.AIScriptedFrameFlags.None, false);
						return;
					}
					if (!this.CheckForSearchNewTarget(ScriptBehavior.State.NearAgent))
					{
						AgentNavigator navigator2 = base.Navigator;
						WorldPosition worldPosition2 = base.OwnerAgent.GetWorldPosition();
						MatrixFrame frame = base.OwnerAgent.Frame;
						navigator2.SetTargetFrame(worldPosition2, frame.rotation.f.AsVec2.RotationInRadians, this._customTargetReachedRangeThreshold, this._customTargetReachedRotationThreshold, Agent.AIScriptedFrameFlags.None, false);
						this.RemoveTargets();
						return;
					}
					break;
				}
				case ScriptBehavior.State.GoToTargetFrame:
					if (!this._sentToTarget)
					{
						base.Navigator.SetTargetFrame(this._targetFrame.Origin, this._targetFrame.Rotation.f.AsVec2.RotationInRadians, this._customTargetReachedRangeThreshold, this._customTargetReachedRotationThreshold, Agent.AIScriptedFrameFlags.DoNotRun, false);
						this._sentToTarget = true;
						return;
					}
					if (base.Navigator.IsTargetReached() && !this.CheckForSearchNewTarget(ScriptBehavior.State.NearStationaryTarget) && this._waitTimer == null)
					{
						this.RemoveTargets();
						return;
					}
					break;
				case ScriptBehavior.State.NearAgent:
				{
					if (base.OwnerAgent.Position.DistanceSquared(this._targetAgent.Position) >= 1f)
					{
						this._state = ScriptBehavior.State.GoToAgent;
						return;
					}
					AgentNavigator navigator3 = base.Navigator;
					WorldPosition worldPosition3 = base.OwnerAgent.GetWorldPosition();
					MatrixFrame frame = base.OwnerAgent.Frame;
					navigator3.SetTargetFrame(worldPosition3, frame.rotation.f.AsVec2.RotationInRadians, this._customTargetReachedRangeThreshold, this._customTargetReachedRotationThreshold, Agent.AIScriptedFrameFlags.None, false);
					this.RemoveTargets();
					break;
				}
				default:
					return;
				}
			}
		}

		// Token: 0x0600073B RID: 1851 RVA: 0x00031E7C File Offset: 0x0003007C
		private bool CheckForSearchNewTarget(ScriptBehavior.State endState)
		{
			bool flag = false;
			bool flag2 = false;
			if (this._onTargetReachWaitDelegate != null && !this._isWaiting)
			{
				this._onTargetReachWaitDelegate(base.OwnerAgent, ref this._waitTimeInSeconds);
				this._isWaiting = this._waitTimeInSeconds > 0f;
			}
			if (this._isWaiting)
			{
				if (this._waitTimer == null)
				{
					this._waitTimer = new MissionTimer(this._waitTimeInSeconds);
				}
				else if (this._waitTimer.Check(false))
				{
					this._isWaiting = false;
					this._waitTimer = null;
					flag = true;
				}
			}
			else
			{
				flag = true;
			}
			if (flag)
			{
				if (this._onTargetReachedDelegate != null)
				{
					flag2 = this._onTargetReachedDelegate(base.OwnerAgent, ref this._targetAgent, ref this._targetUsableMachine, ref this._targetFrame);
				}
				if (flag2)
				{
					this.SearchForNewTarget();
				}
				else
				{
					this._state = endState;
				}
				return flag2;
			}
			return false;
		}

		// Token: 0x0600073C RID: 1852 RVA: 0x00031F50 File Offset: 0x00030150
		private void SearchForNewTarget()
		{
			Agent agent = null;
			UsableMachine usableMachine = null;
			WorldFrame invalid = WorldFrame.Invalid;
			float customTargetReachedRangeThreshold = this._customTargetReachedRangeThreshold;
			float customTargetReachedRotationThreshold = this._customTargetReachedRotationThreshold;
			if (this._selectTargetDelegate(base.OwnerAgent, ref agent, ref usableMachine, ref invalid, ref customTargetReachedRangeThreshold, ref customTargetReachedRotationThreshold))
			{
				if (agent != null)
				{
					this._targetAgent = agent;
					this._state = ScriptBehavior.State.GoToAgent;
					this._sentToTarget = false;
				}
				else if (usableMachine != null)
				{
					this._targetUsableMachine = usableMachine;
					this._state = ScriptBehavior.State.GoToUsableMachine;
					this._sentToTarget = false;
				}
				else
				{
					this._targetFrame = invalid;
					this._state = ScriptBehavior.State.GoToTargetFrame;
					this._sentToTarget = false;
				}
				this._customTargetReachedRangeThreshold = customTargetReachedRangeThreshold;
				this._customTargetReachedRotationThreshold = customTargetReachedRotationThreshold;
			}
		}

		// Token: 0x0600073D RID: 1853 RVA: 0x00031FEB File Offset: 0x000301EB
		public override float GetAvailability(bool isSimulation)
		{
			return (float)((this._state == ScriptBehavior.State.NoTarget) ? 0 : 1);
		}

		// Token: 0x0600073E RID: 1854 RVA: 0x00031FFA File Offset: 0x000301FA
		protected override void OnDeactivate()
		{
			base.Navigator.ClearTarget();
			this.RemoveTargets();
		}

		// Token: 0x0600073F RID: 1855 RVA: 0x0003200D File Offset: 0x0003020D
		private void RemoveTargets()
		{
			this._targetUsableMachine = null;
			this._targetAgent = null;
			this._targetFrame = WorldFrame.Invalid;
			this._state = ScriptBehavior.State.NoTarget;
			this._selectTargetDelegate = null;
			this._onTargetReachedDelegate = null;
			this._sentToTarget = false;
		}

		// Token: 0x06000740 RID: 1856 RVA: 0x00032044 File Offset: 0x00030244
		public override string GetDebugInfo()
		{
			return "Scripted";
		}

		// Token: 0x040003DA RID: 986
		private UsableMachine _targetUsableMachine;

		// Token: 0x040003DB RID: 987
		private Agent _targetAgent;

		// Token: 0x040003DC RID: 988
		private WorldFrame _targetFrame;

		// Token: 0x040003DD RID: 989
		private ScriptBehavior.State _state;

		// Token: 0x040003DE RID: 990
		private bool _sentToTarget;

		// Token: 0x040003DF RID: 991
		private float _waitTimeInSeconds;

		// Token: 0x040003E0 RID: 992
		private bool _isWaiting;

		// Token: 0x040003E1 RID: 993
		private MissionTimer _waitTimer;

		// Token: 0x040003E2 RID: 994
		private float _customTargetReachedRangeThreshold = 1f;

		// Token: 0x040003E3 RID: 995
		private float _customTargetReachedRotationThreshold = 1f;

		// Token: 0x040003E4 RID: 996
		private float _initialWaitInSeconds;

		// Token: 0x040003E5 RID: 997
		private bool _isInitiallyWaiting;

		// Token: 0x040003E6 RID: 998
		private ScriptBehavior.SelectTargetDelegate _selectTargetDelegate;

		// Token: 0x040003E7 RID: 999
		private ScriptBehavior.OnTargetReachedDelegate _onTargetReachedDelegate;

		// Token: 0x040003E8 RID: 1000
		private ScriptBehavior.OnTargetReachedWaitDelegate _onTargetReachWaitDelegate;

		// Token: 0x020001B1 RID: 433
		// (Invoke) Token: 0x06000F1E RID: 3870
		public delegate bool SelectTargetDelegate(Agent agent, ref Agent targetAgent, ref UsableMachine targetUsableMachine, ref WorldFrame targetFrame, ref float customTargetReachedRangeThreshold, ref float customTargetReachedRotationThreshold);

		// Token: 0x020001B2 RID: 434
		// (Invoke) Token: 0x06000F22 RID: 3874
		public delegate bool OnTargetReachedDelegate(Agent agent, ref Agent targetAgent, ref UsableMachine targetUsableMachine, ref WorldFrame targetFrame);

		// Token: 0x020001B3 RID: 435
		// (Invoke) Token: 0x06000F26 RID: 3878
		public delegate void OnTargetReachedWaitDelegate(Agent agent, ref float waitTimeInSeconds);

		// Token: 0x020001B4 RID: 436
		private enum State
		{
			// Token: 0x040007E8 RID: 2024
			NoTarget,
			// Token: 0x040007E9 RID: 2025
			GoToUsableMachine,
			// Token: 0x040007EA RID: 2026
			GoToAgent,
			// Token: 0x040007EB RID: 2027
			GoToTargetFrame,
			// Token: 0x040007EC RID: 2028
			NearAgent,
			// Token: 0x040007ED RID: 2029
			NearStationaryTarget
		}
	}
}
