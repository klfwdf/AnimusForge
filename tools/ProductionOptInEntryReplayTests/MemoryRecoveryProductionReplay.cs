using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

internal static class MemoryRecoveryProductionReplay
{
    private const BindingFlags DeclaredStatic = BindingFlags.DeclaredOnly
        | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private const BindingFlags AnyInstance = BindingFlags.Instance
        | BindingFlags.Public | BindingFlags.NonPublic;
    private const BindingFlags AnyStatic = BindingFlags.Static
        | BindingFlags.Public | BindingFlags.NonPublic;

    private static readonly string[] ForbiddenRecoveryMemberFragments =
    {
        "ActionPlan",
        "ActionRequest",
        "IActionPlanExecutor",
        "InteractionResult",
        "Raw",
        "Postprocess",
        "AfterCommit"
    };

    private static int _assertions;

    public static void Run(Assembly production)
    {
        Require(production != null, "production assembly is required");

        Type ownerType = RequireType(production, "AnimusForge.MyBehavior");
        Type memoryCommitType = RequireType(
            production,
            "AnimusForge.Refactor.Contracts.InteractionMemoryCommit");
        Type channelType = RequireType(
            production,
            "AnimusForge.Refactor.Contracts.InteractionChannel");
        Type factType = RequireType(
            production,
            "AnimusForge.Refactor.Contracts.FactRecord");
        Type resultType = RequireType(
            production,
            "AnimusForge.Refactor.Contracts.MemoryCommitResult");

        VerifyOwnerEntryPointAbi(ownerType, memoryCommitType, resultType);
        VerifyCompatibilityVoidAbi(ownerType);
        VerifyMemoryCommitConstructorAbi(memoryCommitType, channelType, factType);
        VerifyRecoveryIsolationAndBounds(production, ownerType);
        VerifyHiddenMarkerReadback(production, ownerType);
        VerifyPersistedMarkerReconciliation(production, ownerType);
        VerifyAdapterTraceNonce(production);

        Console.WriteLine(
            "PASS memoryRecoveryProductionReplay abi=1 isolatedPayload=1 bounds=64/512 hiddenMarkers=1 rebuildPreservesMarkers=1 tombstoneReconcile=1 orphanCleanup=1 wrongOwnerQuarantine=1 processNonce=1 sceneProvenance=1 assertions="
            + _assertions);
    }

    private static void VerifyOwnerEntryPointAbi(
        Type ownerType,
        Type memoryCommitType,
        Type resultType)
    {
        MethodInfo[] legacyByName = ownerType.GetMethods(AnyStatic)
            .Where(method => method.Name == "CommitExternalDialogueHistory")
            .ToArray();
        Require(legacyByName.Length == 1,
            "CommitExternalDialogueHistory must remain the sole method with its legacy name");
        MethodInfo legacy = legacyByName[0];
        Require(legacy.IsPublic && legacy.IsStatic,
            "CommitExternalDialogueHistory is no longer public static");
        Require(legacy.ReturnType == resultType,
            "CommitExternalDialogueHistory return type changed");
        RequireParameterTypes(
            legacy,
            typeof(string), typeof(bool), typeof(string),
            typeof(string), typeof(string), typeof(string));

        // This unqualified lookup is intentionally retained as the regression
        // check: a same-name overload would throw AmbiguousMatchException.
        Require(ownerType.GetMethod("CommitExternalDialogueHistory") == legacy,
            "legacy CommitExternalDialogueHistory lookup is ambiguous or unstable");

        MethodInfo[] recoverableByName = ownerType.GetMethods(AnyStatic)
            .Where(method => method.Name == "CommitExternalDialogueHistoryRecoverable")
            .ToArray();
        Require(recoverableByName.Length == 1,
            "recoverable memory owner entry point is missing or overloaded");
        MethodInfo recoverable = recoverableByName[0];
        Require(recoverable.IsAssembly && !recoverable.IsPublic && recoverable.IsStatic,
            "recoverable memory owner entry point must remain internal static");
        Require(recoverable.ReturnType == resultType,
            "recoverable memory owner return type changed");
        RequireParameterTypes(recoverable, memoryCommitType, typeof(bool), typeof(string));
        Require(ownerType.GetMethod(
                "CommitExternalDialogueHistoryRecoverable",
                AnyStatic) == recoverable,
            "recoverable owner lookup is ambiguous");
    }

    private static void VerifyCompatibilityVoidAbi(Type ownerType)
    {
        Type heroType = Assembly.Load("TaleWorlds.CampaignSystem")
            .GetType("TaleWorlds.CampaignSystem.Hero", throwOnError: true);

        VerifyVoidMethod(
            ownerType,
            "AppendExternalDialogueHistory",
            new[] { heroType, typeof(string), typeof(string), typeof(string) },
            Array.Empty<int>());
        VerifyVoidMethod(
            ownerType,
            "AppendExternalSceneDialogueHistory",
            new[]
            {
                heroType, typeof(string), typeof(string), typeof(string),
                typeof(int), typeof(int), typeof(string)
            },
            new[] { 5, 6 });
        VerifyVoidMethod(
            ownerType,
            "AppendExternalNonHeroDialogueHistory",
            new[]
            {
                typeof(string), typeof(string), typeof(string),
                typeof(string), typeof(string)
            },
            Array.Empty<int>());
        VerifyVoidMethod(
            ownerType,
            "AppendExternalNonHeroSceneDialogueHistory",
            new[]
            {
                typeof(string), typeof(string), typeof(string), typeof(string),
                typeof(string), typeof(int), typeof(int), typeof(string)
            },
            new[] { 6, 7 });
    }

