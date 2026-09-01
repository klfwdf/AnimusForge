using System;
using System.Collections;
using System.Linq;
using System.Reflection;

internal static class CourierInboundCompletionReplay
{
    private const BindingFlags AnyInstance = BindingFlags.Instance
        | BindingFlags.Public | BindingFlags.NonPublic;
    private const BindingFlags AnyStatic = BindingFlags.Static
        | BindingFlags.Public | BindingFlags.NonPublic;
    private static int _assertions;

    public static void Run(Assembly production)
    {
        Type courier = RequireType(production, "AnimusForge.CourierDeliveryBehavior");
        Type receipt = RequireType(
            production,
            "AnimusForge.Refactor.Runtime.CourierInboundCompletionReceipt");
        Type recoveryStatus = RequireType(
            production,
            "AnimusForge.Refactor.Runtime.InteractionMemoryRecoveryLookupStatus");
        Type session = courier.GetNestedType("CourierSession", BindingFlags.NonPublic);
        Require(session != null, "CourierSession type missing");

        FieldInfo receiptField = session.GetField("InboundCompletionReceipt", AnyInstance);
        Require(receiptField != null && receiptField.FieldType == typeof(string),
            "CourierSession must persist one additive string completion receipt");
        Require(session.GetField("PostprocessConsumed", AnyInstance) != null,
            "existing outbound Economy consumption field changed");
        FieldInfo storageKey = courier.GetField(
            "SessionStorageKey",
            BindingFlags.Static | BindingFlags.NonPublic);
        Require(storageKey != null
                && (string)storageKey.GetRawConstantValue() == "_af_courier_sessions_v1",
            "Courier session persistence key changed");

        VerifyReceiptIsolation(receipt);
        string serialized = CreateAndRoundTripReceipt(receipt);
        VerifyLoadGenerationGate(courier, session, receiptField, serialized);
        VerifyCompletionState(courier, session, receipt, receiptField, serialized);
        VerifyCompletionFailClosed(courier, session, receipt, receiptField);
        VerifyOneReceiptPerTick(courier, session, receipt, receiptField);
        VerifyOwnerAndTickSeams(production, courier, recoveryStatus);

        Console.WriteLine(
            "PASS courierInboundCompletionReplay receipt=1 checksum=1 payloadConflict=1 sessionJson=1 loadGate=1 completion=1 stateIdempotent=1 ownerQuerySeam=1 tickOne=1 isolated=1 assertions="
            + _assertions);
    }

    private static void VerifyReceiptIsolation(Type receipt)
    {
        string[] forbidden =
        {
            "ActionPlan", "ActionRequest", "IActionPlanExecutor",
            "InteractionResult", "Postprocess", "AfterCommit", "Delegate", "Economy"
        };
        foreach (FieldInfo field in receipt.GetFields(AnyInstance | BindingFlags.DeclaredOnly))
        {
            string signature = field.Name + ":" + (field.FieldType.FullName ?? field.FieldType.Name);
            Require(!forbidden.Any(fragment =>
                    signature.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0),
                "completion receipt retained forbidden payload: " + signature);
        }
        Require(receipt.GetFields(AnyInstance | BindingFlags.DeclaredOnly).Any(field =>
                field.Name.IndexOf("RecoveryId", StringComparison.OrdinalIgnoreCase) >= 0),
            "completion receipt lost opaque recovery identity");
        Require(receipt.GetFields(AnyInstance | BindingFlags.DeclaredOnly).Any(field =>
                field.Name.IndexOf("PayloadHash", StringComparison.OrdinalIgnoreCase) >= 0),
            "completion receipt lost payload hash");
        Require(receipt.GetFields(AnyInstance | BindingFlags.DeclaredOnly).Any(field =>
                field.Name.IndexOf("MemoryPayloadHash", StringComparison.OrdinalIgnoreCase) >= 0),
            "completion receipt lost memory-owner payload binding");
    }

