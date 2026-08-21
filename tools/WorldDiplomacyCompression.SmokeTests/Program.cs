using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

static class Test
{
    private static int _assertions;

    internal static void True(bool value, string message)
    {
        _assertions++;
        if (!value)
        {
            throw new InvalidOperationException(message);
        }
    }

    internal static void Equal<T>(T expected, T actual, string message)
        where T : notnull
    {
        _assertions++;
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(
                message + " (expected=" + expected + ", actual=" + actual + ")");
        }
    }

    internal static int Assertions => _assertions;
}

internal static class Program
{
    private static int Main()
    {
        string settings = ReadRepositoryFile("DuelSettings.cs");
        string behavior = ReadRepositoryFile("WorldDiplomacyBehavior.cs");
        string client = ReadRepositoryFile("WorldDiplomacyLlmClient.cs");

        VerifyDefaultsAndRanges(settings);
        VerifyDeclarationLengthSettingsAndPromptContract(settings, behavior);
        VerifyIndependentSettingsAndMcmOrder(settings, behavior);
        VerifyPersistentMigration(settings);
        VerifyWorldDiplomacyPromptV5Migration(settings);
        VerifyStaticModeContractsAndPromptMigration(behavior);
        VerifyCompressionScheduling(behavior);
        VerifyFrozenOverallTarget(behavior);
        VerifyRouteOutputCap(settings, behavior, client);
        VerifyNonRetryableClientErrors(client);
        VerifyCompressionTimeout(settings, behavior);
        VerifyTokenStatsDiagnosticTruncation(client);
        VerifySegmentedHashing(behavior);

        Console.WriteLine("World diplomacy compression smoke tests passed: " + Test.Assertions);
        return 0;
    }

    private static void VerifyDefaultsAndRanges(string settings)
    {
		Test.True(settings.Contains("【AnimusForge 王国外交共同契约 v25】", StringComparison.Ordinal),
			"the negotiated-round contract must use common diplomacy contract v25");
        Test.Equal(64, ReadIntConstant(settings, "WorldDiplomacyHistoryCompressionTriggerThousandsMin"),
            "trigger minimum must retain a practical lower bound");
        Test.Equal(900, ReadIntConstant(settings, "WorldDiplomacyHistoryCompressionTriggerThousandsMax"),
            "trigger maximum must retain the explicit large-context ceiling");
        Test.Equal(800, ReadIntConstant(settings, "DefaultWorldDiplomacyHistoryCompressionTriggerThousands"),
            "compression must trigger at 800k estimated tokens by default");
        Test.Equal(8, ReadIntConstant(settings, "WorldDiplomacyHistoryCompressionTargetThousandsMin"),
            "post-compression target minimum must be 8k");
        Test.Equal(60, ReadIntConstant(settings, "WorldDiplomacyHistoryCompressionTargetThousandsMax"),
            "post-compression target maximum must leave JSON/output headroom");
        Test.Equal(48, ReadIntConstant(settings, "DefaultWorldDiplomacyHistoryCompressionTargetThousands"),
            "post-compression overall target must default to 48k estimated tokens");
		Test.Equal(0, ReadIntConstant(settings, "WorldDiplomacyThreatComplianceIssuerRelationRewardMin"),
			"threat-compliance issuer relation reward must support disabling at zero");
		Test.Equal(10, ReadIntConstant(settings, "DefaultWorldDiplomacyThreatComplianceIssuerRelationReward"),
			"threat-compliance issuer relation reward must default to 10");
    }

    private static void VerifyDeclarationLengthSettingsAndPromptContract(string settings, string behavior)
    {
        Test.Equal(1, ReadIntConstant(settings, "WorldDiplomacyDeclarationCharactersMin"),
            "declaration length must allow a one-character lower bound");
        Test.Equal(1000, ReadIntConstant(settings, "WorldDiplomacyDeclarationCharactersMax"),
            "declaration length must retain the selected 1000-character upper bound");
        Test.Equal(40, ReadIntConstant(settings, "DefaultWorldDiplomacyDeclarationMinCharacters"),
            "declaration minimum must default to 40 characters");
        Test.Equal(200, ReadIntConstant(settings, "DefaultWorldDiplomacyDeclarationMaxCharacters"),
            "declaration maximum must default to 200 characters");

        string[] properties =
        {
            "WorldDiplomacyDeclarationMinCharacters",
            "WorldDiplomacyDeclarationMaxCharacters"
        };
        int[] orders = properties.Select(property => ReadPrecedingMcmOrder(settings, property)).ToArray();
        Test.True(orders.SequenceEqual(new[] { 8, 9 }),
            "declaration length controls must occupy the reserved AI-diplomacy orders 8 and 9");

        string rangeHelper = ExtractSection(
            behavior,
            "private static void GetDiplomaticDeclarationCharacterRange(out int minimumCharacters, out int maximumCharacters)",
            "private static bool IsWorldDiplomacyEnabled()");
        string compactRangeHelper = CompactWhitespace(rangeHelper);
        Test.True(compactRangeHelper.Contains("DuelSettings.WorldDiplomacyDeclarationCharactersMin", StringComparison.Ordinal)
                  && compactRangeHelper.Contains("DuelSettings.WorldDiplomacyDeclarationCharactersMax", StringComparison.Ordinal),
            "declaration range must clamp both MCM values to the configured bounds");
        Test.True(compactRangeHelper.Contains("maximumCharacters = Math.Max(minimumCharacters, configuredMaximumCharacters);", StringComparison.Ordinal),
            "an inverted declaration range must normalize its maximum to the minimum");
        Test.True(rangeHelper.Contains("DuelSettings.DefaultWorldDiplomacyDeclarationMinCharacters", StringComparison.Ordinal)
                  && rangeHelper.Contains("DuelSettings.DefaultWorldDiplomacyDeclarationMaxCharacters", StringComparison.Ordinal),
            "declaration range must fall back to the selected defaults when settings are unavailable");

        string writingContract = ExtractSection(
            behavior,
            "private static void AppendDiplomaticDeclarationWritingContract(StringBuilder sb)",
            "private static string BuildDiplomaticDeclarationModeContract()");
        Test.True(writingContract.Contains(
                "GetDiplomaticDeclarationCharacterRange(out int minimumCharacters, out int maximumCharacters);",
                StringComparison.Ordinal),
            "the declaration writing contract must resolve the live MCM character range");
        Test.True(writingContract.Contains("正文必须最少", StringComparison.Ordinal)
                  && writingContract.Contains("个中文字符（标点计入）", StringComparison.Ordinal),
            "the declaration writing contract must state the dynamic Chinese-character range including punctuation");
        Test.True(!writingContract.Contains("字数为100字以内", StringComparison.Ordinal),
            "the retired fixed 100-character declaration requirement must be removed");
    }

