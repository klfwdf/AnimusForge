using System;
using System.Diagnostics;
using SandBox.Objects;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.AI
{
	// Token: 0x02000107 RID: 263
	public class PassageAI : UsableMachineAIBase
	{
		// Token: 0x06000D33 RID: 3379 RVA: 0x00060340 File Offset: 0x0005E540
		public PassageAI(UsableMachine usableMachine)
			: base(usableMachine)
		{
		}

		// Token: 0x06000D34 RID: 3380 RVA: 0x00060349 File Offset: 0x0005E549
		protected override Agent.AIScriptedFrameFlags GetScriptedFrameFlags(Agent agent)
		{
			if (agent.CurrentWatchState != Agent.WatchState.Alarmed)
			{
				return Agent.AIScriptedFrameFlags.NoAttack | Agent.AIScriptedFrameFlags.DoNotRun;
			}
			return Agent.AIScriptedFrameFlags.NoAttack | Agent.AIScriptedFrameFlags.NeverSlowDown;
		}

		// Token: 0x06000D35 RID: 3381 RVA: 0x0006035C File Offset: 0x0005E55C
		protected override void OnTick(Agent agentToCompareTo, Formation formationToCompareTo, Team potentialUsersTeam, float dt)
		{
			foreach (StandingPoint standingPoint in this.UsableMachine.StandingPoints)
			{
				PassageUsePoint passageUsePoint = (PassageUsePoint)standingPoint;
				if ((agentToCompareTo == null || passageUsePoint.UserAgent == agentToCompareTo) && (formationToCompareTo == null || (passageUsePoint.UserAgent != null && passageUsePoint.UserAgent.IsAIControlled && passageUsePoint.UserAgent.Formation == formationToCompareTo)))
				{
					Debug.FailedAssert("isAgentManagedByThisMachineAI(standingPoint.UserAgent)", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\AI\\PassageAI.cs", "OnTick", 41);
					Agent userAgent = passageUsePoint.UserAgent;
					if (this.HasActionCompleted || (potentialUsersTeam != null && this.UsableMachine.IsDisabledForBattleSideAI(potentialUsersTeam.Side)) || userAgent.IsRunningAway)
					{
						this.HandleAgentStopUsingStandingPoint(userAgent, passageUsePoint);
					}
				}
				for (int i = passageUsePoint.MovingAgents.Count - 1; i >= 0; i--)
				{
					Agent agent = passageUsePoint.MovingAgents[i];
					if ((agentToCompareTo == null || agent == agentToCompareTo) && (formationToCompareTo == null || (agent != null && agent.IsAIControlled && agent.Formation == formationToCompareTo)))
					{
						if (this.HasActionCompleted || (potentialUsersTeam != null && this.UsableMachine.IsDisabledForBattleSideAI(potentialUsersTeam.Side)) || agent.IsRunningAway)
						{
							Debug.FailedAssert("HasActionCompleted || (potentialUsersTeam != null && UsableMachine.IsDisabledForBattleSideAI(potentialUsersTeam.Side)) || agent.IsRunningAway", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\AI\\PassageAI.cs", "OnTick", 69);
							this.HandleAgentStopUsingStandingPoint(agent, passageUsePoint);
						}
						else if (!passageUsePoint.IsDisabled && !agent.IsPaused && agent.CanReachAndUseObject(passageUsePoint, passageUsePoint.GetUserFrameForAgent(agent).Origin.GetGroundVec3().DistanceSquared(agent.Position)))
						{
							agent.UseGameObject(passageUsePoint, -1);
							agent.SetScriptedFlags(agent.GetScriptedFlags() & ~passageUsePoint.DisableScriptedFrameFlags);
						}
					}
				}
			}
		}

		// Token: 0x06000D36 RID: 3382 RVA: 0x00060544 File Offset: 0x0005E744
		[Conditional("DEBUG")]
		private void TickForDebug()
		{
			if (Input.DebugInput.IsHotKeyDown("UsableMachineAiBaseHotkeyShowMachineUsers"))
			{
				foreach (StandingPoint standingPoint in this.UsableMachine.StandingPoints)
				{
					PassageUsePoint passageUsePoint = (PassageUsePoint)standingPoint;
					foreach (Agent agent in passageUsePoint.MovingAgents)
					{
					}
					Agent userAgent = passageUsePoint.UserAgent;
				}
			}
		}
	}
}