    private static string CreateAndRoundTripReceipt(Type receipt)
    {
        MethodInfo create = receipt.GetMethods(AnyStatic)
            .SingleOrDefault(method => method.Name == "TryCreate"
                && method.GetParameters().Length == 10);
        Require(create != null, "completion receipt factory missing");
        object[] createArgs =
        {
            "session-1", "sender-1", "player-1", "party-1",
            new string('A', 64), new string('E', 64),
            "frozen visible letter", 123L, null, null
        };
        Require((bool)create.Invoke(null, createArgs),
            "valid completion receipt was rejected: " + (createArgs[9] ?? ""));
        object value = createArgs[8];
        Require(value != null, "completion receipt factory returned null");

        MethodInfo serialize = receipt.GetMethod("Serialize", AnyInstance);
        Require(serialize != null && serialize.ReturnType == typeof(string),
            "completion receipt serializer missing");
        string wire = (string)serialize.Invoke(value, null);
        Require(!string.IsNullOrWhiteSpace(wire) && wire.StartsWith("AFCI1:", StringComparison.Ordinal),
            "completion receipt wire prefix mismatch");

        MethodInfo deserialize = receipt.GetMethods(AnyStatic)
            .SingleOrDefault(method => method.Name == "TryDeserialize"
                && method.GetParameters().Length == 3);
        Require(deserialize != null, "completion receipt parser missing");
        object[] parseArgs = { wire, null, null };
        Require((bool)deserialize.Invoke(null, parseArgs),
            "completion receipt round-trip failed: " + (parseArgs[2] ?? ""));

        byte[] tamperedBytes = Convert.FromBase64String(wire.Substring("AFCI1:".Length));
        int bodyLength = BitConverter.ToInt32(tamperedBytes, 0);
        tamperedBytes[4 + (bodyLength / 2)] ^= 0x01;
        object[] corruptArgs =
        {
            "AFCI1:" + Convert.ToBase64String(tamperedBytes), null, null
        };
        Require(!(bool)deserialize.Invoke(null, corruptArgs)
                && string.Equals(
                    corruptArgs[2]?.ToString(),
                    "courier_completion_checksum_mismatch",
                    StringComparison.Ordinal),
            "completion receipt accepted checksum corruption");

        object[] conflictArgs =
        {
            "session-1", "sender-1", "player-1", "party-1",
            new string('A', 64), new string('E', 64),
            "different frozen letter", 123L, null, null
        };
        Require((bool)create.Invoke(null, conflictArgs),
            "second valid completion receipt was rejected");
        string firstHash = ReadString(value, "PayloadHash");
        string secondHash = ReadString(conflictArgs[8], "PayloadHash");
        Require(!string.Equals(firstHash, secondHash, StringComparison.Ordinal),
            "completion receipt payload conflict did not change hash");
        return wire;
    }

    private static void VerifyLoadGenerationGate(
        Type courier,
        Type session,
        FieldInfo receiptField,
        string serialized)
    {
        MethodInfo reset = courier.GetMethod("ResetReplyGenerationAfterLoad", AnyStatic);
        Require(reset != null, "Courier load generation reset method missing");

        object pending = Activator.CreateInstance(session, nonPublic: true);
        session.GetField("Id", AnyInstance).SetValue(pending, "session-1");
        session.GetField("Direction", AnyInstance).SetValue(pending, "InboundToPlayer");
        session.GetField("Stage", AnyInstance).SetValue(pending, "GeneratingReply");
        session.GetField("ReplyGenerated", AnyInstance).SetValue(pending, false);
        session.GetField("ReplyGenerationStarted", AnyInstance).SetValue(pending, true);
        receiptField.SetValue(pending, serialized);
        string sessionJson = SerializeJson(pending);
        object restored = DeserializeJson(sessionJson, session);
        Require((string)receiptField.GetValue(restored) == serialized,
            "CourierSession JSON round-trip lost completion receipt");
        pending = restored;
        reset.Invoke(null, new[] { pending, "production_replay" });
        Require((bool)session.GetField("ReplyGenerationStarted", AnyInstance).GetValue(pending),
            "load reset re-armed duplicate inbound generation despite durable receipt");

        object legacy = Activator.CreateInstance(session, nonPublic: true);
        session.GetField("Id", AnyInstance).SetValue(legacy, "legacy-session");
        session.GetField("Direction", AnyInstance).SetValue(legacy, "InboundToPlayer");
        session.GetField("Stage", AnyInstance).SetValue(legacy, "GeneratingReply");
        session.GetField("ReplyGenerated", AnyInstance).SetValue(legacy, false);
        session.GetField("ReplyGenerationStarted", AnyInstance).SetValue(legacy, true);
        receiptField.SetValue(legacy, string.Empty);
        reset.Invoke(null, new[] { legacy, "production_replay" });
        Require(!(bool)session.GetField("ReplyGenerationStarted", AnyInstance).GetValue(legacy),
            "legacy receipt-free save no longer re-arms generation");

        object legacyJson = DeserializeJson(
            "{\"Id\":\"legacy-json\",\"Direction\":\"InboundToPlayer\",\"Stage\":\"GeneratingReply\",\"ReplyGenerationStarted\":true}",
            session);
        MethodInfo normalize = courier.GetMethod("NormalizeSession", AnyStatic);
        Require(normalize != null, "Courier session normalizer missing");
        normalize.Invoke(null, new[] { legacyJson });
        Require((string)receiptField.GetValue(legacyJson) == string.Empty,
            "old CourierSession JSON did not normalize missing receipt to empty");
    }