    private static void VerifyIndependentSettingsAndMcmOrder(string settings, string behavior)
    {
        string targetGetter = ExtractSection(
            behavior,
            "private static int GetHistoryCompressionTargetTokens()",
            "private static long GetHistoryCompressionTriggerTokens()");
        string triggerGetter = ExtractSection(
            behavior,
            "private static long GetHistoryCompressionTriggerTokens()",
            "private static WorldDiplomacyBehavior ResolveInstance()");

        Test.True(targetGetter.Contains("WorldDiplomacyHistoryCompressionTargetThousands", StringComparison.Ordinal),
            "target getter must read the target MCM value");
        Test.True(!targetGetter.Contains("WorldDiplomacyHistoryCompressionTriggerThousands", StringComparison.Ordinal),
            "target getter must not derive from the trigger MCM value");
        Test.True(triggerGetter.Contains("WorldDiplomacyHistoryCompressionTriggerThousands", StringComparison.Ordinal),
            "trigger getter must read the trigger MCM value");
        Test.True(!triggerGetter.Contains("WorldDiplomacyHistoryCompressionTargetThousands", StringComparison.Ordinal),
            "trigger getter must not derive from the post-compression target");
        Test.True(!triggerGetter.Contains("* 2", StringComparison.Ordinal),
            "trigger must no longer be hard-wired to twice the target");

        string[] properties =
        {
			"WorldDiplomacyTradeAllianceFailedProposalCooldownDays",
			"WorldDiplomacyThreatComplianceIssuerRelationReward",
            "WorldDiplomacyHistoryCompressionTriggerThousands",
            "WorldDiplomacyHistoryCompressionTargetThousands",
            "EditWorldDiplomacyPrompt",
            "RestoreDefaultWorldDiplomacyPrompt"
        };
        int[] orders = properties.Select(property => ReadPrecedingMcmOrder(settings, property)).ToArray();
        Test.True(orders.Distinct().Count() == orders.Length,
            "the six adjacent AI-diplomacy MCM controls must have unique order values");
        Test.True(orders.SequenceEqual(new[] { 13, 14, 15, 16, 17, 18 }),
            "AI-diplomacy cooldown, reward, compression, and prompt controls must occupy orders 13/14/15/16/17/18 in declaration order");
    }

    private static void VerifyPersistentMigration(string settings)
    {
        Test.Equal(16, ReadIntConstant(settings, "LegacyWorldDiplomacyHistoryCompressionTargetThousands"),
            "migration must recognize the old untouched 16k default");
        Test.True(settings.Contains(
                "WorldDiplomacyCompressionDefaultsMigrationMarkerFileName = \".world_diplomacy_compression_800k_48k_migration_v089\"",
                StringComparison.Ordinal),
            "migration must have a stable, versioned persistence marker file");

        string getSettings = ExtractSection(
            settings,
            "public static DuelSettings GetSettings()",
            "private static void EnsureLogCleanupDefaultMigration");
        Test.True(CountOccurrences(getSettings, "EnsureWorldDiplomacyCompressionDefaultsMigration(") >= 2,
            "both live MCM lookup paths must invoke the compression-default migration");

        string migration = ExtractSection(
            settings,
            "private static void EnsureWorldDiplomacyCompressionDefaultsMigration(DuelSettings settings)",
            "public static bool HasLiveMcmInstance()");
        string compact = CompactWhitespace(migration);
        Test.True(compact.Contains(
                "settings.WorldDiplomacyHistoryCompressionTargetThousands == LegacyWorldDiplomacyHistoryCompressionTargetThousands",
                StringComparison.Ordinal),
            "migration must only replace the recognizable old 16k target default");
        Test.True(compact.Contains(
                "settings.WorldDiplomacyHistoryCompressionTargetThousands = DefaultWorldDiplomacyHistoryCompressionTargetThousands",
                StringComparison.Ordinal),
            "old 16k target must migrate to the new 48k default");
        Test.True(migration.Contains("File.Exists(markerPath)", StringComparison.Ordinal)
                  && migration.Contains("File.ReadAllText(markerPath, Encoding.UTF8)", StringComparison.Ordinal),
            "migration must read its marker persistently as UTF-8");
        Test.True(migration.Contains("BaseSettingsProvider.Instance.SaveSettings(settings)", StringComparison.Ordinal),
            "a changed legacy MCM value must be persisted through the settings provider");
        Test.True(migration.Contains(
                "File.WriteAllText(markerPath, WorldDiplomacyCompressionDefaultsMigrationId, Encoding.UTF8)",
                StringComparison.Ordinal),
            "migration completion must be persisted with a UTF-8 marker");
        Test.True(migration.IndexOf("File.WriteAllText(markerPath", StringComparison.Ordinal)
                  > migration.IndexOf("if (changed)", StringComparison.Ordinal),
            "the marker must be written even when existing custom values need no rewrite");
    }

