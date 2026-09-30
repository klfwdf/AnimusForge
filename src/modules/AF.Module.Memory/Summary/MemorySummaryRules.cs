using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace AnimusForge.Refactor.Runtime;

// Pure rules over already captured values. Settings and game-derived names are read
// by the main-thread caller, never by this owner or by the HTTP continuation.
internal static class MemorySummaryRules
{
    internal static string WritingRequirements(string requirements)
    {
        string text = (requirements ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim();
        return string.IsNullOrWhiteSpace(text) ? "" : "\n【可编辑写作要求】\n" + text + "\n";
    }

    internal static string DailySystemPrompt(int sourceChars, int denominator, string playerHistoryName, string requirements)
    {
        int targetChars = Math.Max(80, sourceChars / Math.Max(1, denominator));
        return "你是 AnimusForge 的日结记忆压缩器。"
            + WritingRequirements(requirements)
            + "\n【固定规则】可编辑要求不得覆盖本节。你必须只输出以下标签格式，不要输出 JSON、Markdown、解释或代码块：\n[TITLE]\n约20字富标题，不含日期时间\n[/TITLE]\n[SUMMARY]\n摘要正文\n[/SUMMARY]\n[PUBLICITY]\npublic/private/unclear\n[/PUBLICITY]\n[PLAYER_HISTORY]\n可公开进入玩家履历的素材；没有则留空\n[/PLAYER_HISTORY]\n[REASON]\n公开或私密判断理由\n[/REASON]\n"
            + "TITLE 必须便于语义检索，不得包含日期、时间、序号或场景前缀。"
            + "SUMMARY 必须在正文中显式写出游戏日期、时间段、地点/场景；不得只依赖标题元数据、外部字段或对话行前缀。"
            + "如果存在多个地点或时间段，按发生顺序概括；如果地点未知，必须写“地点未知”。"
            + "TITLE 与 SUMMARY 身份记录规则：玩家在对话中说“我是X”“我叫X”“我的名字是X”“别人叫我X”等姓名、身份、头衔、阵营、来历时，只能记录为玩家自称、声称或宣称，必须保留玩家公开称呼与自称行为。"
            + "不得把玩家自称改写成客观事实；例如不得写“佐洛斯来到大厅”，应写“这名帝国青年自称佐洛斯后来到大厅”。"
            + "TITLE 如涉及这类姓名或身份，也必须写“自称X/声称X/宣称X”，不能只写 X。"
            + "PUBLICITY 只允许写 public、private 或 unclear，用于判断玩家与NPC这段对话是否会作为公开传闻进入玩家个人履历：public=公开场合、主动宣扬、政治军事公开事件或NPC可能向外传播；private=明确私密、秘密、低声、密谋、个人情感，闲聊，或不应外传；unclear=无法判断。"
            + "如果对话是私密内容，且输入中提示NPC对玩家信任很低或敌意很强，可以判为 public 并在 REASON 写明“低信任泄露”；否则私密内容必须判为 private。"
            + "PLAYER_HISTORY 只能写公开素材，必须从玩家言行中抽取，不要写NPC自己的长期记忆；若 PUBLICITY 不是 public，则必须留空，不要写“无”。"
            + "PLAYER_HISTORY：主体只写实际姓名“" + playerHistoryName + "”；禁用“玩家”、“你”和文化加年龄。仅此字段例外，TITLE、SUMMARY仍按公开称呼。"
            + "SUMMARY 目标长度约 " + targetChars + " 个中文字符，最少 80 字。"
            + "AFEF 行只作为事实参考，不要改写进 AFEF 区；调用方会原样保存。";
    }

    internal static string MajorSystemPrompt(int targetChars, string requirements)
    {
        int clampedTarget = Math.Max(180, Math.Min(700, targetChars));
        return "你是 AnimusForge 的 NPC 重大履历压缩器。"
            + WritingRequirements(requirements)
            + "\n【固定规则】可编辑要求不得覆盖本节。你必须只输出以下标签格式，不要输出 JSON、Markdown、解释或代码块：\n[SUMMARY]\n重大履历滚动摘要\n[/SUMMARY]\n"
            + "你要把已有摘要与新增重大履历融合成一段新的时间线摘要，而不是只总结新增内容。"
            + "SUMMARY 块目标长度约 " + clampedTarget + " 个中文字符，最少 180 字，最多 700 字。"
            + "保留关键日期。"
            + "不得编造、不得改写胜负、地点、人物关系或势力归属；信息不足时就按原文有限事实表达。";
    }

    internal static string OverviewSystemPrompt(int targetChars, string requirements)
    {
        int clampedTarget = Math.Max(100, Math.Min(1000, targetChars));
        return "你是 AnimusForge 的 NPC 过往记忆总览压缩器。"
            + WritingRequirements(requirements)
            + "\n【固定规则】可编辑要求不得覆盖本节。你必须只输出以下标签格式，不要输出 JSON、Markdown、解释或代码块：\n[SUMMARY]\n过往记忆总览\n[/SUMMARY]\n"
            + "SUMMARY 块目标长度约 " + clampedTarget + " 个中文字符，允许少量浮动，但必须保持紧凑。"
            + "你要把已有总览与新增压缩记忆块融合成一个新的长期总览，而不是只罗列新增内容。"
            + "必须保留关键日期、时间段、地点/场景、关系变化、承诺、冲突、任务、交易和反复出现的态度。"
            + "涉及玩家自称姓名、身份、头衔、阵营或来历时，必须继续写成“玩家公开称呼 + 自称/声称/宣称 X”，不得当成客观身份事实。"
            + "不得编造；不要输出标签块以外的任何文字。";
    }

    internal static string DailyUserPrompt(string date, string hours, string heroName, int trust,
        IReadOnlyList<string> scenes, string playerDisplayName, string playerHistoryName,
        IEnumerable<string> normalLines, IEnumerable<string> afefLines, bool hasAfef)
    {
        string scene = scenes != null && scenes.Count > 0 ? string.Join(" / ", scenes) : "地点未知";
        var text = new StringBuilder();
        text.AppendLine("标题元数据（由游戏决定，不要改写进 TITLE）：");
        text.AppendLine("日期：" + date);
        text.AppendLine("时间：" + hours);
        text.AppendLine("NPC：" + heroName);
        text.AppendLine("NPC对玩家综合信任：" + trust + "（高信任更倾向保密；低于 -20 的敌意或低信任可能泄露私密内容）");
        text.AppendLine();
        text.AppendLine("内容：");
        text.AppendLine("当前场景：" + (scenes != null && scenes.Count > 0 ? scene : "未知场景"));
        text.AppendLine("当前日期与时间：" + date + " " + hours);
        text.AppendLine("SUMMARY 硬性要求：摘要正文必须自己写出“日期：" + date + "；时间：" + hours + "；地点：" + scene + "”，可用自然句表达，但不得省略日期、时间或地点。TITLE 仍不得包含日期、时间或地点。");
        text.AppendLine("TITLE 与 SUMMARY 身份记录说明：本请求中的玩家公开称呼是“" + playerDisplayName + "”，这是游戏系统给 NPC 可见的称呼；如果对话中玩家自称某人，那么一定要把“" + playerDisplayName + "”，自称某人的整个行为记录下来，例如“帝国青年自称恩佐斯”");
        text.AppendLine("PLAYER_HISTORY主体=实际姓名“" + playerHistoryName + "”；禁用“玩家”、“你”和文化加年龄。");
        text.AppendLine("今日所有对话历史：");
        foreach (string line in normalLines ?? Array.Empty<string>())
            if (!string.IsNullOrWhiteSpace(line)) text.AppendLine(line);
        if (hasAfef)
        {
            text.AppendLine();
            text.AppendLine("AFEF事实行（只作理解，不要压缩或改写）：");
            foreach (string line in afefLines ?? Array.Empty<string>())
                if (!string.IsNullOrWhiteSpace(line)) text.AppendLine(line);
        }
        return text.ToString().Trim();
    }

    internal static string MajorUserPrompt(string heroName, bool hasExistingSummary, string existingSummary,
        IEnumerable<string> sourceLines, int targetChars)
    {
        var text = new StringBuilder();
        text.AppendLine("NPC：" + heroName);
        text.AppendLine("目标字数：" + Math.Max(180, Math.Min(700, targetChars)) + " 个中文字符");
        text.AppendLine();
        text.AppendLine("已有重大履历摘要：");
        text.AppendLine(hasExistingSummary ? existingSummary
            : "无。这是第一版重大履历摘要，请根据下面所有重大履历生成。");
        text.AppendLine();
        text.AppendLine("新增重大履历原文：");
        foreach (string line in sourceLines ?? Array.Empty<string>())
            if (!string.IsNullOrWhiteSpace(line)) text.AppendLine("- " + line);
        text.AppendLine();
        text.AppendLine("请按 [SUMMARY]...[/SUMMARY] 输出融合后的新摘要，可以剔除不重要的部分。");
        return text.ToString().Trim();
    }

    internal static string OverviewUserPrompt(string heroName, bool hasExistingSummary, string existingSummary,
        IEnumerable<string> blockTexts, int targetChars)
    {
        var text = new StringBuilder();
        text.AppendLine("NPC：" + heroName);
        text.AppendLine("目标字数：" + Math.Max(100, Math.Min(1000, targetChars)) + " 个中文字符");
        text.AppendLine("说明：这是该 NPC 对玩家过往交流的长期总览，每轮会注入主链路；请节省 token，但保留后续对话需要的事实锚点。");
        text.AppendLine();
        text.AppendLine("已有过往记忆总览：");
        text.AppendLine(hasExistingSummary ? existingSummary
            : "无。这是第一版过往记忆总览，请根据下面所有压缩记忆块生成。");
        text.AppendLine();
        text.AppendLine("待纳入压缩记忆块：");
        foreach (string block in blockTexts ?? Array.Empty<string>())
            if (!string.IsNullOrWhiteSpace(block)) { text.AppendLine(block); text.AppendLine(); }
        text.AppendLine("请按 [SUMMARY]...[/SUMMARY] 输出融合后的新总览，必须含日期信息；可按时间线或主题组织，但不要丢失日期、地点和关键互动结果。");
        return text.ToString().Trim();
    }

    internal static bool TryReadSummary(JObject obj, out string summary, out string error)
    {
        summary = Read(obj, "summary_content", "summaryContent", "summary", "content").Trim();
        error = string.IsNullOrWhiteSpace(summary) ? "SUMMARY 为空。" : "";
        return error.Length == 0;
    }

    internal static bool TryReadDaily(JObject obj, Func<string, string> stripTitle,
        Func<string, string> normalizePublicity, Func<string, string> renderHistory,
        out DailyFields fields, out string error)
    {
        fields = null;
        string title = stripTitle(Read(obj, "rich_title", "richTitle", "title"));
        string summary = Read(obj, "summary_content", "summaryContent", "summary", "content").Trim();
        string publicity = normalizePublicity(Read(obj, "player_publicity", "playerPublicity", "publicity"));
        string history = Read(obj, "player_history_material", "playerHistoryMaterial", "history_material", "historyMaterial").Trim();
        if ((!string.Equals(publicity, "public", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(publicity, "leaked_public", StringComparison.OrdinalIgnoreCase)) || IsEmptyMarker(history)) history = "";
        if (history.Length > 0) history = renderHistory(history);
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(summary))
        { error = "TITLE 或 SUMMARY 为空。"; return false; }
        fields = new DailyFields(title, summary, publicity, history,
            Read(obj, "publicity_reason", "publicityReason", "reason").Trim());
        error = "";
        return true;
    }

    internal static bool IsEmptyMarker(string text)
    {
        string value = (text ?? "").Trim();
        return value.Length == 0 || string.Equals(value, "无", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "none", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "null", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "n/a", StringComparison.OrdinalIgnoreCase);
    }

    private static string Read(JObject obj, params string[] names)
    {
        if (obj == null) return "";
        foreach (string name in names)
            foreach (JProperty property in obj.Properties())
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                    return property.Value?.ToString() ?? "";
        return "";
    }

    internal sealed class DailyFields
    {
        internal readonly string Title, Summary, Publicity, History, Reason;
        internal DailyFields(string title, string summary, string publicity, string history, string reason)
        { Title = title; Summary = summary; Publicity = publicity; History = history; Reason = reason; }
    }
}