    private static void VerifyCompletionState(
        Type courier,
        Type session,
        Type receipt,
        FieldInfo receiptField,
        string serialized)
    {
        MethodInfo deserialize = receipt.GetMethods(AnyStatic)
            .Single(method => method.Name == "TryDeserialize"
                && method.GetParameters().Length == 3);
        object[] parseArgs = { serialized, null, null };
        Require((bool)deserialize.Invoke(null, parseArgs),
            "ready fixture parse failed");
        object receiptValue = parseArgs[1];
        receipt.GetMethod("MarkReady", AnyInstance).Invoke(receiptValue, new object[] { 456L });
        serialized = (string)receipt.GetMethod("Serialize", AnyInstance).Invoke(receiptValue, null);

        object value = Activator.CreateInstance(session, nonPublic: true);
        session.GetField("Id", AnyInstance).SetValue(value, "session-1");
        session.GetField("Direction", AnyInstance).SetValue(value, "InboundToPlayer");
        session.GetField("Stage", AnyInstance).SetValue(value, "GeneratingReply");
        session.GetField("SenderHeroId", AnyInstance).SetValue(value, "sender-1");
        session.GetField("RecipientHeroId", AnyInstance).SetValue(value, "player-1");
        session.GetField("CourierPartyId", AnyInstance).SetValue(value, "party-1");
        session.GetField("ReplyGenerated", AnyInstance).SetValue(value, false);
        session.GetField("ReplyGenerationStarted", AnyInstance).SetValue(value, true);
        session.GetField("DeliveryApplied", AnyInstance).SetValue(value, false);
        receiptField.SetValue(value, serialized);

        object owner = Activator.CreateInstance(courier, nonPublic: true);
        MethodInfo complete = courier.GetMethod(
            "TryCompleteCourierInboundCompletionReceipt",
            AnyInstance);
        Require(complete != null, "Courier completion consumer missing");
        complete.Invoke(owner, new[] { value, receiptValue, false, "production_replay" });
        Require((bool)session.GetField("ReplyGenerated", AnyInstance).GetValue(value),
            "ready receipt did not set ReplyGenerated");
        Require(!(bool)session.GetField("ReplyGenerationStarted", AnyInstance).GetValue(value),
            "ready receipt did not clear ReplyGenerationStarted");
        Require((string)session.GetField("LetterText", AnyInstance).GetValue(value)
                == "frozen visible letter",
            "ready receipt did not apply frozen visible letter");

        string appliedWire = (string)receiptField.GetValue(value);
        object[] appliedArgs = { appliedWire, null, null };
        Require((bool)deserialize.Invoke(null, appliedArgs),
            "applied receipt did not remain parseable");
        Require(ReadString(appliedArgs[1], "Lifecycle") == "Applied",
            "completion receipt did not write Applied tombstone");

        complete.Invoke(owner, new[] { value, appliedArgs[1], false, "production_replay_duplicate" });
        Require((string)session.GetField("LetterText", AnyInstance).GetValue(value)
                == "frozen visible letter"
            && (bool)session.GetField("ReplyGenerated", AnyInstance).GetValue(value),
            "duplicate completion changed terminal session state");

        object recovered = Activator.CreateInstance(session, nonPublic: true);
        session.GetField("Id", AnyInstance).SetValue(recovered, "session-1");
        session.GetField("Direction", AnyInstance).SetValue(recovered, "InboundToPlayer");
        session.GetField("Stage", AnyInstance).SetValue(recovered, "GeneratingReply");
        session.GetField("SenderHeroId", AnyInstance).SetValue(recovered, "sender-1");
        session.GetField("RecipientHeroId", AnyInstance).SetValue(recovered, "player-1");
        session.GetField("CourierPartyId", AnyInstance).SetValue(recovered, "party-1");
        session.GetField("LetterText", AnyInstance).SetValue(recovered, "stale fallback");
        session.GetField("ReplyGenerated", AnyInstance).SetValue(recovered, false);
        session.GetField("ReplyGenerationStarted", AnyInstance).SetValue(recovered, true);
        receiptField.SetValue(recovered, appliedWire);
        recovered = DeserializeJson(SerializeJson(recovered), session);
        object[] recoveredReceiptArgs = { receiptField.GetValue(recovered), null, null };
        Require((bool)deserialize.Invoke(null, recoveredReceiptArgs),
            "applied crash-window receipt did not survive session JSON");
        complete.Invoke(owner, new[]
        {
            recovered, recoveredReceiptArgs[1], false, "production_replay_applied_recovery"
        });
        Require((string)session.GetField("LetterText", AnyInstance).GetValue(recovered)
                == "frozen visible letter"
            && (bool)session.GetField("ReplyGenerated", AnyInstance).GetValue(recovered)
            && !(bool)session.GetField("ReplyGenerationStarted", AnyInstance).GetValue(recovered),
            "Applied tombstone did not recover a partially updated loaded session");
    }

