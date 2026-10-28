using System.Text;
using AnimusForge;
int checks = 0;
void Check(bool result, string name) { if (!result) throw new Exception(name); checks++; }
var history = Enumerable.Range(0, 40).Select(i => new ConversationMessage {
    Role = i % 2 == 0 ? "user" : "assistant", EventSequence = i + 1,
    Content = (i == 10 || i == 11 ? "trade" : "sports") + " message-" + i + ":" + new string('私', 1500)
}).ToList();
string related = DiplomacyMemoryMaterial.BuildRelevantHistory(history, "trade");
Check(related.Length <= 4800, "whole turn budget");
Check(related.Contains("message-10:") && related.Contains("message-11:"), "old relevant question and answer are retained together");
Check(!related.Contains("message-39:"), "latest unrelated conversation is not filler");
Check(related.IndexOf("message-10:") < related.IndexOf("message-11:"), "original ordering");
Check(related.Contains(MemoryTextResources.Prompt("MEM_Prompt_054") + "user") && related.Contains(MemoryTextResources.Prompt("MEM_Prompt_054") + "assistant"), "actual source roles");
Check(DiplomacyMemoryMaterial.BuildRelevantHistory(null, "trade") == "", "missing material");
Check(DiplomacyMemoryMaterial.BuildRelevantHistory(history, "") == "", "no subject means no forced recent history");
Check(DiplomacyMemoryMaterial.BuildRelevantHistory(history, "unknownmatter") == "", "no relevant match means no filler");
Check(DiplomacyMemoryMaterial.BuildRelevantHistory(history, "trade", maximumChars: 1000) == "", "oversized pair omitted rather than cutting off conditions");
history[10].Content += "\n\nonly if the town is returned";
related = DiplomacyMemoryMaterial.BuildRelevantHistory(history, "trade");
Check(related.Contains("only if the town is returned") && !related.Contains("\n\n"), "embedded paragraphs stay inside their complete paired source");
StringBuilder prompt = new("private context\n");
DiplomacyMemoryMaterial.AppendSection(prompt, related, 4800, 12000);
for (int i = 0; i < 100; i++) DiplomacyMemoryMaterial.AppendSection(prompt, new string('忆', 10000), 1800, 12000);
Check(prompt.Length <= 12000 && prompt.ToString().Contains("only if the town is returned"), "whole summary budget retains conditions");
StringBuilder unicode = new(); DiplomacyMemoryMaterial.AppendSection(unicode, "A😀B", 2, 100);
Check(unicode.Length == 0, "small budget omits the entire source");
Check(DiplomacyMemoryMaterial.Relevant("trade", "A trade promise"), "lexical relevance");
Check(!DiplomacyMemoryMaterial.Relevant("trade", "A sporting match"), "small archive is not automatically relevant");
float[] Embed(string text) => text is "commerce" or "trade" ? new float[] { 1, 0 } : new float[] { 0, 1 };
Check(DiplomacyMemoryMaterial.Relevant("commerce", "trade", "exact promise", Embed), "semantic relevance without literal overlap");
Check(!DiplomacyMemoryMaterial.Relevant("commerce", "sports", "", Embed), "semantic threshold rejects unrelated candidate");
Check(DiplomacyMemoryMaterial.IsRulerSnapshotCurrent("old", "old", true), "same living ruler");
Check(!DiplomacyMemoryMaterial.IsRulerSnapshotCurrent("old", "new", true), "successor cannot inherit private context");
Check(!DiplomacyMemoryMaterial.IsRulerSnapshotCurrent("old", "old", false), "dead ruler");
Check(DiplomacyMemoryMaterial.IsRulerSnapshotCurrent("", "new", true), "old job without private context");
Console.WriteLine("PASS " + checks + " diplomacy memory checks");
