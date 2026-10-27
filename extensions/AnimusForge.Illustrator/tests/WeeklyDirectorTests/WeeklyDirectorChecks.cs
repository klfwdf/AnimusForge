using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;

// Runs only the production text-resolution path. No HTTP, game objects or GPU.
public static class WeeklyDirectorChecks
{
    private const BindingFlags Static = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
    private static Assembly dll;
    private static Type engine, planType;
    private static object options;
    private static int checks;
    private const string Facts = "EVENT_FACT_SENTINEL: Benford captured Aegon; the castle remains with its original owner.";
    private const string Narrative = "Benford captured Aegon after the fighting ended outside the castle.";

    private static void Check(bool ok, string label)
    {
        if (!ok) throw new Exception("FAIL " + label);
        checks++;
        Console.WriteLine("PASS " + label);
    }
    private static string Raw(string key)
    {
        return (string)dll.GetType("AnimusForge.Illustrator.IllustratorText", true)
            .GetMethod("Raw", Static).Invoke(null, new object[] { key });
    }
    private static object Plan(string mode, string redraw = null)
    {
        return Activator.CreateInstance(planType, new object[] { mode, Facts, "", Narrative, redraw });
    }
    private static object Resolve(string body, string mode, string redraw = null)
    {
        return engine.GetMethod("ResolveDirection", Static).Invoke(null, new object[] { body, Plan(mode, redraw), options });
    }
    private static T Get<T>(object value, string property)
    {
        return (T)value.GetType().GetProperty(property).GetValue(value, null);
    }
    private static void Accepted(string body, string name)
    {
        var result = Resolve(body, Raw("Director_937"));
        string prompt = Get<string>(result, "Prompt");
        Check(!Get<bool>(result, "UsedLocalFallback") && Get<string>(result, "DirectionStatus") == "complete", name + ": retained");
        object parsed = dll.GetType("AnimusForge.Illustrator.Core.IllustrationDirection", true)
            .GetMethod("SplitMetadata", Static).Invoke(null, new object[] { body });
        Check(prompt.Contains(Get<string>(parsed, "Prompt")) && prompt.Contains(Facts), name + ": body and event facts preserved");
        string contract = (string)dll.GetType("AnimusForge.Illustrator.Core.VisualFidelityRules", true)
            .GetMethod("GetEssentialContract", Static).Invoke(null, new object[] { false, false, true });
        Check(!string.IsNullOrWhiteSpace(contract) && prompt.Contains(contract), name + ": weekly fidelity contract preserved");
    }
    public static void Run(string dllPath, string incidentBodyPath)
    {
        checks = 0;
        dll = Assembly.LoadFrom(Path.GetFullPath(dllPath));
        engine = dll.GetType("AnimusForge.Illustrator.Core.VisualDirectorEngine", true);
        planType = dll.GetType("AnimusForge.Illustrator.Core.IllustrationPromptPlan", true);
        var type = dll.GetType("AnimusForge.Illustrator.Core.IllustrationOptions", true);
        options = FormatterServices.GetUninitializedObject(type);
        var fidelity = type.GetField("<PreserveEquipmentFidelity>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
        fidelity.SetValue(options, true);

        string negative = "画面采用横向16:9中近景，机位略低于马背，不画任何背盾或背负长矛。";
        Check(engine.GetMethod("ViolatesShieldVisibility", Static) == null, "keyword rejection was removed");
        Accepted(negative, "Chinese negated equipment");
        Accepted("Do not draw a shield strapped to his back. The captive dismounts beside the guards.", "English negated equipment");
        Accepted("A rider dismounts beside the captured lord.", "unstructured visual direction");
        Accepted("纯黑背景中，守卫带走刚被俘虏的领主。", "background keyword");
        Accepted(Narrative, "event narrative sentence");
        if (!string.IsNullOrWhiteSpace(incidentBodyPath)) Accepted(File.ReadAllText(incidentBodyPath), "actual incident director body");

        var metadata = Resolve("[[AFI_PROTOCOL_TITLE]] Captive\n[[AFI_PROTOCOL_ACTION]] The captive dismounts.\n" + negative, Raw("Director_937"));
        Check(Get<string>(metadata, "Title") == "Captive" && Get<string>(metadata, "ActionSummary").Contains("dismounts"), "director title and action retained");
        foreach (string empty in new[] { "", " \n\t", "[[AFI_PROTOCOL_TITLE]] Captive\n" })
            Check(Get<string>(Resolve(empty, Raw("Director_937")), "DirectionStatus") == "failed", "empty or metadata-only body fails without local replacement");
        foreach (string mode in new[] { "Director_1020", "Director_933", "Director_979" })
            Check(!Get<bool>(Resolve(negative, Raw(mode)), "UsedLocalFallback"), mode + ": nonempty direction is retained");
        var screenshot = Resolve(negative, Raw("ScreenshotRules_836"));
        Check(!Get<bool>(screenshot, "UsedLocalFallback") && Get<string>(screenshot, "Prompt").Contains(negative), "free-camera nonempty direction is retained");

        var redraw = Resolve(negative, Raw("Director_937"), "Change the clothing.");
        Check(!Get<bool>(redraw, "UsedLocalFallback") && !Get<string>(redraw, "Prompt").Contains(Facts), "directed redraw does not restore superseded facts");
        fidelity.SetValue(options, false);
        var relaxed = Resolve(negative, Raw("Director_937"));
        string expected = (string)engine.GetMethod("ComposeFinalPrompt", Static).Invoke(null,
            new object[] { negative, Facts, false, false, true, false, false });
        Check(Get<string>(relaxed, "Prompt") == expected, "equipment fidelity toggle still honored");
        Console.WriteLine("PASS " + checks + " actual-DLL checks; no provider calls or game state access.");
    }
}
