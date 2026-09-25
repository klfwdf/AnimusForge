using System.Text;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Domain;
using AnimusForge.Refactor.Modules;

static class Test
{
    private static int _assertions;

    internal static void True(bool value, string message)
    {
        _assertions++;
        if (!value) throw new InvalidOperationException(message);
    }

    internal static int Assertions => _assertions;
}
internal static class Program
{
    private static int Main()
    {
        VerifyRejections();
        VerifyValidPairs();
        VerifyFacade();
        VerifySourceBoundary();
        Console.WriteLine($"World diplomacy oral break-alliance smoke tests passed: {Test.Assertions} assertions.");
        return 0;
    }

    private static void VerifyRejections()
    {
        Test.True(Resolve(null!).Status == WorldDiplomacyOralBreakAllianceResolutionStatus.BadPayloadFormat,
            "missing payload must be rejected");
        Test.True(Resolve("player").Status == WorldDiplomacyOralBreakAllianceResolutionStatus.BadPayloadFormat,
            "single-ID payload must be rejected");
        Test.True(Resolve(":npc").Status == WorldDiplomacyOralBreakAllianceResolutionStatus.EmptyKingdomId,
            "blank kingdom ID must be rejected");
        Test.True(Resolve("player:npc", playerExists: false).Status
                  == WorldDiplomacyOralBreakAllianceResolutionStatus.PlayerKingdomUnavailable,
            "missing player kingdom must be rejected");
        Test.True(Resolve("player:npc", playerEliminated: true).Status
                  == WorldDiplomacyOralBreakAllianceResolutionStatus.PlayerKingdomUnavailable,
            "eliminated player kingdom must be rejected");
        Test.True(Resolve("player:npc", npcExists: false).Status
                  == WorldDiplomacyOralBreakAllianceResolutionStatus.NpcKingdomUnavailable,
            "missing NPC kingdom must be rejected");
        Test.True(Resolve("other:npc").Status
                  == WorldDiplomacyOralBreakAllianceResolutionStatus.KingdomPairMismatch,
            "payload must identify the exact player and NPC pair");
    }

    private static void VerifyValidPairs()
    {
        WorldDiplomacyOralBreakAllianceResolution playerFirst = Resolve(" player : NPC : 80 :ignored");
        Test.True(playerFirst.IsReady, "player-first alliance pair must resolve");
        Test.True(playerFirst.Command.PlayerKingdomId == "player"
                  && playerFirst.Command.NpcKingdomId == "npc",
            "command must use canonical live kingdom IDs");
        Test.True(playerFirst.Command.SpeakerHeroId == "npc-speaker",
            "speaker ID must cross the boundary as a stable value");

        WorldDiplomacyOralBreakAllianceResolution npcFirst = Resolve("NPC:PLAYER");
        Test.True(npcFirst.IsReady,
            "reverse pair must preserve existing semantics");
    }

    private static void VerifyFacade()
    {
        FakePort port = new FakePort();
        WorldDiplomacyBreakAllianceCommandFacade facade = new WorldDiplomacyBreakAllianceCommandFacade(port);
        WorldDiplomacyBreakAllianceCommand invalid = new WorldDiplomacyBreakAllianceCommand(
            "player", "npc", "");
        Test.True(facade.Execute(invalid).Status == WorldDiplomacyBreakAllianceExecutionStatus.InvalidCommand,
            "facade must reject an invalid command");
        Test.True(port.CallCount == 0, "invalid command must not invoke the game port");

        WorldDiplomacyBreakAllianceCommand valid = new WorldDiplomacyBreakAllianceCommand(
            "player", "npc", "npc-speaker");
        port.NextReceipt = new WorldDiplomacyBreakAllianceExecutionReceipt(
            WorldDiplomacyBreakAllianceExecutionStatus.ActionNotApplied,
            "player",
            "npc",
            "not-applied");
        Test.True(facade.Execute(valid).Status == WorldDiplomacyBreakAllianceExecutionStatus.ActionNotApplied,
            "facade must preserve the game-port receipt");
        Test.True(port.CallCount == 1 && port.LastCommand.SpeakerHeroId == "npc-speaker",
            "valid command must invoke the game port exactly once with stable IDs");

        port.ThrowOnExecute = true;
        WorldDiplomacyBreakAllianceExecutionReceipt exceptionReceipt = facade.Execute(valid);
        Test.True(port.CallCount == 2
                  && exceptionReceipt.Status == WorldDiplomacyBreakAllianceExecutionStatus.UnknownAfterStart,
            "thrown port call must map to one indeterminate receipt");
    }

