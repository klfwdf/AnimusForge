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
        Console.WriteLine($"World diplomacy oral make-trade smoke tests passed: {Test.Assertions} assertions.");
        return 0;
    }

    private static void VerifyRejections()
    {
        Test.True(Resolve(null!).Status == WorldDiplomacyOralMakeTradeResolutionStatus.BadPayloadFormat,
            "missing payload must be rejected");
        Test.True(Resolve("player").Status == WorldDiplomacyOralMakeTradeResolutionStatus.BadPayloadFormat,
            "single-ID payload must be rejected");
        Test.True(Resolve(":npc").Status == WorldDiplomacyOralMakeTradeResolutionStatus.EmptyKingdomId,
            "blank kingdom ID must be rejected");
        Test.True(Resolve("player:npc", playerExists: false).Status
                  == WorldDiplomacyOralMakeTradeResolutionStatus.PlayerKingdomUnavailable,
            "missing player kingdom must be rejected");
        Test.True(Resolve("player:npc", playerEliminated: true).Status
                  == WorldDiplomacyOralMakeTradeResolutionStatus.PlayerKingdomUnavailable,
            "eliminated player kingdom must be rejected");
        Test.True(Resolve("player:npc", npcExists: false).Status
                  == WorldDiplomacyOralMakeTradeResolutionStatus.NpcKingdomUnavailable,
            "missing NPC kingdom must be rejected");
        Test.True(Resolve("player:npc", playerIsRuler: false).Status
                  == WorldDiplomacyOralMakeTradeResolutionStatus.PlayerNotRuler,
            "player must still rule the player kingdom");
        Test.True(Resolve("player:npc", npcIsRuler: false).Status
                  == WorldDiplomacyOralMakeTradeResolutionStatus.NpcSpeakerNotRuler,
            "NPC speaker must still rule the NPC kingdom");
        Test.True(Resolve("other:npc").Status
                  == WorldDiplomacyOralMakeTradeResolutionStatus.KingdomPairMismatch,
            "payload must identify the exact player and NPC pair");
    }

    private static void VerifyValidPairs()
    {
        WorldDiplomacyOralMakeTradeResolution playerFirst = Resolve(" player : NPC : 80 :ignored");
        Test.True(playerFirst.IsReady, "player-first trade pair must resolve");
        Test.True(playerFirst.Command.PlayerKingdomId == "player"
                  && playerFirst.Command.NpcKingdomId == "npc",
            "command must use canonical live kingdom IDs");
        Test.True(playerFirst.Command.DurationToken == "80"
                  && playerFirst.Command.SpeakerHeroId == "npc-speaker",
            "existing duration token and speaker ID must be retained");

        WorldDiplomacyOralMakeTradeResolution npcFirst = Resolve("NPC:PLAYER");
        Test.True(npcFirst.IsReady && npcFirst.Command.DurationToken == "default",
            "reverse pair and omitted duration must preserve existing semantics");
        Test.True(Resolve("NPC:PLAYER:").Command.DurationToken == "",
            "explicit blank duration must preserve the default-duration semantics");
    }

    private static void VerifyFacade()
    {
        FakePort port = new FakePort();
        WorldDiplomacyMakeTradeCommandFacade facade = new WorldDiplomacyMakeTradeCommandFacade(port);
        WorldDiplomacyMakeTradeCommand invalid =
            new WorldDiplomacyMakeTradeCommand("player", "npc", "", "default");
        Test.True(facade.Execute(invalid).Status == WorldDiplomacyMakeTradeExecutionStatus.InvalidCommand,
            "facade must reject an invalid command");
        Test.True(port.CallCount == 0, "invalid command must not invoke the game port");

        WorldDiplomacyMakeTradeCommand valid =
            new WorldDiplomacyMakeTradeCommand("player", "npc", "npc-speaker", "default");
        port.NextReceipt = new WorldDiplomacyMakeTradeExecutionReceipt(
            WorldDiplomacyMakeTradeExecutionStatus.ActionNotApplied,
            "player",
            "npc",
            84,
            "not-applied");
        WorldDiplomacyMakeTradeExecutionReceipt rejected = facade.Execute(valid);
        Test.True(rejected.Status == WorldDiplomacyMakeTradeExecutionStatus.ActionNotApplied
                  && rejected.AppliedDurationDays == 84,
            "facade must preserve the game-port receipt");
        Test.True(port.CallCount == 1 && port.LastCommand.DurationToken == "default",
            "valid command must invoke the game port exactly once");

        port.ThrowOnExecute = true;
        WorldDiplomacyMakeTradeExecutionReceipt exceptionReceipt = facade.Execute(valid);
        Test.True(port.CallCount == 2
                  && exceptionReceipt.Status == WorldDiplomacyMakeTradeExecutionStatus.UnknownAfterStart,
            "a thrown port call must become one indeterminate receipt");
    }

    private static void VerifyApplicationReplay()
    {
        var invalid=new FakeOralSource();
        Test.True(DiplomacyOralMakeTradeApplication.Execute(ref invalid,"bad")==""
                  && invalid.Executions==0 && invalid.Notifications==0,
            "invalid trade payload must not execute");
        var applied=new FakeOralSource
        {
            Receipt=new WorldDiplomacyMakeTradeExecutionReceipt(
                WorldDiplomacyMakeTradeExecutionStatus.Applied,"player","npc",84,""),
            EndpointsAvailable=true
        };
        Test.True(DiplomacyOralMakeTradeApplication.Execute(ref applied,"player:npc:84")==""
                  && applied.Executions==1 && applied.Notifications==1
                  && applied.LastLog.Contains("days=84"),
            "applied trade must publish once with confirmed duration");
        var refused=new FakeOralSource
        {
            Receipt=new WorldDiplomacyMakeTradeExecutionReceipt(
                WorldDiplomacyMakeTradeExecutionStatus.AlreadyTrading,"player","npc",0,"active"),
            EndpointsAvailable=true
        };
        DiplomacyOralMakeTradeApplication.Execute(ref refused,"player:npc");
        Test.True(refused.Executions==1 && refused.EndpointLookups==0 && refused.Notifications==0,
            "rejected trade receipt must not publish");
        var missing=new FakeOralSource
        {
            Receipt=new WorldDiplomacyMakeTradeExecutionReceipt(
                WorldDiplomacyMakeTradeExecutionStatus.Applied,"player","npc",84,"")
        };
        DiplomacyOralMakeTradeApplication.Execute(ref missing,"player:npc");
        Test.True(missing.EndpointLookups==1 && missing.Notifications==0,
            "applied trade with missing endpoint must not publish");
    }

    private struct FakeOralSource : IDiplomacyOralMakeTradeSource
    {
        internal WorldDiplomacyMakeTradeExecutionReceipt Receipt;
        internal bool EndpointsAvailable;
        internal int Executions;
        internal int EndpointLookups;
        internal int Notifications;
        internal string LastLog;
        public DiplomacyOralRoyalSnapshot Capture() => new(true,"player",false,true,true,"npc","speaker",true);
        public WorldDiplomacyMakeTradeExecutionReceipt Execute(WorldDiplomacyMakeTradeCommand command)
        { Executions++;return Receipt; }
        public bool TryResolveAppliedEndpoints(string playerId,string npcId,
            out string resolvedPlayerId,out string resolvedNpcId)
        { EndpointLookups++;resolvedPlayerId=playerId;resolvedNpcId=npcId;return EndpointsAvailable; }
        public void NotifyResolved() { Notifications++; }
        public void Log(string message) { LastLog=message; }
    }

    private static void VerifySourceBoundary()
    {
        string contracts = Read("Refactor", "Contracts", "WorldDiplomacyMakeTradeContracts.cs");
        string rules = Read("Refactor", "Domain", "WorldDiplomacyOralMakeTradeRules.cs");
        string facade = Read("Refactor", "Modules", "WorldDiplomacyMakeTradeCommandFacade.cs");
        string adapter = Read("Refactor", "Adapters", "BannerlordWorldDiplomacyMakeTradeGameActionPort.cs");
        string behavior = (Read("src/modules/AF.Module.Diplomacy/Direct/DiplomacyBehavior.cs") + Read("src/modules/AF.Module.Diplomacy/Direct/DiplomacyBehavior.Actions.cs"));
        string method = ExtractMethod(behavior, "private string TryExecuteMakeTrade(");
        string application = Read("src/modules/AF.Module.Diplomacy/Application/DiplomacyOralMakeTradeApplication.cs");
        string oralSource = Read("src/modules/AF.Module.Diplomacy/Adapters/DiplomacyOralMakeTradeSource.cs");

        Test.True(!contracts.Contains("TaleWorlds", StringComparison.Ordinal)
                  && !rules.Contains("TaleWorlds", StringComparison.Ordinal)
                  && !facade.Contains("TaleWorlds", StringComparison.Ordinal),
            "contracts, rules, and facade must remain TaleWorlds-free");
        Test.True(method.Contains("DiplomacyOralMakeTradeApplication.Execute(ref source, payload)", StringComparison.Ordinal)
                  && application.Contains("WorldDiplomacyOralMakeTradeRules.ResolveCommand", StringComparison.Ordinal)
                  && application.Contains("source.Execute(resolution.Command)", StringComparison.Ordinal),
            "behavior must forward trade resolution and execution to Application");
        Test.True(!method.Contains("(payload ?? \"\").Split(':')", StringComparison.Ordinal)
                  && !method.Contains("MakeTradeAgreement", StringComparison.Ordinal)
                  && !method.Contains("GetTradeAgreementDurationInYears", StringComparison.Ordinal),
            "legacy parser, duration selection, and action must be removed from behavior");
        Test.True(adapter.Contains("TWParallel.IsMainThread()", StringComparison.Ordinal)
                  && adapter.Contains("ResolveKingdoms(command.PlayerKingdomId, command.NpcKingdomId",
                      StringComparison.Ordinal)
                  && adapter.Contains("Hero.Find(command.SpeakerHeroId)", StringComparison.Ordinal),
            "adapter must re-resolve live objects from stable IDs on the main thread");
        Test.True(Count(adapter, "foreach (Kingdom kingdom in Kingdom.All)") == 1,
            "both kingdom IDs must be resolved in one bounded scan");
        Test.True(adapter.Contains("durationToken == \"0\"", StringComparison.Ordinal)
                  && adapter.Contains("MBMath.ClampInt(parsedDays, 1, 252)", StringComparison.Ordinal)
                  && Count(adapter, "GetTradeAgreementDurationInYears(playerKingdom, npcKingdom)") == 2,
            "default, blank, zero, invalid, and explicit duration semantics must remain unchanged");
        Test.True(Count(adapter, "trade.MakeTradeAgreement(playerKingdom, npcKingdom, duration)") == 1,
            "adapter must invoke the trade action exactly once");
        int action = adapter.IndexOf(
            "trade.MakeTradeAgreement(playerKingdom, npcKingdom, duration)",
            StringComparison.Ordinal);
        int confirmation = adapter.IndexOf(
            "if (!BannerlordApiCompat.HasTradeAgreement(trade, playerKingdom, npcKingdom))",
            action,
            StringComparison.Ordinal);
        Test.True(action >= 0 && confirmation > action,
            "adapter must confirm the trade agreement after the action");
        int appliedGuard = application.IndexOf("if (!receipt.IsApplied)", StringComparison.Ordinal);
        int notification = application.IndexOf("source.NotifyResolved()", StringComparison.Ordinal);
        Test.True(appliedGuard >= 0 && notification > appliedGuard
                  && oralSource.Contains("WorldDiplomacyBehavior.NotifyExternalDiplomacyResolved", StringComparison.Ordinal),
            "confirmed fact must be published only after an Applied receipt");
        string cancelMethod = ExtractMethod(behavior, "private string TryExecuteCancelTrade(");
        Test.True(cancelMethod.Contains("WorldDiplomacyOralCancelTradeRules.ResolveCommand", StringComparison.Ordinal)
                  && cancelMethod.Contains("CancelTradeCommandFacade.Execute(resolution.Command)",
                      StringComparison.Ordinal),
            "cancel-trade must remain delegated after its follow-up slice");
    }

    private static WorldDiplomacyOralMakeTradeResolution Resolve(
        string payload,
        bool playerExists = true,
        bool playerEliminated = false,
        bool playerIsRuler = true,
        bool npcExists = true,
        bool npcIsRuler = true)
    {
        return WorldDiplomacyOralMakeTradeRules.ResolveCommand(
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

    private sealed class FakePort : IWorldDiplomacyMakeTradeGameActionPort
    {
        internal int CallCount { get; private set; }
        internal bool ThrowOnExecute { get; set; }
        internal WorldDiplomacyMakeTradeCommand LastCommand { get; private set; }
        internal WorldDiplomacyMakeTradeExecutionReceipt NextReceipt { get; set; }

        public WorldDiplomacyMakeTradeExecutionReceipt Execute(WorldDiplomacyMakeTradeCommand command)
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
