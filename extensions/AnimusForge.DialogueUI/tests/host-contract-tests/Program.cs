using System;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;

internal static class Program
{
    private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
    private static void Require(bool value, string label) { if (!value) throw new InvalidOperationException(label); }
    private static int Main(string[] args)
    {
        try { return Probe(args); }
        catch (Exception error)
        {
            // Dependency/type-load failures must be distinguishable from contract failures.
            for (Exception item = error; item != null; item = item.InnerException)
                Console.Error.WriteLine(item.GetType().FullName + ": " + item.Message);
            return 1;
        }
    }
    private static int Probe(string[] args)
    {
        if (args.Length < 1) throw new ArgumentException("HostContractTests <actual AnimusForge.dll> [dependency directories...]");
        string dll = Path.GetFullPath(args[0]);
        string[] roots = new[] { Path.GetDirectoryName(dll) }.Concat(args.Skip(1).Select(Path.GetFullPath)).ToArray();
        AppDomain.CurrentDomain.AssemblyResolve += (sender, request) => {
            string name = new AssemblyName(request.Name).Name + ".dll";
            foreach (string root in roots) { string path = Path.Combine(root, name); if (File.Exists(path)) return Assembly.LoadFrom(path); }
            return null;
        };
        Assembly host = Assembly.LoadFrom(dll);
        Type behavior = host.GetType("AnimusForge.ShoutBehavior", true);
        FieldInfo ownerField = behavior.GetField("_j17SceneTradeController", Flags);
        Require(ownerField != null, "current trade owner field");
        Type owner = ownerField.FieldType;
        Require(owner.FullName == "AnimusForge.SceneTradeController", "unique trade state owner");
        Require(behavior.GetField("_shoutTradeOptions", Flags) == null, "contract must test migrated owner, not old private fields");
        FieldInfo capture = owner.GetField("InlineInquiryCapture", Flags);
        Require(capture != null && capture.FieldType.IsGenericType
            && capture.FieldType.GetGenericArguments()[0].FullName == "TaleWorlds.Core.MultiSelectionInquiryData"
            && capture.FieldType.GetGenericArguments()[1] == typeof(bool), "synchronous exact inquiry capture contract");
        MethodInfo amount = owner.GetMethod("ShowShoutTradeAmountInquiry", Flags);
        Require(amount != null && owner.GetMethod("OnShoutTradeResourcesSelected", Flags) != null
            && owner.GetMethod("CommitShoutTradeActionOnly", Flags) != null, "actual controller trade callbacks");
        foreach (string name in new[] { "_shoutTradeOptions", "_shoutPendingTradeItems", "_shoutPendingTradeItemIndex", "_shoutTradeActionOnly", "_shoutTradeActionOnlyFinished" })
            Require(owner.GetField(name, Flags) != null, "controller member " + name);
        Type statusRef = typeof(string).MakeByRefType();
        Type indices = typeof(System.Collections.Generic.IReadOnlyList<int>);
        Require(behavior.GetMethod("LoadScenePresentationTradeOptionsForExternal", Flags, null,
            new[] { typeof(string), statusRef }, null) != null, "scene trade UI option entry");
        Require(behavior.GetMethod("StageScenePresentationTradeForExternal", Flags, null,
            new[] { typeof(long), indices, indices, statusRef }, null) != null, "scene trade UI revision-bound stage entry");
        Require(behavior.GetMethod("CancelScenePresentationTradeForExternal", Flags, null,
            new[] { typeof(long) }, null) != null, "scene trade UI revision-bound cancel entry");
        Type bridge = host.GetType("AnimusForge.DialogueUI.Native.InlineTradeBridge", true);
        var harmony = new Harmony("DialogueUI.ActualHostContract." + Guid.NewGuid().ToString("N"));
        try
        {
            bridge.GetMethod("Install", Flags).Invoke(null, new object[] { harmony });
            Require((bool)bridge.GetProperty("Available", Flags).GetValue(null), "actual DLL InlineTradeBridge.Install available");
            Require(Harmony.GetPatchInfo(amount)?.Prefixes.Any(p => p.owner == harmony.Id) == true, "real Harmony patch targets controller amount method");
            Type inputOwner = behavior.GetField("_j17SceneShoutInputController", Flags).FieldType;
            Require(inputOwner.GetMethod("OpenShoutTextInput", Flags) != null && owner.GetMethod("ShowShoutTradeChatInput", Flags) != null, "scene input owner callbacks");
            foreach (string name in new[] { "_activeShoutTargetingContext", "_sceneConversationEpoch", "_shoutTradeTargetNpc", "_shoutTradeTargetAgentSnapshot", "_shoutTradeActionOnly" })
                Require(behavior.GetField(name, Flags) != null || behavior.GetProperty(name, Flags) != null, "cached scene reader " + name);
            Type sceneBridge = host.GetType("AnimusForge.DialogueUI.Shout.ShoutUiAdapter", true);
            sceneBridge.GetMethod("Install", Flags).Invoke(null, new object[] { harmony });
            Require((bool)sceneBridge.GetField("_installed", Flags).GetValue(null), "actual DLL ShoutUiAdapter.Install available");
            Require(Harmony.GetPatchInfo(inputOwner.GetMethod("OpenShoutTextInput", Flags))?.Prefixes.Any(p => p.owner == harmony.Id) == true
                && Harmony.GetPatchInfo(owner.GetMethod("ShowShoutTradeChatInput", Flags))?.Prefixes.Any(p => p.owner == harmony.Id) == true,
                "scene input Harmony hooks target both new owners");
            SceneWheelProducerReplay.Run(host, harmony);
            Console.WriteLine("PASS actual DLL trade contract + Harmony.Install + scene entries: " + dll);
            return 0;
        }
        finally { harmony.UnpatchAll(harmony.Id); }
    }
}
