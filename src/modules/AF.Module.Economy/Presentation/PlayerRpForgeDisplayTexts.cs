using System;
using System.Text.RegularExpressions;

namespace AnimusForge;

/// <summary>
/// Display-boundary projection for the RP forge (player RP crafting) reject and
/// failure reasons produced by <c>RewardSystemBehavior.PlayerRpCrafting.cs</c>.
///
/// The producer keeps its raw Chinese reason bytes on purpose: the same string is
/// returned to callers, written into the rollback ledger, and matched by the
/// source-text stress test <c>GiveAssetTagCodec.StressTests</c>. Translating in
/// place would change a business fact and break those callers, so the English
/// projection happens here instead, at the point where the value is turned into
/// player-facing text.
///
/// Hard rules honoured below:
/// - Unrecognized input is returned byte-for-byte, so legacy reasons, exception
///   tails and future producer messages stay readable instead of being dropped.
/// - <c>null</c> and whitespace-only input are returned as-is.
/// - No producer literal, safety check, confirmation ticket, refund or rollback
///   path is touched or weakened by this file; it is a pure read-only projection.
/// - Compound producer output (multi-line, or a "；"-separated issue list) is
///   projected fragment by fragment so one untranslatable fragment cannot swallow
///   the rest of the message.
/// </summary>
internal static class PlayerRpForgeDisplayTexts
{
	/// <summary>
	/// Exact producer reasons, in the byte order the producer emits them, mapped
	/// to the resource key that carries the player-facing text and the English
	/// fallback used when the key is not present in the loaded language file.
	/// </summary>
	private static readonly (string Literal, string Key, string Fallback)[] ExactMessages =
	{
		// --- crafter availability (TryResolveAvailablePlayerRpCrafter and siblings)
		("玩家主队伍当前不可用。", "AF_ECO_UI_RpForgeErrMainPartyUnavailable", "The player's main party is currently unavailable."),
		("当前主队伍中没有可用的家族成员或同伴。", "AF_ECO_UI_RpForgeErrNoCrafterInParty", "No available clan member or companion is in the main party."),
		("尚未选择制造者。", "AF_ECO_UI_RpForgeErrCrafterNotSelected", "No crafter has been selected yet."),
		("所选制造者已经不可用，请重新选择。", "AF_ECO_UI_RpForgeErrCrafterUnavailableReselect", "The selected crafter is no longer available; please choose again."),
		("所选制造者已经离开玩家主队伍，请重新选择。", "AF_ECO_UI_RpForgeErrCrafterLeftParty", "The selected crafter has left the player's main party; please choose again."),
		("无法确认制造者仍在玩家主队伍中。", "AF_ECO_UI_RpForgeErrCrafterPartyUnverified", "Could not confirm that the crafter is still in the player's main party."),
		("所选制造者当前不可用。", "AF_ECO_UI_RpForgeErrCrafterCurrentlyUnavailable", "The selected crafter is currently unavailable."),
		("所选制造者当前无法获得锻造经验。", "AF_ECO_UI_RpForgeErrCrafterNoCraftingXp", "The selected crafter cannot gain crafting experience right now."),

		// --- crafting stamina plumbing
		("原版锻造体力系统当前不可用。", "AF_ECO_UI_RpForgeErrStaminaSystemUnavailable", "The native crafting stamina system is currently unavailable."),
		("无法读取所选制造者的锻造体力。", "AF_ECO_UI_RpForgeErrStaminaUnreadable", "Could not read the selected crafter's crafting stamina."),
		("所选制造者的最大锻造体力不可用。", "AF_ECO_UI_RpForgeErrMaxStaminaUnavailable", "The selected crafter's maximum crafting stamina is unavailable."),
		("锻造体力在制造期间发生变化，请重新确认。", "AF_ECO_UI_RpForgeErrStaminaChangedDuringCraft", "Crafting stamina changed while the item was being crafted; please confirm again."),
		("锻造体力扣除未能完成。", "AF_ECO_UI_RpForgeErrStaminaDeductionFailed", "The crafting stamina deduction could not be completed."),

		// --- template candidate selection
		("模板候选已经失效。", "AF_ECO_UI_RpForgeErrTemplateCandidatesExpired", "The template candidates are no longer valid."),
		("模板候选为空。", "AF_ECO_UI_RpForgeErrTemplateCandidatesEmpty", "The template candidate list is empty."),
		("所选模板不在当前 Top 50 安全候选中。", "AF_ECO_UI_RpForgeErrTemplateNotInTop50", "The selected template is not in the current Top 50 safe candidates."),
		("没有找到安全的垃圾物品模板。", "AF_ECO_UI_RpForgeErrNoSafeJunkTemplate", "No safe junk-item template was found."),
		("匹配模板已经不可用。", "AF_ECO_UI_RpForgeErrMatchedTemplateUnavailable", "The matched template is no longer available."),
		("所选模板ID为空。", "AF_ECO_UI_RpForgeErrTemplateIdEmpty", "The selected template ID is empty."),
		("所选模板已经不可用。", "AF_ECO_UI_RpForgeErrTemplateUnavailable", "The selected template is no longer available."),
		("精确匹配物品无法建立安全制造快照。", "AF_ECO_UI_RpForgeErrExactSnapshotFailed", "A safe crafting snapshot could not be built for the exactly matched item."),
		("游戏数据中的精确匹配物品已经变化，请重新确认。", "AF_ECO_UI_RpForgeErrExactMatchChanged", "The exactly matched item in the game data has changed; please confirm again."),
		("精确匹配模板已经不再属于当前安全制造类别。", "AF_ECO_UI_RpForgeErrExactKindChanged", "The exactly matched template no longer belongs to the current safe crafting category."),
		("所选模板不再属于当前名称对应的安全武器装备类别。", "AF_ECO_UI_RpForgeErrTemplateEquipmentKindChanged", "The selected template no longer belongs to the safe equipment category for this name."),
		("无装备后缀时，物品名称必须与所选模板名称或ID完全一致。", "AF_ECO_UI_RpForgeErrNameMustMatchTemplate", "Without an equipment suffix, the item name must exactly match the selected template's name or ID."),
		("所选模板不再属于当前食物、饮料、水或药物类别。", "AF_ECO_UI_RpForgeErrTemplateFoodKindChanged", "The selected template no longer belongs to the current food, drink, water, or medicine category."),
		("所选模板不再是安全的杂物或文书模板。", "AF_ECO_UI_RpForgeErrTemplateGoodsUnsafe", "The selected template is no longer a safe goods or document template."),
		("所选模板价格已经变化，请重新建立候选榜单。", "AF_ECO_UI_RpForgeErrTemplatePriceChanged", "The selected template's price has changed; please rebuild the candidate list."),
		("该装备类别没有可用模板。", "AF_ECO_UI_RpForgeErrNoTemplateInKind", "No template is available for that equipment category."),
		("匹配到的装备模板缺少安全模型或完整组件。", "AF_ECO_UI_RpForgeErrTemplateComponentUnsafe", "The matched equipment template is missing a safe model or complete components."),
		("该类别没有安全且可复制的武器装备模板。", "AF_ECO_UI_RpForgeErrNoSafeEquipmentTemplate", "No safe, copyable weapon or equipment template exists in that category."),
		("没有安全的食物或杂物模板。", "AF_ECO_UI_RpForgeErrNoSafeFoodOrGoodsTemplate", "There is no safe food or goods template."),
		("名称属于武器装备；请先勾选 FORGE AS WEAPON/EQUIPMENT?。", "AF_ECO_UI_RpForgeErrNameIsEquipment", "The name belongs to a weapon or equipment item; please tick FORGE AS WEAPON/EQUIPMENT? first."),
		("装备名称需要以明确的中英文装备后缀结尾，或与已加载模板名称/ID完全一致。", "AF_ECO_UI_RpForgeErrEquipmentSuffixRequired", "An equipment name must end with an explicit Chinese or English equipment suffix, or exactly match a loaded template name/ID."),
		("名称精确匹配到游戏中的普通物品；请取消勾选 FORGE AS WEAPON/EQUIPMENT?。", "AF_ECO_UI_RpForgeErrExactMatchIsGoods", "The name exactly matches an ordinary game item; please untick FORGE AS WEAPON/EQUIPMENT?."),
		("名称精确匹配到游戏中的武器装备；请勾选 FORGE AS WEAPON/EQUIPMENT?。", "AF_ECO_UI_RpForgeErrExactMatchIsEquipment", "The name exactly matches a game weapon or equipment item; please tick FORGE AS WEAPON/EQUIPMENT?."),
		("名称精确匹配到游戏物品，但该模板缺少可安全复制的组件或模型。", "AF_ECO_UI_RpForgeErrExactMatchComponentUnsafe", "The name exactly matches a game item, but that template lacks components or a model that can be copied safely."),
		("玩家RP制造暂不支持直接制造马匹；马甲、马铠和马具仍受支持。", "AF_ECO_UI_RpForgeErrHorseCraftUnsupported", "Player RP crafting cannot craft horses directly; horse armour, barding, and harness remain supported."),
		("玩家RP制造暂不支持旗帜模板。", "AF_ECO_UI_RpForgeErrBannerCraftUnsupported", "Player RP crafting does not support banner templates yet."),

		// --- request, confirmation and economy guards
		("制造请求已经失效。", "AF_ECO_UI_RpForgeErrRequestExpired", "The crafting request is no longer valid."),
		("正在处理上一项制造，请勿重复点击。", "AF_ECO_UI_RpForgeErrCraftInProgress", "The previous craft is still being processed; please do not click again."),
		("模板或价格已经变化，请重新确认。", "AF_ECO_UI_RpForgeErrTemplateOrPriceChanged", "The template or price has changed; please confirm again."),
		("制造者资源或成品价值已经变化，请重新确认。", "AF_ECO_UI_RpForgeErrCrafterResourcesChanged", "The crafter's resources or the crafted item's value have changed; please confirm again."),
		("锻造等级或三档概率已经变化，请重新确认。", "AF_ECO_UI_RpForgeErrSmithingTierChanged", "The smithing tier or the three outcome weights have changed; please confirm again."),
		("最大锻造体力或体力消耗已经变化，请重新确认。", "AF_ECO_UI_RpForgeErrMaxStaminaChanged", "The maximum crafting stamina or the stamina cost has changed; please confirm again."),
		("请输入有效的物品名称。", "AF_ECO_UI_RpForgeErrInvalidItemName", "Please enter a valid item name."),
		("投入金额必须大于 0。", "AF_ECO_UI_RpForgeErrInvestmentNotPositive", "The invested amount must be greater than 0."),
		("玩家或玩家背包尚未就绪。", "AF_ECO_UI_RpForgeErrPlayerNotReady", "The player or the player's inventory is not ready yet."),
		("第纳尔不足。", "AF_ECO_UI_RpForgeErrNotEnoughDenars", "Not enough denars."),
		("玩家背包不可用。", "AF_ECO_UI_RpForgeErrPlayerRosterUnavailable", "The player's inventory is unavailable."),
		("扣款失败，未制造物品。", "AF_ECO_UI_RpForgeErrChargeFailed", "Payment failed; no item was crafted."),
		("物品创建或加入背包失败。", "AF_ECO_UI_RpForgeErrItemCreationFailed", "The item could not be created or added to the inventory."),
		("制造记录创建失败。", "AF_ECO_UI_RpForgeErrRecordCreationFailed", "The crafting record could not be created."),
		("物品属性或价格写入失败。", "AF_ECO_UI_RpForgeErrItemStateWriteFailed", "The item's attributes or price could not be written."),
		("物品价格写入失败。", "AF_ECO_UI_RpForgeErrItemPriceWriteFailed", "The item's price could not be written."),

		// --- template type label fallback (GetPlayerRpTemplateTypeLabel)
		("未知", "AF_ECO_UI_RpForgeLabelUnknownType", "Unknown")
	};

