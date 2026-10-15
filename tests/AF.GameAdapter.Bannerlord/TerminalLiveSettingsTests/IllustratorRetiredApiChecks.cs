using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MCM.Abstractions.Base;
using Newtonsoft.Json.Linq;

// Real MCM JSON load/save and production resolver/options; the host API boundary is deterministic.
static class IllustratorRetiredApiChecks
{
    const BindingFlags All = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static bool HostAvailable = true;
    static bool Primary(ref string apiUrl, ref string apiKey, ref string modelName, ref bool __result)
    {
        apiUrl = HostAvailable ? "https://main.fixture.invalid/v1/chat/completions" : "";
        apiKey = HostAvailable ? "main-fixture-key" : "";
        modelName = HostAvailable ? "main-fixture-model" : "";
        __result = HostAvailable;
        return false;
    }

    internal static void Run(Assembly af, object format, Action<bool, string> check)
    {
        Type settingsType = af.GetType("AnimusForge.Illustrator.IllustratorSettings", true);
        Type director = af.GetType("AnimusForge.Illustrator.Core.VisualDirectorEngine", true);
        Type optionsType = af.GetType("AnimusForge.Illustrator.Core.IllustrationOptions", true);
        var harmony = new Harmony("af.tests.illustrator.retired.api");
        MethodInfo host = director.GetMethod("TryGetHostPrimaryChatConfig", All);
        harmony.Patch(host, prefix: new HarmonyMethod(typeof(IllustratorRetiredApiChecks).GetMethod("Primary", All)));
        var save = format.GetType().GetMethod("SaveJson");
        var load = format.GetType().GetMethod("LoadFromJson");
        string[] retired = { "DirectorApiBaseUrl", "DirectorApiKey", "DirectorModelName", "DirectorModelDropdown", "FetchDirectorModelList", "UseExactEndpointUrl" };
        try
        {
            foreach (bool exact in new[] { false, true })
            {
                var settings = (BaseSettings)Activator.CreateInstance(settingsType);
                var legacy = JObject.Parse((string)save.Invoke(format, new object[] { settings }));
                legacy["DirectorApiBaseUrl"] = "https://stale.fixture.invalid/v1";
                legacy["DirectorApiKey"] = "retired-fixture-key";
                legacy["DirectorModelName"] = "retired-fixture-model";
                legacy["UseExactEndpointUrl"] = exact;
                legacy["ApiBaseUrl"] = "https://image.fixture.invalid/v1";
                legacy["ApiKey"] = "image-fixture-key";
                legacy["CustomDirectorPrompt"] = "fixture custom composition";
                load.Invoke(format, new object[] { settings, legacy.ToString() });
                // Invoke the old resolver too: the pre-fix control must fail on stale overrides.
                MethodInfo resolve = director.GetMethod("ResolveChatConfigForSnapshot", All);
                object[] args = resolve.GetParameters().Length == 4
                    ? new object[] { settings, null, null, null } : new object[] { null, null, null };
                HostAvailable = true;
                check((bool)resolve.Invoke(null, args), "director resolves main API with legacy config");
                int offset = args.Length - 3;
                check((string)args[offset] == "https://main.fixture.invalid/v1/chat/completions"
                    && (string)args[offset + 1] == "main-fixture-key" && (string)args[offset + 2] == "main-fixture-model",
                    "retired director URL/key/model cannot override main API");
                object options = Activator.CreateInstance(optionsType, All, null,
                    new object[] { settings, args[offset], args[offset + 1], args[offset + 2] }, null);
                check((string)optionsType.GetProperty("DirectorApiBaseUrl").GetValue(options) == (string)args[offset], "request freezes main director endpoint");
                check(optionsType.GetProperty("UseExactEndpointUrl", All) == null, "exact endpoint flag removed from request snapshot");
                foreach (string property in retired)
                {
                    check(settingsType.GetProperty(property, All) == null, "retired property absent: " + property);
                    check(JObject.Parse((string)save.Invoke(format, new object[] { settings }))[property] == null, "native MCM save omits retired property: " + property);
                }
                var clean = JObject.Parse((string)save.Invoke(format, new object[] { settings }));
                check((string)clean["ApiKey"] == "image-fixture-key", "image credentials survive legacy load/save");
                var preset = (BaseSettings)Activator.CreateInstance(settingsType);
                load.Invoke(format, new object[] { preset, clean.ToString() });
                check(!retired.Any(p => JObject.Parse((string)save.Invoke(format, new object[] { preset }))[p] != null), "preset roundtrip cannot revive retired fields");
                HostAvailable = false;
                Array.Clear(args, offset, 3);
                check(!(bool)resolve.Invoke(null, args) && args.Skip(offset).All(value => string.IsNullOrEmpty((string)value)),
                    "missing main API never borrows image credentials or stale director endpoint");
            }
        }
        finally { HostAvailable = true; harmony.Unpatch(host, HarmonyPatchType.All, harmony.Id); }
    }
}
