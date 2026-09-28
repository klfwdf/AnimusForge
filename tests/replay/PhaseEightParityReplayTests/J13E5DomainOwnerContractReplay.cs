using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

internal static class J13E5DomainOwnerContractReplay
{
    internal static void Run(string repo)
    {
        string Read(string path) => File.ReadAllText(Path.Combine(repo, path));
        void Require(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException("J13E5 owner contract: " + name);
        }
        string Slice(string source, string first, string next)
        {
            int start = source.IndexOf(first, StringComparison.Ordinal);
            int end = start < 0 ? -1 : source.IndexOf(next, start + first.Length, StringComparison.Ordinal);
            Require(start >= 0 && end > start, "method boundary missing: " + first);
            return source.Substring(start, end - start);
        }
        int Count(string source, string value) => source.Split(value, StringSplitOptions.None).Length - 1;

        string host = Read("MilitaryExerciseBehavior.cs");
        string terminal = Read("AnimusForgeTerminalBehavior.cs");
        string patches = Read("src/AF.GameAdapter.Bannerlord/Composition/StartupPatchComposition.cs");
        string identity = Read("src/modules/AF.Module.Exercise/ExerciseMapEventIdentityOwner.cs");
        string session = Read("src/modules/AF.Module.Exercise/MilitaryExerciseSessionOwner.cs");
        string settlement = Read("src/modules/AF.Module.Exercise/ExerciseSettlementOwner.cs");
        string mission = Slice(host, "internal sealed class MilitaryExerciseMissionLogic : MissionLogic",
            "[HarmonyPatch(typeof(SandBox.GameComponents.SandboxAgentDecideKilledOrUnconsciousModel)");
        string open = Slice(host, "public static void OpenExerciseFromTerminal()", "public static bool IsCurrentExerciseRuntime()");
        string reward = Slice(host, "[HarmonyPatch(typeof(PlayerEncounter), \"GetBattleRewards\")]",
            "[HarmonyPatch(typeof(MapEvent), \"ApplyRenownAndInfluenceChanges\")]");
        string cleanup = Slice(host, "internal static void CleanupExerciseRuntime(MilitaryExerciseRuntime runtime,",
            "private static void RestorePlayerHitPointsAfterExercise(");
        string gate = Slice(host, "internal static bool ShouldZeroBattleRewardsForExercise(",
            "internal static void TryCommitXpOnlyBeforeMissionMapEventRemoval(");
        string death = Slice(host, "public static class MilitaryExerciseDeathRatePatch",
            "[HarmonyPatch(typeof(MapEvent), \"CalculateAndCommitMapEventResults\")]");
        string register = Slice(host, "public static void RegisterHarmonyPatches(Harmony harmony)",
            "private static void EnsureHarmonyPatched()");
        string second = Slice(host, "private static void OnSecondTeamScreenClosed(",
            "private static void PrepareDummyPartiesAndSplitRoster(");

        Require(Count(terminal, "MilitaryExerciseBehavior.NeedsEngineTick()") == 1
            && Count(terminal, "MilitaryExerciseBehavior.OnEngineTick()") == 1
            && Count(terminal, "MilitaryExerciseBehavior.OpenExerciseFromTerminal()") == 1
            && Count(patches, "MilitaryExerciseBehavior.RegisterHarmonyPatches(harmony)") == 1,
            "Terminal tick/open or startup patch registration changed");
        foreach (string patch in new[] { "MilitaryExerciseDeathRatePatch", "MilitaryExerciseMapEventXpOnlySettlementPatch",
            "MilitaryExercisePlayerEncounterResultsCleanupPatch", "MilitaryExercisePlayerEncounterContinueCleanupPatch",
            "MilitaryExercisePlayerEncounterVictoryCleanupPatch", "MilitaryExercisePlayerEncounterDefeatCleanupPatch",
            "MilitaryExercisePlayerEncounterEndCleanupPatch", "MilitaryExerciseBattleRewardsZeroPatch",
            "MilitaryExerciseRenownInfluenceSkipPatch" })
            Require(Count(register, "PatchHarmonyClass(harmony, typeof(" + patch + "))") == 1,
                "missing or duplicate Harmony patch: " + patch);
        Require(register.Contains("#if !BANNERLORD_1_4_OR_GREATER", StringComparison.Ordinal)
            && reward.Contains("#if BANNERLORD_1_4_OR_GREATER", StringComparison.Ordinal)
            && reward.Contains("ShouldZeroBattleRewardsForExercise", StringComparison.Ordinal),
            "dual reward/renown conditional drifted");
        Require(death.Contains("Mission.Current?.GetMissionBehavior<MilitaryExerciseMissionLogic>() == null", StringComparison.Ordinal)
            && death.Contains("if (effectedAgent == null || !effectedAgent.IsHuman)", StringComparison.Ordinal)
            && death.Contains("ShouldProtectPlayerOrCompanionFromDeath(effectedAgent)", StringComparison.Ordinal)
            && host.Contains("return !DuelSettings.MilitaryExerciseAllowPlayerDeathEnabled;", StringComparison.Ordinal)
            && host.Contains("return !DuelSettings.MilitaryExerciseAllowCompanionDeathEnabled;", StringComparison.Ordinal),
            "death adjustment no longer scoped to exercise Mission or death MCM toggles");
        Require(gate.Contains("GetRuntimeForMapEvent(mapEvent)", StringComparison.Ordinal)
            && gate.Contains("IsMilitaryExerciseMapEventByDummyParty(mapEvent)", StringComparison.Ordinal)
            && host.Contains("mobileParty?.PartyComponent is MilitaryExerciseDummyPartyComponent", StringComparison.Ordinal)
            && identity.Contains("ReferenceEquals(currentEvent, candidate)", StringComparison.Ordinal)
            && identity.Contains("containsParty(candidate, opponent)", StringComparison.Ordinal),
            "real MapEvent reward exclusion lost identity/component guard");
        Require(open.Contains("_sessionOwner.HasActiveRuntime || _queuedOpenBattle", StringComparison.Ordinal)
            && open.Contains("CanOpenFromCurrentState", StringComparison.Ordinal)
            && host.Contains("_sessionOwner.IsSecondDue(now)", StringComparison.Ordinal)
            && host.Contains("_sessionOwner.IsBattleDue(now)", StringComparison.Ordinal)
            && host.Contains("OnFirstTeamScreenClosed(openedSelection,", StringComparison.Ordinal)
            && host.Contains("OnSecondTeamScreenClosed(openedSelection,", StringComparison.Ordinal)
            && Count(host, "_sessionOwner.IsCurrentSelection(openedSelection)") == 2
            && second.Contains("if (fromCancel)", StringComparison.Ordinal)
            && second.Contains("CleanupSplitRuntime(failedRuntime, \"second_exception\")", StringComparison.Ordinal)
            && session.Contains("ReferenceEquals(Selection, selection)", StringComparison.Ordinal)
            && session.Contains("SecondQueued = false;", StringComparison.Ordinal)
            && session.Contains("BattleQueued = false;", StringComparison.Ordinal),
            "two-stage cancellation, queue expiry, reentry or split failure cleanup disconnected");
        Require(mission.Contains("TryCommitXpOnlyBeforeMissionMapEventRemoval(_runtime, \"OnEndMission\")", StringComparison.Ordinal)
            && mission.Contains("CleanupExerciseRuntime(_runtime, reason, skipXpCommit: false)", StringComparison.Ordinal)
            && host.Contains("runtime.Settlement.TryBeginXpCommit()", StringComparison.Ordinal)
            && host.Contains("runtime.Settlement.TryBeginCleanupXpCommit(skipXpCommit)", StringComparison.Ordinal)
            && settlement.Contains("if (SettlementDone || XpCommitAttempted) return false;", StringComparison.Ordinal)
            && Count(host, "OrphanXpOwner.CommitOnce(") == 3
            && settlement.Contains("ConditionalWeakTable<TEvent, ExerciseSettlementOwner>", StringComparison.Ordinal)
            && cleanup.Contains("runtime.Settlement.TryBeginSettlement()", StringComparison.Ordinal)
            && cleanup.Contains("RestoreRoutedRegularTroops(runtime, reason)", StringComparison.Ordinal)
            && cleanup.Contains("MoveAllMembersBackToMainParty(runtime.OpponentDummyParty", StringComparison.Ordinal)
            && cleanup.Contains("MoveAllMembersBackToMainParty(runtime.HoldingDummyParty", StringComparison.Ordinal)
            && cleanup.Contains("RestoreMainPartyRolesFromSnapshot(runtime, reason)", StringComparison.Ordinal)
            && cleanup.Contains("RestorePlayerHitPointsAfterExercise(runtime)", StringComparison.Ordinal)
            && cleanup.Contains("CleanupMapEventAndPlayerEncounter(runtime.MapEvent, reason)", StringComparison.Ordinal)
            && cleanup.Contains("DestroyDummyParty(runtime.OpponentDummyParty", StringComparison.Ordinal)
            && cleanup.Contains("DestroyDummyParty(runtime.HoldingDummyParty", StringComparison.Ordinal)
            && cleanup.Contains("runtime.Settlement.CompleteSettlement()", StringComparison.Ordinal),
            "XP-only receipt or original roster/Hero/encounter teardown disconnected");

        CheckRewardSignature(Path.Combine(repo, "bin/Debug/single_module_artifacts/versions/1.3/AnimusForge.dll"),
            new[] { "renownChange", "influenceChange", "moraleChange", "goldChange", "playerEarnedLootPercentage",
                "playerEarnedFigurehead", "renownExplainedNumber", "influenceExplainedNumber", "moraleExplainedNumber" }, 6);
        CheckRewardSignature(Path.Combine(repo, "bin/Debug/single_module_artifacts/versions/1.4/AnimusForge.dll"),
            new[] { "renownChange", "influenceChange", "moraleChange", "playerEarnedLootRate", "playerEarnedFigurehead" }, 5);
        CheckRewardSignature(Path.Combine(repo, "bin/Release/single_module_artifacts/versions/1.3/AnimusForge.dll"),
            new[] { "renownChange", "influenceChange", "moraleChange", "goldChange", "playerEarnedLootPercentage",
                "playerEarnedFigurehead", "renownExplainedNumber", "influenceExplainedNumber", "moraleExplainedNumber" }, 6);
        CheckRewardSignature(Path.Combine(repo, "bin/Release/single_module_artifacts/versions/1.4/AnimusForge.dll"),
            new[] { "renownChange", "influenceChange", "moraleChange", "playerEarnedLootRate", "playerEarnedFigurehead" }, 5);
        Console.WriteLine("PASS J13E5DomainOwnerContractReplay Terminal/Harmony/Mission/death/reward/XP/restoration wiring and four-DLL reward signatures; source-wiring-only live=NOT_RUN");
    }