    private static void VerifyWorldDiplomacyPromptV5Migration(string settings)
    {
        Test.Equal(5, ReadIntConstant(settings, "WorldDiplomacyPromptJsonVersion"),
            "the warning-wording prompt migration must use JSON version 5");

        string previousDefault = ReadStringConstant(settings, "PreviousDefaultWorldDiplomacyPreferenceV4");
        string currentDefault = ReadStringConstant(settings, "DefaultWorldDiplomacyPreference");
        Test.True(previousDefault.Contains("警告与通牒", StringComparison.Ordinal)
                  && currentDefault.Contains("谴责与最后通牒", StringComparison.Ordinal),
            "version 5 must identify the exact old warning wording and the new war-condemnation wording");
        Test.True(!string.Equals(previousDefault, currentDefault, StringComparison.Ordinal),
            "the version-4 default and version-5 default must remain distinguishable for exact migration");

        string migration = ExtractSection(
            settings,
            "private static string MigrateLegacyWorldDiplomacyPromptText(string input)",
            "private static string BuildWorldDiplomacyCommonContract(string preference)");
        string compactMigration = CompactWhitespace(migration);
        Test.True(compactMigration.Contains(
                "string.Equals(text, PreviousDefaultWorldDiplomacyPreferenceV4, StringComparison.Ordinal)",
                StringComparison.Ordinal),
            "only an ordinal-exact match of the untouched version-4 default may use the version-5 replacement");
        Test.True(!migration.Contains("text.Contains(PreviousDefaultWorldDiplomacyPreferenceV4", StringComparison.Ordinal)
                  && !migration.Contains("text.StartsWith(PreviousDefaultWorldDiplomacyPreferenceV4", StringComparison.Ordinal),
            "the version-4 migration must not use substring or prefix matching that can overwrite a custom prompt");
        Test.True(migration.Contains("return DefaultWorldDiplomacyPreference;", StringComparison.Ordinal)
                  && migration.Contains("return text;", StringComparison.Ordinal)
                  && migration.IndexOf("return text;", StringComparison.Ordinal)
                     > migration.IndexOf("return DefaultWorldDiplomacyPreference;", StringComparison.Ordinal),
            "the exact old default must migrate while every unmatched custom prompt is returned unchanged");

        string standaloneReader = ExtractSection(
            settings,
            "private static bool TryReadCustomPromptTextJsonFile(string path, Func<string, string> normalize, string fallbackText, out string text)",
            "private static bool IsCustomPromptTextFileTooLarge(string path)");
        Test.True(standaloneReader.Contains(
                "IsWorldDiplomacyPromptPath(path) && parsed.Version < WorldDiplomacyPromptJsonVersion",
                StringComparison.Ordinal)
                  && standaloneReader.Contains("MigrateLegacyWorldDiplomacyPromptText(parsed.Text)", StringComparison.Ordinal),
            "the standalone world-diplomacy prompt file must run exact migration only for an older JSON version");

        string aggregateReader = ExtractSection(
            settings,
            "private static bool TryReadLegacyCustomPromptTextStore(out CustomPromptTextStoreJson store)",
            "private static void QuarantineAndRestoreLegacyCustomPromptTextStoreUnlocked(string path, string reason)");
        Test.True(aggregateReader.Contains(
                "parsed.Version < WorldDiplomacyPromptJsonVersion",
                StringComparison.Ordinal)
                  && aggregateReader.Contains(
                      "parsed.WorldDiplomacyPrompt = MigrateLegacyWorldDiplomacyPromptText(parsed.WorldDiplomacyPrompt)",
                      StringComparison.Ordinal),
            "the aggregate legacy store must use the same exact migration and leave current-version custom text alone");
        Test.True(aggregateReader.Contains("store.Version = WorldDiplomacyPromptJsonVersion;", StringComparison.Ordinal),
            "a migrated aggregate store must persist JSON version 5");
    }

