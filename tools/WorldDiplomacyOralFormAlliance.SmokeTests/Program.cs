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
        Console.WriteLine($"World diplomacy oral form-alliance smoke tests passed: {Test.Assertions} assertions.");
        return 0;
    }

    private static void VerifyRejections()
    {
        Test.True(Resolve(null!).Status == WorldDiplomacyOralFormAllianceResolutionStatus.BadPayloadFormat,
            "missing payload must be rejected");
        Test.True(Resolve("player").Status == WorldDiplomacyOralFormAllianceResolutionStatus.BadPayloadFormat,
            "single-ID payload must be rejected");
        Test.True(Resolve(":npc").Status == WorldDiplomacyOralFormAllianceResolutionStatus.EmptyKingdomId,
            "blank kingdom ID must be rejected");
        Test.True(Resolve("player:npc", playerExists: false).Status
                  == WorldDiplomacyOralFormAllianceResolutionStatus.PlayerKingdomUnavailable,
            "missing player kingdom must be rejected");
        Test.True(Resolve("player:npc", playerEliminated: true).Status
                  == WorldDiplomacyOralFormAllianceResolutionStatus.PlayerKingdomUnavailable,
            "eliminated player kingdom must be rejected");
        Test.True(Resolve("player:npc", npcExists: false).Status
                  == WorldDiplomacyOralFormAllianceResolutionStatus.NpcKingdomUnavailable,
            "missing NPC kingdom must be rejected");
        Test.True(Resolve("player:npc", playerIsRuler: false).Status
                  == WorldDiplomacyOralFormAllianceResolutionStatus.PlayerNotRuler,
            "player must rule the player kingdom");
        Test.True(Resolve("player:npc", npcIsRuler: false).Status
                  == WorldDiplomacyOralFormAllianceResolutionStatus.NpcSpeakerNotRuler,
            "NPC speaker must rule the NPC kingdom");
        Test.True(Resolve("other:npc").Status
                  == WorldDiplomacyOralFormAllianceResolutionStatus.KingdomPairMismatch,
            "payload must identify the exact player and NPC pair");
    }

    private static void VerifyValidPairs()
    {
        WorldDiplomacyOralFormAllianceResolution playerFirst = Resolve(" player : NPC : 80 :ignored");
        Test.True(playerFirst.IsReady, "player-first alliance pair must resolve");
        Test.True(playerFirst.Command.PlayerKingdomId == "player"
                  && playerFirst.Command.NpcKingdomId == "npc",
            "command must use canonical live kingdom IDs");
        Test.True(playerFirst.Command.DurationToken == "80"
                  && playerFirst.Command.SpeakerHeroId == "npc-speaker",
            "existing duration token and speaker ID must be retained");

        WorldDiplomacyOralFormAllianceResolution npcFirst = Resolve("NPC:PLAYER");
        Test.True(npcFirst.IsReady && npcFirst.Command.DurationToken == "default",
            "reverse pair and omitted duration must preserve existing semantics");
    }

    private static void VerifyFacade()
    {
        FakePort port = new FakePort();
        WorldDiplomacyFormAllianceCommandFacade facade = new WorldDiplomacyFormAllianceCommandFacade(port);
        WorldDiplomacyFormAllianceCommand invalid = new WorldDiplomacyFormAllianceCommand(
            "player", "npc", "", "default");
        Test.True(facade.Execute(invalid).Status == WorldDiplomacyFormAllianceExecutionStatus.InvalidCommand,
            "facade must reject an invalid command");
        Test.True(port.CallCount == 0, "invalid command must not invoke the game port");

        WorldDiplomacyFormAllianceCommand valid = new WorldDiplomacyFormAllianceCommand(
            "player", "npc", "npc-speaker", "default");
        port.NextReceipt = new WorldDiplomacyFormAllianceExecutionReceipt(
            WorldDiplomacyFormAllianceExecutionStatus.ActionNotApplied,
            "player",
            "npc",
            "not-applied");
        Test.True(facade.Execute(valid).Status == WorldDiplomacyFormAllianceExecutionStatus.ActionNotApplied,
            "facade must preserve the game-port receipt");
        Test.True(port.CallCount == 1 && port.LastCommand.SpeakerHeroId == "npc-speaker",
            "valid command must invoke the game port exactly once with stable IDs");

        port.ThrowOnExecute = true;
        WorldDiplomacyFormAllianceExecutionReceipt exceptionReceipt = facade.Execute(valid);
        Test.True(port.CallCount == 2
                  && exceptionReceipt.Status == WorldDiplomacyFormAllianceExecutionStatus.UnknownAfterStart,
            "thrown port call must map to one indeterminate receipt");
    }

    private static void VerifyApplicationReplay()
    {
        var invalid=new FakeOralSource();
        Test.True(DiplomacyOralFormAllianceApplication.Execute(ref invalid,"bad")==""
                  && invalid.Executions==0 && invalid.Notifications==0,
            "invalid alliance payload must not execute");
        var applied=new FakeOralSource
        {
            Receipt=new WorldDiplomacyFormAllianceExecutionReceipt(
                WorldDiplomacyFormAllianceExecutionStatus.Applied,"player","npc", ""),
            EndpointsAvailable=true
        };
        Test.True(DiplomacyOralFormAllianceApplication.Execute(ref applied,"player:npc:84")==""
                  && applied.Executions==1 && applied.Notifications==1,
            "applied alliance must publish once");
        var refused=new FakeOralSource
        {
            Receipt=new WorldDiplomacyFormAllianceExecutionReceipt(
                WorldDiplomacyFormAllianceExecutionStatus.AlreadyAllied,"player","npc","allied"),
            EndpointsAvailable=true
        };
        DiplomacyOralFormAllianceApplication.Execute(ref refused,"player:npc");
        Test.True(refused.Executions==1 && refused.EndpointLookups==0 && refused.Notifications==0,
            "rejected alliance receipt must not publish");
        var missing=new FakeOralSource
        {
            Receipt=new WorldDiplomacyFormAllianceExecutionReceipt(
                WorldDiplomacyFormAllianceExecutionStatus.Applied,"player","npc","")
        };
        DiplomacyOralFormAllianceApplication.Execute(ref missing,"player:npc");
        Test.True(missing.EndpointLookups==1 && missing.Notifications==0,
            "applied alliance with missing endpoint must not publish");
    }

    private struct FakeOralSource : IDiplomacyOralFormAllianceSource
    {
        internal WorldDiplomacyFormAllianceExecutionReceipt Receipt;
        internal bool EndpointsAvailable;
        internal int Executions;
        internal int EndpointLookups;
        internal int Notifications;
        public DiplomacyOralRoyalSnapshot Capture() => new(true,"player",false,true,true,"npc","speaker",true);
        public WorldDiplomacyFormAllianceExecutionReceipt Execute(WorldDiplomacyFormAllianceCommand command)
        { Executions++;return Receipt; }
        public bool TryResolveAppliedEndpoints(string playerId,string npcId,
            out string resolvedPlayerId,out string resolvedNpcId)
        { EndpointLookups++;resolvedPlayerId=playerId;resolvedNpcId=npcId;return EndpointsAvailable; }
        public void NotifyResolved() { Notifications++; }
        public void Log(string message) { }
    }

    private static void VerifySourceBoundary()
    {
        string contracts = Read("Refactor", "Contracts", "WorldDiplomacyFormAllianceContracts.cs");
        string rules = Read("Refactor", "Domain", "WorldDiplomacyOralFormAllianceRules.cs");
        string facade = Read("Refactor", "Modules", "WorldDiplomacyFormAllianceCommandFacade.cs");
        string adapter = Read("Refactor", "Adapters", "BannerlordWorldDiplomacyFormAllianceGameActionPort.cs");
        string behavior = (Read("src/modules/AF.Module.Diplomacy/Direct/DiplomacyBehavior.cs") + Read("src/modules/AF.Module.Diplomacy/Direct/DiplomacyBehavior.Actions.cs"));
        string method = ExtractMethod(behavior, "private string TryExecuteFormAlliance(");
        string application = Read("src/modules/AF.Module.Diplomacy/Application/DiplomacyOralFormAllianceApplication.cs");
        string oralSource = Read("src/modules/AF.Module.Diplomacy/Adapters/DiplomacyOralFormAllianceSource.cs");

        Test.True(!contracts.Contains("TaleWorlds", StringComparison.Ordinal)
                  && !rules.Contains("TaleWorlds", StringComparison.Ordinal)
                  && !facade.Contains("TaleWorlds", StringComparison.Ordinal),
            "contracts, rules, and facade must remain TaleWorlds-free");
        Test.True(method.Contains("DiplomacyOralFormAllianceApplication.Execute(ref source, payload)", StringComparison.Ordinal)
                  && application.Contains("WorldDiplomacyOralFormAllianceRules.ResolveCommand", StringComparison.Ordinal)
                  && application.Contains("source.Execute(resolution.Command)", StringComparison.Ordinal),
            "behavior must forward alliance resolution and execution to Application");
        Test.True(!method.Contains("(payload ?? \"\").Split(':')", StringComparison.Ordinal)
                  && !method.Contains("StartAlliance", StringComparison.Ordinal)
                  && !method.Contains("foreach (Kingdom", StringComparison.Ordinal),
            "legacy parser, action, and alliance scans must be removed from behavior");
        Test.True(adapter.Contains("TWParallel.IsMainThread()", StringComparison.Ordinal)
                  && adapter.Contains("ResolveKingdom(command.PlayerKingdomId)", StringComparison.Ordinal)
                  && adapter.Contains("ResolveHero(command.SpeakerHeroId)", StringComparison.Ordinal),
            "adapter must re-resolve live objects from stable IDs on the main thread");
        Test.True(Count(adapter, "foreach (Kingdom kingdom in Kingdom.All)") == 1,
            "both alliance limits must be counted in one bounded kingdom scan");
        Test.True(Count(adapter, "alliance.StartAlliance(playerKingdom, npcKingdom)") == 1,
            "adapter must invoke the alliance action once");
        int action = adapter.IndexOf("alliance.StartAlliance(playerKingdom, npcKingdom)", StringComparison.Ordinal);
        int confirmation = adapter.IndexOf("if (!alliance.IsAllyWithKingdom(playerKingdom, npcKingdom))", action,
            StringComparison.Ordinal);
        Test.True(action >= 0 && confirmation > action,
            "adapter must confirm the alliance after the action");
        int appliedGuard = application.IndexOf("if (!receipt.IsApplied)", StringComparison.Ordinal);
        int notification = application.IndexOf("source.NotifyResolved()", StringComparison.Ordinal);
        Test.True(appliedGuard >= 0 && notification > appliedGuard
                  && oralSource.Contains("WorldDiplomacyBehavior.NotifyExternalDiplomacyResolved", StringComparison.Ordinal),
            "confirmed fact must be published only after an Applied receipt");
    }

    private static WorldDiplomacyOralFormAllianceResolution Resolve(
        string payload,
        bool playerExists = true,
        bool playerEliminated = false,
        bool playerIsRuler = true,
        bool npcExists = true,
        bool npcIsRuler = true)
    {
        return WorldDiplomacyOralFormAllianceRules.ResolveCommand(
            payload,
            playerExists,
            "player",
            playerEliminated,
            playerIsRuler,
            npcExists,
            "npc",
            "npc-speaker",
            npcIsRuler);
    }

    private sealed class FakePort : IWorldDiplomacyFormAllianceGameActionPort
    {
        internal int CallCount { get; private set; }
        internal bool ThrowOnExecute { get; set; }
        internal WorldDiplomacyFormAllianceCommand LastCommand { get; private set; }
        internal WorldDiplomacyFormAllianceExecutionReceipt NextReceipt { get; set; }

        public WorldDiplomacyFormAllianceExecutionReceipt Execute(WorldDiplomacyFormAllianceCommand command)
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