	/// <summary>
	/// Rollback ledger entries, as joined by the producer with "；". These are
	/// business facts that are also shown to the player inside
	/// <c>AF_ECO_UI_RpForgeRollbackIncomplete</c>, so each known entry gets a
	/// keyed projection while unknown entries pass through untouched.
	/// </summary>
	private static readonly (string Literal, string Key, string Fallback)[] RollbackIssueMessages =
	{
		("生成标识不属于本次玩家制造命名空间，未反注册对象", "AF_ECO_UI_RpForgeRollbackIssueForeignKey", "the generation key does not belong to this player-crafting namespace, so no object was unregistered"),
		("玩家背包不可用，无法验证生成物品是否已移除", "AF_ECO_UI_RpForgeRollbackIssueRosterUnavailable", "the player's inventory is unavailable, so it could not be verified that the generated item was removed"),
		("玩家背包仍残留本次生成物品", "AF_ECO_UI_RpForgeRollbackIssueRosterResidual", "the generated item is still left in the player's inventory"),
		("对象管理器不可用，无法反注册生成物品", "AF_ECO_UI_RpForgeRollbackIssueObjectManagerUnavailable", "the object manager is unavailable, so the generated item could not be unregistered"),
		("本次生成对象仍存在于字符串索引", "AF_ECO_UI_RpForgeRollbackIssueStringIndexResidual", "the generated object is still present in the string index"),
		("本次生成对象仍存在于对象ID索引", "AF_ECO_UI_RpForgeRollbackIssueObjectIdIndexResidual", "the generated object is still present in the object-ID index"),
		("对象ID清单缓存存在其他物品，已保留该缓存", "AF_ECO_UI_RpForgeRollbackIssueObjectIdCacheKept", "the object-ID manifest cache holds other items, so that cache was kept"),
		("Detached 对象ID缓存存在其他物品，已保留该缓存", "AF_ECO_UI_RpForgeRollbackIssueDetachedCacheKept", "the detached object-ID cache holds other items, so that cache was kept"),
		("Pending 对象ID缓存存在其他物品，已保留该缓存", "AF_ECO_UI_RpForgeRollbackIssuePendingCacheKept", "the pending object-ID cache holds other items, so that cache was kept"),
		("同一玩家制造标识对应多个注册对象，已拒绝不安全反注册", "AF_ECO_UI_RpForgeRollbackIssueDuplicateRegistration", "one player-crafting key maps to several registered objects, so the unsafe unregistration was refused"),
		("玩家制造对象ID已被其他对象占用，已拒绝不安全反注册", "AF_ECO_UI_RpForgeRollbackIssueObjectIdTaken", "the player-crafting object ID is taken by another object, so the unsafe unregistration was refused")
	};

