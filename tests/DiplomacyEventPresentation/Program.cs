using AnimusForge;
using AnimusForge.Refactor.Contracts;
using System.Xml.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

int checks = 0;
void Check(bool value, string message) { if (!value) throw new Exception(message); checks++; }
DiplomacyEventPresentation Data(DiplomacyEventScene scene, string culture = "", string id = "case-1", string saved = "", DiplomacyEventStage stage = DiplomacyEventStage.Started)
    => new(id, 1, stage, "{literal}<title>", "body\nbody", Array.Empty<string>(), Array.Empty<DiplomacyEventEffect>(), scene, culture, saved);

var names = new List<string> { "A" };
var effects = new List<DiplomacyEventEffect> { new("applied change") };
var snapshot = new DiplomacyEventPresentation("test", 1, DiplomacyEventStage.Started, "title", "body", names, effects);
names[0] = "changed"; effects.Clear();
Check(snapshot.Participants[0] == "A" && snapshot.Effects.Count == 1, "Caller mutations changed the presentation snapshot.");
Check(!DiplomacyEventIllustrations.IsKnown("../../secret"), "Path accepted as asset ID.");
var neutral = DiplomacyEventIllustrations.Entries.Where(x => x.Culture.Length == 0).Select(x => x.Id).ToHashSet();
Check(neutral.Contains(DiplomacyEventIllustrations.Select(Data(DiplomacyEventScene.Unspecified))), "Unknown scene must use neutral fallback.");
Check(neutral.Contains(DiplomacyEventIllustrations.Select(Data(DiplomacyEventScene.Triumph, "khuzait"))), "Missing culture art must use neutral fallback.");
Check(DiplomacyEventIllustrations.Select(Data(DiplomacyEventScene.Battle, saved: "16-siege-encampment")) == "15-armies-clash", "Persisted image bypassed factual eligibility.");
Check(DiplomacyEventIllustrations.Select(Data(DiplomacyEventScene.Muster, "unknown")) == "14-army-departure", "Unknown culture picked foreign banners.");
foreach (string culture in new[] { "aserai", "sturgia", "vlandia", "khuzait", "battania", "empire" })
{
    var allowed = DiplomacyEventIllustrations.Entries.Where(x => x.Culture == culture).Select(x => x.Id).Append("14-army-departure").ToHashSet();
    string last = "";
    for (int i=0; i<50; i++)
    {
        string chosen = DiplomacyEventIllustrations.Select(Data(DiplomacyEventScene.Muster,culture), new[] { last });
        Check(allowed.Contains(chosen) && chosen != last, "Culture selection or adjacent exclusion failed.");
        last = chosen;
    }
}
string previous = "00-approved-armistice";
for (int i=0;i<100;i++)
{
    string selected = DiplomacyEventIllustrations.Select(Data(DiplomacyEventScene.Negotiation, saved: previous), new[] { "16-siege-encampment", previous });
    Check(selected != previous && neutral.Contains(selected), "Last entry is newest; stored image must not bypass adjacent exclusion.");
    previous = selected;
}
Check(DiplomacyEventIllustrations.Select(Data(DiplomacyEventScene.Siege), new[] { "16-siege-encampment" }) != "16-siege-encampment", "One suitable image must fall back instead of repeating.");
Check(DiplomacyEventIllustrations.Select(Data(DiplomacyEventScene.Negotiation, stage: DiplomacyEventStage.Ended)) == "00-approved-armistice", "First negotiation uses matching art.");