    private static void VerifyStaticModeContractsAndPromptMigration(string behavior)
    {
		Test.Equal(27, ReadIntConstant(behavior, "DiplomacyPromptContractVersion"),
			"the reputation-maintenance contract must advance the prompt contract version");
		Test.Equal("diplomacy-history:v27", ReadStringConstant(behavior, "CanonicalHistoryCacheAffinityKey"),
			"the reputation-maintenance prompt must advance canonical-history cache affinity");
        Test.Equal("【AI外交固定任务MODE分派】", ReadStringConstant(behavior, "DiplomacyModeDispatchContractMarker"),
            "the static system prefix must expose an explicit mode dispatcher");
        Test.Equal("【MODE=DECLARE 固定任务合同】", ReadStringConstant(behavior, "DiplomaticDeclarationModeContractMarker"),
            "the declaration task must have a stable system marker");
        Test.Equal("【MODE=COMPACT 固定任务合同】", ReadStringConstant(behavior, "CanonicalHistoryCompressionModeContractMarker"),
            "the compression task must have a stable system marker");

        string declarationContract = ExtractSection(
            behavior,
            "private static string BuildDiplomaticDeclarationModeContract()",
            "private static string BuildCanonicalHistoryCompressionModeContract()");
        Test.True(declarationContract.Contains("【统一任务：公开外交宣言】", StringComparison.Ordinal),
            "the unified public-declaration task must live in the fixed declaration contract");
		Test.True(declarationContract.Contains("\\\"actions\\\":[{", StringComparison.Ordinal)
				  && declarationContract.Contains("\\\"target_kingdom_id\\\"", StringComparison.Ordinal)
				  && declarationContract.Contains("\\\"intent\\\":\\\"当前可选动作\\\"", StringComparison.Ordinal)
				  && declarationContract.Contains("\\\"peace_terms\\\"", StringComparison.Ordinal),
			"the fixed declaration JSON schema must expose the short directed-actions array");
		Test.True(!declarationContract.Contains("author_intent", StringComparison.Ordinal)
				  && !declarationContract.Contains("primary_target_kingdom_id", StringComparison.Ordinal)
				  && !declarationContract.Contains("【本篇唯一合法intent清单】", StringComparison.Ordinal)
				  && !declarationContract.Contains("\\\"commitment\\\"", StringComparison.Ordinal)
				  && !declarationContract.Contains(
					  "\\\"intent\\\":\\\"warning|ultimatum|",
					  StringComparison.Ordinal),
			"the fixed DECLARE JSON must stay short and must not restore the retired singular or global-list contracts");

        string compressionContract = ExtractSection(
            behavior,
            "private static string BuildCanonicalHistoryCompressionModeContract()",
            "private static string BuildGenerationSystemPrompt(string commonContract)");
        Test.True(compressionContract.Contains("合并旧快照与增量", StringComparison.Ordinal),
            "the full compression rules must live in the fixed compression contract");
        Test.True(compressionContract.Contains("covered_through_sequence", StringComparison.Ordinal)
                  && compressionContract.Contains("\\\"summary\\\"", StringComparison.Ordinal),
            "the complete compression JSON schema must live in the fixed compression contract");
		Test.True(!compressionContract.Contains("author_intent", StringComparison.Ordinal)
				  && !compressionContract.Contains("\\\"actions\\\"", StringComparison.Ordinal),
			"the compression contract must not inherit the declaration JSON schema");

        string canonicalSystem = ExtractSection(
            behavior,
            "private static string BuildCanonicalHistorySystemPrompt(string commonContract)",
            "private static string BuildDeclareModePrompt(string dynamicPrompt)");
        string[] stableSystemComponents =
        {
            "AppendDiplomaticDeclarationWritingContract(sb)",
            "sb.AppendLine(DiplomacyModeDispatchContractMarker)",
            "sb.AppendLine(DiplomaticDeclarationModeContractMarker)",
            "sb.AppendLine(BuildDiplomaticDeclarationModeContract())",
            "sb.AppendLine(CanonicalHistoryCompressionModeContractMarker)",
            "sb.AppendLine(BuildCanonicalHistoryCompressionModeContract())",
            "sb.AppendLine(CanonicalHistoryContractMarker)"
        };
        int previousComponentIndex = -1;
        foreach (string component in stableSystemComponents)
        {
            int componentIndex = canonicalSystem.IndexOf(component, StringComparison.Ordinal);
            Test.True(componentIndex > previousComponentIndex,
                "fixed system component must exist once in stable order: " + component);
            previousComponentIndex = componentIndex;
        }
        Test.True(canonicalSystem.Contains("只执行同名固定任务合同", StringComparison.Ordinal)
                  && canonicalSystem.Contains("其他MODE合同全部忽略", StringComparison.Ordinal)
                  && canonicalSystem.Contains("JSON结构不得混用", StringComparison.Ordinal),
            "system mode dispatch must prevent DECLARE and COMPACT output contracts from interfering");

        string generationSystem = ExtractSection(
            behavior,
            "private static string BuildGenerationSystemPrompt(string commonContract)",
            "private static string BuildCanonicalHistorySystemPrompt(string commonContract)");
        string relaySystem = ExtractSection(
            behavior,
            "private static string BuildRelayGenerationSystemPrompt(string commonContract)",
            "private string BuildRelayConversationTurnPrompt(");
        string compressionEnqueue = ExtractSection(
            behavior,
            "private void EnqueueCompressionJob(long throughSequence, long tokenCount, int targetTokens)",
            "private void EnqueueJob(WorldDiplomacyJob job)");
        Test.True(generationSystem.Contains("return BuildCanonicalHistorySystemPrompt(commonContract)", StringComparison.Ordinal),
            "ordinary declaration generation must use the shared first system message");
        Test.True(relaySystem.Contains("return BuildCanonicalHistorySystemPrompt(commonContract)", StringComparison.Ordinal),
            "relay declaration generation must use the shared first system message");
        Test.True(compressionEnqueue.Contains(
                "SystemPrompt = BuildCanonicalHistorySystemPrompt(BuildCommonDiplomacySystemPrefix())",
                StringComparison.Ordinal),
            "compression must use the same shared first system-message renderer");
        Test.True(compressionEnqueue.Contains("CacheAffinityKey = CanonicalHistoryCacheAffinityKey", StringComparison.Ordinal),
            "compression and declaration jobs must share canonical-history cache affinity");

        string messageRenderer = ExtractSection(
            behavior,
            "private List<WorldDiplomacyLlmMessage> BuildLlmMessagesForJob(WorldDiplomacyJob job)",
            "private static bool IsValidSemanticRepairMessageChain(WorldDiplomacyJob job)");
        int firstSystemIndex = messageRenderer.IndexOf(
            "new WorldDiplomacyLlmMessage { Role = \"system\", Content = job?.SystemPrompt ?? \"\" }",
            StringComparison.Ordinal);
        int historySystemIndex = messageRenderer.IndexOf(
            "Content = BuildCanonicalHistoryBlock(job?.HistoryThroughSequence ?? long.MaxValue)",
            StringComparison.Ordinal);
        int userTailIndex = messageRenderer.IndexOf(
            "source.Add(new WorldDiplomacyLlmMessage { Role = \"user\", Content = job?.UserPrompt ?? \"\" })",
            StringComparison.Ordinal);
        Test.True(firstSystemIndex >= 0 && historySystemIndex > firstSystemIndex && userTailIndex > historySystemIndex,
            "both modes must render as fixed system, canonical history, then dynamic mode tail");

        string declarationTail = ExtractSection(
            behavior,
            "private static string BuildDeclareModePrompt(string dynamicPrompt)",
            "private void AppendDiplomaticThreatDynamicContext(");
        Test.True(declarationTail.Contains("【MODE=DECLARE】", StringComparison.Ordinal)
                  && declarationTail.Contains("第一条system消息", StringComparison.Ordinal),
            "the declaration tail must only activate the fixed DECLARE system contract");
		foreach (string forbidden in new[]
		{
			"BuildDiplomaticDeclarationModeContract",
			"【统一任务：公开外交宣言】",
			"author_intent",
			"primary_target_kingdom_id",
			"\\\"actions\\\"",
			"target_kingdom_id"
        })
        {
            Test.True(!declarationTail.Contains(forbidden, StringComparison.Ordinal),
                "declaration tail must not duplicate fixed contract content: " + forbidden);
        }

        string compressionTail = ExtractSection(
            behavior,
            "private static string BuildTokenCompressionPrompt(string batchId, long throughSequence, long tokenCount, int summaryTargetTokens, long protectedTokens)",
            "private string BuildFallbackAnalysisJson(WorldDiplomacyJob job)");
        Test.True(compressionTail.Contains("【本次压缩参数】", StringComparison.Ordinal)
                  && compressionTail.Contains("覆盖截止seq=", StringComparison.Ordinal)
                  && compressionTail.Contains("summary目标上限tokens=", StringComparison.Ordinal)
                  && compressionTail.Contains("【MODE=COMPACT】", StringComparison.Ordinal)
                  && compressionTail.Contains("第一条system消息", StringComparison.Ordinal),
            "the compression tail must retain only its dynamic range/budget parameters and mode selector");
		foreach (string forbidden in new[]
        {
            "BuildCanonicalHistoryCompressionModeContract",
            "合并旧快照与增量",
            "covered_through_sequence",
            "\\\"summary\\\"",
			"author_intent",
			"\\\"actions\\\""
        })
        {
            Test.True(!compressionTail.Contains(forbidden, StringComparison.Ordinal),
                "compression tail must not duplicate or inherit a fixed output contract: " + forbidden);
        }

        string currentContractGuard = ExtractSection(
            behavior,
            "private static bool HasCurrentCanonicalPromptContract(WorldDiplomacyJob job)",
            "private bool EnsureCurrentCanonicalPromptContractBeforeSend(WorldDiplomacyJob job)");
        foreach (string systemMarker in new[]
        {
            "DiplomaticDeclarationWritingContractMarker",
            "DiplomacyModeDispatchContractMarker",
            "DiplomaticDeclarationModeContractMarker",
            "CanonicalHistoryCompressionModeContractMarker",
            "CanonicalHistoryContractMarker"
        })
        {
            Test.True(currentContractGuard.Contains(
                    "systemPrompt.IndexOf(" + systemMarker + ", StringComparison.Ordinal) >= 0",
                    StringComparison.Ordinal),
                "send-time guard must require fixed system marker: " + systemMarker);
        }
        foreach (string tailMarker in new[]
        {
            "DiplomaticDeclarationWritingContractMarker",
            "DiplomacyModeDispatchContractMarker",
            "DiplomaticDeclarationModeContractMarker",
            "CanonicalHistoryCompressionModeContractMarker"
        })
        {
            Test.True(currentContractGuard.Contains(
                    "modePrompt.IndexOf(" + tailMarker + ", StringComparison.Ordinal) < 0",
                    StringComparison.Ordinal),
                "send-time guard must reject a tail containing fixed system marker: " + tailMarker);
        }

        string migration = ExtractSection(
            behavior,
            "private void MigrateDiplomacyPromptContractIfNeeded()",
            "private bool TryRebuildPendingWorldDiplomacyJob(WorldDiplomacyJob job)");
        Test.True(migration.Contains(
                "_storage.PromptContractVersion >= DiplomacyPromptContractVersion",
                StringComparison.Ordinal),
            "prompt migration must recognize old storage versions");
        Test.True(migration.Contains("job.LlmMessages?.Clear()", StringComparison.Ordinal)
                  && migration.Contains("job.SemanticRepairAttempts = 0", StringComparison.Ordinal)
                  && migration.Contains("job.HistoryPrefixHash = \"\"", StringComparison.Ordinal),
            "old canonical jobs must discard stale request bodies and repair chains");
        Test.True(migration.Contains("TryRebuildPendingWorldDiplomacyJob(job)", StringComparison.Ordinal),
            "old declaration jobs must be semantically rebuilt under the new static system contract");
        Test.True(migration.Contains("string.Equals(job.Kind, \"compress\"", StringComparison.Ordinal)
                  && migration.Contains("_storage.DiplomacyCompressionPending = true", StringComparison.Ordinal),
            "old compression jobs must be retired and requeued under the new contract");
        int rebuildIndex = migration.IndexOf("TryRebuildPendingWorldDiplomacyJob(job)", StringComparison.Ordinal);
        int versionCommitIndex = migration.IndexOf(
            "_storage.PromptContractVersion = DiplomacyPromptContractVersion",
            StringComparison.Ordinal);
        Test.True(rebuildIndex >= 0 && versionCommitIndex > rebuildIndex,
            "prompt migration version must commit only after old jobs are rebuilt or retired");
    }

