using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

internal static class NotorietyConversationOutcomeProductionReplay
{
    private const BindingFlags AnyInstance = BindingFlags.Instance
        | BindingFlags.Public | BindingFlags.NonPublic;
    private const BindingFlags AnyStatic = BindingFlags.Static
        | BindingFlags.Public | BindingFlags.NonPublic;
    private const BindingFlags DeclaredInstance = AnyInstance | BindingFlags.DeclaredOnly;
    private const BindingFlags DeclaredStatic = AnyStatic | BindingFlags.DeclaredOnly;

    internal static void Run(Assembly assembly)
    {
        Type owner = RequireType(assembly, "AnimusForge.PlayerNotorietyBehavior");
        Type host = RequireType(assembly, "AnimusForge.MyBehavior");
        Type ledger = RequireType(
            assembly,
            "AnimusForge.Refactor.Runtime.NotorietyConversationOutcomeLedger");
        Type receipt = RequireType(
            assembly,
            "AnimusForge.Refactor.Runtime.NotorietyConversationOutcomeReceipt");
        Type candidate = RequireType(
            assembly,
            "AnimusForge.Refactor.Runtime.NotorietyConversationOutcomeCandidate");
        Type target = RequireType(
            assembly,
            "AnimusForge.Refactor.Runtime.NotorietyConversationFinalizeTarget");
        Type status = RequireType(
            assembly,
            "AnimusForge.Refactor.Runtime.NotorietyConversationOutcomeOperationStatus");
        Type state = RequireNested(owner, "PlayerNotorietyState");
        Type active = RequireNested(owner, "ActiveConversationState");
        Type knowledge = RequireNested(owner, "PlayerNpcKnowledgeState");
        Type character = Assembly.Load("TaleWorlds.CampaignSystem")
            .GetType("TaleWorlds.CampaignSystem.CharacterObject", true);
        Type hero = Assembly.Load("TaleWorlds.CampaignSystem")
            .GetType("TaleWorlds.CampaignSystem.Hero", true);
        Type characterEnumerable = typeof(IEnumerable<>).MakeGenericType(character);

        AssertEmbeddedStateWitness(state);
        AssertLedgerBounds(ledger, receipt);
        AssertLineEntryAbi(owner, status);
        AssertRecoverableLineOrdering(owner, ledger, receipt, candidate, hero);
        AssertExactFinalizeOrdering(owner, ledger, receipt, target, active, knowledge);
        AssertPersistenceRecoverySeams(owner, ledger, receipt);
        AssertMyBehaviorExactFinalizeCall(host, owner, characterEnumerable);
        AssertPersistentDtoAuthority(candidate, receipt, target);
        Console.WriteLine(
            "PASS notorietyConversationOutcomeProductionReplay embeddedWitness=1 bounds=64/512/260 exactLine=1 duplicateBeforeRoll=1 exactFinalize=1 loadUnknown=1 confirmedReconcile=1 legacyAbi=1 dataOnly=1");
    }

    private static void AssertEmbeddedStateWitness(Type state)
    {
        FieldInfo witness = state.GetField(
            "ConversationOutcomeReceipts",
            DeclaredInstance);
        Require(witness != null
                && witness.FieldType.IsGenericType
                && witness.FieldType.GetGenericTypeDefinition() == typeof(Dictionary<,>)
                && witness.FieldType.GetGenericArguments().SequenceEqual(
                    new[] { typeof(string), typeof(string) }),
            "PlayerNotoriety JSON state no longer embeds the string/string outcome witness");
    }

    private static void AssertLedgerBounds(Type ledger, Type receipt)
    {
        Require(ReadLiteral<int>(ledger, "MaximumPendingEntries") == 64
                && ReadLiteral<int>(ledger, "MaximumTerminalEntries") == 512,
            "notoriety conversation ledger retention bounds drifted");
        Require(ReadLiteral<int>(receipt, "MaximumLineCount") == 260,
            "notoriety conversation per-receipt line bound drifted");
        Require(string.Equals(
                ReadLiteral<string>(receipt, "WirePrefix"),
                "AFNR1:",
                StringComparison.Ordinal),
            "notoriety conversation receipt wire prefix drifted");
    }

