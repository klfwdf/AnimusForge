using RichExecutions.Core;
using RichExecutions.Diagnostics;
using RichExecutions.Scene;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

int checks = 0;
void Check(bool condition, string label)
{
    if (!condition) throw new InvalidOperationException(label);
    checks++;
    Console.WriteLine("PASS " + label);
}

foreach (var state in new[] { ExecutionSessionState.Preparing, ExecutionSessionState.WaitingForPlayer,
    ExecutionSessionState.CeremonyPreparation, ExecutionSessionState.Execution,
    ExecutionSessionState.CrowdReaction, ExecutionSessionState.Cancelled })
{
    var owner = new TownExecutionMissionBehavior { State = state };
    owner.Ready();
    var agent = owner.AddSpectator();
    owner.Tick(50);
    Check(agent.Controller == AgentControllerType.None && agent.ClearCount == 0, state + " keeps crowd untouched");
}
for (int flags = 0; flags < 7; flags++)
{
    var owner = new TownExecutionMissionBehavior();
    owner.Ready((flags & 1) != 0, (flags & 2) != 0, (flags & 4) != 0);
    var agent = owner.AddSpectator();
    owner.Tick(50);
    Check(agent.Controller == AgentControllerType.None, "partial aftermath does not disperse: " + flags);
}
var crowd = new TownExecutionMissionBehavior();
crowd.Ready();
var agents = Enumerable.Range(0, 14).Select(_ => crowd.AddSpectator()).ToArray();
crowd.Tick(30);
Check(agents.Count(a => a.Controller == AgentControllerType.AI) == 1, "long frame releases at most one spectator");
crowd.Tick(.1f);
Check(agents[1].Controller == AgentControllerType.None, "release interval enforced");
for (int i = 1; i < agents.Length; i++) crowd.Tick(.36f);
Check(agents.All(a => a.Controller == AgentControllerType.AI), "all 14 spectators eventually released");
Check(agents.All(a => a.ClearCount == 2 && a.MovementReleaseCount == 1
    && a.Component.AgentNavigator.Wanderer && a.Component.AgentNavigator.SpecialTargetTag == "npc_common"
    && a.Component.AgentNavigator.TargetCleared), "clear cheering and install native walking once");
crowd.Tick(90);
Check(agents.All(a => a.ClearCount == 2), "finished schedule never takes control back");
foreach (string takeover in new[] { "AI", "team", "using_object", "removed" })
{
    var owner = new TownExecutionMissionBehavior(); owner.Ready();
    var agent = owner.AddSpectator(); var next = owner.AddSpectator();
    switch (takeover)
    {
        case "AI": agent.Controller = AgentControllerType.AI; break;
        case "team": agent.Team = new Team(); break;
        case "using_object": agent.CurrentlyUsedGameObject = new object(); break;
        case "removed": agent.Active = false; break;
    }
    owner.Tick(1); owner.Tick(1);
    Check(agent.ClearCount == 0 && next.Controller == AgentControllerType.AI, takeover + " is skipped without blocking next spectator");
}
var retry = new TownExecutionMissionBehavior(); retry.Ready();
var alarm = new TownExecutionMissionBehavior(); alarm.Ready();
var alarmed = alarm.AddSpectator(); alarmed.Watch = Agent.WatchState.Alarmed; alarm.Tick(1);
Check(alarmed.Controller == AgentControllerType.AI && alarmed.Watch == Agent.WatchState.Patrolling,
    "ceremony alarm resumes peaceful walking without panic retreat");
var idle = new TownExecutionMissionBehavior(); idle.Ready();
var idleAgent = idle.AddSpectator(); idleAgent.Actions = new ActionIndexCache[2]; idleAgent.ClearSucceeds = false;
idle.Tick(1);
Check(idleAgent.Controller == AgentControllerType.AI && idleAgent.ClearCount == 0,
    "already empty action channels do not need native reset acceptance");
var failed = retry.AddSpectator(); failed.ClearSucceeds = false;
var healthy = retry.AddSpectator();
for (int i = 0; i < 4; i++) retry.Tick(1);
Check(failed.ClearCount == 6 && healthy.Controller == AgentControllerType.AI, "failed action clear bounded to three attempts");
var broken = new TownExecutionMissionBehavior(); broken.Ready();
broken.AddSpectator().ThrowOnComponent = true;
var following = broken.AddSpectator();
for (int i = 0; i < 4; i++) broken.Tick(1);
Check(RexLog.Errors == 3 && following.Controller == AgentControllerType.AI, "throwing engine call cannot strand the queue");
var limited = new TownExecutionMissionBehavior(); limited.Ready();
limited.Mission.Handler.Common = false; limited.Mission.Handler.Limited = true;
var limitedAgent = limited.AddSpectator(); limited.Tick(1);
Check(limitedAgent.Controller == AgentControllerType.AI, "native limited walking points accepted");
var empty = new TownExecutionMissionBehavior(); empty.Ready(); empty.Mission.Handler.Common = false;
var noPoints = empty.AddSpectator();
for (int i = 0; i < 9; i++) empty.Tick(1);
Check(noPoints.ClearCount == 0 && RexLog.Warnings == 3, "missing town points bounded and preserves spectator");
var exited = new TownExecutionMissionBehavior(); exited.Ready(cleanup: true);
var ended = exited.AddSpectator(); exited.Tick(1);
Check(ended.ClearCount == 0, "mission cleanup prevents late release");
var timing = new ExecutionCrowdDispersalSchedule();
Check(!timing.TryTakeNext(float.NaN, 2, out _) && !timing.TryTakeNext(float.PositiveInfinity, 2, out _)
    && !timing.TryTakeNext(-1, 2, out _) && timing.TryTakeNext(0, 2, out int index) && index == 0,
    "invalid delta cannot poison timer");
Console.WriteLine($"{checks}/{checks} production crowd schedule/release tests passed with fake engine objects; native pathfinding NOT_RUN");