	/// <summary>
	/// Rollback ledger entries whose text embeds an exception type name, keyed by
	/// the fixed Chinese frame; the type name is re-attached as a variable.
	/// </summary>
	private static readonly (string Frame, string Key, string Fallback)[] RollbackExceptionFrames =
	{
		("玩家背包清理失败", "AF_ECO_UI_RpForgeRollbackIssueRosterCleanupFailed", "cleaning the player's inventory failed with {ERROR}"),
		("记录或缓存清理失败", "AF_ECO_UI_RpForgeRollbackIssueRecordCleanupFailed", "cleaning the records or caches failed with {ERROR}"),
		("反注册生成对象失败", "AF_ECO_UI_RpForgeRollbackIssueUnregisterFailed", "unregistering the generated object failed with {ERROR}")
	};

	/// <summary>
	/// "&lt;crafter&gt;的锻造体力不足：需要 &lt;cost&gt;，当前 &lt;have&gt;。" — emitted in three
	/// places (preview, external selection request, commit). The crafter name is
	/// dynamic, so the sentence is matched structurally.
	/// </summary>
	private static readonly Regex StaminaInsufficientPattern = new Regex(
		@"^(?<who>.+?)的锻造体力不足：需要 (?<cost>\d+)，当前 (?<have>\d+)。$",
		RegexOptions.CultureInvariant);