    private static void CheckRewardSignature(string path, string[] expectedNames, int expectedOut)
    {
        using FileStream stream = File.OpenRead(path);
        using PEReader pe = new PEReader(stream);
        MetadataReader reader = pe.GetMetadataReader();
        TypeDefinition type = reader.TypeDefinitions.Select(handle => reader.GetTypeDefinition(handle))
            .Single(definition => reader.GetString(definition.Name) == "MilitaryExerciseBattleRewardsZeroPatch");
        MethodDefinition method = type.GetMethods().Select(handle => reader.GetMethodDefinition(handle))
            .Single(definition => reader.GetString(definition.Name) == "Prefix");
        string[] names = method.GetParameters().Select(handle => reader.GetParameter(handle))
            .Where(parameter => parameter.SequenceNumber > 0)
            .OrderBy(parameter => parameter.SequenceNumber)
            .Select(parameter => reader.GetString(parameter.Name)).ToArray();
        int outCount = method.GetParameters().Select(handle => reader.GetParameter(handle))
            .Count(parameter => parameter.SequenceNumber > 0
                && (parameter.Attributes & ParameterAttributes.Out) != 0);
        if (!names.SequenceEqual(expectedNames) || outCount != expectedOut)
            throw new InvalidOperationException("J13E5 reward signature drift: " + path);
    }
}
