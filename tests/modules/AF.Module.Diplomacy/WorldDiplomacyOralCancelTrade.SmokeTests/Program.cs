using System.Text;
using AnimusForge;
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
        VerifyApplicationReplay();
        VerifySourceBoundary();
        Console.WriteLine($"World diplomacy oral cancel-trade smoke tests passed: {Test.Assertions} assertions.");
        return 0;
    }

    private static void VerifyRejections()
    {
        Test.True(Resolve(null!).Status == WorldDiplomacyOralCancelTradeResolutionStatus.BadPayloadFormat,
            "missing payload must be rejected");
        Test.True(Resolve("player").Status == WorldDiplomacyOralCancelTradeResolutionStatus.BadPayloadFormat,
            "single-ID payload must be rejected");
        Test.True(Resolve(":npc").Status == WorldDiplomacyOralCancelTradeResolutionStatus.EmptyKingdomId,
            "blank kingdom ID must be rejected");
        Test.True(Resolve("player:npc", playerExists: false).Status
                  == WorldDiplomacyOralCancelTradeResolutionStatus.PlayerKingdomUnavailable,
            "missing player kingdom must be rejected");
        Test.True(Resolve("player:npc", playerEliminated: true).Status
                  == WorldDiplomacyOralCancelTradeResolutionStatus.PlayerKingdomUnavailable,
            "eliminated player kingdom must be rejected");
        Test.True(Resolve("player:npc", npcExists: false).Status
                  == WorldDiplomacyOralCancelTradeResolutionStatus.NpcKingdomUnavailable,
            "missing NPC kingdom must be rejected");
        Test.True(Resolve("other:npc").Status
                  == WorldDiplomacyOralCancelTradeResolutionStatus.KingdomPairMismatch,
            "payload must identify the exact player and NPC pair");
    }

    private static void VerifyValidPairs()
    {
        WorldDiplomacyOralCancelTradeResolution playerFirst = Resolve(" player : NPC :ignored");
        Test.True(playerFirst.IsReady, "player-first pair must resolve");
        Test.True(playerFirst.Command.PlayerKingdomId == "player"
                  && playerFirst.Command.NpcKingdomId == "npc",
            "command must use canonical live kingdom IDs");
        Test.True(playerFirst.Command.SpeakerHeroId == "npc-speaker",
            "speaker ID must cross the boundary as a stable value");

        WorldDiplomacyOralCancelTradeResolution npcFirst = Resolve("NPC:PLAYER");
        Test.True(npcFirst.IsReady, "reverse pair must preserve existing semantics");
    }

    private static void VerifyFacade()
    {
        FakePort port = new FakePort();
        WorldDiplomacyCancelTradeCommandFacade facade = new WorldDiplomacyCancelTradeCommandFacade(port);
        WorldDiplomacyCancelTradeCommand invalid =
            new WorldDiplomacyCancelTradeCommand("player", "npc", "");
        Test.True(facade.Execute(invalid).Status == WorldDiplomacyCancelTradeExecutionStatus.InvalidCommand,
            "facade must reject an invalid command");
        Test.True(port.CallCount == 0, "invalid command must not invoke the game port");

        WorldDiplomacyCancelTradeCommand valid =
            new WorldDiplomacyCancelTradeCommand("player", "npc", "npc-speaker");
        port.NextReceipt = new WorldDiplomacyCancelTradeExecutionReceipt(
            WorldDiplomacyCancelTradeExecutionStatus.ActionNotApplied,
            "player",
            "npc",
            "not-applied");
        Test.True(facade.Execute(valid).Status == WorldDiplomacyCancelTradeExecutionStatus.ActionNotApplied,
            "facade must preserve the game-port receipt");
        Test.True(port.CallCount == 1 && port.LastCommand.SpeakerHeroId == "npc-speaker",
            "valid command must invoke the game port exactly once");

        port.ThrowOnExecute = true;
        WorldDiplomacyCancelTradeExecutionReceipt exceptionReceipt = facade.Execute(valid);
        Test.True(port.CallCount == 2
                  && exceptionReceipt.Status == WorldDiplomacyCancelTradeExecutionStatus.UnknownAfterStart,
            "a thrown port call must become one indeterminate receipt");
    }

    private static void VerifyApplicationReplay()
    {
        var invalid = new FakeOralSource();
        Test.True(DiplomacyOralCancelTradeApplication.Execute(ref invalid, "bad") == ""
                  && invalid.Executions == 0 && invalid.Notifications == 0,
            "invalid cancellation payload must not execute");
        var applied = new FakeOralSource
        {
            Receipt = new WorldDiplomacyCancelTradeExecutionReceipt(
                WorldDiplomacyCancelTradeExecutionStatus.Applied, "player", "npc", ""),
            EndpointsAvailable = true
        };
        Test.True(DiplomacyOralCancelTradeApplication.Execute(ref applied, "player:npc") == ""
                  && applied.Executions == 1 && applied.Notifications == 1,
            "applied cancellation must publish once");
        var refused = new FakeOralSource
        {
            Receipt = new WorldDiplomacyCancelTradeExecutionReceipt(
                WorldDiplomacyCancelTradeExecutionStatus.NotTrading, "player", "npc", "not-trading"),
            EndpointsAvailable = true
        };
        DiplomacyOralCancelTradeApplication.Execute(ref refused, "player:npc");
        Test.True(refused.Executions == 1 && refused.EndpointLookups == 0 && refused.Notifications == 0,
            "rejected cancellation receipt must not publish");
        var missing = new FakeOralSource
        {
            Receipt = new WorldDiplomacyCancelTradeExecutionReceipt(
                WorldDiplomacyCancelTradeExecutionStatus.Applied, "player", "npc", "")
        };
        DiplomacyOralCancelTradeApplication.Execute(ref missing, "player:npc");
        Test.True(missing.EndpointLookups == 1 && missing.Notifications == 0,
            "applied cancellation with missing endpoint must not publish");
    }

    private struct FakeOralSource : IDiplomacyOralCancelTradeSource
    {
        internal WorldDiplomacyCancelTradeExecutionReceipt Receipt;
        internal bool EndpointsAvailable;
        internal int Executions;
        internal int EndpointLookups;
        internal int Notifications;
        public DiplomacyOralPairSnapshot Capture() => new(true, "player", false, true, "npc", "speaker");
        public WorldDiplomacyCancelTradeExecutionReceipt Execute(WorldDiplomacyCancelTradeCommand command)
        { Executions++; return Receipt; }
        public bool TryResolveAppliedEndpoints(string playerId, string npcId,
            out string resolvedPlayerId, out string resolvedNpcId)
        { EndpointLookups++; resolvedPlayerId = playerId; resolvedNpcId = npcId; return EndpointsAvailable; }
        public void NotifyResolved() { Notifications++; }
        public void Log(string message) { }
    }

    private static void VerifySourceBoundary()
    {
        string contracts = Read("src/AF.Contracts/Internal/WorldDiplomacyCancelTradeContracts.cs");
        string rules = Read("src", "modules", "AF.Module.Diplomacy", "Domain", "WorldDiplomacyOralCancelTradeRules.cs");
        string facade = Read("src/modules/AF.Module.Diplomacy/Adapters/Facades/WorldDiplomacyCancelTradeCommandFacade.cs");
        string adapter = Read("src/modules/AF.Module.Diplomacy/Adapters/BannerlordWorldDiplomacyCancelTradeGameActionPort.cs");
        string behavior = (Read("src/modules/AF.Module.Diplomacy/Direct/DiplomacyBehavior.cs") + Read("src/modules/AF.Module.Diplomacy/Direct/DiplomacyBehavior.Actions.cs"));
        string method = ExtractMethod(behavior, "private string TryExecuteCancelTrade(");
        string application = Read("src/modules/AF.Module.Diplomacy/Application/DiplomacyOralCancelTradeApplication.cs");
        string oralSource = Read("src/modules/AF.Module.Diplomacy/Adapters/DiplomacyOralCancelTradeSource.cs");

        Test.True(!contracts.Contains("TaleWorlds", StringComparison.Ordinal)
                  && !rules.Contains("TaleWorlds", StringComparison.Ordinal)
                  && !facade.Contains("TaleWorlds", StringComparison.Ordinal),
            "contracts, rules, and facade must remain TaleWorlds-free");
        Test.True(method.Contains("DiplomacyOralCancelTradeApplication.Execute(ref source, payload)", StringComparison.Ordinal)
                  && application.Contains("WorldDiplomacyOralCancelTradeRules.ResolveCommand", StringComparison.Ordinal)
                  && application.Contains("source.Execute(resolution.Command)", StringComparison.Ordinal),
            "behavior must forward trade cancellation resolution and execution to Application");
        Test.True(!method.Contains("(payload ?? \"\").Split(':')", StringComparison.Ordinal)
                  && !method.Contains("EndTradeAgreement", StringComparison.Ordinal)
                  && !method.Contains("HasTradeAgreementCompat", StringComparison.Ordinal),
            "legacy parser, state check, and action must be removed from behavior");
        Test.True(!behavior.Contains("private static bool IsPlayerNpcPair(", StringComparison.Ordinal)
                  && !behavior.Contains("private static bool HasTradeAgreementCompat(", StringComparison.Ordinal),
            "obsolete behavior helpers must be removed after the last caller migrates");
        Test.True(adapter.Contains("TWParallel.IsMainThread()", StringComparison.Ordinal)
                  && adapter.Contains("ResolveKingdoms(command.PlayerKingdomId, command.NpcKingdomId",
                      StringComparison.Ordinal)
                  && adapter.Contains("Hero.Find(command.SpeakerHeroId)", StringComparison.Ordinal),
            "adapter must re-resolve live objects from stable IDs on the main thread");
        Test.True(!adapter.Contains("RulingClan?.Leader", StringComparison.Ordinal),
            "unilateral cancellation must not add a ruler requirement");
        Test.True(!adapter.Contains("npcKingdom.IsEliminated", StringComparison.Ordinal),
            "cancellation must preserve the existing lack of an NPC elimination gate");
        Test.True(oralSource.Contains("ResolveKingdom(npcId, includeEliminated: true)",
                      StringComparison.Ordinal)
                  && oralSource.Contains("ResolveKingdom(playerId, includeEliminated: true)",
                      StringComparison.Ordinal),
            "post-action publication must preserve cancellation for eliminated NPC kingdoms");
        Test.True(Count(adapter, "foreach (Kingdom kingdom in Kingdom.All)") == 1,
            "both kingdom IDs must be resolved in one bounded scan");
        Test.True(Count(adapter, "trade.EndTradeAgreement(playerKingdom, npcKingdom)") == 1,
            "adapter must invoke the cancellation exactly once");
        int action = adapter.IndexOf(
            "trade.EndTradeAgreement(playerKingdom, npcKingdom)",
            StringComparison.Ordinal);
        int confirmation = adapter.IndexOf(
            "() => !ReadTradeState(trade, playerKingdom, npcKingdom)",
            action,
            StringComparison.Ordinal);
        Test.True(action >= 0 && confirmation > action,
            "adapter must confirm the agreement ended after the action");
        int appliedGuard = application.IndexOf("if (!receipt.IsApplied)", StringComparison.Ordinal);
        int notification = application.IndexOf("source.NotifyResolved()", StringComparison.Ordinal);
        Test.True(appliedGuard >= 0 && notification > appliedGuard
                  && oralSource.Contains("WorldDiplomacyBehavior.NotifyExternalDiplomacyResolved", StringComparison.Ordinal),
            "confirmed fact must be published only after an Applied receipt");
    }

    private static WorldDiplomacyOralCancelTradeResolution Resolve(
        string payload,
        bool playerExists = true,
        bool playerEliminated = false,
        bool npcExists = true)
    {
        return WorldDiplomacyOralCancelTradeRules.ResolveCommand(
            payload,
            playerExists,
            "player",
            playerEliminated,
            npcExists,
            "npc",
            "npc-speaker");
    }

    private sealed class FakePort : IWorldDiplomacyCancelTradeGameActionPort
    {
        internal int CallCount { get; private set; }
        internal bool ThrowOnExecute { get; set; }
        internal WorldDiplomacyCancelTradeCommand LastCommand { get; private set; }
        internal WorldDiplomacyCancelTradeExecutionReceipt NextReceipt { get; set; }

        public WorldDiplomacyCancelTradeExecutionReceipt Execute(WorldDiplomacyCancelTradeCommand command)
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
            else if (source[index] == '}' && --depth == 0)
            {
                return source.Substring(start, index - start + 1);
            }
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
        throw new FileNotFoundException(
            "Could not locate repository file",
            Path.Combine(relativeSegments));
    }
}
