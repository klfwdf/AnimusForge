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
        if (!value)
        {
            throw new InvalidOperationException(message);
        }
    }

    internal static int Assertions => _assertions;
}

internal static class Program
{
    private static int Main()
    {
        VerifyRejectedCommands();
        VerifyPlayerDeclaration();
        VerifyNpcDeclaration();
        VerifyCommandFacade();
        VerifyApplicationReplay();
        EngineReceiptReplay.Run();
        VerifySourceBoundary();
        Console.WriteLine($"World diplomacy oral declare-war smoke tests passed: {Test.Assertions} assertions.");
        return 0;
    }

    private static void VerifyRejectedCommands()
    {
        Test.True(Resolve(null!).Status == WorldDiplomacyOralDeclareWarResolutionStatus.BadPayloadFormat,
            "missing payload must be rejected as bad format");
        Test.True(Resolve("player").Status == WorldDiplomacyOralDeclareWarResolutionStatus.BadPayloadFormat,
            "single-ID payload must be rejected as bad format");
        Test.True(Resolve(" :npc").Status == WorldDiplomacyOralDeclareWarResolutionStatus.EmptyKingdomId,
            "blank declarer ID must be rejected");
        Test.True(Resolve("player:npc", npcExists: false).Status
                  == WorldDiplomacyOralDeclareWarResolutionStatus.NpcKingdomUnavailable,
            "missing NPC kingdom must be rejected");
        Test.True(Resolve("player:npc", playerExists: false).Status
                  == WorldDiplomacyOralDeclareWarResolutionStatus.PlayerKingdomUnavailable,
            "missing player kingdom must reject the player-declarer direction");
        Test.True(Resolve("player:npc", playerEliminated: true).Status
                  == WorldDiplomacyOralDeclareWarResolutionStatus.PlayerKingdomUnavailable,
            "eliminated player kingdom must reject the player-declarer direction");
        Test.True(Resolve("other:npc").Status
                  == WorldDiplomacyOralDeclareWarResolutionStatus.PlayerDeclarerMismatch,
            "player-declarer payload must identify the current player kingdom");
        Test.True(Resolve("npc:target", npcIsRuler: false).Status
                  == WorldDiplomacyOralDeclareWarResolutionStatus.NpcSpeakerNotRuler,
            "NPC declaration must require the NPC kingdom ruler");
        Test.True(Resolve("first:second").Status
                  == WorldDiplomacyOralDeclareWarResolutionStatus.NpcKingdomMismatch,
            "payload unrelated to the NPC kingdom must be rejected");
    }

    private static void VerifyPlayerDeclaration()
    {
        WorldDiplomacyOralDeclareWarResolution result = Resolve(" player : NPC :ignored");
        Test.True(result.IsReady, "authorized player declaration must resolve");
        Test.True(result.Command.DeclarerKind == WorldDiplomacyDeclareWarDeclarerKind.PlayerKingdom,
            "player-to-NPC direction must be retained");
        Test.True(result.Command.DeclarerKingdomId == "player" && result.Command.TargetKingdomId == "NPC",
            "command IDs must be trimmed without changing payload casing");
        Test.True(result.Command.SpeakerHeroId == "npc-speaker",
            "player declaration must retain the NPC speaker stable ID");
        Test.True(result.FirstKingdomId == "player" && result.SecondKingdomId == "NPC",
            "normalized endpoint diagnostics must be retained");
    }

    private static void VerifyNpcDeclaration()
    {
        WorldDiplomacyOralDeclareWarResolution result = Resolve("NPC: target ");
        Test.True(result.IsReady, "authorized NPC declaration must resolve case-insensitively");
        Test.True(result.Command.DeclarerKind == WorldDiplomacyDeclareWarDeclarerKind.NpcKingdom,
            "NPC-to-target direction must be retained");
        Test.True(result.Command.DeclarerKingdomId == "NPC" && result.Command.TargetKingdomId == "target",
            "NPC command IDs must remain stable trimmed values");
        Test.True(result.Command.SpeakerHeroId == "npc-speaker",
            "NPC declaration must retain the ruler stable ID");
    }

