using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

internal static class WeeklyActionOutcomeProductionReplay
{
    private const BindingFlags AnyInstance = BindingFlags.Instance
        | BindingFlags.Public | BindingFlags.NonPublic;
    private const BindingFlags AnyStatic = BindingFlags.Static
        | BindingFlags.Public | BindingFlags.NonPublic;

    internal static void Run(Assembly assembly)
    {
        AssertCurrentProductionBuild(assembly);
        Type owner = RequireType(assembly, "AnimusForge.MyBehavior");
        Type facade = RequireType(assembly,
            "AnimusForge.Refactor.Adapters.MyBehaviorMemoryFacade");
        Type executor = RequireType(assembly,
            "AnimusForge.Refactor.Adapters.LegacyNativeActionPlanExecutor");
        Type outcomeOwner = RequireType(assembly,
            "AnimusForge.Refactor.Runtime.IWeeklyMemoryMaterialOutcomeOwner");
        Type candidateSource = RequireType(assembly,
            "AnimusForge.Refactor.Runtime.IWeeklyMemoryMaterialCandidateSource");
        Type executionReceipt = RequireType(assembly,
            "AnimusForge.Refactor.Runtime.IWeeklyMemoryMaterialExecutionReceipt");
        Type committer = RequireType(assembly,
            "AnimusForge.Refactor.Runtime.InteractionResultCommitter");
        Type ledger = RequireType(assembly,
            "AnimusForge.Refactor.Runtime.WeeklyMemoryMaterialOutcomeLedger");
        Type receipt = RequireType(assembly,
            "AnimusForge.Refactor.Runtime.WeeklyMemoryMaterialOutcomeReceipt");
        Type candidate = RequireType(assembly,
            "AnimusForge.Refactor.Runtime.WeeklyMemoryMaterialOutcomeCandidate");

        Require(outcomeOwner.IsAssignableFrom(facade),
            "production MyBehavior memory facade does not own weekly outcomes");
        Require(candidateSource.IsAssignableFrom(executor),
            "production action executor does not expose the Economy-only candidate source");
        Require(executionReceipt.IsAssignableFrom(executor),
            "production action executor does not expose the exact execution fingerprint");
        MethodInfo completionGate = committer.GetMethod(
            "CompleteWeeklyMaterialOutcome",
            AnyStatic | BindingFlags.DeclaredOnly);
        Require(completionGate?.GetMethodBody()?.LocalVariables.Any(variable =>
                variable.LocalType == executionReceipt) == true,
            "production committer does not read the exact execution fingerprint receipt");

        FieldInfo storageKey = owner.GetField(
            "WeeklyActionOutcomeReceiptsStorageKey",
            AnyStatic | BindingFlags.DeclaredOnly);
        Require(storageKey != null && storageKey.IsLiteral
            && string.Equals(
                storageKey.GetRawConstantValue() as string,
                "_af_weeklyActionOutcomeReceipts_v1",
                StringComparison.Ordinal),
            "weekly outcome storage key drifted");
        Require(owner.GetField("_weeklyActionOutcomeLedger", AnyInstance)?.FieldType == ledger,
            "weekly outcome ledger owner field drifted");
        FieldInfo storage = owner.GetField("_weeklyActionOutcomeStorage", AnyInstance);
        Require(storage != null
            && storage.FieldType.IsGenericType
            && storage.FieldType.GetGenericTypeDefinition() == typeof(Dictionary<,>)
            && storage.FieldType.GetGenericArguments().SequenceEqual(new[] { typeof(string), typeof(string) }),
            "weekly outcome storage type drifted");

        Require((int)ledger.GetField("MaximumPendingEntries", AnyStatic)
                .GetRawConstantValue() == 64
            && (int)ledger.GetField("MaximumTerminalEntries", AnyStatic)
                .GetRawConstantValue() == 512,
            "weekly outcome retention bounds drifted");
        Require((int)receipt.GetField("MaximumSerializedLength", AnyStatic)
                .GetRawConstantValue() == 196608,
            "weekly outcome wire bound drifted");
        AssertCallBefore(
            owner,
            "PrepareWeeklyActionOutcomeForExternal",
            ledger,
            "ProbeExistingCandidate",
            owner,
            "TryBuildWeeklyActionOutcomePayload");

        Type trigger = owner.GetNestedType("WeeklyMemoryMaterialTrigger", BindingFlags.NonPublic);
        Require(trigger != null, "weekly material trigger type missing");
        foreach (string fieldName in new[]
        {
            "OutcomeReceiptId",
            "OutcomeCandidateHash",
            "OutcomePayloadHash",
            "OutcomeActionFingerprint",
            "OutcomeTurnFingerprint"
        })
        {
            Require(trigger.GetField(fieldName, AnyInstance)?.FieldType == typeof(string),
                "weekly outcome trigger provenance missing: " + fieldName);
        }
        AssertCompiledTriggerSanitizer(owner, trigger);

        MethodInfo label = owner.GetMethod(
            "BuildWeeklyMemoryMaterialTagLabel",
            AnyStatic | BindingFlags.DeclaredOnly,
            null,
            new[] { typeof(string) },
            null);
        Require(label != null
            && !string.IsNullOrWhiteSpace((string)label.Invoke(null,
                new object[] { "[WEEKLY:ECONOMY_GIVE_GOLD]" }))
            && !string.IsNullOrWhiteSpace((string)label.Invoke(null,
                new object[] { "[WEEKLY:ECONOMY_DEBT_RESOLVE]" })),
            "weekly semantic labels are not renderable");

        AssertCall(owner, "SyncTailPersistenceData", "SyncWeeklyActionOutcomeData");
        AssertCall(owner, "ResetTailPersistenceTransientState",
            "ResetInteractionMemoryRecoveryTransientState");
        AssertCall(owner, "ResetTailPersistenceTransientState",
            "ResetWeeklyActionOutcomeTransientState");
        AssertCall(owner, "ActivateTailPersistenceAfterLoad",
            "ActivateInteractionMemoryRecoveryAfterLoad");
        AssertCall(owner, "ActivateTailPersistenceAfterLoad",
            "ActivateWeeklyActionOutcomeAfterLoad");
        AssertCall(owner, "ProcessOneTailPersistenceRecoveryOnTick",
            "ProcessOneInteractionMemoryRecoveryOnTick");
        AssertCall(owner, "ProcessOneTailPersistenceRecoveryOnTick",
            "ProcessOneWeeklyActionOutcomeOnTick");
        AssertCall(owner, "ProcessOneWeeklyActionOutcomeOnTick",
            "TryPublishWeeklyActionOutcome");
        AssertCall(owner, "TryPublishWeeklyActionOutcome",
            "HasExactWeeklyActionOutcomeTrigger");
        AssertCall(owner, "TryPublishWeeklyActionOutcome",
            "AddWeeklyMemoryMaterialTriggerToDraft");
        AssertCall(owner, "TryPublishWeeklyActionOutcome",
            "SaveDailyMemoryDraftsById");

        string[] forbidden =
        {
            "ActionPlan", "ActionRequest", "RawPostprocess", "Executor",
            "Callback", "Delegate", "Hero", "EconomyRewardDebtAction"
        };
        foreach (Type dataType in new[] { receipt, candidate })
        {
            foreach (MemberInfo member in dataType.GetMembers(
                AnyInstance | BindingFlags.DeclaredOnly)
                .Where(value => value is FieldInfo || value is PropertyInfo))
            {
                Type valueType = member is FieldInfo field
                    ? field.FieldType
                    : ((PropertyInfo)member).PropertyType;
                string signature = member.Name + ":" + (valueType.FullName ?? valueType.Name);
                Require(!forbidden.Any(fragment => signature.IndexOf(
                        fragment,
                        StringComparison.OrdinalIgnoreCase) >= 0),
                    "weekly persistent DTO retained forbidden authority: " + signature);
            }
        }

        MethodInfo tick = owner.GetMethod(
            "ProcessOneWeeklyActionOutcomeOnTick",
            AnyInstance | BindingFlags.DeclaredOnly);
        MethodBody tickBody = tick?.GetMethodBody();
        Require(tickBody != null
            && !tickBody.LocalVariables.Any(variable => forbidden.Any(fragment =>
                (variable.LocalType.FullName ?? variable.LocalType.Name).IndexOf(
                    fragment,
                    StringComparison.OrdinalIgnoreCase) >= 0)),
            "weekly load/tick path retained executable action authority");
    }

