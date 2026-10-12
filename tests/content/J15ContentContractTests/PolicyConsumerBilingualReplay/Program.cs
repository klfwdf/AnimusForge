using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TaleWorlds.Localization;

internal static class Program
{
    private static Assembly Candidate;
    private static string References;
    private static int Checks;
    private static readonly Dictionary<string, int> Counts = new Dictionary<string, int>();
    private static readonly List<object> Outputs = new List<object>();
    private static string Category;
    private static bool EnglishPackage;
    private const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    private static void Check(bool condition, string label)
    {
        Checks++;
        Counts[Category] = Counts.TryGetValue(Category, out int count) ? count + 1 : 1;
        if (!condition) throw new InvalidOperationException(Category + ": " + label);
    }
    private static Type Type(string name) => Candidate.GetType(name, true);
    private static object Invoke(string owner, string method, params object[] args)
    {
        MethodInfo member = Type(owner).GetMethods(Any).Single(x => x.Name == method && x.GetParameters().Length == args.Length);
        try { return member.Invoke(null, args); }
        catch (TargetInvocationException ex) { throw ex.InnerException ?? ex; }
    }
    private static string Text(string owner, string method, params object[] args) => (string)Invoke(owner, method, args);
    private static void Set(object target, string key, object value)
    {
        var property = target.GetType().GetProperty(key, Any);
        if (property != null) property.SetValue(target, value);
        else target.GetType().GetField(key, Any).SetValue(target, value);
    }
    private static object Get(object target, string key)
    {
        var property = target.GetType().GetProperty(key, Any);
        return property != null ? property.GetValue(target) : target.GetType().GetField(key, Any).GetValue(target);
    }
    private static string Json(object value) => JsonConvert.SerializeObject(value);
    private static object New(string name) => Activator.CreateInstance(Type(name), true);
    private static string Limit(string value, int max) => value.Length <= max ? value : value.Substring(0, max).TrimEnd() + "…";
    private static string JoinParts(string[] parts, string[] values)
    {
        var result = new StringBuilder(parts[0]);
        for (int index = 0; index < values.Length; index++) result.Append(values[index]).Append(parts[index + 1]);
        return result.ToString();
    }
    private static string Expand(string template, string[] values)
    {
        // Independent one-pass oracle: data never becomes a second template.
        var result = new StringBuilder();
        for (int index = 0; index < template.Length; index++)
        {
            if (template[index] == '{' && index + 2 < template.Length && template[index + 2] == '}'
                && template[index + 1] >= '0' && template[index + 1] <= '9')
            {
                int slot = template[index + 1] - '0';
                if (slot < values.Length) { result.Append(values[slot]); index += 2; continue; }
            }
            result.Append(template[index]);
        }
        return result.ToString();
    }
    private static void Register(string module, string relative, string language)
    {
        string path = relative == null ? Path.Combine(module, "ModuleData", "Languages") : Path.Combine(module, "ModuleData", relative);
        var xml = new XmlDocument();
        xml.LoadXml("<LanguageData id=\"" + language + "\" name=\"" + language
            + "\" supported_iso=\"" + (language == "English" ? "en" : "zh-CN")
            + "\" under_development=\"false\" text_processor=\"TaleWorlds.Localization.TextProcessor.LanguageProcessors.EnglishTextProcessor\">"
            + (relative == null ? "" : "<LanguageFile xml_path=\"" + Path.GetFileName(path) + "\" />") + "</LanguageData>");
        typeof(TextObject).Assembly.GetType("TaleWorlds.Localization.LanguageData", true)
            .GetMethod("LoadFromXml", Any).Invoke(null, new object[] { xml, Path.GetDirectoryName(path) });
    }
    private static void Memory(JObject data)
    {
        Category = "policy-agenda-memory";
        string[] parts = data["agendaRawParts"].Values<string>().ToArray();
        string template = (string)data[EnglishPackage ? "agendaEn" : "agendaCn"];
        string[] prefixValues = data["factPrefixes"].Values<string>().ToArray();
        string[] scopes = data["factScopes"].Values<string>().ToArray();
        var names = new[] { "Ruler", "名 {0}/{1}/{2}/{HERO}/$1/$&", "O'Brien-Smith", "Queen I of X" };
        foreach (string name in names)
        {
            string[] values = { name, "proposal {2} $1 at 123 denars, still pending", "policy {0} {PLAYER} $& 7" };
            string body = JoinParts(parts, values);
            string expected = EnglishPackage ? Expand(template, values) : body;
            Check(Text("AnimusForge.MemorySystemFactProjection", "ProjectBody", body) == expected, "known body and exact package semantics");
            foreach (string prefix in prefixValues)
            {
                string fact = prefix + " " + body;
                string translated = prefix + " " + expected;
                Check(Text("AnimusForge.MemorySystemFactProjection", "ProjectNpcFact", fact) == translated, "AFEF prefix and literal fields");
                Check(Text("AnimusForge.MemorySystemFactProjection", "ProjectNpcFact", translated) == translated, "projection idempotent");
                foreach (string scope in scopes)
                {
                    string source = scope + "[1084年春季7日 | scene {PLAYER} $1 | speaker {0}] " + fact;
                    string target = scope + "[1084年春季7日 | scene {PLAYER} $1 | speaker {0}] " + translated;
                    Check(Text("AnimusForge.MemorySystemFactProjection", "ProjectRecordedFactForPrompt", source) == target, "captured scope/date/scene/speaker preserved");
                    string multi = source + "\r\nunknown {1} $1\n" + fact;
                    string multiExpected = target + "\r\nunknown {1} $1\n" + translated;
                    Check(Text("AnimusForge.MemorySystemFactProjection", "ProjectRecordedFactForPrompt", multi) == multiExpected, "mixed newlines and unknown records preserved");
                }
            }
            var ambiguous = new[] {
                "unknown fact body {HERO} $1",
                JoinParts(parts, new[] { name + parts[1] + "dup", values[1], values[2] }),
                JoinParts(parts, new[] { name, values[1] + parts[2] + "dup", values[2] }),
                JoinParts(parts, new[] { name, values[1], values[2] + parts[3] + "dup" }),
                JoinParts(parts, new[] { "", values[1], values[2] }),
                body.Substring(0, body.Length - 1)
            };
            foreach (string source in ambiguous)
            {
                string result = Text("AnimusForge.MemorySystemFactProjection", "ProjectBody", source);
                Check(result == source, "unknown/ambiguous/incomplete body retained: " + Json(source) + " -> " + Json(result));
            }
            Outputs.Add(new { kind = "agenda", body, expected });
        }
        Check(((string)data["agendaEn"]).Contains("still pending, has not passed"), "review is pending and not enacted");
        Check(((string)data["agendaEn"]).Contains("no numerical effects, policy events, or public feedback"), "no fabricated effects/events/feedback");
    }
    private static void Weekly(JObject data)
    {
        Category = "policy-weekly-actual-producer";
        string noNumericRaw = (string)data["noNumericRaw"];
        string noNumericEn = (string)data["noNumericEn"];
        foreach (bool vassal in new[] { false, true })
        {
            string kind = vassal ? "player_vassal_policy" : "ruler_policy";
            string suffix = vassal ? "Vassal" : "Ruler";
            string[] rawParts = data["weeklyRaw" + suffix].Values<string>().ToArray();
            string[] enParts = data["weeklyEn" + suffix].Values<string>().ToArray();
            string labelRaw = (string)data["labelRaw" + suffix], labelEn = (string)data["labelEn" + suffix];
            foreach (string literal in new[] { "name", "名 {0}/{TARGET}/{HERO}/$1/$&", "O'Brien I", "Queen of X" })
            {
                object record = New("AnimusForge.NpcRulerPolicyRecord");
                Set(record, "PolicyName", "Policy " + literal); Set(record, "RulerName", "Ruler " + literal);
                Set(record, "PolicyDigest", "Summary " + literal); Set(record, "FeedbackDigest", "Event " + literal);
                object attribution = Activator.CreateInstance(Type("AnimusForge.NpcRulerPolicyBehavior")
                    .GetNestedType("UnifiedPolicyWeeklyAttribution", Any), true);
                Set(attribution, "IsPlayerVassalPolicy", vassal); Set(attribution, "PublisherLabel", "Player " + literal);
                Set(attribution, "IssuerKingdomName", "Issuer " + literal); Set(attribution, "TargetKingdomName", "Vassal " + literal);
                string[] values = vassal
                    ? new[] { "Player " + literal, "Vassal " + literal, "Ruler " + literal, "Policy " + literal,
                        "Summary " + literal, "Event " + literal, "Target " + literal, noNumericRaw }
                    : new[] { "Issuer " + literal, "Ruler " + literal, "Policy " + literal, "Summary " + literal,
                        "Event " + literal, "Target " + literal, noNumericRaw };
                string raw = Text("AnimusForge.NpcRulerPolicyBehavior", "BuildUnifiedPolicyWeeklySnapshot", record, attribution, "Target " + literal, noNumericRaw);
                Check(raw == JoinParts(rawParts, values), "real producer matches conserved fact template");
                string rawBefore = Json(record), attrBefore = Json(attribution);
                string[] enValues = (string[])values.Clone(); enValues[enValues.Length - 1] = noNumericEn;
                string expected = EnglishPackage ? JoinParts(enParts, enValues) : raw;
                string label = labelRaw + " - Target " + literal + " / Policy " + literal;
                string labelExpected = (EnglishPackage ? labelEn : labelRaw) + " - Target " + literal + " / Policy " + literal;
                Check(Text("AnimusForge.PolicyWeeklyFactPromptProjection", "Snapshot", kind, raw) == expected, "real producer snapshot projected at request");
                Check(Text("AnimusForge.PolicyWeeklyFactPromptProjection", "Snapshot", kind, expected) == expected, "snapshot idempotent");
                Check(Text("AnimusForge.PolicyWeeklyFactPromptProjection", "Label", kind, label) == labelExpected, "literal label suffix retained");
                Check(rawBefore == Json(record) && attrBefore == Json(attribution), "record and attribution unmodified");
                foreach (bool shortOnly in new[] { false, true }) Detached(kind, label, raw, labelExpected, expected, shortOnly);
                foreach (string unknown in new[] { "Unknown " + raw, rawParts[0] + "unknown short fragment", raw.Substring(0, raw.Length - 1), rawParts[0] + "short…" })
                    Check(Text("AnimusForge.PolicyWeeklyFactPromptProjection", "Snapshot", kind, unknown) == unknown, "incomplete/unknown snapshot retained");
                string[] repeated = (string[])values.Clone(); repeated[0] += rawParts[1] + "duplicate";
                string ambiguous = JoinParts(rawParts, repeated);
                Check(Text("AnimusForge.PolicyWeeklyFactPromptProjection", "Snapshot", kind, ambiguous) == ambiguous, "repeated boundary retained");
                Check(Text("AnimusForge.PolicyWeeklyFactPromptProjection", "Snapshot", "not_policy", raw) == raw, "non-policy material untouched");
                Outputs.Add(new { kind, raw, expected, label, labelExpected });
            }
            // Exercise the actual producer's cap and typed full/short consumers.
            var longRecord = New("AnimusForge.NpcRulerPolicyRecord");
            Set(longRecord, "RulerName", new string('R', 50)); Set(longRecord, "PolicyName", new string('P', 70));
            Set(longRecord, "PolicyDigest", new string('D', 140)); Set(longRecord, "FeedbackDigest", new string('F', 70));
            var longAttribution = Activator.CreateInstance(Type("AnimusForge.NpcRulerPolicyBehavior").GetNestedType("UnifiedPolicyWeeklyAttribution", Any), true);
            Set(longAttribution, "IsPlayerVassalPolicy", vassal); Set(longAttribution, "PublisherLabel", new string('U', 50));
            Set(longAttribution, "IssuerKingdomName", new string('K', 50)); Set(longAttribution, "TargetKingdomName", new string('T', 50));
            string[] longValues = vassal ? new[] { new string('U', 50), new string('T', 50), new string('R', 50), new string('P', 70), new string('D', 140), new string('F', 70), new string('A', 50), new string('E', 120) }
                : new[] { new string('K', 50), new string('R', 50), new string('P', 70), new string('D', 140), new string('F', 70), new string('A', 50), new string('E', 120) };
            string capped = Text("AnimusForge.NpcRulerPolicyBehavior", "BuildUnifiedPolicyWeeklySnapshot", longRecord, longAttribution, new string('A', 50), new string('E', 120));
            Check(capped == Limit(JoinParts(rawParts, longValues), 420), "actual 420 limit and ellipsis conserved");
            // An independent oracle replaces only fully present, unique known wrappers.
            string cappedExpected = capped;
            if (EnglishPackage)
                for (int part = 0; part < rawParts.Length - 1; part++)
                    if (cappedExpected.Contains(rawParts[part])) cappedExpected = cappedExpected.Replace(rawParts[part], enParts[part]);
            Check(Text("AnimusForge.PolicyWeeklyFactPromptProjection", "Snapshot", kind, capped) == cappedExpected, "real capped snapshot retains opaque tail");
            foreach (bool shortOnly in new[] { false, true }) Detached(kind, labelRaw + " - long", capped, (EnglishPackage ? labelEn : labelRaw) + " - long", cappedExpected, shortOnly);
            Outputs.Add(new { kind, capped, cappedExpected });
        }
    }
    private static void Detached(string kind, string label, string snapshot, string expectedLabel, string expectedSnapshot, bool shortOnly)
    {
        string previous = Category; Category = "policy-weekly-real-prepare";
        object group = New("AnimusForge.MyBehavior+WeeklyEventMaterialPreviewGroup");
        object material = New("AnimusForge.MyBehavior+EventMaterialReference");
        Set(material, "MaterialType", kind); Set(material, "Label", label); Set(material, "SnapshotText", snapshot);
        Set(material, "HeroId", "hero:key:{0}"); Set(material, "KingdomId", "kingdom:key:$1");
        Set(material, "ActionDay", (int?)7); Set(material, "ActionSequence", (int?)123); Set(material, "ActionStableKey", "policy:key:$1");
        ((IList)Get(material, "RelatedHeroIds")).Add("related:hero");
        ((IList)Get(material, "SourceStableKeys")).Add("source:key");
        object unknown = New("AnimusForge.MyBehavior+EventMaterialReference");
        Set(unknown, "MaterialType", "not_policy"); Set(unknown, "Label", "Unknown {0} $1"); Set(unknown, "SnapshotText", "Unknown original {HERO} $&");
        ((IList)Get(group, "Materials")).Add(material); ((IList)Get(group, "Materials")).Add(unknown);
        object originalList = Get(group, "Materials");
        string before = Json(originalList);
        Invoke("AnimusForge.WeeklyPromptMaterialOwner", "Prepare", group, shortOnly);
        Check(ReferenceEquals(originalList, Get(group, "Materials")) && Json(originalList) == before, "saved source list identity and all fields retained");
        var prompts = (IList)Get(group, "PromptMaterials");
        Check(prompts.Count == 2, "original material eligibility/count preserved");
        object copy = prompts.Cast<object>().Single(x => (string)Get(x, "MaterialType") == kind);
        Check(!ReferenceEquals(material, copy), "request material is a detached copy");
        Check((string)Get(copy, "Label") == expectedLabel && (string)Get(copy, "SnapshotText") == expectedSnapshot, "full/short actual prompt copy language");
        foreach (var field in material.GetType().GetFields(Any))
        {
            if (field.IsStatic || field.Name == "Label" || field.Name == "SnapshotText") continue;
            // The pre-existing copy contract normalizes null/whitespace string
            // leaves. The saved source must stay byte-identical above.
            object expectedField = field.GetValue(material);
            if (field.FieldType == typeof(string)) expectedField = ((string)expectedField ?? "").Trim();
            Check(Json(field.GetValue(copy)) == Json(expectedField), "material metadata: " + field.Name);
            if (field.GetValue(material) is IList) Check(!ReferenceEquals(field.GetValue(copy), field.GetValue(material)), "detached metadata list: " + field.Name);
        }
        object opaque = prompts.Cast<object>().Single(x => (string)Get(x, "MaterialType") == "not_policy");
        Check((string)Get(opaque, "Label") == (string)Get(unknown, "Label") && (string)Get(opaque, "SnapshotText") == (string)Get(unknown, "SnapshotText"), "other kinds unchanged");
        Check((bool)Get(group, "IncludePreviousReportInPrompt") == !shortOnly, "previous report/output mode preserved");
        Check(Get(group, "OutputMode").ToString() == (shortOnly ? "TitleShortTagsOnly" : "FullReport"), "typed full/short mode preserved");
        Category = previous;
    }
    private static void Cues(JObject data)
    {
        Category = "effect-modules-real-descriptors";
        foreach (JObject module in data["moduleCues"])
        {
            object instance = New((string)module["type"]);
            object descriptor = Get(instance, "Descriptor");
            string[] actual = ((IEnumerable)Get(descriptor, "CueTerms")).Cast<string>().ToArray();
            string[] expected = module["cueTerms"].Values<string>().ToArray();
            Check(actual.SequenceEqual(expected), "all original ordered Chinese/English cues: " + module["type"]);
            Check((string)Get(descriptor, "Id") == (string)module["id"], "canonical module ID retained");
            Check((int)Get(descriptor, "Order") == (int)module["order"], "descriptor order retained");
        }
    }
    private static int Main(string[] args)
    {
        Console.OutputEncoding = new UTF8Encoding(false);
        References = args[1]; EnglishPackage = args[2] == "EN";
        AppDomain.CurrentDomain.AssemblyResolve += (sender, e) => {
            foreach (string directory in References.Split('|'))
            {
                string path = Path.Combine(directory, new AssemblyName(e.Name).Name + ".dll");
                if (File.Exists(path)) return Assembly.LoadFrom(path);
            }
            return null;
        };
        try
        {
            Candidate = Assembly.LoadFrom(args[0]);
            string module = args[3]; JObject data = JObject.Parse(File.ReadAllText(args[4], Encoding.UTF8));
            Category = "real-module-and-package";
            Check(Path.GetFullPath((string)Invoke("AnimusForge.AnimusForgeModulePaths", "GetCurrentModuleRoot")) == Path.GetFullPath(module), "real module paths resolve isolated content fixture");
            Check(Text("AnimusForge.PromptComposerTexts", "Get", "policy_projection_language") == (EnglishPackage ? "en" : "zh-CN"), "fixed content package selection");
            Register(module, "Languages/afpol_ui_strings.xml", "English");
            Register(module, "Languages/CNs/afpol_ui_strings-zh-CN.xml", "简体中文");
            // A real locale with no AF translations exercises the native fallback.
            Register(module, null, "French");
            foreach (string language in new[] { "English", "简体中文", "French", "English" })
            {
                Category = "real-module-and-package";
                Check(MBTextManager.ChangeLanguage(language), "real engine language switch: " + language);
                Check(Text("AnimusForge.PromptComposerTexts", "Get", "policy_projection_language") == (EnglishPackage ? "en" : "zh-CN"), "UI does not change package prompts");
                Memory(data); Weekly(data); Cues(data);
            }
            File.WriteAllText(args[5], JsonConvert.SerializeObject(new { passed = true, package = args[2], assembly = args[0], assemblyVersion = Candidate.GetName().Version.ToString(), checks = Checks, counts = Counts, outputs = Outputs }, Newtonsoft.Json.Formatting.Indented), new UTF8Encoding(false));
            Console.WriteLine("PASS actual policy consumers " + args[2] + ": " + Checks + " checks; " + Json(Counts));
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