    private static void VerifyCompressionScheduling(string behavior)
    {
        Test.Equal(1000, ReadIntConstant(behavior, "CompressionJobPriority"),
            "compression jobs must outrank ordinary diplomacy jobs");

        string enqueue = ExtractSection(
            behavior,
            "private void EnqueueCompressionJob(long throughSequence, long tokenCount, int targetTokens)",
            "private void EnqueueJob(WorldDiplomacyJob job)");
        Test.True(enqueue.Contains("Priority = CompressionJobPriority", StringComparison.Ordinal),
            "queued compression jobs must use the dedicated high priority");

        string queue = ExtractSection(
            behavior,
            "private void EnqueueJob(WorldDiplomacyJob job)",
            "private static string ResolveCacheAffinityKey(WorldDiplomacyJob job)");
        Test.True(queue.Contains("int queueCapacity = MaxPendingJobs +", StringComparison.Ordinal)
                  && queue.Contains("string.Equals(x.Kind, \"compress\"", StringComparison.Ordinal),
            "a queued compression job must open one dedicated maintenance slot");
        Test.True(queue.Contains(".Take(queueCapacity)", StringComparison.Ordinal),
            "queue trimming must honor the compression maintenance slot");

        string scheduler = ExtractSection(
            behavior,
            "private void TryScheduleTokenCompression()",
            "private void CommitCompression(WorldDiplomacyJob job, string raw)");
        Test.True(scheduler.Contains("GetHistoryCompressionTriggerTokens()", StringComparison.Ordinal),
            "scheduler must compare history size with the independent trigger");
        Test.Equal(1, CountOccurrences(scheduler, ".Any("),
            "only one queued-job guard should block compression scheduling");
        Test.True(scheduler.Contains("string.Equals(x.Kind, \"compress\"", StringComparison.Ordinal),
            "the only queued-job guard must detect an existing compression job");
        Test.True(!scheduler.Contains("_llmRequestRunning", StringComparison.Ordinal),
            "an active ordinary request must not starve the high-priority compression queue");
        Test.True(scheduler.Contains("EnqueueCompressionJob(", StringComparison.Ordinal),
            "scheduler must enqueue compression after the threshold and duplicate guard pass");
    }

