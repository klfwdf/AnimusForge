using System;
using TaleWorlds.Localization;

namespace AnimusForge;

/// <summary>
/// Player-facing WorldMap command text. Runtime protocol, AFEF facts and logs
/// remain owned by their callers; this helper only formats visible notices.
/// </summary>
internal static class WorldMapUiText
{
	private static string Text(string id, string fallback, params (string Name, object Value)[] variables)
	{
		TextObject text = new TextObject("{=" + id + "}" + fallback);
		if (variables != null)
		{
			foreach ((string name, object value) in variables)
			{
				text.SetTextVariable(name, value?.ToString() ?? "");
			}
		}
		return text.ToString();
	}

	public static string GoToSettlement(string actor, string settlement, int days, bool holdUntilSpecified)
		=> holdUntilSpecified
			? Text("AF_WM_GoToSettlementUntil", "{ACTOR} is heading to {SETTLEMENT}; it will stay there until the specified deadline.", ("ACTOR", actor), ("SETTLEMENT", settlement))
			: Text("AF_WM_GoToSettlement", "{ACTOR} is heading to {SETTLEMENT} and will stay there for {DAYS} days.", ("ACTOR", actor), ("SETTLEMENT", settlement), ("DAYS", days));

	public static string Patrol(string actor, string settlement, int days)
		=> Text("AF_WM_Patrol", "{ACTOR} is heading near {SETTLEMENT} and will patrol for {DAYS} days.", ("ACTOR", actor), ("SETTLEMENT", settlement), ("DAYS", days));

	public static string SettlementAttack(string actor, string settlement, string action, int days, string mode)
	{
		string localizedAction = string.Equals(action, "raid", StringComparison.OrdinalIgnoreCase)
			? Text("AF_WM_ActionRaid", "raid")
			: string.Equals(action, "besiege", StringComparison.OrdinalIgnoreCase)
				? Text("AF_WM_ActionBesiege", "besiege")
				: action;
		return Text("AF_WM_SettlementAttack", "{ACTOR} is moving toward {SETTLEMENT} to {ACTION}; limit: {DAYS} days ({MODE}).", ("ACTOR", actor), ("SETTLEMENT", settlement), ("ACTION", localizedAction), ("DAYS", days), ("MODE", mode));
	}

	public static string WaitForHero(string actor, string settlement, string hero)
		=> Text("AF_WM_WaitForHero", "{ACTOR} is heading outside {SETTLEMENT} and waiting for {HERO} to leave before attacking.", ("ACTOR", actor), ("SETTLEMENT", settlement), ("HERO", hero));

	public static string TrackHero(string actor, string hero, int days, string mode)
		=> Text("AF_WM_TrackHero", "{ACTOR} is tracking {HERO}'s party to attack; limit: {DAYS} days ({MODE}).", ("ACTOR", actor), ("HERO", hero), ("DAYS", days), ("MODE", mode));

	public static string TrackParty(string actor, string party, int days, string mode)
		=> Text("AF_WM_TrackParty", "{ACTOR} is tracking {PARTY} to attack; limit: {DAYS} days ({MODE}).", ("ACTOR", actor), ("PARTY", party), ("DAYS", days), ("MODE", mode));

	public static string Merge(string hero, bool foreignGuest)
		=> foreignGuest
			? Text("AF_WM_MergeForeign", "{HERO} is returning to the player's party and will merge as a guest force while keeping the original clan identity.", ("HERO", hero))
			: Text("AF_WM_Merge", "{HERO} is returning to the player's party to regroup and transfer troops.", ("HERO", hero));

	public static string FollowStart(string actor, string target, int days)
		=> Text("AF_WM_FollowStart", "{ACTOR} is heading to and following {TARGET} for {DAYS} days.", ("ACTOR", actor), ("TARGET", target), ("DAYS", days));

	public static string HideoutTravel(string actor, string settlement)
		=> Text("AF_WM_HideoutTravel", "{ACTOR} is heading to {SETTLEMENT} to clear the hideout.", ("ACTOR", actor), ("SETTLEMENT", settlement));

	public static string HideoutEngage(string actor, string settlement)
		=> Text("AF_WM_HideoutEngage", "{ACTOR} is starting a hideout-clear battle at {SETTLEMENT}; the native battle resolves the outcome and casualties.", ("ACTOR", actor), ("SETTLEMENT", settlement));

	public static string ForeignGuestDepart(string hero)
		=> Text("AF_WM_ForeignGuestDepart", "{HERO}'s faction is now at war with the player; the foreign guest hero has left the player's party.", ("HERO", hero));

	public static string ForeignGuestDetachmentReleased(string hero)
		=> Text("AF_WM_ForeignGuestDetachmentReleased", "{HERO}'s faction is now at war with the player; the foreign guest detachment is no longer under player control.", ("HERO", hero));

	public static string PartyName(string hero)
		=> Text("AF_WM_PartyName", "{HERO}'s party", ("HERO", hero));

	public static string MissingExternalSource
		=> Text("AF_WM_CommandMissingSource", "World-map command failed: the external command source is missing.");