	/// <summary>
	/// "物品名称不能包含系统保留标记 &lt;marker&gt;。" — the marker is a producer
	/// constant, so the sentence is matched around it rather than by equality.
	/// </summary>
	private static readonly Regex ReservedMarkerPattern = new Regex(
		@"^物品名称不能包含系统保留标记 (?<marker>.+)。$",
		RegexOptions.CultureInvariant);

	/// <summary>
	/// "游戏中存在多个同名物品；请改为输入其中一个模板 StringId：&lt;ids&gt;……" — the
	/// StringId list and the optional trailing ellipsis are preserved verbatim so
	/// the player can still copy an ID out of the message. Matched before any
	/// "；" splitting because the sentence itself contains a full-width semicolon.
	/// </summary>
	private static readonly Regex AmbiguousExactMatchPattern = new Regex(
		@"^游戏中存在多个同名物品；请改为输入其中一个模板 StringId：(?<ids>.*)$",
		RegexOptions.CultureInvariant);

	/// <summary>
	/// "无法安全复制该装备模板：&lt;detail&gt;" — the detail comes from the snapshot
	/// service and is kept verbatim.
	/// </summary>
	private static readonly Regex TemplateCloneFailedPattern = new Regex(
		@"^无法安全复制该装备模板：(?<detail>.+)$",
		RegexOptions.CultureInvariant);