    private static void VerifyCommandFacade()
    {
        FakeGameActionPort port = new FakeGameActionPort();
        WorldDiplomacyDeclareWarCommandFacade facade = new WorldDiplomacyDeclareWarCommandFacade(port);
        WorldDiplomacyDeclareWarCommand invalid = new WorldDiplomacyDeclareWarCommand(
            "",
            "target",
            "speaker",
            WorldDiplomacyDeclareWarDeclarerKind.NpcKingdom);

        WorldDiplomacyDeclareWarExecutionReceipt invalidReceipt = facade.Execute(invalid);
        Test.True(invalidReceipt.Status == WorldDiplomacyDeclareWarExecutionStatus.InvalidCommand,
            "facade must reject an invalid command");
        Test.True(port.CallCount == 0,
            "facade must not call the game action port for an invalid command");

        WorldDiplomacyDeclareWarCommand valid = new WorldDiplomacyDeclareWarCommand(
            "npc",
            "target",
            "npc-speaker",
            WorldDiplomacyDeclareWarDeclarerKind.NpcKingdom);
        port.NextReceipt = new WorldDiplomacyDeclareWarExecutionReceipt(
            WorldDiplomacyDeclareWarExecutionStatus.ActionNotApplied,
            "npc",
            "target",
            "not-applied");
        WorldDiplomacyDeclareWarExecutionReceipt rejectedReceipt = facade.Execute(valid);
        Test.True(port.CallCount == 1 && port.LastCommand.SpeakerHeroId == "npc-speaker",
            "facade must call the game action port exactly once with stable IDs");
        Test.True(rejectedReceipt.Status == WorldDiplomacyDeclareWarExecutionStatus.ActionNotApplied
                  && rejectedReceipt.ErrorCode == "not-applied",
            "facade must preserve the game action receipt");

        port.ThrowOnExecute = true;
        WorldDiplomacyDeclareWarExecutionReceipt exceptionReceipt = facade.Execute(valid);
        Test.True(port.CallCount == 2,
            "facade must make only one port call per valid command");
        Test.True(exceptionReceipt.Status == WorldDiplomacyDeclareWarExecutionStatus.UnknownAfterStart
                  && exceptionReceipt.ErrorCode == "diplomacy.declare_war.port_exception",
            "facade must map a thrown port call to an indeterminate post-start receipt");
    }

    private static void VerifyApplicationReplay()
    {
        var rejected=new FakeOralSource();
        Test.True(DiplomacyOralDeclareWarApplication.Execute(ref rejected,"bad")==""
                  && rejected.Executions==0 && rejected.Notifications==0,
            "invalid payload must not execute or publish");
        var applied=new FakeOralSource
        {
            Receipt=new WorldDiplomacyDeclareWarExecutionReceipt(
                WorldDiplomacyDeclareWarExecutionStatus.Applied,"player","npc",""),
            EndpointsAvailable=true
        };
        Test.True(DiplomacyOralDeclareWarApplication.Execute(ref applied,"player:npc")==""
                  && applied.Executions==1 && applied.Notifications==1
                  && applied.LastCommand.DeclarerKind==WorldDiplomacyDeclareWarDeclarerKind.PlayerKingdom,
            "applied player declaration must publish once after receipt and endpoint resolution");
        var refused=new FakeOralSource
        {
            Receipt=new WorldDiplomacyDeclareWarExecutionReceipt(
                WorldDiplomacyDeclareWarExecutionStatus.AlreadyAtWar,"player","npc","already"),
            EndpointsAvailable=true
        };
        DiplomacyOralDeclareWarApplication.Execute(ref refused,"player:npc");
        Test.True(refused.Executions==1 && refused.Notifications==0 && refused.EndpointLookups==0,
            "rejected receipt must not resolve or publish endpoints");
        var missing=new FakeOralSource
        {
            Receipt=new WorldDiplomacyDeclareWarExecutionReceipt(
                WorldDiplomacyDeclareWarExecutionStatus.Applied,"player","npc",""),
            EndpointsAvailable=false
        };
        DiplomacyOralDeclareWarApplication.Execute(ref missing,"player:npc");
        Test.True(missing.Executions==1 && missing.EndpointLookups==1 && missing.Notifications==0,
            "applied receipt with unavailable endpoints must not publish");
    }

