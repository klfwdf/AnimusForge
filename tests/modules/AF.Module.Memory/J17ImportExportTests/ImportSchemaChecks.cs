using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace TaleWorlds.Library
{
    internal sealed class InformationMessage
    {
        internal string Text;
        internal InformationMessage(string text) { Text = text; }
    }
    internal static class InformationManager
    {
        internal static readonly List<string> Messages = new();
        internal static void DisplayMessage(InformationMessage message) => Messages.Add(message.Text);
    }
}
namespace AnimusForge.Refactor.Runtime
{
    internal static class SaveRuntimeGuard { internal static long CaptureGeneration() => 1; }
}
namespace AnimusForge
{
    internal static class Logger { internal static void Log(string tag, string text) { } }
    // Only campaign lifetime/UI/file selection are fakes. Parser, current single-NPC
    // method and memory import owner are production code.
    internal sealed partial class MyBehavior
    {
        internal MemoryImportExportState State = new();
        internal bool Current = true;
        internal int Confirmations;
        internal Action Overwrite, Skip;
        internal void Import(string path) => ImportSingleNpcDialogueHistoryData(path, "hero");
        internal void ImportBulk(string path) => ImportDialogueHistoryData(path);
        private bool IsMemorySourceEditorCurrent(long generation) => Current && generation == 1;
        private static string FindNpcJsonByHeroId(string dir, string id) => throw new Exception("explicit file only");
        private bool HasCompressedMemoryDataForHero(string id) => MemoryImportExportOwner.HasData(id, State);
        private static string NormalizeMemoryHeroId(string id) => MemoryRecordRules.NormalizeMemoryHeroId(id);
        private bool ApplyCompressedMemoryExportBundle(string id, CompressedMemoryExportBundle bundle, bool overwriteExisting)
            => MemoryImportExportOwner.Apply(id, bundle, overwriteExisting, State, null);
        private void ShowDuplicateImportInquiry(string title, string text, Action overwrite, Action skip, Action cancel)
        {
            Confirmations++;
            Overwrite = overwrite;
            Skip = skip;
        }
    }
    internal static class NpcDataIdentityFileAdapter
    {
        internal static string FindNpcJsonByHeroId(string dir,string id) => throw new Exception("explicit file only");
    }
    internal static class ImportSchemaChecks
    {
        private static int checks;
        private static void Check(bool ok, string label) { if (!ok) throw new Exception(label); checks++; }
        private static string Snapshot(MyBehavior host) => JsonConvert.SerializeObject(new
        {
            host.State.DailyDrafts, host.State.Blocks, host.State.SummaryQueue,
            host.State.Overviews, host.State.OverviewQueue
        });
        private static MyBehavior Existing()
        {
            var host = new MyBehavior();
            host.State.DailyDrafts = new() { ["hero"] = new() { new() { HeroId = "hero", GameDayIndex = 1 } } };
            host.State.Blocks = new() { ["hero"] = new() { new() { HeroId = "hero", Id = "old", GameDayIndex = 1 } } };
            host.State.SummaryQueue = new() { new() { HeroId = "hero" } };
            host.State.Overviews = new() { ["hero"] = new() { HeroId = "hero", Summary = "old overview" } };
            host.State.OverviewQueue = new() { new() { HeroId = "hero" } };
            return host;
        }
        internal static void Run()
        {
            // Synthetic structure only: no player's private name/persona/history is embedded.
            string path = Path.GetFullPath("synthetic-memory-import.json");
            string persona = "{\"HeroId\":\"generated\",\"HeroName\":\"NPC\",\"Personality\":\"synthetic\",\"Background\":\"synthetic\",\"VoiceId\":\"\"}";
            File.WriteAllText(path, persona);
            var legacy = Existing();
            var legacyBundle = PlayerExportsStore.ReadJson<CompressedMemoryExportBundle>(path);
            Check(legacyBundle != null, "legacy decoder accepts persona as an empty memory bundle");
            Check(MemoryImportExportOwner.Apply("hero", legacyBundle, true, legacy.State, null)
                && !MemoryImportExportOwner.HasData("hero", legacy.State), "reproduced old false-success deletion");
            Console.WriteLine("REPRO legacy persona-as-memory overwrite returned success and removed all five memory domains");

            foreach (string invalid in new[] { persona, "{}", "{\"HeroId\":\"hero\"}", "null", "[]", "badJSON", "",
                "{\"Blocks\":\"wrong\"}", "{\"Overview\":[]}", "{\"SummaryQueue\":{}}", "{\"OverviewQueue\":42}",
                "{\"DailyDrafts\":[42]}", "{\"Blocks\":[],\"Overview\":\"wrong\"}" })
            {
                File.WriteAllText(path, invalid);
                Check(!CompressedMemoryExportBundleReader.TryRead(path, out var rejected, out var error)
                    && rejected == null && !string.IsNullOrWhiteSpace(error), "invalid schema rejected with a reason");
                var host = Existing();
                string before = Snapshot(host);
                TaleWorlds.Library.InformationManager.Messages.Clear();
                host.Import(path);
                Check(host.Confirmations == 0 && Snapshot(host) == before, "current single import preserves all domains before confirmation");
                Check(TaleWorlds.Library.InformationManager.Messages.Single().StartsWith("导入失败："), "current entry cannot report invalid input as success");
                if (invalid == persona)
                    Check(TaleWorlds.Library.InformationManager.Messages.Single().Contains("个性/背景导入"), "persona gives correct import entry");
            }

            var exported = new CompressedMemoryExportBundle
            {
                Blocks = new() { new() { HeroId = "source", Id = "new", GameDayIndex = 9 } },
                Overview = new() { HeroId = "source", Summary = "imported overview" }
            };
            File.WriteAllText(path, JsonConvert.SerializeObject(exported));
            var fresh = new MyBehavior();
            fresh.Import(path);
            Check(fresh.State.Blocks["hero"].Single().Id == "new" && fresh.State.Overviews["hero"].Summary == "imported overview", "valid export reaches real memory owner");
            var overwrite = Existing();
            overwrite.Import(path);
            Check(overwrite.Confirmations == 1 && overwrite.State.Blocks["hero"].Single().Id == "old", "valid duplicate still requires confirmation");
            overwrite.Overwrite();
            Check(overwrite.State.Blocks["hero"].Single().Id == "new", "valid confirmed overwrite preserved");
            var skip = Existing();
            string prior = Snapshot(skip);
            skip.Import(path);
            skip.Skip();
            Check(Snapshot(skip) == prior, "valid skip keeps existing memory");
            var late = Existing();
            prior = Snapshot(late);
            late.Import(path);
            late.Current = false;
            late.Overwrite();
            Check(Snapshot(late) == prior, "late callback still checks campaign lifetime");

            foreach (string valid in new[] { "{\"Blocks\":[]}", "{\"blocks\":[],\"overview\":{\"Summary\":\"old-format\"}}",
                "{\"DailyDrafts\":null,\"Blocks\":null,\"Overview\":null}",
                "{\"Overview\":{\"Summary\":\"legacy\"},\"FutureField\":true}" })
            {
                File.WriteAllText(path, valid);
                Check(CompressedMemoryExportBundleReader.TryRead(path, out _, out _), "legacy partial/case/null/unknown field compatibility");
            }
            File.WriteAllText(path, JsonConvert.SerializeObject(new CompressedMemoryExportBundle()));
            var empty = Existing();
            empty.Import(path);
            empty.Overwrite();
            Check(!MemoryImportExportOwner.HasData("hero", empty.State), "intentional valid empty export still clears on confirmed overwrite");
            Check(!CompressedMemoryExportBundleReader.TryRead(path + ".missing", out _, out _), "missing file rejects");
            string package = Path.GetFullPath("synthetic-memory-package");
            string memoryDir = Path.Combine(package, "compressed_memory");
            Directory.CreateDirectory(memoryDir);
            string wrongFile = Path.Combine(memoryDir, "wrong__NPC.json");
            File.WriteAllText(wrongFile, persona);
            var bulkInvalid = Existing();
            prior = Snapshot(bulkInvalid);
            TaleWorlds.Library.InformationManager.Messages.Clear();
            bulkInvalid.ImportBulk(package);
            Check(bulkInvalid.Confirmations == 0 && Snapshot(bulkInvalid) == prior, "all-invalid bulk refuses before memory mutation");
            Check(TaleWorlds.Library.InformationManager.Messages.Single().StartsWith("导入失败："), "all-invalid bulk reports failure");
            File.WriteAllText(Path.Combine(memoryDir, "hero__NPC.json"), JsonConvert.SerializeObject(exported));
            var mixed = Existing();
            TaleWorlds.Library.InformationManager.Messages.Clear();
            mixed.ImportBulk(package);
            mixed.Overwrite();
            Check(mixed.State.Blocks["hero"].Single().Id == "new" && !MemoryImportExportOwner.HasData("wrong", mixed.State), "mixed bulk imports only valid memory");
            Check(TaleWorlds.Library.InformationManager.Messages.Single().Contains("跳过 1 个无效压缩记忆文件"), "mixed bulk reports partial completion");
            Console.WriteLine($"PASS {checks} schema/current single-NPC/bulk import assertions (real parser/entries/owner; campaign and UI fakes)");
        }
    }
}