	/// <summary>
	/// "退款失败：无法定位付款玩家，应退 &lt;n&gt; 第纳尔。"
	/// </summary>
	private static readonly Regex RefundPayerMissingPattern = new Regex(
		@"^退款失败：无法定位付款玩家，应退 (?<expected>\d+) 第纳尔。$",
		RegexOptions.CultureInvariant);

	/// <summary>
	/// "退款失败：无法读取退款前余额（&lt;ExceptionType&gt;），应退 &lt;n&gt; 第纳尔。"
	/// </summary>
	private static readonly Regex RefundBalanceUnreadablePattern = new Regex(
		@"^退款失败：无法读取退款前余额（(?<type>[^（）]+)），应退 (?<expected>\d+) 第纳尔。$",
		RegexOptions.CultureInvariant);

	/// <summary>
	/// "退款失败：无法验证退款后余额（&lt;ExceptionType&gt;），应退 &lt;n&gt; 第纳尔。"
	/// </summary>
	private static readonly Regex RefundBalanceUnverifiedPattern = new Regex(
		@"^退款失败：无法验证退款后余额（(?<type>[^（）]+)），应退 (?<expected>\d+) 第纳尔。$",
		RegexOptions.CultureInvariant);

	/// <summary>
	/// "退款失败：应退 &lt;n&gt; 第纳尔，实际退回 &lt;m&gt; 第纳尔。" and its
	/// "…第纳尔（&lt;ExceptionType&gt;）。" variant.
	/// </summary>
	private static readonly Regex RefundShortfallPattern = new Regex(
		@"^退款失败：应退 (?<expected>\d+) 第纳尔，实际退回 (?<actual>\d+) 第纳尔(?<tail>。|（(?<type>[^（）]+)）。)$",
		RegexOptions.CultureInvariant);

	/// <summary>
	/// "锻造体力未能恢复：&lt;ExceptionType&gt;。" — appended by
	/// <c>AppendPlayerRpCraftFailureStatus</c> in the commit rollback path.
	/// </summary>
	private static readonly Regex StaminaRestoreFailedPattern = new Regex(
		@"^锻造体力未能恢复：(?<type>[^：。]+)。$",
		RegexOptions.CultureInvariant);

