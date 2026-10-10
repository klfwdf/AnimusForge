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
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeErrMainPartyUnavailable"), "AF_ECO_UI_RpForgeErrMainPartyUnavailable", "The player's main party is currently unavailable."),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeErrNoCrafterInParty"), "AF_ECO_UI_RpForgeErrNoCrafterInParty", "No available clan member or companion is in the main party."),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeErrCrafterNotSelected"), "AF_ECO_UI_RpForgeErrCrafterNotSelected", "No crafter has been selected yet."),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeErrCrafterUnavailableReselect"), "AF_ECO_UI_RpForgeErrCrafterUnavailableReselect", "The selected crafter is no longer available; please choose again."),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeErrCrafterLeftParty"), "AF_ECO_UI_RpForgeErrCrafterLeftParty", "The selected crafter has left the player's main party; please choose again."),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeErrCrafterPartyUnverified"), "AF_ECO_UI_RpForgeErrCrafterPartyUnverified", "Could not confirm that the crafter is still in the player's main party."),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeErrCrafterCurrentlyUnavailable"), "AF_ECO_UI_RpForgeErrCrafterCurrentlyUnavailable", "The selected crafter is currently unavailable."),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeErrCrafterNoCraftingXp"), "AF_ECO_UI_RpForgeErrCrafterNoCraftingXp", "The selected crafter cannot gain crafting experience right now."),

		// --- crafting stamina plumbing
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeErrStaminaSystemUnavailable"), "AF_ECO_UI_RpForgeErrStaminaSystemUnavailable", "The native crafting stamina system is currently unavailable."),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeErrStaminaUnreadable"), "AF_ECO_UI_RpForgeErrStaminaUnreadable", "Could not read the selected crafter's crafting stamina."),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeErrMaxStaminaUnavailable"), "AF_ECO_UI_RpForgeErrMaxStaminaUnavailable", "The selected crafter's maximum crafting stamina is unavailable."),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeErrStaminaChangedDuringCraft"), "AF_ECO_UI_RpForgeErrStaminaChangedDuringCraft", "Crafting stamina changed while the item was being crafted; please confirm again."),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeErrStaminaDeductionFailed"), "AF_ECO_UI_RpForgeErrStaminaDeductionFailed", "The crafting stamina deduction could not be completed."),

		// --- template candidate selection
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeErrTemplateCandidatesExpired"), "AF_ECO_UI_RpForgeErrTemplateCandidatesExpired", "The template candidates are no longer valid."),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeErrTemplateCandidatesEmpty"), "AF_ECO_UI_RpForgeErrTemplateCandidatesEmpty", "The template candidate list is empty."),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeErrTemplateNotInTop50"), "AF_ECO_UI_RpForgeErrTemplateNotInTop50", "The selected template is not in the current Top 50 safe candidates."),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeErrNoSafeJunkTemplate"), "AF_ECO_UI_RpForgeErrNoSafeJunkTemplate", "No safe junk-item template was found."),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeErrMatchedTemplateUnavailable"), "AF_ECO_UI_RpForgeErrMatchedTemplateUnavailable", "The matched template is no longer available."),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeErrTemplateIdEmpty"), "AF_ECO_UI_RpForgeErrTemplateIdEmpty", "The selected template ID is empty."),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeErrTemplateUnavailable"), "AF_ECO_UI_RpForgeErrTemplateUnavailable", "The selected template is no longer available."),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeErrExactSnapshotFailed"), "AF_ECO_UI_RpForgeErrExactSnapshotFailed", "A safe crafting snapshot could not be built for the exactly matched item."),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeErrExactMatchChanged"), "AF_ECO_UI_RpForgeErrExactMatchChanged", "The exactly matched item in the game data has changed; please confirm again."),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeErrExactKindChanged"), "AF_ECO_UI_RpForgeErrExactKindChanged", "The exactly matched template no longer belongs to the current safe crafting category."),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeErrTemplateEquipmentKindChanged"), "AF_ECO_UI_RpForgeErrTemplateEquipmentKindChanged", "The selected template no longer belongs to the safe equipment category for this name."),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeErrNameMustMatchTemplate"), "AF_ECO_UI_RpForgeErrNameMustMatchTemplate", "Without an equipment suffix, the item name must exactly match the selected template's name or ID."),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeErrTemplateFoodKindChanged"), "AF_ECO_UI_RpForgeErrTemplateFoodKindChanged", "The selected template no longer belongs to the current food, drink, water, or medicine category."),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeErrTemplateGoodsUnsafe"), "AF_ECO_UI_RpForgeErrTemplateGoodsUnsafe", "The selected template is no longer a safe goods or document template."),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeErrTemplatePriceChanged"), "AF_ECO_UI_RpForgeErrTemplatePriceChanged", "The selected template's price has changed; please rebuild the candidate list."),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeErrNoTemplateInKind"), "AF_ECO_UI_RpForgeErrNoTemplateInKind", "No template is available for that equipment category."),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeErrTemplateComponentUnsafe"), "AF_ECO_UI_RpForgeErrTemplateComponentUnsafe", "The matched equipment template is missing a safe model or complete components."),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeErrNoSafeEquipmentTemplate"), "AF_ECO_UI_RpForgeErrNoSafeEquipmentTemplate", "No safe, copyable weapon or equipment template exists in that category."),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeErrNoSafeFoodOrGoodsTemplate"), "AF_ECO_UI_RpForgeErrNoSafeFoodOrGoodsTemplate", "There is no safe food or goods template."),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeErrNameIsEquipment"), "AF_ECO_UI_RpForgeErrNameIsEquipment", "The name belongs to a weapon or equipment item; please tick FORGE AS WEAPON/EQUIPMENT? first."),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeErrEquipmentSuffixRequired"), "AF_ECO_UI_RpForgeErrEquipmentSuffixRequired", "An equipment name must end with an explicit Chinese or English equipment suffix, or exactly match a loaded template name/ID."),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeErrExactMatchIsGoods"), "AF_ECO_UI_RpForgeErrExactMatchIsGoods", "The name exactly matches an ordinary game item; please untick FORGE AS WEAPON/EQUIPMENT?."),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeErrExactMatchIsEquipment"), "AF_ECO_UI_RpForgeErrExactMatchIsEquipment", "The name exactly matches a game weapon or equipment item; please tick FORGE AS WEAPON/EQUIPMENT?."),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeErrExactMatchComponentUnsafe"), "AF_ECO_UI_RpForgeErrExactMatchComponentUnsafe", "The name exactly matches a game item, but that template lacks components or a model that can be copied safely."),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeErrHorseCraftUnsupported"), "AF_ECO_UI_RpForgeErrHorseCraftUnsupported", "Player RP crafting cannot craft horses directly; horse armour, barding, and harness remain supported."),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeErrBannerCraftUnsupported"), "AF_ECO_UI_RpForgeErrBannerCraftUnsupported", "Player RP crafting does not support banner templates yet."),

		// --- request, confirmation and economy guards
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeErrRequestExpired"), "AF_ECO_UI_RpForgeErrRequestExpired", "The crafting request is no longer valid."),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeErrCraftInProgress"), "AF_ECO_UI_RpForgeErrCraftInProgress", "The previous craft is still being processed; please do not click again."),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeErrTemplateOrPriceChanged"), "AF_ECO_UI_RpForgeErrTemplateOrPriceChanged", "The template or price has changed; please confirm again."),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeErrCrafterResourcesChanged"), "AF_ECO_UI_RpForgeErrCrafterResourcesChanged", "The crafter's resources or the crafted item's value have changed; please confirm again."),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeErrSmithingTierChanged"), "AF_ECO_UI_RpForgeErrSmithingTierChanged", "The smithing tier or the three outcome weights have changed; please confirm again."),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeErrMaxStaminaChanged"), "AF_ECO_UI_RpForgeErrMaxStaminaChanged", "The maximum crafting stamina or the stamina cost has changed; please confirm again."),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeErrInvalidItemName"), "AF_ECO_UI_RpForgeErrInvalidItemName", "Please enter a valid item name."),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeErrInvestmentNotPositive"), "AF_ECO_UI_RpForgeErrInvestmentNotPositive", "The invested amount must be greater than 0."),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeErrPlayerNotReady"), "AF_ECO_UI_RpForgeErrPlayerNotReady", "The player or the player's inventory is not ready yet."),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeErrNotEnoughDenars"), "AF_ECO_UI_RpForgeErrNotEnoughDenars", "Not enough denars."),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeErrPlayerRosterUnavailable"), "AF_ECO_UI_RpForgeErrPlayerRosterUnavailable", "The player's inventory is unavailable."),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeErrChargeFailed"), "AF_ECO_UI_RpForgeErrChargeFailed", "Payment failed; no item was crafted."),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeErrItemCreationFailed"), "AF_ECO_UI_RpForgeErrItemCreationFailed", "The item could not be created or added to the inventory."),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeErrRecordCreationFailed"), "AF_ECO_UI_RpForgeErrRecordCreationFailed", "The crafting record could not be created."),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeErrItemStateWriteFailed"), "AF_ECO_UI_RpForgeErrItemStateWriteFailed", "The item's attributes or price could not be written."),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeErrItemPriceWriteFailed"), "AF_ECO_UI_RpForgeErrItemPriceWriteFailed", "The item's price could not be written."),

		// --- template type label fallback (GetPlayerRpTemplateTypeLabel)
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeLabelUnknownType"), "AF_ECO_UI_RpForgeLabelUnknownType", "Unknown")
	};

	/// <summary>
	/// Rollback ledger entries, as joined by the producer with "；". These are
	/// business facts that are also shown to the player inside
	/// <c>AF_ECO_UI_RpForgeRollbackIncomplete</c>, so each known entry gets a
	/// keyed projection while unknown entries pass through untouched.
	/// </summary>
	private static readonly (string Literal, string Key, string Fallback)[] RollbackIssueMessages =
	{
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeRollbackIssueForeignKey"), "AF_ECO_UI_RpForgeRollbackIssueForeignKey", "the generation key does not belong to this player-crafting namespace, so no object was unregistered"),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeRollbackIssueRosterUnavailable"), "AF_ECO_UI_RpForgeRollbackIssueRosterUnavailable", "the player's inventory is unavailable, so it could not be verified that the generated item was removed"),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeRollbackIssueRosterResidual"), "AF_ECO_UI_RpForgeRollbackIssueRosterResidual", "the generated item is still left in the player's inventory"),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeRollbackIssueObjectManagerUnavailable"), "AF_ECO_UI_RpForgeRollbackIssueObjectManagerUnavailable", "the object manager is unavailable, so the generated item could not be unregistered"),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeRollbackIssueStringIndexResidual"), "AF_ECO_UI_RpForgeRollbackIssueStringIndexResidual", "the generated object is still present in the string index"),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeRollbackIssueObjectIdIndexResidual"), "AF_ECO_UI_RpForgeRollbackIssueObjectIdIndexResidual", "the generated object is still present in the object-ID index"),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeRollbackIssueObjectIdCacheKept"), "AF_ECO_UI_RpForgeRollbackIssueObjectIdCacheKept", "the object-ID manifest cache holds other items, so that cache was kept"),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeRollbackIssueDetachedCacheKept"), "AF_ECO_UI_RpForgeRollbackIssueDetachedCacheKept", "the detached object-ID cache holds other items, so that cache was kept"),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeRollbackIssuePendingCacheKept"), "AF_ECO_UI_RpForgeRollbackIssuePendingCacheKept", "the pending object-ID cache holds other items, so that cache was kept"),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeRollbackIssueDuplicateRegistration"), "AF_ECO_UI_RpForgeRollbackIssueDuplicateRegistration", "one player-crafting key maps to several registered objects, so the unsafe unregistration was refused"),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeRollbackIssueObjectIdTaken"), "AF_ECO_UI_RpForgeRollbackIssueObjectIdTaken", "the player-crafting object ID is taken by another object, so the unsafe unregistration was refused")
	};

	/// <summary>
	/// Rollback ledger entries whose text embeds an exception type name, keyed by
	/// the fixed Chinese frame; the type name is re-attached as a variable.
	/// </summary>
	private static readonly (string Frame, string Key, string Fallback)[] RollbackExceptionFrames =
	{
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeRollbackIssueRosterCleanupFailed"), "AF_ECO_UI_RpForgeRollbackIssueRosterCleanupFailed", "cleaning the player's inventory failed with {ERROR}"),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeRollbackIssueRecordCleanupFailed"), "AF_ECO_UI_RpForgeRollbackIssueRecordCleanupFailed", "cleaning the records or caches failed with {ERROR}"),
		(EconomyDisplaySourceTexts.Get("Forge.RpForgeRollbackIssueUnregisterFailed"), "AF_ECO_UI_RpForgeRollbackIssueUnregisterFailed", "unregistering the generated object failed with {ERROR}")
	};

	/// <summary>
	/// "&lt;crafter&gt;的锻造体力不足：需要 &lt;cost&gt;，当前 &lt;have&gt;。" — emitted in three
	/// places (preview, external selection request, commit). The crafter name is
	/// dynamic, so the sentence is matched structurally.
	/// </summary>
	private static readonly Regex StaminaInsufficientPattern = new Regex(
		EconomyDisplaySourceTexts.Get("Forge.StaminaInsufficientPattern"),
		RegexOptions.CultureInvariant);

	/// <summary>
	/// "物品名称不能包含系统保留标记 &lt;marker&gt;。" — the marker is a producer
	/// constant, so the sentence is matched around it rather than by equality.
	/// </summary>
	private static readonly Regex ReservedMarkerPattern = new Regex(
		EconomyDisplaySourceTexts.Get("Forge.ReservedMarkerPattern"),
		RegexOptions.CultureInvariant);

	/// <summary>
	/// "游戏中存在多个同名物品；请改为输入其中一个模板 StringId：&lt;ids&gt;……" — the
	/// StringId list and the optional trailing ellipsis are preserved verbatim so
	/// the player can still copy an ID out of the message. Matched before any
	/// "；" splitting because the sentence itself contains a full-width semicolon.
	/// </summary>
	private static readonly Regex AmbiguousExactMatchPattern = new Regex(
		EconomyDisplaySourceTexts.Get("Forge.AmbiguousExactMatchPattern"),
		RegexOptions.CultureInvariant);

	/// <summary>
	/// "无法安全复制该装备模板：&lt;detail&gt;" — the detail comes from the snapshot
	/// service and is kept verbatim.
	/// </summary>
	private static readonly Regex TemplateCloneFailedPattern = new Regex(
		EconomyDisplaySourceTexts.Get("Forge.TemplateCloneFailedPattern"),
		RegexOptions.CultureInvariant);

	/// <summary>
	/// "退款失败：无法定位付款玩家，应退 &lt;n&gt; 第纳尔。"
	/// </summary>
	private static readonly Regex RefundPayerMissingPattern = new Regex(
		EconomyDisplaySourceTexts.Get("Forge.RefundPayerMissingPattern"),
		RegexOptions.CultureInvariant);

	/// <summary>
	/// "退款失败：无法读取退款前余额（&lt;ExceptionType&gt;），应退 &lt;n&gt; 第纳尔。"
	/// </summary>
	private static readonly Regex RefundBalanceUnreadablePattern = new Regex(
		EconomyDisplaySourceTexts.Get("Forge.RefundBalanceUnreadablePattern"),
		RegexOptions.CultureInvariant);

	/// <summary>
	/// "退款失败：无法验证退款后余额（&lt;ExceptionType&gt;），应退 &lt;n&gt; 第纳尔。"
	/// </summary>
	private static readonly Regex RefundBalanceUnverifiedPattern = new Regex(
		EconomyDisplaySourceTexts.Get("Forge.RefundBalanceUnverifiedPattern"),
		RegexOptions.CultureInvariant);

	/// <summary>
	/// "退款失败：应退 &lt;n&gt; 第纳尔，实际退回 &lt;m&gt; 第纳尔。" and its
	/// "…第纳尔（&lt;ExceptionType&gt;）。" variant.
	/// </summary>
	private static readonly Regex RefundShortfallPattern = new Regex(
		EconomyDisplaySourceTexts.Get("Forge.RefundShortfallPattern"),
		RegexOptions.CultureInvariant);

	/// <summary>
	/// "锻造体力未能恢复：&lt;ExceptionType&gt;。" — appended by
	/// <c>AppendPlayerRpCraftFailureStatus</c> in the commit rollback path.
	/// </summary>
	private static readonly Regex StaminaRestoreFailedPattern = new Regex(
		EconomyDisplaySourceTexts.Get("Forge.StaminaRestoreFailedPattern"),
		RegexOptions.CultureInvariant);

	/// <summary>
	/// "退款失败：事务回滚发生 &lt;ExceptionType&gt;，请检查玩家第纳尔和背包。" — the
	/// outer rollback itself threw, so the player must be told to check manually.
	/// </summary>
	private static readonly Regex RollbackThrewPattern = new Regex(
		EconomyDisplaySourceTexts.Get("Forge.RollbackThrewPattern"),
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
		EconomyDisplaySourceTexts.Get("Forge.RollbackIncompleteFramePattern"),
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
		if (string.Equals(label.Trim(), EconomyDisplaySourceTexts.Get("Forge.RpForgeLabelUnknownType"), StringComparison.Ordinal))
		{
			return EconomyUiTexts.Localized("AF_ECO_UI_RpForgeLabelUnknownType", "Unknown");
		}
		return label;
	}
}
