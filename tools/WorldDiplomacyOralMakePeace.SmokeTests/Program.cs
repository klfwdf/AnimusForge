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
        VerifyPlayerPays();
        VerifyNpcPays();
        VerifyCommandFacade();
        VerifyApplicationReplay();
        VerifySourceBoundary();
        Console.WriteLine($"World diplomacy oral make-peace smoke tests passed: {Test.Assertions} assertions.");
        return 0;
    }

    private static void VerifyRejections()
    {
        Test.True(Resolve(null!).Status == WorldDiplomacyOralMakePeaceResolutionStatus.BadPayloadFormat,
            "missing payload must be rejected");
        Test.True(Resolve("player:npc").Status == WorldDiplomacyOralMakePeaceResolutionStatus.BadPayloadFormat,
            "peace payload must include an amount token");
        Test.True(Resolve(":npc:0").Status == WorldDiplomacyOralMakePeaceResolutionStatus.EmptyKingdomId,
            "blank kingdom IDs must be rejected");
        Test.True(Resolve("player:npc:0", playerExists: false).Status
                  == WorldDiplomacyOralMakePeaceResolutionStatus.PlayerKingdomUnavailable,
            "missing player kingdom must be rejected");
        Test.True(Resolve("player:npc:0", playerEliminated: true).Status
                  == WorldDiplomacyOralMakePeaceResolutionStatus.PlayerKingdomUnavailable,
            "eliminated player kingdom must be rejected");
        Test.True(Resolve("player:npc:0", npcExists: false).Status
                  == WorldDiplomacyOralMakePeaceResolutionStatus.NpcKingdomUnavailable,
            "missing NPC kingdom must be rejected");
        Test.True(Resolve("player:npc:0", playerIsRuler: false).Status
                  == WorldDiplomacyOralMakePeaceResolutionStatus.PlayerNotRuler,
            "player must still rule the player kingdom");
        Test.True(Resolve("player:npc:0", npcIsRuler: false).Status
                  == WorldDiplomacyOralMakePeaceResolutionStatus.NpcSpeakerNotRuler,
            "NPC speaker must still rule the NPC kingdom");
        Test.True(Resolve("other:npc:0").Status
                  == WorldDiplomacyOralMakePeaceResolutionStatus.KingdomPairMismatch,
            "payload must contain the exact player and NPC kingdom pair");
    }

    private static void VerifyPlayerPays()
    {
        WorldDiplomacyOralMakePeaceResolution result = Resolve(" player : NPC : auto : 120 : ignored");
        Test.True(result.IsReady, "player-to-NPC peace terms must resolve");
        Test.True(result.Command.PayerKingdomId == "player"
                  && result.Command.ReceiverKingdomId == "NPC",
            "the payload order must retain the tribute direction");
        Test.True(result.Command.AmountToken == "auto" && result.Command.DurationToken == "120",
            "amount and duration tokens must be trimmed and retained");
        Test.True(result.Command.SpeakerHeroId == "npc-speaker",
            "command must retain the NPC speaker stable ID");
    }

    private static void VerifyNpcPays()
    {
        WorldDiplomacyOralMakePeaceResolution result = Resolve("NPC:player::");
        Test.True(result.IsReady, "NPC-to-player peace terms must resolve case-insensitively");
        Test.True(result.Command.PayerKingdomId == "NPC"
                  && result.Command.ReceiverKingdomId == "player",
            "reverse payload order must reverse the tribute direction");
        Test.True(result.Command.AmountToken == "" && result.Command.DurationToken == "",
            "explicit blank terms must preserve the existing zero/default semantics");
    }

    private static void VerifyCommandFacade()
    {
        FakeGameActionPort port = new FakeGameActionPort();
        WorldDiplomacyMakePeaceCommandFacade facade = new WorldDiplomacyMakePeaceCommandFacade(port);
        WorldDiplomacyMakePeaceCommand invalid = new WorldDiplomacyMakePeaceCommand(
            "player",
            "npc",
            "",
            "0",
            "default");

        WorldDiplomacyMakePeaceExecutionReceipt invalidReceipt = facade.Execute(invalid);
        Test.True(invalidReceipt.Status == WorldDiplomacyMakePeaceExecutionStatus.InvalidCommand,
            "facade must reject an invalid command");
        Test.True(port.CallCount == 0,
            "facade must not invoke the game port for an invalid command");

        WorldDiplomacyMakePeaceCommand valid = new WorldDiplomacyMakePeaceCommand(
            "player",
            "npc",
            "npc-speaker",
            "25",
            "100");
        port.NextReceipt = new WorldDiplomacyMakePeaceExecutionReceipt(
            WorldDiplomacyMakePeaceExecutionStatus.ActionNotApplied,
            "player",
            "npc",
            25,
            100,
            "not-applied");
        WorldDiplomacyMakePeaceExecutionReceipt rejectedReceipt = facade.Execute(valid);
        Test.True(port.CallCount == 1 && port.LastCommand.SpeakerHeroId == "npc-speaker",
            "facade must invoke the game port exactly once with stable IDs");
        Test.True(rejectedReceipt.Status == WorldDiplomacyMakePeaceExecutionStatus.ActionNotApplied
                  && rejectedReceipt.AppliedDailyTribute == 25,
            "facade must preserve the game action receipt");

        port.ThrowOnExecute = true;
        WorldDiplomacyMakePeaceExecutionReceipt exceptionReceipt = facade.Execute(valid);
        Test.True(port.CallCount == 2,
            "facade must make one game-port call per valid command");
        Test.True(exceptionReceipt.Status == WorldDiplomacyMakePeaceExecutionStatus.UnknownAfterStart
                  && exceptionReceipt.ErrorCode == "diplomacy.make_peace.port_exception",
            "facade must map a thrown port call to an indeterminate receipt");
    }

    private static void VerifyApplicationReplay()
    {
        var invalid=new FakeOralSource();
        Test.True(DiplomacyOralMakePeaceApplication.Execute(ref invalid,"player:npc")==""
                  && invalid.Executions==0 && invalid.Notifications==0,
            "invalid peace payload must not execute or publish");
        var applied=new FakeOralSource
        {
            Receipt=new WorldDiplomacyMakePeaceExecutionReceipt(
                WorldDiplomacyMakePeaceExecutionStatus.Applied,"player","npc",20,100,"")
        };
        applied.EndpointsAvailable=true;
        Test.True(DiplomacyOralMakePeaceApplication.Execute(ref applied,"player:npc:auto:100")==""
                  && applied.Executions==1 && applied.Notifications==1
                  && applied.LastLog.Contains("tribute=20 days=100"),
            "applied peace receipt must publish once after endpoint resolution");
        var refused=new FakeOralSource
        {
            Receipt=new WorldDiplomacyMakePeaceExecutionReceipt(
                WorldDiplomacyMakePeaceExecutionStatus.NotAtWar,"player","npc",0,0,"not-war")
        };
        DiplomacyOralMakePeaceApplication.Execute(ref refused,"player:npc:0");
        Test.True(refused.Executions==1 && refused.EndpointLookups==0 && refused.Notifications==0,
            "rejected peace receipt must not resolve endpoints or publish");
        var missing=new FakeOralSource
        {
            Receipt=new WorldDiplomacyMakePeaceExecutionReceipt(
                WorldDiplomacyMakePeaceExecutionStatus.Applied,"player","npc",0,100,"")
        };
        DiplomacyOralMakePeaceApplication.Execute(ref missing,"player:npc:0");
        Test.True(missing.Executions==1 && missing.EndpointLookups==1 && missing.Notifications==0,
            "applied peace with missing endpoint must not publish");
    }

    private struct FakeOralSource : IDiplomacyOralMakePeaceSource
    {
        internal WorldDiplomacyMakePeaceExecutionReceipt Receipt;
        internal bool EndpointsAvailable;
        internal int Executions;
        internal int EndpointLookups;
        internal int Notifications;
        internal string LastLog;
        public DiplomacyOralRoyalSnapshot Capture() => new(true,"player",false,true,true,"npc","speaker",true);
        public WorldDiplomacyMakePeaceExecutionReceipt Execute(WorldDiplomacyMakePeaceCommand command)
        { Executions++;return Receipt; }
        public bool TryResolveAppliedEndpoints(string payerId,string receiverId,
            out string resolvedPayerId,out string resolvedReceiverId)
        { EndpointLookups++;resolvedPayerId=payerId;resolvedReceiverId=receiverId;return EndpointsAvailable; }
        public void NotifyResolved(WorldDiplomacyMakePeaceExecutionReceipt receipt) { Notifications++; }
        public void Log(string message) { LastLog=message; }
    }

    private static void VerifySourceBoundary()
    {
        string contracts = File.ReadAllText(
            FindRepositoryFile("Refactor", "Contracts", "WorldDiplomacyMakePeaceContracts.cs"),
            Encoding.UTF8);
        string rules = File.ReadAllText(
            FindRepositoryFile("Refactor", "Domain", "WorldDiplomacyOralMakePeaceRules.cs"),
            Encoding.UTF8);
        string facade = File.ReadAllText(
            FindRepositoryFile("Refactor", "Modules", "WorldDiplomacyMakePeaceCommandFacade.cs"),
            Encoding.UTF8);
        string adapter = File.ReadAllText(
            FindRepositoryFile("Refactor", "Adapters", "BannerlordWorldDiplomacyMakePeaceGameActionPort.cs"),
            Encoding.UTF8);
        string peaceService = File.ReadAllText(
            FindRepositoryFile("DiplomacyPeaceTermsService.cs"),
            Encoding.UTF8);
        string behavior = File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/Direct/DiplomacyBehavior.cs"), Encoding.UTF8);
        behavior += File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/Direct/DiplomacyBehavior.Actions.cs"));
        string method = ExtractMethod(behavior, "private string TryExecuteMakePeace(");
        string application = File.ReadAllText(
            FindRepositoryFile("src/modules/AF.Module.Diplomacy/Application/DiplomacyOralMakePeaceApplication.cs"), Encoding.UTF8);
        string oralSource = File.ReadAllText(
            FindRepositoryFile("src/modules/AF.Module.Diplomacy/Adapters/DiplomacyOralMakePeaceSource.cs"), Encoding.UTF8);

        Test.True(!contracts.Contains("TaleWorlds", StringComparison.Ordinal)
                  && !rules.Contains("TaleWorlds", StringComparison.Ordinal)
                  && !facade.Contains("TaleWorlds", StringComparison.Ordinal),
            "make-peace contracts, rules, and facade must remain TaleWorlds-free");
        Test.True(method.Contains("DiplomacyOralMakePeaceApplication.Execute(ref source, payload)", StringComparison.Ordinal)
                  && application.Contains("WorldDiplomacyOralMakePeaceRules.ResolveCommand", StringComparison.Ordinal)
                  && application.Contains("source.Execute(resolution.Command)", StringComparison.Ordinal),
            "the live behavior must forward peace resolution and execution to Application");
        Test.True(!method.Contains("(payload ?? \"\").Split(':')", StringComparison.Ordinal)
                  && !method.Contains("IsPlayerNpcPair(", StringComparison.Ordinal),
            "the replaced inline payload and pair algorithm must be removed");
        Test.True(adapter.Contains("TWParallel.IsMainThread()", StringComparison.Ordinal)
                  && adapter.Contains("ResolveKingdom(command.PayerKingdomId)", StringComparison.Ordinal)
                  && adapter.Contains("ResolveKingdom(command.ReceiverKingdomId)", StringComparison.Ordinal)
                  && adapter.Contains("ResolveHero(command.SpeakerHeroId)", StringComparison.Ordinal),
            "game adapter must re-resolve live objects from stable IDs on the main thread");
        Test.True(adapter.Contains("ReferenceEquals(Hero.MainHero, playerKingdom.RulingClan?.Leader)", StringComparison.Ordinal)
                  && adapter.Contains("ReferenceEquals(npcKingdom.RulingClan?.Leader, speaker)", StringComparison.Ordinal)
                  && adapter.Contains("FactionManager.IsAtWarAgainstFaction(payer, receiver)", StringComparison.Ordinal),
            "game adapter must perform final ruler and war-state validation");
        Test.True(adapter.Contains("DiplomacyPeaceTermsService.ResolveTributeAmount", StringComparison.Ordinal)
                  && adapter.Contains("DiplomacyPeaceTermsService.ResolveDurationDays", StringComparison.Ordinal)
                  && adapter.Contains("DiplomacyPeaceTermsService.ApplyPeace(", StringComparison.Ordinal),
            "game adapter must resolve and execute the established peace terms once");
        Test.True(!method.Contains("DiplomacyPeaceTermsService.ApplyPeace(", StringComparison.Ordinal)
                  && application.Contains("if (!receipt.PeaceApplied)", StringComparison.Ordinal),
            "Application must consume the execution receipt instead of applying peace directly");
        Test.True(peaceService.IndexOf("FactionManager.IsAtWarAgainstFaction(payer, receiver)",
                      peaceService.IndexOf("MakePeaceAction.ApplyByKingdomDecision", StringComparison.Ordinal),
                      StringComparison.Ordinal) >= 0
                  && peaceService.IndexOf("DiplomacyRecentPeaceGuard.RegisterPeace", StringComparison.Ordinal)
                      > peaceService.IndexOf("MakePeaceAction.ApplyByKingdomDecision", StringComparison.Ordinal),
            "peace service must confirm the action before registering recent peace");
        int execution = application.IndexOf("source.Execute(resolution.Command)", StringComparison.Ordinal);
        int notification = application.IndexOf("source.NotifyResolved(receipt)", StringComparison.Ordinal);
        Test.True(execution >= 0 && notification > execution
                  && oralSource.Contains("WorldDiplomacyBehavior.NotifyExternalDiplomacyResolved", StringComparison.Ordinal),
            "confirmed-fact notification must remain after successful peace execution");
    }

    private sealed class FakeGameActionPort : IWorldDiplomacyMakePeaceGameActionPort
    {
        internal int CallCount { get; private set; }
        internal bool ThrowOnExecute { get; set; }
        internal WorldDiplomacyMakePeaceCommand LastCommand { get; private set; }
        internal WorldDiplomacyMakePeaceExecutionReceipt NextReceipt { get; set; }

        public WorldDiplomacyMakePeaceExecutionReceipt Execute(WorldDiplomacyMakePeaceCommand command)
        {
            CallCount++;
            LastCommand = command;
            if (ThrowOnExecute) throw new InvalidOperationException("synthetic port failure");
            return NextReceipt;
        }
    }

    private static WorldDiplomacyOralMakePeaceResolution Resolve(
        string payload,
        bool playerExists = true,
        bool playerEliminated = false,
        bool playerIsRuler = true,
        bool npcExists = true,
        bool npcIsRuler = true)
    {
        return WorldDiplomacyOralMakePeaceRules.ResolveCommand(
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