	/// <summary>
	/// "退款失败：事务回滚发生 &lt;ExceptionType&gt;，请检查玩家第纳尔和背包。" — the
	/// outer rollback itself threw, so the player must be told to check manually.
	/// </summary>
	private static readonly Regex RollbackThrewPattern = new Regex(
		@"^退款失败：事务回滚发生 (?<type>[^，。]+)，请检查玩家第纳尔和背包。$",
		RegexOptions.CultureInvariant);

	/// <summary>
	/// "&lt;frame&gt;（&lt;ExceptionType&gt;）" — a rollback ledger entry that carries an
	/// exception type. No trailing period: the producer appends none here.
	/// </summary>
	private static readonly Regex RollbackExceptionFramePattern = new Regex(
		@"^(?<frame>[^（]+)（(?<type>[^（）]+)）$",
		RegexOptions.CultureInvariant);

	/// <summary>
	/// The rollback headline built by <c>EconomyUiTexts.PlayerRpCraftRollbackIncomplete</c>,
	/// in either the English or the Chinese rendering, so the embedded issue list
	/// can be projected without changing the frame.
	/// </summary>
	private static readonly Regex RollbackIncompleteFramePattern = new Regex(
		@"^(?<frame>物品回滚未完全完成|Item rollback was not fully completed)[：:]\s*(?<issues>.*)$",
		RegexOptions.CultureInvariant);

	/// <summary>
	/// Projects one producer reason (or a compound reason produced by
	/// <c>AppendPlayerRpCraftFailureStatus</c>) into player-facing text. Anything
	/// that is not recognized is returned unchanged.
	/// </summary>
	internal static string ForDisplay(string reason)
	{
		if (reason == null)
		{
			return null;
		}
		if (reason.Trim().Length == 0)
		{
			return reason;
		}

		// The producer joins a primary reason and its rollback status with "\n",
		// so each line is projected independently; a line that is not recognized
		// is left exactly as it was.
		if (reason.IndexOf('\n') >= 0)
		{
			string[] lines = reason.Split('\n');
			bool changed = false;
			for (int index = 0; index < lines.Length; index++)
			{
				string projected = ProjectLine(lines[index]);
				if (projected != null)
				{
					lines[index] = projected;
					changed = true;
				}
			}
			return changed ? string.Join("\n", lines) : reason;
		}

		string single = ProjectLine(reason);
		return single ?? reason;
	}

	/// <summary>
	/// Projects one line, or returns <c>null</c> when no rule matched so the
	/// caller can keep the original bytes.
	/// </summary>
	private static string ProjectLine(string line)
	{
		string value = (line ?? string.Empty).Trim();
		if (value.Length == 0)
		{
			return null;
		}

		// Whole-line rules run first: several producer sentences contain a
		// full-width "；" internally, so splitting before matching would break them.
		string whole = ProjectKnown(value);
		if (whole != null)
		{
			return whole;
		}

		if (value.IndexOf('；') >= 0)
		{
			string[] issues = value.Split('；');
			bool changed = false;
			for (int index = 0; index < issues.Length; index++)
			{
				string fragment = issues[index].Trim();
				string projected = ProjectKnown(fragment);
				if (projected != null)
				{
					issues[index] = projected;
					changed = true;
				}
				else
				{
					issues[index] = fragment;
				}
			}
			if (changed)
			{
				return string.Join("; ", issues);
			}
		}

		return null;
	}