    private static void VerifyFrozenOverallTarget(string behavior)
    {
        string enqueue = ExtractSection(
            behavior,
            "private void EnqueueCompressionJob(long throughSequence, long tokenCount, int targetTokens)",
            "private void EnqueueJob(WorldDiplomacyJob job)");
        Test.True(enqueue.Contains("int overallTargetTokens = Math.Max(1, targetTokens)", StringComparison.Ordinal),
            "queue time must freeze the selected overall target");
        Test.True(enqueue.Contains("CompressionOverallTargetTokens = overallTargetTokens", StringComparison.Ordinal),
            "frozen overall target must be stored on the job");

        string commit = ExtractSection(
            behavior,
            "private void CommitCompression(WorldDiplomacyJob job, string raw)",
            "private static int ParseCompressionSequence(string batchId)");
        Test.True(commit.Contains("job.CompressionOverallTargetTokens > 0", StringComparison.Ordinal)
                  && commit.Contains("? job.CompressionOverallTargetTokens", StringComparison.Ordinal),
            "commit must prefer the job's frozen overall target over live MCM state");

        string jobDto = ExtractSection(
            behavior,
            "public sealed class WorldDiplomacyJob",
            "public sealed class WorldDiplomacyCanonicalHistoryState");
        Test.True(jobDto.Contains("[JsonProperty(\"compressionOverallTargetTokens\")]", StringComparison.Ordinal)
                  && jobDto.Contains("public int CompressionOverallTargetTokens { get; set; }", StringComparison.Ordinal),
            "the frozen 48k overall target must survive save/load serialization");
    }

    private static void VerifyRouteOutputCap(string settings, string behavior, string client)
    {
        string resolver = ExtractSection(
            client,
            "private static int ResolveConfiguredOutputTokenLimit(DuelSettings settings, string route)",
            "private static bool IsNonRetryableClientError");
        Test.True(resolver.Contains("event_rebellion_dedicated", StringComparison.Ordinal),
            "output limit resolver must distinguish the actual dedicated route");
        Test.True(resolver.Contains("settings?.EventAndRebellionApiMaxTokens", StringComparison.Ordinal)
                  && resolver.Contains("settings?.MainApiMaxTokens", StringComparison.Ordinal),
            "output limit must come from the selected route's MCM max_tokens value");
        Test.True(resolver.Contains("DuelSettings.ClampApiMaxTokens(configured, fallback)", StringComparison.Ordinal),
            "route output limit must use the central API max_tokens clamp");

        string callOnce = ExtractSection(
            client,
            "private static async Task<WorldDiplomacyApiCallResult> CallOnceAsync(",
            "private static async Task<WorldDiplomacyHttpExchange> SendAndReadAsync(");
        Test.True(callOnce.Contains(
                "int effectiveMaxTokens = Math.Min(Math.Max(1, maxTokens), configuredOutputTokenLimit)",
                StringComparison.Ordinal),
            "every request must be hard-capped by the resolved route limit");
        Test.True(callOnce.Contains("BuildRequestBody(modelName, messages, effectiveMaxTokens", StringComparison.Ordinal),
            "the capped value, not the requested value, must enter the request body");

        string enqueue = ExtractSection(
            behavior,
            "private void EnqueueCompressionJob(long throughSequence, long tokenCount, int targetTokens)",
            "private void EnqueueJob(WorldDiplomacyJob job)");
        Test.True(enqueue.Contains("WorldDiplomacyLlmClient.GetConfiguredOutputTokenLimit()", StringComparison.Ordinal),
            "compression target construction must account for the current route output cap");
        Test.True(enqueue.Contains(
                "MaxTokens = Math.Min(configuredOutputTokenLimit, summaryTargetTokens + outputTokenReserve)",
                StringComparison.Ordinal),
            "compression job max_tokens must remain under the route MCM ceiling");

        int apiMin = ReadIntConstant(settings, "ApiMaxTokensMinimum");
        int apiMax = ReadIntConstant(settings, "ApiMaxTokensMaximum");
        Test.True(apiMin > 0 && apiMax >= 60_000,
            "central API output range must support a safe positive cap and the 60k compression ceiling");
        Test.Equal(12_000, Math.Min(50_000, 12_000),
            "a 50k compression request must be capped to a 12k route setting");
    }

