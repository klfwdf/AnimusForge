using System;
using System.IO;
using System.Linq;
using System.Reflection;

static void AssertTrue(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

string stageDirectory = Path.GetFullPath(Path.Combine(
    AppContext.BaseDirectory,
    "..", "..", "..", "..", "..",
    "bin", "Debug", "single_module_stage", "AnimusForge",
    "bin", "Win64_Shipping_Client"));
string implementationPath = Path.Combine(stageDirectory, "versions", "1.4", "AnimusForge.dll");
AssertTrue(File.Exists(implementationPath), "project-local 1.4 AnimusForge.dll is missing");

AppDomain.CurrentDomain.AssemblyResolve += (_, arguments) =>
{
    string name = new AssemblyName(arguments.Name).Name;
    foreach (string root in new[] { AppContext.BaseDirectory, stageDirectory })
    {
        if (!Directory.Exists(root))
        {
            continue;
        }
        foreach (string candidate in Directory.GetFiles(root, name + ".dll", SearchOption.AllDirectories))
        {
            try
            {
                return Assembly.LoadFrom(candidate);
            }
            catch
            {
                // Continue looking for the matching Bannerlord assembly.
            }
        }
    }
    return null;
};

Assembly animusForge = Assembly.LoadFrom(implementationPath);
Type identityType = animusForge.GetType("AnimusForge.Refactor.Contracts.InteractionIdentity", true);
Type channelType = animusForge.GetType("AnimusForge.Refactor.Contracts.InteractionChannel", true);
Type envelopeType = animusForge.GetType("AnimusForge.Refactor.Contracts.InteractionEnvelope", true);
Type snapshotType = animusForge.GetType("AnimusForge.Refactor.Contracts.GameInteractionSnapshot", true);
Type behaviorType = animusForge.GetType("AnimusForge.ShoutBehavior", true);
Type courierType = animusForge.GetType("AnimusForge.CourierDeliveryBehavior", true);

object InvokeStatic(Type owner, string name, params object[] arguments)
{
    MethodInfo method = owner.GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Where(candidate => candidate.Name == name)
        .Where(candidate => candidate.GetParameters().Length == arguments.Length)
        .Single(candidate => candidate.GetParameters()
            .Select(parameter => parameter.ParameterType)
            .Zip(arguments, (parameterType, argument) =>
                argument == null || parameterType.IsInstanceOfType(argument))
            .All(match => match));
    return method.Invoke(null, arguments);
}

object nativeEnvelope = InvokeStatic(
    behaviorType,
    "CaptureNativeConversationRefactorEnvelopeForExternal",
    "native capture replay");
object sceneEnvelope = InvokeStatic(
    behaviorType,
    "CaptureSceneShoutRefactorEnvelopeForExternal",
    "scene capture replay",
    -1);
object courierEnvelope = InvokeStatic(
    courierType,
    "CaptureCourierReplyRefactorEnvelopeForExternal",
    "missing-courier-session",
    "courier capture replay");

string Channel(object envelope)
{
    object snapshot = envelopeType.GetProperty("Snapshot").GetValue(envelope);
    object identity = snapshotType.GetProperty("Identity").GetValue(snapshot);
    return identityType.GetProperty("Channel").GetValue(identity).ToString();
}

string SessionId(object envelope)
{
    object snapshot = envelopeType.GetProperty("Snapshot").GetValue(envelope);
    object identity = snapshotType.GetProperty("Identity").GetValue(snapshot);
    return (string)identityType.GetProperty("SessionId").GetValue(identity);
}

string PlayerText(object envelope)
{
    object snapshot = envelopeType.GetProperty("Snapshot").GetValue(envelope);
    return (string)snapshotType.GetProperty("PlayerText").GetValue(snapshot);
}

AssertTrue(nativeEnvelope != null && Channel(nativeEnvelope) == "NativeConversation", "Native capture did not preserve channel identity");
AssertTrue(sceneEnvelope != null && Channel(sceneEnvelope) == "SceneShout", "SceneShout capture did not preserve channel identity");
AssertTrue(courierEnvelope != null && Channel(courierEnvelope) == "Courier", "Courier capture did not preserve channel identity");
AssertTrue(PlayerText(nativeEnvelope) == "native capture replay", "Native capture changed player text");
AssertTrue(PlayerText(sceneEnvelope) == "scene capture replay", "SceneShout capture changed player text");
AssertTrue(PlayerText(courierEnvelope) == "courier capture replay", "Courier capture changed player text");
AssertTrue(SessionId(courierEnvelope) == "missing-courier-session", "Courier capture changed session identity");

Type stringEnumerableType = typeof(System.Collections.Generic.IEnumerable<string>);
MethodInfo scenePortsFactory = behaviorType.GetMethods(BindingFlags.Public | BindingFlags.Static)
    .Single(method => method.Name == "CreateSceneShoutDetachedPortsForExternal"
        && method.GetParameters().Length == 2);
MethodInfo nativePortsFactory = behaviorType.GetMethods(BindingFlags.Public | BindingFlags.Static)
    .Single(method => method.Name == "CreateNativeConversationDetachedPortsForExternal"
        && method.GetParameters().Length == 3);
MethodInfo courierPortsFactory = courierType.GetMethods(BindingFlags.Public | BindingFlags.Static)
    .Single(method => method.Name == "CreateCourierDetachedPortsForExternal"
        && method.GetParameters().Length == 3);
object allowedTags = new[] { "ACTION:DUEL" };
object scenePorts = scenePortsFactory.Invoke(null, new object[] { allowedTags, 64 });
object nativePorts = nativePortsFactory.Invoke(null, new object[] { allowedTags, 12, 64 });
object courierPorts = courierPortsFactory.Invoke(null, new object[] { allowedTags, true, 64 });
AssertTrue(scenePorts != null && nativePorts != null && courierPorts != null, "one or more production opt-in port factories returned null");

MemoryOwnerReceiptReplay.Run(animusForge);
MemoryOwnerReadbackReplay.Run(animusForge);
MemoryRecoveryProductionReplay.Run(animusForge);
CourierEconomyReservationReplay.Run(animusForge);
Console.WriteLine("PASS productionOptInEntryReplay native=1 scene=1 courier=1 identity=1 failClosed=1 ports=1 noDefaultCutover=1");