	/// <summary>
	/// Applies every known rule to one trimmed fragment. Returns <c>null</c> when
	/// nothing matched.
	/// </summary>
	private static string ProjectKnown(string value)
	{
		string trimmed = (value ?? string.Empty).Trim();
		if (trimmed.Length == 0)
		{
			return null;
		}

		foreach ((string Literal, string Key, string Fallback) entry in ExactMessages)
		{
			if (string.Equals(trimmed, entry.Literal, StringComparison.Ordinal))
			{
				return EconomyUiTexts.Localized(entry.Key, entry.Fallback);
			}
		}

		foreach ((string Literal, string Key, string Fallback) entry in RollbackIssueMessages)
		{
			if (string.Equals(trimmed, entry.Literal, StringComparison.Ordinal))
			{
				return EconomyUiTexts.Localized(entry.Key, entry.Fallback);
			}
		}

		Match stamina = StaminaInsufficientPattern.Match(trimmed);
		if (stamina.Success)
		{
			return EconomyUiTexts.Localized(
				"AF_ECO_UI_RpForgeErrStaminaInsufficient",
				"{CRAFTER} does not have enough crafting stamina: {COST} required, {HAVE} available.",
				("CRAFTER", stamina.Groups["who"].Value),
				("COST", stamina.Groups["cost"].Value),
				("HAVE", stamina.Groups["have"].Value));
		}

		Match reserved = ReservedMarkerPattern.Match(trimmed);
		if (reserved.Success)
		{
			return EconomyUiTexts.Localized(
				"AF_ECO_UI_RpForgeErrReservedMarker",
				"The item name cannot contain the system-reserved marker {MARKER}.",
				("MARKER", reserved.Groups["marker"].Value));
		}

		Match ambiguous = AmbiguousExactMatchPattern.Match(trimmed);
		if (ambiguous.Success)
		{
			return EconomyUiTexts.Localized(
				"AF_ECO_UI_RpForgeErrAmbiguousExactMatch",
				"Several game items share that name; please enter one of their template StringIds instead: {IDS}",
				("IDS", ambiguous.Groups["ids"].Value));
		}

		Match cloneFailed = TemplateCloneFailedPattern.Match(trimmed);
		if (cloneFailed.Success)
		{
			return EconomyUiTexts.Localized(
				"AF_ECO_UI_RpForgeErrTemplateCloneDetail",
				"The equipment template could not be copied safely: {DETAIL}",
				("DETAIL", cloneFailed.Groups["detail"].Value));
		}

		Match payerMissing = RefundPayerMissingPattern.Match(trimmed);
		if (payerMissing.Success)
		{
			return EconomyUiTexts.Localized(
				"AF_ECO_UI_RpForgeErrRefundPayerMissingAmount",
				"Refund failed: the paying player could not be located; {AMOUNT} denars should be refunded.",
				("AMOUNT", payerMissing.Groups["expected"].Value));
		}

		Match unreadable = RefundBalanceUnreadablePattern.Match(trimmed);
		if (unreadable.Success)
		{
			return EconomyUiTexts.Localized(
				"AF_ECO_UI_RpForgeErrRefundBalanceUnreadable",
				"Refund failed: the pre-refund balance could not be read ({ERROR}); {AMOUNT} denars should be refunded.",
				("ERROR", unreadable.Groups["type"].Value),
				("AMOUNT", unreadable.Groups["expected"].Value));
		}

		Match unverified = RefundBalanceUnverifiedPattern.Match(trimmed);
		if (unverified.Success)
		{
			return EconomyUiTexts.Localized(
				"AF_ECO_UI_RpForgeErrRefundBalanceUnverified",
				"Refund failed: the post-refund balance could not be verified ({ERROR}); {AMOUNT} denars should be refunded.",
				("ERROR", unverified.Groups["type"].Value),
				("AMOUNT", unverified.Groups["expected"].Value));
		}

		Match shortfall = RefundShortfallPattern.Match(trimmed);
		if (shortfall.Success)
		{
			// The producer emits two shapes here: a plain shortfall, and a
			// shortfall where the refund call itself threw. They become two keys
			// so that neither the sentence nor its trailing punctuation has to be
			// assembled from hard-coded English fragments.
			if (shortfall.Groups["type"].Success)
			{
				return EconomyUiTexts.Localized(
					"AF_ECO_UI_RpForgeErrRefundShortfallWithError",
					"Refund failed: {EXPECTED} denars should be refunded but only {ACTUAL} denars were returned ({ERROR}).",
					("EXPECTED", shortfall.Groups["expected"].Value),
					("ACTUAL", shortfall.Groups["actual"].Value),
					("ERROR", shortfall.Groups["type"].Value));
			}
			return EconomyUiTexts.Localized(
				"AF_ECO_UI_RpForgeErrRefundShortfall",
				"Refund failed: {EXPECTED} denars should be refunded but only {ACTUAL} denars were returned.",
				("EXPECTED", shortfall.Groups["expected"].Value),
				("ACTUAL", shortfall.Groups["actual"].Value));
		}

		Match staminaRestore = StaminaRestoreFailedPattern.Match(trimmed);
		if (staminaRestore.Success)
		{
			return EconomyUiTexts.Localized(
				"AF_ECO_UI_RpForgeErrStaminaRestoreFailed",
				"The crafting stamina could not be restored: {ERROR}.",
				("ERROR", staminaRestore.Groups["type"].Value));
		}

		Match rollbackThrew = RollbackThrewPattern.Match(trimmed);
		if (rollbackThrew.Success)
		{
			return EconomyUiTexts.Localized(
				"AF_ECO_UI_RpForgeErrRollbackThrew",
				"Refund failed: the transaction rollback threw {ERROR}; please check the player's denars and inventory.",
				("ERROR", rollbackThrew.Groups["type"].Value));
		}

		Match rollbackFrame = RollbackIncompleteFramePattern.Match(trimmed);
		if (rollbackFrame.Success)
		{
			string issues = ProjectIssueList(rollbackFrame.Groups["issues"].Value);
			return EconomyUiTexts.Localized(
				"AF_ECO_UI_RpForgeRollbackIncomplete",
				"Item rollback was not fully completed: {ISSUES}",
				("ISSUES", issues));
		}

		Match exceptionFrame = RollbackExceptionFramePattern.Match(trimmed);
		if (exceptionFrame.Success)
		{
			string frame = exceptionFrame.Groups["frame"].Value.Trim();
			foreach ((string Frame, string Key, string Fallback) entry in RollbackExceptionFrames)
			{
				if (string.Equals(frame, entry.Frame, StringComparison.Ordinal))
				{
					return EconomyUiTexts.Localized(
						entry.Key,
						entry.Fallback,
						("ERROR", exceptionFrame.Groups["type"].Value));
				}
			}
		}

		return null;
	}

