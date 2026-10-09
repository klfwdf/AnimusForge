using TaleWorlds.Localization;

namespace AnimusForge;

/// <summary>Localized shell text for player-facing import/export and weekly notices.</summary>
internal static class ImportExportUiText
{
    internal static string T(string id, string fallback, params (string Name, string Value)[] variables)
    {
        var text = new TextObject("{" + "=" + id + "}" + fallback);
        if (variables != null)
        {
            foreach (var variable in variables)
            {
                text.SetTextVariable(variable.Name, variable.Value ?? "");
            }
        }
        return text.ToString();
    }

    internal static string ExportCompleted(string path) => T("AF_UI_ImportExport_ExportCompleted", "Export completed: {PATH}", ("PATH", path));
    internal static string ExportFailed(string error) => T("AF_UI_ImportExport_ExportFailed", "Export failed: {ERROR}", ("ERROR", error));
    internal static string ImportFailed(string error) => T("AF_UI_ImportExport_ImportFailed", "Import failed: {ERROR}", ("ERROR", error));
    internal static string ImportCompleted(string path) => T("AF_UI_ImportExport_ImportCompleted", "Import completed: {PATH}", ("PATH", path));
    internal static string ImportCompletedDuplicates(string path) => T("AF_UI_ImportExport_ImportCompletedDuplicates", "Import completed (duplicates skipped): {PATH}", ("PATH", path));
    internal static string ImportCompletedMerged(string path) => T("AF_UI_ImportExport_ImportCompletedMerged", "Import completed (merged): {PATH}", ("PATH", path));
    internal static string PartialImport(int count, string path) => T("AF_UI_ImportExport_PartialImport", "Partial import completed (skipped {COUNT} invalid compressed-memory files): {PATH}", ("COUNT", count.ToString()), ("PATH", path));
    internal static string PartialImportDuplicates(int count, string path) => T("AF_UI_ImportExport_PartialImportDuplicates", "Partial import completed (skipped {COUNT} invalid compressed-memory files; duplicates were skipped): {PATH}", ("COUNT", count.ToString()), ("PATH", path));
    internal static string MissingExportDirectory() => T("AF_UI_ImportExport_MissingExportDirectory", "Import failed: export directory not found.");
    internal static string MissingDirectory(string directory) => T("AF_UI_ImportExport_MissingDirectory", "Import failed: {DIRECTORY} directory not found.", ("DIRECTORY", directory));
    internal static string MissingFile(string file) => T("AF_UI_ImportExport_MissingFile", "Import failed: {FILE} was not found.", ("FILE", file));
    internal static string EmptyFile(string file) => T("AF_UI_ImportExport_EmptyFile", "Import failed: {FILE} is empty.", ("FILE", file));
    internal static string InvalidFile(string file) => T("AF_UI_ImportExport_InvalidFile", "Import failed: {FILE} is invalid.", ("FILE", file));
    internal static string InvalidData(string data) => T("AF_UI_ImportExport_InvalidData", "Import failed: {DATA} is invalid.", ("DATA", data));
    internal static string BehaviorNotInitialized(string behavior) => T("AF_UI_ImportExport_BehaviorNotInitialized", "Import failed: {BEHAVIOR} is not initialized.", ("BEHAVIOR", behavior));
    internal static string KeyEmpty() => T("AF_UI_ImportExport_KeyEmpty", "Export failed: key is empty.");
    internal static string EntryNotFound(string value) => T("AF_UI_ImportExport_EntryNotFound", "Export failed: the entry was not found: {VALUE}", ("VALUE", value));
    internal static string NpcExportMissing() => T("AF_UI_ImportExport_NpcExportMissing", "Import failed: this NPC has no corresponding export file.");
    internal static string NpcFileMismatch() => T("AF_UI_ImportExport_NpcFileMismatch", "Import failed: the personality/background file does not match the current NPC.");
    internal static string JsonParseFailed() => T("AF_UI_ImportExport_JsonParseFailed", "Import failed: JSON parsing failed.");
    internal static string WriteFailed() => T("AF_UI_ImportExport_WriteFailed", "Import failed: writing the rule failed.");
    internal static string IdMismatch(string id) => T("AF_UI_ImportExport_IdMismatch", "Import failed: the file Id does not match (file Id={ID}).", ("ID", id));
    internal static string AllInvalid(int count) => T("AF_UI_ImportExport_AllInvalid", "Import failed: all compressed-memory files were invalid (skipped {COUNT}).", ("COUNT", count.ToString()));
    internal static string DuplicateTitle(string kind) => T("AF_UI_ImportExport_DuplicateTitle", "Duplicates detected - {KIND}", ("KIND", kind));
    internal static string DuplicateExisting(string kind, string heroId) => T("AF_UI_ImportExport_DuplicateExisting", "This NPC already has {KIND} data: {ID}\nChoose how to proceed:", ("KIND", kind), ("ID", heroId));
    internal static string DuplicateMany(string kind, int count, int total) => T("AF_UI_ImportExport_DuplicateMany", "Imported data contains HeroIds already present in the current game.\n{KIND} duplicates: {COUNT} / total: {TOTAL}\nChoose how to proceed:", ("KIND", kind), ("COUNT", count.ToString()), ("TOTAL", total.ToString()));
    internal static string DuplicateSkipped(string id) => T("AF_UI_ImportExport_DuplicateSkipped", "Duplicate entry skipped: {ID}", ("ID", id));
    internal static string WarningSkipped(string name) => T("AF_UI_ImportExport_WarningSkipped", "Warning: {NAME} import failed and was skipped.", ("NAME", name));
    internal static string IdEmpty() => T("AF_UI_ImportExport_IdEmpty", "Operation failed: RuleId/key is empty.");
    internal static string UnnamedNpcNotFound(string value) => T("AF_UI_ImportExport_UnnamedNpcNotFound", "Export failed: unnamed NPC entry was not found: {VALUE}", ("VALUE", value));
    internal static string UnnamedNpcFileMissing(string value) => T("AF_UI_ImportExport_UnnamedNpcFileMissing", "Import failed: export file for unnamed NPC entry was not found: {VALUE}", ("VALUE", value));
    internal static string DuplicateKeyBlocked(string value) => T("AF_UI_ImportExport_DuplicateKeyBlocked", "Import failed: duplicate key {VALUE} already exists in the current game; overwriting is disabled.", ("VALUE", value));
    internal static string KnowledgeRulesMissingDetail() => T("AF_UI_ImportExport_KnowledgeRulesMissingDetail", "knowledge\\AIConfig.json, knowledge\\rules\\*.json, or knowledge\\KnowledgeRules.json was not found.");
    internal static string KnowledgeImportFailure(string detail) => ImportFailed(detail);
    internal static string KeywordValidationDetail(string error) => T("AF_UI_ImportExport_KeywordValidationDetail", "keyword validation failed: {ERROR}", ("ERROR", error));
    internal static string KnowledgeBehaviorDetail() => T("AF_UI_ImportExport_KnowledgeBehaviorDetail", "KnowledgeLibraryBehavior is not initialized.");
    internal static string KnowledgeNoEntriesDetail() => T("AF_UI_ImportExport_KnowledgeNoEntriesDetail", "There are no Knowledge entries to export.");
    internal static string KnowledgeNoFilesDetail() => T("AF_UI_ImportExport_KnowledgeNoFilesDetail", "No Knowledge files were written successfully.");
    internal static string KnowledgeWriteFailedDetail() => T("AF_UI_ImportExport_KnowledgeWriteFailedDetail", "Knowledge rules could not be written.");
    internal static string KnowledgeImportExceptionDetail() => T("AF_UI_ImportExport_KnowledgeImportExceptionDetail", "An exception occurred during Knowledge import.");
    internal static string VoiceMappingInvalid() => T("AF_UI_ImportExport_VoiceMappingInvalid", "Import failed: VoiceMapping JSON is invalid.");
    internal static string VoiceMappingWarningSkipped() => T("AF_UI_ImportExport_VoiceMappingWarningSkipped", "Warning: VoiceMapping import failed and was skipped.");
    internal static string VoiceMappingDuplicateTitle() => T("AF_UI_ImportExport_VoiceMappingDuplicateTitle", "Duplicates detected - VoiceMapping");
    internal static string VoiceMappingDuplicateBody() => T("AF_UI_ImportExport_VoiceMappingDuplicateBody", "Voice mappings already exist.\nChoose how to proceed:");
    internal static string KnowledgeExportCount(string path, int count) => T("AF_UI_ImportExport_KnowledgeExportCount", "Export completed: {PATH} (Knowledge entries: {COUNT})", ("PATH", path), ("COUNT", count.ToString()));
    internal static string KnowledgeNoEntries() => T("AF_UI_ImportExport_KnowledgeNoEntries", "There are no Knowledge entries to export.");
    internal static string KnowledgeNoFilesWritten() => T("AF_UI_ImportExport_KnowledgeNoFilesWritten", "No Knowledge files were written successfully.");
    internal static string KnowledgeRulesMissing() => T("AF_UI_ImportExport_KnowledgeRulesMissing", "knowledge\\AIConfig.json, knowledge\\rules\\*.json, or knowledge\\KnowledgeRules.json was not found.");
    internal static string KnowledgeImportFallbackSummary(int imported, int failed, string ruleId, string reason) => T("AF_UI_ImportExport_KnowledgeImportFallbackSummary", "Knowledge import used per-entry fallback: {IMPORTED} succeeded, {FAILED} failed. First failed RuleId={RULEID}{REASON}", ("IMPORTED", imported.ToString()), ("FAILED", failed.ToString()), ("RULEID", ruleId), ("REASON", reason));
    internal static string KnowledgeRulesAllFailed(int failed, string ruleId, string reason) => T("AF_UI_ImportExport_KnowledgeRulesAllFailed", "All Knowledge rules failed to import. Failed: {FAILED}. First failed RuleId={RULEID}{REASON}", ("FAILED", failed.ToString()), ("RULEID", ruleId), ("REASON", reason));
    internal static string KnowledgeImportException() => T("AF_UI_ImportExport_KnowledgeImportException", "An exception occurred during Knowledge import.");
    internal static string KnowledgeDuplicateTitle() => T("AF_UI_ImportExport_KnowledgeDuplicateTitle", "Duplicates detected - Knowledge");
    internal static string KnowledgeDuplicateBody(string id) => T("AF_UI_ImportExport_KnowledgeDuplicateBody", "A Knowledge rule with the same RuleId already exists: {ID}\nChoose how to proceed:", ("ID", id));
    internal static string KnowledgeDuplicateMany(int count, int total) => T("AF_UI_ImportExport_KnowledgeDuplicateMany", "Imported Knowledge rules contain duplicate RuleIds.\nDuplicates: {COUNT} / total: {TOTAL}\nChoose how to proceed:", ("COUNT", count.ToString()), ("TOTAL", total.ToString()));
    internal static string PersonaDuplicateTitle() => T("AF_UI_ImportExport_PersonaDuplicateTitle", "Duplicates detected - personality/background");
    internal static string PersonaDuplicateBody(string id) => T("AF_UI_ImportExport_PersonaDuplicateBody", "This NPC already has personality/background data: {ID}\nChoose how to proceed:", ("ID", id));
    internal static string PersonaDuplicateMany(int count, int total) => T("AF_UI_ImportExport_PersonaDuplicateMany", "Imported data contains duplicate HeroIds.\nDuplicates: {COUNT} / total: {TOTAL}\nChoose how to proceed:", ("COUNT", count.ToString()), ("TOTAL", total.ToString()));
    internal static string DebtDuplicateTitle() => T("AF_UI_ImportExport_DebtDuplicateTitle", "Duplicates detected - debt");
    internal static string DebtDuplicateBody(string id) => T("AF_UI_ImportExport_DebtDuplicateBody", "This NPC already has debt records: {ID}\nChoose how to proceed:", ("ID", id));
    internal static string DebtDuplicateMany(int count, int total) => T("AF_UI_ImportExport_DebtDuplicateMany", "Imported debt data contains duplicate HeroIds.\nDuplicates: {COUNT} / total: {TOTAL}\nChoose how to proceed:", ("COUNT", count.ToString()), ("TOTAL", total.ToString()));
    internal static string WeeklyEventDuplicateTitle() => T("AF_UI_ImportExport_WeeklyEventDuplicateTitle", "Duplicates detected - event editor");
    internal static string WeeklyEventDuplicateBody(int world, int kingdoms, int events, int worldTotal, int kingdomTotal, int eventTotal) => T("AF_UI_ImportExport_WeeklyEventDuplicateBody", "Imported data overlaps the current game.\nWorld opening summaries: {WORLD}/{WORLD_TOTAL}\nKingdom opening summaries: {KINGDOM}/{KINGDOM_TOTAL}\nEvent records: {EVENT}/{EVENT_TOTAL}\nChoose how to proceed:", ("WORLD", world.ToString()), ("WORLD_TOTAL", worldTotal.ToString()), ("KINGDOM", kingdoms.ToString()), ("KINGDOM_TOTAL", kingdomTotal.ToString()), ("EVENT", events.ToString()), ("EVENT_TOTAL", eventTotal.ToString()));
    internal static string FilenameFormatMissing() => T("AF_UI_ImportExport_FilenameFormatMissing", "The file name is missing the heroId__name format.");
    internal static string FilenameNameMismatch(string fileName, string currentName) => T("AF_UI_ImportExport_FilenameNameMismatch", "The file name character name \"{FILE}\" does not match the current character with the same ID \"{CURRENT}\".", ("FILE", fileName), ("CURRENT", currentName));
    internal static string FilenameIdUnreliable(string id) => T("AF_UI_ImportExport_FilenameIdUnreliable", "Auto-numbered ID {ID} cannot be matched reliably in the current save, and the file name cannot uniquely identify the character.", ("ID", id));
    internal static string WeeklyNoticeOpenHint(string text) => T("AF_UI_Weekly_Notice_OpenHint", "Click to view: {TEXT}", ("TEXT", text));
    internal static string WeeklyMissing() => T("AF_UI_Weekly_Notice_Missing", "This weekly report no longer exists.");
    internal static string WeeklyOpenFailed() => T("AF_UI_Weekly_Notice_OpenFailed", "The weekly report could not be opened.");
    internal static string WeeklyXpBatch(int count, int charm, int leadership, int steward) => T("AF_UI_Weekly_Notice_XpBatch", "Weekly-report reading breakthrough: {COUNT} reports read; Charm +{CHARM}, Leadership +{LEADERSHIP}, Steward +{STEWARD}.", ("COUNT", count.ToString()), ("CHARM", charm.ToString()), ("LEADERSHIP", leadership.ToString()), ("STEWARD", steward.ToString()));
}
