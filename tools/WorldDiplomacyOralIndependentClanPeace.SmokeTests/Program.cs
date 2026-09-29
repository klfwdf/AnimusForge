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
        VerifyResolution();
        VerifyFacade();
        VerifyApplicationReplay();
        VerifySourceBoundary();
        Console.WriteLine(
            $"World diplomacy oral independent-clan peace smoke tests passed: {Test.Assertions} assertions.");
        return 0;
    }

    private static void VerifyResolution()
    {
        Test.True(Resolve("unexpected").Status
                  == WorldDiplomacyOralIndependentClanPeaceResolutionStatus.UnexpectedPayload,
            "independent-clan peace must reject every nonblank payload");
        Test.True(Resolve("  ").IsReady,
            "whitespace payload must preserve the existing no-payload behavior");
        Test.True(Resolve("", contextAvailable: false).Status
                  == WorldDiplomacyOralIndependentClanPeaceResolutionStatus.ContextUnavailable,
            "stale runtime eligibility must reject the command");
        Test.True(Resolve("", playerClanId: "").Status
                  == WorldDiplomacyOralIndependentClanPeaceResolutionStatus.MissingIdentity,
            "missing stable identities must reject the command");

        WorldDiplomacyOralIndependentClanPeaceResolution ready = Resolve("");
        Test.True(ready.IsReady, "eligible no-payload action must resolve");
        Test.True(ready.Command.PlayerClanId == "player-clan"
                  && ready.Command.TargetKingdomId == "target-kingdom"
                  && ready.Command.SpeakerHeroId == "target-ruler",
            "the command must carry only the three stable identities");
    }

    private static void VerifyFacade()
    {
        FakePort port = new FakePort();
        WorldDiplomacyIndependentClanPeaceCommandFacade facade =
            new WorldDiplomacyIndependentClanPeaceCommandFacade(port);
        WorldDiplomacyIndependentClanPeaceCommand invalid =
            new WorldDiplomacyIndependentClanPeaceCommand("player-clan", "target-kingdom", "");
        Test.True(facade.Execute(invalid).Status
                  == WorldDiplomacyIndependentClanPeaceExecutionStatus.InvalidCommand,
            "facade must reject an invalid command");
        Test.True(port.CallCount == 0, "invalid command must not invoke the game port");

        WorldDiplomacyIndependentClanPeaceCommand valid =
            new WorldDiplomacyIndependentClanPeaceCommand(
                "player-clan",
                "target-kingdom",
                "target-ruler");
        port.NextReceipt = new WorldDiplomacyIndependentClanPeaceExecutionReceipt(
            WorldDiplomacyIndependentClanPeaceExecutionStatus.ActionNotApplied,
            "player-clan",
            "target-kingdom",
            "target-ruler",
            "not-applied");
        Test.True(facade.Execute(valid).Status
                  == WorldDiplomacyIndependentClanPeaceExecutionStatus.ActionNotApplied,
            "facade must preserve the game-port receipt");
        Test.True(port.CallCount == 1 && port.LastCommand.SpeakerHeroId == "target-ruler",
            "valid command must invoke the game port exactly once");

        port.ThrowOnExecute = true;
        WorldDiplomacyIndependentClanPeaceExecutionReceipt exceptionReceipt = facade.Execute(valid);
        Test.True(port.CallCount == 2
                  && exceptionReceipt.Status
                  == WorldDiplomacyIndependentClanPeaceExecutionStatus.UnknownAfterStart,
            "a thrown port call must become one indeterminate receipt");
    }

    private static void VerifyApplicationReplay()
    {
        var unavailable=new FakeOralSource{ContextAvailable=false};
        Test.True(DiplomacyOralIndependentPeaceApplication.Execute(ref unavailable,"")==""
                  && unavailable.Executions==0 && unavailable.LastLog.Contains("Rejected status="),
            "stale independent peace context must reject before effect");
        var applied=new FakeOralSource
        {
            ContextAvailable=true,
            Receipt=new WorldDiplomacyIndependentClanPeaceExecutionReceipt(
                WorldDiplomacyIndependentClanPeaceExecutionStatus.Applied,"player","target","speaker","")
        };
        Test.True(DiplomacyOralIndependentPeaceApplication.Execute(ref applied,"")==""
                  && applied.Executions==1 && applied.LastCommand.PlayerClanId=="player"
                  && applied.LastLog.Contains("success playerClan=player"),
            "applied independent peace receipt must be logged after one effect");
        var refused=new FakeOralSource
        {
            ContextAvailable=true,
            Receipt=new WorldDiplomacyIndependentClanPeaceExecutionReceipt(
                WorldDiplomacyIndependentClanPeaceExecutionStatus.ConstantWar,"player","target","speaker","constant")
        };
        DiplomacyOralIndependentPeaceApplication.Execute(ref refused,"");
        Test.True(refused.Executions==1 && refused.LastLog.Contains("Rejected status=ConstantWar"),
            "failed effect receipt must remain rejected");
    }

    private struct FakeOralSource : IDiplomacyOralIndependentPeaceSource
    {
        internal bool ContextAvailable;
        internal int Executions;
        internal string LastLog;
        internal WorldDiplomacyIndependentClanPeaceCommand LastCommand;
        internal WorldDiplomacyIndependentClanPeaceExecutionReceipt Receipt;
        public DiplomacyOralIndependentPeaceSnapshot Capture() =>
            new(ContextAvailable,"player","target","speaker");
        public WorldDiplomacyIndependentClanPeaceExecutionReceipt Execute(WorldDiplomacyIndependentClanPeaceCommand command)
        { Executions++;LastCommand=command;return Receipt; }
        public void Log(string message) { LastLog=message; }
    }

    private static void VerifySourceBoundary()
    {
        string contracts = Read("Refactor", "Contracts",
            "WorldDiplomacyIndependentClanPeaceContracts.cs");
        string rules = Read("Refactor", "Domain",
            "WorldDiplomacyOralIndependentClanPeaceRules.cs");
        string facade = Read("Refactor", "Modules",
            "WorldDiplomacyIndependentClanPeaceCommandFacade.cs");
        string adapter = Read("Refactor", "Adapters",
            "BannerlordWorldDiplomacyIndependentClanPeaceGameActionPort.cs");
        string behavior = (Read("src/modules/AF.Module.Diplomacy/Direct/DiplomacyBehavior.cs") + Read("src/modules/AF.Module.Diplomacy/Direct/DiplomacyBehavior.Actions.cs"));
        string eligibility = Read("src/modules/AF.Module.Diplomacy/Application/DiplomacyIndependentPeaceApplication.cs");
        string application = Read("src/modules/AF.Module.Diplomacy/Application/DiplomacyOralIndependentPeaceApplication.cs");
        string oralSource = Read("src/modules/AF.Module.Diplomacy/Adapters/DiplomacyOralIndependentPeaceSource.cs");
        string method = ExtractMethod(behavior, "private string TryExecuteIndependentClanPeace(");

        Test.True(!contracts.Contains("TaleWorlds", StringComparison.Ordinal)
                  && !rules.Contains("TaleWorlds", StringComparison.Ordinal)
                  && !facade.Contains("TaleWorlds", StringComparison.Ordinal),
            "contracts, rules, and facade must remain TaleWorlds-free");
        Test.True(method.Contains("DiplomacyOralIndependentPeaceApplication.Execute(ref source, payload)", StringComparison.Ordinal)
                  && application.Contains("WorldDiplomacyOralIndependentClanPeaceRules.ResolveCommand", StringComparison.Ordinal)
                  && application.Contains("source.Execute(resolution.Command)", StringComparison.Ordinal)
                  && oralSource.Contains("CommandFacade.Execute(command)", StringComparison.Ordinal),
            "behavior must forward independent peace command and receipt ordering to Application");
        Test.True(!method.Contains("MakePeaceAction.Apply", StringComparison.Ordinal)
                  && !method.Contains("DiplomacyRecentPeaceGuard.RegisterPeace", StringComparison.Ordinal)
                  && !method.Contains("RunWithDiplomaticSideEffectsUnlocked", StringComparison.Ordinal),
            "behavior must no longer own the independent-clan peace action");
        Test.True(adapter.Contains("TWParallel.IsMainThread()", StringComparison.Ordinal)
                  && adapter.Contains("Clan.PlayerClan ?? Hero.MainHero.Clan", StringComparison.Ordinal)
                  && adapter.Contains("Hero.Find(command.SpeakerHeroId)", StringComparison.Ordinal)
                  && adapter.Contains("targetClan.Kingdom ?? speaker.MapFaction as Kingdom",
                      StringComparison.Ordinal),
            "adapter must re-resolve the three live endpoints without a world scan");
        Test.True(!adapter.Contains("Clan.All", StringComparison.Ordinal)
                  && !adapter.Contains("Kingdom.All", StringComparison.Ordinal)
                  && !adapter.Contains("Hero.FindFirst", StringComparison.Ordinal),
            "the user-triggered adapter must use direct current-context lookups");
        Test.True(adapter.Contains(
                      "FactionManager.IsAtConstantWarAgainstFaction(playerClan, targetKingdom)",
                      StringComparison.Ordinal),
            "adapter must preserve the non-negotiable constant-war guard");
        Test.True(Count(adapter, "MakePeaceAction.Apply(playerClan, targetKingdom)") == 1,
            "adapter must attempt the peace action exactly once");
        int action = adapter.IndexOf(
            "MakePeaceAction.Apply(playerClan, targetKingdom)",
            StringComparison.Ordinal);
        int confirmation = adapter.IndexOf(
            "if (FactionManager.IsAtWarAgainstFaction(playerClan, targetKingdom))",
            action,
            StringComparison.Ordinal);
        int register = adapter.IndexOf(
            "DiplomacyRecentPeaceGuard.RegisterPeace(playerClan, targetKingdom, ActionSource)",
            StringComparison.Ordinal);
        Test.True(action >= 0 && confirmation > action && register > confirmation,
            "recent peace may be registered only after the action is confirmed");
        Test.True(!behavior.Contains("IndependentClanPeaceTag =", StringComparison.Ordinal)
                  && behavior.Contains("DiplomacyConversationEligibilityApplication.IsIndependentClanPeaceTag(", StringComparison.Ordinal)
                  && behavior.Contains("DiplomacyIndependentPeaceApplication.CanUse(", StringComparison.Ordinal)
                  && eligibility.Contains("!IsEligible(source.CapturePlayer())", StringComparison.Ordinal)
                  && eligibility.Contains("!IsEligible(source.CaptureSpeaker())", StringComparison.Ordinal)
                  && eligibility.Contains("!IsEligible(source.CaptureTarget())", StringComparison.Ordinal),
            "tag identity and prompt eligibility must route through Application");
    }

    private sealed class FakePort : IWorldDiplomacyIndependentClanPeaceGameActionPort
    {
        internal int CallCount { get; private set; }
        internal bool ThrowOnExecute { get; set; }
        internal WorldDiplomacyIndependentClanPeaceCommand LastCommand { get; private set; }
        internal WorldDiplomacyIndependentClanPeaceExecutionReceipt NextReceipt { get; set; }

        public WorldDiplomacyIndependentClanPeaceExecutionReceipt Execute(
            WorldDiplomacyIndependentClanPeaceCommand command)
        {
            CallCount++;
            LastCommand = command;
            if (ThrowOnExecute) throw new InvalidOperationException("synthetic port failure");
            return NextReceipt;
        }
    }

    private static WorldDiplomacyOralIndependentClanPeaceResolution Resolve(
        string payload,
        bool contextAvailable = true,
        string playerClanId = "player-clan",
        string targetKingdomId = "target-kingdom",
        string speakerHeroId = "target-ruler")
    {
        return WorldDiplomacyOralIndependentClanPeaceRules.ResolveCommand(
            payload,
            contextAvailable,
            playerClanId,
            targetKingdomId,
            speakerHeroId);
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
