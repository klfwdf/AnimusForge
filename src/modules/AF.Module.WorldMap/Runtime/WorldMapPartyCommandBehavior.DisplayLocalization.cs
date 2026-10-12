using System;
using System.Text.RegularExpressions;

namespace AnimusForge;

public sealed partial class WorldMapPartyCommandBehavior
{
	// Rules are prepared once; only notification events project text, never queue ticks or saved facts.
	private sealed class WorldMapDisplayRule
	{
		internal readonly string Id;
		internal readonly string Fallback;
		internal readonly Regex Pattern;
		internal readonly string[] Variables;

		internal WorldMapDisplayRule(string suffix, string pattern, string fallback)
		{
			Id = "AF_WM_Review_" + suffix;
			Fallback = fallback;
			Pattern = new Regex(pattern, RegexOptions.CultureInvariant);
			Variables = Pattern.GetGroupNames();
		}
	}

	private static readonly WorldMapDisplayRule[] AdditionalWorldMapDisplayRules =
	{
		new WorldMapDisplayRule("RebellionNoFiefs", WorldMapSourceTexts.Get("raw_literals.wm_lit_0240"), "The player's clan has no fiefs and cannot rebel while retaining land."),
		new WorldMapDisplayRule("RebellionWarMissing", WorldMapSourceTexts.Get("raw_literals.wm_lit_0241"), "The rebellion action failed to establish war with the former kingdom."),
		new WorldMapDisplayRule("RebellionException", WorldMapSourceTexts.Get("raw_literals.wm_lit_0242"), "The player's clan rebellion failed: {DETAIL}"),
		new WorldMapDisplayRule("RebellionStarted", WorldMapSourceTexts.Get("raw_literals.wm_lit_0243"), "The player's clan retained its fiefs and rebelled against {KINGDOM}; {HERO} then continued {ACTION} {TARGET}."),
		new WorldMapDisplayRule("TroopXpInputs", WorldMapSourceTexts.Get("raw_literals.wm_lit_0244"), "Troop XP adjustment parameters are invalid: {TROOP}"),
		new WorldMapDisplayRule("TroopXpWrite", WorldMapSourceTexts.Get("raw_literals.wm_lit_0245"), "Troop XP write verification failed: {TROOP}; context={CONTEXT}"),
		new WorldMapDisplayRule("ArmyMaintained", WorldMapSourceTexts.Get("raw_literals.wm_lit_0246"), "{ACTOR} spent {COST} influence to maintain the army and continue {ACTION} {SETTLEMENT} (leader influence: {INFLUENCE})."),
		new WorldMapDisplayRule("MergeCompleted", WorldMapSourceTexts.Get("raw_literals.wm_lit_0247"), "{HERO} met the player's party and transferred {TROOPS} members, {PRISONERS} prisoners, and all supplies; the temporary expedition record was cleared."),
		new WorldMapDisplayRule("GuestMergeCompleted", WorldMapSourceTexts.Get("raw_literals.wm_lit_0248"), "{HERO} met the player's party and transferred {TROOPS} members, {PRISONERS} prisoners, and all supplies; the original party was disbanded. {GUEST} accompanies the player as a foreign-clan guest, retaining clan, occupation, and companion status."),
		new WorldMapDisplayRule("GovernorReturned", WorldMapSourceTexts.Get("raw_literals.wm_lit_0249"), "{HERO}'s expedition returned to {SETTLEMENT}; troops, prisoners, and supplies were placed and the temporary party was removed.{SHIPS} {RESTORATION}"),
		new WorldMapDisplayRule("ForeignGuestWarDeparture", WorldMapSourceTexts.Get("raw_literals.wm_lit_0250"), "{HERO}'s faction is at war with the player, so the foreign-clan guest left the player's party and returned to {DESTINATION}; troops and supplies previously merged into the player's party were not automatically withdrawn."),
		new WorldMapDisplayRule("ForeignGuestPartyReleased", WorldMapSourceTexts.Get("raw_literals.wm_lit_0251"), "{HERO}'s faction is at war with the player, so the foreign-clan guest party left player control and returned to the original clan's native AI; its troops and supplies were not automatically transferred to the player's party."),
		new WorldMapDisplayRule("HoldUntilCompleted", WorldMapSourceTexts.Get("raw_literals.wm_lit_0252"), "{ACTOR} completed the order to stay in {SETTLEMENT} until the specified date."),
		new WorldMapDisplayRule("HoldDaysCompleted", WorldMapSourceTexts.Get("raw_literals.wm_lit_0253"), "{ACTOR} completed the order to stay in {SETTLEMENT} for {DAYS} days."),
		new WorldMapDisplayRule("PatrolCompleted", WorldMapSourceTexts.Get("raw_literals.wm_lit_0254"), "{ACTOR} completed the order to patrol near {SETTLEMENT} for {DAYS} days."),
		new WorldMapDisplayRule("FollowCompleted", WorldMapSourceTexts.Get("raw_literals.wm_lit_0255"), "{ACTOR} completed the order to follow {TARGET} for {DAYS} days."),
		new WorldMapDisplayRule("PartyCreationCancelled", WorldMapSourceTexts.Get("raw_literals.wm_lit_0256"), "Companion party creation for {HERO} was cancelled; follow-up world-map commands were not executed."),
		new WorldMapDisplayRule("PartyCreated", WorldMapSourceTexts.Get("raw_literals.wm_lit_0257"), "{HERO} created a companion party and received {TROOPS} troops and {PRISONERS} prisoners."),
		new WorldMapDisplayRule("QueueSafeExit", WorldMapSourceTexts.Get("raw_literals.wm_lit_0258"), "{ACTOR}'s world-map command queue ended; after the current battle resolves, the party will safely leave the siege and resume native behavior."),
		new WorldMapDisplayRule("TimeoutCommand", WorldMapSourceTexts.Get("raw_literals.wm_lit_0259"), "{ACTOR}'s world-map command timed out; the current command was skipped."),
		new WorldMapDisplayRule("TimeoutTravel", WorldMapSourceTexts.Get("raw_literals.wm_lit_0260"), "{ACTOR} could not reach {SETTLEMENT} before the time limit; the travel command was skipped."),
		new WorldMapDisplayRule("TimeoutPatrol", WorldMapSourceTexts.Get("raw_literals.wm_lit_0261"), "{ACTOR} could not reach the area near {SETTLEMENT} before the time limit; the patrol command was skipped."),
		new WorldMapDisplayRule("TimeoutFollow", WorldMapSourceTexts.Get("raw_literals.wm_lit_0262"), "{ACTOR} could not catch up with {TARGET} before the time limit; the follow command was skipped."),
		new WorldMapDisplayRule("TimeoutMerge", WorldMapSourceTexts.Get("raw_literals.wm_lit_0263"), "{ACTOR} could not meet the player's party before the time limit; the return command was skipped."),
		new WorldMapDisplayRule("RaidCompleted", WorldMapSourceTexts.Get("raw_literals.wm_lit_0264"), "{HERO} completed the raid on {SETTLEMENT}."),
		new WorldMapDisplayRule("PartyDefeated", WorldMapSourceTexts.Get("raw_literals.wm_lit_0265"), "{PARTY}'s party was defeated."),
		new WorldMapDisplayRule("PartyRepelled", WorldMapSourceTexts.Get("raw_literals.wm_lit_0266"), "{PARTY}'s party was repelled."),
		new WorldMapDisplayRule("SettlementCaptured", WorldMapSourceTexts.Get("raw_literals.wm_lit_0267"), "{SETTLEMENT} was captured."),
		new WorldMapDisplayRule("GovernorNoReturnTarget", WorldMapSourceTexts.Get("raw_literals.wm_lit_0268"), "{HERO}'s governor expedition has no safe clan town or castle to return to; the party was preserved and returned to native AI."),
		new WorldMapDisplayRule("GovernorReturnUnsafe", WorldMapSourceTexts.Get("raw_literals.wm_lit_0269"), "{HERO}'s governor expedition could not safely transfer assets on return; the party was preserved and returned to native AI."),
		new WorldMapDisplayRule("HideoutMissingResult", WorldMapSourceTexts.Get("raw_literals.wm_lit_0270"), "The native battle ended without a confirmed hideout result; the attack was not repeated."),
		new WorldMapDisplayRule("HideoutQuestBusy", WorldMapSourceTexts.Get("raw_literals.wm_lit_0271"), "The target is reserved by a story or quest; the hideout attack was not started."),
		new WorldMapDisplayRule("HideoutUnavailable", WorldMapSourceTexts.Get("raw_literals.wm_lit_0272"), "The target is unavailable or safe battle resolution is not ready; the hideout attack was not started."),
		new WorldMapDisplayRule("HideoutAlreadyEmpty", WorldMapSourceTexts.Get("raw_literals.wm_lit_0273"), "The target no longer has bandits; another party's clearance was not counted as this command's success."),
		new WorldMapDisplayRule("HideoutActorIneligible", WorldMapSourceTexts.Get("raw_literals.wm_lit_0274"), "The executing party cannot fight the target bandits; no war was declared and the target was not forcibly removed."),
		new WorldMapDisplayRule("HideoutBattleNotCreated", WorldMapSourceTexts.Get("raw_literals.wm_lit_0275"), "The native hideout battle could not be created."),
		new WorldMapDisplayRule("HideoutCleared", WorldMapSourceTexts.Get("raw_literals.wm_lit_0276"), "The native battle was won and the resident bandits were cleared."),
		new WorldMapDisplayRule("HideoutDefeated", WorldMapSourceTexts.Get("raw_literals.wm_lit_0277"), "The native battle determined that the clearing party was defeated."),
		new WorldMapDisplayRule("HideoutIncomplete", WorldMapSourceTexts.Get("raw_literals.wm_lit_0278"), "No definitive clearance was confirmed, or the target still has resident bandits."),
		new WorldMapDisplayRule("TimeoutNoResult", WorldMapSourceTexts.Get("raw_literals.wm_lit_0279"), "The command timed out without a definitive result."),
		new WorldMapDisplayRule("PartyCreationInvalid", WorldMapSourceTexts.Get("raw_literals.wm_lit_0280"), "The new task party was not created correctly."),
		new WorldMapDisplayRule("GovernorResignFailed", WorldMapSourceTexts.Get("raw_literals.wm_lit_0281"), "The current governor could not safely step down."),
		new WorldMapDisplayRule("GovernorPartyInvalid", WorldMapSourceTexts.Get("raw_literals.wm_lit_0282"), "The native lord party was not correctly created for this governor and clan."),
		new WorldMapDisplayRule("GovernorRosterMissing", WorldMapSourceTexts.Get("raw_literals.wm_lit_0283"), "The new lord party roster is unavailable."),
		new WorldMapDisplayRule("GovernorUnexpectedHero", WorldMapSourceTexts.Get("raw_literals.wm_lit_0284"), "An unexpected hero appeared in the new lord party; creation was stopped."),
		new WorldMapDisplayRule("GovernorTemplateRemaining", WorldMapSourceTexts.Get("raw_literals.wm_lit_0285"), "Native template troops, prisoners, or ships could not be fully removed."),
		new WorldMapDisplayRule("GovernorTemplateShips", WorldMapSourceTexts.Get("raw_literals.wm_lit_0286"), "Native lord party template ships could not be removed."),
		new WorldMapDisplayRule("GovernorTransferInputs", WorldMapSourceTexts.Get("raw_literals.wm_lit_0287"), "The garrison, expedition party, or troop transfer plan is unavailable."),
		new WorldMapDisplayRule("GovernorTransferTroop", WorldMapSourceTexts.Get("raw_literals.wm_lit_0288"), "The garrison transfer plan contains an invalid troop type."),
		new WorldMapDisplayRule("GovernorTroopChanged", WorldMapSourceTexts.Get("raw_literals.wm_lit_0289"), "Garrison troop state changed before transfer: {TROOP}"),
		new WorldMapDisplayRule("GovernorTroopDeduction", WorldMapSourceTexts.Get("raw_literals.wm_lit_0290"), "Garrison troop deduction verification failed: {TROOP}"),
		new WorldMapDisplayRule("GovernorXpDeduction", WorldMapSourceTexts.Get("raw_literals.wm_lit_0291"), "Garrison XP deduction was inconsistent: {TROOP}"),
		new WorldMapDisplayRule("GovernorTroopWrite", WorldMapSourceTexts.Get("raw_literals.wm_lit_0292"), "Expedition troop insertion verification failed: {TROOP}"),
		new WorldMapDisplayRule("GovernorConservation", WorldMapSourceTexts.Get("raw_literals.wm_lit_0293"), "Garrison and expedition troop or XP conservation verification failed: {TROOP}"),
		new WorldMapDisplayRule("GovernorTransferFailure", WorldMapSourceTexts.Get("raw_literals.wm_lit_0294"), "Governor expedition troop transfer failed: troop={TROOP} step={STEP}"),
		new WorldMapDisplayRule("MergeSourceHasRemainingAssets", WorldMapSourceTexts.Get("raw_literals.wm_lit_0295"), "Source party still has untransferred assets after merging into main party."),
		new WorldMapDisplayRule("GovernorRecordMissingIdentity", WorldMapSourceTexts.Get("raw_literals.wm_lit_0296"), "Governor expedition record is missing required identity information."),
		new WorldMapDisplayRule("GovernorRollbackMissingTroop", WorldMapSourceTexts.Get("raw_literals.wm_lit_0297"), "Governor expedition roster rollback is missing troop or roster."),
		new WorldMapDisplayRule("GovernorRollbackTroopFailed", WorldMapSourceTexts.Get("raw_literals.wm_lit_0298"), "Governor expedition roster exact rollback failed: {TROOP}"),
		new WorldMapDisplayRule("GarrisonRollbackMissingSnapshot", WorldMapSourceTexts.Get("raw_literals.wm_lit_0299"), "Garrison rollback is missing valid transaction snapshot."),
		new WorldMapDisplayRule("GarrisonRollbackFailed", WorldMapSourceTexts.Get("raw_literals.wm_lit_0300"), "Garrison transaction snapshot rollback verification failed: {TROOP}"),
		new WorldMapDisplayRule("GarrisonRosterSnapshotMismatch", WorldMapSourceTexts.Get("raw_literals.wm_lit_0301"), "Full garrison roster does not match pre-creation snapshot."),
		new WorldMapDisplayRule("GovernorLeaderNotInRoster", WorldMapSourceTexts.Get("raw_literals.wm_lit_0302"), "Expedition leader hero is no longer in member roster."),
		new WorldMapDisplayRule("MergeChangedHeroIdentity", WorldMapSourceTexts.Get("raw_literals.wm_lit_0303"), "Merge altered accompanying hero's clan, occupation, or companion status: {HERO}"),
		new WorldMapDisplayRule("MergeCountConservationFailed", WorldMapSourceTexts.Get("raw_literals.wm_lit_0304"), "Roster head count or wounded conservation verification failed: {TROOP}"),
		new WorldMapDisplayRule("MergeXpBoundaryFailed", WorldMapSourceTexts.Get("raw_literals.wm_lit_0305"), "Roster experience boundary verification failed: {TROOP}"),
		new WorldMapDisplayRule("MergeTransferFailed", WorldMapSourceTexts.Get("raw_literals.wm_lit_0306"), "Roster transfer failed: troop={TROOP} step={STEP}"),
		new WorldMapDisplayRule("MergePartiesUnavailable", WorldMapSourceTexts.Get("raw_literals.wm_lit_0307"), "Source party or target party is unavailable during merge."),
		new WorldMapDisplayRule("MergeHeroPartyChanged", WorldMapSourceTexts.Get("raw_literals.wm_lit_0308"), "Accompanying hero's belonging party has changed: {HERO}"),
		new WorldMapDisplayRule("MergeHeroNotMerged", WorldMapSourceTexts.Get("raw_literals.wm_lit_0309"), "Accompanying hero failed to merge into player's main party: {HERO}"),
		new WorldMapDisplayRule("MergeRollbackShipsFailed", WorldMapSourceTexts.Get("raw_literals.wm_lit_0310"), "Merge rollback failed to restore source party ship ownership."),
		new WorldMapDisplayRule("MergeRollbackAssetsMismatch", WorldMapSourceTexts.Get("raw_literals.wm_lit_0311"), "Source party assets after merge rollback do not match transaction snapshot."),
		new WorldMapDisplayRule("MergeRollbackMissingRecords", WorldMapSourceTexts.Get("raw_literals.wm_lit_0312"), "Merge roster rollback is missing transaction records."),
		new WorldMapDisplayRule("MergeRollbackTroopFailed", WorldMapSourceTexts.Get("raw_literals.wm_lit_0313"), "Merge roster transaction snapshot rollback failed: {TROOP}"),
		new WorldMapDisplayRule("MergeRollbackPrisonerFailed", WorldMapSourceTexts.Get("raw_literals.wm_lit_0314"), "Hero prisoner merge rollback failed: {HERO}"),
		new WorldMapDisplayRule("MergeRollbackItemsMissing", WorldMapSourceTexts.Get("raw_literals.wm_lit_0315"), "Merge items rollback is missing source, target, or snapshot."),
		new WorldMapDisplayRule("MergeRollbackItemsDiff", WorldMapSourceTexts.Get("raw_literals.wm_lit_0316"), "Merge items rollback difference inconsistency."),
		new WorldMapDisplayRule("MergeRollbackItemsFailed", WorldMapSourceTexts.Get("raw_literals.wm_lit_0317"), "Merge items rollback verification failed."),
		new WorldMapDisplayRule("MergeItemsConservationFailed", WorldMapSourceTexts.Get("raw_literals.wm_lit_0318"), "Item conservation verification failed."),
		new WorldMapDisplayRule("MergeShipsInvalidSourceTarget", WorldMapSourceTexts.Get("raw_literals.wm_lit_0319"), "Source or target for ship transfer is invalid."),
		new WorldMapDisplayRule("MergeShipsOwnershipChanged", WorldMapSourceTexts.Get("raw_literals.wm_lit_0320"), "Ship ownership changed before transfer."),
		new WorldMapDisplayRule("MergeShipsTransferFailed", WorldMapSourceTexts.Get("raw_literals.wm_lit_0321"), "Ship ownership transfer verification failed."),
		new WorldMapDisplayRule("MergeShipsTransferFailedRollbackFailed", WorldMapSourceTexts.Get("raw_literals.wm_lit_0322"), "Ship transfer failed and could not be fully rolled back."),
		new WorldMapDisplayRule("MergeShipsTransferFailedRollbackDone", WorldMapSourceTexts.Get("raw_literals.wm_lit_0323"), "Ship transfer failed; ownership has been rolled back."),
	};