	/// <summary>
	/// Projects a "；"-separated rollback issue list, leaving unrecognized entries
	/// untouched.
	/// </summary>
	private static string ProjectIssueList(string issues)
	{
		string value = (issues ?? string.Empty).Trim();
		if (value.Length == 0)
		{
			return value;
		}
		string[] fragments = value.Split('；');
		for (int index = 0; index < fragments.Length; index++)
		{
			string fragment = fragments[index].Trim();
			string projected = ProjectKnown(fragment);
			fragments[index] = projected ?? fragment;
		}
		return string.Join("; ", fragments);
	}

	/// <summary>
	/// Projects the template type label produced by
	/// <c>RewardSystemBehavior.GetPlayerRpTemplateTypeLabel</c> for display. Only
	/// the Chinese-only fallback is rewritten; composed labels such as
	/// "Sword (OneHanded)" and the already-bilingual "鞭/Whip" pass through
	/// unchanged.
	/// </summary>
	internal static string TypeLabelForDisplay(string label)
	{
		if (label == null)
		{
			return null;
		}
		if (string.Equals(label.Trim(), "未知", StringComparison.Ordinal))
		{
			return EconomyUiTexts.Localized("AF_ECO_UI_RpForgeLabelUnknownType", "Unknown");
		}
		return label;
	}
}