    private static void AssertLineEntryAbi(Type owner, Type status)
    {
        MethodInfo legacy = RequireMethod(
            owner,
            "NoteConversationLineForExternal",
            DeclaredStatic,
            typeof(string));
        Require(legacy.IsPublic && legacy.ReturnType == typeof(void),
            "legacy void NoteConversationLineForExternal(string) ABI drifted");

        Type[] exactParameters =
        {
            typeof(string), typeof(string), typeof(long), typeof(long),
            typeof(int), typeof(int), typeof(string), typeof(string), typeof(string)
        };
        MethodInfo exact = RequireMethod(
            owner,
            "NoteConversationLineRecoverableForExternal",
            DeclaredStatic,
            exactParameters);
        Require(!exact.IsPublic && exact.ReturnType == status,
            "exact nine-parameter notoriety line status ABI drifted");
        Require(owner.GetMethods(DeclaredStatic)
                .Count(method => method.Name == exact.Name) == 1,
            "exact notoriety line status API gained an ambiguous overload");
    }

    private static void AssertRecoverableLineOrdering(
        Type owner,
        Type ledger,
        Type receipt,
        Type candidate,
        Type hero)
    {
        Type[] lineParameters =
        {
            typeof(string), typeof(string), typeof(long), typeof(long),
            typeof(int), typeof(int), typeof(string), typeof(string), typeof(string)
        };
        MethodInfo note = RequireMethod(
            owner,
            "NoteConversationLineRecoverable",
            DeclaredInstance,
            lineParameters);
        MethodInfo probe = RequireMethod(
            ledger,
            "ProbeLine",
            DeclaredInstance,
            typeof(string), typeof(string), typeof(string), typeof(string),
            typeof(string), typeof(int), typeof(int), receipt.MakeByRefType(),
            typeof(string).MakeByRefType());
        MethodInfo prepare = RequireMethod(
            ledger,
            "Prepare",
            DeclaredInstance,
            candidate, typeof(long), receipt.MakeByRefType(),
            typeof(string).MakeByRefType());
        MethodInfo addLine = RequireMethod(
            ledger,
            "AddLine",
            DeclaredInstance,
            typeof(string), typeof(string), typeof(string), typeof(string),
            typeof(string), typeof(string), typeof(int), typeof(int), typeof(long),
            receipt.MakeByRefType(), typeof(string).MakeByRefType());
        MethodInfo getByHero = RequireMethod(
            owner,
            "GetOrCreateActiveConversation",
            DeclaredInstance,
            hero);
        MethodInfo getById = RequireMethod(
            owner,
            "GetOrCreateActiveConversation",
            DeclaredInstance,
            typeof(string), typeof(string));
        MethodInfo roll = RequireMethod(
            owner,
            "RollPercent",
            DeclaredStatic,
            typeof(int));

        int probeOffset = RequireCallOffset(note, probe);
        int prepareOffset = RequireCallOffset(note, prepare);
        int addOffset = RequireCallOffset(note, addLine);
        Require(probeOffset < prepareOffset && prepareOffset < addOffset,
            "recoverable notoriety line no longer probes before Prepare/AddLine");
        foreach (MethodInfo getOrCreate in new[] { getByHero, getById })
        {
            int offset = RequireCallOffset(note, getOrCreate);
            Require(probeOffset < offset,
                "recoverable notoriety line can create/roll before ProbeLine");
        }
        Require(RequireCallOffset(getById, roll) >= 0,
            "notoriety active-conversation creation no longer exposes the guarded roll seam");

        int directRoll = FindCallOffset(note, roll);
        Require(directRoll < 0 || probeOffset < directRoll,
            "recoverable notoriety line directly rolls before ProbeLine");
    }