    private static void AssertCompiledTriggerSanitizer(Type owner, Type triggerType)
    {
        const string semanticTag = "[WEEKLY:ECONOMY_GIVE_GOLD]";
        string receiptId = new string('A', 64);
        string candidateHash = new string('B', 64);
        string payloadHash = new string('C', 64);
        string actionFingerprint = new string('D', 64);
        string turnFingerprint = new string('E', 64);
        string stableKey = "weekly_outcome:" + receiptId + ":" + payloadHash;

        object malformed = RuntimeHelpers.GetUninitializedObject(triggerType);
        SetField(triggerType, malformed, "MemoryId", "hero-malformed");
        SetField(triggerType, malformed, "FootholdKingdomId", "kingdom-malformed");
        SetField(triggerType, malformed, "NormalizedTagText", semanticTag);
        SetField(triggerType, malformed, "Tags", new List<string> { semanticTag });
        SetField(
            triggerType,
            malformed,
            "StableKey",
            "weekly_outcome:" + new string('F', 64) + ":" + new string('0', 64));

        object valid = RuntimeHelpers.GetUninitializedObject(triggerType);
        SetField(triggerType, valid, "MemoryId", "hero-production");
        SetField(triggerType, valid, "NpcName", "NPC One");
        SetField(triggerType, valid, "GameDayIndex", 7);
        SetField(triggerType, valid, "GameDate", "1084-01-02");
        SetField(triggerType, valid, "SceneSessionId", 11);
        SetField(triggerType, valid, "DialogueSessionId", 12);
        SetField(triggerType, valid, "TargetAgentIndex", 13);
        SetField(triggerType, valid, "FootholdKingdomId", "kingdom-production");
        SetField(triggerType, valid, "FootholdSettlementId", "town-production");
        SetField(triggerType, valid, "NormalizedTagText", semanticTag);
        SetField(triggerType, valid, "Tags", new List<string> { semanticTag });
        SetField(triggerType, valid, "EstimatedValueDenars", 30001L);
        SetField(triggerType, valid, "TriggerReason", "owner confirmed");
        SetField(triggerType, valid, "StableKey", stableKey);
        SetField(triggerType, valid, "OutcomeReceiptId", receiptId);
        SetField(triggerType, valid, "OutcomeCandidateHash", candidateHash);
        SetField(triggerType, valid, "OutcomePayloadHash", payloadHash);
        SetField(triggerType, valid, "OutcomeActionFingerprint", actionFingerprint);
        SetField(triggerType, valid, "OutcomeTurnFingerprint", turnFingerprint);
        SetField(triggerType, valid, "CreatedUtcTicks", 123L);

        Array input = Array.CreateInstance(triggerType, 2);
        input.SetValue(malformed, 0);
        input.SetValue(valid, 1);
        MethodInfo sanitizer = owner.GetMethod(
            "SanitizeWeeklyMemoryMaterialTriggers",
            AnyStatic | BindingFlags.DeclaredOnly);
        Require(sanitizer != null, "compiled weekly material trigger sanitizer is missing");
        IEnumerable sanitized = sanitizer.Invoke(null, new object[] { input }) as IEnumerable;
        Require(sanitized != null, "compiled weekly material trigger sanitizer returned no sequence");
        List<object> retained = sanitized.Cast<object>().ToList();
        Require(retained.Count == 1 && ReferenceEquals(retained[0], valid),
            "malformed outcome semantic trigger was not dropped");

        Require(ReadField<string>(triggerType, valid, "MemoryId") == "hero-production"
                && ReadField<string>(triggerType, valid, "NpcName") == "NPC One"
                && ReadField<int>(triggerType, valid, "GameDayIndex") == 7
                && ReadField<string>(triggerType, valid, "GameDate") == "1084-01-02"
                && ReadField<int>(triggerType, valid, "SceneSessionId") == 11
                && ReadField<int>(triggerType, valid, "DialogueSessionId") == 12
                && ReadField<int>(triggerType, valid, "TargetAgentIndex") == 13
                && ReadField<string>(triggerType, valid, "FootholdKingdomId")
                    == "kingdom-production"
                && ReadField<string>(triggerType, valid, "FootholdSettlementId")
                    == "town-production"
                && ReadField<string>(triggerType, valid, "NormalizedTagText") == semanticTag
                && ReadField<List<string>>(triggerType, valid, "Tags")
                    .SequenceEqual(new[] { semanticTag })
                && ReadField<long>(triggerType, valid, "EstimatedValueDenars") == 30001L
                && ReadField<string>(triggerType, valid, "TriggerReason") == "owner confirmed"
                && ReadField<string>(triggerType, valid, "StableKey") == stableKey
                && ReadField<string>(triggerType, valid, "OutcomeReceiptId") == receiptId
                && ReadField<string>(triggerType, valid, "OutcomeCandidateHash") == candidateHash
                && ReadField<string>(triggerType, valid, "OutcomePayloadHash") == payloadHash
                && ReadField<string>(triggerType, valid, "OutcomeActionFingerprint")
                    == actionFingerprint
                && ReadField<string>(triggerType, valid, "OutcomeTurnFingerprint")
                    == turnFingerprint
                && ReadField<long>(triggerType, valid, "CreatedUtcTicks") == 123L,
            "valid compiled outcome trigger lost exact provenance or required payload fields");
    }

