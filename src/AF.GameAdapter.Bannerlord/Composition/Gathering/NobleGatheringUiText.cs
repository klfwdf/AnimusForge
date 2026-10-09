using TaleWorlds.Localization;

namespace AnimusForge;

internal static class NobleGatheringUiText
{
	private static string Resolve(string id, string fallback) => new TextObject("{=" + id + "}" + fallback).ToString();
	private static string Format(string id, string fallback, string name, object value)
	{
		TextObject text = new TextObject("{=" + id + "}" + fallback);
		text.SetTextVariable(name, value?.ToString() ?? string.Empty);
		return text.ToString();
	}
	private static string Format2(string id, string fallback, string firstName, object firstValue, string secondName, object secondValue)
	{
		TextObject text = new TextObject("{=" + id + "}" + fallback);
		text.SetTextVariable(firstName, firstValue?.ToString() ?? string.Empty);
		text.SetTextVariable(secondName, secondValue?.ToString() ?? string.Empty);
		return text.ToString();
	}
	private static string Format3(string id, string fallback, string firstName, object firstValue, string secondName, object secondValue, string thirdName, object thirdValue)
	{
		TextObject text = new TextObject("{=" + id + "}" + fallback);
		text.SetTextVariable(firstName, firstValue?.ToString() ?? string.Empty);
		text.SetTextVariable(secondName, secondValue?.ToString() ?? string.Empty);
		text.SetTextVariable(thirdName, thirdValue?.ToString() ?? string.Empty);
		return text.ToString();
	}
	private static string Format4(string id, string fallback, string firstName, object firstValue, string secondName, object secondValue, string thirdName, object thirdValue, string fourthName, object fourthValue)
	{
		TextObject text = new TextObject("{=" + id + "}" + fallback);
		text.SetTextVariable(firstName, firstValue?.ToString() ?? string.Empty);
		text.SetTextVariable(secondName, secondValue?.ToString() ?? string.Empty);
		text.SetTextVariable(thirdName, thirdValue?.ToString() ?? string.Empty);
		text.SetTextVariable(fourthName, fourthValue?.ToString() ?? string.Empty);
		return text.ToString();
	}

