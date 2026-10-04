using System.Text;
using AnimusForge;

int checks = 0;
void Check(bool result, string name)
{
    if (!result) throw new Exception(name);
    checks++;
}

var history = Enumerable.Range(0, 40).Select(i => new ConversationMessage
{
    Role = i % 2 == 0 ? "user" : "assistant",
    Content = "message-" + i + ":" + new string('私', 1500)
}).ToList();
string recent = DiplomacyMemoryMaterial.BuildRecentHistory(history);
Check(recent.Length <= 4800, "recent budget");
Check(recent.Contains("message-39:"), "latest ruler promise survives long history");
Check(recent.Contains("message-38:"), "latest player statement survives long history");
Check(!recent.Contains("message-27:"), "old messages excluded");
Check(recent.IndexOf("message-38:") < recent.IndexOf("message-39:"), "chronological rendering");
Check(recent.Contains("role=user") && recent.Contains("role=assistant"), "source roles preserved");
Check(DiplomacyMemoryMaterial.BuildRecentHistory(null) == "", "missing hero material is empty");

var isolated = new[] { new ConversationMessage { Role = "assistant", Content = "另一统治者的经历" } };
Check(!recent.Contains("另一统治者"), "unrequested material never merged");
Check(DiplomacyMemoryMaterial.BuildRecentHistory(isolated).Contains("另一统治者"), "explicit material rendered");

StringBuilder prompt = new StringBuilder("私人决策材料\n");
DiplomacyMemoryMaterial.AppendSection(prompt, recent, 4800, 12000);
for (int i = 0; i < 100; i++) DiplomacyMemoryMaterial.AppendSection(prompt, new string('忆', 10000), 1800, 12000);
Check(prompt.Length <= 12000, "combined input budget");
Check(prompt.ToString().Contains("message-39:"), "recent dialogue retains reserved budget");

StringBuilder unicode = new StringBuilder();
DiplomacyMemoryMaterial.AppendSection(unicode, "A😀B", 2, 100);
Check(unicode.ToString() == "A" + Environment.NewLine, "UTF16 pair never split");
string tiny = DiplomacyMemoryMaterial.BuildRecentHistory(history, maximumChars: 5);
Check(tiny == "", "tiny budget does not produce a misleading partial role");
Check(DiplomacyMemoryMaterial.IsRulerSnapshotCurrent("old", "old", true), "live original ruler may use private material");
Check(!DiplomacyMemoryMaterial.IsRulerSnapshotCurrent("old", "new", true), "successor may not use queued private material");
Check(!DiplomacyMemoryMaterial.IsRulerSnapshotCurrent("old", "old", false), "dead ruler draft invalidated");
Check(!DiplomacyMemoryMaterial.IsRulerSnapshotCurrent("old", "", true), "lost throne invalidates snapshot");
Check(DiplomacyMemoryMaterial.IsRulerSnapshotCurrent("", "new", true), "legacy jobs without private snapshot remain compatible");
Console.WriteLine("PASS " + checks + " diplomacy memory checks");