string root = args.Length > 0 ? Path.GetFullPath(args[0]) : Directory.GetCurrentDirectory();
string content = Path.Combine(root, "content/modules/AF.Module.Diplomacy");
var en = XDocument.Load(Path.Combine(content, "ModuleData/Languages/afdip_event_ui_strings.xml")).Descendants("string").ToDictionary(x => (string)x.Attribute("id"), x => (string)x.Attribute("text"));
var cn = XDocument.Load(Path.Combine(content, "ModuleData/Languages/CNs/afdip_event_ui_strings-zh-CN.xml")).Descendants("string").ToDictionary(x => (string)x.Attribute("id"), x => (string)x.Attribute("text"));
Check(en.Keys.ToHashSet().SetEquals(cn.Keys), "CN/EN key mismatch.");
Check(en.Values.All(x => !string.IsNullOrEmpty(x)) && cn.Values.All(x => !string.IsNullOrEmpty(x)), "Empty translation.");
foreach (string sub in new[] { "", "CNs/" })
    Check(XDocument.Load(Path.Combine(root, "content/foundation/AF.Foundation.Localization/ModuleData/Languages/" + sub + "language_data.xml")).Descendants("LanguageFile").Any(x => ((string)x.Attribute("xml_path")).Contains("afdip_event_ui_strings")), "Language registration missing.");
var map = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "content/content-map.json"))).RootElement.GetProperty("entries").EnumerateArray().ToArray();
foreach (var asset in DiplomacyEventIllustrations.Entries.Select(x => x.Id).Append("event-chrome"))
{
    string target = "GUI/SpriteParts/af_diplomacy_event/" + asset + ".png";
    var row = map.Single(x => x.GetProperty("target").GetString() == target);
    byte[] png = File.ReadAllBytes(Path.Combine(root, row.GetProperty("source").GetString()));
    Check(png.Length < 8 * 1024 * 1024 && png[0] == 137 && png[1] == 80, "Invalid or oversized texture.");
}
XDocument prefab = XDocument.Load(Path.Combine(content, "GUI/Prefabs/DiplomacyEventPopup.xml"));
var chromeClip = prefab.Descendants("Widget").Single(x => (string)x.Attribute("Id") == "ChromeClip");
var chrome = chromeClip.Descendants("Widget").Single(x => (string)x.Attribute("Sprite") == "@ChromeSprite");
Check((string)chromeClip.Attribute("ClipContents") == "true", "Outer chrome matte must be clipped.");
foreach (string edge in new[] { "Left", "Top" })
    Check((int)chromeClip.Attribute("Margin" + edge) + (int)chrome.Attribute("Margin" + edge) == 0,
        "Chrome clipping shifted the artwork relative to text: " + edge);
Check((int)chrome.Attribute("SuggestedWidth") == 640 && (int)chrome.Attribute("SuggestedHeight") == 640,
    "Chrome clipping rescaled the artwork.");
Check((int)chromeClip.Attribute("MarginLeft") == 14 && (int)chromeClip.Attribute("MarginRight") == 14
    && (int)chromeClip.Attribute("MarginTop") == 12 && (int)chromeClip.Attribute("MarginBottom") == 14,
    "Crop must follow the gold perimeter without cutting ornamentation.");
string vm = File.ReadAllText(Path.Combine(root, "src/AF.GameAdapter.Bannerlord/UI/Diplomacy/DiplomacyEventPopupVM.cs"));
foreach (var attr in prefab.Descendants().Attributes().Where(x => x.Value.StartsWith("@")))
    Check(Regex.IsMatch(vm, @"\b" + Regex.Escape(attr.Value.Substring(1)) + @"\b"), "Missing binding " + attr.Value);
foreach (var attr in prefab.Descendants().Attributes().Where(x => x.Name.LocalName == "Text"))
    Check(attr.Value.StartsWith("@"), "Hard-coded prefab text.");
Check(prefab.Descendants("ScrollablePanel").Count() == 1, "Long-content scroll owner missing.");
Check(prefab.Descendants("ButtonWidget").Single().Attribute("DoNotPassEventsToChildren")?.Value == "true", "Acknowledgement button loses clicks.");
SpriteLifetimeReplay.Run(Check);
PreviewPendingReplay.Run(Check);
Console.WriteLine($"PASS {checks} checks; production selector/snapshot, asynchronous sprite lifetime + localized asset/binding contracts. Native Gauntlet rendering is not exercised.");