    private static void SetField(Type owner, object target, string name, object value)
    {
        FieldInfo field = owner.GetField(name, AnyInstance);
        Require(field != null, "compiled weekly trigger field is missing: " + name);
        field.SetValue(target, value);
    }

    private static T ReadField<T>(Type owner, object target, string name)
    {
        FieldInfo field = owner.GetField(name, AnyInstance);
        Require(field != null, "compiled weekly trigger field is missing: " + name);
        return (T)field.GetValue(target);
    }

    private static void AssertCall(Type owner, string callerName, string calleeName)
    {
        const BindingFlags allDeclared = BindingFlags.Instance | BindingFlags.Static
            | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        MethodInfo caller = owner.GetMethod(callerName, allDeclared);
        MethodInfo callee = owner.GetMethod(calleeName, allDeclared);
        Require(caller != null && callee != null && CallsMethod(caller, callee),
            callerName + " does not call " + calleeName);
    }

    private static void AssertCallBefore(
        Type callerOwner,
        string callerName,
        Type firstOwner,
        string firstName,
        Type secondOwner,
        string secondName)
    {
        const BindingFlags allDeclared = BindingFlags.Instance | BindingFlags.Static
            | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        MethodInfo caller = callerOwner.GetMethod(callerName, allDeclared);
        MethodInfo first = firstOwner.GetMethod(firstName, allDeclared);
        MethodInfo second = secondOwner.GetMethod(secondName, allDeclared);
        int firstOffset = FindCallOffset(caller, first);
        int secondOffset = FindCallOffset(caller, second);
        Require(firstOffset >= 0 && secondOffset >= 0 && firstOffset < secondOffset,
            callerName + " does not probe durable identity before rebuilding live payload");
    }

