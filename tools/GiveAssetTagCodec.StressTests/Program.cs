using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using AnimusForge;

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
    {
        _assertions++;
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(message + "; expected=" + expected + "; actual=" + actual);
        }
    }

    internal static int Assertions => _assertions;
}

internal static class Program
{
private static void AssertSingle(string asset, string quantity)
{
    string raw = "[ACTION:GIVE_ASSET:" + asset + ":" + quantity + "]";
    Test.True(GiveAssetTagCodec.TryParseWhole(raw, out GiveAssetTag tag), "must parse: " + raw);
    Test.Equal(asset, tag.AssetToken, "asset round-trip");
    Test.Equal(quantity, tag.QuantityToken, "quantity round-trip");
    Test.Equal(raw, tag.RawTag, "raw round-trip");
}

private static bool HasAmbiguousTerminator(string asset)
{
    return Regex.IsMatch(asset, ":(?:ALL|[0-9]+)\\]", RegexOptions.IgnoreCase);
}

private static int Main()
{
string[] importantNames =
{
    "[ROT]佛雷甲",
    "[ROT]贵族头饰",
    "[ROT]安达尔马鞍配钢面甲",
	"[ROT]西境骏马",
    "火焰之剑:北境版",
    "A]B",
    "[A:B]",
    "{#id=weapon}钢剑",
    "盔甲@稀有+3",
    "旧罩袍",
    "空 格　全角",
    "emoji🗡️护甲",
    "[ACTION:MOOD:ANNOYED]作为名称",
    "\\\\/|^$*+?.(){}\"'`~!@#%&_=,;<>"
};

foreach (string name in importantNames)
{
    AssertSingle(name, "1");
}
AssertSingle("任何物品", "ALL");
AssertSingle("任何物品", "all");
AssertSingle("物品:含多个:冒号", "007");

for (int code = 32; code <= 126; code++)
{
    char symbol = (char)code;
    AssertSingle("左" + symbol + "右", "1");
}

string first = "[ACTION:GIVE_ASSET:[ROT]佛雷甲:1]";
string second = "[ACTION:GIVE_ASSET:火焰:北境版:2]";
string combined = "前缀 " + first + second + " 后缀";
List<GiveAssetTag> tags = GiveAssetTagCodec.Extract(combined);
Test.Equal(2, tags.Count, "contiguous tags must stay separate");
Test.Equal("[ROT]佛雷甲", tags[0].AssetToken, "first contiguous asset");
Test.Equal("火焰:北境版", tags[1].AssetToken, "second contiguous asset");
Test.Equal("前缀  后缀", GiveAssetTagCodec.StripTags(combined), "strip must leave visible text intact");
Test.Equal("前缀 <[ROT]佛雷甲|1><火焰:北境版|2> 后缀", GiveAssetTagCodec.ReplaceTags(combined, tag => "<" + tag.AssetToken + "|" + tag.QuantityToken + ">"), "replace must preserve order and boundaries");

string malformedThenValid = "[ACTION:GIVE_ASSET:损坏:xyz]" + first;
tags = GiveAssetTagCodec.Extract(malformedThenValid);
Test.Equal(1, tags.Count, "malformed tag must not consume following valid tag");
Test.Equal("[ROT]佛雷甲", tags[0].AssetToken, "following valid tag must survive malformed predecessor");
Test.True(!GiveAssetTagCodec.TryParseWhole("[ACTION:GIVE_ASSET::1]", out _), "empty asset must be rejected");
Test.True(!GiveAssetTagCodec.TryParseWhole("[ACTION:GIVE_ASSET:物品:-1]", out _), "negative syntax must be rejected");
Test.True(!GiveAssetTagCodec.TryParseWhole("[ACTION:GIVE_ASSET:物品:一]", out _), "non-numeric quantity must be rejected");
Test.True(!GiveAssetTagCodec.TryParseWhole("[ACTION:GIVE_ASSET:物品:1\n]", out _), "line breaks must be rejected");
tags = GiveAssetTagCodec.Extract("[ACTION:GIVE_ASSET:损坏:1\n" + first);
Test.Equal(1, tags.Count, "line-broken malformed tag must not hide next-line valid tag");
Test.True(GiveAssetTagCodec.TryParseWhole("[ACTION:GIVE_ASSET:物品:0]", out _), "zero is syntactically parseable and must be rejected by quantity policy, not by boundary parsing");
Test.True(GiveAssetTagCodec.TryParseWhole("[ACTION:GIVE_ASSET:物品:999999999999999999999]", out _), "overflow quantity is syntactically parseable and must be rejected by executor policy");

// '[ACTION:GIVE_ASSET:' is the reserved introducer. If it appears after a malformed tag,
// it starts a fresh candidate rather than allowing a cross-tag accidental grant.
string nestedRecovery = "[ACTION:GIVE_ASSET:坏标签" + first;
tags = GiveAssetTagCodec.Extract(nestedRecovery);
Test.Equal(1, tags.Count, "nested introducer must recover the later candidate");
Test.Equal("[ROT]佛雷甲", tags[0].AssetToken, "later candidate after nested introducer");

var random = new Random(20260723);
const string alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 []:{}()<>!@#$%^&*-_=+;,.?/\\|\"'~`中文测试🗡️";
for (int index = 0; index < 20000; index++)
{
    string asset;
    do
    {
        int length = random.Next(1, 80);
        var builder = new StringBuilder(length);
        for (int charIndex = 0; charIndex < length; charIndex++)
        {
            builder.Append(alphabet[random.Next(alphabet.Length)]);
        }
        asset = builder.ToString();
    }
    while (HasAmbiguousTerminator(asset) || asset.IndexOf(GiveAssetTagCodec.Prefix, StringComparison.OrdinalIgnoreCase) >= 0);

    string quantity = index % 7 == 0 ? "ALL" : (index % 97 + 1).ToString();
    AssertSingle(asset, quantity);
}

var pressure = new StringBuilder(1_500_000);
for (int index = 0; index < 25000; index++)
{
    pressure.Append("正文");
    pressure.Append("[ACTION:GIVE_ASSET:[ROT]物品:");
    pressure.Append(index % 19 + 1);
    pressure.Append("]");
}
var stopwatch = Stopwatch.StartNew();
tags = GiveAssetTagCodec.Extract(pressure.ToString());
string stripped = GiveAssetTagCodec.StripTags(pressure.ToString());
stopwatch.Stop();
Test.Equal(25000, tags.Count, "pressure extraction count");
Test.Equal("正文".Length * 25000, stripped.Length, "pressure stripping result");
Test.True(stopwatch.Elapsed < TimeSpan.FromSeconds(10), "postprocess-sized parser pressure run exceeded 10 seconds: " + stopwatch.Elapsed);

string repoRoot = Directory.GetCurrentDirectory();
while (!File.Exists(Path.Combine(repoRoot, "MyBehavior.cs")))
{
    string? parent = Directory.GetParent(repoRoot)?.FullName;
    if (string.IsNullOrWhiteSpace(parent))
    {
        throw new InvalidOperationException("Could not find repository root.");
    }
    repoRoot = parent;
}

string myBehavior = File.ReadAllText(Path.Combine(repoRoot, "MyBehavior.cs"));
string shoutBehavior = File.ReadAllText(Path.Combine(repoRoot, "ShoutBehavior.cs"));
string rewardSystem = File.ReadAllText(Path.Combine(repoRoot, "RewardSystemBehavior.cs"));
Test.True(myBehavior.Contains("GiveAssetTagCodec.TryParseWhole", StringComparison.Ordinal) && myBehavior.Contains("GiveAssetTagCodec.ReplaceTags", StringComparison.Ordinal), "free-conversation parser integration missing");
Test.True(shoutBehavior.Contains("GiveAssetTagCodec.Extract", StringComparison.Ordinal) && shoutBehavior.Contains("GiveAssetTagCodec.StripTags", StringComparison.Ordinal), "scene/courier parser integration missing");
Test.True(rewardSystem.Contains("GiveAssetTagCodec.ReplaceTags", StringComparison.Ordinal) && rewardSystem.Contains("GiveAssetTagCodec.StripTags", StringComparison.Ordinal), "all reward execution parser integration missing");
Test.True(!rewardSystem.Contains("known_global_give_asset", StringComparison.Ordinal), "global fuzzy lookup must not replace a postprocess asset name");
Test.True(myBehavior.Contains("LooksLikeFixedAssetTransferIdForExternal", StringComparison.Ordinal)
    && myBehavior.Contains("TryResolveFixedAssetTransferEntryByIdForExternal", StringComparison.Ordinal)
    && rewardSystem.Contains("TryResolveFixedAssetTokenForGiveAsset", StringComparison.Ordinal)
    && rewardSystem.Contains("allowDirectFixedAssetIdOverride", StringComparison.Ordinal),
    "an exact fixed-asset ID outside the prompt snapshot must route to a real transfer before RP fallback");
Test.True(!myBehavior.Contains("if (!LooksLikeFixedAssetTransferIdForExternal(text))", StringComparison.Ordinal)
    && myBehavior.Contains("FindSettlementByExactRuntimeIdForFixedAssetTransfer", StringComparison.Ordinal)
    && rewardSystem.Contains("if (MyBehavior.TryResolveFixedAssetTransferEntryByIdForExternal(token, out entry))", StringComparison.Ordinal),
    "an exact custom Settlement.StringId must resolve as a fixed asset without a global settlement scan");
Match tournamentParticipantPromptContract = Regex.Match(myBehavior,
    @"private void RecordTournamentParticipantNpcActions\(.*?(?=\r?\n\s*private static Kingdom ResolveTournamentHostKingdom)",
    RegexOptions.Singleline);
Match tournamentParticipantSummaryContract = Regex.Match(myBehavior,
    @"private static string BuildTournamentParticipantSummary\(.*?(?=\r?\n\s*private static string GetTournamentPrizeDisplayName)",
    RegexOptions.Singleline);
Test.True(tournamentParticipantPromptContract.Success
    && tournamentParticipantPromptContract.Value.Contains("全部参赛者（含冠军）", StringComparison.Ordinal)
    && tournamentParticipantPromptContract.Value.Contains("冠军是", StringComparison.Ordinal)
    && tournamentParticipantPromptContract.Value.Contains("foreach (Hero tournamentHero in participantHeroes)", StringComparison.Ordinal)
    && tournamentParticipantPromptContract.Value.Contains("RecordNpcRecentAction(tournamentHero, participantActionText", StringComparison.Ordinal)
    && tournamentParticipantSummaryContract.Success
    && tournamentParticipantSummaryContract.Value.Contains("GetTournamentCharacterDisplayName(participant, \"未命名参赛者\")", StringComparison.Ordinal)
    && !tournamentParticipantSummaryContract.Value.Contains("list.Count >= 8", StringComparison.Ordinal)
    && !tournamentParticipantSummaryContract.Value.Contains(".Take(8)", StringComparison.Ordinal),
    "tournament prompt context must keep the champion and every participant, including non-Hero entrants, without an eight-person cap");
Match tournamentRankContract = Regex.Match(myBehavior,
    @"private static Dictionary<string, string> BuildTournamentParticipantRankLabels\(.*?(?=\r?\n\s*private static Kingdom ResolveTournamentHostKingdom)",
    RegexOptions.Singleline);
Test.True(tournamentRankContract.Success
    && tournamentRankContract.Value.Contains("Mission.Current?.GetMissionBehavior<TournamentBehavior>()", StringComparison.Ordinal)
    && tournamentRankContract.Value.Contains("missionBehavior.Settlement?.Town != town", StringComparison.Ordinal)
    && tournamentRankContract.Value.Contains("missionBehavior.Winner?.Character != winner", StringComparison.Ordinal)
    && tournamentRankContract.Value.Contains("foreach (TournamentMatch match in tournamentRound.Matches)", StringComparison.Ordinal)
    && tournamentRankContract.Value.Contains("foreach (TournamentParticipant participant in match.Participants)", StringComparison.Ordinal)
    && tournamentRankContract.Value.Contains("冠军（第1名）", StringComparison.Ordinal)
    && tournamentRankContract.Value.Contains("亚军（第2名）", StringComparison.Ordinal)
    && tournamentRankContract.Value.Contains("四强（并列第3名）", StringComparison.Ordinal)
    && tournamentRankContract.Value.Contains("八强（并列第5名）", StringComparison.Ordinal)
    && tournamentRankContract.Value.Contains("十六强（并列第9名）", StringComparison.Ordinal)
    && !tournamentRankContract.Value.Contains("GetLeaderBoardRank", StringComparison.Ordinal)
    && !tournamentRankContract.Value.Contains("TournamentParticipant.Score", StringComparison.Ordinal),
    "tournament placement prompt context must derive only truthful bracket tiers from the completed live tournament tree");
Test.True(rewardSystem.Contains("itemName = requestedName;", StringComparison.Ordinal), "generated RP item must report the requested postprocess name");
Test.True(rewardSystem.Contains("[RewardRpLiteral] generated", StringComparison.Ordinal), "literal RP generation diagnostic missing");
MatchCollection inventoryFirstRoutes = Regex.Matches(rewardSystem,
    @"bool isAuthorizedInventoryItem = TryResolveAuthorized(?:Hero|Party|Merchant)RewardItem\([^;]+;\s*bool isGeneratedRpItem = !isAuthorizedInventoryItem",
    RegexOptions.Singleline);
Test.Equal(3, inventoryFirstRoutes.Count, "all GIVE_ASSET paths must resolve the live exact inventory before RP fallback");
Test.True(rewardSystem.Contains("TryResolveExactAuthorizedRewardItem", StringComparison.Ordinal)
    && rewardSystem.Contains("authorizedItems = GetHeroInventoryItems(giver);", StringComparison.Ordinal), "hero GIVE_ASSET must check the current backpack with literal matching");
Test.True(rewardSystem.Contains("GeneratedRpEquipmentKind.Horse", StringComparison.Ordinal)
    && rewardSystem.Contains("ItemObject.ItemTypeEnum.Horse", StringComparison.Ordinal), "RP horse template category missing");
Test.True(rewardSystem.Contains("GeneratedRpEquipmentKind.Whip", StringComparison.Ordinal)
    && rewardSystem.Contains("\"马鞭\"", StringComparison.Ordinal)
    && rewardSystem.Contains("\"whip\"", StringComparison.Ordinal)
    && rewardSystem.Contains("IsGeneratedRpWhipWeaponTemplateItem", StringComparison.Ordinal), "RP whip semantic category guard missing");
Match whipSwordIsolation = Regex.Match(rewardSystem,
    @"bool isExplicitWhip = IsGeneratedRpWhipWeaponTemplateItem\(item\);.*?if \(isExplicitWhip\).*?GeneratedRpEquipmentKind\.Whip.*?return;.*?switch \(item\.Type\)",
    RegexOptions.Singleline);
Test.True(whipSwordIsolation.Success, "explicit whip templates must not enter the borrowed sword weapon-class pool");
string playerRpCrafting = File.ReadAllText(Path.Combine(repoRoot, "RewardSystemBehavior.PlayerRpCrafting.cs"));
string playerRpModels = File.ReadAllText(Path.Combine(repoRoot, "PlayerRpCraftModels.cs"));
string playerRpComponents = File.ReadAllText(Path.Combine(repoRoot, "PlayerRpCraftItemComponentService.cs"));
string preprocessPrompts = File.ReadAllText(Path.Combine(repoRoot, "AnimusForge", "ModuleData", "PreprocessPrompts.json"));
string terminalBehavior = File.ReadAllText(Path.Combine(repoRoot, "AnimusForgeTerminalBehavior.cs"));
string playerRpPopup = File.ReadAllText(Path.Combine(repoRoot, "PlayerRpForgePopup.cs"));
Test.True(playerRpCrafting.Contains("PlayerRpTemplateCandidateLimit = 50", StringComparison.Ordinal)
    && playerRpCrafting.Contains("TryBuildPlayerRpCraftTemplateSelectionForExternal(", StringComparison.Ordinal)
    && playerRpCrafting.Contains("Candidates = candidates", StringComparison.Ordinal)
    && terminalBehavior.Contains("candidate.TypeLabel", StringComparison.Ordinal)
    && terminalBehavior.Contains("candidate.StandardPrice", StringComparison.Ordinal)
    && playerRpModels.Contains("public string TypeLabel;", StringComparison.Ordinal)
    && playerRpModels.Contains("public int StandardPrice;", StringComparison.Ordinal),
    "player RP template selection must build a local Top 50 candidate snapshot with type and price");
Test.True(playerRpCrafting.Contains("TryPreviewPlayerRpCraftWithPlayerSelectedTemplateForExternal(", StringComparison.Ordinal)
    && playerRpCrafting.Contains("\"player_choice\"", StringComparison.Ordinal)
    && playerRpCrafting.Contains("所选模板不在当前 Top 50 安全候选中。", StringComparison.Ordinal)
    && playerRpCrafting.Contains("preview.TemplateStringId", StringComparison.Ordinal)
    && playerRpCrafting.Contains("preview.TemplateSelectionSource", StringComparison.Ordinal),
    "player-selected templates must stay inside the captured candidate snapshot through preview and commit");
Test.True(terminalBehavior.Contains("ShowPlayerRpCraftTemplateSelection(", StringComparison.Ordinal)
    && terminalBehavior.Contains("new ItemImageIdentifier(template)", StringComparison.Ordinal)
    && terminalBehavior.Contains("new InquiryElement(", StringComparison.Ordinal)
    && terminalBehavior.Contains("isSeachAvailable: true", StringComparison.Ordinal)
    && terminalBehavior.Contains("TryPreviewPlayerRpCraftWithPlayerSelectedTemplateForExternal", StringComparison.Ordinal)
    && terminalBehavior.Contains("Interlocked.Exchange(ref callbackGate, 1)", StringComparison.Ordinal),
    "player RP template picker must be a searchable single-choice list with item thumbnails and a one-shot callback");
Test.True(!terminalBehavior.Contains("Task.Run(", StringComparison.Ordinal)
    && !terminalBehavior.Contains("TryPreviewPlayerRpCraftExactMatchForExternal(", StringComparison.Ordinal)
    && !terminalBehavior.Contains("SelectPlayerRpCraftTemplateWithPreprocessForExternal", StringComparison.Ordinal)
    && !playerRpCrafting.Contains("BuildPlayerRpTemplateSelectionPromptForExternal", StringComparison.Ordinal)
    && !playerRpCrafting.Contains("SendPlayerRpTemplateSelectionHttpRequest", StringComparison.Ordinal)
    && !playerRpCrafting.Contains("PlayerRpCraftTemplateSelectorLog", StringComparison.Ordinal),
    "U-key player RP crafting must not retain an LLM request, HTTP selector, or selector log path");
Test.True(preprocessPrompts.Contains("PlayerRpTemplateSelection", StringComparison.Ordinal)
    && preprocessPrompts.Contains("Rows: rank|template_id|name|type|standard_price", StringComparison.Ordinal),
    "the retired selector prompt configuration must remain untouched for backward-compatible config loading");

Test.True(rewardSystem.Contains("|| IsGeneratedRewardItemStringId(item.StringId)", StringComparison.Ordinal), "generated AF items must not feed back into RP template caches");
Test.True(playerRpComponents.Contains("IsRuntimeCraftedWeapon(template)", StringComparison.Ordinal)
    && playerRpComponents.Contains("template.IsCraftedByPlayer", StringComparison.Ordinal)
    && playerRpComponents.Contains("\"crafted_item_\"", StringComparison.Ordinal)
    && playerRpComponents.Contains("template.WeaponDesign?.Template == null", StringComparison.Ordinal)
    && !playerRpComponents.Contains("template.IsCraftedWeapon || template.WeaponDesign != null", StringComparison.Ordinal), "static catalog CraftedItem weapons must remain eligible while runtime forged weapons stay excluded");
Test.True(rewardSystem.Contains("GeneratedRpEquipmentTemplateCacheReady", StringComparison.Ordinal)
    && rewardSystem.Contains("scanCompleted && hasSafeCandidates", StringComparison.Ordinal)
    && rewardSystem.Contains("GeneratedRpEquipmentTemplateCacheRetryAfterUtc", StringComparison.Ordinal)
    && !rewardSystem.Contains("GeneratedRpEquipmentTemplatesByKind.Count > 0", StringComparison.Ordinal), "empty or interrupted equipment scans must not poison the template cache");
Test.True(playerRpPopup.Contains("popup._ignoreSubmitUntilUtc = DateTime.UtcNow.AddMilliseconds", StringComparison.Ordinal)
    && Regex.IsMatch(terminalBehavior,
        @"InformationManager\.DisplayMessage\([\s\S]{0,500}?PlayerRpForgePopup\.TryRestoreEditing\(sessionId\);",
        RegexOptions.CultureInvariant), "failed player RP preview must debounce queued duplicate submit commands");
Test.True(playerRpCrafting.Contains("\\n未命中时：垃圾物品（投入仍会消耗）。", StringComparison.Ordinal)
    && !playerRpCrafting.Contains("\\n失败结果：垃圾物品", StringComparison.Ordinal)
    && terminalBehavior.Contains("SanitizePlayerRpForgeInquiryText", StringComparison.Ordinal)
    && terminalBehavior.Contains("BreakPlayerRpForgeInquiryToken", StringComparison.Ordinal)
    && terminalBehavior.Contains("builder.Append('\\u200B');", StringComparison.Ordinal)
    && terminalBehavior.Contains("SanitizePlayerRpForgeInquiryText(confirmationTitle)", StringComparison.Ordinal)
    && terminalBehavior.Contains("SanitizePlayerRpForgeInquiryText(confirmationText)", StringComparison.Ordinal)
    && terminalBehavior.Contains("SanitizePlayerRpForgeInquiryText(resultMessage)", StringComparison.Ordinal)
    && terminalBehavior.Contains("\"失败\",", StringComparison.Ordinal)
    && terminalBehavior.Contains("\"错误\",", StringComparison.Ordinal)
    && terminalBehavior.Contains("\"问题\",", StringComparison.Ordinal)
    && terminalBehavior.Contains("\"error\",", StringComparison.Ordinal)
    && terminalBehavior.Contains("\"problem\"", StringComparison.Ordinal),
    "player RP inquiries must avoid ExceptionSentry's post-load generic error keywords");
Test.True(Regex.IsMatch(rewardSystem,
        @"ClearGeneratedRewardRuntimeState\(\s*""sync_data_load_begin"",\s*preservePendingItems:\s*true\)",
        RegexOptions.CultureInvariant)
    && Regex.IsMatch(rewardSystem,
        @"ClearGeneratedRewardRuntimeState\(\s*""sync_data_load"",\s*preservePendingItems:\s*true\)",
        RegexOptions.CultureInvariant)
    && rewardSystem.Contains("if (!preservePendingItems)", StringComparison.Ordinal)
    && rewardSystem.Contains("GeneratedRewardPendingItemsByObjectId.Clear();", StringComparison.Ordinal),
    "save-load recovery must preserve early pending item references until in-place retarget completes");
Match compactEquipmentProbabilityPreview = Regex.Match(
    playerRpCrafting,
    @"private static void AppendPlayerRpEquipmentProbabilityPreview\([\s\S]*?(?=\r?\n\s*private static void AppendPlayerRpEquipmentProbabilityRow\()",
    RegexOptions.CultureInvariant);
Test.True(compactEquipmentProbabilityPreview.Success
    && compactEquipmentProbabilityPreview.Value.Contains("\"good\"", StringComparison.Ordinal)
    && compactEquipmentProbabilityPreview.Value.Contains("\"normal\"", StringComparison.Ordinal)
    && compactEquipmentProbabilityPreview.Value.Contains("\"bad\"", StringComparison.Ordinal)
    && !compactEquipmentProbabilityPreview.Value.Contains("TryCreateSnapshot", StringComparison.Ordinal)
    && !compactEquipmentProbabilityPreview.Value.Contains("BuildAttributeSummary", StringComparison.Ordinal),
    "equipment confirmation must show only three compact probability outcomes without building full stat snapshots");
Test.True(playerRpCrafting.Contains("if (badWeight > normalWeight)", StringComparison.Ordinal)
    && playerRpCrafting.Contains("【警告】劣化概率高于 33.33%", StringComparison.Ordinal)
    && playerRpCrafting.Contains("建议将锻造从 ", StringComparison.Ordinal)
    && playerRpCrafting.Contains("PlayerRpTemplatePricePerSmithingLevel.ToString", StringComparison.Ordinal)
    && playerRpCrafting.Contains(" 第纳尔对应 1 级）。", StringComparison.Ordinal)
    && playerRpCrafting.Contains("有效正属性保留 ", StringComparison.Ordinal)
    && playerRpCrafting.Contains("每项有效正属性 +", StringComparison.Ordinal)
    && playerRpCrafting.Contains("有效正属性与重量不变", StringComparison.Ordinal),
    "equipment confirmation effect summaries or degradation warning are missing");
Test.True(playerRpCrafting.Contains("GetPlayerRpEquipmentRecommendedSmithingLevel(templateBaseValue)", StringComparison.Ordinal)
    && playerRpCrafting.Contains("PlayerRpTemplatePricePerSmithingLevel = 1000", StringComparison.Ordinal)
    && playerRpCrafting.Contains("(safeTemplateValue + PlayerRpTemplatePricePerSmithingLevel - 1L)", StringComparison.Ordinal)
    && playerRpCrafting.Contains("/ PlayerRpTemplatePricePerSmithingLevel;", StringComparison.Ordinal)
    && !playerRpCrafting.Contains("Math.Sqrt(Math.Max(1, templateBaseValue))", StringComparison.Ordinal)
    && !playerRpCrafting.Contains("Math.Min(300d, Math.Sqrt", StringComparison.Ordinal)
    && playerRpModels.Contains("CurrentFormulaVersion = 4", StringComparison.Ordinal),
    "equipment probability difficulty must use one smithing level per 1000 denars without the former sqrt/300 cap");
Test.True(playerRpCrafting.Contains("PlayerRpNormalAttributeBonusPerUpgradeLevel = 3", StringComparison.Ordinal)
    && playerRpCrafting.Contains("PlayerRpGoodAttributeBonusPerUpgradeLevel =", StringComparison.Ordinal)
    && playerRpCrafting.Contains("PlayerRpNormalAttributeBonusPerUpgradeLevel * 2", StringComparison.Ordinal)
    && playerRpCrafting.Contains("int normalBonus =", StringComparison.Ordinal)
    && playerRpCrafting.Contains("upgradeLevel * PlayerRpNormalAttributeBonusPerUpgradeLevel", StringComparison.Ordinal)
    && playerRpCrafting.Contains("int goodBonus =", StringComparison.Ordinal)
    && playerRpCrafting.Contains("upgradeLevel * PlayerRpGoodAttributeBonusPerUpgradeLevel", StringComparison.Ordinal)
    && playerRpCrafting.Contains("? goodBonus", StringComparison.Ordinal)
    && playerRpCrafting.Contains("? normalBonus", StringComparison.Ordinal)
    && playerRpCrafting.Contains("while (threshold <= invested / 2L)", StringComparison.Ordinal),
    "each surplus-investment doubling must grant normal +3 and good +6 per applicable positive attribute");
Test.True(playerRpCrafting.Contains("PlayerRpMasterSmithingLevel = 275", StringComparison.Ordinal)
    && playerRpCrafting.Contains("safeSmithing >= PlayerRpMasterSmithingLevel", StringComparison.Ordinal)
    && playerRpCrafting.Contains("good = 20000;", StringComparison.Ordinal)
    && playerRpCrafting.Contains("normal = 10000;", StringComparison.Ordinal)
    && playerRpCrafting.Contains("bad = 0;", StringComparison.Ordinal)
    && playerRpCrafting.Contains("PlayerRpMasterNormalAttributeBonus = 3", StringComparison.Ordinal)
    && playerRpCrafting.Contains("normalBonus += PlayerRpMasterNormalAttributeBonus", StringComparison.Ordinal)
    && playerRpCrafting.Contains("PlayerRpMasterGoodAttributeBonus =", StringComparison.Ordinal)
    && playerRpCrafting.Contains("goodBonus += PlayerRpMasterGoodAttributeBonus", StringComparison.Ordinal)
    && playerRpCrafting.Contains("if (underfunded)", StringComparison.Ordinal)
    && playerRpCrafting.Contains("return;", StringComparison.Ordinal),
    "smithing 275 must remove the bad roll and add normal +3/good +6 only after the template is fully funded");
Test.True(terminalBehavior.Contains("OpenPlayerRpCrafterSelection()", StringComparison.Ordinal)
    && terminalBehavior.Contains("\"选择制造者\"", StringComparison.Ordinal)
    && terminalBehavior.Contains("\"　锻造等级：\"", StringComparison.Ordinal)
    && terminalBehavior.Contains("\"　锻造体力：\"", StringComparison.Ordinal)
    && terminalBehavior.Contains("isSeachAvailable: false", StringComparison.Ordinal),
    "player RP forge must open a compact crafter selection before the parchment editor");
Test.True(playerRpCrafting.Contains("TryGetAvailablePlayerRpCraftersForExternal", StringComparison.Ordinal)
    && playerRpCrafting.Contains("PartyBase.MainParty.MemberRoster.GetTroopRoster()", StringComparison.Ordinal)
    && playerRpCrafting.Contains("AIConfigHandler.IsPlayerCompanionOrFamilyTradeTarget(hero)", StringComparison.Ordinal)
    && playerRpCrafting.Contains("GetHeroCraftingStamina(", StringComparison.Ordinal)
    && playerRpCrafting.Contains("GetMaxHeroCraftingStamina(", StringComparison.Ordinal)
    && playerRpCrafting.Contains("SetHeroCraftingStamina", StringComparison.Ordinal)
    && playerRpCrafting.Contains("GetPlayerRpCraftingStaminaCost", StringComparison.Ordinal)
    && playerRpCrafting.Contains("((long)maxStamina + 3L) / 4L", StringComparison.Ordinal)
    && terminalBehavior.Contains("isEnabled: crafter.HasEnoughCraftingStamina", StringComparison.Ordinal),
    "crafter selection and commit must enforce a rounded-up quarter of the selected crafter's maximum stamina");
Test.True(playerRpModels.Contains("public string CrafterHeroId;", StringComparison.Ordinal)
    && playerRpModels.Contains("public string CrafterDisplayNameSnapshot;", StringComparison.Ordinal)
    && playerRpModels.Contains("CurrentSchemaVersion = 3", StringComparison.Ordinal)
    && playerRpModels.Contains("public int CraftingStaminaCost;", StringComparison.Ordinal)
    && playerRpModels.Contains("public int CraftingExperienceBaseAmount;", StringComparison.Ordinal)
    && playerRpCrafting.Contains("crafter.GetSkillValue(DefaultSkills.Crafting)", StringComparison.Ordinal)
    && playerRpCrafting.Contains("CrafterHeroId = current.CrafterHeroId", StringComparison.Ordinal)
    && playerRpCrafting.Contains("CreatorHeroId = player.StringId", StringComparison.Ordinal),
    "selected crafter identity/skill snapshots must survive request, preview, commit, and save while the player remains the payer/creator");
Test.True(playerRpCrafting.Contains("current.SmithingSkill != preview.SmithingSkill", StringComparison.Ordinal)
    && playerRpCrafting.Contains("current.GoodWeight != preview.GoodWeight", StringComparison.Ordinal)
    && playerRpCrafting.Contains("current.NormalWeight != preview.NormalWeight", StringComparison.Ordinal)
    && playerRpCrafting.Contains("current.BadWeight != preview.BadWeight", StringComparison.Ordinal)
    && playerRpCrafting.Contains("锻造等级或三档概率已经变化，请重新确认", StringComparison.Ordinal),
    "equipment commit must reject probability changes after the confirmation preview");
Test.True(!playerRpCrafting.Contains("\\n玩家锻造：", StringComparison.Ordinal)
    && !playerRpCrafting.Contains("\\n优良 / 正常 / 劣化：", StringComparison.Ordinal)
    && terminalBehavior.Contains("confirmationTitle = \"制造“\" + compactItemName", StringComparison.Ordinal),
    "equipment confirmation still exposes the verbose numeric header instead of moving identity to the title");
Test.True(!terminalBehavior.Contains("TryPreviewPlayerRpCraftExactMatchForExternal(", StringComparison.Ordinal)
    && terminalBehavior.Contains("TryBuildPlayerRpCraftTemplateSelectionForExternal(", StringComparison.Ordinal)
    && terminalBehavior.Contains("ShowPlayerRpCraftTemplateSelection(", StringComparison.Ordinal),
    "every U-key forge submission must enter the player-controlled template list instead of an automatic exact-match path");
Test.True(playerRpCrafting.Contains("\"player_choice\"", StringComparison.Ordinal)
    && playerRpCrafting.Contains("模板选择：玩家手动选择（Top ", StringComparison.Ordinal)
    && playerRpCrafting.Contains("requireExactGameItemMatch", StringComparison.Ordinal)
    && playerRpCrafting.Contains("TryValidatePlayerRpSelectedTemplateForCurrentRequest(", StringComparison.Ordinal),
    "manual choice must retain template safety and current-price revalidation before preview and commit");

Test.True(rewardSystem.Contains("TryGetPlayerRpCraftStoredItemValue", StringComparison.Ordinal)
    && rewardSystem.Contains("GetStoredPlayerRpCraftItemValue", StringComparison.Ordinal)
    && rewardSystem.Contains("HasExpectedPlayerRpCraftItemValue", StringComparison.Ordinal)
    && rewardSystem.Contains("\"Value\"", StringComparison.Ordinal)
    && rewardSystem.Contains("? playerCraftValue", StringComparison.Ordinal)
    && rewardSystem.Contains("GuidePrice = Math.Max(1, generatedItem.Value)", StringComparison.Ordinal)
    && playerRpModels.Contains("public int CraftedItemValue;", StringComparison.Ordinal)
    && playerRpCrafting.Contains("Math.Max(1, Math.Max(0, investedDenars) / 2)", StringComparison.Ordinal)
    && playerRpCrafting.Contains("CraftedItemValue = current.CraftedItemValue", StringComparison.Ordinal)
    && playerRpCrafting.Contains("generatedItem.Value != current.CraftedItemValue", StringComparison.Ordinal)
    && rewardSystem.Contains("data.CraftedItemValue = data.InvestedDenars;", StringComparison.Ordinal)
    && rewardSystem.Contains("storedSchemaVersion < 3", StringComparison.Ordinal)
    && rewardSystem.Contains("craftData.SchemaVersion < 3", StringComparison.Ordinal)
    && rewardSystem.Contains("craftData.InvestedDenars <= 0", StringComparison.Ordinal)
    && playerRpCrafting.Contains("物品属性或价格写入失败", StringComparison.Ordinal),
    "new player RP output value must be stored at half investment while legacy saves retain their original value");
foreach ((int invested, int expectedValue) in new[]
{
    (1, 1),
    (2, 1),
    (3, 1),
    (100, 50),
    (101, 50),
    (int.MaxValue, 1073741823)
})
{
    Test.Equal(
        expectedValue,
        Math.Max(1, Math.Max(0, invested) / 2),
        "crafted item half-value boundary");
}
Test.True(playerRpCrafting.Contains("GetPlayerRpCraftingExperience(current.CraftedItemValue)", StringComparison.Ordinal)
        || playerRpCrafting.Contains("GetPlayerRpCraftingExperience(craftedItemValue)", StringComparison.Ordinal),
    "crafting XP must be derived from the stored crafted item value");
int staminaCommitMarker = playerRpCrafting.IndexOf(
    "transactionCommitted = true;",
    StringComparison.Ordinal);
int addCraftingXpCall = playerRpCrafting.IndexOf(
    "staminaCrafter.AddSkillXp(",
    StringComparison.Ordinal);
Test.True(staminaCommitMarker >= 0
    && addCraftingXpCall > staminaCommitMarker
    && playerRpCrafting.Contains("current.CraftingExperienceBaseAmount", StringComparison.Ordinal)
    && playerRpCrafting.Contains("(safeValue + 25L) / 50L", StringComparison.Ordinal)
    && playerRpCrafting.Contains("staminaAdjustmentAttempted", StringComparison.Ordinal)
    && playerRpCrafting.Contains("crafting_stamina_restore_mismatch", StringComparison.Ordinal)
    && playerRpCrafting.Contains("crafting_xp_event_interrupted", StringComparison.Ordinal),
    "selected crafter must receive base smithing XP only after the item/stamina transaction commits, without refunding partially applied XP");
Test.True(!playerRpCrafting.Contains("candidate.StandardPrice < investedDenars", StringComparison.Ordinal)
    && !playerRpCrafting.Contains("杂物投入不能高于所选模板的标准价格", StringComparison.Ordinal)
    && playerRpCrafting.Contains("current.InvestedDenars >= current.TemplateBaseValue", StringComparison.Ordinal),
    "ordinary RP items must have no template-price investment cap while retaining the underfunded junk roll");
Test.True(playerRpCrafting.Contains("if (current.InvestedDenars > 10000)", StringComparison.Ordinal)
    && playerRpCrafting.IndexOf("transactionCommitted = true;", StringComparison.Ordinal)
        < playerRpCrafting.IndexOf("RecordPlayerHighValueRpCraftForExternal(", StringComparison.Ordinal)
    && myBehavior.Contains("if (investedDenars <= 10000)", StringComparison.Ordinal)
    && myBehavior.Contains("\"player_rp_craft_high_value:\"", StringComparison.Ordinal)
    && myBehavior.Contains("\"player_rp_craft\"", StringComparison.Ordinal)
    && myBehavior.Contains("isMajor: true", StringComparison.Ordinal)
    && myBehavior.Contains("RecordEventSourceMaterial(", StringComparison.Ordinal)
    && myBehavior.Contains("string outputValue = Math.Max(1, craftedItemValue)", StringComparison.Ordinal)
    && myBehavior.Contains("\"；成品价值 \" + outputValue", StringComparison.Ordinal)
    && myBehavior.Contains("includeInWorld: true", StringComparison.Ordinal),
    "successful RP crafts above 10,000 denars must enter weekly material, recent actions, and major history only after commit");
Test.True(playerRpCrafting.Contains("NormalizePlayerRpStrictExactLookup", StringComparison.Ordinal)
    && playerRpCrafting.Contains("[\\\\s\\\\u3000]+", StringComparison.Ordinal),
    "strict exact display-name matching must preserve hyphen/underscore boundaries");
Test.True(myBehavior.Contains("getItemDisplayName", StringComparison.Ordinal) && !myBehavior.Contains("knownItemKey.Trim()", StringComparison.Ordinal), "free-conversation normalization must preserve direct asset names");
Test.True(shoutBehavior.Contains("getItemDisplayName", StringComparison.Ordinal) && !shoutBehavior.Contains("knownItemKey.Trim()", StringComparison.Ordinal), "scene normalization must preserve direct asset names");
Match foodSuffixBlock = Regex.Match(rewardSystem, @"private static readonly GeneratedRpFoodSuffixRule\[\] GeneratedRpFoodSuffixRules.*?(?=\r?\n\s*public class RewardItemInfo)", RegexOptions.Singleline);
Test.True(foodSuffixBlock.Success, "generated RP food suffix pool missing");
int foodSuffixCount = Regex.Matches(foodSuffixBlock.Value, "\"(?:[^\"\\\\]|\\\\.)*\"").Count;
Test.True(foodSuffixCount >= 500, "generated RP food suffix pool unexpectedly small: " + foodSuffixCount);
foreach (string requiredFoodSuffix in new[] { "\"糕\"", "\"果\"", "\"梨\"", "\"菜\"", "\"肉\"", "\"牛肉\"", "\"羊肉\"", "\"猪肉\"", "\"牛排\"", "\"卜丁\"", "\"鱼青\"", "\"虾圭\"", "\"虾面盒\"", "\"焖鸡\"", "\"鸡徘\"", "\"肉元\"", "\"肉丝\"", "\"羊腿\"", "\"羊肝\"", "\"牛扒\"", "\"牛尾\"", "\"谷物\"", "\"穀物\"", "\"水\"", "\"药\"", "\"药水\"", "\"apple\"", "\"beef\"", "\"bread\"", "\"water\"", "\"medicine\"", "\"potion\"" })
{
    Test.True(foodSuffixBlock.Value.Contains(requiredFoodSuffix, StringComparison.Ordinal), "required food suffix missing: " + requiredFoodSuffix);
}
Test.True(rewardSystem.Contains("GeneratedRpFoodNonFoodEndingExceptions", StringComparison.Ordinal)
    && rewardSystem.Contains("\"结果\"", StringComparison.Ordinal)
    && rewardSystem.Contains("\"王八蛋\"", StringComparison.Ordinal)
    && rewardSystem.Contains("\"铁饼\"", StringComparison.Ordinal)
    && rewardSystem.Contains("\"香水\"", StringComparison.Ordinal)
    && rewardSystem.Contains("\"火药\"", StringComparison.Ordinal), "food false-positive guards missing");
Test.True(rewardSystem.Contains("IsCloneSafeGeneratedRpFoodTemplateItem", StringComparison.Ordinal)
    && rewardSystem.Contains("rp_food_template", StringComparison.Ordinal)
    && rewardSystem.Contains("ClearGeneratedRpFoodTemplateCache", StringComparison.Ordinal), "food template cache integration missing");
Test.True(rewardSystem.Contains("'\\uff09'", StringComparison.Ordinal), "full-width closing parenthesis must not block a terminal food suffix");
Test.True(rewardSystem.Contains("GeneratedRpFoodKind.Water", StringComparison.Ordinal)
    && rewardSystem.Contains("GeneratedRpFoodKind.Medicine", StringComparison.Ordinal)
    && rewardSystem.Contains("item.IsFood && (IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Beer)", StringComparison.Ordinal)
    && rewardSystem.Contains("item.IsFood && (IsGeneratedRpFoodItemCategory(item, DefaultItemCategories.Butter)", StringComparison.Ordinal), "water and medicine templates must remain consumable");

Console.WriteLine("PASS assertions=" + Test.Assertions + " fuzz=20000 pressureTags=25000 elapsedMs=" + stopwatch.Elapsed.TotalMilliseconds.ToString("F2"));
return 0;
}
}
