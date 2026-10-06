using RichExecutions.Core;
using RichExecutions.Diagnostics;
using RichExecutions.Scene;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using SandBox.Missions.AgentBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements.Locations;

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
    && a.Component.AgentNavigator.AddBehaviorGroup<DailyBehaviorGroup>().GetBehavior<ExecutionCrowdWalkAwayBehavior>() != null
    && a.Component.AgentNavigator.TargetUsableMachine != null
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
var busy = new TownExecutionMissionBehavior(); busy.Ready(); busy.Mission.Handler.Limited=true;
busy.Mission.Handler.CommonPoints[0].Available=false;
var busyAgent=busy.AddSpectator();busy.Tick(1);
Check(busyAgent.Component.AgentNavigator.TargetUsableMachine==busy.Mission.Handler.LimitedPoints[0],
    "occupied common points use an actually available limited point");
var disabled = new TownExecutionMissionBehavior();disabled.Ready();disabled.Mission.Handler.Limited=true;
disabled.Mission.Handler.CommonPoints[0].IsDisabled=true;
var disabledAgent=disabled.AddSpectator();disabled.Tick(1);
Check(disabledAgent.Component.AgentNavigator.TargetUsableMachine==disabled.Mission.Handler.LimitedPoints[0],
    "disabled common target does not hide valid limited target");
var inward=new TownExecutionMissionBehavior();inward.Ready();inward.Mission.Handler.CommonPoints[0].Point.Position=new(1,0);
var inwardAgent=inward.AddSpectator();inward.Tick(1);
Check(inwardAgent.Controller==AgentControllerType.None && inwardAgent.ClearCount==0,"stage destination rejected before clearing control");
var through=new TownExecutionMissionBehavior();through.Ready();through.Mission.Scene.Detour=new[]{new Vec2(0,0)};
var throughAgent=through.AddSpectator();through.Tick(1);
Check(throughAgent.Controller==AgentControllerType.None,"outward destination with path through stage rejected");
var away=new TownExecutionMissionBehavior();away.Ready();away.Mission.Scene.Detour=new[]{new Vec2(15,5),new Vec2(20,5)};
var awayAgent=away.AddSpectator();away.Tick(1);
Check(awayAgent.Controller==AgentControllerType.AI,"outward navigation detour avoiding stage accepted");
var subsequent=new TownExecutionMissionBehavior();subsequent.Ready();
var secondDestination=new UsableMachine{Point=new StandingPoint{Position=new(40,0)}};
subsequent.Mission.Handler.CommonPoints.Add(new UsableMachine{Point=new StandingPoint{Position=new(1,0)}});
subsequent.Mission.Handler.CommonPoints.Add(secondDestination);
var subsequentAgent=subsequent.AddSpectator();subsequent.Tick(1);
var subsequentWalk=subsequentAgent.Component.AgentNavigator.AddBehaviorGroup<DailyBehaviorGroup>().GetBehavior<ExecutionCrowdWalkAwayBehavior>();
subsequentAgent.Position=new(25,0);subsequentAgent.CurrentlyUsedGameObject=new object();subsequent.Mission.CurrentTime=2;subsequentWalk.Tick(1,false);
subsequentAgent.CurrentlyUsedGameObject=null;subsequent.Mission.CurrentTime=4;subsequentWalk.Tick(1,false);
Check(subsequentAgent.Component.AgentNavigator.TargetUsableMachine==secondDestination,"after arrival next destination also excludes return to stage");
subsequentWalk.IsActive=false;secondDestination.IsDisabled=true;subsequent.Mission.CurrentTime=6;subsequentWalk.Tick(1,false);
Check(subsequentAgent.Component.AgentNavigator.TargetUsableMachine==secondDestination,"inactive walk behavior cannot override combat group control");
var failedPath=new TownExecutionMissionBehavior();failedPath.Ready();failedPath.Mission.Scene.PathSucceeds=false;
var noPath=failedPath.AddSpectator();for(int i=0;i<4;i++)failedPath.Tick(1);
Check(noPath.ClearCount==0,"unreachable point never reported as released");
var freshAlarm=new TownExecutionMissionBehavior();freshAlarm.Ready();var alarmAgent=freshAlarm.AddSpectator();
alarmAgent.Watch=Agent.WatchState.Alarmed;
freshAlarm.OnAgentAlarmedStateChanged(alarmAgent,Agent.AIStateFlag.Alarmed);freshAlarm.Tick(1);
Check(alarmAgent.ClearCount==0 && alarmAgent.Watch==Agent.WatchState.Alarmed,"new alarm with unchanged team/controller is preserved");
var freshHit=new TownExecutionMissionBehavior();freshHit.Ready();var hitAgent=freshHit.AddSpectator();
freshHit.OnAgentHit(hitAgent,null,new MissionWeapon(),new Blow(),new AttackCollisionData());freshHit.Tick(1);
Check(hitAgent.ClearCount==0,"new hit stops pending ceremony releases");
var fight=new TownExecutionMissionBehavior();fight.Ready();var fightAgent=fight.AddSpectator();fight.Mission.Fight.Active=true;fight.Tick(1);
Check(fightAgent.ClearCount==0,"native active fight stops pending releases");
var freed=new TownExecutionMissionBehavior();freed.Ready();freed.Mission.Handler.CommonPoints[0].Available=false;
var freedAgent=freed.AddSpectator();freed.Tick(1);freed.Mission.Handler.CommonPoints[0].Available=true;freed.Tick(1);
Check(freedAgent.Controller==AgentControllerType.AI,"bounded retry refreshes a newly freed standing point");
var nonPending=new TownExecutionMissionBehavior();nonPending.Ready();var releasedOne=nonPending.AddSpectator();var pendingOne=nonPending.AddSpectator();
nonPending.Tick(1);nonPending.OnAgentAlarmedStateChanged(releasedOne,Agent.AIStateFlag.Cautious);nonPending.Tick(1);
Check(pendingOne.Controller==AgentControllerType.AI,"released civilian ambient caution does not cancel all pending spectators");
var budgetAgent=new Agent{Mission=new Mission()};budgetAgent.Mission.Scene.PathSucceeds=false;
budgetAgent.Mission.Handler.CommonPoints=Enumerable.Range(0,100).Select(_=>new UsableMachine()).ToList();
var budgetRoute=new ExecutionCrowdRoute(budgetAgent,budgetAgent.Mission.Handler,new(0,0),4);budgetRoute.FindNext();
Check(budgetAgent.Mission.Scene.Requests==4,"native path requests bounded per attempt");
Check(!ExecutionCrowdRoutePolicy.IsOutwardTarget(new(10,0),new(-20,0),new(0,0),4),"opposite-side destination rejected");
Check(!ExecutionCrowdRoutePolicy.IsSafeSegment(new(10,0),new(-10,0),new(0,0),4),"segment crossing stage rejected");
Check(ExecutionCrowdRoutePolicy.IsSafeSegment(new(2,0),new(8,0),new(0,0),4),"spectator inside envelope can leave outward");
Check(!ExecutionCrowdRoutePolicy.IsSafeSegment(new(2,0),new(-8,0),new(0,0),4),"inside spectator cannot walk through stage center");
var replanner=awayAgent.Component.AgentNavigator.AddBehaviorGroup<DailyBehaviorGroup>().GetBehavior<ExecutionCrowdWalkAwayBehavior>();
var oldTarget=awayAgent.Component.AgentNavigator.TargetUsableMachine;
oldTarget.IsDisabled=true;away.Mission.CurrentTime=2;replanner.Tick(1,false);
Check(awayAgent.Component.AgentNavigator.TargetUsableMachine!=oldTarget || !oldTarget.IsDisabled,
    "disabled assigned target no longer stays bound");
