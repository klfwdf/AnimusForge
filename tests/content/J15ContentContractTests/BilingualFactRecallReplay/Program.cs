using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AnimusForge;

internal static class Program
{
    internal static string PackageRoot;
    private static int checks;
    private static void Check(bool condition, string label)
    { checks++; if (!condition) throw new InvalidOperationException(label); }

    private static void Main(string[] args)
    {
        try { Run(args); }
        catch (Exception error) { Console.Error.WriteLine(error.GetType().Name + ": " + error.Message); Environment.ExitCode = 1; }
    }
    private static void Run(string[] args)
    {
        PackageRoot = args[0];
        bool english = args[1] == "en";
        string playerPrefix = PromptComposerTexts.Raw("afef_player_action");
        string npcPrefix = PromptComposerTexts.Raw("afef_npc_action");
        string[] keys = {
            "memory_courier_player_destroyed_before_delivery", "memory_courier_player_destroyed_after_delivery",
            "memory_courier_player_missing", "memory_courier_npc_destroyed", "memory_courier_npc_missing"
        };
        string[] names = { "Player", "raw {NAME} {0} $1", "原存档人物", "name [literal]" };
        foreach (string name in names)
        foreach (string key in keys)
        foreach (string kind in new[] { PromptComposerTexts.Raw("memory_courier_raw_588"), "user {KIND} $1" })
        {
            string[] values = key == "memory_courier_npc_destroyed" ? new[] { name, "Party {0} $1", kind }
                : key == "memory_courier_npc_missing" ? new[] { name, kind }
                : key == "memory_courier_player_missing" ? Array.Empty<string>()
                : new[] { name, "Party {0} $1" };
            string prefix = key.StartsWith("memory_courier_npc_", StringComparison.Ordinal) ? npcPrefix : playerPrefix;
            string rawBody = string.Format(PromptComposerTexts.Raw(key + "_source"), values.Cast<object>().ToArray());
            string raw = prefix + " " + rawBody;
            string[] translated = (string[])values.Clone();
            int kindIndex = key == "memory_courier_npc_destroyed" ? 2 : key == "memory_courier_npc_missing" ? 1 : -1;
            if (kindIndex >= 0 && translated[kindIndex] == PromptComposerTexts.Raw("memory_courier_raw_588"))
                translated[kindIndex] = PromptComposerTexts.Get("memory_courier_value_588");
            string expected = prefix + " " + string.Format(PromptComposerTexts.Get(key), translated.Cast<object>().ToArray());
            Check(MemorySystemFactProjection.ProjectNpcFact(raw) == expected, "actual terminal template projection: " + key);
            Check(english ? expected != raw : expected == raw, "fixed package language: " + key);
            Check(MemorySystemFactProjection.ProjectNpcFact(expected) == expected, "idempotent terminal projection");
            foreach (string metadata in new[] { "[2026-10-12 hour 8 | Town {NAME} $1 | AFEF] ", "[冬季8日 8时｜Town {NAME} $1｜AFEF] " })
            foreach (string scope in new[] { "", MemoryTextResources.Raw("MEM_Raw_032"), MemoryTextResources.Raw("MEM_Raw_033"), PromptComposerTexts.Raw("history_section_scope_historical") })
            {
                string recorded = scope + metadata + raw;
                string projected = MemorySystemFactProjection.ProjectRecordedFactForPrompt(recorded);
                Check(projected == scope + metadata + expected, "metadata and scope retained literally");
                Check(MemorySystemFactProjection.ProjectRecordedFactForPrompt(projected) == projected, "idempotent recorded fact");
                string past = MemoryRecallContextOwner.FormatPastAfefLineForPrompt(recorded);
                string expectedPast = scope == MemoryTextResources.Raw("MEM_Raw_032") || scope == MemoryTextResources.Raw("MEM_Raw_033")
                    ? projected : MemoryTextResources.Raw("MEM_Raw_032") + projected;
                Check(past == expectedPast, "actual compressed recall consumer projects saved facts");
                Check(recorded == scope + metadata + raw, "saved input immutable");
            }
            string bracketPrefix = key.StartsWith("memory_courier_npc_", StringComparison.Ordinal)
                ? PromptComposerTexts.Raw("afef_npc_action_bracket") : PromptComposerTexts.Raw("afef_player_action_bracket");
            Check(MemorySystemFactProjection.ProjectNpcFact(bracketPrefix + " " + rawBody) == bracketPrefix + " " + expected.Substring(prefix.Length + 1), "legacy bracket envelope retained");
        }

        string delivered = playerPrefix + " Player通过信使转移了 12 第纳尔。";
        var line = new DailyMemoryLine { Text = delivered, IsAfef = true, GameDate = "Old date {DATE}", GameHour = 8, Scene = "Old scene {SCENE} $1", Speaker = "AFEF" };
        string persisted = MemorySummaryApplicationAdapter.BuildDailyMemoryLineForPrompt(line, value => value.Scene);
        Check(persisted.EndsWith(delivered, StringComparison.Ordinal), "daily renderer still preserves original saved body");
        string summaryInput = MemorySystemFactProjection.ProjectRecordedFactForPrompt(persisted);
        Check(summaryInput.EndsWith(MemorySystemFactProjection.ProjectNpcFact(delivered), StringComparison.Ordinal), "daily summary input projection matches direct history");
        Check(line.Text == delivered && line.Scene == "Old scene {SCENE} $1", "daily source DTO unchanged");
        var block = new CompressedMemoryBlock { Id = "old-id", GameDate = "Old date", RichTitle = "Title", Summary = "User text {NAME} $1", AfefLines = new List<string> { persisted } };
        string overview = MemorySummaryApplicationAdapter.BuildMemoryOverviewBlockSourceText(block);
        Check(overview.Contains(summaryInput), "actual overview prompt consumer projects saved metadata facts");
        Check(block.AfefLines.Single() == persisted && block.Summary == "User text {NAME} $1", "overview source block unchanged");
        string before = MemorySystemFactProjection.ProjectBody(string.Format(PromptComposerTexts.Raw(keys[0] + "_source"), "Player", "Enemy"));
        string after = MemorySystemFactProjection.ProjectBody(string.Format(PromptComposerTexts.Raw(keys[1] + "_source"), "Player", "Enemy"));
        Check(before != after, "delivery and return loss are distinct outcomes");
        Check(!english || before.Contains("were not delivered; no fixed assets were transferred"), "pre-delivery failure forbids successful asset transfer");
        Check(!english || after.Contains("completed delivery, but could not bring the reply"), "return failure retains already completed delivery");
        Check(!english || MemorySystemFactProjection.ProjectBody(PromptComposerTexts.Raw(keys[2] + "_source")).Contains("could not be confirmed"), "missing delivery remains uncertain");
        string translatedDelivery = MemorySystemFactProjection.ProjectNpcFact(delivered);
        string capturedDelivery = "[old date | scene {0} | AFEF] " + delivered;
        foreach (string separator in new[] { "\n", "\r\n" })
        {
            string rawLines = delivered + separator + "unknown {NAME} $1" + separator + capturedDelivery + separator;
            string expectedLines = translatedDelivery + separator + "unknown {NAME} $1" + separator
                + "[old date | scene {0} | AFEF] " + translatedDelivery + separator;
            string projectedLines = MemorySystemFactProjection.ProjectRecordedFactForPrompt(rawLines);
            Check(projectedLines == expectedLines, "recorded multi-line facts retain newline bytes");
            Check(MemorySystemFactProjection.ProjectRecordedFactForPrompt(projectedLines) == projectedLines, "idempotent multi-line projection");
            Check(MemorySystemFactProjection.ProjectNpcFact(delivered + separator + "unknown {NAME} $1" + separator)
                == translatedDelivery + separator + "unknown {NAME} $1" + separator, "direct multi-line facts retain newline bytes");
        }
        string mixedLines = delivered + "\r\n" + capturedDelivery + "\nunknown\r\n";
        Check(MemorySystemFactProjection.ProjectRecordedFactForPrompt(mixedLines)
            == translatedDelivery + "\r\n[old date | scene {0} | AFEF] " + translatedDelivery + "\nunknown\r\n", "mixed newline records retained");
        foreach (string raw in new[] { "user " + delivered, "[metadata] " + delivered, "[one | separator] " + delivered,
            "[date | scene | AFEF] " + playerPrefix + " unknown text {0} $1", "[date | scene | AFEF] " + delivered + " trailing user text",
            "[date | scene | AFEF] " + delivered + " [date | scene | AFEF] " + delivered })
            Check(MemorySystemFactProjection.ProjectRecordedFactForPrompt(raw) == raw, "unknown or ambiguous prose unchanged");
        Console.WriteLine("PASS fact recall " + args[1] + ": " + checks + " assertions; production projector/readers and exact recall/overview/daily renderer methods; save text unchanged");
    }
}

namespace AnimusForge
{
    internal static class AnimusForgeModulePaths { internal static string GetModuleDataFilePath(string name) => Path.Combine(Program.PackageRoot, name); }
    // Unrelated gathering and record-title rendering are not exercised by this replay.
    internal static class NobleGatheringTextProjection { internal static string ProjectFactBodyForPrompt(string text) => text; }
    internal static class MemoryRecordRules
    {
        internal static string StripMemoryTitleDateTime(string text) => text;
        internal static string BuildCompressedMemoryBlockId(string heroId, int day) => heroId + ":" + day;
    }
    internal sealed class DailyMemoryLine
    {
        internal string Text, GameDate, Scene, Speaker;
        internal int GameDayIndex, GameHour;
        internal bool IsAfef;
    }
    internal sealed class CompressedMemoryBlock
    {
        internal string Id, HeroId, GameDate, RichTitle, Summary;
        internal int GameDayIndex, StartHour, EndHour;
        internal List<string> Scenes, AfefLines;
    }
}