    private static bool CallsMethod(MethodInfo caller, MethodInfo callee)
        => FindCallOffset(caller, callee) >= 0;

    private static int FindCallOffset(MethodInfo caller, MethodInfo callee)
    {
        if (caller == null || callee == null)
        {
            return -1;
        }
        byte[] il = caller.GetMethodBody()?.GetILAsByteArray() ?? Array.Empty<byte>();
        byte[] tokenBytes = BitConverter.GetBytes(callee.MetadataToken);
        for (int offset = 1; offset + tokenBytes.Length <= il.Length; offset++)
        {
            if ((il[offset - 1] == 0x28 || il[offset - 1] == 0x6f)
                && il.Skip(offset).Take(tokenBytes.Length).SequenceEqual(tokenBytes))
            {
                return offset - 1;
            }
        }
        return -1;
    }

    private static Type RequireType(Assembly assembly, string name)
        => assembly.GetType(name, false)
            ?? throw new InvalidOperationException("missing production type " + name);

    private static void AssertCurrentProductionBuild(Assembly assembly)
    {
        DirectoryInfo directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "MyBehavior.cs")))
        {
            directory = directory.Parent;
        }
        Require(directory != null, "repository root for production freshness was not found");
        string[] sources =
        {
            "MyBehavior.cs",
            "MyBehavior.MemoryRecovery.cs",
            "MyBehavior.WeeklyActionOutcomeReceipts.cs",
            "Refactor/Adapters/LegacyInteractionSnapshotAdapters.cs",
            "Refactor/Adapters/LegacyNativeActionPlanExecutor.cs",
            "Refactor/Runtime/InteractionResultCommitter.cs",
            "Refactor/Runtime/WeeklyMemoryMaterialOutcomeReceipt.cs"
        };
        DateTime newestSource = sources
            .Select(relative => Path.Combine(directory.FullName,
                relative.Replace('/', Path.DirectorySeparatorChar)))
            .Select(path => File.GetLastWriteTimeUtc(path))
            .Max();
        Require(File.GetLastWriteTimeUtc(assembly.Location) >= newestSource,
            "project-local production DLL is stale relative to LOCAL-7-K sources");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