    private static void VerifyOwnerAndTickSeams(
        Assembly production,
        Type courier,
        Type recoveryStatus)
    {
        Type owner = RequireType(production, "AnimusForge.MyBehavior");
        MethodInfo buildId = owner.GetMethods(AnyStatic)
            .SingleOrDefault(method => method.Name == "TryPrepareExternalDialogueHistoryRecoveryIdentity");
        Require(buildId != null && buildId.IsAssembly,
            "MyBehavior opaque recovery identity seam missing or too public");
        MethodInfo query = owner.GetMethods(AnyStatic)
            .SingleOrDefault(method => method.Name == "GetExternalDialogueHistoryRecoveryStatus");
        Require(query != null && query.IsAssembly && query.ReturnType == recoveryStatus,
            "MyBehavior narrow recovery-status seam missing or too public");

        Type wrapper = courier.GetNestedType(
            "CourierInboundCompletionMemory",
            BindingFlags.NonPublic);
        Require(wrapper != null, "Courier-owned inbound batch memory wrapper missing");
        string[] interfaces = wrapper.GetInterfaces().Select(type => type.Name).ToArray();
        Require(interfaces.Contains("IInteractionMemory"),
            "Courier inbound wrapper lost memory read compatibility");
        Require(interfaces.Contains("IInteractionMemoryBatchCommitter"),
            "Courier inbound wrapper cannot arm receipt before batch commit");

        MethodInfo processOne = courier.GetMethod(
            "ProcessOneCourierInboundCompletionReceipt",
            AnyInstance);
        Require(processOne != null && processOne.ReturnType == typeof(void),
            "Courier main-thread one-receipt processor missing");
        MethodInfo memoryFactory = courier.GetMethods(AnyStatic)
            .SingleOrDefault(method => method.Name == "CreateCourierInboundMemoryFacadeForExternal"
                && method.GetParameters().Length == 2);
        Require(memoryFactory != null,
            "Courier inbound memory factory lost explicit session binding");
    }

    private static void VerifyCompletionFailClosed(
        Type courier,
        Type session,
        Type receipt,
        FieldInfo receiptField)
    {
        VerifyCompletionGateCase(
            courier, session, receipt, receiptField,
            "wrong-direction",
            value => session.GetField("Direction", AnyInstance).SetValue(value, "Outbound"));
        VerifyCompletionGateCase(
            courier, session, receipt, receiptField,
            "already-delivered",
            value => session.GetField("DeliveryApplied", AnyInstance).SetValue(value, true));
        VerifyCompletionGateCase(
            courier, session, receipt, receiptField,
            "terminal-stage",
            value => session.GetField("Stage", AnyInstance).SetValue(value, "Destroyed"));
        VerifyCompletionGateCase(
            courier, session, receipt, receiptField,
            "party-mismatch",
            value => session.GetField("CourierPartyId", AnyInstance).SetValue(value, "other-party"));
    }

