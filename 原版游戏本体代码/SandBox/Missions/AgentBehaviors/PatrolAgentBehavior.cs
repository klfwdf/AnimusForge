using System;
using System.Linq;
using SandBox.Objects;
using SandBox.Objects.Usables;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.AgentBehaviors
{
	// Token: 0x020000AA RID: 170
	public class PatrolAgentBehavior : AgentBehavior
	{
		// Token: 0x17000097 RID: 151
		// (get) Token: 0x06000725 RID: 1829 RVA: 0x00031480 File Offset: 0x0002F680
		private int NextPatrolIndex
		{
			get
			{
				int num = this._currentPatrolIndex + 1;
				if (num >= this._patrolPoints.Length)
				{
					num = 0;
				}
				return num;
			}
		}

		// Token: 0x06000726 RID: 1830 RVA: 0x000314A4 File Offset: 0x0002F6A4
		public PatrolAgentBehavior(AgentBehaviorGroup behaviorGroup)
			: base(behaviorGroup)
		{
		}

		// Token: 0x06000727 RID: 1831 RVA: 0x000314B0 File Offset: 0x0002F6B0
		public void SetDynamicPatrolArea(GameEntity parentPatrolPoint)
		{
			this._patrolPoints = new PatrolPoint[parentPatrolPoint.ChildCount];
			PatrolPoint[] array = new PatrolPoint[parentPatrolPoint.ChildCount];
			for (int i = 0; i < parentPatrolPoint.ChildCount; i++)
			{
				array[i] = parentPatrolPoint.GetChild(i).GetChild(0).GetFirstScriptOfType<PatrolPoint>();
			}
			this._patrolPoints = (from x in array
				orderby x.Index
				select x).ToArray<PatrolPoint>();
		}

		// Token: 0x06000728 RID: 1832 RVA: 0x00031530 File Offset: 0x0002F730
		protected override void OnActivate()
		{
			base.OwnerAgent.SetMaximumSpeedLimit(1.05f, false);
			this._infiniteWaitPointReached = false;
			PatrolPoint item = null;
			float num = float.MaxValue;
			foreach (PatrolPoint patrolPoint in this._patrolPoints)
			{
				float num2 = patrolPoint.GameEntity.GlobalPosition.DistanceSquared(base.OwnerAgent.Position);
				if (num2 < num)
				{
					num = num2;
					item = patrolPoint;
				}
			}
			this._currentPatrolIndex = this._patrolPoints.IndexOf(item);
			this.MoveAgentToThePoint(this._currentPatrolIndex, true, false);
		}

		// Token: 0x06000729 RID: 1833 RVA: 0x000315C8 File Offset: 0x0002F7C8
		protected override void OnDeactivate()
		{
			this._waitTimer = null;
			if (base.OwnerAgent.CurrentlyUsedGameObject != null)
			{
				base.OwnerAgent.StopUsingGameObjectMT(true, Agent.StopUsingGameObjectFlags.AutoAttachAfterStoppingUsingGameObject);
			}
			base.Navigator.SetTarget(null, false, Agent.AIScriptedFrameFlags.None);
			if (this._patrolPoints[this._currentPatrolIndex].GameEntity.GetFirstScriptOfType<PatrolPoint>().PatrollingSpeed != -1f || base.OwnerAgent.GetMaximumSpeedLimit().Equals(1.05f))
			{
				base.OwnerAgent.SetMaximumSpeedLimit(-1f, false);
			}
		}

		// Token: 0x0600072A RID: 1834 RVA: 0x00031658 File Offset: 0x0002F858
		public override void Tick(float dt, bool isSimulation)
		{
			if (!this._infiniteWaitPointReached && base.OwnerAgent.CurrentlyUsedGameObject != null)
			{
				if (this._waitTimer == null)
				{
					PatrolPoint patrolPoint;
					if ((patrolPoint = base.OwnerAgent.CurrentlyUsedGameObject as PatrolPoint) != null)
					{
						if (patrolPoint.IsInfiniteWaitPoint)
						{
							this._infiniteWaitPointReached = true;
							return;
						}
						float num = (float)patrolPoint.WaitDuration + MBRandom.RandomFloatRanged((float)(-(float)patrolPoint.WaitDeviation), (float)patrolPoint.WaitDeviation);
						if (num == 0f)
						{
							this.MoveAgentToNextPatrolPoint(isSimulation);
							return;
						}
						this._waitTimer = new Timer(base.Mission.CurrentTime, num, true);
						return;
					}
				}
				else if (this._waitTimer.Check(base.Mission.CurrentTime))
				{
					this.MoveAgentToNextPatrolPoint(isSimulation);
					return;
				}
			}
			else
			{
				if (base.Navigator.IsTargetReached())
				{
					base.Navigator.ClearTarget();
				}
				if (base.Navigator.TargetUsableMachine == null && !base.Navigator.TargetPosition.IsValid)
				{
					this.MoveAgentToNextPatrolPoint(isSimulation);
				}
			}
		}

		// Token: 0x0600072B RID: 1835 RVA: 0x00031756 File Offset: 0x0002F956
		public override float GetAvailability(bool isSimulation)
		{
			if (!base.OwnerAgent.IsAlarmed() && !base.OwnerAgent.IsPatrollingCautious())
			{
				return 0.5f;
			}
			return 0f;
		}

		// Token: 0x0600072C RID: 1836 RVA: 0x00031780 File Offset: 0x0002F980
		private void MoveAgentToNextPatrolPoint(bool isSimulation)
		{
			this._waitTimer = null;
			PatrolPoint firstScriptOfType = this._patrolPoints[this._currentPatrolIndex].GameEntity.GetFirstScriptOfType<PatrolPoint>();
			base.OwnerAgent.SetMaximumSpeedLimit((firstScriptOfType.PatrollingSpeed == -1f) ? 1.05f : firstScriptOfType.PatrollingSpeed, false);
			this.MoveAgentToThePoint(this.NextPatrolIndex, false, isSimulation);
			this._currentPatrolIndex = this.NextPatrolIndex;
		}

		// Token: 0x0600072D RID: 1837 RVA: 0x000317F0 File Offset: 0x0002F9F0
		private void MoveAgentToThePoint(int pointIndex, bool correctRotation, bool isSimulation)
		{
			WeakGameEntity gameEntity = this._patrolPoints[pointIndex].GameEntity;
			PatrolPoint firstScriptOfType = gameEntity.GetFirstScriptOfType<PatrolPoint>();
			if (firstScriptOfType.WaitDuration == 0 && firstScriptOfType.WaitDeviation == 0)
			{
				WorldPosition position = new WorldPosition(gameEntity.Scene, gameEntity.GlobalPosition);
				base.Navigator.SetTargetFrame(position, gameEntity.GetFrame().rotation.f.RotationX, correctRotation ? 1f : (-1f), correctRotation ? 0.8f : (-10f), Agent.AIScriptedFrameFlags.None, false);
				return;
			}
			base.Navigator.SetTarget(gameEntity.Parent.GetFirstScriptOfType<UsablePlace>(), isSimulation, Agent.AIScriptedFrameFlags.NeverSlowDown | Agent.AIScriptedFrameFlags.DoNotRun);
		}

		// Token: 0x0600072E RID: 1838 RVA: 0x0003189C File Offset: 0x0002FA9C
		public override string GetDebugInfo()
		{
			return "Patrol Agent Behavior";
		}

		// Token: 0x040003D3 RID: 979
		private const float DefaultPatrollingSpeed = 1.05f;

		// Token: 0x040003D4 RID: 980
		private PatrolPoint[] _patrolPoints;

		// Token: 0x040003D5 RID: 981
		private int _currentPatrolIndex;

		// Token: 0x040003D6 RID: 982
		private Timer _waitTimer;

		// Token: 0x040003D7 RID: 983
		private bool _infiniteWaitPointReached;
	}
}