	internal static string NoHostSettlement => Resolve("AF_DIP_UI_Gathering_NoHostSettlement", "The clan has no town or castle available as a gathering venue.");
	internal static string NoVenue => Resolve("AF_DIP_UI_Gathering_NoVenue", "No eligible gathering venue is available.");
	internal static string VenueTitle => Resolve("AF_DIP_UI_Gathering_VenueTitle", "Host a gathering: choose a venue");
	internal static string VenueDescription => Resolve("AF_DIP_UI_Gathering_VenueDescription", "The venue must be a town or castle owned by your clan.");
	internal static string Next => Resolve("AF_DIP_UI_Gathering_Next", "Next");
	internal static string VenueNext => Resolve("AF_DIP_UI_Gathering_Next", "Next");
	internal static string Cancel => Resolve("AF_DIP_UI_Gathering_Cancel", "Cancel");
	internal static string VenueAvailable => Resolve("AF_DIP_UI_Gathering_VenueAvailable", "Available as the gathering venue.");
	internal static string NoClans => Resolve("AF_DIP_UI_Gathering_NoClans", "There are no noble clans available to invite.");
	internal static string ConfirmCurrent(int count) => Format("AF_DIP_UI_Gathering_ConfirmCurrent", "Confirm current list ({COUNT})", "COUNT", count);
	internal static string ClearList => Resolve("AF_DIP_UI_Gathering_ClearList", "Clear selected guests");
	internal static string ClearHint => Resolve("AF_DIP_UI_Gathering_ClearHint", "Choose the gathering guests again.");
	internal static string ClanSelected(int count) => Format("AF_DIP_UI_Gathering_ClanSelected", "selected {COUNT}", "COUNT", count);
	internal static string ClanSelectedHint(int count) => Format("AF_DIP_UI_Gathering_ClanSelectedHint", "This clan has {COUNT} selected guest(s).", "COUNT", count);
	internal static string ClanTitle => Resolve("AF_DIP_UI_Gathering_ClanTitle", "Host a gathering: choose clans");
	internal static string ClanDescription(string settlement, int count) => Format2("AF_DIP_UI_Gathering_ClanDescription", "Venue: {SETTLEMENT}\nSelected guests: {COUNT}.\nChoose a clan to open its member list.", "SETTLEMENT", settlement, "COUNT", count);
	internal static string NoHeroes => Resolve("AF_DIP_UI_Gathering_NoHeroes", "The selected clan has no noble members to show.");
	internal static string SelectedMarker => Resolve("AF_DIP_UI_Gathering_SelectedMarker", "[Selected] ");
	internal static string HeroLabel(string clan, string hero, string identity) => Format3("AF_DIP_UI_Gathering_HeroLabel", "[{CLAN}] {HERO} ({IDENTITY})", "CLAN", clan, "HERO", hero, "IDENTITY", identity);
	internal static string HeroHint(string hero, string clan, string identity, string state) => Format4("AF_DIP_UI_Gathering_HeroHint", "{HERO} / {CLAN}\n{IDENTITY}\n{STATE}", "HERO", hero, "CLAN", clan, "IDENTITY", identity, "STATE", state);
	internal static string AlreadyInList => Resolve("AF_DIP_UI_Gathering_AlreadyInList", "Already on the current guest list.");
	internal static string CanInvite => Resolve("AF_DIP_UI_Gathering_CanInvite", "Available to invite; heroes without an independent party will travel to the venue using the native travel system.");
	internal static string CannotInvite(string reason) => Format("AF_DIP_UI_Gathering_CannotInvite", "Cannot invite: {REASON}", "REASON", reason);
	internal static string HeroTitle => Resolve("AF_DIP_UI_Gathering_HeroTitle", "Host a gathering: choose guests");
	internal static string HeroDescription(string clan, string settlement, int count) => Format3("AF_DIP_UI_Gathering_HeroDescription", "Clan: {CLAN}\nThis is a player-hosted gathering; governors may still be invited. Heroes with independent parties are dispatched directly. Nobles remaining in towns, governors, and heroes without parties travel to the venue using the native system, then return to their original settlement after the gathering.\nSelected guests: {COUNT}.", "CLAN", clan, "SETTLEMENT", settlement, "COUNT", count);
	internal static string AddToList => Resolve("AF_DIP_UI_Gathering_AddToList", "Add to list");
	internal static string Back => Resolve("AF_DIP_UI_Gathering_Back", "Back");
	internal static string NoGuests => Resolve("AF_DIP_UI_Gathering_NoGuests", "Gathering not started: no guests were selected.");
	internal static string ConfirmTitle => Resolve("AF_DIP_UI_Gathering_ConfirmTitle", "Confirm gathering");
	internal static string ConfirmBody(string settlement, int cost, int days, int guests) => Format4("AF_DIP_UI_Gathering_ConfirmBody", "Venue: {SETTLEMENT}\nCost: {COST} denars\nDuration: {DAYS} days\nGuests: {GUESTS}\n\nConfirming will deduct the cost and send gathering invitations to the guests.", "SETTLEMENT", settlement, "COST", cost, "DAYS", days, "GUESTS", guests);
	internal static string PayAndInvite => Resolve("AF_DIP_UI_Gathering_PayAndInvite", "Pay and send invitations");
	internal static string NotSelected => Resolve("AF_DIP_UI_Gathering_NotSelected", "No guests selected yet.");
	internal static string SelectedGuests(string names, int count, bool more) => Format2(more ? "AF_DIP_UI_Gathering_SelectedGuestsMore" : "AF_DIP_UI_Gathering_SelectedGuestsExact", more ? "Selected: {NAMES} and {COUNT} more." : "Selected: {NAMES}", "NAMES", names, "COUNT", count);
	internal static string IdentityUnknown => Resolve("AF_DIP_UI_Gathering_IdentityUnknown", "Unknown identity");
	internal static string Governor => Resolve("AF_DIP_UI_Gathering_Governor", "Governor");
	internal static string Ruler => Resolve("AF_DIP_UI_Gathering_Ruler", "Ruler");
	internal static string ClanLeader => Resolve("AF_DIP_UI_Gathering_ClanLeader", "Clan leader");
	internal static string Lady => Resolve("AF_DIP_UI_Gathering_Lady", "Lady");
	internal static string Lord => Resolve("AF_DIP_UI_Gathering_Lord", "Lord");
	internal static string IdentityHint(string identity, string settlement) => string.IsNullOrWhiteSpace(settlement) ? Format("AF_DIP_UI_Gathering_IdentityHint", "Identity: {IDENTITY}", "IDENTITY", identity) : Format2("AF_DIP_UI_Gathering_IdentityHintGoverned", "Identity: {IDENTITY}\nGoverned settlement: {SETTLEMENT}", "IDENTITY", identity, "SETTLEMENT", settlement);
	internal static string UnknownClan => Resolve("AF_DIP_UI_Gathering_UnknownClan", "Unknown clan");
	internal static string UnknownSettlement => Resolve("AF_DIP_UI_Gathering_UnknownSettlement", "Unknown settlement");
	internal static string ClanHint(int members, int movable, int travel) => Format3("AF_DIP_UI_Gathering_ClanHint", "Members: {MEMBERS}; independent parties: {MOVABLE}; can travel to a gathering by the native system: {TRAVEL}.", "MEMBERS", members, "MOVABLE", movable, "TRAVEL", travel);
	internal static string EndPlayerCancelled(string place, string reason) => Format2("AF_DIP_UI_Gathering_EndPlayerCancelled", "Gathering cancelled: your gathering at {PLACE} has ended.{REASON}", "PLACE", place, "REASON", reason);
	internal static string EndPlayerFinished(string place) => Format("AF_DIP_UI_Gathering_EndPlayerFinished", "Gathering ended: your gathering at {PLACE} has ended.", "PLACE", place);
	internal static string EndNpcCancelled(string host, string place, string reason) => Format3("AF_DIP_UI_Gathering_EndNpcCancelled", "Gathering cancelled: {HOST}'s gathering at {PLACE} has ended.{REASON}", "HOST", host, "PLACE", place, "REASON", reason);
	internal static string EndNpcFinished(string host, string place) => Format2("AF_DIP_UI_Gathering_EndNpcFinished", "Gathering ended: {HOST}'s gathering at {PLACE} has ended.", "HOST", host, "PLACE", place);
	internal static string Reason(string reason) => string.IsNullOrWhiteSpace(reason) ? string.Empty : Format("AF_DIP_UI_Gathering_Reason", " Reason: {REASON}", "REASON", reason);
	internal static string DialogueHostOption => Resolve("AF_DIP_UI_Gathering_DialogueHostOption", "Host a gathering");
	internal static string DialogueCompanionOption => Resolve("AF_DIP_UI_Gathering_DialogueCompanionOption", "I want to host a gathering. Help me arrange it.");
	internal static string DialogueGovernorResponse => Resolve("AF_DIP_UI_Gathering_DialogueGovernorResponse", "I will prepare the guest list and invitations.");
	internal static string DialogueCompanionResponse => Resolve("AF_DIP_UI_Gathering_DialogueCompanionResponse", "Understood. I will prepare the venue and guest list.");
	internal static string Disabled => Resolve("AF_DIP_UI_Gathering_Disabled", "Gatherings are disabled in MCM.");
	internal static string Created(string settlement, int guests, int days) => Format3("AF_DIP_UI_Gathering_Created", "Gathering invitations sent: {SETTLEMENT}; {GUESTS} guest(s), {DAYS} days.", "SETTLEMENT", settlement, "GUESTS", guests, "DAYS", days);
	internal static string DisabledCancelled => Resolve("AF_DIP_UI_Gathering_DisabledCancelled", "Gatherings were disabled; active gatherings have been cancelled and hosts and guests will return over time.");
	internal static string GuestArrived(string hero, string settlement) => Format2("AF_DIP_UI_Gathering_GuestArrived", "{HERO} arrived at {SETTLEMENT} for the gathering.", "HERO", hero, "SETTLEMENT", settlement);
	internal static string AttendanceReward(string host) => Format("AF_DIP_UI_Gathering_AttendanceReward", "You attended {HOST}'s gathering and your relation with the host increased.", "HOST", host);
	internal static string CrisisWar => Resolve("AF_DIP_UI_Gathering_CrisisWar", "The kingdom has entered a war; the host decided to cancel the gathering.");
	internal static string CrisisSiege(string settlement) => Format("AF_DIP_UI_Gathering_CrisisSiege", "{SETTLEMENT} in the kingdom is under siege; the host decided to cancel the gathering.", "SETTLEMENT", settlement);
	internal static string NpcCreated(string host, string settlement) => Format2("AF_DIP_UI_Gathering_NpcCreated", "{HOST} will host a gathering at {SETTLEMENT}.", "HOST", host, "SETTLEMENT", settlement);
	internal static string CourierLetter(string host, string player, string settlement, string endDate) => Format4("AF_DIP_UI_Gathering_CourierLetter", "{HOST} to {PLAYER}:\n\nI will host a gathering at {SETTLEMENT} and invite you to attend. It is expected to last until {DATE}; arrive at the venue if you wish to join.", "HOST", host, "PLAYER", player, "SETTLEMENT", settlement, "DATE", endDate);
}