    private static void VerifyCompletionGateCase(
        Type courier,
        Type session,
        Type receipt,
        FieldInfo receiptField,
        string caseName,
        Action<object> mutate)
    {
        object value = CreateReadySession(
            session,
            receipt,
            receiptField,
            "gate-" + caseName,
            "gate-sender",
            "gate-party",
            new string('9', 64));
        mutate(value);
        MethodInfo deserialize = receipt.GetMethods(AnyStatic)
            .Single(method => method.Name == "TryDeserialize"
                && method.GetParameters().Length == 3);
        object[] receiptArgs = { receiptField.GetValue(value), null, null };
        Require((bool)deserialize.Invoke(null, receiptArgs),
            caseName + " receipt parse failed");
        object owner = Activator.CreateInstance(courier, nonPublic: true);
        MethodInfo complete = courier.GetMethod(
            "TryCompleteCourierInboundCompletionReceipt",
            AnyInstance);
        complete.Invoke(owner, new[] { value, receiptArgs[1], false, "gate_replay" });
        Require(!(bool)session.GetField("ReplyGenerated", AnyInstance).GetValue(value),
            caseName + " unexpectedly completed the Courier session");
        object[] quarantinedArgs = { receiptField.GetValue(value), null, null };
        Require((bool)deserialize.Invoke(null, quarantinedArgs)
                && ReadString(quarantinedArgs[1], "Lifecycle") == "Quarantined",
            caseName + " did not quarantine the completion receipt");
    }

    private static void VerifyOneReceiptPerTick(
        Type courier,
        Type session,
        Type receipt,
        FieldInfo receiptField)
    {
        object owner = Activator.CreateInstance(courier, nonPublic: true);
        object first = CreateReadySession(
            session, receipt, receiptField,
            "a-session", "a-sender", "a-party", new string('B', 64));
        object second = CreateReadySession(
            session, receipt, receiptField,
            "b-session", "b-sender", "b-party", new string('C', 64));
        object quarantined = CreateReadySession(
            session, receipt, receiptField,
            "0-quarantined", "q-sender", "q-party", new string('D', 64));
        object[] quarantineArgs = { receiptField.GetValue(quarantined), null, null };
        MethodInfo deserialize = receipt.GetMethods(AnyStatic)
            .Single(method => method.Name == "TryDeserialize"
                && method.GetParameters().Length == 3);
        Require((bool)deserialize.Invoke(null, quarantineArgs),
            "quarantine scheduler fixture parse failed");
        object quarantineReceipt = quarantineArgs[1];
        receipt.GetMethod("Quarantine", AnyInstance).Invoke(
            quarantineReceipt,
            new object[] { "fixture_quarantine" });
        receiptField.SetValue(
            quarantined,
            receipt.GetMethod("Serialize", AnyInstance).Invoke(quarantineReceipt, null));
        object invalid = CreateReadySession(
            session, receipt, receiptField,
            "1-invalid", "i-sender", "i-party", new string('F', 64));
        receiptField.SetValue(invalid, "not-a-valid-receipt");
        object pending = CreateReadySession(
            session, receipt, receiptField,
            "2-pending", "p-sender", "p-party", new string('1', 64),
            markReady: false);
        session.GetField("ReplyWaitPopupShown", AnyInstance).SetValue(quarantined, true);
        courier.GetField("_courierReplyWaitTimeLocked", AnyInstance).SetValue(owner, true);

        FieldInfo sessionsField = courier.GetField("_sessions", AnyInstance);
        Require(sessionsField != null, "Courier session owner dictionary missing");
        IDictionary sessions = (IDictionary)sessionsField.GetValue(owner);
        sessions.Add("0-quarantined", quarantined);
        sessions.Add("1-invalid", invalid);
        sessions.Add("2-pending", pending);
        sessions.Add("a-session", first);
        sessions.Add("b-session", second);

        MethodInfo processOne = courier.GetMethod(
            "ProcessOneCourierInboundCompletionReceipt",
            AnyInstance);
        int before = sessions.Count;
        processOne.Invoke(owner, null);
        Require(sessions.Count == before - 1
            && !(bool)session.GetField("ReplyGenerated", AnyInstance).GetValue(first)
            && !(bool)session.GetField("ReplyGenerated", AnyInstance).GetValue(second),
            "quarantined receipt tick did not abort exactly one session");
        Require(!(bool)courier.GetField("_courierReplyWaitTimeLocked", AnyInstance).GetValue(owner),
            "quarantined receipt abort did not release Courier wait pause");
        before = sessions.Count;
        processOne.Invoke(owner, null);
        Require(sessions.Count == before - 1
            && !(bool)session.GetField("ReplyGenerated", AnyInstance).GetValue(first)
            && !(bool)session.GetField("ReplyGenerated", AnyInstance).GetValue(second),
            "invalid receipt tick did not abort exactly one session");
        processOne.Invoke(owner, null);
        Require(!(bool)session.GetField("ReplyGenerated", AnyInstance).GetValue(pending)
            && !(bool)session.GetField("ReplyGenerated", AnyInstance).GetValue(first)
            && !(bool)session.GetField("ReplyGenerated", AnyInstance).GetValue(second),
            "unavailable Pending receipt did not remain gated for one tick");
        processOne.Invoke(owner, null);
        int generated = ((bool)session.GetField("ReplyGenerated", AnyInstance).GetValue(first) ? 1 : 0)
            + ((bool)session.GetField("ReplyGenerated", AnyInstance).GetValue(second) ? 1 : 0);
        Require(generated == 1, "one Courier tick completed more than one durable receipt");
    }

