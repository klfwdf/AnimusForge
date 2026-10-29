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
        Check(!Field(reply[0],"content").Contains("Write all newly generated natural-language content"), "reply adds no fixed language tail");
        Check(Field(reply[6],"content").Contains("recalled history") && Field(reply[6],"content").Contains("recent facts"), "variable recall and fact context follows selected chronological history");
        Check(Field(reply[1],"role")=="user" && Field(reply[1],"content").EndsWith("hello"), "player role");
        Check(Field(reply[2],"role")=="assistant" && Field(reply[2],"content").EndsWith("answer"), "viewer role");
        Check(Field(reply[3],"role")=="user" && Field(reply[3],"content").Contains("Borin"), "overheard is not viewer speech");
        Check(Field(reply[4],"role")=="user" && Field(reply[4],"content").Contains("【过往行为】[AFEF玩家行为补充]"), "player fact scope");
        Check(Field(reply[5],"role")=="user" && Field(reply[5],"content").Contains("[AFEF NPC行为补充]"), "npc fact readback");
        Check(Field(reply[7],"content").Contains("current letter") && Field(reply[7],"content").Contains("delivery fact"), "current letter last");
        var inbound = MainPromptMessageAssemblyOwner.BuildInboundNpcLetterMessages("Alda", "Player", "intent", "rules", "inbound fact", "history", history, "persona", "exclude", "recent", "recipient", "kinship", "location", "date", 220, "私人来信", "custom");
        Check(inbound.Count==reply.Count, "same history cardinality across direction");
        Check(!Field(inbound[0],"content").Contains("Write all newly generated natural-language content"), "inbound letters add no fixed language tail");
        for(int i=1;i<6;i++) Check(Field(inbound[i],"role")==Field(reply[i],"role") && Field(inbound[i],"content")==Field(reply[i],"content"), "history direction parity " + i);
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
            Check(expected.SequenceEqual(new RequestRecallBudget(actual).FinishSections()), "legacy Scene message grouping " + layout);
            Check(expected.SequenceEqual(new RequestRecallBudget(SceneCallSites.Read(layout)).FinishSections()), "actual production Scene callsite grouping " + layout);
            var empty = MainPromptMessageAssemblyOwner.BuildSceneSingleSpeakerPrefixSections(layout, "", "", "", "", "", "", "", "", "", "");
            Check(empty.All(string.IsNullOrEmpty), "prepared-empty Scene grouping " + layout);
        }
        Check(MainPromptMessageAssemblyOwner.BuildSceneReactionSystemPrompt("identity", "rule", "custom-role")=="identity\n\nrule\n\ncustom-role", "reaction system order");
        try { MainPromptMessageAssemblyOwner.BuildSceneSingleSpeakerPrefixSections((MainPromptMessageAssemblyOwner.SceneSingleSpeakerLayout)99, "", "", "", "", "", "", "", "", "", ""); throw new Exception("unknown layout accepted"); }
        catch (ArgumentOutOfRangeException) { Check(true, "unknown layout rejected"); }

        string evidence = "{\"source\":\"doc_1\",\"Body\":\"A complete promise with unchanged conditions.\"}";
        string legacy = "I have officially published a diplomatic document, source=doc_1, date=1, publishing kingdom=A, title=T. Original text: A complete promise with unchanged conditions.. This entry only confirms the document was published or received; it does not prove the proposal was accepted or diplomatic actions executed.";
        var copies = new[] { new ConversationMessage { Role="system", Content=legacy }, new ConversationMessage {Role="user", Content=legacy} };
        var budget = new RequestRecallBudget(new[] { "RULES\n" + RequestRecallBudget.WrapDiplomacy(evidence), RequestRecallBudget.WrapPersonal("OLD PARAGRAPH") }, 3000);
        var picked = budget.SelectMessages(copies, x => x.Content.Length);
        Check(picked.Count==1 && picked[0].Role=="user", "only exact legacy producer duplicate is removed; actual player quotation survives");
        Check(budget.FinishSections()[0].StartsWith("RULES\n") && !string.Join("",budget.FinishSections()).Contains("<af-recall"), "transport markers removed and business rules preserved");
        var pair = new[] { new ConversationMessage {Role="user",Content=new string('q',700)}, new ConversationMessage {Role="assistant",Content="yes"} };
        budget = new RequestRecallBudget(new[] { RequestRecallBudget.WrapDiplomacy(evidence) }, 400);
        Check(budget.SelectMessages(pair, x=>x.Content.Length).Count==0, "oversized question and assent dropped together without sentence truncation");
        pair[1].PromptFactScopeCaptured=true; pair[1].PromptIsCurrentFact=true;
        Check(budget.SelectMessages(pair,x=>x.Content.Length,x=>x.PromptIsCurrentFact).Contains(pair[1]), "current actual fact never discarded as historical recall");
        var largeHistory = Enumerable.Range(0,80).Select(i=> new ConversationMessage {Role="system",Content="history "+i+new string('x',400),GameDayIndex=i,GameHour=1,GameDate="day",Scene="court",SpeakerName="Alda"}).ToList();
        string extra = "KEEP_WORLD_RULES\n"+RequestRecallBudget.WrapDiplomacy(evidence);
        foreach (bool inboundMode in new[]{false,true})
        {
            var assembled = inboundMode
                ? MainPromptMessageAssemblyOwner.BuildInboundNpcLetterMessages("Alda","Player","CURRENT_REQUEST",extra,"CURRENT_FACT",new string('z',18000),largeHistory,"","","","","","","",100,"","PERSONAL_RULE")
                : MainPromptMessageAssemblyOwner.BuildCourierReplyMessages("Alda","Player","CURRENT_REQUEST",extra,"CURRENT_FACT",new string('z',18000),largeHistory,"","","","","","","","PERSONAL_RULE");
            string all = string.Join("\n",assembled.Select(x=>Field(x,"content")));
            Check(!all.Contains("<af-recall") && !all.Contains("</af-recall>"), "actual courier output strips markers both directions");
            Check(all.Contains("KEEP_WORLD_RULES") && all.Contains("CURRENT_REQUEST") && all.Contains("CURRENT_FACT") && all.Contains("PERSONAL_RULE"), "courier current request/world/actual facts and custom rules protected");
            Check(!all.Contains(new string('z',18000)) && assembled.Count < largeHistory.Count, "actual courier shares budget across diplomatic and personal raw/compressed history");
            Check(all.Length < RequestRecallBudget.MaximumChars+2000, "rendered metadata included within shared recall budget plus independent current context");
            Check(largeHistory.Count==80 && largeHistory[0].Content.EndsWith(new string('x',400)), "budget selection does not mutate personal archive");
        }
        Console.WriteLine("PASS " + checks + " detached main message assembly checks");
    }
}