var location = new Location(); var interior = new Location();
var complex = new LocationComplex(); complex.Locations.Add(location); complex.Locations.Add(interior);
var identities = new ExecutionSceneLocationCharacters(); var temporary = new Agent();
// Match the native passage contract: its permission delegate dereferences the
// LocationCharacter resolved by exact origin, rather than accepting null.
bool CanUseDoor(Agent a) => location.GetLocationCharacter(a.Origin).Data != null;
bool oldFailure = false;
try { CanUseDoor(temporary); } catch (NullReferenceException) { oldFailure = true; }
Check(oldFailure, "unregistered temporary actor reproduces native door contract null dereference");
identities.Register(temporary, location, complex, true);
Check(CanUseDoor(temporary), "registered temporary actor supplies non-null native door identity");
var identity = location.GetLocationCharacter(temporary.Origin);
Check(ReferenceEquals(identity.Data.Origin, temporary.Origin), "registration preserves exact origin identity");
identities.Register(temporary, location, complex, true);
Check(location.Characters.Count == 1, "repeated registration does not duplicate actor");
var second = new Agent { Character = temporary.Character };
identities.Register(second, location, complex, true);
Check(location.Characters.Count == 2 && !ReferenceEquals(location.GetLocationCharacter(second.Origin), identity),
    "same troop template gets distinct origin-based identities");
var existing = new Agent(); var existingEntry = new LocationCharacter(new AgentData(existing.Origin), null, null, true,
    LocationCharacter.CharacterRelations.Neutral, null, true, false); location.AddCharacter(existingEntry);
identities.Register(existing, location, complex, true);
Check(ReferenceEquals(location.GetLocationCharacter(existing.Origin), existingEntry), "existing native identity reused");
var hero = new Agent { Character = new CharacterObject { IsHero = true } };
identities.Register(hero, location, complex, false);
Check(location.GetLocationCharacter(hero.Origin) == null, "real hero identity is never created or replaced");
bool missingLocationRejected = false;
try { identities.Register(new Agent(), null, complex, true); } catch (InvalidOperationException) { missingLocationRejected = true; }
Check(missingLocationRejected, "missing location rejects actor initialization before native AI handoff");
location.RemoveLocationCharacter(identity); interior.AddCharacter(identity);
Check(interior.Characters.Count == 1, "fleeing actor can move to another location");
complex.FailRemoval = true;
Check(!identities.Clear() && interior.Characters.Count == 1, "cleanup failure retains ownership for retry");
complex.FailRemoval = false;
Check(identities.Clear() && interior.Characters.Count == 0 && location.Characters.Count == 1,
    "cleanup removes temporary actors including actors that used doors");
Check(ReferenceEquals(location.Characters[0], existingEntry), "cleanup preserves pre-existing native character");
Check(identities.Clear() && location.Characters.Count == 1, "repeated cleanup is idempotent");
Console.WriteLine($"{checks}/{checks} production crowd and location identity tests passed with fake engine objects; native pathfinding NOT_RUN");