    private struct FakeOralSource : IDiplomacyOralDeclareWarSource
    {
        internal WorldDiplomacyDeclareWarExecutionReceipt Receipt;
        internal WorldDiplomacyDeclareWarCommand LastCommand;
        internal bool EndpointsAvailable;
        internal int Executions;
        internal int EndpointLookups;
        internal int Notifications;
        public DiplomacyOralDeclareWarSnapshot Capture() =>
            new(true,"npc","npc-speaker",true,"player",false,true);
        public WorldDiplomacyDeclareWarExecutionReceipt Execute(WorldDiplomacyDeclareWarCommand command)
        { Executions++;LastCommand=command;return Receipt; }
        public bool TryResolveAppliedEndpoints(string declarerId,string targetId,
            out string resolvedDeclarerId,out string resolvedTargetId)
        { EndpointLookups++;resolvedDeclarerId=declarerId;resolvedTargetId=targetId;return EndpointsAvailable; }
        public void NotifyResolved() { Notifications++; }
        public void Log(string message) { }
    }

    private static void VerifySourceBoundary()
    {
        string contracts = File.ReadAllText(
            FindRepositoryFile("src/AF.Contracts/Internal/WorldDiplomacyDeclareWarContracts.cs"),
            Encoding.UTF8);
        string rules = File.ReadAllText(
            FindRepositoryFile("src", "modules", "AF.Module.Diplomacy", "Domain", "WorldDiplomacyOralDeclareWarRules.cs"),
            Encoding.UTF8);
        string facade = File.ReadAllText(
            FindRepositoryFile("src/modules/AF.Module.Diplomacy/Adapters/Facades/WorldDiplomacyDeclareWarCommandFacade.cs"),
            Encoding.UTF8);
        string adapter = File.ReadAllText(
            FindRepositoryFile("src/modules/AF.Module.Diplomacy/Adapters/BannerlordWorldDiplomacyDeclareWarGameActionPort.cs"),
            Encoding.UTF8);
        string behavior = File.ReadAllText(
            FindRepositoryFile("src/modules/AF.Module.Diplomacy/Direct/DiplomacyBehavior.cs"),
            Encoding.UTF8);
        behavior += File.ReadAllText(FindRepositoryFile("src/modules/AF.Module.Diplomacy/Direct/DiplomacyBehavior.Actions.cs"));
        string method = ExtractMethod(behavior, "private string TryExecuteDeclareWar(");
        string application = File.ReadAllText(
            FindRepositoryFile("src/modules/AF.Module.Diplomacy/Application/DiplomacyOralDeclareWarApplication.cs"), Encoding.UTF8);
        string oralSource = File.ReadAllText(
            FindRepositoryFile("src/modules/AF.Module.Diplomacy/Adapters/DiplomacyOralDeclareWarSource.cs"), Encoding.UTF8);

        Test.True(!contracts.Contains("TaleWorlds", StringComparison.Ordinal)
                  && !rules.Contains("TaleWorlds", StringComparison.Ordinal)
                  && !facade.Contains("TaleWorlds", StringComparison.Ordinal),
            "declare-war contracts, rules, and command facade must remain TaleWorlds-free");
        Test.True(method.Contains("DiplomacyOralDeclareWarApplication.Execute(ref source, payload)", StringComparison.Ordinal)
                  && application.Contains("WorldDiplomacyOralDeclareWarRules.ResolveCommand", StringComparison.Ordinal)
                  && application.Contains("source.Execute(resolution.Command)", StringComparison.Ordinal),
            "legacy behavior must forward the full oral use case to Application");
        Test.True(!method.Contains("(payload ?? \"\").Split(':')", StringComparison.Ordinal)
                  && !method.Contains("string.Equals(id2, npcKingdomId", StringComparison.Ordinal)
                  && !method.Contains("string.Equals(id1, npcKingdomId", StringComparison.Ordinal),
            "replaced inline payload and direction algorithm must be removed");
        Test.True(adapter.Contains("TWParallel.IsMainThread()", StringComparison.Ordinal)
                  && adapter.Contains("ResolveKingdom(command.DeclarerKingdomId)", StringComparison.Ordinal)
                  && adapter.Contains("ResolveKingdom(command.TargetKingdomId)", StringComparison.Ordinal)
                  && adapter.Contains("Hero.FindFirst", StringComparison.Ordinal),
            "game adapter must re-resolve all live objects from stable IDs on the main thread");
        Test.True(adapter.Contains("declarer.IsEliminated", StringComparison.Ordinal)
                  && adapter.Contains("target.IsEliminated", StringComparison.Ordinal)
                  && adapter.Contains("HasCurrentAuthority(command, declarer, target)", StringComparison.Ordinal)
                  && adapter.Contains("ReferenceEquals(declarer, target)", StringComparison.Ordinal)
                  && adapter.Contains("FactionManager.IsAtWarAgainstFaction(declarer, target)", StringComparison.Ordinal)
                  && adapter.Contains("IsAllyWithKingdom(declarer, target)", StringComparison.Ordinal),
            "game adapter must perform final authority and diplomatic-state validation");
        Test.True(!adapter.Contains("EndAlliance", StringComparison.Ordinal),
            "declare-war adapter must not implicitly break an alliance");
        Test.True(CountOccurrences(adapter, "DeclareWarAction.ApplyByKingdomDecision(declarer, target)") == 1
                  && !method.Contains("DeclareWarAction.ApplyByKingdomDecision", StringComparison.Ordinal),
            "game adapter must own the only declaration action call");
        int actionIndex = adapter.IndexOf(
            "DeclareWarAction.ApplyByKingdomDecision(declarer, target)",
            StringComparison.Ordinal);
        int confirmationIndex = adapter.IndexOf(
            "if (!DirectDiplomacyWarGuard.DidDeclarationTakeEffect(FactionManager.IsAtWarAgainstFaction(declarer, target)))",
            actionIndex,
            StringComparison.Ordinal);
        Test.True(actionIndex >= 0 && confirmationIndex > actionIndex,
            "game adapter must confirm the live war state after the action");
        int appliedGuardIndex = application.IndexOf("if (!receipt.IsApplied)", StringComparison.Ordinal);
        int notificationIndex = application.IndexOf("source.NotifyResolved()", StringComparison.Ordinal);
        Test.True(appliedGuardIndex >= 0 && notificationIndex > appliedGuardIndex
                  && oralSource.Contains("WorldDiplomacyBehavior.NotifyExternalDiplomacyResolved", StringComparison.Ordinal),
            "confirmed-fact notification must run only after an Applied receipt");
    }

