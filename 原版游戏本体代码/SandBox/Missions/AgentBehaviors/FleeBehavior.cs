using System;
using System.Collections.Generic;
using System.Linq;
using SandBox.Missions.MissionLogics;
using SandBox.Objects.Usables;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.AgentBehaviors
{
	// Token: 0x020000A5 RID: 165
	public class FleeBehavior : AgentBehavior
	{
		// Token: 0x17000096 RID: 150
		// (get) Token: 0x060006F3 RID: 1779 RVA: 0x0002F6E1 File Offset: 0x0002D8E1
		// (set) Token: 0x060006F4 RID: 1780 RVA: 0x0002F6EC File Offset: 0x0002D8EC
		private FleeBehavior.FleeTargetType SelectedFleeTargetType
		{
			get
			{
				return this._selectedFleeTargetType;
			}
			set
			{
				if (value != this._selectedFleeTargetType)
				{
					this._selectedFleeTargetType = value;
					MBActionSet actionSet = base.OwnerAgent.ActionSet;
					ActionIndexCache currentAction = base.OwnerAgent.GetCurrentAction(1);
					if (this._selectedFleeTargetType != FleeBehavior.FleeTargetType.Cover && !actionSet.AreActionsAlternatives(currentAction, ActionIndexCache.act_scared_idle_1) && !actionSet.AreActionsAlternatives(currentAction, ActionIndexCache.act_scared_reaction_1))
					{
						base.OwnerAgent.SetActionChannel(1, ActionIndexCache.act_scared_reaction_1, false, (AnimFlags)0UL, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true);
					}
					if (this._selectedFleeTargetType == FleeBehavior.FleeTargetType.Cover)
					{
						this.BeAfraid();
					}
					this._selectedGoal.GoToTarget();
				}
			}
		}

		// Token: 0x060006F5 RID: 1781 RVA: 0x0002F79E File Offset: 0x0002D99E
		public FleeBehavior(AgentBehaviorGroup behaviorGroup)
			: base(behaviorGroup)
		{
			this._missionAgentHandler = base.Mission.GetMissionBehavior<MissionAgentHandler>();
			this._missionFightHandler = base.Mission.GetMissionBehavior<MissionFightHandler>();
			this._reconsiderFleeTargetTimer = new BasicMissionTimer();
			this._state = FleeBehavior.State.None;
		}

		// Token: 0x060006F6 RID: 1782 RVA: 0x0002F7DC File Offset: 0x0002D9DC
		public override void Tick(float dt, bool isSimulation)
		{
			switch (this._state)
			{
			case FleeBehavior.State.None:
				base.OwnerAgent.DisableScriptedMovement();
				base.OwnerAgent.SetActionChannel(1, ActionIndexCache.act_scared_reaction_1, false, (AnimFlags)0UL, 0f, 1f, -0.2f, 0.4f, MBRandom.RandomFloat, false, -0.2f, 0, true);
				this._selectedGoal = new FleeBehavior.FleeCoverTarget(base.Navigator, base.OwnerAgent);
				this.SelectedFleeTargetType = FleeBehavior.FleeTargetType.Cover;
				return;
			case FleeBehavior.State.Afraid:
				if (this._scareTimer.ElapsedTime > this._scareTime)
				{
					this._state = FleeBehavior.State.LookForPlace;
					this._scareTimer = null;
					return;
				}
				break;
			case FleeBehavior.State.LookForPlace:
				this.LookForPlace();
				return;
			case FleeBehavior.State.Flee:
				this.Flee();
				return;
			case FleeBehavior.State.Complain:
				if (this._complainToGuardTimer != null && this._complainToGuardTimer.ElapsedTime > 2f)
				{
					this._complainToGuardTimer = null;
					base.OwnerAgent.SetActionChannel(0, ActionIndexCache.act_none, false, (AnimFlags)0UL, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true);
					base.OwnerAgent.SetLookAgent(null);
					(this._selectedGoal as FleeBehavior.FleeAgentTarget).Savior.SetLookAgent(null);
					AlarmedBehaviorGroup.AlarmAgent((this._selectedGoal as FleeBehavior.FleeAgentTarget).Savior);
					this._state = FleeBehavior.State.LookForPlace;
				}
				break;
			default:
				return;
			}
		}

		// Token: 0x060006F7 RID: 1783 RVA: 0x0002F938 File Offset: 0x0002DB38
		private Vec3 GetDangerPosition()
		{
			Vec3 vec = Vec3.Zero;
			if (this._missionFightHandler != null)
			{
				IEnumerable<Agent> dangerSources = this._missionFightHandler.GetDangerSources(base.OwnerAgent);
				if (dangerSources.Any<Agent>())
				{
					foreach (Agent agent in dangerSources)
					{
						vec += agent.Position;
					}
					vec /= (float)dangerSources.Count<Agent>();
				}
			}
			return vec;
		}

		// Token: 0x060006F8 RID: 1784 RVA: 0x0002F9C0 File Offset: 0x0002DBC0
		private bool IsThereDanger()
		{
			return this._missionFightHandler != null && this._missionFightHandler.GetDangerSources(base.OwnerAgent).Any<Agent>();
		}

		// Token: 0x060006F9 RID: 1785 RVA: 0x0002F9E4 File Offset: 0x0002DBE4
		private float GetPathScore(WorldPosition startWorldPos, WorldPosition targetWorldPos)
		{
			float num = 1f;
			NavigationPath navigationPath = new NavigationPath();
			base.Mission.Scene.GetPathBetweenAIFaces(startWorldPos.GetNearestNavMesh(), targetWorldPos.GetNearestNavMesh(), startWorldPos.AsVec2, targetWorldPos.AsVec2, 0f, navigationPath, null);
			Vec2 asVec = this.GetDangerPosition().AsVec2;
			float toAngle = MBMath.WrapAngle((asVec - startWorldPos.AsVec2).RotationInRadians);
			float num2 = MathF.Abs(MBMath.GetSmallestDifferenceBetweenTwoAngles(MBMath.WrapAngle((navigationPath.Size > 0) ? (navigationPath.PathPoints[0] - startWorldPos.AsVec2).RotationInRadians : (targetWorldPos.AsVec2 - startWorldPos.AsVec2).RotationInRadians), toAngle)) / 3.1415927f * 1f;
			float num3 = startWorldPos.AsVec2.DistanceSquared(asVec);
			if (navigationPath.Size > 0)
			{
				float num4 = float.MaxValue;
				Vec2 line = startWorldPos.AsVec2;
				for (int i = 0; i < navigationPath.Size; i++)
				{
					float num5 = Vec2.DistanceToLineSegmentSquared(navigationPath.PathPoints[i], line, asVec);
					line = navigationPath.PathPoints[i];
					if (num5 < num4)
					{
						num4 = num5;
					}
				}
				if (num3 > num4 && num4 < 25f)
				{
					num = 1f * (num4 - num3) / 225f;
				}
				else if (num3 > 4f)
				{
					num = 1f * num4 / 225f;
				}
				else
				{
					num = 1f;
				}
			}
			float num6 = 1f * (225f / startWorldPos.AsVec2.DistanceSquared(targetWorldPos.AsVec2));
			return (1f + num2) * (1f + num2) - 2f + num + num6;
		}

		// Token: 0x060006FA RID: 1786 RVA: 0x0002FBBC File Offset: 0x0002DDBC
		private void LookForPlace()
		{
			FleeBehavior.FleeGoalBase selectedGoal = new FleeBehavior.FleeCoverTarget(base.Navigator, base.OwnerAgent);
			FleeBehavior.FleeTargetType selectedFleeTargetType = FleeBehavior.FleeTargetType.Cover;
			if (this.IsThereDanger())
			{
				List<ValueTuple<float, Agent>> availableGuardScores = this.GetAvailableGuardScores(5);
				List<ValueTuple<float, Passage>> availablePassageScores = this.GetAvailablePassageScores(10);
				float num = float.MinValue;
				foreach (ValueTuple<float, Passage> valueTuple in availablePassageScores)
				{
					float item = valueTuple.Item1;
					if (item > num)
					{
						num = item;
						selectedFleeTargetType = FleeBehavior.FleeTargetType.Indoor;
						selectedGoal = new FleeBehavior.FleePassageTarget(base.Navigator, base.OwnerAgent, valueTuple.Item2);
					}
				}
				foreach (ValueTuple<float, Agent> valueTuple2 in availableGuardScores)
				{
					float item2 = valueTuple2.Item1;
					if (item2 > num)
					{
						num = item2;
						selectedFleeTargetType = FleeBehavior.FleeTargetType.Guard;
						selectedGoal = new FleeBehavior.FleeAgentTarget(base.Navigator, base.OwnerAgent, valueTuple2.Item2);
					}
				}
			}
			this._selectedGoal = selectedGoal;
			this.SelectedFleeTargetType = selectedFleeTargetType;
			this._state = FleeBehavior.State.Flee;
		}

		// Token: 0x060006FB RID: 1787 RVA: 0x0002FCE0 File Offset: 0x0002DEE0
		private bool ShouldChangeTarget()
		{
			if (this._selectedFleeTargetType == FleeBehavior.FleeTargetType.Guard)
			{
				WorldPosition worldPosition = (this._selectedGoal as FleeBehavior.FleeAgentTarget).Savior.GetWorldPosition();
				WorldPosition worldPosition2 = base.OwnerAgent.GetWorldPosition();
				return this.GetPathScore(worldPosition2, worldPosition) <= 1f && this.IsThereASafePlaceToEscape();
			}
			if (this._selectedFleeTargetType != FleeBehavior.FleeTargetType.Indoor)
			{
				return true;
			}
			StandingPoint vacantStandingPointForAI = (this._selectedGoal as FleeBehavior.FleePassageTarget).EscapePortal.GetVacantStandingPointForAI(base.OwnerAgent);
			if (vacantStandingPointForAI == null)
			{
				return true;
			}
			WorldPosition worldPosition3 = base.OwnerAgent.GetWorldPosition();
			WorldPosition origin = vacantStandingPointForAI.GetUserFrameForAgent(base.OwnerAgent).Origin;
			return this.GetPathScore(worldPosition3, origin) <= 1f && this.IsThereASafePlaceToEscape();
		}

		// Token: 0x060006FC RID: 1788 RVA: 0x0002FD94 File Offset: 0x0002DF94
		private bool IsThereASafePlaceToEscape()
		{
			if (!this.GetAvailablePassageScores(1).Any((ValueTuple<float, Passage> d) => d.Item1 > 1f))
			{
				return this.GetAvailableGuardScores(1).Any((ValueTuple<float, Agent> d) => d.Item1 > 1f);
			}
			return true;
		}

		// Token: 0x060006FD RID: 1789 RVA: 0x0002FDFC File Offset: 0x0002DFFC
		private List<ValueTuple<float, Passage>> GetAvailablePassageScores(int maxPaths = 10)
		{
			WorldPosition worldPosition = base.OwnerAgent.GetWorldPosition();
			List<ValueTuple<float, Passage>> list = new List<ValueTuple<float, Passage>>();
			List<ValueTuple<float, Passage>> list2 = new List<ValueTuple<float, Passage>>();
			List<ValueTuple<WorldPosition, Passage>> list3 = new List<ValueTuple<WorldPosition, Passage>>();
			if (this._missionAgentHandler.TownPassageProps != null)
			{
				foreach (UsableMachine usableMachine in this._missionAgentHandler.TownPassageProps)
				{
					StandingPoint vacantStandingPointForAI = usableMachine.GetVacantStandingPointForAI(base.OwnerAgent);
					Passage passage = usableMachine as Passage;
					if (vacantStandingPointForAI != null && passage != null)
					{
						WorldPosition origin = vacantStandingPointForAI.GetUserFrameForAgent(base.OwnerAgent).Origin;
						list3.Add(new ValueTuple<WorldPosition, Passage>(origin, passage));
					}
				}
			}
			list3 = (from a in list3
				orderby base.OwnerAgent.Position.AsVec2.DistanceSquared(a.Item1.AsVec2)
				select a).ToList<ValueTuple<WorldPosition, Passage>>();
			foreach (ValueTuple<WorldPosition, Passage> valueTuple in list3)
			{
				WorldPosition item = valueTuple.Item1;
				if (item.IsValid && !(item.GetNearestNavMesh() == UIntPtr.Zero))
				{
					float pathScore = this.GetPathScore(worldPosition, item);
					ValueTuple<float, Passage> item2 = new ValueTuple<float, Passage>(pathScore, valueTuple.Item2);
					list.Add(item2);
					if (pathScore > 1f)
					{
						list2.Add(item2);
					}
					if (list2.Count >= maxPaths)
					{
						break;
					}
				}
			}
			if (list2.Count > 0)
			{
				return list2;
			}
			return list;
		}

		// Token: 0x060006FE RID: 1790 RVA: 0x0002FF78 File Offset: 0x0002E178
		private List<ValueTuple<float, Agent>> GetAvailableGuardScores(int maxGuards = 5)
		{
			WorldPosition worldPosition = base.OwnerAgent.GetWorldPosition();
			List<ValueTuple<float, Agent>> list = new List<ValueTuple<float, Agent>>();
			List<ValueTuple<float, Agent>> list2 = new List<ValueTuple<float, Agent>>();
			List<Agent> list3 = new List<Agent>();
			foreach (Agent agent in base.OwnerAgent.Team.ActiveAgents)
			{
				CharacterObject characterObject;
				if ((characterObject = agent.Character as CharacterObject) != null && agent.IsAIControlled && agent.CurrentWatchState != Agent.WatchState.Alarmed && (characterObject.Occupation == Occupation.Soldier || characterObject.Occupation == Occupation.Guard || characterObject.Occupation == Occupation.PrisonGuard))
				{
					list3.Add(agent);
				}
			}
			list3 = (from a in list3
				orderby base.OwnerAgent.Position.DistanceSquared(a.Position)
				select a).ToList<Agent>();
			foreach (Agent agent2 in list3)
			{
				WorldPosition worldPosition2 = agent2.GetWorldPosition();
				if (worldPosition2.IsValid)
				{
					float pathScore = this.GetPathScore(worldPosition, worldPosition2);
					ValueTuple<float, Agent> item = new ValueTuple<float, Agent>(pathScore, agent2);
					list.Add(item);
					if (pathScore > 1f)
					{
						list2.Add(item);
					}
					if (list2.Count >= maxGuards)
					{
						break;
					}
				}
			}
			if (list2.Count > 0)
			{
				return list2;
			}
			return list;
		}

		// Token: 0x060006FF RID: 1791 RVA: 0x000300E4 File Offset: 0x0002E2E4
		protected override void OnActivate()
		{
			base.OnActivate();
			this._state = FleeBehavior.State.None;
		}

		// Token: 0x06000700 RID: 1792 RVA: 0x000300F4 File Offset: 0x0002E2F4
		private void Flee()
		{
			if (this._selectedGoal.IsGoalAchievable())
			{
				if (this._selectedGoal.IsGoalAchieved())
				{
					this._selectedGoal.TargetReached();
					FleeBehavior.FleeTargetType selectedFleeTargetType = this.SelectedFleeTargetType;
					if (selectedFleeTargetType == FleeBehavior.FleeTargetType.Guard)
					{
						this._complainToGuardTimer = new BasicMissionTimer();
						this._state = FleeBehavior.State.Complain;
						return;
					}
					if (selectedFleeTargetType == FleeBehavior.FleeTargetType.Cover && this._reconsiderFleeTargetTimer.ElapsedTime > 0.5f)
					{
						this._state = FleeBehavior.State.LookForPlace;
						this._reconsiderFleeTargetTimer.Reset();
						return;
					}
				}
				else
				{
					if (this.SelectedFleeTargetType == FleeBehavior.FleeTargetType.Guard)
					{
						this._selectedGoal.GoToTarget();
					}
					if (this._reconsiderFleeTargetTimer.ElapsedTime > 1f)
					{
						this._reconsiderFleeTargetTimer.Reset();
						if (this.ShouldChangeTarget())
						{
							this._state = FleeBehavior.State.LookForPlace;
							return;
						}
					}
				}
			}
			else
			{
				this._state = FleeBehavior.State.LookForPlace;
			}
		}

		// Token: 0x06000701 RID: 1793 RVA: 0x000301BB File Offset: 0x0002E3BB
		private void BeAfraid()
		{
			this._scareTimer = new BasicMissionTimer();
			this._scareTime = 0.5f + MBRandom.RandomFloat * 0.5f;
			this._state = FleeBehavior.State.Afraid;
		}

		// Token: 0x06000702 RID: 1794 RVA: 0x000301E6 File Offset: 0x0002E3E6
		public override string GetDebugInfo()
		{
			return "Flee " + this._state;
		}

		// Token: 0x06000703 RID: 1795 RVA: 0x000301FD File Offset: 0x0002E3FD
		public override float GetAvailability(bool isSimulation)
		{
			if (base.Mission.CurrentTime < 3f)
			{
				return 0f;
			}
			if (!MissionFightHandler.IsAgentAggressive(base.OwnerAgent))
			{
				return 0.9f;
			}
			return 0.1f;
		}

		// Token: 0x040003A9 RID: 937
		public const float ScoreThreshold = 1f;

		// Token: 0x040003AA RID: 938
		public const float DangerDistance = 5f;

		// Token: 0x040003AB RID: 939
		public const float ImmediateDangerDistance = 2f;

		// Token: 0x040003AC RID: 940
		public const float DangerDistanceSquared = 25f;

		// Token: 0x040003AD RID: 941
		public const float ImmediateDangerDistanceSquared = 4f;

		// Token: 0x040003AE RID: 942
		private readonly MissionAgentHandler _missionAgentHandler;

		// Token: 0x040003AF RID: 943
		private readonly MissionFightHandler _missionFightHandler;

		// Token: 0x040003B0 RID: 944
		private FleeBehavior.State _state;

		// Token: 0x040003B1 RID: 945
		private readonly BasicMissionTimer _reconsiderFleeTargetTimer;

		// Token: 0x040003B2 RID: 946
		private const float ReconsiderImmobilizedFleeTargetTime = 0.5f;

		// Token: 0x040003B3 RID: 947
		private const float ReconsiderDefaultFleeTargetTime = 1f;

		// Token: 0x040003B4 RID: 948
		private FleeBehavior.FleeGoalBase _selectedGoal;

		// Token: 0x040003B5 RID: 949
		private BasicMissionTimer _scareTimer;

		// Token: 0x040003B6 RID: 950
		private float _scareTime;

		// Token: 0x040003B7 RID: 951
		private BasicMissionTimer _complainToGuardTimer;

		// Token: 0x040003B8 RID: 952
		private const float ComplainToGuardTime = 2f;

		// Token: 0x040003B9 RID: 953
		private FleeBehavior.FleeTargetType _selectedFleeTargetType;

		// Token: 0x020001A6 RID: 422
		private abstract class FleeGoalBase
		{
			// Token: 0x06000EF3 RID: 3827 RVA: 0x000666D8 File Offset: 0x000648D8
			protected FleeGoalBase(AgentNavigator navigator, Agent ownerAgent)
			{
				this._navigator = navigator;
				this._ownerAgent = ownerAgent;
			}

			// Token: 0x06000EF4 RID: 3828
			public abstract void TargetReached();

			// Token: 0x06000EF5 RID: 3829
			public abstract void GoToTarget();

			// Token: 0x06000EF6 RID: 3830
			public abstract bool IsGoalAchievable();

			// Token: 0x06000EF7 RID: 3831
			public abstract bool IsGoalAchieved();

			// Token: 0x040007CC RID: 1996
			protected readonly AgentNavigator _navigator;

			// Token: 0x040007CD RID: 1997
			protected readonly Agent _ownerAgent;
		}

		// Token: 0x020001A7 RID: 423
		private class FleeAgentTarget : FleeBehavior.FleeGoalBase
		{
			// Token: 0x1700012A RID: 298
			// (get) Token: 0x06000EF8 RID: 3832 RVA: 0x000666EE File Offset: 0x000648EE
			// (set) Token: 0x06000EF9 RID: 3833 RVA: 0x000666F6 File Offset: 0x000648F6
			public Agent Savior { get; private set; }

			// Token: 0x06000EFA RID: 3834 RVA: 0x000666FF File Offset: 0x000648FF
			public FleeAgentTarget(AgentNavigator navigator, Agent ownerAgent, Agent savior)
				: base(navigator, ownerAgent)
			{
				this.Savior = savior;
			}

			// Token: 0x06000EFB RID: 3835 RVA: 0x00066710 File Offset: 0x00064910
			public override void GoToTarget()
			{
				this._navigator.SetTargetFrame(this.Savior.GetWorldPosition(), this.Savior.Frame.rotation.f.AsVec2.RotationInRadians, 0.2f, 0.02f, Agent.AIScriptedFrameFlags.NoAttack | Agent.AIScriptedFrameFlags.NeverSlowDown, false);
			}

			// Token: 0x06000EFC RID: 3836 RVA: 0x00066768 File Offset: 0x00064968
			public override bool IsGoalAchievable()
			{
				return this.Savior.GetWorldPosition().GetNearestNavMesh() != UIntPtr.Zero && this._navigator.TargetPosition.IsValid && this.Savior.IsActive() && this.Savior.CurrentWatchState != Agent.WatchState.Alarmed;
			}

			// Token: 0x06000EFD RID: 3837 RVA: 0x000667CC File Offset: 0x000649CC
			public override bool IsGoalAchieved()
			{
				return this._navigator.TargetPosition.IsValid && this._navigator.TargetPosition.GetGroundVec3().Distance(this._ownerAgent.Position) <= this._ownerAgent.GetInteractionDistanceToUsable(this.Savior);
			}

			// Token: 0x06000EFE RID: 3838 RVA: 0x0006682C File Offset: 0x00064A2C
			public override void TargetReached()
			{
				this._ownerAgent.SetActionChannel(0, ActionIndexCache.act_cheer_1, true, (AnimFlags)0UL, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true);
				this._ownerAgent.SetActionChannel(1, ActionIndexCache.act_none, true, (AnimFlags)0UL, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true);
				this._ownerAgent.DisableScriptedMovement();
				this.Savior.DisableScriptedMovement();
				this.Savior.SetLookAgent(this._ownerAgent);
				this._ownerAgent.SetLookAgent(this.Savior);
			}
		}

		// Token: 0x020001A8 RID: 424
		private class FleePassageTarget : FleeBehavior.FleeGoalBase
		{
			// Token: 0x1700012B RID: 299
			// (get) Token: 0x06000EFF RID: 3839 RVA: 0x000668DD File Offset: 0x00064ADD
			// (set) Token: 0x06000F00 RID: 3840 RVA: 0x000668E5 File Offset: 0x00064AE5
			public Passage EscapePortal { get; private set; }

			// Token: 0x06000F01 RID: 3841 RVA: 0x000668EE File Offset: 0x00064AEE
			public FleePassageTarget(AgentNavigator navigator, Agent ownerAgent, Passage escapePortal)
				: base(navigator, ownerAgent)
			{
				this.EscapePortal = escapePortal;
			}

			// Token: 0x06000F02 RID: 3842 RVA: 0x000668FF File Offset: 0x00064AFF
			public override void GoToTarget()
			{
				this._navigator.SetTarget(this.EscapePortal, false, Agent.AIScriptedFrameFlags.None);
			}

			// Token: 0x06000F03 RID: 3843 RVA: 0x00066914 File Offset: 0x00064B14
			public override bool IsGoalAchievable()
			{
				return this.EscapePortal.GetVacantStandingPointForAI(this._ownerAgent) != null && !this.EscapePortal.IsDestroyed;
			}

			// Token: 0x06000F04 RID: 3844 RVA: 0x0006693C File Offset: 0x00064B3C
			public override bool IsGoalAchieved()
			{
				StandingPoint vacantStandingPointForAI = this.EscapePortal.GetVacantStandingPointForAI(this._ownerAgent);
				return vacantStandingPointForAI != null && vacantStandingPointForAI.IsUsableByAgent(this._ownerAgent);
			}

			// Token: 0x06000F05 RID: 3845 RVA: 0x0006696C File Offset: 0x00064B6C
			public override void TargetReached()
			{
			}
		}

		// Token: 0x020001A9 RID: 425
		private class FleePositionTarget : FleeBehavior.FleeGoalBase
		{
			// Token: 0x1700012C RID: 300
			// (get) Token: 0x06000F06 RID: 3846 RVA: 0x0006696E File Offset: 0x00064B6E
			// (set) Token: 0x06000F07 RID: 3847 RVA: 0x00066976 File Offset: 0x00064B76
			public Vec3 Position { get; private set; }

			// Token: 0x06000F08 RID: 3848 RVA: 0x0006697F File Offset: 0x00064B7F
			public FleePositionTarget(AgentNavigator navigator, Agent ownerAgent, Vec3 position)
				: base(navigator, ownerAgent)
			{
				this.Position = position;
			}

			// Token: 0x06000F09 RID: 3849 RVA: 0x00066990 File Offset: 0x00064B90
			public override void GoToTarget()
			{
			}

			// Token: 0x06000F0A RID: 3850 RVA: 0x00066994 File Offset: 0x00064B94
			public override bool IsGoalAchievable()
			{
				return this._navigator.TargetPosition.IsValid;
			}

			// Token: 0x06000F0B RID: 3851 RVA: 0x000669B4 File Offset: 0x00064BB4
			public override bool IsGoalAchieved()
			{
				return this._navigator.TargetPosition.IsValid && this._navigator.IsTargetReached();
			}

			// Token: 0x06000F0C RID: 3852 RVA: 0x000669E3 File Offset: 0x00064BE3
			public override void TargetReached()
			{
			}
		}

		// Token: 0x020001AA RID: 426
		private class FleeCoverTarget : FleeBehavior.FleeGoalBase
		{
			// Token: 0x06000F0D RID: 3853 RVA: 0x000669E5 File Offset: 0x00064BE5
			public FleeCoverTarget(AgentNavigator navigator, Agent ownerAgent)
				: base(navigator, ownerAgent)
			{
			}

			// Token: 0x06000F0E RID: 3854 RVA: 0x000669EF File Offset: 0x00064BEF
			public override void GoToTarget()
			{
				this._ownerAgent.DisableScriptedMovement();
			}

			// Token: 0x06000F0F RID: 3855 RVA: 0x000669FC File Offset: 0x00064BFC
			public override bool IsGoalAchievable()
			{
				return true;
			}

			// Token: 0x06000F10 RID: 3856 RVA: 0x000669FF File Offset: 0x00064BFF
			public override bool IsGoalAchieved()
			{
				return true;
			}

			// Token: 0x06000F11 RID: 3857 RVA: 0x00066A02 File Offset: 0x00064C02
			public override void TargetReached()
			{
			}
		}

		// Token: 0x020001AB RID: 427
		private enum State
		{
			// Token: 0x040007D2 RID: 2002
			None,
			// Token: 0x040007D3 RID: 2003
			Afraid,
			// Token: 0x040007D4 RID: 2004
			LookForPlace,
			// Token: 0x040007D5 RID: 2005
			Flee,
			// Token: 0x040007D6 RID: 2006
			Complain
		}

		// Token: 0x020001AC RID: 428
		private enum FleeTargetType
		{
			// Token: 0x040007D8 RID: 2008
			Indoor,
			// Token: 0x040007D9 RID: 2009
			Guard,
			// Token: 0x040007DA RID: 2010
			Cover
		}
	}
}
