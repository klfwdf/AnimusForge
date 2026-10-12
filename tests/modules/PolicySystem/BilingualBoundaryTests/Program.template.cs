using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Newtonsoft.Json;

namespace TaleWorlds.Localization
{
    // Test-only UI language provider; resources are the actual paired Policy XML.
    public sealed class TextObject
    {
        internal static Dictionary<string, string> Values;
        private readonly string _text;
        public TextObject(string text) { _text = text; }
        public void SetTextVariable(string name, string value) { }
        public override string ToString()
        {
            int end = _text.IndexOf('}');
            string id = _text.Substring(2, end - 2);
            return Values.TryGetValue(id, out string value) ? value : _text.Substring(end + 1);
        }
    }
}
namespace AnimusForge.PolicyEffects
{
    internal static class PolicyEffectPromptService
    {
        internal static string DefaultAutoDraftPrompt => PolicyGenerationPromptTexts.Get("DefaultAutoDraft", "missing");
    }
}
namespace AnimusForge
{
    internal static class AnimusForgeModulePaths
    {
        internal static string Directory;
        internal static int Reads;
        internal static string GetModuleDataFilePath(string name) { Reads++; return Path.Combine(Directory, name); }
    }
    internal static class DisplayProjection
    {
/*UI_SOURCE*/
        internal static string Apply(string error) => MapPlayerPolicyAutoDraftErrorToDisplay(error);
/*STORAGE_SOURCE*/
        internal static string ApplyStorage(string error) => ProjectStorageErrorForDisplay(error);
        internal static string ApplyBalance(string message)
        {
            var ex = new Exception(message);
/*BALANCE_SOURCE*/
            return displayError;
        }
    }
    internal static class Program
    {
        private static void Require(bool ok, string reason) { if (!ok) throw new Exception(reason); }
        private sealed class Embedding { internal string LastError = "native detail 原始 {NAME} $1"; }
        private sealed class Entity { internal string DocumentId = "entity-17"; }
        private sealed class Facet { internal string Id = "facet-9"; }
        private static Exception Diagnostic(int index, bool before)
        {
            float[] queryVector = null;
            object recall = null;
            Embedding embedding = new Embedding();
            Entity entity = new Entity();
            Facet facet = new Facet();
            switch (index)
            {
/*DIAGNOSTIC_SOURCE*/
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }
        private static void Main(string[] args)
        {
            AnimusForgeModulePaths.Directory = args[0];
            bool chinesePackage = args[1] == "CN";
            bool chineseUi = args[2] == "CN";
            string root = args[3];
            var ui = new Dictionary<string, string>();
            string languages = Path.Combine(root, "content/modules/PolicySystem/ModuleData/Languages");
            foreach (string xml in chineseUi
                ? new[] { "CNs/afpol_ui_strings-zh-CN.xml", "CNs/afpol_misc_strings-zh-CN.xml" }
                : new[] { "afpol_ui_strings.xml", "afpol_misc_strings.xml" })
                foreach (var text in XDocument.Load(Path.Combine(languages, xml)).Descendants("string"))
                    ui.Add((string)text.Attribute("id"), (string)text.Attribute("text"));
            TaleWorlds.Localization.TextObject.Values = ui;
            string[] rawErrors = {
                "AI编写没有返回有效的结构化结果。", "AI编写返回的结构化结果无法解析。",
                "AI编写只能返回政策名和政策正文两个字段。", "AI编写返回的政策名和政策正文必须是文本。",
                "AI编写返回了空的政策名或政策正文。", "AI编写修改了玩家已指定的政策名。"
            };
            string[] englishAliases = {
                "AI drafting did not return a valid structured result.", "Structured result returned by AI drafting cannot be parsed.",
                "AI drafting can only return policyName and policyContent.", "policyName and policyContent returned by AI drafting must be text.",
                "AI drafting returned empty policyName or policyContent.", "AI drafting modified the player-specified policy name."
            };
            string[] ids = { "EmptyResult", "UnparseableResult", "FieldsOnly", "FieldsMustBeText", "EmptyFields", "ChangedPolicyName" };
            string[] invalid = { "", "{", "{\"policyName\":\"Fixed\",\"policyContent\":\"body\",\"extra\":1}",
                "{\"policyName\":1,\"policyContent\":\"body\"}", "{\"policyName\":\"\",\"policyContent\":\"body\"}",
                "{\"policyName\":\"Changed\",\"policyContent\":\"body\"}" };
            var request = new PlayerPolicyAutoDraftRequest { ExistingPolicyName = "Fixed" };
            for (int i = 0; i < invalid.Length; i++)
            {
                Require(!PlayerPolicyAutoDraftPromptBuilder.TryParseResult(invalid[i], request, out var result, out string error), "invalid result accepted");
                Require(error == rawErrors[i] && result == null, "raw legacy parser contract changed");
                string expected = ui["AFPOL_UI_AiDraft" + ids[i]];
                Require(DisplayProjection.Apply(error) == expected && DisplayProjection.Apply(englishAliases[i]) == expected, "UI projection/alias incorrect");
                Require(PlayerPolicyAutoDraftResult.Failed(error).Error == error, "Failed changed raw error");
            }
            Require(DisplayProjection.Apply("user text {NAME} $1") == "user text {NAME} $1", "unknown raw error changed");
            string[] storageErrors = {
                "The existing player AI drafting prompt file is corrupted; back up or repair the original first. The original was not overwritten.",
                "The existing shared effect requirements file is corrupted; the original was not overwritten.",
                "The existing policy effect requirements file is corrupted; the original was not overwritten.",
                "The policy prompt target directory is empty.", "Unable to locate the policy prompt folder.",
                "The policy effect ModuleId cannot be safely mapped to a prompt file name."
            };
            string[] storageIds = { "AutoDraftFileCorrupt", "CommonEffectFileCorrupt", "ModuleFileCorrupt", "TargetDirectoryEmpty", "StorageFolderUnavailable", "ModuleIdUnsafe" };
            for (int i = 0; i < storageErrors.Length; i++)
                Require(DisplayProjection.ApplyStorage(storageErrors[i]) == ui["AFPOL_UI_Prompt" + storageIds[i]], "storage error UI projection incorrect");
            Require(DisplayProjection.ApplyStorage("native error 原文 {NAME} $1") == "native error 原文 {NAME} $1", "native storage details changed");
            Require(DisplayProjection.ApplyBalance("No settings screen is available.") == ui["AFPOL_UI_NoSettingsScreen"], "balance UI projection incorrect");
            Require(DisplayProjection.ApplyBalance("native error 原文 {NAME} $1") == "native error 原文 {NAME} $1", "native balance details changed");
            foreach (string name in new[] { "Chinese title 政策", "English title {NAME} $1" })
            {
                string content = "Player content 玩家原文 {NAME} $1";
                request.ExistingPolicyName = name;
                string json = JsonConvert.SerializeObject(new { policyName = name, policyContent = content });
                Require(PlayerPolicyAutoDraftPromptBuilder.TryParseResult(json, request, out var result, out string error), "valid bilingual input rejected");
                Require(result.PolicyName == name && result.PolicyContent == content && error == "", "player input modified");
            }
            request.ExistingPolicyName = "Fixed {NAME} $1";
            request.PlayerDescription = "Player original 原文 {AMOUNT} $1";
            request.WritingPrompt = "Editable requirement {NAME} $1";
            string messages = JsonConvert.SerializeObject(PlayerPolicyAutoDraftPromptBuilder.BuildMessages(request));
            Require(messages.Contains(request.ExistingPolicyName) && messages.Contains(request.PlayerDescription) && messages.Contains(request.WritingPrompt), "dynamic prompt data reinterpreted");
            string[] diagnostics = {
                "Policy target query embedding is invalid.", "Policy target semantic snapshot is unavailable.",
                "Policy target semantic recall result is invalid.", "Policy target ONNX embedding is unavailable: native detail 原始 {NAME} $1",
                "Policy target entity vector build failed: entity-17: native detail 原始 {NAME} $1",
                "Policy target Facet ONNX embedding is unavailable: native detail 原始 {NAME} $1",
                "Policy target Facet vector build failed: facet-9: native detail 原始 {NAME} $1",
                "Policy target semantic routing internal contract is invalid."
            };
            int beforeMismatches = 0;
            for (int i = 0; i < diagnostics.Length; i++)
            {
                Exception current = Diagnostic(i, false);
                string message = current is ArgumentException arg ? current.Message.Split(" (Parameter")[0] : current.Message;
                Require(message == diagnostics[i], "internal diagnostic depends on package/UI or loses details");
                if (current is ArgumentException argument) Require(argument.ParamName == (i == 0 ? "queryVector" : "recall"), "parameter identity lost");
                string old = Diagnostic(i, true).Message;
                if (!old.StartsWith(diagnostics[i], StringComparison.Ordinal)) beforeMismatches++;
            }
            Require(beforeMismatches == (chinesePackage ? 8 : 2), "baseline reproduction no longer matches source");
            Require(AnimusForgeModulePaths.Reads == 1, "package catalog re-read");
            Console.WriteLine(JsonConvert.SerializeObject(new {
                Package = args[1], UI = args[2], RawParserErrors = 6, ProjectedCN_ENAliases = 12,
                SuccessBilingualInputs = 2, InternalDiagnostics = 8, BaselineMismatches = beforeMismatches,
                StorageErrorProjections = 6, BalanceErrorProjections = 1, NativeErrorPreservation = true,
                PackageCatalogReads = AnimusForgeModulePaths.Reads, Messages = messages,
                DisplayedParseError = DisplayProjection.Apply(rawErrors[0])
            }));
        }
    }
}
