using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AnimusForge.Refactor.Runtime;
using Newtonsoft.Json.Linq;

internal static class Program
{
    private static int _checks;
    private static void Check(bool value, string name)
    { _checks++; if (!value) throw new Exception("FAIL " + name); }

    private static async Task Main()
    {
        Check(MemorySummaryRules.WritingRequirements("\r\na\rb\r\n") == "\n【可编辑写作要求】\na\nb\n", "requirements-normalize");
        Check(MemorySummaryRules.DailySystemPrompt(200, 2, "张三", "x").Contains("SUMMARY 目标长度约 100"), "daily-target");
        Check(MemorySummaryRules.DailySystemPrompt(0, 0, "张三", "").Contains("SUMMARY 目标长度约 80"), "daily-minimum");
        Check(MemorySummaryRules.MajorSystemPrompt(9999, "").Contains("目标长度约 700"), "major-clamp");
        Check(MemorySummaryRules.OverviewSystemPrompt(1, "").Contains("目标长度约 100"), "overview-clamp");
        Check(MemorySummaryRules.DailySystemPrompt(100, 1, "张三", "").Contains("AFEF 行只作为事实参考"), "afef-rule");
        string daily = MemorySummaryRules.DailyUserPrompt("第5日", "09:00-10:00", "NPC", -21,
            new[] { "城堡" }, "帝国青年", "张三", new[] { "玩家：你好" }, new[] { "AFEF 原文" }, true);
        Check(daily.Contains("地点：城堡") && daily.IndexOf("玩家：你好", StringComparison.Ordinal) < daily.IndexOf("AFEF 原文", StringComparison.Ordinal), "daily-user-order");
        Check(MemorySummaryRules.DailyUserPrompt("第5日", "09:00", "NPC", 0, Array.Empty<string>(), "玩家", "张三",
            Array.Empty<string>(), Array.Empty<string>(), false).Contains("地点：地点未知"), "daily-unknown-scene");
        string major = MemorySummaryRules.MajorUserPrompt("NPC", true, "旧摘要", new[] { "事件甲", "事件乙" }, 300);
        Check(major.IndexOf("- 事件甲", StringComparison.Ordinal) < major.IndexOf("- 事件乙", StringComparison.Ordinal), "major-source-order");
        Check(!MemorySummaryRules.MajorUserPrompt("NPC", true, "", Array.Empty<string>(), 300).Contains("无。这是第一版"), "stripped-existing-summary-remains-empty");
        string overview = MemorySummaryRules.OverviewUserPrompt("NPC", true, "旧总览", new[] { "块甲", "块乙" }, 300);
        Check(overview.IndexOf("块甲", StringComparison.Ordinal) < overview.IndexOf("块乙", StringComparison.Ordinal), "overview-block-order");
        Check(MemorySummaryRules.TryReadSummary(JObject.Parse("{\"summaryContent\":\"内容\"}"), out var summary, out _) && summary == "内容", "summary-alias");
        Check(!MemorySummaryRules.TryReadSummary(JObject.Parse("{\"summary\":\"  \"}"), out _, out var emptyError) && emptyError == "SUMMARY 为空。", "summary-empty");
        var payload = JObject.Parse("{\"richTitle\":\" 标题 \",\"summaryContent\":\" 正文 \",\"publicity\":\"private\",\"historyMaterial\":\"秘密\",\"reason\":\"理由\"}");
        Check(MemorySummaryRules.TryReadDaily(payload, s => s.Trim(), s => s, s => "render:" + s, out var privateFields, out _)
            && privateFields.History == "" && privateFields.Reason == "理由", "private-history-gate");
        payload["publicity"] = "public";
        Check(MemorySummaryRules.TryReadDaily(payload, s => s.Trim(), s => s, s => "render:" + s, out var publicFields, out _)
            && publicFields.History == "render:秘密" && publicFields.Title == "标题", "public-history-render");
        payload["historyMaterial"] = "无";
        Check(MemorySummaryRules.TryReadDaily(payload, s => s.Trim(), s => s, s => "render:" + s, out var noHistory, out _)
            && noHistory.History == "", "empty-history-marker");
        var events = new List<string>();
        int attempts = 0;
        var outcome = await MemorySummaryAttemptRunner.RunAsync(3,
            () => { events.Add("validate"); return Task.FromResult(true); },
            () => { events.Add("owner"); return true; },
            async () => { await Task.Yield(); events.Add("attempt"); attempts++; return new MemorySummaryAttemptRunner.Receipt(true, attempts == 2, 0); },
            ms => { events.Add("delay:" + ms); return Task.CompletedTask; });
        Check(outcome == MemorySummaryAttemptRunner.Outcome.Completed && attempts == 2
            && string.Join(",", events) == "owner,attempt,delay:1000,validate,owner,attempt", "forced-async-retry-order");
        attempts = 0;
        outcome = await MemorySummaryAttemptRunner.RunAsync(3,
            () => Task.FromResult(false), () => true,
            () => { attempts++; return Task.FromResult(new MemorySummaryAttemptRunner.Receipt(true, false, null)); },
            _ => Task.CompletedTask);
        Check(outcome == MemorySummaryAttemptRunner.Outcome.Obsolete && attempts == 1, "source-change-stops-retry");
        bool ownerCurrent = true;
        outcome = await MemorySummaryAttemptRunner.RunAsync(3,
            () => Task.FromResult(true), () => ownerCurrent,
            () => { ownerCurrent = false; return Task.FromResult(new MemorySummaryAttemptRunner.Receipt(true, false, null)); },
            _ => Task.CompletedTask);
        Check(outcome == MemorySummaryAttemptRunner.Outcome.Obsolete, "owner-replacement-stops-retry");
        outcome = await MemorySummaryAttemptRunner.RunAsync(1,
            () => Task.FromResult(true), () => true,
            () => Task.FromResult(new MemorySummaryAttemptRunner.Receipt(false, true, null)), _ => Task.CompletedTask);
        Check(outcome == MemorySummaryAttemptRunner.Outcome.Obsolete, "late-result-rejected");
        int writes = 0, failures = 0, checks = 0;
        bool applied = MemorySummaryAttemptRunner.Accept(true, true, () => { checks++; return true; }, true,
            () => { writes++; return true; }, () => failures++);
        Check(!applied && checks == 0 && writes == 0 && failures == 0, "obsolete-no-source-scan-or-write");
        applied = MemorySummaryAttemptRunner.Accept(true, false, () => { checks++; return true; }, false,
            () => { writes++; return true; }, () => failures++);
        Check(!applied && checks == 1 && writes == 0 && failures == 1, "failure-once");
        applied = MemorySummaryAttemptRunner.Accept(true, false, () => { checks++; return false; }, true,
            () => { writes++; return true; }, () => failures++);
        Check(!applied && checks == 2 && writes == 0, "changed-source-no-write");
        applied = MemorySummaryAttemptRunner.Accept(true, false, () => { checks++; return true; }, true,
            () => { writes++; return true; }, () => failures++);
        Check(applied && writes == 1, "valid-write-once");
        bool sourceCurrent = true;
        bool first = MemorySummaryAttemptRunner.Accept(true, false, () => sourceCurrent, true,
            () => { sourceCurrent = false; writes++; return true; }, () => failures++);
        bool second = MemorySummaryAttemptRunner.Accept(true, false, () => sourceCurrent, true,
            () => { writes++; return true; }, () => failures++);
        Check(first && !second && writes == 2, "duplicate-receipt-source-retired");
        Console.WriteLine("J17_SUMMARY_OWNER_PASS " + _checks);
    }
}
