using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NetworkMessages.FromServer;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public abstract class UsableMachineAIBase
{
	protected readonly UsableMachine UsableMachine;

	private GameEntity _lastActiveWaitStandingPoint;

	public virtual bool HasActionCompleted => false;

	protected virtual MovementOrder NextOrder => MovementOrder.MovementOrderStop;

	protected UsableMachineAIBase(UsableMachine usableMachine)
	{
		UsableMachine = usableMachine;
		_lastActiveWaitStandingPoint = UsableMachine.WaitEntity;
	}

	protected internal virtual Agent.AIScriptedFrameFlags GetScriptedFrameFlags(Agent agent)
	{
		return Agent.AIScriptedFrameFlags.None;
	}

	public void Tick(Agent agentToCompareTo, Formation formationToCompareTo, Team potentialUsersTeam, float dt)
	{
		OnTick(agentToCompareTo, formationToCompareTo, potentialUsersTeam, dt);
	}

	protected virtual void OnTick(Agent agentToCompareTo, Formation formationToCompareTo, Team potentialUsersTeam, float dt)
	{
		foreach (StandingPoint standingPoint in UsableMachine.StandingPoints)
		{
			Agent userAgent = standingPoint.UserAgent;
			if ((agentToCompareTo == null || userAgent == agentToCompareTo) && userAgent != null && userAgent.IsAIControlled && (formationToCompareTo == null || userAgent.Formation == formationToCompareTo) && (HasActionCompleted || (potentialUsersTeam != null && UsableMachine.IsDisabledForBattleSideAI(potentialUsersTeam.Side)) || userAgent.IsRunningAway))
			{
				HandleAgentStopUsingStandingPoint(userAgent, standingPoint);
			}
			if (standingPoint.HasAIMovingTo)
			{
				Agent movingAgent = standingPoint.MovingAgent;
				if ((agentToCompareTo == null || movingAgent == agentToCompareTo) && (formationToCompareTo == null || (movingAgent != null && movingAgent.IsAIControlled && movingAgent.Formation == formationToCompareTo)))
				{
					if (standingPoint.IsDeactivated || HasActionCompleted || (potentialUsersTeam != null && UsableMachine.IsDisabledForBattleSideAI(potentialUsersTeam.Side)) || movingAgent.IsRunningAway)
					{
						HandleAgentStopUsingStandingPoint(movingAgent, standingPoint);
					}
					else
					{
						if (standingPoint.HasAlternative() && UsableMachine.IsInRangeToCheckAlternativePoints(movingAgent))
						{
							StandingPoint bestPointAlternativeTo = UsableMachine.GetBestPointAlternativeTo(standingPoint, movingAgent);
							if (bestPointAlternativeTo != standingPoint)
							{
								standingPoint.OnMoveToStopped(movingAgent);
								movingAgent.AIMoveToGameObjectEnable(bestPointAlternativeTo, UsableMachine, GetScriptedFrameFlags(movingAgent));
								if (standingPoint == UsableMachine.CurrentlyUsedAmmoPickUpPoint)
								{
									UsableMachine.CurrentlyUsedAmmoPickUpPoint = bestPointAlternativeTo;
								}
								continue;
							}
						}
						if ((standingPoint.LockUserFrames || standingPoint.LockUserPositions) && standingPoint.HasUserPositionsChanged(movingAgent))
						{
							WorldFrame userFrameForAgent = standingPoint.GetUserFrameForAgent(movingAgent);
							movingAgent.SetScriptedPositionAndDirection(ref userFrameForAgent.Origin, userFrameForAgent.Rotation.f.AsVec2.RotationInRadians, addHumanLikeDelay: false, GetScriptedFrameFlags(movingAgent));
						}
						if (!standingPoint.IsDisabled && !standingPoint.HasUser && !movingAgent.IsPaused && movingAgent.CanReachAndUseObject(standingPoint, standingPoint.GetUserFrameForAgent(movingAgent).Origin.GetGroundVec3().DistanceSquared(movingAgent.Position)))
						{
							movingAgent.UseGameObject(standingPoint);
							movingAgent.SetScriptedFlags(movingAgent.GetScriptedFlags() & ~standingPoint.DisableScriptedFrameFlags);
						}
					}
				}
			}
			for (int num = standingPoint.GetDefendingAgentCount() - 1; num >= 0; num--)
			{
				Agent agent = standingPoint.DefendingAgents[num];
				if ((agentToCompareTo == null || agent == agentToCompareTo) && (formationToCompareTo == null || (agent != null && agent.IsAIControlled && agent.Formation == formationToCompareTo)) && (HasActionCompleted || (potentialUsersTeam != null && !UsableMachine.IsDisabledForBattleSideAI(potentialUsersTeam.Side)) || agent.IsRunningAway))
				{
					HandleAgentStopUsingStandingPoint(agent, standingPoint);
				}
			}
		}
		if (!(_lastActiveWaitStandingPoint != UsableMachine.WaitEntity))
		{
			return;
		}
		foreach (Formation item in potentialUsersTeam.FormationsIncludingSpecialAndEmpty.Where((Formation f) => f.CountOfUnits > 0 && UsableMachine.IsUsedByFormation(f) && f.GetReadonlyMovementOrderReference().OrderEnum == MovementOrder.MovementOrderEnum.FollowEntity && f.GetReadonlyMovementOrderReference().TargetEntity == _lastActiveWaitStandingPoint))
		{
			if (this is SiegeTowerAI)
			{
				item.SetMovementOrder(NextOrder);
			}
			else
			{
				item.SetMovementOrder(MovementOrder.MovementOrderFollowEntity(UsableMachine.WaitEntity));
			}
		}
		_lastActiveWaitStandingPoint = UsableMachine.WaitEntity;
	}

	[Conditional("DEBUG")]
	private void TickForDebug()
	{
		if (!Input.DebugInput.IsHotKeyDown("UsableMachineAiBaseHotkeyShowMachineUsers"))
		{
			return;
		}
		foreach (StandingPoint standingPoint in UsableMachine.StandingPoints)
		{
			_ = standingPoint.HasAIMovingTo;
			_ = standingPoint.UserAgent;
		}
	}

	public static Agent GetSuitableAgentForStandingPoint(UsableMachine usableMachine, StandingPoint standingPoint, IEnumerable<Agent> agents, List<Agent> usedAgents)
	{
		if (usableMachine.AmmoPickUpPoints.Contains(standingPoint) && usableMachine.StandingPoints.Any((StandingPoint standingPoint2) => (standingPoint2.IsDeactivated || standingPoint2.HasUser || standingPoint2.HasAIMovingTo) && !standingPoint2.GameEntity.HasTag(usableMachine.AmmoPickUpTag) && standingPoint2 is StandingPointWithWeaponRequirement))
		{
			return null;
		}
		IEnumerable<Agent> source = agents.Where((Agent a) => !usedAgents.Contains(a) && a.IsAIControlled && a.IsActive() && !a.IsRunningAway && !a.InteractingWithAnyGameObject() && !standingPoint.IsDisabledForAgent(a) && (a.Formation == null || !a.IsDetachedFromFormation));
		if (!source.Any())
		{
			return null;
		}
		return TaleWorlds.Core.Extensions.MaxBy(source, (Agent a) => standingPoint.GetUsageScoreForAgent(a));
	}

	public static Agent GetSuitableAgentForStandingPoint(UsableMachine usableMachine, StandingPoint standingPoint, List<(Agent, float)> agents, List<Agent> usedAgents, float weight)
	{
		if (usableMachine.IsStandingPointNotUsedOnAccountOfBeingAmmoLoad(standingPoint))
		{
			return null;
		}
		Agent result = null;
		float num = float.MinValue;
		foreach (var agent in agents)
		{
			Agent item = agent.Item1;
			if (!usedAgents.Contains(item) && item.IsAIControlled && item.IsActive() && !item.IsRunningAway && !item.InteractingWithAnyGameObject() && !standingPoint.IsDisabledForAgent(item) && (item.Formation == null || !item.IsDetachedFromFormation || item.DetachmentWeight * 0.4f > weight))
			{
				float usageScoreForAgent = standingPoint.GetUsageScoreForAgent(item);
				if (num < usageScoreForAgent)
				{
					num = usageScoreForAgent;
					result = item;
				}
			}
		}
		return result;
	}

	public virtual void TeleportUserAgentsToMachine(List<Agent> agentList)
	{
		int num = 0;
		bool flag;
		do
		{
			num++;
			flag = false;
			foreach (Agent agent in agentList)
			{
				if (!agent.IsAIControlled || !agent.AIMoveToGameObjectIsEnabled())
				{
					continue;
				}
				flag = true;
				WorldFrame userFrameForAgent = UsableMachine.GetTargetStandingPointOfAIAgent(agent).GetUserFrameForAgent(agent);
				Vec2 direction = userFrameForAgent.Rotation.f.AsVec2.Normalized();
				if ((agent.Position.AsVec2 - userFrameForAgent.Origin.AsVec2).LengthSquared > 0.0001f || (agent.GetMovementDirection() - direction).LengthSquared > 0.0001f)
				{
					agent.TeleportToPosition(userFrameForAgent.Origin.GetGroundVec3());
					agent.SetMovementDirection(in direction);
					if (GameNetwork.IsServerOrRecorder)
					{
						GameNetwork.BeginBroadcastModuleEvent();
						GameNetwork.WriteMessage(new AgentTeleportToFrame(agent.Index, userFrameForAgent.Origin.GetGroundVec3(), direction));
						GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord);
					}
				}
			}
		}
		while (flag && num < 10);
	}

	public void StopUsingStandingPoint(StandingPoint standingPoint)
	{
		Agent agent = (standingPoint.HasUser ? standingPoint.UserAgent : (standingPoint.HasAIMovingTo ? standingPoint.MovingAgent : null));
		HandleAgentStopUsingStandingPoint(agent, standingPoint);
	}

	protected Agent.StopUsingGameObjectFlags GetStopUsingStandingPointFlags(Agent agent, StandingPoint standingPoint)
	{
		Agent.StopUsingGameObjectFlags stopUsingGameObjectFlags = Agent.StopUsingGameObjectFlags.None;
		if (agent.Team == null || agent.IsRunningAway)
		{
			stopUsingGameObjectFlags |= Agent.StopUsingGameObjectFlags.AutoAttachAfterStoppingUsingGameObject;
		}
		else
		{
			if (UsableMachine.AutoAttachUserToFormation(agent.Team.Side))
			{
				stopUsingGameObjectFlags |= Agent.StopUsingGameObjectFlags.AutoAttachAfterStoppingUsingGameObject;
			}
			if (UsableMachine.HasToBeDefendedByUser(agent.Team.Side))
			{
				stopUsingGameObjectFlags |= Agent.StopUsingGameObjectFlags.DefendAfterStoppingUsingGameObject;
			}
		}
		return stopUsingGameObjectFlags;
	}

	protected virtual void HandleAgentStopUsingStandingPoint(Agent agent, StandingPoint standingPoint)
	{
		agent.StopUsingGameObjectMT(isSuccessful: true, GetStopUsingStandingPointFlags(agent, standingPoint));
	}
}
