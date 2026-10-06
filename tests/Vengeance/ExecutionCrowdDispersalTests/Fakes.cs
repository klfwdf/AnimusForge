using RichExecutions.Core;
using SandBox;
using SandBox.Missions.MissionLogics;
using TaleWorlds.MountAndBlade;

namespace TaleWorlds.Core
{
    public enum AgentControllerType { None, AI, Player }
    public readonly record struct ActionIndexCache(int Id) { public static ActionIndexCache act_none; }
}
namespace TaleWorlds.CampaignSystem
{
    public class CampaignMission
    {
        public static CampaignMission Current = new();
        public object Location = new();
    }
}
namespace TaleWorlds.MountAndBlade
{
    public sealed class Team { }
    public sealed class Mission
    {
        public MissionAgentHandler Handler = new();
        public T GetMissionBehavior<T>() where T : class => Handler as T;
    }
    public sealed class Agent
    {
        public int Index;
        public bool Active = true, ClearSucceeds = true, ThrowOnComponent;
        public TaleWorlds.Core.AgentControllerType Controller;
        public Team Team;
        public object CurrentlyUsedGameObject;
        public enum WatchState { Patrolling, Alarmed }
        public WatchState Watch;
        public int ClearCount, MovementReleaseCount;
        public TaleWorlds.Core.ActionIndexCache[] Actions = { new(1), new(2) };
        public CampaignAgentComponent Component;
        public bool IsActive() => Active;
        public void SetWatchState(WatchState state) => Watch = state;
        public T GetComponent<T>() where T : class => Component as T;
        public void AddComponent(CampaignAgentComponent component)
        {
            if (ThrowOnComponent) throw new InvalidOperationException("injected component failure");
            Component = component;
        }
        public void DisableScriptedMovement() => MovementReleaseCount++;
        public void SetMaximumSpeedLimit(float speed, bool isMultiplier) { }
        public TaleWorlds.Core.ActionIndexCache GetCurrentAction(int channel) => Actions[channel];
        public bool SetActionChannel(int channel, in TaleWorlds.Core.ActionIndexCache action, bool ignorePriority)
        { ClearCount++; if (ClearSucceeds) Actions[channel] = action; return ClearSucceeds; }
    }
}
namespace SandBox
{
    public sealed class CampaignAgentComponent
    {
        public AgentNavigator AgentNavigator;
        public CampaignAgentComponent(Agent agent) { }
        public AgentNavigator CreateAgentNavigator() => AgentNavigator = new();
    }
    public sealed class AgentNavigator
    {
        public string SpecialTargetTag;
        public bool TargetCleared, Wanderer;
        public void ClearTarget() => TargetCleared = true;
    }
}
namespace SandBox.Missions.AgentBehaviors
{
    public static class BehaviorSets
    {
        public static void AddWandererBehaviors(Agent agent) => agent.Component.AgentNavigator.Wanderer = true;
    }
}
namespace SandBox.Missions.MissionLogics
{
    public sealed class MissionAgentHandler
    {
        public bool Common = true, Limited;
        public bool HasUsablePointWithTag(string tag) => tag == "npc_common" ? Common : Limited;
    }
}
namespace RichExecutions.Diagnostics
{
    public static class RexLog
    {
        public static int Errors, Warnings;
        public static void Info(string text) { }
        public static void Warning(string text) => Warnings++;
        public static void Error(string text, Exception exception) => Errors++;
    }
}
namespace RichExecutions.Scene
{
    public sealed partial class TownExecutionMissionBehavior
    {
        private bool _cleanupComplete, _playerExecutionStateRestored,
            _aftermathConversationsRestored, _aftermathSessionReleased;
        private readonly List<Agent> _crowdAgents = new();
        private readonly Team _executionTeam = new();
        public ExecutionSessionState State = ExecutionSessionState.Aftermath;
        public Mission Mission = new();
        public sealed class RequestFixture { public string SessionId = "test"; }
        public RequestFixture Request = new();
        public void Ready(bool player = true, bool conversations = true, bool released = true, bool cleanup = false)
        {
            _playerExecutionStateRestored = player;
            _aftermathConversationsRestored = conversations;
            _aftermathSessionReleased = released;
            _cleanupComplete = cleanup;
        }
        public Agent AddSpectator()
        {
            var agent = new Agent { Team = _executionTeam, Index = _crowdAgents.Count };
            _crowdAgents.Add(agent);
            return agent;
        }
        public void Tick(float dt) => TickCrowdDispersal(dt);
    }
}