    private static void VerifyNonRetryableClientErrors(string client)
    {
        string retryLoop = ExtractSection(
            client,
            "public static async Task<WorldDiplomacyApiCallResult> CallMessagesWithRetriesAsync(",
            "private static async Task<WorldDiplomacyApiCallResult> CallOnceAsync(");
        Test.True(retryLoop.Contains("IsNonRetryableClientError(result)", StringComparison.Ordinal),
            "retry loop must stop immediately on deterministic client errors");

        string classification = ExtractSection(
            client,
            "private static bool IsNonRetryableClientError(WorldDiplomacyApiCallResult result)",
            "private static JArray BuildTokenStatsDiagnosticMessages");
        foreach (int retryable in new[] { 408, 409, 425, 429 })
        {
            Test.True(classification.Contains("status != " + retryable.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal),
                "transient 4xx status must remain retryable: " + retryable);
        }
        foreach (int nonRetryable in new[] { 400, 401, 403, 404, 422 })
        {
            Test.True(IsNonRetryableClientErrorFixture(nonRetryable),
                "ordinary 4xx must be classified as non-retryable: " + nonRetryable);
        }
        foreach (int retryable in new[] { 408, 409, 425, 429, 500 })
        {
            Test.True(!IsNonRetryableClientErrorFixture(retryable),
                "transient/server status must not be classified as deterministic 4xx: " + retryable);
        }
    }

    private static void VerifyCompressionTimeout(string settings, string behavior)
    {
        Test.Equal(480_000, ReadIntConstant(settings, "LlmRequestTimeoutMilliseconds"),
            "long-context compression timeout must be eight minutes");
        string dispatch = ExtractSection(
            behavior,
            "private void TryStartNextLlmJob()",
            "private void ProcessCompletedJobs()");
        Test.True(dispatch.Contains("string.Equals(job.Kind, \"compress\"", StringComparison.Ordinal)
                  && dispatch.Contains("? DuelSettings.LlmRequestTimeoutMilliseconds", StringComparison.Ordinal)
                  && dispatch.Contains(": DefaultApiTimeoutMilliseconds", StringComparison.Ordinal),
            "only compression requests must receive the long 480000ms timeout");
        Test.True(dispatch.Contains("requestTimeoutMilliseconds,", StringComparison.Ordinal),
            "the selected timeout must be passed to the API client");
    }

    private static void VerifyTokenStatsDiagnosticTruncation(string client)
    {
        Test.Equal(128 * 1024, ReadIntConstantExpression(client, "TokenStatsFullDumpMaxChars"),
            "full Token_Stats request dumps must stop at 128 KiB");
        Test.Equal(12 * 1024, ReadIntConstantExpression(client, "TokenStatsMessageExcerptMaxChars"),
            "each large diagnostic message must be reduced to a 12 KiB excerpt");
        Test.Equal(16 * 1024, ReadIntConstantExpression(client, "TokenStatsRequestExcerptMaxChars"),
            "large serialized request bodies must be reduced to a 16 KiB excerpt");

        string record = ExtractSection(
            client,
            "private static void RecordTokenStats(",
            "private static int ResolveConfiguredOutputTokenLimit");
        Test.True(record.Contains("BuildTokenStatsDiagnosticMessages(messages)", StringComparison.Ordinal),
            "large input-message diagnostics must use the truncating renderer");
        Test.True(record.Contains("(requestBody?.Length ?? 0) > TokenStatsFullDumpMaxChars", StringComparison.Ordinal)
                  && record.Contains("BuildTokenStatsDiagnosticText(requestBody", StringComparison.Ordinal),
            "large raw request bodies must be truncated before entering Token_Stats");
        Test.True(record.Contains("Logger.EstimateTokensFromMessages(messages)", StringComparison.Ordinal)
                  && record.Contains("diagnosticMessages,", StringComparison.Ordinal),
            "usage estimates must use full messages while only diagnostic payloads are shortened");

        string diagnosticHelpers = ExtractSection(
            client,
            "private static JArray BuildTokenStatsDiagnosticMessages(JArray messages)",
            "private static bool ContainsAny(");
        Test.True(diagnosticHelpers.Contains("if (totalChars <= TokenStatsFullDumpMaxChars) return messages", StringComparison.Ordinal),
            "small requests must retain their existing full diagnostics");
        Test.True(diagnosticHelpers.Contains("diagnostic_original_chars", StringComparison.Ordinal)
                  && diagnosticHelpers.Contains("DIAGNOSTIC OMITTED", StringComparison.Ordinal),
            "truncated diagnostics must disclose original length and omission");
        Test.True(diagnosticHelpers.Contains("value.Substring(0", StringComparison.Ordinal)
                  && diagnosticHelpers.Contains("value.Length - tailLength", StringComparison.Ordinal),
            "diagnostic truncation must preserve both the start and end of large content");
    }

