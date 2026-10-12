using System;
using System.Globalization;
using System.Text.RegularExpressions;
using TaleWorlds.Localization;

namespace AnimusForge;

/// <summary>
/// Player-facing reward and economy notifications.  Facts, prompts, logs and
/// operation protocol text remain owned by their callers; this helper only
/// resolves text that is sent to the campaign information feed or quick info.
/// </summary>
internal static class EconomyUiTexts
{
    internal static string ProjectPartyAssetNameForUi(string value)
    {
        if (value == null) return null;
        if (string.Equals(value, EconomySourceTexts.Get("PartyAsset_Other"), StringComparison.Ordinal)) return Text("AF_ECO_UI_PartyAsset_Other", "the other party");
        if (string.Equals(value, EconomySourceTexts.Get("PartyAsset_Unknown"), StringComparison.Ordinal)) return Text("AF_ECO_UI_PartyAsset_Unknown", "unknown");
        if (string.Equals(value, EconomySourceTexts.Get("PartyAsset_UnknownAsset"), StringComparison.Ordinal)) return Text("AF_ECO_UI_PartyAsset_UnknownAsset", "unknown asset");
        if (string.Equals(value, EconomySourceTexts.Get("PartyAsset_UnknownSettlement"), StringComparison.Ordinal)) return Text("AF_ECO_UI_PartyAsset_UnknownSettlement", "unknown settlement");
        if (string.Equals(value, EconomySourceTexts.Get("PartyAsset_Workshop"), StringComparison.Ordinal)) return Text("AF_ECO_UI_PartyAsset_Workshop", "workshop");
        if (string.Equals(value, EconomySourceTexts.Get("PartyAsset_Caravan"), StringComparison.Ordinal)) return Text("AF_ECO_UI_PartyAsset_Caravan", "caravan");
        string workshop = EconomySourceTexts.Get("PartyAsset_Workshop");
        string caravan = EconomySourceTexts.Get("PartyAsset_Caravan");
        foreach (string label in new[] { workshop, caravan })
            if (value.StartsWith(label + EconomySourceTexts.Get("PartyAsset_OpenName"), StringComparison.Ordinal) && value.EndsWith(EconomySourceTexts.Get("PartyAsset_CloseName"), StringComparison.Ordinal))
                return ProjectPartyAssetNameForUi(label) + value.Substring(label.Length);
        return value;
    }

    private static readonly Regex LiteralValues = new Regex(@"AFECOUILITERAL([0-9]+)END", RegexOptions.CultureInvariant);
	internal readonly struct FixedAssetDisplayContext
	{
		internal readonly string AssetName;
		internal readonly string GiverName;
		internal FixedAssetDisplayContext(string assetName, string giverName)
		{
			AssetName = assetName;
			GiverName = giverName;
		}
		internal bool Matches(string value, string prefix, string separator, string suffix)
			=> AssetName != null && GiverName != null && string.Equals(
				value, prefix + AssetName + separator + GiverName + suffix, StringComparison.Ordinal);
	}

