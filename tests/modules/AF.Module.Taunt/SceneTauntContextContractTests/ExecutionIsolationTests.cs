using AnimusForge;
using RichExecutions.Core;
using RichExecutions.Scene;
using TaleWorlds.MountAndBlade;

internal static class ExecutionIsolationTests
{
    internal static void Run()
    {
        int checks = 0;
        void Check(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException(label);
            checks++;
            Console.WriteLine("PASS execution isolation: " + label);
        }

        var mission = new Mission();
        var otherMission = new Mission();
        var victim = new Agent { Index = 3 };
        var bystander = new Agent { Index = 4 };
        var reusedIndex = new Agent { Index = 3 };
        var owner = new TownExecutionMissionBehavior();
        Check(!ExecutionSceneConflictBridge.IsSceneControlled(mission), "ordinary Mission remains open");
        Check(!ExecutionSceneConflictBridge.BlocksConflict(null, victim), "no Mission has no isolation");
        ExecutionSceneConflictBridge.Register(mission, owner);
        foreach (var state in new[] { ExecutionSessionState.Preparing, ExecutionSessionState.WaitingForPlayer,
            ExecutionSessionState.CeremonyPreparation, ExecutionSessionState.Execution, ExecutionSessionState.CrowdReaction })
        {
            owner.SetFixture(state, victim: victim);
            Check(ExecutionSceneConflictBridge.IsSceneControlled(mission)
                && ExecutionSceneConflictBridge.BlocksConflict(mission, bystander), state + " blocks all scene conflicts");
        }
        // Aftermath begins BEFORE cleanup. Exercise every partial completion,
        // including the case where session release succeeded but player restore failed.
        for (int flags = 0; flags < 8; flags++)
        {
            owner.SetFixture(ExecutionSessionState.Aftermath, (flags & 1) != 0,
                (flags & 2) != 0, (flags & 4) != 0, victim: victim);
            Check(ExecutionSceneConflictBridge.IsSceneControlled(mission) == (flags != 7),
                "Aftermath restoration flags=" + flags);
        }
        Check(!ExecutionSceneConflictBridge.BlocksConflict(mission, bystander), "same Mission ordinary NPC resumes");
        Check(ExecutionSceneConflictBridge.BlocksConflict(mission, victim), "executed frozen victim remains excluded");
        Check(!ExecutionSceneConflictBridge.BlocksConflict(mission, reusedIndex), "agent index alone cannot exempt another NPC");
        Check(!ExecutionSceneConflictBridge.BlocksConflict(otherMission, victim), "another Mission never inherits isolation");
        owner.SetFixture(ExecutionSessionState.Cancelled, victim: victim);
        Check(ExecutionSceneConflictBridge.IsSceneControlled(mission), "cancellation still cleaning up stays isolated");
        owner.SetFixture(ExecutionSessionState.Cancelled, cleanup: true, victim: victim);
        Check(!ExecutionSceneConflictBridge.IsSceneControlled(mission)
            && !ExecutionSceneConflictBridge.BlocksConflict(mission, victim), "completed cancellation clears both protections");
        owner.SetFixture(ExecutionSessionState.Execution, cleanup: true, victim: victim);
        Check(!ExecutionSceneConflictBridge.IsSceneControlled(mission), "early mission-exit cleanup releases isolation");
        var nextOwner = new TownExecutionMissionBehavior();
        ExecutionSceneConflictBridge.Register(mission, nextOwner);
        Check(ExecutionSceneConflictBridge.IsSceneControlled(mission), "new controller reestablishes isolation");
        owner.SetFixture(ExecutionSessionState.Cancelled, cleanup: true);
        Check(ExecutionSceneConflictBridge.IsSceneControlled(mission), "old controller cleanup cannot release new owner");
        Console.WriteLine($"{checks}/{checks} execution isolation cases passed; real bridge/projection with fake Mission/owner fields; native callbacks NOT_RUN");
    }
}

// Test-only engine objects and owner backing fields. The actual eligibility
// property and Mission/Agent identity lookups are linked production source.
namespace TaleWorlds.MountAndBlade
{
    public sealed class Mission { }
    public sealed class Agent { public int Index { get; set; } }
}

namespace RichExecutions.Scene
{
    public sealed partial class TownExecutionMissionBehavior
    {
        private bool _cleanupComplete, _playerExecutionStateRestored,
            _aftermathConversationsRestored, _aftermathSessionReleased;
        private Agent _victimAgent;
        public ExecutionSessionState State { get; private set; }

        internal void SetFixture(ExecutionSessionState state, bool player = false,
            bool conversations = false, bool released = false, bool cleanup = false, Agent victim = null)
        {
            State = state;
            _playerExecutionStateRestored = player;
            _aftermathConversationsRestored = conversations;
            _aftermathSessionReleased = released;
            _cleanupComplete = cleanup;
            _victimAgent = victim;
        }
    }
}
