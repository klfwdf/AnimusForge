using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AnimusForge;
internal static class MessageAssemblyChecks
{
    static int checks;
    static void Check(bool value, string reason) { checks++; if (!value) throw new Exception("FAIL " + reason); }
    static string Field(object message, string name) => (string)message.GetType().GetProperty(name).GetValue(message);
    static int Main()
    {
        try { Run(); return 0; }
        catch (Exception error) { Console.Error.WriteLine(error.Message); return 1; }
    }
    static void Run()
    {
        var history = new[] {
            new ConversationMessage {Role="user", Content="Player: hello", SpeakerName="Player"},
            new ConversationMessage {Role="assistant", Content="Alda: answer", SpeakerName="Alda"},
            new ConversationMessage {Role="assistant", Content="Borin: overheard", SpeakerName="Borin"},
            new ConversationMessage {Role="system", Content="【当下行为】[AFEF玩家行为补充] transfer confirmed", SpeakerName="AFEF"},
            new ConversationMessage {Role="system", Content="[AFEF NPC行为补充] reply delivered", SpeakerName="AFEF"},
            null, new ConversationMessage {Content=" "}
        };
        var reply = MainPromptMessageAssemblyOwner.BuildCourierReplyMessages("Alda", "Player", "current letter", "rules", "delivery fact", "recalled history", history, "persona", "exclusion", "recent facts", "identity", "relation", "location", "date", "custom rule");
        Check(reply.Count==8, "reply message count");
        Check(Field(reply[0],"role")=="system", "system first");
        Check(Field(reply[0],"content").StartsWith("custom rule\n"), "custom rule order");
        Check(Field(reply[0],"content").Contains("persona") && Field(reply[0],"content").Contains("exclusion"), "system sections");
        Check(Field(reply[1],"content").Contains("recalled history") && Field(reply[1],"content").Contains("recent facts"), "history and fact contexts");
        Check(Field(reply[2],"role")=="user" && Field(reply[2],"content").EndsWith("hello"), "player role");
        Check(Field(reply[3],"role")=="assistant" && Field(reply[3],"content").EndsWith("answer"), "viewer role");
        Check(Field(reply[4],"role")=="user" && Field(reply[4],"content").Contains("Borin"), "overheard is not viewer speech");
        Check(Field(reply[5],"role")=="user" && Field(reply[5],"content").Contains("【过往行为】[AFEF玩家行为补充]"), "player fact scope");
        Check(Field(reply[6],"role")=="user" && Field(reply[6],"content").Contains("[AFEF NPC行为补充]"), "npc fact readback");
        Check(Field(reply[7],"content").Contains("current letter") && Field(reply[7],"content").Contains("delivery fact"), "current letter last");
        var inbound = MainPromptMessageAssemblyOwner.BuildInboundNpcLetterMessages("Alda", "Player", "intent", "rules", "inbound fact", "history", history, "persona", "exclude", "recent", "recipient", "kinship", "location", "date", 220, "私人来信", "custom");
        Check(inbound.Count==reply.Count, "same history cardinality across direction");
        for(int i=2;i<7;i++) Check(Field(inbound[i],"role")==Field(reply[i],"role") && Field(inbound[i],"content")==Field(reply[i],"content"), "history direction parity " + i);
        Check(Field(inbound[7],"content").Contains("intent") && Field(inbound[7],"content").Contains("inbound fact"), "inbound current suffix");
        Check(!MainPromptMessageAssemblyOwner.TryConvertCourierMemoryMessageToChatMessage(null,"Alda","Player",out _), "null history rejected");
        Check(!MainPromptMessageAssemblyOwner.TryConvertCourierMemoryMessageToChatMessage(new ConversationMessage {Content=" "},"Alda","Player",out _), "empty history rejected");
        Check(MainPromptMessageAssemblyOwner.BuildSceneCompositeUserBlock(" history ", null, " ", " rules ")=="history\n\nrules", "shared block ordering");
        foreach (var layout in Enum.GetValues<MainPromptMessageAssemblyOwner.SceneSingleSpeakerLayout>())
        {
            string Join(params string[] values) => string.Join("\n\n", values.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()));
            string[] expected = layout switch {
                MainPromptMessageAssemblyOwner.SceneSingleSpeakerLayout.GroupFallback => new[] {"private", "persisted", "runtime", "local", "fact", "trust", "misc", "patience", Join("knowledge", "rules")},
                MainPromptMessageAssemblyOwner.SceneSingleSpeakerLayout.Passive => new[] {"private", "persisted", "runtime", "local", "trust", "misc", "patience", Join("knowledge", "rules")},
                MainPromptMessageAssemblyOwner.SceneSingleSpeakerLayout.GroupTurn => new[] {"private", "persisted", Join("runtime", "local", "fact", "trust", "misc", "patience"), Join("knowledge", "rules")},
                MainPromptMessageAssemblyOwner.SceneSingleSpeakerLayout.ImmediateReaction => new[] {"private", "persisted", Join("runtime", "knowledge", Join(Join("local", "trust", "misc"), "fact"))},
                _ => new[] {"private", "persisted", Join("runtime", "knowledge"), Join(Join("local", "trust", "misc"), "fact")}
            };
            var actual = MainPromptMessageAssemblyOwner.BuildSceneSingleSpeakerPrefixSections(layout, "private", "persisted", "runtime", "local", "fact", "trust", "misc", "patience", "knowledge", "rules");
            Check(expected.SequenceEqual(actual), "legacy Scene message grouping " + layout);
            Check(expected.SequenceEqual(SceneCallSites.Read(layout)), "actual production Scene callsite grouping " + layout);
            var empty = MainPromptMessageAssemblyOwner.BuildSceneSingleSpeakerPrefixSections(layout, "", "", "", "", "", "", "", "", "", "");
            Check(empty.All(string.IsNullOrEmpty), "prepared-empty Scene grouping " + layout);
        }
        Check(MainPromptMessageAssemblyOwner.BuildSceneReactionSystemPrompt("identity", "rule", "custom-role")=="identity\n\nrule\n\ncustom-role", "reaction system order");
        try { MainPromptMessageAssemblyOwner.BuildSceneSingleSpeakerPrefixSections((MainPromptMessageAssemblyOwner.SceneSingleSpeakerLayout)99, "", "", "", "", "", "", "", "", "", ""); throw new Exception("unknown layout accepted"); }
        catch (ArgumentOutOfRangeException) { Check(true, "unknown layout rejected"); }
        Console.WriteLine("PASS " + checks + " detached main message assembly checks");
    }
}