	private static string Text(string id, string fallback, params (string Name, object Value)[] variables)
	{
		TextObject text = new TextObject("{=" + id + "}" + fallback);
		if (variables != null)
        {
            for (int i = 0; i < variables.Length; i++)
                text.SetTextVariable(variables[i].Name, "AFECOUILITERAL" + i.ToString(CultureInfo.InvariantCulture) + "END");
        }
        return LiteralValues.Replace(text.ToString(), match =>
            variables[int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture)].Value?.ToString() ?? string.Empty);
	}

	/// <summary>
	/// Shared resolution entry for other presentation helpers in this module so
	/// that every player-visible string goes through the same keyed pipeline.
	/// </summary>
	internal static string Localized(string id, string fallback, params (string Name, object Value)[] variables)
		=> Text(id, fallback, variables);

	public static string CompanionReturnedToClan(string hero, string clan)
		=> Text("AF_ECO_UI_CompanionReturnedClan", "[Companion departed] {HERO} returned to {CLAN}.", ("HERO", hero), ("CLAN", clan));

	public static string CompanionReturnedToSettlement(string hero, string settlement)
		=> Text("AF_ECO_UI_CompanionReturnedSettlement", "[Companion departed] {HERO} returned to {SETTLEMENT}.", ("HERO", hero), ("SETTLEMENT", settlement));

	public static string CompanionBecameWanderer(string reason, string hero)
		=> Text("AF_ECO_UI_CompanionWanderer", "[Companion departed] {REASON}{HERO} became a wanderer.", ("REASON", reason), ("HERO", hero));

	public static string CompanionWandererReason(bool clanExists, string clan)
		=> clanExists
			? Text("AF_ECO_UI_CompanionWandererClanReason", "{CLAN} has fallen; ", ("CLAN", clan))
			: Text("AF_ECO_UI_CompanionWandererUnknownReason", "The original affiliation is unavailable; ");

	public static string SpecialRpItemCannotEnterMarket
		=> Text("AF_ECO_UI_SpecialRpItemMarketBlocked", "This special RP item cannot be traded on the market.");

	public static string GeneratedItemGiven(string source, string item)
		=> Text("AF_ECO_UI_ItemGiven", "{SOURCE} gave you {ITEM}.", ("SOURCE", source), ("ITEM", item));

	public static string PromiseRecorded(string note, string value)
		=> Text("AF_ECO_UI_PromiseRecorded", "[Promise or debt recorded] {NOTE}; promise value: {VALUE}", ("NOTE", note), ("VALUE", value));

	public static string DebtRelease(string status, bool success)
		=> Text(success ? "AF_ECO_UI_DebtReleased" : "AF_ECO_UI_DebtReleaseFailed", success ? "[Debt released] {STATUS}" : "[Debt release failed] {STATUS}", ("STATUS", status));

	/// <summary>
	/// Converts the fixed ledger status labels used by the reward executor into
	/// player-facing text without changing the raw status returned to facts or
	/// replay consumers. Unknown/dynamic status text is deliberately preserved.
	/// </summary>
	public static string DebtStatusForDisplay(string status)
	{
		string value = (status ?? string.Empty).Trim();
		if (string.Equals(value, EconomySourceTexts.Get("S019"), StringComparison.Ordinal))
		{
			return Text("AF_ECO_UI_DebtStatusInvalidNpc", "Invalid debt request: the NPC or debt ID is missing.");
		}
		if (string.Equals(value, EconomySourceTexts.Get("S020"), StringComparison.Ordinal))
		{
			return Text("AF_ECO_UI_DebtStatusNpcRecordMissing", "No debt record was found for this NPC.");
		}
		if (string.Equals(value, EconomySourceTexts.Get("S021"), StringComparison.Ordinal))
		{
			return Text("AF_ECO_UI_DebtStatusNpcNoDebt", "This NPC has no debt that can currently be repaid.");
		}
		if (string.Equals(value, EconomySourceTexts.Get("S024"), StringComparison.Ordinal))
		{
			return Text("AF_ECO_UI_DebtStatusInvalidMarket", "Invalid market debt request: the market creditor or debt ID is missing.");
		}
		if (string.Equals(value, EconomySourceTexts.Get("S025"), StringComparison.Ordinal))
		{
			return Text("AF_ECO_UI_DebtStatusMarketRecordMissing", "No debt record was found for this market.");
		}
		if (string.Equals(value, EconomySourceTexts.Get("S022"), StringComparison.Ordinal))
		{
			return Text("AF_ECO_UI_DebtStatusLineMissing", "No unsettled debt line with that ID was found.");
		}

		string marker = EconomySourceTexts.Get("S069");
		string prefix = EconomySourceTexts.Get("S070");
		if (value.StartsWith(prefix, StringComparison.Ordinal))
		{
			int markerIndex = value.IndexOf(marker, prefix.Length, StringComparison.Ordinal);
			if (markerIndex > prefix.Length && value.EndsWith("）。", StringComparison.Ordinal))
			{
				string debtId = value.Substring(prefix.Length, markerIndex - prefix.Length);
				string amount = value.Substring(markerIndex + marker.Length, value.Length - markerIndex - marker.Length - 2);
				return Text("AF_ECO_UI_DebtStatusReleased", "Debt ID {ID} was released by agreement ({AMOUNT}).", ("ID", debtId), ("AMOUNT", amount));
			}
		}

		return status ?? string.Empty;
	}

	public static string AllDebtCleared(string giver)
		=> Text("AF_ECO_UI_AllDebtCleared", "[Debt cleared] All of your debt to {GIVER} has been repaid!", ("GIVER", giver));

	public static string MarketDebtRelease(string status, bool success)
		=> Text(success ? "AF_ECO_UI_MarketDebtReleased" : "AF_ECO_UI_MarketDebtReleaseFailed", success ? "[Market debt released] {STATUS}" : "[Market debt release failed] {STATUS}", ("STATUS", status));

	public static string MarketDebtCleared
		=> Text("AF_ECO_UI_MarketDebtCleared", "[Market debt] All current market debt has been cleared.");

	public static string DenarsReceived(string giver, object amount)
		=> Text("AF_ECO_UI_DenarsReceived", "{GIVER} gave you {AMOUNT} denars.", ("GIVER", giver), ("AMOUNT", amount));

	public static string DenarsGiven(string receiver, object amount)
		=> Text("AF_ECO_UI_DenarsGiven", "You gave {RECEIVER} {AMOUNT} denars.", ("RECEIVER", receiver), ("AMOUNT", amount));

	public static string ItemsReceived(string giver, string items)
		=> Text("AF_ECO_UI_ItemsReceived", "{GIVER} gave you {ITEMS}.", ("GIVER", giver), ("ITEMS", items));

	public static string ItemsGiven(string receiver, string items)
		=> Text("AF_ECO_UI_ItemsGiven", "You gave {RECEIVER} {ITEMS}.", ("RECEIVER", receiver), ("ITEMS", items));

	public static string PartyItemsReceived(string giver, string items)
		=> Text("AF_ECO_UI_PartyItemsReceived", "{GIVER} gave you {ITEMS}.", ("GIVER", giver), ("ITEMS", items));

	public static string PartyItemsGiven(string items)
		=> Text("AF_ECO_UI_PartyItemsGiven", "You gave the other party {ITEMS}.", ("ITEMS", items));

	public static string UnknownPerson => Text("AF_ECO_UI_UnknownPerson", "someone");

	public static string OtherParty => Text("AF_ECO_UI_OtherParty", "the other party");

	public static string OriginalSettlement => Text("AF_ECO_UI_OriginalSettlement", "the original settlement");

	public static string MerchantAt(string marketType)
		=> Text("AF_ECO_UI_MerchantAt", "the {MARKET} merchant", ("MARKET", marketType));

	public static string SettlementMarketType(bool village)
		=> village
			? Text("AF_ECO_UI_MarketVillage", "village")
			: Text("AF_ECO_UI_MarketTown", "town");

	public static string RecruitmentIntercepted
		=> Text("AF_ECO_UI_RecruitmentIntercepted", "[Join intercepted] The native conversation had already handled this action; the stale action was blocked.");

	public static string Status(string id, string fallback, string status)
		=> Text(id, fallback, ("STATUS", status));

	public static string CivilWarFaction(string status)
		=> Text("AF_ECO_UI_CivilWarFaction", "[Civil war faction] {STATUS}", ("STATUS", status));

	public static string JoinResult(bool success, bool preserveFamily, bool companion, string status)
	{
		string id;
		string fallback;
		if (!success)
		{
			id = "AF_ECO_UI_JoinFailed";
			fallback = "[Join failed] {STATUS}";
		}
		else if (preserveFamily)
		{
			id = "AF_ECO_UI_JoinParty";
			fallback = "[Join party] {STATUS}";
		}
		else if (companion)
		{
			id = "AF_ECO_UI_BecomeCompanion";
			fallback = "[Become companion] {STATUS}";
		}
		else
		{
			id = "AF_ECO_UI_JoinClan";
			fallback = "[Join clan] {STATUS}";
		}
		return Text(id, fallback, ("STATUS", status));
	}

	public static string TrustChanged(string giver, string personal, string publicPart)
		=> Text("AF_ECO_UI_TrustChanged", "[Trust change] {GIVER} increased your personal trust by {PERSONAL}{PUBLIC}", ("GIVER", giver), ("PERSONAL", personal), ("PUBLIC", publicPart));

	public static string PublicTrustGain(object amount)
		=> Text("AF_ECO_UI_PublicTrustGain", ", public trust +{VALUE}", ("VALUE", amount));

	public static string TaskGiverFallback => Text("AF_ECO_UI_TaskGiverFallback", "quest giver");

	public static string EstimatedHits => Text("AF_ECO_UI_EstimatedHits", "estimated hits");

	public static string ActualHits => Text("AF_ECO_UI_ActualHits", "actual hits");

	public static string SettlementTrustGain(string settlement, object local, object shared, string hitType, string direct, string share, string total)
		=> Text("AF_ECO_UI_SettlementTrustGain", "{SETTLEMENT}: settlement trust +{LOCAL}{SHARED}\n{HIT} {DIRECT}, share {SHARE}, total {TOTAL}", ("SETTLEMENT", settlement), ("LOCAL", local), ("SHARED", shared == null ? "" : PublicTrustGain(shared)), ("HIT", hitType), ("DIRECT", direct), ("SHARE", share), ("TOTAL", total));

	public static string SettlementTrustCarry(string settlement, string total, object shared, string hitType, string direct, string share)
		=> Text("AF_ECO_UI_SettlementTrustCarry", "{SETTLEMENT}: settlement trust accumulated +{TOTAL}{SHARED}\n{HIT} {DIRECT}, share {SHARE}; less than 1 point", ("SETTLEMENT", settlement), ("TOTAL", total), ("SHARED", shared == null ? "" : PublicTrustGain(shared)), ("HIT", hitType), ("DIRECT", direct), ("SHARE", share));

	public static string MarketTrustChanged(string market, string personal, string publicPart)
		=> Text("AF_ECO_UI_MarketTrustChanged", "[Market trust change] {MARKET} increased your market trust by {PERSONAL}{PUBLIC}", ("MARKET", market), ("PERSONAL", personal), ("PUBLIC", publicPart));

	public static string QuestTrustChanged(string giver, string personal, string publicPart)
		=> Text("AF_ECO_UI_QuestTrustChanged", "[Trust change] Completing {GIVER}'s quest increased personal trust by {PERSONAL}{PUBLIC}", ("GIVER", giver), ("PERSONAL", personal), ("PUBLIC", publicPart));

	public static string SettlementTrustBattleLost
		=> Text("AF_ECO_UI_SettlementTrustBattleLost", "[Settlement trust settlement] The battle was not won; no settlement trust was gained.");

	public static string SettlementTrustNoNearby
		=> Text("AF_ECO_UI_SettlementTrustNoNearby", "[Settlement trust settlement] No affected settlement was found near this battle.");

	public static string SettlementTrustDetails(string details)
		=> Text("AF_ECO_UI_SettlementTrustDetails", "[Settlement trust settlement]\n{DETAILS}", ("DETAILS", details));

	public static string SettlementTrustNoDefeatedHostile
		=> Text("AF_ECO_UI_SettlementTrustNoDefeatedHostile", "[Settlement trust settlement] No hostile force near a settlement was defeated, so no settlement trust was gained.");

	public static string DebtSummaryNoDebt
		=> Text("AF_ECO_UI_DebtSummaryNoDebt", "No outstanding debt.");

	public static string DebtSummaryGold(object amount)
		=> Text("AF_ECO_UI_DebtSummaryGold", "Total gold debt: {AMOUNT}", ("AMOUNT", amount));

	public static string DebtSummaryItems(string items)
		=> Text("AF_ECO_UI_DebtSummaryItems", "Total item debt: {ITEMS}", ("ITEMS", items));

	public static string DebtSummaryLines(object count)
		=> Text("AF_ECO_UI_DebtSummaryLines", "Unsettled lines: {COUNT}", ("COUNT", count));

	public static string DebtSummaryLine(string id, string type, string remaining, string due, bool itemUnavailable)
		=> Text("AF_ECO_UI_DebtSummaryLine", "- [{ID}] {TYPE}; remaining {REMAINING}{DUE}{UNAVAILABLE}", ("ID", id), ("TYPE", type), ("REMAINING", remaining), ("DUE", string.IsNullOrWhiteSpace(due) ? "" : ", " + due), ("UNAVAILABLE", itemUnavailable ? Text("AF_ECO_UI_DebtSummaryUnavailableSuffix", "; original item unavailable") : ""));

	public static string DebtSummaryMore(object count)
		=> Text("AF_ECO_UI_DebtSummaryMore", "... {COUNT} more line(s) hidden.", ("COUNT", count));

	public static string DebtSummaryReadFailed
		=> Text("AF_ECO_UI_DebtSummaryReadFailed", "Failed to read the debt summary.");

	public static string DebtDueUnlimited
		=> Text("AF_ECO_UI_DebtDueUnlimited", "repayment deadline: unlimited (debt remains active)");

	public static string DebtPromiseCounterpartyForDisplay(string name)
	{
		string value = (name ?? string.Empty).Trim();
		return string.IsNullOrWhiteSpace(value) || string.Equals(value, EconomySourceTexts.Get("S066"), StringComparison.Ordinal)
			? Text("AF_ECO_UI_DebtPromiseCounterparty", "the counterparty")
			: value;
	}

	public static string DebtPromiseNoteForDisplay(string note)
	{
		string value = (note ?? string.Empty).Trim();
		return string.IsNullOrWhiteSpace(value) || string.Equals(value, EconomySourceTexts.Get("S013"), StringComparison.Ordinal)
			? Text("AF_ECO_UI_DebtPromiseNoNote", "None")
			: value;
	}

	public static string DebtPromisePlayer
		=> Text("AF_ECO_UI_DebtPromisePlayer", "the player");

	public static string DebtSeason(int seasonIndex)
		=> seasonIndex switch
		{
			0 => Text("AF_ECO_UI_DebtSeasonSpring", "Spring"),
			1 => Text("AF_ECO_UI_DebtSeasonSummer", "Summer"),
			2 => Text("AF_ECO_UI_DebtSeasonAutumn", "Autumn"),
			_ => Text("AF_ECO_UI_DebtSeasonWinter", "Winter")
		};

	public static string DebtCalendarDate(int year, string season, int day)
		=> Text("AF_ECO_UI_DebtCalendarDate", "Year {YEAR}, {SEASON}, day {DAY}", ("YEAR", year), ("SEASON", season), ("DAY", day));

	public static string DebtDueDays(object days, string date)
		=> Text("AF_ECO_UI_DebtDueDays", "repayment deadline: about {DAYS} day(s) (due {DATE})", ("DAYS", days), ("DATE", date));

	public static string DebtDueToday(string date)
		=> Text("AF_ECO_UI_DebtDueToday", "repayment deadline: due today ({DATE})", ("DATE", date));

	public static string DebtOverdue(object days, string date)
		=> Text("AF_ECO_UI_DebtOverdue", "repayment deadline: {DAYS} day(s) overdue (due {DATE})", ("DAYS", days), ("DATE", date));

	public static string DebtNoDeadline
		=> Text("AF_ECO_UI_DebtNoDeadline", "no deadline");

	public static string PlayerRpForgeConfirmInput
		=> Text("AF_ECO_UI_RpForgeConfirmInput", "Validating input and preparing safe templates... (Esc to cancel)");

	public static string PlayerRpForgePreparingCandidates
		=> Text("AF_ECO_UI_RpForgePreparingCandidates", "Preparing the top 50 safe templates... (Esc to cancel)");

	public static string PlayerRpOutcomeLabel(string outcome)
	{
		string normalized = (outcome ?? string.Empty).Trim().ToLowerInvariant();
		return normalized switch
		{
			"good" => Text("AF_ECO_UI_RpForgeOutcomeGood", "Good"),
			"normal" => Text("AF_ECO_UI_RpForgeOutcomeNormal", "Normal"),
			"bad" => Text("AF_ECO_UI_RpForgeOutcomeBad", "Degraded"),
			"junk" => Text("AF_ECO_UI_RpForgeOutcomeJunk", "Junk item"),
			_ => Text("AF_ECO_UI_RpForgeOutcomeSuccess", "Success")
		};
	}

	public static string PlayerRpConfirmationLine(string label, string value)
		=> Text("AF_ECO_UI_RpForgeConfirmationLine", "{LABEL}: {VALUE}", ("LABEL", label), ("VALUE", value));

	public static string PlayerRpLabelItemName => Text("AF_ECO_UI_RpForgeLabelItemName", "Item name");

	public static string PlayerRpLabelMatchingTemplate => Text("AF_ECO_UI_RpForgeLabelMatchingTemplate", "Matching template");

	public static string PlayerRpLabelTemplateType => Text("AF_ECO_UI_RpForgeLabelTemplateType", "Template type");

	public static string PlayerRpLabelBasePrice => Text("AF_ECO_UI_RpForgeLabelBasePrice", "Base price");

	public static string PlayerRpLabelInvestment => Text("AF_ECO_UI_RpForgeLabelInvestment", "Investment");

	public static string PlayerRpLabelCraftedValue => Text("AF_ECO_UI_RpForgeLabelCraftedValue", "Crafted value");

	public static string PlayerRpConfirmationPriceLine(string label, object amount)
		=> Text("AF_ECO_UI_RpForgeConfirmationPrice", "{LABEL}: {AMOUNT} denars", ("LABEL", label), ("AMOUNT", amount));

	public static string PlayerRpConfirmationPlayerChoice(object count)
		=> Text("AF_ECO_UI_RpForgeConfirmationPlayerChoice", "Template selection: player choice ({COUNT} candidates)", ("COUNT", count));

	public static string PlayerRpConfirmationExactMatch
		=> Text("AF_ECO_UI_RpForgeConfirmationExactMatch", "Template selection: exact game-data match");

	public static string PlayerRpConfirmationGuaranteed
		=> Text("AF_ECO_UI_RpForgeConfirmationGuaranteed", "Craft success chance: 100%\nResult: the item will be crafted successfully.");

	public static string PlayerRpConfirmationChance(string chance)
		=> Text("AF_ECO_UI_RpForgeConfirmationChance", "Craft success chance: {CHANCE}\nOn a miss: a junk item (the investment is still consumed).", ("CHANCE", chance));

	public static string PlayerRpConfirmationQuestion
		=> Text("AF_ECO_UI_RpForgeConfirmationQuestion", "\n\nConfirm crafting?");

	public static string PlayerRpEquipmentWarning(object currentSmithing, object recommendedSmithing, object pricePerLevel)
		=> Text("AF_ECO_UI_RpForgeEquipmentWarning", "\n\nWarning: degradation is more likely than 33.33%; consider raising smithing from {CURRENT} to {RECOMMENDED} (one level per {PRICE} denars of template value).",
			("CURRENT", currentSmithing), ("RECOMMENDED", recommendedSmithing), ("PRICE", pricePerLevel));

	public static string PlayerRpEquipmentEffectRetained(string outcome, string probability, string retained, string weightIncrease)
		=> Text("AF_ECO_UI_RpForgeEquipmentEffectRetained", "{OUTCOME} {PROBABILITY}: positive attributes retained {RETAINED}; weight +{WEIGHT}",
			("OUTCOME", outcome), ("PROBABILITY", probability), ("RETAINED", retained), ("WEIGHT", weightIncrease));

	public static string PlayerRpEquipmentEffectBonus(string outcome, string probability, object bonus, string weightReduction)
		=> Text("AF_ECO_UI_RpForgeEquipmentEffectBonus", "{OUTCOME} {PROBABILITY}: each positive attribute +{VALUE}; weight -{WEIGHT}",
			("OUTCOME", outcome), ("PROBABILITY", probability), ("VALUE", bonus), ("WEIGHT", weightReduction));

	public static string PlayerRpEquipmentEffectStable(string outcome, string probability)
		=> Text("AF_ECO_UI_RpForgeEquipmentEffectStable", "{OUTCOME} {PROBABILITY}: positive attributes and weight unchanged", ("OUTCOME", outcome), ("PROBABILITY", probability));

	public static string PlayerRpCraftCompleted(string name, string outcome, object value, object stamina, object experience, string attributes)
		=> Text("AF_ECO_UI_RpForgeCompleted", "Crafted: {NAME}\nOutcome: {OUTCOME}\nItem value: {VALUE} denars\nCrafting stamina: -{STAMINA}; base crafting XP: +{EXPERIENCE}{ATTRIBUTES}",
			("NAME", name), ("OUTCOME", outcome), ("VALUE", value), ("STAMINA", stamina), ("EXPERIENCE", experience), ("ATTRIBUTES", attributes));

	public static string PlayerRpCraftXpInterrupted
		=> Text("AF_ECO_UI_RpForgeXpInterrupted", "\nBase crafting XP notification was interrupted by another mod; the item and costs were still settled to prevent duplicate rewards.");

	public static string PartyTransferTroopReceived(object amount, string name, bool volunteer)
		=> volunteer
			? Text("AF_ECO_UI_PartyTransferVolunteerReceived", "Recruited {AMOUNT} {NAME}", ("AMOUNT", amount), ("NAME", name))
			: Text("AF_ECO_UI_PartyTransferTroopReceived", "Received {AMOUNT} {NAME}", ("AMOUNT", amount), ("NAME", name));

	public static string PartyTransferPrisonerReceived(object amount, string name, bool hero)
		=> hero
			? Text("AF_ECO_UI_PartyTransferHeroPrisonerReceived", "Received prisoner {NAME}", ("NAME", name))
			: Text("AF_ECO_UI_PartyTransferPrisonerReceived", "Received {AMOUNT} {NAME} prisoners", ("AMOUNT", amount), ("NAME", name));

	public static string PartyTransferBatchSummary(object attempted, object succeeded, object failed, object units, object value)
		=> Text("AF_ECO_UI_PartyTransferBatchSummary", "Party and prisoner transfer: attempted {ATTEMPTED}, succeeded {SUCCEEDED}, failed or partial {FAILED}; transferred {UNITS} units; estimated value {VALUE} denars.",
			("ATTEMPTED", attempted), ("SUCCEEDED", succeeded), ("FAILED", failed), ("UNITS", units), ("VALUE", value));

	public static string LetterDefaultTitle => Text("AF_ECO_UI_LetterDefaultTitle", "Letter");

	public static string ViewLetterTitle => Text("AF_ECO_UI_ViewLetterTitle", "View Letter");

	public static string PopupCloseButton => Text("AF_ECO_UI_PopupCloseButton", "Close");

	public static string RpItemDefaultTitle => Text("AF_ECO_UI_RpItemDefaultTitle", "RP Item");

	public static string ViewRpItemIntroductionTitle => Text("AF_ECO_UI_ViewRpItemIntroductionTitle", "View Item Description");

	public static string RpItemIntroductionPending => Text("AF_ECO_UI_RpItemIntroductionPending", "Item description is generating, please check back later.");

	public static string RpItemIntroductionEmpty => Text("AF_ECO_UI_RpItemIntroductionEmpty", "This RP item has no description yet.");

	public static string PlayerRpCraftFailedFallback => Text("AF_ECO_UI_RpForgeFailed", "Crafting failed.");

	public static string PlayerRpCraftFailedWithException(object error)
		=> Text("AF_ECO_UI_RpForgeFailedException", "Crafting failed: {ERROR}", ("ERROR", error));

	public static string PlayerRpCraftRollbackIncomplete(object issues)
		=> Text("AF_ECO_UI_RpForgeRollbackIncomplete", "Item rollback was not fully completed: {ISSUES}", ("ISSUES", issues));

	public static string PlayerRpCraftRollbackCleaned => Text("AF_ECO_UI_RpForgeRollbackCleaned", "Cleaned up the generated item, records, and runtime caches.");

	public static string PlayerRpCraftRefundNoCharge => Text("AF_ECO_UI_RpForgeRefundNoCharge", "No denars were deducted; no refund needed.");

	public static string PlayerRpCraftRefundFull(object amount)
		=> Text("AF_ECO_UI_RpForgeRefundFull", "Fully refunded {AMOUNT} denars.", ("AMOUNT", amount));

	internal static string FixedAssetStatusForDisplay(string status, FixedAssetDisplayContext context)
	{
		string value = (status ?? string.Empty).Trim();
		if (string.IsNullOrEmpty(value))
		{
			return string.Empty;
		}

		if (string.Equals(value, EconomySourceTexts.Get("S071"), StringComparison.Ordinal))
		{
			return Text("AF_ECO_UI_FixedAssetMissingParties", "Missing transfer counterparties.");
		}
		if (string.Equals(value, EconomySourceTexts.Get("S072"), StringComparison.Ordinal))
		{
			return Text("AF_ECO_UI_FixedAssetNpcOnly", "Postprocess only allows NPCs to transfer fixed assets to the player.");
		}
		if (string.Equals(value, EconomySourceTexts.Get("S073"), StringComparison.Ordinal))
		{
			return Text("AF_ECO_UI_FixedAssetClanMemberBlocked", "Clan members or companions cannot be transferred via fixed assets.");
		}
		if (string.Equals(value, EconomySourceTexts.Get("S074"), StringComparison.Ordinal))
		{
			return Text("AF_ECO_UI_FixedAssetMissingAsset", "Missing transferable fixed asset.");
		}
		if (string.Equals(value, EconomySourceTexts.Get("S075"), StringComparison.Ordinal))
		{
			return Text("AF_ECO_UI_FixedAssetUnknownType", "Unknown fixed asset type.");
		}
		if (string.Equals(value, EconomySourceTexts.Get("S076"), StringComparison.Ordinal))
		{
			return Text("AF_ECO_UI_FixedAssetQuantityMustBeOne", "Fixed asset quantity must be 1.");
		}
		if (string.Equals(value, EconomySourceTexts.Get("S077"), StringComparison.Ordinal))
		{
			return Text("AF_ECO_UI_FixedAssetTargetNoClan", "Target recipient does not have a valid clan.");
		}
		if (string.Equals(value, EconomySourceTexts.Get("S078"), StringComparison.Ordinal))
		{
			return Text("AF_ECO_UI_FixedAssetWorkshopMissingRecipient", "Missing workshop recipient.");
		}
		if (string.Equals(value, EconomySourceTexts.Get("S079"), StringComparison.Ordinal))
		{
			return Text("AF_ECO_UI_FixedAssetWorkshopRecipientInvalid", "Workshop recipient is not a valid living hero.");
		}
		if (string.Equals(value, EconomySourceTexts.Get("S080"), StringComparison.Ordinal))
		{
			return Text("AF_ECO_UI_FixedAssetCaravanMissingRecipient", "Missing caravan recipient.");
		}

		string notFoundPrefix = EconomySourceTexts.Get("S081");
		if (value.StartsWith(notFoundPrefix, StringComparison.Ordinal) && value.EndsWith("）。", StringComparison.Ordinal))
		{
			string token = value.Substring(notFoundPrefix.Length, value.Length - notFoundPrefix.Length - 2);
			return Text("AF_ECO_UI_FixedAssetNotFound", "No transferable fixed asset was found ({TOKEN}).", ("TOKEN", token));
		}

		string unknownDirPrefix = EconomySourceTexts.Get("S082");
		if (value.StartsWith(unknownDirPrefix, StringComparison.Ordinal) && value.EndsWith("。", StringComparison.Ordinal))
		{
			string dir = value.Substring(unknownDirPrefix.Length, value.Length - unknownDirPrefix.Length - 1);
			return Text("AF_ECO_UI_FixedAssetUnknownDirection", "Unknown fixed asset transfer direction ({DIRECTION}).", ("DIRECTION", dir));
		}

		string exPrefix = EconomySourceTexts.Get("S083");
		if (value.StartsWith(exPrefix, StringComparison.Ordinal))
		{
			string err = value.Substring(exPrefix.Length);
			return Text("AF_ECO_UI_FixedAssetException", "Transfer failed with an error: {ERROR}", ("ERROR", err));
		}

		string succSettlementPlayerPrefix = EconomySourceTexts.Get("S084");
		string succSettlementPlayerSuffix = EconomySourceTexts.Get("S085");
		if (value.StartsWith(succSettlementPlayerPrefix, StringComparison.Ordinal) && value.EndsWith(succSettlementPlayerSuffix, StringComparison.Ordinal))
		{
			string asset = value.Substring(succSettlementPlayerPrefix.Length, value.Length - succSettlementPlayerPrefix.Length - succSettlementPlayerSuffix.Length);
			return Text("AF_ECO_UI_FixedAssetSettlementToPlayer", "{ASSET} was transferred to the player's clan (payment not automatically deducted).", ("ASSET", asset));
		}

		if (context.Matches(value, EconomySourceTexts.Get("S086"), EconomySourceTexts.Get("S087"), EconomySourceTexts.Get("S088")))
		{
			return Text("AF_ECO_UI_FixedAssetSettlementToNpc", "Player transferred {ASSET} to {GIVER}'s clan (payment not automatically deducted).", ("ASSET", context.AssetName), ("GIVER", context.GiverName));
		}

		string succAssetPlayerSuffix = EconomySourceTexts.Get("S089");
		if (value.StartsWith(succSettlementPlayerPrefix, StringComparison.Ordinal) && value.EndsWith(succAssetPlayerSuffix, StringComparison.Ordinal))
		{
			string asset = value.Substring(succSettlementPlayerPrefix.Length, value.Length - succSettlementPlayerPrefix.Length - succAssetPlayerSuffix.Length);
			return Text("AF_ECO_UI_FixedAssetAssetToPlayer", "{ASSET} was transferred to the player (payment not automatically deducted).", ("ASSET", asset));
		}

		if (context.Matches(value, EconomySourceTexts.Get("S086"), EconomySourceTexts.Get("S087"), EconomySourceTexts.Get("S090")))
		{
			return Text("AF_ECO_UI_FixedAssetAssetToNpc", "Player transferred {ASSET} to {GIVER} (payment not automatically deducted).", ("ASSET", context.AssetName), ("GIVER", context.GiverName));
		}

		string skipClanPrefix = EconomySourceTexts.Get("S091");
		string skipClanSuffix = EconomySourceTexts.Get("S092");
		if (value.StartsWith(skipClanPrefix, StringComparison.Ordinal) && value.EndsWith(skipClanSuffix, StringComparison.Ordinal))
		{
			string asset = value.Substring(skipClanPrefix.Length, value.Length - skipClanPrefix.Length - skipClanSuffix.Length);
			return Text("AF_ECO_UI_FixedAssetAlreadyTargetClan", "{ASSET} already belongs to the target clan.", ("ASSET", asset));
		}

		string skipPersonSuffix = EconomySourceTexts.Get("S093");
		if (value.StartsWith(skipClanPrefix, StringComparison.Ordinal) && value.EndsWith(skipPersonSuffix, StringComparison.Ordinal))
		{
			string asset = value.Substring(skipClanPrefix.Length, value.Length - skipClanPrefix.Length - skipPersonSuffix.Length);
			return Text("AF_ECO_UI_FixedAssetAlreadyTargetPerson", "{ASSET} already belongs to the target character.", ("ASSET", asset));
		}

		string failPrefix = EconomySourceTexts.Get("S094");
		string notFortSuffix = EconomySourceTexts.Get("S095");
		if (value.StartsWith(failPrefix, StringComparison.Ordinal) && value.EndsWith(notFortSuffix, StringComparison.Ordinal))
		{
			string asset = value.Substring(failPrefix.Length, value.Length - failPrefix.Length - notFortSuffix.Length);
			return Text("AF_ECO_UI_FixedAssetNotFortification", "{ASSET} is not a transferable town or castle.", ("ASSET", asset));
		}

		string noOwnerSuffix = EconomySourceTexts.Get("S096");
		if (value.StartsWith(failPrefix, StringComparison.Ordinal) && value.EndsWith(noOwnerSuffix, StringComparison.Ordinal))
		{
			string asset = value.Substring(failPrefix.Length, value.Length - failPrefix.Length - noOwnerSuffix.Length);
			return Text("AF_ECO_UI_FixedAssetNoValidOwner", "{ASSET} has no recognized valid owner.", ("ASSET", asset));
		}

		string underSiegeSuffix = EconomySourceTexts.Get("S097");
		if (value.StartsWith(failPrefix, StringComparison.Ordinal) && value.EndsWith(underSiegeSuffix, StringComparison.Ordinal))
		{
			string asset = value.Substring(failPrefix.Length, value.Length - failPrefix.Length - underSiegeSuffix.Length);
			return Text("AF_ECO_UI_FixedAssetUnderSiege", "{ASSET} is currently in battle or under siege and cannot be transferred.", ("ASSET", asset));
		}

		if (context.Matches(value, EconomySourceTexts.Get("S094"), EconomySourceTexts.Get("S098"), EconomySourceTexts.Get("S099")))
		{
			return Text("AF_ECO_UI_FixedAssetNotGiverClan", "{ASSET} does not belong to {GIVER}'s clan and cannot be transferred to the player.", ("ASSET", context.AssetName), ("GIVER", context.GiverName));
		}

		string notPlayerClanSuffix = EconomySourceTexts.Get("S100");
		if (value.StartsWith(failPrefix, StringComparison.Ordinal) && value.EndsWith(notPlayerClanSuffix, StringComparison.Ordinal))
		{
			string asset = value.Substring(failPrefix.Length, value.Length - failPrefix.Length - notPlayerClanSuffix.Length);
			return Text("AF_ECO_UI_FixedAssetNotPlayerClan", "{ASSET} currently does not belong to the player's clan.", ("ASSET", asset));
		}

		string notWorkshopSuffix = EconomySourceTexts.Get("S101");
		if (value.StartsWith(failPrefix, StringComparison.Ordinal) && value.EndsWith(notWorkshopSuffix, StringComparison.Ordinal))
		{
			string asset = value.Substring(failPrefix.Length, value.Length - failPrefix.Length - notWorkshopSuffix.Length);
			return Text("AF_ECO_UI_FixedAssetNotWorkshop", "{ASSET} is not a workshop that can be safely transferred.", ("ASSET", asset));
		}

		string noWorkshopOwnerSuffix = EconomySourceTexts.Get("S102");
		if (value.StartsWith(failPrefix, StringComparison.Ordinal) && value.EndsWith(noWorkshopOwnerSuffix, StringComparison.Ordinal))
		{
			string asset = value.Substring(failPrefix.Length, value.Length - failPrefix.Length - noWorkshopOwnerSuffix.Length);
			return Text("AF_ECO_UI_FixedAssetNoWorkshopOwner", "{ASSET} currently has no recognized workshop owner.", ("ASSET", asset));
		}

		string workshopOwnerDeadSuffix = EconomySourceTexts.Get("S103");
		if (value.StartsWith(failPrefix, StringComparison.Ordinal) && value.EndsWith(workshopOwnerDeadSuffix, StringComparison.Ordinal))
		{
			string asset = value.Substring(failPrefix.Length, value.Length - failPrefix.Length - workshopOwnerDeadSuffix.Length);
			return Text("AF_ECO_UI_FixedAssetWorkshopOwnerDead", "{ASSET}'s current workshop owner is invalid or deceased.", ("ASSET", asset));
		}

		if (context.Matches(value, EconomySourceTexts.Get("S094"), EconomySourceTexts.Get("S098"), EconomySourceTexts.Get("S104")))
		{
			return Text("AF_ECO_UI_FixedAssetNotGiver", "{ASSET} does not belong to {GIVER} and cannot be transferred to the player.", ("ASSET", context.AssetName), ("GIVER", context.GiverName));
		}

		string notPlayerSuffix = EconomySourceTexts.Get("S105");
		if (value.StartsWith(failPrefix, StringComparison.Ordinal) && value.EndsWith(notPlayerSuffix, StringComparison.Ordinal))
		{
			string asset = value.Substring(failPrefix.Length, value.Length - failPrefix.Length - notPlayerSuffix.Length);
			return Text("AF_ECO_UI_FixedAssetNotPlayer", "{ASSET} currently does not belong to the player.", ("ASSET", asset));
		}

		string notCaravanSuffix = EconomySourceTexts.Get("S106");
		if (value.StartsWith(failPrefix, StringComparison.Ordinal) && value.EndsWith(notCaravanSuffix, StringComparison.Ordinal))
		{
			string asset = value.Substring(failPrefix.Length, value.Length - failPrefix.Length - notCaravanSuffix.Length);
			return Text("AF_ECO_UI_FixedAssetNotCaravan", "{ASSET} is not a transferable caravan.", ("ASSET", asset));
		}

		string caravanEventSuffix = EconomySourceTexts.Get("S107");
		if (value.StartsWith(failPrefix, StringComparison.Ordinal) && value.EndsWith(caravanEventSuffix, StringComparison.Ordinal))
		{
			string asset = value.Substring(failPrefix.Length, value.Length - failPrefix.Length - caravanEventSuffix.Length);
			return Text("AF_ECO_UI_FixedAssetCaravanInEvent", "{ASSET} is currently in an event or battle and cannot be transferred.", ("ASSET", asset));
		}

		string caravanOwnerDeadSuffix = EconomySourceTexts.Get("S108");
		if (value.StartsWith(failPrefix, StringComparison.Ordinal) && value.EndsWith(caravanOwnerDeadSuffix, StringComparison.Ordinal))
		{
			string asset = value.Substring(failPrefix.Length, value.Length - failPrefix.Length - caravanOwnerDeadSuffix.Length);
			return Text("AF_ECO_UI_FixedAssetCaravanOwnerDead", "{ASSET}'s current caravan owner is invalid or deceased.", ("ASSET", asset));
		}

		string caravanNoHomeSuffix = EconomySourceTexts.Get("S109");
		if (value.StartsWith(failPrefix, StringComparison.Ordinal) && value.EndsWith(caravanNoHomeSuffix, StringComparison.Ordinal))
		{
			string asset = value.Substring(failPrefix.Length, value.Length - failPrefix.Length - caravanNoHomeSuffix.Length);
			return Text("AF_ECO_UI_FixedAssetCaravanNoHomeSettlement", "{ASSET} lacks an available home settlement for the caravan.", ("ASSET", asset));
		}

		return status ?? string.Empty;
	}

	public static string KingdomTransferStatusForDisplay(string status)
	{
		string value = (status ?? string.Empty).Trim();
		if (string.IsNullOrEmpty(value))
		{
			return string.Empty;
		}

		if (string.Equals(value, EconomySourceTexts.Get("S110"), StringComparison.Ordinal))
		{
			return Text("AF_ECO_UI_KingdomTransferPlayerClanNotFound", "Player clan was not found.");
		}
		if (string.Equals(value, EconomySourceTexts.Get("S111"), StringComparison.Ordinal))
		{
			return Text("AF_ECO_UI_KingdomTransferNotKing", "The conversation partner is not a reigning king who can abdicate.");
		}
		if (string.Equals(value, EconomySourceTexts.Get("S112"), StringComparison.Ordinal))
		{
			return Text("AF_ECO_UI_KingdomTransferNoKingdom", "The conversation partner has no valid kingdom.");
		}
		if (string.Equals(value, EconomySourceTexts.Get("S113"), StringComparison.Ordinal))
		{
			return Text("AF_ECO_UI_KingdomTransferAlreadyKing", "The player's clan cannot reacquire the crown through abdication.");
		}

		Match mSkip = Regex.Match(value, EconomySourceTexts.Get("S114"));
		if (mSkip.Success)
		{
			return Text("AF_ECO_UI_KingdomTransferAlreadyRuling", "{CLAN} is already the ruling clan of {KINGDOM}.", ("CLAN", mSkip.Groups[1].Value), ("KINGDOM", mSkip.Groups[2].Value));
		}

		Match mNotKing = Regex.Match(value, EconomySourceTexts.Get("S115"));
		if (mNotKing.Success)
		{
			return Text("AF_ECO_UI_KingdomTransferPartnerNotKing", "{GIVER} is not the king of {KINGDOM} and cannot abdicate.", ("GIVER", mNotKing.Groups[1].Value), ("KINGDOM", mNotKing.Groups[2].Value));
		}

		Match mJoinFailed = Regex.Match(value, EconomySourceTexts.Get("S116"));
		if (mJoinFailed.Success)
		{
			return Text("AF_ECO_UI_KingdomTransferJoinFailed", "The player's clan failed to join {KINGDOM}; the crown was not transferred.", ("KINGDOM", mJoinFailed.Groups[1].Value));
		}

		// The producer appends an optional transition summary after the ruling
		// clan sentence, so the tail must be captured rather than swallowed by a
		// greedy ".*": dropping it would hide old-kingdom repair, dissolution and
		// election details from the player.
		Match mSuccess = Regex.Match(value, EconomySourceTexts.Get("S117"));
		if (mSuccess.Success)
		{
			string rulingClanText = Text("AF_ECO_UI_KingdomTransferSuccess", "{GIVER} has abdicated the throne of {KINGDOM} to {PLAYER}; {CLAN} is now the ruling clan.",
				("GIVER", mSuccess.Groups[1].Value), ("KINGDOM", mSuccess.Groups[2].Value), ("PLAYER", mSuccess.Groups[3].Value), ("CLAN", mSuccess.Groups[4].Value));
			return rulingClanText + EconomyRecruitmentDetailTexts.Describe(mSuccess.Groups["tail"].Value);
		}

		string exPrefix = EconomySourceTexts.Get("S083");
		if (value.StartsWith(exPrefix, StringComparison.Ordinal))
		{
			return Text("AF_ECO_UI_KingdomTransferException", "Kingdom transfer failed with an error: {ERROR}", ("ERROR", value.Substring(exPrefix.Length)));
		}

		return status ?? string.Empty;
	}

	public static string FactionIdentityStatusForDisplay(string status)
	{
		string value = (status ?? string.Empty).Trim();
		if (string.IsNullOrEmpty(value))
		{
			return string.Empty;
		}

		if (string.Equals(value, EconomySourceTexts.Get("S118"), StringComparison.Ordinal))
		{
			return Text("AF_ECO_UI_FactionOnlyLeaderCanPledge", "Only the active clan leader can pledge the entire clan.");
		}
		if (string.Equals(value, EconomySourceTexts.Get("S119"), StringComparison.Ordinal))
		{
			return Text("AF_ECO_UI_FactionNoKingdomToAccept", "The player currently has no valid kingdom to accept the clan.");
		}
		if (string.Equals(value, EconomySourceTexts.Get("S120"), StringComparison.Ordinal))
		{
			return Text("AF_ECO_UI_FactionPlayerNotRuler", "The player is not the ruler of the kingdom and cannot approve clan allegiance directly.");
		}
		if (string.Equals(value, EconomySourceTexts.Get("S121"), StringComparison.Ordinal))
		{
			return Text("AF_ECO_UI_FactionTargetKingdomInvalid", "The target kingdom is invalid or has been eliminated.");
		}
		if (string.Equals(value, EconomySourceTexts.Get("S122"), StringComparison.Ordinal))
		{
			return Text("AF_ECO_UI_FactionNpcClanNotFound", "The NPC's clan could not be found.");
		}
		if (string.Equals(value, EconomySourceTexts.Get("S123"), StringComparison.Ordinal))
		{
			return Text("AF_ECO_UI_FactionCannotRePledgePlayerClan", "The player's clan cannot re-pledge allegiance through this NPC.");
		}
		if (string.Equals(value, EconomySourceTexts.Get("S124"), StringComparison.Ordinal))
		{
			return Text("AF_ECO_UI_FactionOnlyClanLeaderCanChoose", "Only the clan leader can pledge the clan's allegiance to a kingdom.");
		}
		if (string.Equals(value, EconomySourceTexts.Get("S125"), StringComparison.Ordinal))
		{
			return Text("AF_ECO_UI_FactionPlayerNotInKingdom", "The player is not currently part of any faction.");
		}
		if (string.Equals(value, EconomySourceTexts.Get("S126"), StringComparison.Ordinal))
		{
			return Text("AF_ECO_UI_FactionNoKingdomForTarget", "The player currently has no kingdom for the target clan to join.");
		}
		if (string.Equals(value, EconomySourceTexts.Get("S127"), StringComparison.Ordinal))
		{
			return Text("AF_ECO_UI_FactionPartnerNotVassal", "The conversation partner is not a vassal of this faction and cannot sign a mercenary contract.");
		}
		if (string.Equals(value, EconomySourceTexts.Get("S128"), StringComparison.Ordinal))
		{
			return Text("AF_ECO_UI_FactionPlayerAlreadyVassal", "The player is already a vassal of a kingdom and cannot join as a mercenary.");
		}
		if (string.Equals(value, EconomySourceTexts.Get("S129"), StringComparison.Ordinal))
		{
			return Text("AF_ECO_UI_FactionOnlyKingGrantsVassalage", "Only the king of the target kingdom can grant official vassalage.");
		}
		if (string.Equals(value, EconomySourceTexts.Get("S110"), StringComparison.Ordinal))
		{
			return Text("AF_ECO_UI_FactionPlayerClanNotFound", "The player's clan could not be found.");
		}

		// The leave-authorization failure embeds both the partner's kingdom and the
		// kingdom the player is being asked to leave; both must survive projection.
		string leaveMismatchPrefix = EconomySourceTexts.Get("S130");
		if (value.StartsWith(leaveMismatchPrefix, StringComparison.Ordinal))
		{
			Match mLeaveMismatch = Regex.Match(value, EconomySourceTexts.Get("S131"));
			if (mLeaveMismatch.Success)
			{
				return Text("AF_ECO_UI_FactionLeavePartyMismatch",
					"The conversation partner belongs to {PARTY} and cannot approve on behalf of {KINGDOM} the player's departure from the current service.",
					("PARTY", mLeaveMismatch.Groups[1].Value), ("KINGDOM", mLeaveMismatch.Groups[2].Value));
			}
		}

		// These three failures embed a raw id/token produced by the caller rather
		// than a localized name, so the token is passed through as-is.
		Match mTargetKingdomIdInvalid = Regex.Match(value, EconomySourceTexts.Get("S132"));
		if (mTargetKingdomIdInvalid.Success)
		{
			return Text("AF_ECO_UI_FactionTargetKingdomIdInvalid", "The target kingdom id is invalid ({TOKEN}).",
				("TOKEN", mTargetKingdomIdInvalid.Groups[1].Value));
		}

		Match mTargetFactionInvalid = Regex.Match(value, EconomySourceTexts.Get("S133"));
		if (mTargetFactionInvalid.Success)
		{
			return Text("AF_ECO_UI_FactionTargetFactionInvalid", "The target faction is invalid ({TOKEN}).",
				("TOKEN", mTargetFactionInvalid.Groups[1].Value));
		}

		string unknownServicePrefix = EconomySourceTexts.Get("S134");
		if (value.StartsWith(unknownServicePrefix, StringComparison.Ordinal))
		{
			string serviceType = value.Substring(unknownServicePrefix.Length).TrimEnd('。');
			if (serviceType.Length > 0)
			{
				return Text("AF_ECO_UI_FactionUnknownServiceType", "Unknown faction service type {SERVICE_TYPE}.",
					("SERVICE_TYPE", serviceType));
			}
		}

		// The clan-leader branch wraps an arbitrary exception message; it must be
		// recognized before the generic clan-join failure pattern can claim it.
		string clanLeaderExceptionPrefix = EconomySourceTexts.Get("S135");
		if (value.StartsWith(clanLeaderExceptionPrefix, StringComparison.Ordinal))
		{
			return Text("AF_ECO_UI_FactionClanLeaderException", "Clan leader allegiance failed with an error: {ERROR}",
				("ERROR", value.Substring(clanLeaderExceptionPrefix.Length)));
		}

		Match mSkipPledge = Regex.Match(value, EconomySourceTexts.Get("S136"));
		if (mSkipPledge.Success)
		{
			return Text("AF_ECO_UI_FactionAlreadyServesKingdom", "{CLAN} already serves {KINGDOM}.", ("CLAN", mSkipPledge.Groups[1].Value), ("KINGDOM", mSkipPledge.Groups[2].Value));
		}

		Match mClanDead = Regex.Match(value, EconomySourceTexts.Get("S137"));
		if (mClanDead.Success)
		{
			return Text("AF_ECO_UI_FactionClanEliminated", "{CLAN} has been eliminated or is unavailable.", ("CLAN", mClanDead.Groups[1].Value));
		}

		Match mClanJoinFail = Regex.Match(value, EconomySourceTexts.Get("S138"));
		if (mClanJoinFail.Success)
		{
			return Text("AF_ECO_UI_FactionClanJoinFailed", "{CLAN} failed to join {KINGDOM}.", ("CLAN", mClanJoinFail.Groups[1].Value), ("KINGDOM", mClanJoinFail.Groups[2].Value));
		}

		// Each of the following producers appends two independent optional detail
		// blocks after the "（KingdomId=…）" marker: a carried-fief summary and a
		// transition summary.  Both are captured into "tail" and re-rendered by
		// EconomyRecruitmentDetailTexts, which returns anything it does not
		// recognise verbatim, so no detail is silently dropped.
		Match mMercToLord = Regex.Match(value, EconomySourceTexts.Get("S139"));
		if (mMercToLord.Success)
		{
			return Text("AF_ECO_UI_FactionMercToLordSuccess", "{CLAN} ended its mercenary contract and joined {KINGDOM} as a lord.", ("CLAN", mMercToLord.Groups[1].Value), ("KINGDOM", mMercToLord.Groups[2].Value))
				+ EconomyRecruitmentDetailTexts.Describe(mMercToLord.Groups["tail"].Value);
		}

		Match mDefectToLord = Regex.Match(value, EconomySourceTexts.Get("S140"));
		if (mDefectToLord.Success)
		{
			return Text("AF_ECO_UI_FactionDefectToLordSuccess", "{CLAN} defected from {OLD_KINGDOM} and joined {KINGDOM} as a lord.", ("CLAN", mDefectToLord.Groups[1].Value), ("OLD_KINGDOM", mDefectToLord.Groups[2].Value), ("KINGDOM", mDefectToLord.Groups[3].Value))
				+ EconomyRecruitmentDetailTexts.Describe(mDefectToLord.Groups["tail"].Value);
		}

		Match mJoinAsLord = Regex.Match(value, EconomySourceTexts.Get("S141"));
		if (mJoinAsLord.Success)
		{
			return Text("AF_ECO_UI_FactionJoinAsLordSuccess", "{CLAN} joined {KINGDOM} as a lord.", ("CLAN", mJoinAsLord.Groups[1].Value), ("KINGDOM", mJoinAsLord.Groups[2].Value))
				+ EconomyRecruitmentDetailTexts.Describe(mJoinAsLord.Groups["tail"].Value);
		}

		Match mEndMerc = Regex.Match(value, EconomySourceTexts.Get("S142"));
		if (mEndMerc.Success)
		{
			return Text("AF_ECO_UI_FactionPlayerEndMercSuccess", "The player ended their mercenary contract with {KINGDOM}.", ("KINGDOM", mEndMerc.Groups[1].Value));
		}

		Match mLeaveVassal = Regex.Match(value, EconomySourceTexts.Get("S143"));
		if (mLeaveVassal.Success)
		{
			return Text("AF_ECO_UI_FactionPlayerLeaveVassalSuccess", "The player left {KINGDOM} and is no longer an official vassal.", ("KINGDOM", mLeaveVassal.Groups[1].Value));
		}

		Match mJoinMerc = Regex.Match(value, EconomySourceTexts.Get("S144"));
		if (mJoinMerc.Success)
		{
			return Text("AF_ECO_UI_FactionPlayerJoinMercSuccess", "The player joined {KINGDOM} as a mercenary.", ("KINGDOM", mJoinMerc.Groups[1].Value))
				+ EconomyRecruitmentDetailTexts.Describe(mJoinMerc.Groups["tail"].Value);
		}

		Match mAlreadyVassal = Regex.Match(value, EconomySourceTexts.Get("S145"));
		if (mAlreadyVassal.Success)
		{
			return Text("AF_ECO_UI_FactionPlayerAlreadyVassalSkip", "The player is already an official vassal of {KINGDOM}.", ("KINGDOM", mAlreadyVassal.Groups[1].Value));
		}

		Match mFailVassal = Regex.Match(value, EconomySourceTexts.Get("S146"));
		if (mFailVassal.Success)
		{
			return Text("AF_ECO_UI_FactionPlayerFailVassal", "The player failed to become an official vassal of {KINGDOM}.", ("KINGDOM", mFailVassal.Groups[1].Value));
		}

		Match mJoinVassal = Regex.Match(value, EconomySourceTexts.Get("S147"));
		if (mJoinVassal.Success)
		{
			return Text("AF_ECO_UI_FactionPlayerJoinVassalSuccess", "The player joined {KINGDOM} as an official vassal.", ("KINGDOM", mJoinVassal.Groups[1].Value))
				+ EconomyRecruitmentDetailTexts.Describe(mJoinVassal.Groups["tail"].Value);
		}

		string exPrefix = EconomySourceTexts.Get("S083");
		if (value.StartsWith(exPrefix, StringComparison.Ordinal))
		{
			return Text("AF_ECO_UI_FactionException", "Faction action failed with an error: {ERROR}", ("ERROR", value.Substring(exPrefix.Length)));
		}

		return status ?? string.Empty;
	}
}
