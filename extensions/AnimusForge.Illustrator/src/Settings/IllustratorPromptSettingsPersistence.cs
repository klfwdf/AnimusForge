using System;
using HarmonyLib;
using MCM.Abstractions;
using MCM.Abstractions.Base;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AnimusForge.Illustrator
{
    // MCM serializes/copies registered properties only. Keep editor-only text in
    // its existing JSON and copy/reset flow without adding duplicate text boxes.
    internal static class IllustratorPromptSettingsPersistence
    {
        internal static void Install(Harmony harmony, Type jsonFormat)
        {
            var save = AccessTools.Method(jsonFormat, "SaveJson", new[] { typeof(BaseSettings) });
            var load = AccessTools.Method(jsonFormat, "TryLoadFromJson", new[] { typeof(BaseSettings).MakeByRefType(), typeof(string) });
            // Patch the real copy body; OverrideSettings is a tiny wrapper that may be inlined.
            var copy = AccessTools.Method(typeof(SettingsUtils), nameof(SettingsUtils.OverrideValues), new[] { typeof(BaseSettings), typeof(BaseSettings) });
            if (save == null || load == null || copy == null)
                throw new MissingMethodException("MCM editor prompt persistence is unavailable.");
            harmony.Patch(save, postfix: new HarmonyMethod(typeof(IllustratorPromptSettingsPersistence), nameof(Saved)));
            harmony.Patch(load, postfix: new HarmonyMethod(typeof(IllustratorPromptSettingsPersistence), nameof(Loaded)));
            harmony.Patch(copy, postfix: new HarmonyMethod(typeof(IllustratorPromptSettingsPersistence), nameof(Copied)));
        }

        private static void Saved(BaseSettings __0, ref string __result)
        {
            if (!(__0 is IllustratorSettings settings)) return;
            var json = JObject.Parse(__result);
            json[nameof(IllustratorSettings.CustomStylePrompt)] = settings.CustomStylePrompt ?? "";
            json[nameof(IllustratorSettings.NegativePrompt)] = settings.NegativePrompt ?? "";
            json[nameof(IllustratorSettings.CustomDirectorPrompt)] = settings.CustomDirectorPrompt ?? "";
            __result = json.ToString(Formatting.Indented);
        }

        private static void Loaded(BaseSettings __0, string __1, bool __result)
        {
            if (!__result || !(__0 is IllustratorSettings settings)) return;
            var json = JObject.Parse(__1);
            settings.CustomStylePrompt = ReadText(json, nameof(IllustratorSettings.CustomStylePrompt), settings.CustomStylePrompt);
            settings.NegativePrompt = ReadText(json, nameof(IllustratorSettings.NegativePrompt), settings.NegativePrompt);
            settings.CustomDirectorPrompt = ReadText(json, nameof(IllustratorSettings.CustomDirectorPrompt), settings.CustomDirectorPrompt);
        }

        private static string ReadText(JObject json, string key, string fallback)
            => json.TryGetValue(key, out JToken value) && value.Type == JTokenType.String
                ? value.Value<string>() : fallback;

        private static void Copied(BaseSettings __0, BaseSettings __1)
        {
            if (!(__0 is IllustratorSettings target) || !(__1 is IllustratorSettings source)) return;
            target.CustomStylePrompt = source.CustomStylePrompt;
            target.NegativePrompt = source.NegativePrompt;
            target.CustomDirectorPrompt = source.CustomDirectorPrompt;
            // MCM also copies button delegates, which would otherwise edit the
            // source object and leave the new page holding stale prompt text.
            target.BindPromptEditors();
        }
    }
}