    private static void VerifyMemoryCommitConstructorAbi(
        Type memoryCommitType,
        Type channelType,
        Type factType)
    {
        ConstructorInfo[] publicConstructors = memoryCommitType
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public);
        Require(publicConstructors.Length == 1,
            "InteractionMemoryCommit must retain exactly one public constructor");
        Type factsType = typeof(IEnumerable<>).MakeGenericType(factType);
        RequireParameterTypes(
            publicConstructors[0],
            typeof(string), channelType, typeof(string), typeof(string),
            typeof(string), typeof(string), factsType);

        foreach (ConstructorInfo constructor in memoryCommitType
            .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic))
        {
            Require(!constructor.IsPublic,
                "provenance constructor became public");
            ParameterInfo[] parameters = constructor.GetParameters();
            Require(parameters.Length >= 7,
                "non-public InteractionMemoryCommit constructor lost the compatibility prefix");
            RequireParameterPrefix(
                constructor,
                typeof(string), channelType, typeof(string), typeof(string),
                typeof(string), typeof(string), factsType);
        }

        VerifyInternalProperty(memoryCommitType, "SceneSessionId", typeof(int));
        VerifyInternalProperty(memoryCommitType, "TargetAgentIndex", typeof(int));
        VerifyInternalProperty(memoryCommitType, "TargetName", typeof(string));

        ConstructorInfo provenanceConstructor = memoryCommitType
            .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(constructor => constructor.GetParameters().Length > 7);
        object sceneCommit = provenanceConstructor.Invoke(new object[]
        {
            "scene-provenance-commit",
            Enum.Parse(channelType, "SceneShout"),
            "scene-provenance-session",
            "af_nonhero:scene-provenance",
            "player",
            "assistant",
            Array.CreateInstance(factType, 0),
            11L,
            12L,
            "af-trace-scene-provenance",
            84,
            17,
            "scene-provenance-location",
            73,
            9,
            "Scene Target"
        });
        Require((int)Get(sceneCommit, "SceneSessionId") == 73,
            "InteractionMemoryCommit lost SceneSessionId provenance");
        Require((int)Get(sceneCommit, "TargetAgentIndex") == 9,
            "InteractionMemoryCommit lost TargetAgentIndex provenance");
        Require((string)Get(sceneCommit, "TargetName") == "Scene Target",
            "InteractionMemoryCommit lost TargetName provenance");
    }

    private static void VerifyRecoveryIsolationAndBounds(Assembly production, Type ownerType)
    {
        Type ledgerType = RequireType(
            production,
            "AnimusForge.Refactor.Runtime.InteractionMemoryRecoveryLedger");
        RequireConstant(ledgerType, "MaximumPendingEntries", 64);
        RequireConstant(ledgerType, "MaximumCompletedEntries", 512);
        RequireConstant(
            ownerType,
            "InteractionMemoryRecoveryStorageKey",
            "_af_interactionMemoryRecovery_v1");

        string[] recoveryTypes =
        {
            "AnimusForge.Refactor.Runtime.InteractionMemoryRecoveryLedger",
            "AnimusForge.Refactor.Runtime.InteractionMemoryRecoverySeed",
            "AnimusForge.Refactor.Runtime.InteractionMemoryRecoveryComponentSeed",
            "AnimusForge.Refactor.Runtime.InteractionMemoryRecoveryWorkItem",
            "AnimusForge.Refactor.Runtime.InteractionMemoryRecoveryEntry",
            "AnimusForge.Refactor.Runtime.InteractionMemoryRecoveryComponent"
        };
        foreach (string typeName in recoveryTypes)
        {
            Type type = RequireType(production, typeName);
            BindingFlags declaredMembers = BindingFlags.DeclaredOnly
                | BindingFlags.Instance | BindingFlags.Static
                | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (FieldInfo field in type.GetFields(declaredMembers))
            {
                RequireRecoverySignatureIsIsolated(
                    type,
                    field.Name + ":" + FriendlyTypeName(field.FieldType));
            }
            foreach (PropertyInfo property in type.GetProperties(declaredMembers))
            {
                RequireRecoverySignatureIsIsolated(
                    type,
                    property.Name + ":" + FriendlyTypeName(property.PropertyType));
            }
            foreach (MethodInfo method in type.GetMethods(declaredMembers))
            {
                string signature = method.Name + ":" + FriendlyTypeName(method.ReturnType)
                    + "(" + string.Join(",", method.GetParameters()
                        .Select(parameter => FriendlyTypeName(parameter.ParameterType))) + ")";
                RequireRecoverySignatureIsIsolated(type, signature);
            }
            foreach (ConstructorInfo constructor in type.GetConstructors(declaredMembers))
            {
                string signature = ".ctor(" + string.Join(",", constructor.GetParameters()
                    .Select(parameter => FriendlyTypeName(parameter.ParameterType))) + ")";
                RequireRecoverySignatureIsIsolated(type, signature);
            }
        }


        foreach (string typeName in new[]
        {
            "AnimusForge.Refactor.Runtime.InteractionMemoryRecoverySeed",
            "AnimusForge.Refactor.Runtime.InteractionMemoryRecoveryWorkItem",
            "AnimusForge.Refactor.Runtime.InteractionMemoryRecoveryEntry"
        })
        {
            Type provenanceType = RequireType(production, typeName);
            VerifyInternalProperty(provenanceType, "SceneSessionId", typeof(int));
            VerifyInternalProperty(provenanceType, "TargetAgentIndex", typeof(int));
            VerifyInternalProperty(provenanceType, "TargetName", typeof(string));
        }
    }

    private static void RequireRecoverySignatureIsIsolated(Type ownerType, string signature)
    {
        foreach (string forbidden in ForbiddenRecoveryMemberFragments)
        {
            Require(signature.IndexOf(forbidden, StringComparison.OrdinalIgnoreCase) < 0,
                ownerType.FullName + " member " + signature
                + " admits forbidden recovery surface " + forbidden);
        }
    }

    private static void VerifyHiddenMarkerReadback(Assembly production, Type ownerType)
    {
        Type draftType = RequireNestedType(ownerType, "DailyMemoryDraft");
        Type dailyLineType = RequireNestedType(ownerType, "DailyMemoryLine");
        Type dialogueDayType = RequireNestedType(ownerType, "DialogueDay");
        Type workType = RequireType(
            production,
            "AnimusForge.Refactor.Runtime.InteractionMemoryRecoveryWorkItem");

        VerifyField(dailyLineType, "MemoryCommitId", typeof(string));
        VerifyField(dailyLineType, "MemoryCommitPart", typeof(string));
        VerifyField(dailyLineType, "MemoryCommitHash", typeof(string));
        VerifyField(dailyLineType, "MemoryCommitOriginGameDay", typeof(int));
        VerifyField(dailyLineType, "MemoryCommitOriginGameDate", typeof(string));
        VerifyField(
            dialogueDayType,
            "MemoryCommitMarkers",
            typeof(Dictionary<string, string>));

        const string ownerId = "af_nonhero:local7h-production-replay";
        const string recoveryId =
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
        const string payloadHash =
            "BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB";
        const string conflictingHash =
            "CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC";
        const string part = "user";
        const int dayIndex = 84;

        object owner = RuntimeHelpers.GetUninitializedObject(ownerType);
        object work = New(workType);
        Set(work, "RecoveryId", recoveryId);
        Set(work, "PayloadHash", payloadHash);
        Set(work, "SubjectId", ownerId);
        Set(work, "Part", part);

        object dailyLine = New(dailyLineType);
        Set(dailyLine, "GameDayIndex", dayIndex + 1);
        Set(dailyLine, "Text", "daily marker fixture");
        Set(dailyLine, "MemoryCommitId", recoveryId);
        Set(dailyLine, "MemoryCommitPart", part);
        Set(dailyLine, "MemoryCommitHash", payloadHash);
        Set(dailyLine, "MemoryCommitOriginGameDay", dayIndex);
        Set(dailyLine, "MemoryCommitOriginGameDate", "origin-day-84");
        object draft = New(draftType);
        Set(draft, "HeroId", ownerId);
        Set(draft, "GameDayIndex", dayIndex + 1);
        Add(Get(draft, "Lines"), dailyLine);
        IList drafts = NewList(draftType, draft);
        InvokeInstance(ownerType, owner, "SaveDailyMemoryDraftsById", ownerId, drafts);

        MethodInfo dailyReadback = RequireInstanceMethod(
            ownerType,
            "HasDailyInteractionMemoryMarker");
        AssertMarker(dailyReadback, owner, work, expectedMatch: true, expectedConflict: false,
            "daily matching marker");
        Set(work, "PayloadHash", conflictingHash);
        AssertMarker(dailyReadback, owner, work, expectedMatch: false, expectedConflict: true,
            "daily conflicting marker");
        Set(work, "PayloadHash", payloadHash);

        IList clonedDrafts = InvokeGenericClone(ownerType, drafts.GetType(), drafts);
        object clonedDraft = clonedDrafts[0];
        object clonedDailyLine = ((IList)Get(clonedDraft, "Lines"))[0];
        Require((string)Get(clonedDailyLine, "MemoryCommitId") == recoveryId,
            "ordinary daily JSON copy lost recovery id");
        Require((string)Get(clonedDailyLine, "MemoryCommitPart") == part,
            "ordinary daily JSON copy lost recovery part");
        Require((string)Get(clonedDailyLine, "MemoryCommitHash") == payloadHash,
            "ordinary daily JSON copy lost recovery hash");
        Require((int)Get(clonedDailyLine, "GameDayIndex") == dayIndex + 1,
            "daily copy changed cross-day storage day");
        Require((int)Get(clonedDailyLine, "MemoryCommitOriginGameDay") == dayIndex,
            "daily copy lost cross-day origin day");
        Require((string)Get(clonedDailyLine, "MemoryCommitOriginGameDate") == "origin-day-84",
            "daily copy lost cross-day origin date");

        object dialogueDay = New(dialogueDayType);
        Set(dialogueDay, "GameDayIndex", dayIndex);
        Set(dialogueDay, "GameDate", "fixture-date");
        IList recentLines = (IList)Get(dialogueDay, "Lines");
        for (int index = 0; index < 261; index++)
        {
            recentLines.Add("recent marker fixture " + index);
        }
        IDictionary markers = (IDictionary)Get(dialogueDay, "MemoryCommitMarkers");
        markers.Add(recoveryId + ":" + part, payloadHash);
        IList dialogueDays = NewList(dialogueDayType, dialogueDay);
        InvokeInstance(ownerType, owner, "SaveDialogueHistoryById", ownerId, dialogueDays);

        MethodInfo recentReadback = RequireInstanceMethod(
            ownerType,
            "HasRecentInteractionMemoryMarker");
        AssertMarker(recentReadback, owner, work, expectedMatch: true, expectedConflict: false,
            "recent matching marker");
        Set(work, "PayloadHash", conflictingHash);
        AssertMarker(recentReadback, owner, work, expectedMatch: false, expectedConflict: true,
            "recent conflicting marker");
        Set(work, "PayloadHash", payloadHash);

        MethodInfo trim = ownerType.GetMethods(DeclaredStatic)
            .Single(method => method.Name == "TrimDialogueHistoryForMemoryRecovery");
        IList rebuilt = (IList)(trim.Invoke(null, new object[] { dialogueDays })
            ?? throw new InvalidOperationException("recent rebuild returned null"));
        Require(rebuilt.Cast<object>()
                .Sum(item => ((IList)Get(item, "Lines")).Count) == 260,
            "recent rebuild did not retain its documented 260-line bound");
        object rebuiltDay = rebuilt.Cast<object>()
            .Single(item => (int)Get(item, "GameDayIndex") == dayIndex);
        IDictionary rebuiltMarkers = (IDictionary)Get(rebuiltDay, "MemoryCommitMarkers");
        Require((string)rebuiltMarkers[recoveryId + ":" + part] == payloadHash,
            "ordinary recent-history rebuild lost recovery marker");

        InvokeInstance(ownerType, owner, "SaveDialogueHistoryById", ownerId, rebuilt);
        AssertMarker(recentReadback, owner, work, expectedMatch: true, expectedConflict: false,
            "rebuilt recent matching marker");
    }

    private static void VerifyPersistedMarkerReconciliation(Assembly production, Type ownerType)
    {
        VerifyCompletedTombstoneMissingMarkerIsBlocked(production, ownerType);
        VerifyCompletedTombstoneWithAllMarkersIsRetained(production, ownerType);
        VerifyOrphanMarkersAreCleared(production, ownerType);
        VerifyWrongOwnerMarkersAreQuarantined(production, ownerType);
    }

    private static void VerifyCompletedTombstoneMissingMarkerIsBlocked(
        Assembly production,
        Type ownerType)
    {
        LedgerFixture fixture = CreateLedgerFixture(
            production,
            "missing-recent-marker-commit",
            "af_nonhero:missing-recent-marker",
            complete: true);
        object owner = CreateOwnerWithLedger(ownerType, fixture.Ledger);
        StoreDailyMarkers(
            ownerType,
            owner,
            fixture.SubjectId,
            fixture.RecoveryId,
            fixture.PayloadHash,
            "user",
            "fact",
            "assistant");
        IDictionary markers = StoreRecentMarkers(
            ownerType,
            owner,
            fixture.SubjectId,
            fixture.RecoveryId,
            fixture.PayloadHash,
            "user",
            "fact");

        InvokeInstance(ownerType, owner, "ReconcilePersistedInteractionMemoryRecoveryMarkers");

        Require(!InvokeLedgerBool(fixture.Ledger, "IsCompleted", fixture.RecoveryId),
            "completed tombstone survived with a missing recent marker");
        Require(ReadInt(fixture.Ledger, "QuarantineCount") == 1,
            "missing-marker tombstone was not quarantined");
        (string beginStatus, string beginError) = BeginAgain(fixture);
        Require(beginStatus == "Rejected" && beginError == "memory_recovery_quarantined",
            "missing-marker tombstone still produced a duplicate receipt");
        Require(markers.Count == 0,
            "quarantined missing-marker tombstone retained recent markers");
    }

    private static void VerifyCompletedTombstoneWithAllMarkersIsRetained(
        Assembly production,
        Type ownerType)
    {
        LedgerFixture fixture = CreateLedgerFixture(
            production,
            "complete-recent-markers-commit",
            "af_nonhero:complete-recent-markers",
            complete: true);
        object owner = CreateOwnerWithLedger(ownerType, fixture.Ledger);
        StoreDailyMarkers(
            ownerType,
            owner,
            fixture.SubjectId,
            fixture.RecoveryId,
            fixture.PayloadHash,
            "user",
            "fact",
            "assistant");
        IDictionary markers = StoreRecentMarkers(
            ownerType,
            owner,
            fixture.SubjectId,
            fixture.RecoveryId,
            fixture.PayloadHash,
            "user",
            "fact",
            "assistant");

        InvokeInstance(ownerType, owner, "ReconcilePersistedInteractionMemoryRecoveryMarkers");

        Require(InvokeLedgerBool(fixture.Ledger, "IsCompleted", fixture.RecoveryId),
            "complete marker set discarded a valid tombstone");
        Require(ReadInt(fixture.Ledger, "QuarantineCount") == 0,
            "complete marker set was incorrectly quarantined");
        (string beginStatus, string beginError) = BeginAgain(fixture);
        Require(beginStatus == "DuplicateCompleted" && beginError.Length == 0,
            "complete marker set no longer returns the duplicate tombstone receipt");
        Require(markers.Count == 3,
            "complete marker set was pruned during reconciliation");
    }

    private static void VerifyOrphanMarkersAreCleared(Assembly production, Type ownerType)
    {
        Type ledgerType = RequireType(
            production,
            "AnimusForge.Refactor.Runtime.InteractionMemoryRecoveryLedger");
        object ledger = New(ledgerType);
        object owner = CreateOwnerWithLedger(ownerType, ledger);
        const string ownerId = "af_nonhero:orphan-marker-owner";
        const string recoveryId =
            "DDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDD";
        const string payloadHash =
            "EEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEE";
        object orphanDailyLine = StoreDailyMarker(
            ownerType,
            owner,
            ownerId,
            recoveryId,
            payloadHash,
            "user");
        IDictionary orphanRecentMarkers = StoreRecentMarkers(
            ownerType,
            owner,
            ownerId,
            recoveryId,
            payloadHash,
            "user");

        InvokeInstance(ownerType, owner, "ReconcilePersistedInteractionMemoryRecoveryMarkers");

        Require((string)Get(orphanDailyLine, "MemoryCommitId") == string.Empty,
            "orphan daily recovery id was not cleared");
        Require((string)Get(orphanDailyLine, "MemoryCommitPart") == string.Empty,
            "orphan daily recovery part was not cleared");
        Require((string)Get(orphanDailyLine, "MemoryCommitHash") == string.Empty,
            "orphan daily recovery hash was not cleared");
        Require((int)Get(orphanDailyLine, "MemoryCommitOriginGameDay") == -1,
            "orphan daily origin day was not cleared");
        Require((string)Get(orphanDailyLine, "MemoryCommitOriginGameDate") == string.Empty,
            "orphan daily origin date was not cleared");
        Require(orphanRecentMarkers.Count == 0,
            "orphan recent marker was not cleared");
        Require(ReadInt(ledger, "QuarantineCount") == 0,
            "orphan marker without a retained record created a false quarantine");
    }

    private static void VerifyWrongOwnerMarkersAreQuarantined(
        Assembly production,
        Type ownerType)
    {
        LedgerFixture fixture = CreateLedgerFixture(
            production,
            "wrong-owner-marker-commit",
            "af_nonhero:correct-marker-owner",
            complete: false);
        object owner = CreateOwnerWithLedger(ownerType, fixture.Ledger);
        const string wrongOwnerId = "af_nonhero:wrong-marker-owner";
        object wrongDailyLine = StoreDailyMarker(
            ownerType,
            owner,
            wrongOwnerId,
            fixture.RecoveryId,
            fixture.PayloadHash,
            "user");
        IDictionary wrongRecentMarkers = StoreRecentMarkers(
            ownerType,
            owner,
            wrongOwnerId,
            fixture.RecoveryId,
            fixture.PayloadHash,
            "user");

        InvokeInstance(ownerType, owner, "ReconcilePersistedInteractionMemoryRecoveryMarkers");

        Require((string)Get(wrongDailyLine, "MemoryCommitId") == string.Empty,
            "wrong-owner daily marker was not cleared");
        Require(wrongRecentMarkers.Count == 0,
            "wrong-owner recent marker was not cleared");
        Require(ReadInt(fixture.Ledger, "QuarantineCount") == 1,
            "wrong-owner marker conflict was not quarantined");
        (string beginStatus, string beginError) = BeginAgain(fixture);
        Require(beginStatus == "Rejected" && beginError == "memory_recovery_quarantined",
            "wrong-owner quarantined record was not blocked");
    }

    private static void VerifyAdapterTraceNonce(Assembly production)
    {
        Type adapterType = RequireType(
            production,
            "AnimusForge.Refactor.Adapters.LegacyInteractionSnapshotAdapters");
        Type heroType = Assembly.Load("TaleWorlds.CampaignSystem")
            .GetType("TaleWorlds.CampaignSystem.Hero", throwOnError: true);
        FieldInfo nonceField = adapterType.GetField("ProcessTraceNonce", DeclaredStatic)
            ?? throw new InvalidOperationException("missing process trace nonce");
        Require(nonceField.IsPrivate && nonceField.IsStatic && nonceField.IsInitOnly,
            "process trace nonce must remain private static readonly");
        Require(nonceField.FieldType == typeof(string),
            "process trace nonce type changed");
        string nonce = (string)(nonceField.GetValue(null) ?? string.Empty);
        Require(nonce.Length == 32 && nonce.All(Uri.IsHexDigit),
            "process trace nonce is empty or not an opaque Guid-N value");

        MethodInfo captureCourier = adapterType.GetMethod(
            "CaptureCourier",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[] { heroType, typeof(string), typeof(string), typeof(string) },
            modifiers: null)
            ?? throw new InvalidOperationException("missing four-parameter Courier capture");
        const string sessionId = "memory-recovery-explicit-courier-session";
        object first = captureCourier.Invoke(null, new object[]
        {
            null, "first courier input", sessionId, "first fact"
        });
        object second = captureCourier.Invoke(null, new object[]
        {
            null, "second courier input", sessionId, "second fact"
        });
        string firstTraceId = ReadTraceId(first);
        string secondTraceId = ReadTraceId(second);
        Require(firstTraceId == secondTraceId,
            "same explicit Courier session produced unstable in-process TraceId");
        Require(firstTraceId == "af-trace-" + nonce + "-" + sessionId,
            "Courier TraceId does not contain the process nonce and explicit session");
        Require(ReadSessionId(first) == sessionId && ReadSessionId(second) == sessionId,
            "explicit Courier session identity changed during capture");
    }

    private static LedgerFixture CreateLedgerFixture(
        Assembly production,
        string commitId,
        string subjectId,
        bool complete)
    {
        Type ledgerType = RequireType(
            production,
            "AnimusForge.Refactor.Runtime.InteractionMemoryRecoveryLedger");
        Type seedType = RequireType(
            production,
            "AnimusForge.Refactor.Runtime.InteractionMemoryRecoverySeed");
        Type componentSeedType = RequireType(
            production,
            "AnimusForge.Refactor.Runtime.InteractionMemoryRecoveryComponentSeed");
        object ledger = New(ledgerType);
        object seed = New(seedType);
        Set(seed, "CommitId", commitId);
        Set(seed, "Channel", 2);
        Set(seed, "SessionId", "fixture-session-" + commitId);
        Set(seed, "SubjectId", subjectId);
        Set(seed, "IsNonHero", true);
        Set(seed, "NpcName", "Fixture NPC");
        Set(seed, "RuntimeGeneration", 41L);
        Set(seed, "SaveGeneration", 42L);
        Set(seed, "TraceId", "fixture-trace-" + commitId);
        Set(seed, "OriginGameDay", 84);
        Set(seed, "OriginGameDate", "origin-day-84");
        Set(seed, "OriginGameHour", 17);
        Set(seed, "OriginScene", "fixture-scene");
        Set(seed, "DailyStorageDay", 85);
        Set(seed, "DailyStorageDate", "storage-day-85");
        Set(seed, "SceneSessionId", -1);
        Set(seed, "DialogueSessionId", -1);
        Set(seed, "MemorySessionKey", "fixture-memory-session");
        Set(seed, "TargetAgentIndex", -1);
        Set(seed, "TargetName", string.Empty);

        IList components = NewList(componentSeedType);
        foreach (string part in new[] { "user", "fact", "assistant" })
        {
            object component = New(componentSeedType);
            Set(component, "Part", part);
            Set(component, "DailySpeaker", part == "fact" ? "AFEF" : "Fixture NPC");
            Set(component, "DailyText", "daily-" + part);
            Set(component, "RecentText", "recent-" + part);
            Set(component, "IsAfef", part == "fact");
            Set(component, "IsLlmDialogue", part != "fact");
            components.Add(component);
        }
        Set(seed, "Components", components);

        object[] beginArguments = { seed, null, null };
        object beginStatus = InvokeInstance(ledgerType, ledger, "Begin", beginArguments);
        Require(beginStatus.ToString() == "Began",
            "fixture recovery ledger did not begin: " + beginArguments[2]);
        string recoveryId = (string)beginArguments[1];
        Require(recoveryId.Length == 64 && recoveryId.All(Uri.IsHexDigit),
            "fixture recovery id is not an opaque digest");

        if (complete)
        {
            MethodInfo nextWork = RequireInstanceMethod(ledgerType, "TryGetNextWork");
            MethodInfo markApplied = RequireInstanceMethod(ledgerType, "MarkApplied");
            int applied = 0;
            while (true)
            {
                object[] nextArguments = { null };
                bool hasWork = (bool)(nextWork.Invoke(ledger, nextArguments) ?? false);
                if (!hasWork)
                {
                    break;
                }
                Require((bool)(markApplied.Invoke(ledger, new[] { nextArguments[0] }) ?? false),
                    "fixture recovery work could not be marked applied");
                applied++;
                Require(applied <= 6, "fixture recovery emitted excess work");
            }
            Require(applied == 6, "fixture recovery did not emit all six memory-only steps");
            Require(InvokeLedgerBool(ledger, "IsCompleted", recoveryId),
                "fixture recovery did not complete");
        }

        IEnumerable retained = (IEnumerable)InvokeInstance(
            ledgerType,
            ledger,
            "GetRetainedEntries");
        object retention = retained.Cast<object>().Single();
        string payloadHash = (string)Get(retention, "PayloadHash");
        return new LedgerFixture(ledger, seed, subjectId, recoveryId, payloadHash);
    }

    private static object CreateOwnerWithLedger(Type ownerType, object ledger)
    {
        object owner = RuntimeHelpers.GetUninitializedObject(ownerType);
        Set(owner, "_interactionMemoryRecoveryLedger", ledger);
        return owner;
    }

    private static object StoreDailyMarker(
        Type ownerType,
        object owner,
        string ownerId,
        string recoveryId,
        string payloadHash,
        string part)
    {
        return StoreDailyMarkers(
            ownerType,
            owner,
            ownerId,
            recoveryId,
            payloadHash,
            part)[0];
    }

    private static IList StoreDailyMarkers(
        Type ownerType,
        object owner,
        string ownerId,
        string recoveryId,
        string payloadHash,
        params string[] parts)
    {
        Type draftType = RequireNestedType(ownerType, "DailyMemoryDraft");
        Type lineType = RequireNestedType(ownerType, "DailyMemoryLine");
        object draft = New(draftType);
        Set(draft, "HeroId", ownerId);
        Set(draft, "GameDayIndex", 85);
        Set(draft, "GameDate", "storage-day-85");
        IList lines = (IList)Get(draft, "Lines");
        foreach (string part in parts)
        {
            object line = New(lineType);
            Set(line, "GameDayIndex", 85);
            Set(line, "GameDate", "storage-day-85");
            Set(line, "Text", "daily reconciliation marker " + part);
            Set(line, "MemoryCommitId", recoveryId);
            Set(line, "MemoryCommitPart", part);
            Set(line, "MemoryCommitHash", payloadHash);
            Set(line, "MemoryCommitOriginGameDay", 84);
            Set(line, "MemoryCommitOriginGameDate", "origin-day-84");
            lines.Add(line);
        }
        InvokeInstance(
            ownerType,
            owner,
            "SaveDailyMemoryDraftsById",
            ownerId,
            NewList(draftType, draft));
        return lines;
    }

    private static IDictionary StoreRecentMarkers(
        Type ownerType,
        object owner,
        string ownerId,
        string recoveryId,
        string payloadHash,
        params string[] parts)
    {
        Type dialogueDayType = RequireNestedType(ownerType, "DialogueDay");
        object day = New(dialogueDayType);
        Set(day, "GameDayIndex", 84);
        Set(day, "GameDate", "origin-day-84");
        ((IList)Get(day, "Lines")).Add("recent reconciliation fixture");
        IDictionary markers = (IDictionary)Get(day, "MemoryCommitMarkers");
        foreach (string part in parts)
        {
            markers.Add(recoveryId + ":" + part, payloadHash);
        }
        InvokeInstance(
            ownerType,
            owner,
            "SaveDialogueHistoryById",
            ownerId,
            NewList(dialogueDayType, day));
        return markers;
    }

    private static (string Status, string Error) BeginAgain(LedgerFixture fixture)
    {
        object[] arguments = { fixture.Seed, null, null };
        object status = InvokeInstance(
            fixture.Ledger.GetType(),
            fixture.Ledger,
            "Begin",
            arguments);
        return (status.ToString(), (string)arguments[2]);
    }

    private static bool InvokeLedgerBool(object ledger, string name, params object[] arguments)
        => (bool)(InvokeInstance(ledger.GetType(), ledger, name, arguments) ?? false);

    private static int ReadInt(object target, string name)
        => (int)Get(target, name);

    private static string ReadTraceId(object envelope)
    {
        object snapshot = Get(envelope, "Snapshot");
        object trace = Get(snapshot, "Trace");
        return (string)Get(trace, "TraceId");
    }

    private static string ReadSessionId(object envelope)
    {
        object snapshot = Get(envelope, "Snapshot");
        object identity = Get(snapshot, "Identity");
        return (string)Get(identity, "SessionId");
    }

    private static void VerifyVoidMethod(
        Type ownerType,
        string name,
        Type[] parameterTypes,
        int[] optionalParameters)
    {
        MethodInfo[] methods = ownerType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => method.Name == name)
            .ToArray();
        Require(methods.Length == 1, name + " must remain a unique public static API");
        MethodInfo method = methods[0];
        Require(method.ReturnType == typeof(void), name + " return type changed");
        RequireParameterTypes(method, parameterTypes);
        ParameterInfo[] parameters = method.GetParameters();
        for (int index = 0; index < parameters.Length; index++)
        {
            Require(parameters[index].IsOptional == optionalParameters.Contains(index),
                name + " optional-parameter layout changed at index " + index);
        }
    }

    private static void RequireParameterTypes(MethodBase method, params Type[] expected)
    {
        ParameterInfo[] actual = method.GetParameters();
        Require(actual.Length == expected.Length,
            method.Name + " parameter count changed: " + actual.Length);
        for (int index = 0; index < expected.Length; index++)
        {
            Require(actual[index].ParameterType == expected[index],
                method.Name + " parameter " + index + " changed from "
                + FriendlyTypeName(expected[index]) + " to "
                + FriendlyTypeName(actual[index].ParameterType));
        }
    }

    private static void RequireParameterPrefix(MethodBase method, params Type[] expected)
    {
        ParameterInfo[] actual = method.GetParameters();
        Require(actual.Length >= expected.Length,
            method.Name + " parameter prefix is too short");
        for (int index = 0; index < expected.Length; index++)
        {
            Require(actual[index].ParameterType == expected[index],
                method.Name + " parameter prefix changed at index " + index);
        }
    }

    private static void RequireConstant(Type ownerType, string name, object expected)
    {
        FieldInfo field = ownerType.GetField(name, AnyStatic)
            ?? throw new InvalidOperationException("missing constant " + ownerType.FullName + "." + name);
        Require(field.IsLiteral && !field.IsInitOnly,
            ownerType.FullName + "." + name + " must remain a compile-time constant");
        Require(Equals(field.GetRawConstantValue(), expected),
            ownerType.FullName + "." + name + " changed");
    }

    private static void VerifyField(Type ownerType, string name, Type expectedType)
    {
        FieldInfo field = ownerType.GetField(name, AnyInstance)
            ?? throw new InvalidOperationException("missing marker field " + ownerType.FullName + "." + name);
        Require(field.FieldType == expectedType,
            ownerType.FullName + "." + name + " type changed");
    }

    private static void VerifyInternalProperty(Type ownerType, string name, Type expectedType)
    {
        PropertyInfo property = ownerType.GetProperty(name, AnyInstance)
            ?? throw new InvalidOperationException("missing provenance property " + ownerType.FullName + "." + name);
        MethodInfo getter = property.GetGetMethod(nonPublic: true)
            ?? throw new InvalidOperationException("missing getter " + ownerType.FullName + "." + name);
        Require(property.PropertyType == expectedType,
            ownerType.FullName + "." + name + " type changed");
        Require(getter.IsAssembly && !getter.IsPublic,
            ownerType.FullName + "." + name + " must remain internal");
    }

    private static void AssertMarker(
        MethodInfo method,
        object owner,
        object work,
        bool expectedMatch,
        bool expectedConflict,
        string label)
    {
        object[] arguments = { work, false };
        bool match = (bool)(method.Invoke(owner, arguments) ?? false);
        Require(match == expectedMatch, label + " match result changed");
        Require((bool)arguments[1] == expectedConflict, label + " conflict result changed");
    }

    private static IList InvokeGenericClone(Type ownerType, Type valueType, object value)
    {
        MethodInfo clone = ownerType.GetMethods(DeclaredStatic)
            .Single(method => method.Name == "CloneForMemoryRecovery"
                && method.IsGenericMethodDefinition)
            .MakeGenericMethod(valueType);
        return (IList)(clone.Invoke(null, new[] { value })
            ?? throw new InvalidOperationException("daily marker clone returned null"));
    }

    private static object InvokeInstance(
        Type ownerType,
        object owner,
        string name,
        params object[] arguments)
        => RequireInstanceMethod(ownerType, name).Invoke(owner, arguments);

    private static MethodInfo RequireInstanceMethod(Type ownerType, string name)
        => ownerType.GetMethods(AnyInstance)
            .Single(method => method.Name == name);

    private static object New(Type type) => Activator.CreateInstance(type, nonPublic: true)
        ?? throw new InvalidOperationException("could not create " + type.FullName);

    private static IList NewList(Type itemType, params object[] items)
    {
        IList list = (IList)(Activator.CreateInstance(typeof(List<>).MakeGenericType(itemType))
            ?? throw new InvalidOperationException("could not create list for " + itemType.FullName));
        foreach (object item in items)
        {
            list.Add(item);
        }
        return list;
    }

    private static void Add(object collection, object value)
        => ((IList)collection).Add(value);

    private static object Get(object target, string name)
    {
        FieldInfo field = target.GetType().GetField(name, AnyInstance);
        if (field != null)
        {
            return field.GetValue(target)
                ?? throw new InvalidOperationException("noninitialized field " + target.GetType().FullName + "." + name);
        }
        PropertyInfo property = target.GetType().GetProperty(name, AnyInstance)
            ?? throw new InvalidOperationException("missing member " + target.GetType().FullName + "." + name);
        return property.GetValue(target)
            ?? throw new InvalidOperationException("noninitialized property " + target.GetType().FullName + "." + name);
    }

    private static void Set(object target, string name, object value)
    {
        FieldInfo field = target.GetType().GetField(name, AnyInstance);
        if (field != null)
        {
            field.SetValue(target, value);
            return;
        }
        PropertyInfo property = target.GetType().GetProperty(name, AnyInstance)
            ?? throw new InvalidOperationException("missing member " + target.GetType().FullName + "." + name);
        property.SetValue(target, value);
    }

    private static string FriendlyTypeName(Type type)
        => type.FullName ?? type.Name;

    private static Type RequireType(Assembly assembly, string name)
        => assembly.GetType(name, throwOnError: false)
            ?? throw new InvalidOperationException("missing type " + name);

    private static Type RequireNestedType(Type owner, string name)
        => owner.GetNestedType(name, BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("missing nested type " + name);

    private sealed class LedgerFixture
    {
        internal LedgerFixture(
            object ledger,
            object seed,
            string subjectId,
            string recoveryId,
            string payloadHash)
        {
            Ledger = ledger;
            Seed = seed;
            SubjectId = subjectId;
            RecoveryId = recoveryId;
            PayloadHash = payloadHash;
        }

        internal object Ledger { get; }
        internal object Seed { get; }
        internal string SubjectId { get; }
        internal string RecoveryId { get; }
        internal string PayloadHash { get; }
    }

    private static void Require(bool condition, string message)
    {
        _assertions++;
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