    private static object CreateReadySession(
        Type session,
        Type receipt,
        FieldInfo receiptField,
        string sessionId,
        string senderId,
        string partyId,
        string recoveryId,
        bool markReady = true)
    {
        MethodInfo create = receipt.GetMethods(AnyStatic)
            .Single(method => method.Name == "TryCreate"
                && method.GetParameters().Length == 10);
        object[] createArgs =
        {
            sessionId, senderId, "player-1", partyId,
            recoveryId, new string('E', 64),
            "letter-" + sessionId, 123L, null, null
        };
        Require((bool)create.Invoke(null, createArgs),
            "ready session receipt create failed: " + (createArgs[9] ?? ""));
        object receiptValue = createArgs[8];
        if (markReady)
        {
            receipt.GetMethod("MarkReady", AnyInstance).Invoke(receiptValue, new object[] { 456L });
        }
        string wire = (string)receipt.GetMethod("Serialize", AnyInstance).Invoke(receiptValue, null);

        object value = Activator.CreateInstance(session, nonPublic: true);
        session.GetField("Id", AnyInstance).SetValue(value, sessionId);
        session.GetField("Direction", AnyInstance).SetValue(value, "InboundToPlayer");
        session.GetField("Stage", AnyInstance).SetValue(value, "GeneratingReply");
        session.GetField("SenderHeroId", AnyInstance).SetValue(value, senderId);
        session.GetField("RecipientHeroId", AnyInstance).SetValue(value, "player-1");
        session.GetField("CourierPartyId", AnyInstance).SetValue(value, partyId);
        session.GetField("ReplyGenerated", AnyInstance).SetValue(value, false);
        session.GetField("ReplyGenerationStarted", AnyInstance).SetValue(value, true);
        session.GetField("DeliveryApplied", AnyInstance).SetValue(value, false);
        receiptField.SetValue(value, wire);
        return value;
    }

    private static string ReadString(object instance, string name)
    {
        PropertyInfo property = instance.GetType().GetProperty(name, AnyInstance);
        if (property != null)
        {
            object value = property.GetValue(instance);
            return value?.ToString() ?? string.Empty;
        }
        FieldInfo field = instance.GetType().GetField(name, AnyInstance);
        Require(field != null, "missing receipt member " + name);
        object fieldValue = field.GetValue(instance);
        return fieldValue?.ToString() ?? string.Empty;
    }

    private static string SerializeJson(object value)
    {
        Type json = Assembly.Load("Newtonsoft.Json").GetType(
            "Newtonsoft.Json.JsonConvert",
            throwOnError: true);
        MethodInfo method = json.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(candidate => candidate.Name == "SerializeObject"
                && !candidate.IsGenericMethod
                && candidate.GetParameters().Length == 1
                && candidate.GetParameters()[0].ParameterType == typeof(object));
        return (string)method.Invoke(null, new[] { value });
    }

    private static object DeserializeJson(string value, Type targetType)
    {
        Type json = Assembly.Load("Newtonsoft.Json").GetType(
            "Newtonsoft.Json.JsonConvert",
            throwOnError: true);
        MethodInfo method = json.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(candidate => candidate.Name == "DeserializeObject"
                && !candidate.IsGenericMethod
                && candidate.GetParameters().Length == 2
                && candidate.GetParameters()[0].ParameterType == typeof(string)
                && candidate.GetParameters()[1].ParameterType == typeof(Type));
        return method.Invoke(null, new object[] { value, targetType });
    }

    private static Type RequireType(Assembly assembly, string name)
    {
        Type type = assembly.GetType(name, throwOnError: false);
        Require(type != null, "missing production type " + name);
        return type;
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