    private static void AssertExactFinalizeOrdering(
        Type owner,
        Type ledger,
        Type receipt,
        Type target,
        Type active,
        Type knowledge)
    {
        MethodInfo finalize = RequireMethod(
            owner,
            "TryFinalizeExactNotorietyConversation",
            DeclaredInstance,
            typeof(string), active, knowledge, typeof(bool).MakeByRefType());
        MethodInfo confirm = RequireMethod(
            ledger,
            "Confirm",
            DeclaredInstance,
            typeof(string), typeof(string), target, typeof(long),
            receipt.MakeByRefType(), typeof(string).MakeByRefType());
        MethodInfo applyTarget = RequireMethod(
            owner,
            "ApplyNotorietyFinalizeTarget",
            DeclaredStatic,
            knowledge, target);
        MethodInfo readback = RequireMethod(
            owner,
            "MatchesNotorietyFinalizeTarget",
            DeclaredStatic,
            knowledge, target);
        MethodInfo markApplied = RequireMethod(
            ledger,
            "MarkApplied",
            DeclaredInstance,
            typeof(string), typeof(string), typeof(string), typeof(long),
            typeof(string).MakeByRefType());

        int confirmOffset = RequireCallOffset(finalize, confirm);
        int applyOffset = RequireCallOffset(finalize, applyTarget);
        int readbackOffset = RequireCallOffset(finalize, readback);
        int markOffset = RequireCallOffset(finalize, markApplied);
        Require(confirmOffset < applyOffset
                && applyOffset < readbackOffset
                && readbackOffset < markOffset,
            "exact notoriety finalize no longer confirms, applies, reads back, then marks applied");
    }

    private static void AssertPersistenceRecoverySeams(
        Type owner,
        Type ledger,
        Type receipt)
    {
        MethodInfo syncData = owner.GetMethods(DeclaredInstance)
            .SingleOrDefault(method => method.Name == "SyncData");
        Require(syncData != null, "PlayerNotoriety SyncData override is missing");
        MethodInfo prepareSave = RequireMethod(
            owner,
            "PrepareNotorietyConversationOutcomeStorageForSave",
            DeclaredInstance);
        MethodInfo activateLoad = RequireMethod(
            owner,
            "ActivateNotorietyConversationOutcomeStorageAfterLoad",
            DeclaredInstance);
        MethodInfo resetFailedLoad = RequireMethod(
            owner,
            "ResetNotorietyConversationOutcomeStorageAfterFailedLoad",
            DeclaredInstance);
        RequireCallOffset(syncData, prepareSave);
        RequireCallOffset(syncData, activateLoad);
        RequireCallOffset(syncData, resetFailedLoad);

        MethodInfo import = RequireMethod(
            ledger,
            "Import",
            DeclaredInstance,
            typeof(IDictionary<string, string>), typeof(string).MakeByRefType());
        MethodInfo loadedOpenUnknown = RequireMethod(
            receipt,
            "MarkLoadedOpenUnknown",
            DeclaredInstance,
            typeof(long));
        RequireCallOffset(import, loadedOpenUnknown);

        MethodInfo reconcile = RequireMethod(
            owner,
            "ReconcileConfirmedNotorietyConversationOutcomes",
            DeclaredInstance);
        RequireCallOffset(activateLoad, import);
        RequireCallOffset(activateLoad, reconcile);

        Type work = RequireType(
            owner.Assembly,
            "AnimusForge.Refactor.Runtime.NotorietyConversationConfirmedWorkItem");
        MethodInfo getConfirmed = RequireMethod(
            ledger,
            "GetConfirmedWork",
            DeclaredInstance,
            work.MakeByRefType());
        MethodInfo markApplied = RequireMethod(
            ledger,
            "MarkApplied",
            DeclaredInstance,
            typeof(string), typeof(string), typeof(string), typeof(long),
            typeof(string).MakeByRefType());
        int workOffset = RequireCallOffset(reconcile, getConfirmed);
        int appliedOffset = RequireCallOffset(reconcile, markApplied);
        Require(workOffset < appliedOffset,
            "loaded Confirmed receipt no longer reconciles before MarkApplied");

        Type outcomeState = RequireType(
            owner.Assembly,
            "AnimusForge.Refactor.Runtime.NotorietyConversationOutcomeState");
        Require(Convert.ToInt32(Enum.Parse(outcomeState, "Open")) == 1
                && Convert.ToInt32(Enum.Parse(outcomeState, "Confirmed")) == 2
                && Convert.ToInt32(Enum.Parse(outcomeState, "Unknown")) == 4,
            "notoriety recovery state identities drifted");
    }

