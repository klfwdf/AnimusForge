using System;
using System.Reflection;

// Exercises the production history formatter and director-only dialogue block.
// No host session, renderer or model request is needed.
public static class DialogueHistoryAudit
{
    private const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private static int checks;
    private static Type lineType;
    private static Type extractor;

    private static void Check(bool pass, string name)
    {
        if (!pass) throw new Exception("FAIL " + name);
        checks++;
        Console.WriteLine("PASS " + name);
    }

    private static object Line(string kind, string text, string speaker)
    {
        object line = Activator.CreateInstance(lineType, true);
        lineType.GetField("Kind").SetValue(line, kind);
        lineType.GetField("Text").SetValue(line, text);
        lineType.GetField("Speaker").SetValue(line, speaker);
        return line;
    }

    private static string History(int limit, params object[] entries)
    {
        Array lines = Array.CreateInstance(lineType, entries.Length);
        for (int i = 0; i < entries.Length; i++) lines.SetValue(entries[i], i);
        return (string)extractor.GetMethod("BuildRecentDialogueHistory", Static).Invoke(null, new object[] { lines, limit });
    }

    public static void Run(string dllPath)
    {
        checks = 0;
        Assembly assembly = Assembly.LoadFrom(dllPath);
        extractor = assembly.GetType("AnimusForge.Illustrator.Context.ConversationContextExtractor", true);
        lineType = extractor.GetNestedType("NativeDialogueLine", BindingFlags.NonPublic);
        int limit = (int)extractor.GetField("RecentDialogueLimit", Static).GetRawConstantValue();
        Check(limit == 2, "manual and automatic generation share the two-round limit");

        string refusal = History(limit, Line("player", "应丢弃", ""), Line("npc", "应丢弃回复", "艾拉"),
            Line("player", "旧命令", ""), Line("npc", "旧回答", "艾拉"),
            Line("player", "跪下", ""), Line("npc", "我不跪。", "艾拉"));
        Check(refusal.Contains("最近至多2轮") && refusal.Contains("旧命令") && refusal.Contains("旧回答") && !refusal.Contains("应丢弃"), "latest two player-led rounds survive");
        Check(refusal.IndexOf("玩家：跪下", StringComparison.Ordinal) < refusal.IndexOf("艾拉：我不跪。", StringComparison.Ordinal) && refusal.Contains("艾拉：我不跪。"), "command and refusal retain speaker and chronological order");

        string replies = History(limit, Line("player", "旧玩家发言", ""), Line("npc", "第一段回答", "艾拉"),
            Line("npc", "第二段回答", "艾拉"), Line("npc", "最后一段回答", "艾拉"));
        Check(replies.Contains("旧玩家发言") && replies.Contains("第一段回答") && replies.Contains("第二段回答") && replies.Contains("最后一段回答"), "segmented NPC replies stay with their player round");
        string pending = History(limit, Line("player", "丢弃轮", ""), Line("npc", "答复一", ""),
            Line("player", "保留轮", ""), Line("npc", "答复二", ""), Line("player", "尚待回答", ""));
        Check(!pending.Contains("丢弃轮") && pending.Contains("保留轮") && pending.Contains("尚待回答"), "unfinished current round is preserved");

        string filtered = History(limit, Line("player", "玩家记录", ""), Line("system", "系统记录", ""),
            Line("npc", "对方记录", ""), null, Line("npc", "  ", ""), Line("action", "动作标签", ""));
        Check(filtered.Contains("玩家：玩家记录") && filtered.Contains("对方：对方记录") && !filtered.Contains("系统记录") && !filtered.Contains("动作标签"), "empty and non-dialogue entries do not consume the dialogue limit");

        string longReply = new string('甲', 300) + "我拒绝，仍然站着。";
        Check(History(limit, Line("player", "跪下", ""), Line("npc", longReply, "艾拉")).Contains(longReply), "late refusal is not cut off at the former 240-character boundary");
        Check(History(limit, Line("NPC", "单条开场白", "艾拉")).Contains("共1条发言"), "a short session does not invent a second entry");
        Check(History(limit) == "" && History(0, Line("player", "内容", "")) == "", "empty input and disabled limit return no history");

        Type contextType = assembly.GetType("AnimusForge.Illustrator.Context.ConversationVisualContext", true);
        object context = Activator.CreateInstance(contextType);
        contextType.GetProperty("RecentDialogueHistory").SetValue(context, refusal, null);
        contextType.GetProperty("DialogueSentence").SetValue(context, "STALE_CURRENT_SENTENCE", null);
        string block = (string)contextType.GetMethod("BuildDialogueBlock").Invoke(context, null);
        Check(block.Contains(refusal) && !block.Contains("STALE_CURRENT_SENTENCE"), "director block does not add a third stale focus sentence");
        Check(block.Contains("拒绝") && block.Contains("不能"), "director still receives action-versus-refusal constraints");
        contextType.GetProperty("RecentDialogueHistory").SetValue(context, "", null);
        Check(((string)contextType.GetMethod("BuildDialogueBlock").Invoke(context, null)).Contains("STALE_CURRENT_SENTENCE"), "missing host history retains current-sentence fallback");

        Type plan = assembly.GetType("AnimusForge.Illustrator.Core.IllustrationPromptPlan", true);
        foreach (string mode in new[] { "当前会话最近两轮对话联动的场景插画", "最近2条对话联动的场景插画", "最近一轮对话联动的场景插画", "最近三轮对话联动的场景插画" })
        {
            object value = Activator.CreateInstance(plan, new object[] { mode, "", "", "" });
            Check((bool)plan.GetProperty("IsConversation", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(value, null), "conversation-specific routing recognizes mode: " + mode);
        }
        Console.WriteLine("DialogueHistoryAudit: " + checks + " PASS, 0 FAIL.");
    }
}
