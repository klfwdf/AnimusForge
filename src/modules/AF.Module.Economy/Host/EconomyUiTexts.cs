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
}
