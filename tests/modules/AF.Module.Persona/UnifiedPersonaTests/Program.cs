using AnimusForge.Refactor.Runtime;
using System.Reflection;

int checks = 0, failures = 0;
void Check(bool value, string name)
{
    checks++;
    if (!value) { failures++; Console.WriteLine("FAIL " + name); }
}
var resolver = typeof(NpcPersonaProfilePolicy).GetMethod("ResolveDescription", BindingFlags.Static | BindingFlags.NonPublic);
Check(resolver != null, "production description resolver exists");
if (resolver != null)
{
    string Resolve(string description, int version, string personality, string background)
        => (string)resolver.Invoke(null, new object[] { description, version, personality, background });
    foreach (var sample in new[] {
        (P: "personality", B: "background", Expected: "personality\n\nbackground"),
        (P: "only personality", B: "", Expected: "only personality"),
        (P: "", B: "only background", Expected: "only background"),
        (P: " A\r\nB ", B: "A\nB", Expected: "A\nB"),
        (P: "", B: "", Expected: ""),
        (P: "中文原文", B: "English original", Expected: "中文原文\n\nEnglish original"),
        (P: new string('中', 40000), B: new string('x', 40000), Expected: new string('中', 40000) + "\n\n" + new string('x', 40000)) })
    {
        var merged = Resolve(null, 0, sample.P, sample.B);
        Check(merged == sample.Expected, "legacy lossless merge " + checks);
        Check(Resolve(merged, 3, sample.P, sample.B) == merged, "migration idempotent " + checks);
    }
    Check(Resolve("new", 2, "oldP", "oldB") == "new", "new text wins over legacy");
    Check(Resolve("", 3, "oldP", "oldB") == "", "explicit v3 clear does not resurrect legacy");
    Check(Resolve(null, 3, "oldP", "oldB") == "", "null v3 clear remains empty");
}
Check(!NpcPersonaProfilePolicy.NeedsGeneration("hero", "oldP", ""), "legacy personality alone is ready");
Check(!NpcPersonaProfilePolicy.NeedsGeneration("hero", "", "oldB"), "legacy background alone is ready");
Check(NpcPersonaProfilePolicy.NeedsGeneration("hero", "", ""), "empty persona still needs generation");
Check(!NpcPersonaProfilePolicy.NeedsGeneration("", "", ""), "missing identity cannot generate");
AnimusForge.AnimusForgeModulePaths.Root = args[0];
AnimusForge.AnimusForgeModulePaths.Language = args.Length > 1 ? args[1] : "en";
string body = "ONLY_ONCE_BODY 中英\n\n" + new string('x', 2000);
string extras = "ONLY_ONCE_STRUCTURE";
foreach (string intro in new[] {
    AnimusForge.PersonaIntroMessageComposer.ComposeSystemNpc(new() {description=body,personaContext=extras}),
    AnimusForge.PersonaIntroMessageComposer.ComposeSceneNpc(new() {hasHero=true,description=body,personaContext=extras}) })
{
    Check(intro.Split(new[]{body}, StringSplitOptions.None).Length == 2, "actual composed full body injected exactly once");
    Check(intro.Split(new[]{extras}, StringSplitOptions.None).Length == 2, "actual composed structured context injected once");
    Check(!intro.Contains(AnimusForge.PromptComposerTexts.Get("persona_scene_npc_background")) && !intro.Contains(AnimusForge.PromptComposerTexts.Get("persona_system_npc_background")), "no separate legacy background heading");
}
Console.WriteLine($"Unified persona: {checks} checks, {failures} failures");
return failures == 0 ? 0 : 1;

namespace AnimusForge
{
    // Test-only module locator; the actual lazy JSON reader and composers run unchanged.
    internal static class AnimusForgeModulePaths
    {
        internal static string Root, Language;
        internal static string GetModuleDataFilePath(string file)
            => Path.Combine(Root, "content/modules/AF.Module.Prompt/ModuleData", "PromptComposerTexts." + Language + ".json");
    }
}
