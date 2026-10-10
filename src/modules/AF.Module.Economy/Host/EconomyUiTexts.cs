using System;
using TaleWorlds.Localization;

namespace AnimusForge;

/// <summary>
/// Player-facing reward and economy notifications.  Facts, prompts, logs and
/// operation protocol text remain owned by their callers; this helper only
/// resolves text that is sent to the campaign information feed or quick info.
/// </summary>
internal static class EconomyUiTexts
{
	private static string Text(string id, string fallback, params (string Name, object Value)[] variables)
	{
		TextObject text = new TextObject("{=" + id + "}" + fallback);
		if (variables != null)
		{
			foreach ((string name, object value) in variables)
			{
				text.SetTextVariable(name, value?.ToString() ?? string.Empty);
			}
		}
		return text.ToString();
	}

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
		if (string.Equals(value, "参数无效：缺少NPC或债务ID。", StringComparison.Ordinal))
		{
			return Text("AF_ECO_UI_DebtStatusInvalidNpc", "Invalid debt request: the NPC or debt ID is missing.");
		}
		if (string.Equals(value, "未找到该NPC的债务记录。", StringComparison.Ordinal))
		{
			return Text("AF_ECO_UI_DebtStatusNpcRecordMissing", "No debt record was found for this NPC.");
		}
		if (string.Equals(value, "该NPC当前没有可还债务。", StringComparison.Ordinal))
		{
			return Text("AF_ECO_UI_DebtStatusNpcNoDebt", "This NPC has no debt that can currently be repaid.");
		}
		if (string.Equals(value, "参数无效：缺少市场债主或债务ID。", StringComparison.Ordinal))
		{
			return Text("AF_ECO_UI_DebtStatusInvalidMarket", "Invalid market debt request: the market creditor or debt ID is missing.");
		}
		if (string.Equals(value, "未找到该市场的债务记录。", StringComparison.Ordinal))
		{
			return Text("AF_ECO_UI_DebtStatusMarketRecordMissing", "No debt record was found for this market.");
		}
		if (string.Equals(value, "未找到可结算的债务ID，或该债务已清。", StringComparison.Ordinal))
		{
			return Text("AF_ECO_UI_DebtStatusLineMissing", "No unsettled debt line with that ID was found.");
		}

		const string marker = " 已按协商解除（";
		const string prefix = "债务ID ";
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
}
