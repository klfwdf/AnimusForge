using RichExecutions.Core;
using SandBox;
using SandBox.Missions.AgentBehaviors;
using SandBox.Missions.MissionLogics;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace TaleWorlds.Library
{
    public readonly record struct Vec2(float x, float y)
    {
        public float LengthSquared => x*x+y*y;
        public float Length => MathF.Sqrt(LengthSquared);
        public static Vec2 operator -(Vec2 a, Vec2 b) => new(a.x-b.x,a.y-b.y);
        public static Vec2 operator +(Vec2 a, Vec2 b) => new(a.x+b.x,a.y+b.y);
        public static Vec2 operator *(Vec2 a, float b) => new(a.x*b,a.y*b);
        public static float DotProduct(Vec2 a, Vec2 b) => a.x*b.x+a.y*b.y;
    }
    public readonly record struct Vec3(float x, float y, float z=0) { public Vec2 AsVec2 => new(x,y); }
    public class NavigationPath
    {
        public Vec2[] PathPoints = new Vec2[128]; public int Size;
        public Vec2 this[int i] => PathPoints[i];
    }
}
namespace TaleWorlds.Core
{
    public class AgentData
    {
        public object Origin;
        public AgentData(object origin) { Origin = origin; }
        public AgentData Monster(object monster) => this;
    }
    public enum AgentControllerType { None, AI, Player }
    public readonly record struct ActionIndexCache(int Id) { public static ActionIndexCache act_none; }
}
namespace TaleWorlds.CampaignSystem
{
    public class CharacterObject { public bool IsHero; }
    public class CampaignMission { public static CampaignMission Current = new(); public object Location = new(); }
}
namespace TaleWorlds.MountAndBlade
{
    public struct MissionWeapon { } public struct Blow { } public struct AttackCollisionData { }
    public class MissionLogic
    {
        public virtual void OnAgentAlarmedStateChanged(Agent agent, Agent.AIStateFlag flag) { }
        public virtual void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon weapon, in Blow blow, in AttackCollisionData collision) { }
    }
    public sealed class Team { }
    public sealed class Mission
    {
        public MissionAgentHandler Handler = new(); public MissionFightHandler Fight = new();
        public FakeScene Scene = new(); public float CurrentTime;
        public T GetMissionBehavior<T>() where T : class => (typeof(T)==typeof(MissionAgentHandler)?(object)Handler:Fight) as T;
    }
    public sealed class FakeScene
    {
        public Vec2[] Detour; public bool PathSucceeds = true; public int Requests;
        public bool GetPathBetweenAIFaces(UIntPtr a, UIntPtr b, Vec2 start, Vec2 end, float radius, NavigationPath path, int[] excluded)
        {
            Requests++; path.Size=Detour?.Length??0;
            if (Detour!=null) Array.Copy(Detour,path.PathPoints,Detour.Length);
            return PathSucceeds;
        }
    }
    public readonly record struct WorldPosition(Vec2 AsVec2) { public UIntPtr GetNavMesh()=>new(1); }
    public readonly record struct FakeFrame(WorldPosition Origin);
    public sealed class StandingPoint
    {
        public Vec2 Position;
        public bool IsDisabledForAgent(Agent agent)=>false;
        public FakeFrame GetUserFrameForAgent(Agent agent)=>new(new(Position));
    }
    public sealed class UsableMachine
    {
        public bool IsDisabled, IsDestroyed, Available=true;
        public StandingPoint Point=new(){Position=new(25,0)};
        public bool IsStandingPointAvailableForAgent(Agent agent)=>Available;
        public StandingPoint GetVacantStandingPointForAI(Agent agent)=>Available?Point:null;
    }
    public sealed class Agent
    {
        public object Origin = new(), Monster = new();
        public object Character = new TaleWorlds.CampaignSystem.CharacterObject();
        public int Index; public Mission Mission;
        public bool Active=true, ClearSucceeds=true, ThrowOnComponent;
        public TaleWorlds.Core.AgentControllerType Controller;
        public Team Team; public object CurrentlyUsedGameObject;
        public enum WatchState { Patrolling, Alarmed } public enum AIStateFlag { None, Cautious, Alarmed }
        public WatchState Watch; public int ClearCount, MovementReleaseCount;
        public Vec2 Position=new(10,0);
        public TaleWorlds.Core.ActionIndexCache[] Actions={new(1),new(2)};
        public CampaignAgentComponent Component;
        public bool IsActive()=>Active;
        public void SetWatchState(WatchState state)=>Watch=state;
        public WorldPosition GetWorldPosition()=>new(Position);
        public T GetComponent<T>() where T:class=>Component as T;
        public void AddComponent(CampaignAgentComponent c) { if(ThrowOnComponent)throw new InvalidOperationException();Component=c; }
        public void DisableScriptedMovement()=>MovementReleaseCount++;
        public void SetMaximumSpeedLimit(float speed,bool isMultiplier) { }
        public TaleWorlds.Core.ActionIndexCache GetCurrentAction(int channel)=>Actions[channel];
        public bool SetActionChannel(int channel,in TaleWorlds.Core.ActionIndexCache action,bool ignorePriority)
        {ClearCount++;if(ClearSucceeds)Actions[channel]=action;return ClearSucceeds;}
    }
}
namespace SandBox
{
    public sealed class CampaignAgentComponent
    {
        private Agent _agent; public AgentNavigator AgentNavigator;
        public CampaignAgentComponent(Agent agent)=>_agent=agent;
        public AgentNavigator CreateAgentNavigator()=>AgentNavigator=new(_agent);
    }
    public sealed class AgentNavigator
    {
        public Agent OwnerAgent; public bool TargetCleared; public UsableMachine TargetUsableMachine;
        private Dictionary<Type,AgentBehaviorGroup> _groups=new();
        public AgentNavigator(Agent agent)=>OwnerAgent=agent;
        public T AddBehaviorGroup<T>() where T:AgentBehaviorGroup,new()
        { if(!_groups.TryGetValue(typeof(T),out var g))_groups[typeof(T)]=g=new T{Navigator=this};return (T)g; }
        public void ClearTarget(){TargetCleared=true;TargetUsableMachine=null;}
        public void SetTarget(UsableMachine target)=>TargetUsableMachine=target;
    }
}
namespace SandBox.Missions.AgentBehaviors
{
    public class AgentBehaviorGroup
    {
        public AgentNavigator Navigator; private Dictionary<Type,AgentBehavior> _behaviors=new();
        public T GetBehavior<T>() where T:AgentBehavior=>_behaviors.GetValueOrDefault(typeof(T)) as T;
        public T AddBehavior<T>() where T:AgentBehavior {var b=(T)Activator.CreateInstance(typeof(T),this);_behaviors[typeof(T)]=b;return b;}
        public void RemoveBehavior<T>()=>_behaviors.Remove(typeof(T));
    }
    public abstract class AgentBehavior
    {
        protected AgentBehaviorGroup BehaviorGroup;public bool IsActive=true;
        public AgentBehavior(AgentBehaviorGroup g)=>BehaviorGroup=g;
        public AgentNavigator Navigator=>BehaviorGroup.Navigator;public Agent OwnerAgent=>Navigator.OwnerAgent;
        public Mission Mission=>OwnerAgent.Mission;
        public virtual float GetAvailability(bool isSimulation)=>0;
        public virtual void Tick(float dt,bool isSimulation) { }
        public virtual string GetDebugInfo()=>"";
        protected virtual void OnDeactivate() { }
    }
    public class DailyBehaviorGroup:AgentBehaviorGroup { }
    public class InterruptingBehaviorGroup:AgentBehaviorGroup { }
    public class AlarmedBehaviorGroup:AgentBehaviorGroup { }
    public class WalkingBehavior:AgentBehavior { public WalkingBehavior(AgentBehaviorGroup g):base(g){} }
    public class FleeBehavior:AgentBehavior { public FleeBehavior(AgentBehaviorGroup g):base(g){} }
    public class FightBehavior:AgentBehavior { public FightBehavior(AgentBehaviorGroup g):base(g){} }
}
namespace SandBox.Missions.MissionLogics
{
    public sealed class MissionAgentHandler
    {
        public bool Common=true, Limited;
        public List<UsableMachine> CommonPoints=new(){new()}; public List<UsableMachine> LimitedPoints=new(){new()};
        public List<UsableMachine> FindAllUnusedPoints(Agent agent,string tag)
            => ((tag=="npc_common"?Common:Limited)?(tag=="npc_common"?CommonPoints:LimitedPoints):new()).Where(p=>p.Available&&!p.IsDisabled&&!p.IsDestroyed).ToList();
    }
    public sealed class MissionFightHandler { public bool Active; public bool IsThereActiveFight()=>Active; }
}
namespace RichExecutions.Diagnostics
{
    public static class RexLog
    {
        public static int Errors,Warnings; public static void Info(string text){}
        public static void Warning(string text)=>Warnings++;
        public static void Error(string text,Exception e)=>Errors++;
    }
}
namespace RichExecutions.Scene
{
    public sealed partial class TownExecutionMissionBehavior:MissionLogic
    {
        private bool _cleanupComplete,_playerExecutionStateRestored,_aftermathConversationsRestored,_aftermathSessionReleased;
        private readonly List<Agent> _crowdAgents=new(); private readonly Team _executionTeam=new();
        private Vec3 _victimPosition=new(); private PlacementFixture _placement=new();
        private List<Vec3> _occupiedSpawnPositions=new();
        public class PlacementFixture { public Vec3 Origin=new(); }
        public class EntityFixture { public FrameFixture GetGlobalFrame()=>new(); }
        public class FrameFixture { public Vec3 origin; }
        public ExecutionSessionState State=ExecutionSessionState.Aftermath; public Mission Mission=new();
        public sealed class RequestFixture{public string SessionId="test";}public RequestFixture Request=new();
        public void Ready(bool player=true,bool conversations=true,bool released=true,bool cleanup=false)
        {_playerExecutionStateRestored=player;_aftermathConversationsRestored=conversations;_aftermathSessionReleased=released;_cleanupComplete=cleanup;}
        public Agent AddSpectator(){var a=new Agent{Team=_executionTeam,Index=_crowdAgents.Count,Mission=Mission};_crowdAgents.Add(a);return a;}
        public void Tick(float dt)=>TickCrowdDispersal(dt);
    }
}