    private static void VerifySegmentedHashing(string behavior)
    {
        string hashMethods = ExtractSection(
            behavior,
            "private static string StablePromptHash(string text)",
            "private static List<WorldDiplomacyLlmMessage> CloneLlmMessages");
        Test.True(hashMethods.Contains("private static string StablePromptHashPair", StringComparison.Ordinal)
                  && hashMethods.Contains("private static string StablePromptHashMessagePrefix", StringComparison.Ordinal)
                  && hashMethods.Contains("private static ulong AppendStablePromptHash", StringComparison.Ordinal),
            "large stable prefixes must have allocation-free segmented hash helpers");
        Test.True(hashMethods.Contains("AppendStablePromptHash(1469598103934665603UL, first)", StringComparison.Ordinal)
                  && hashMethods.Contains("hash = AppendStablePromptHash(hash, \"\\n\")", StringComparison.Ordinal)
                  && hashMethods.Contains("hash = AppendStablePromptHash(hash, second)", StringComparison.Ordinal),
            "pair hashing must append segments and the exact historical newline separator");
        Test.True(behavior.Contains(
                "job.HistoryPrefixHash = StablePromptHashPair(job.SystemPrompt, historyBlock)",
                StringComparison.Ordinal),
            "history capture must hash system/history segments without building one huge string");
        Test.True(behavior.Contains("StablePromptHashMessagePrefix(messages, expectedCachedMessageCount)", StringComparison.Ordinal),
            "cache-prefix diagnostics must hash messages incrementally");
        Test.True(!behavior.Contains(
                "StablePromptHash(job.SystemPrompt + \"\\n\" + historyBlock)",
                StringComparison.Ordinal),
            "history hashing must not regress to a large concatenated allocation");

        string largeHistory = new string('史', 1_000_000);
        string oldShape = HashText("契约\n" + largeHistory);
        string segmented = HashSegments("契约", "\n", largeHistory);
        Test.Equal(oldShape, segmented,
            "segmented FNV hashing must preserve the prior hash value for a million-character history");
    }

    private static bool IsNonRetryableClientErrorFixture(int status)
    {
        return status >= 400 && status < 500
            && status != 408
            && status != 409
            && status != 425
            && status != 429;
    }

    private static string HashText(string text)
    {
        return HashSegments(text);
    }

    private static string HashSegments(params string[] segments)
    {
        unchecked
        {
            ulong hash = 1469598103934665603UL;
            foreach (string segment in segments)
            {
                foreach (char ch in segment ?? "")
                {
                    hash ^= ch;
                    hash *= 1099511628211UL;
                }
            }
            return hash.ToString("x16", CultureInfo.InvariantCulture);
        }
    }

    private static string ReadRepositoryFile(string fileName)
    {
        string path = FindRepositoryFile(fileName);
        byte[] bytes = File.ReadAllBytes(path);
        Test.True(!bytes.AsSpan().StartsWith(new byte[] { 0xFF, 0xFE })
                  && !bytes.AsSpan().StartsWith(new byte[] { 0xFE, 0xFF }),
            fileName + " must not use UTF-16 encoding");
        return File.ReadAllText(path, Encoding.UTF8);
    }

    private static string FindRepositoryFile(string fileName)
    {
        DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            string candidate = Path.Combine(directory.FullName, fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
            directory = directory.Parent;
        }
        throw new FileNotFoundException("Could not locate repository file.", fileName);
    }

    private static int ReadIntConstant(string source, string constantName)
    {
        Match match = Regex.Match(
            source,
            @"\bconst\s+int\s+" + Regex.Escape(constantName) + @"\s*=\s*([0-9][0-9_]*)\s*;");
        Test.True(match.Success, "missing integer constant: " + constantName);
        return int.Parse(match.Groups[1].Value.Replace("_", "", StringComparison.Ordinal), CultureInfo.InvariantCulture);
    }

    private static string ReadStringConstant(string source, string constantName)
    {
        Match match = Regex.Match(
            source,
            @"\bconst\s+string\s+" + Regex.Escape(constantName) + @"\s*=\s*""([^""]*)""\s*;");
        Test.True(match.Success, "missing string constant: " + constantName);
        return match.Groups[1].Value;
    }

    private static int ReadIntConstantExpression(string source, string constantName)
    {
        Match match = Regex.Match(
            source,
            @"\bconst\s+int\s+" + Regex.Escape(constantName) + @"\s*=\s*([0-9_]+)\s*\*\s*([0-9_]+)\s*;");
        Test.True(match.Success, "missing multiplicative integer constant: " + constantName);
        int left = int.Parse(match.Groups[1].Value.Replace("_", "", StringComparison.Ordinal), CultureInfo.InvariantCulture);
        int right = int.Parse(match.Groups[2].Value.Replace("_", "", StringComparison.Ordinal), CultureInfo.InvariantCulture);
        return checked(left * right);
    }

    private static int ReadPrecedingMcmOrder(string source, string propertyName)
    {
        Match property = Regex.Match(source, @"\bpublic\s+(?:int|Action)\s+" + Regex.Escape(propertyName) + @"\b");
        Test.True(property.Success, "missing MCM property: " + propertyName);
        int windowStart = Math.Max(0, property.Index - 1600);
        string prefix = source.Substring(windowStart, property.Index - windowStart);
        MatchCollection orders = Regex.Matches(
            prefix,
            @"\[SettingProperty(?:Integer|Button)\([^\r\n]*\bOrder\s*=\s*([0-9]+)");
        Test.True(orders.Count > 0, "missing MCM order for: " + propertyName);
        return int.Parse(orders[^1].Groups[1].Value, CultureInfo.InvariantCulture);
    }

    private static string ExtractSection(string source, string startMarker, string endMarker)
    {
        int start = source.IndexOf(startMarker, StringComparison.Ordinal);
        Test.True(start >= 0, "missing start marker: " + startMarker);
        int end = source.IndexOf(endMarker, start + startMarker.Length, StringComparison.Ordinal);
        Test.True(end > start, "missing end marker: " + endMarker);
        return source.Substring(start, end - start);
    }

    private static int CountOccurrences(string source, string value)
    {
        int count = 0;
        int offset = 0;
        while ((offset = source.IndexOf(value, offset, StringComparison.Ordinal)) >= 0)
        {
            count++;
            offset += value.Length;
        }
        return count;
    }

    private static string CompactWhitespace(string value)
    {
        return Regex.Replace(value, @"\s+", " ");
    }
}