	public static string InvalidHeroOrSettlement
		=> Text("AF_WM_CommandInvalidHeroSettlement", "World-map command failed: no valid hero or settlement was found.");

	public static string ExistingCommand(string hero)
		=> Text("AF_WM_CommandExisting", "{HERO} already has another world-map command and cannot be reassigned to the feast yet.", ("HERO", hero));

	public static string StopMissingHeroOrSource
		=> Text("AF_WM_StopMissingHeroSource", "Stopping the world-map command failed: the hero or command source is missing.");

	public static string StopWrongSource(string hero)
		=> Text("AF_WM_StopWrongSource", "{HERO}'s current command does not belong to this feast source and was not stopped.", ("HERO", hero));

	public static string CannotControlPlayerParty
		=> Text("AF_WM_CannotControlPlayerParty", "World-map command failed: the player's own party cannot be commanded this way.");

	public static string NoControllableParty(string hero)
		=> Text("AF_WM_NoControllableParty", "World-map command failed: {HERO} has no independent controllable party.", ("HERO", hero));

	public static string NoValidCommands
		=> Text("AF_WM_NoValidCommands", "World-map command failed: no command passed ID validation.");

	public static string WaitingForBattle(string actor, bool replacing)
		=> replacing
			? Text("AF_WM_WaitingBattleReplace", "{ACTOR} is waiting for the current battle to resolve safely; the world-map command cannot be replaced yet.", ("ACTOR", actor))
			: Text("AF_WM_WaitingBattleAppend", "{ACTOR} is waiting for the current battle to resolve safely; new world-map commands cannot be appended yet.", ("ACTOR", actor));

	public static string IsolatedSource(string actor)
		=> Text("AF_WM_IsolatedSource", "{ACTOR} is executing an isolated-source command; the chat command did not change that queue.", ("ACTOR", actor));

	public static string QueueAccepted(string actor, int count, bool started)
		=> Text(started ? "AF_WM_QueueStarted" : "AF_WM_QueueAppended", "{ACTOR} {ACTION} {COUNT} world-map command(s).", ("ACTOR", actor), ("ACTION", Text(started ? "AF_WM_QueueStartedAction" : "AF_WM_QueueAppendedAction", started ? "started executing" : "appended")), ("COUNT", count));

	public static string NonHeroNoParty
		=> Text("AF_WM_NonHeroNoParty", "World-map command failed: the current non-hero speaker has no field party that can be taken over.");

	public static string NonHeroQueueUnavailable
		=> Text("AF_WM_NonHeroQueueUnavailable", "World-map command failed: a stable non-hero party command queue could not be created.");

	public static string AcceptedQueue(string actor, int count, string conversion)
		=> Text("AF_WM_AcceptedQueue", "{ACTOR} accepted the world-map command queue ({COUNT} command(s){CONVERSION}).", ("ACTOR", actor), ("COUNT", count), ("CONVERSION", conversion));

	public static string GovernorInvalidState
		=> Text("AF_WM_GovernorInvalidState", "This governor is dead, captured, disabled, or not currently eligible to lead an expedition.");

	public static string GovernorNotCurrent
		=> Text("AF_WM_GovernorNotCurrent", "This hero is no longer the valid current governor of a town or castle.");

	public static string GovernorAway
		=> Text("AF_WM_GovernorAway", "The governor is not currently in the settlement under their authority.");

	public static string GovernorClanMismatch
		=> Text("AF_WM_GovernorClanMismatch", "The settlement ownership does not match the governor's clan.");

	public static string GovernorAlreadyAttached
		=> Text("AF_WM_GovernorAlreadyAttached", "The governor already belongs to another independent party or prisoner party.");

	public static string GovernorCannotLead
		=> Text("AF_WM_GovernorCannotLead", "The governor cannot lead a party because of a quest or native game rule.");

	public static string GovernorSettlementBusy(string settlement)
		=> Text("AF_WM_GovernorSettlementBusy", "{SETTLEMENT} is under siege or in battle; its garrison cannot be drafted.", ("SETTLEMENT", settlement));

	public static string GovernorGarrisonUnavailable(string settlement)
		=> Text("AF_WM_GovernorGarrisonUnavailable", "{SETTLEMENT} has no garrison that can be safely drafted.", ("SETTLEMENT", settlement));

	public static string GovernorValidationFailed(string error)
		=> Text("AF_WM_GovernorValidationFailed", "Governor expedition eligibility check failed: {ERROR}", ("ERROR", error));

	public static string GovernorGarrisonUnavailableGeneric
		=> Text("AF_WM_GovernorGarrisonUnavailableGeneric", "The garrison party is unavailable.");

	public static string GovernorTroopsInsufficient(int minimum, int reserve)
		=> Text("AF_WM_GovernorTroopsInsufficient", "Fewer than {MINIMUM} healthy garrison troops can be drafted; at least {RESERVE} regular troops must remain and the governor party limit must be respected.", ("MINIMUM", minimum), ("RESERVE", reserve));