    private static void AssertMyBehaviorExactFinalizeCall(
        Type host,
        Type owner,
        Type characterEnumerable)
    {
        MethodInfo ended = RequireMethod(
            host,
            "OnMemoryConversationEnded",
            DeclaredInstance,
            characterEnumerable);
        MethodInfo exactFinalize = RequireMethod(
            owner,
            "FinalizeConversationForExternal",
            DeclaredStatic,
            characterEnumerable, typeof(string));
        MethodInfo legacyFinalize = RequireMethod(
            owner,
            "FinalizeConversationForExternal",
            DeclaredStatic,
            characterEnumerable);
        RequireCallOffset(ended, exactFinalize);
        Require(FindCallOffset(ended, legacyFinalize) < 0,
            "MyBehavior memory completion still calls the inexact finalize overload");
    }

    private static void AssertPersistentDtoAuthority(params Type[] dataTypes)
    {
        string[] forbiddenNames =
        {
            "RawSession", "RawMemorySession", "SessionId", "Text", "Dialogue",
            "Hero", "Delegate", "Action", "Executor", "Callback"
        };
        string[] forbiddenTypes =
        {
            "TaleWorlds", "System.Delegate", "System.Action", "Hero",
            "Executor", "Callback"
        };
        foreach (Type dataType in dataTypes)
        {
            IEnumerable<MemberInfo> members = dataType.GetMembers(DeclaredInstance)
                .Where(member => member is FieldInfo || member is PropertyInfo);
            foreach (MemberInfo member in members)
            {
                Type valueType = member is FieldInfo field
                    ? field.FieldType
                    : ((PropertyInfo)member).PropertyType;
                string memberName = member.Name;
                string typeName = valueType.FullName ?? valueType.Name;
                bool rawSessionKey = memberName.IndexOf(
                        "MemorySessionKey",
                        StringComparison.OrdinalIgnoreCase) >= 0
                    && memberName.IndexOf(
                        "Hash",
                        StringComparison.OrdinalIgnoreCase) < 0;
                Require(!rawSessionKey
                        && !forbiddenNames.Any(fragment => memberName.IndexOf(
                            fragment,
                            StringComparison.OrdinalIgnoreCase) >= 0)
                        && !forbiddenTypes.Any(fragment => typeName.IndexOf(
                            fragment,
                            StringComparison.OrdinalIgnoreCase) >= 0),
                    "notoriety persistent DTO retained forbidden authority: "
                        + dataType.Name + "." + memberName + ":" + typeName);
            }
        }
    }

    private static T ReadLiteral<T>(Type owner, string name)
    {
        FieldInfo field = owner.GetField(name, AnyStatic | BindingFlags.DeclaredOnly);
        Require(field != null && field.IsLiteral,
            owner.FullName + "." + name + " is not a compiled constant");
        return (T)field.GetRawConstantValue();
    }

    private static MethodInfo RequireMethod(
        Type owner,
        string name,
        BindingFlags flags,
        params Type[] parameters)
    {
        MethodInfo method = owner.GetMethod(name, flags, null, parameters, null);
        Require(method != null,
            "missing production method " + owner.FullName + "." + name
                + "(" + string.Join(",", parameters.Select(type => type.Name)) + ")");
        return method;
    }

    private static int RequireCallOffset(MethodInfo caller, MethodInfo callee)
    {
        int offset = FindCallOffset(caller, callee);
        Require(offset >= 0,
            caller.DeclaringType?.FullName + "." + caller.Name
                + " does not call " + callee.DeclaringType?.FullName + "." + callee.Name);
        return offset;
    }

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

    private static Type RequireNested(Type owner, string name)
        => owner.GetNestedType(name, BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                "missing production nested type " + owner.FullName + "+" + name);

    private static Type RequireType(Assembly assembly, string name)
        => assembly.GetType(name, false)
            ?? throw new InvalidOperationException("missing production type " + name);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