	private static bool ContainsChineseDisplayText(string text)
	{
		foreach (char ch in text ?? string.Empty)
		{
			if (ch >= '\u3400' && ch <= '\u9fff') return true;
		}
		return false;
	}

	private static bool TryProjectAdditionalWorldMapMessage(string message, out string display)
	{
		foreach (WorldMapDisplayRule rule in AdditionalWorldMapDisplayRules)
		{
			Match match = rule.Pattern.Match(message);
			if (!match.Success) continue;
			var values = new (string Name, object Value)[rule.Variables.Length - 1];
			for (int i = 1; i < rule.Variables.Length; i++)
			{
				string key = rule.Variables[i];
				string value = match.Groups[key].Value;
				if (key == "PRISONERS" && value.Length == 0) value = "0";
				if (key == "ACTION") value = value == WorldMapSourceTexts.Get("raw_literals.wm_lit_0008")
					? WorldMapUiText.Result("AF_WM_Review_Raiding", "raiding")
					: value == WorldMapSourceTexts.Get("raw_literals.wm_lit_0009") ? WorldMapUiText.Result("AF_WM_Review_Besieging", "besieging")
					: WorldMapUiText.Result("AF_WM_Review_Attacking", "attacking");
				if (key == "DETAIL") value = CommandMessageForDisplay(value);
				if (key == "DESTINATION" && value == WorldMapSourceTexts.Get("raw_literals.wm_lit_0324"))
					value = WorldMapUiText.Result("AF_WM_Review_ClanTerritory", "the original clan's territory");
				if (key == "RESTORATION") value = value == WorldMapSourceTexts.Get("raw_literals.wm_lit_0157")
					? WorldMapUiText.Result("AF_WM_Review_GovernorRestored", "The original governorship was restored.")
					: WorldMapUiText.Result("AF_WM_Review_GovernorRetained", "The current governor was not displaced.");
				if (key == "SHIPS" && value.Length > 0)
					value = WorldMapUiText.Result("AF_WM_Review_ShipsReturned", " {COUNT} accompanying ships were transferred to the settlement.",
						("COUNT", value.Substring(0, value.IndexOf(WorldMapSourceTexts.Get("raw_literals.wm_lit_0325")))));
				values[i - 1] = (key, value);
			}
			display = WorldMapUiText.Result(rule.Id, rule.Fallback, values);
			return true;
		}
		display = message;
		return false;
	}
}