    private static void VerifySourceBoundary()
    {
        string contracts = Read("Refactor", "Contracts", "WorldDiplomacyBreakAllianceContracts.cs");
        string rules = Read("Refactor", "Domain", "WorldDiplomacyOralBreakAllianceRules.cs");
        string facade = Read("Refactor", "Modules", "WorldDiplomacyBreakAllianceCommandFacade.cs");
        string adapter = Read("Refactor", "Adapters", "BannerlordWorldDiplomacyBreakAllianceGameActionPort.cs");
        string behavior = (Read("src/modules/AF.Module.Diplomacy/Direct/DiplomacyBehavior.cs") + Read("src/modules/AF.Module.Diplomacy/Direct/DiplomacyBehavior.Actions.cs"));
        string method = ExtractMethod(behavior, "private string TryExecuteBreakAlliance(");

        Test.True(!contracts.Contains("TaleWorlds", StringComparison.Ordinal)
                  && !rules.Contains("TaleWorlds", StringComparison.Ordinal)
                  && !facade.Contains("TaleWorlds", StringComparison.Ordinal),
            "contracts, rules, and facade must remain TaleWorlds-free");
        Test.True(method.Contains("WorldDiplomacyOralBreakAllianceRules.ResolveCommand", StringComparison.Ordinal)
                  && method.Contains("BreakAllianceCommandFacade.Execute(resolution.Command)", StringComparison.Ordinal),
            "behavior must delegate alliance resolution and execution");
        Test.True(!method.Contains("(payload ?? \"\").Split(':')", StringComparison.Ordinal)
                  && !method.Contains("EndAlliance", StringComparison.Ordinal)
                  && !method.Contains("RunAuthorizedBreak", StringComparison.Ordinal)
                  && !method.Contains("foreach (Kingdom", StringComparison.Ordinal),
            "legacy parser, action, and alliance scans must be removed from behavior");
        Test.True(adapter.Contains("TWParallel.IsMainThread()", StringComparison.Ordinal)
                  && adapter.Contains("ResolveKingdoms(command.PlayerKingdomId, command.NpcKingdomId", StringComparison.Ordinal)
                  && adapter.Contains("ResolveHero(command.SpeakerHeroId)", StringComparison.Ordinal),
            "adapter must re-resolve live objects from stable IDs on the main thread");
        Test.True(Count(adapter, "foreach (Kingdom kingdom in Kingdom.All)") == 1,
            "both kingdom IDs must be resolved in one bounded kingdom scan");
        Test.True(Count(adapter, "alliance.EndAlliance(playerKingdom, npcKingdom)") == 1
                  && Count(adapter, "PermanentAllianceGuard.RunAuthorizedBreak(") == 1,
            "adapter must invoke the guarded alliance break exactly once");
        int action = adapter.IndexOf("alliance.EndAlliance(playerKingdom, npcKingdom)", StringComparison.Ordinal);
        int confirmation = adapter.IndexOf("if (alliance.IsAllyWithKingdom(playerKingdom, npcKingdom))", action,
            StringComparison.Ordinal);
        Test.True(action >= 0 && confirmation > action,
            "adapter must confirm the alliance ended after the action");
        int appliedGuard = method.IndexOf("if (!receipt.IsApplied)", StringComparison.Ordinal);
        int notification = method.IndexOf("WorldDiplomacyBehavior.NotifyExternalDiplomacyResolved", StringComparison.Ordinal);
        Test.True(appliedGuard >= 0 && notification > appliedGuard,
            "confirmed fact must be published only after an Applied receipt");
    }

    private static WorldDiplomacyOralBreakAllianceResolution Resolve(
        string payload,
        bool playerExists = true,
        bool playerEliminated = false,
        bool npcExists = true)
    {
        return WorldDiplomacyOralBreakAllianceRules.ResolveCommand(
            payload,
            playerExists,
            "player",
            playerEliminated,
            npcExists,
            "npc",
            "npc-speaker");
    }

    private sealed class FakePort : IWorldDiplomacyBreakAllianceGameActionPort
    {
        internal int CallCount { get; private set; }
        internal bool ThrowOnExecute { get; set; }
        internal WorldDiplomacyBreakAllianceCommand LastCommand { get; private set; }
        internal WorldDiplomacyBreakAllianceExecutionReceipt NextReceipt { get; set; }

        public WorldDiplomacyBreakAllianceExecutionReceipt Execute(WorldDiplomacyBreakAllianceCommand command)
        {
            CallCount++;
            LastCommand = command;
            if (ThrowOnExecute) throw new InvalidOperationException("synthetic port failure");
            return NextReceipt;
        }
    }

    private static string Read(params string[] segments)
    {
        return File.ReadAllText(FindRepositoryFile(segments), Encoding.UTF8);
    }

    private static string ExtractMethod(string source, string signature)
    {
        int start = source.IndexOf(signature, StringComparison.Ordinal);
        if (start < 0) throw new InvalidOperationException("Could not locate method: " + signature);
        int openBrace = source.IndexOf('{', start);
        int depth = 0;
        for (int index = openBrace; index < source.Length; index++)
        {
            if (source[index] == '{') depth++;
            else if (source[index] == '}' && --depth == 0) return source.Substring(start, index - start + 1);
        }
        throw new InvalidOperationException("Could not parse method: " + signature);
    }

    private static int Count(string text, string value)
    {
        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }
        return count;
    }

    private static string FindRepositoryFile(params string[] relativeSegments)
    {
        DirectoryInfo? current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            string candidate = Path.Combine(new[] { current.FullName }.Concat(relativeSegments).ToArray());
            if (File.Exists(candidate)) return candidate;
            current = current.Parent;
        }
        throw new FileNotFoundException("Could not locate repository file", Path.Combine(relativeSegments));
    }
}