    private static WorldDiplomacyOralDeclareWarResolution Resolve(
        string payload,
        bool npcExists = true,
        bool playerExists = true,
        bool playerEliminated = false,
        bool npcIsRuler = true)
    {
        return WorldDiplomacyOralDeclareWarRules.ResolveCommand(
            payload,
            npcExists,
            "npc",
            "npc-speaker",
            playerExists,
            "player",
            playerEliminated,
            npcIsRuler);
    }

    private sealed class FakeGameActionPort : IWorldDiplomacyDeclareWarGameActionPort
    {
        internal int CallCount { get; private set; }
        internal bool ThrowOnExecute { get; set; }
        internal WorldDiplomacyDeclareWarCommand LastCommand { get; private set; }
        internal WorldDiplomacyDeclareWarExecutionReceipt NextReceipt { get; set; }

        public WorldDiplomacyDeclareWarExecutionReceipt Execute(WorldDiplomacyDeclareWarCommand command)
        {
            CallCount++;
            LastCommand = command;
            if (ThrowOnExecute)
            {
                throw new InvalidOperationException("synthetic port failure");
            }
            return NextReceipt;
        }
    }

    private static string ExtractMethod(string source, string signature)
    {
        int start = source.IndexOf(signature, StringComparison.Ordinal);
        if (start < 0)
        {
            throw new InvalidOperationException("Could not locate method: " + signature);
        }
        int openBrace = source.IndexOf('{', start);
        int depth = 0;
        for (int index = openBrace; index < source.Length; index++)
        {
            if (source[index] == '{') depth++;
            else if (source[index] == '}' && --depth == 0) return source.Substring(start, index - start + 1);
        }
        throw new InvalidOperationException("Could not parse method: " + signature);
    }

    private static int CountOccurrences(string text, string value)
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
            if (File.Exists(candidate))
            {
                return candidate;
            }
            current = current.Parent;
        }
        throw new FileNotFoundException("Could not locate repository file", Path.Combine(relativeSegments));
    }
}
