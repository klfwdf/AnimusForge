using System.Diagnostics;
using SandBox.Objects;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.AI;

public class PassageAI : UsableMachineAIBase
{
	public PassageAI(UsableMachine usableMachine)
		: base(usableMachine)
	{
	}

	protected override Agent.AIScriptedFrameFlags GetScriptedFrameFlags(Agent agent)
	{
		if (agent.CurrentWatchState != Agent.WatchState.Alarmed)
		{
			return Agent.AIScriptedFrameFlags.NoAttack | Agent.AIScriptedFrameFlags.DoNotRun;
		}
		return Agent.AIScriptedFrameFlags.NoAttack | Agent.AIScriptedFrameFlags.NeverSlowDown;
	}

	protected override void OnTick(Agent agentToCompareTo, Formation formationToCompareTo, Team potentialUsersTeam, float dt)
	{
		foreach (PassageUsePoint standingPoint in UsableMachine.StandingPoints)
		{
			if ((agentToCompareTo == null || standingPoint.UserAgent == agentToCompareTo) && (formationToCompareTo == null || (standingPoint.UserAgent != null && standingPoint.UserAgent.IsAIControlled && standingPoint.UserAgent.Formation == formationToCompareTo)))
			{
				TaleWorlds.Library.Debug.FailedAssert("isAgentManagedByThisMachineAI(standingPoint.UserAgent)", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\AI\\PassageAI.cs", "OnTick", 41);
				Agent userAgent = standingPoint.UserAgent;
				if (HasActionCompleted || (potentialUsersTeam != null && UsableMachine.IsDisabledForBattleSideAI(potentialUsersTeam.Side)) || userAgent.IsRunningAway)
				{
					HandleAgentStopUsingStandingPoint(userAgent, standingPoint);
				}
			}
			for (int num = standingPoint.MovingAgents.Count - 1; num >= 0; num--)
			{
				Agent agent = standingPoint.MovingAgents[num];
				if ((agentToCompareTo == null || agent == agentToCompareTo) && (formationToCompareTo == null || (agent != null && agent.IsAIControlled && agent.Formation == formationToCompareTo)))
				{
					if (HasActionCompleted || (potentialUsersTeam != null && UsableMachine.IsDisabledForBattleSideAI(potentialUsersTeam.Side)) || agent.IsRunningAway)
					{
						TaleWorlds.Library.Debug.FailedAssert("HasActionCompleted || (potentialUsersTeam != null && UsableMachine.IsDisabledForBattleSideAI(potentialUsersTeam.Side)) || agent.IsRunningAway", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\AI\\PassageAI.cs", "OnTick", 69);
						HandleAgentStopUsingStandingPoint(agent, standingPoint);
					}
					else if (!standingPoint.IsDisabled && !agent.IsPaused && agent.CanReachAndUseObject(standingPoint, standingPoint.GetUserFrameForAgent(agent).Origin.GetGroundVec3().DistanceSquared(agent.Position)))
					{
						agent.UseGameObject(standingPoint);
						agent.SetScriptedFlags(agent.GetScriptedFlags() & ~standingPoint.DisableScriptedFrameFlags);
					}
				}
			}
		}
	}

	[Conditional("DEBUG")]
	private void TickForDebug()
	{
		if (!Input.DebugInput.IsHotKeyDown("UsableMachineAiBaseHotkeyShowMachineUsers"))
		{
			return;
		}
		foreach (PassageUsePoint standingPoint in UsableMachine.StandingPoints)
		{
			foreach (Agent movingAgent in standingPoint.MovingAgents)
			{
				_ = movingAgent;
			}
			_ = standingPoint.UserAgent;
		}
	}
}