	public static string GovernorPlanIncomplete
		=> Text("AF_WM_GovernorPlanIncomplete", "The garrison draft plan could not produce a complete troop list.");

	public static string GovernorNoTask
		=> Text("AF_WM_GovernorNoTask", "No valid task command is available for the expedition.");

	public static string GovernorRequestChanged
		=> Text("AF_WM_GovernorRequestChanged", "The governor's settlement or clan changed while the request was waiting.");

	public static string GovernorIdentityChanged
		=> Text("AF_WM_GovernorIdentityChanged", "The governor identity or settlement ownership changed before party creation.");

	public static string GovernorStarted(string hero, string settlement, int troops)
		=> Text("AF_WM_GovernorStarted", "{HERO} drafted {TROOPS} troops from the garrison of {SETTLEMENT} and began the expedition.", ("HERO", hero), ("SETTLEMENT", settlement), ("TROOPS", troops));

	public static string GovernorCreationFailed(string error)
		=> Text("AF_WM_GovernorCreationFailed", "Creating the governor expedition failed: {ERROR}", ("ERROR", error));

	public static string MergeEligibilityFailure(string reason, string actor)
	{
		string id;
		string fallback;
		switch ((reason ?? "").Trim())
		{
			case "not_player_companion_family_or_registered_detachment": id = "AF_WM_MergeNotPlayerMember"; fallback = "World-map command failed: {ACTOR} is not a player clan member, companion, or registered temporary detachment and cannot merge back."; break;
			case "foreign_guest_missing_foreign_clan": id = "AF_WM_MergeMissingForeignClan"; fallback = "World-map command failed: {ACTOR} is not a player member and has no foreign clan identity to preserve."; break;
			case "foreign_guest_inactive_or_captive": id = "AF_WM_MergeInactiveOrCaptive"; fallback = "World-map command failed: {ACTOR} is dead, inactive, or captive and cannot join as a foreign guest force."; break;
			case "foreign_guest_not_own_lord_party": id = "AF_WM_MergeNotOwnParty"; fallback = "World-map command failed: {ACTOR} is not leading their own clan's lord party and cannot merge the whole party."; break;
			case "foreign_guest_is_governor": id = "AF_WM_MergeGovernor"; fallback = "World-map command failed: {ACTOR} is still a governor; the foreign guest force cannot merge without removing the native office."; break;
			case "foreign_guest_in_army": id = "AF_WM_MergeInArmy"; fallback = "World-map command failed: {ACTOR} is currently in an army and must leave it before merging into the player's party."; break;
			case "foreign_guest_at_war": id = "AF_WM_MergeAtWar"; fallback = "World-map command failed: {ACTOR}'s faction is at war with the player and cannot merge as a foreign guest force."; break;
			case "not_independent_wilderness_party": id = "AF_WM_MergeNoIndependentParty"; fallback = "World-map command failed: {ACTOR} has no independent field party to merge."; break;
			case "hero_party_mismatch": id = "AF_WM_MergePartyMismatch"; fallback = "World-map command failed: {ACTOR} no longer leads the field party and the merge was stopped."; break;
			case "hero_missing_from_party_roster": id = "AF_WM_MergeHeroMissing"; fallback = "World-map command failed: {ACTOR} is not in the field party roster and the merge was stopped."; break;
			case "main_party_invalid": id = "AF_WM_MergeMainPartyInvalid"; fallback = "World-map command failed: the player's main party is unavailable for the merge."; break;
			default: id = "AF_WM_MergeEligibilityUnknown"; fallback = "World-map command failed: {ACTOR} does not meet the merge requirements ({REASON})."; break;
		}
		return Text(id, fallback, ("ACTOR", actor), ("REASON", (reason ?? "").Trim()));
	}

	public static string ConversionMessage(int hostile, int besieged)
	{
		if (hostile <= 0 && besieged <= 0) return "";
		if (hostile > 0 && besieged > 0)
			return Text("AF_WM_ConversionBoth", ", hostile settlements were handled as AI attacks and besieged settlements were changed to attacks on the besieging force.", ("HOSTILE", hostile), ("BESIEGED", besieged));
		return besieged > 0
			? Text("AF_WM_ConversionBesieged", ", besieged settlements were changed to attacks on the besieging force.", ("COUNT", besieged))
			: Text("AF_WM_ConversionHostile", ", hostile settlements were handled as AI attacks.", ("COUNT", hostile));
	}

	public static string GovernorRequestQueued(string hero, string reason)
		=> Text("AF_WM_GovernorRequestQueued", "Recorded {HERO}'s governor expedition request; {REASON}. The garrison will be drafted and the expedition formed after the blocking screen or conversation closes.", ("HERO", hero), ("REASON", reason));

	public static string CompanionRequestQueued(string hero, string reason)
		=> Text("AF_WM_CompanionRequestQueued", "Recorded {HERO}'s companion-party request; {REASON}. The native party screen will open after returning to the world map.", ("HERO", hero), ("REASON", reason));

	public static string Result(string id, string fallback, params (string Name, object Value)[] variables)
		=> Text(id, fallback, variables);
}
