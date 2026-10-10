using System;using System.Collections.Generic;using System.Linq;using System.Text;
namespace AnimusForge;
internal sealed class NpcActionEditorDisplayPort
{
 internal Func<NpcActionEntry,string> ResolveDisplayNameBySettlementEntry;
 internal Func<string,string> ResolveHeroDisplay;
 internal Func<string,string> ResolveClanDisplay;
 internal Func<string,string> ResolveKingdomDisplay;
 internal Func<NpcActionEntry,bool> HasStructuredNpcActionMetadata;
 internal Func<NpcActionEntry,string> BuildNpcActionActorNarrativeText;
 internal Func<string,string> TranslateNpcActionKindForPrompt;
 internal Func<NpcActionEntry,string> BuildNpcActionActorNarrativeForDisplay;
}
// UI-only read projection; actual records and sanitization stay with the NpcAction domain owner.
internal static class NpcActionEditorProjection
{
    private static string T(string key, string fallback, params (string Name, string Value)[] variables)
        => EventEditorUiText.T("AF_NPCACTION_EDITOR_" + key, fallback, variables);

    private static string BuildActorNarrativeForDisplay(NpcActionEditorDisplayPort port, NpcActionEntry entry)
        => port.BuildNpcActionActorNarrativeForDisplay?.Invoke(entry)
            ?? (entry.Text ?? "").Replace("\r", " ").Replace("\n", " ").Trim();

    private static string GetNpcActionKindForDisplay(string actionKind)
    {
        switch ((actionKind ?? "").Trim().ToLowerInvariant())
        {
            case "army_create": return T("KindArmyCreate", "Army formation");
            case "army_gather": return T("KindArmyGather", "Army gathering");
            case "army_disperse": return T("KindArmyDisperse", "Army disbanding");
            case "army_join": return T("KindArmyJoin", "Joining an army");
            case "army_leave": return T("KindArmyLeave", "Leaving an army");
            case "siege_start_attack": return T("KindSiegeStartAttack", "Joining a siege attack");
            case "siege_start_defend": return T("KindSiegeStartDefend", "Joining a siege defense");
            case "siege_end_attack": return T("KindSiegeEndAttack", "Attacker action after a siege");
            case "siege_end_defend": return T("KindSiegeEndDefend", "Defender action after a siege");
            case "siege_join": return T("KindSiegeJoin", "Joining a siege");
            case "siege_leave": return T("KindSiegeLeave", "Leaving a siege");
            case "siege_complete": return T("KindSiegeComplete", "Siege result");
            case "daily_behavior": return T("KindDailyBehavior", "Recent movement");
            case "map_event": return T("KindMapEvent", "Battle engagement");
            case "map_event_aftermath": return T("KindMapEventAftermath", "Battle aftermath");
            case "tournament_finished": return T("KindTournamentFinished", "Tournament victory");
            case "duel_result": return T("KindDuelResult", "Formal duel result");
            case "marriage": return T("KindMarriage", "Marriage");
            case "clan_changed_kingdom": return T("KindClanChangedKingdom", "Clan allegiance change");
            case "clan_defected": return T("KindClanDefected", "Clan defection");
            case "clan_rebellion": return T("KindClanRebellion", "Clan rebellion");
            case "kingdom_decision_concluded": return T("KindKingdomDecision", "Kingdom decision");
            case "bilateral_diplomacy_pending": return T("KindDiplomacyPending", "Bilateral diplomacy reconsideration");
            case "bilateral_diplomacy_outcome": return T("KindDiplomacyOutcome", "Bilateral diplomacy outcome");
            case "ruling_clan_changed": return T("KindRulingClanChanged", "Ruling clan change");
            case "settlement_owner_changed_gain": return T("KindSettlementGain", "Settlement ownership gained");
            case "settlement_owner_changed_loss": return T("KindSettlementLoss", "Settlement ownership lost");
            case "settlement_owner_changed_capture": return T("KindSettlementCapture", "Settlement ownership change");
            case "hero_killed": return T("KindHeroKilled", "Hero death");
            case "hero_executed_victim": return T("KindHeroExecuted", "Execution");
            case "clan_member_killed": return T("KindClanMemberKilled", "Clan member death");
            case "prisoner_taken_captor": return T("KindPrisonerCaptor", "Lord captured");
            case "prisoner_taken_prisoner": return T("KindPrisonerTaken", "Taken prisoner");
            case "prisoner_released_captor": return T("KindPrisonerReleased", "Prisoner released");
            case "prisoner_released_prisoner": return T("KindCaptivityEnded", "Captivity ended");
            case "birth": return T("KindBirth", "Clan birth");
            case "clan_leader_changed": return T("KindClanLeaderChanged", "Clan leader succession");
            default: return "";
        }
    }


	internal static string BuildDevNpcActionItemLabel(NpcActionEditorDisplayPort port, NpcActionEntry entry)
	{
		if (entry == null)
		{
			return T("Invalid", "Invalid action");
		}
		string text = !string.IsNullOrWhiteSpace(entry.GameDate) ? entry.GameDate.Trim() : T("Day", "Day {DAY}", ("DAY", entry.Day.ToString()));
		string text2 = BuildDevNpcActionPreviewText(port, entry);
		if (text2.Length > 108)
		{
			text2 = text2.Substring(0, 108) + "...";
		}
		return text + " " + text2;
	}

	internal static string BuildDevNpcActionDetailSubtitle(NpcActionEditorDisplayPort port, NpcActionEntry entry)
	{
		if (entry == null)
		{
			return T("DetailTitle", "Action record (structured)");
		}
		string text = !string.IsNullOrWhiteSpace(entry.GameDate) ? entry.GameDate.Trim() : T("Day", "Day {DAY}", ("DAY", entry.Day.ToString()));
		string text2 = GetDevNpcActionKindDisplay(port, entry.ActionKind);
		if (string.IsNullOrWhiteSpace(text2))
		{
			return T("DetailDate", "Action record (structured) - {DATE}", ("DATE", text));
		}
		return T("DetailKind", "Action record (structured) - {DATE} / {KIND}", ("DATE", text), ("KIND", text2));
	}

	internal static string BuildDevNpcActionDetailText(NpcActionEditorDisplayPort port, NpcActionEntry entry)
	{
		if (entry == null) return T("Invalid", "Invalid action");
		StringBuilder stringBuilder = new StringBuilder();
		string text = BuildDevNpcActionNarrative(port, entry);
		if (!string.IsNullOrWhiteSpace(text))
		{
			stringBuilder.AppendLine(T("SummaryHeading", "[Readable summary]"));
			stringBuilder.AppendLine(text);
			stringBuilder.AppendLine();
		}
		stringBuilder.AppendLine(T("RawHeading", "[Original fields]"));
		AppendDevNpcActionField(stringBuilder, T("FieldDate", "Date"), !string.IsNullOrWhiteSpace(entry.GameDate) ? entry.GameDate.Trim() : T("Day", "Day {DAY}", ("DAY", entry.Day.ToString())));
		AppendDevNpcActionField(stringBuilder, T("FieldDay", "Day index"), entry.Day.ToString());
		AppendDevNpcActionField(stringBuilder, T("FieldText", "Display text"), (entry.Text ?? "").Trim());
		AppendDevNpcActionField(stringBuilder, "StableKey", (entry.StableKey ?? "").Trim());
		AppendDevNpcActionField(stringBuilder, T("FieldKind", "Action type"), GetDevNpcActionKindDisplay(port, entry.ActionKind));
		AppendDevNpcActionField(stringBuilder, T("FieldMajor", "Major action"), entry.IsMajor ? T("Yes", "Yes") : T("No", "No"));
		AppendDevNpcActionField(stringBuilder, T("FieldResult", "Result"), !entry.Won.HasValue ? "" : (entry.Won.Value ? T("Won", "Victory") : T("Lost", "Defeat")));
		AppendDevNpcActionField(stringBuilder, T("FieldLocation", "Location text"), (entry.LocationText ?? "").Trim());
		AppendDevNpcActionField(stringBuilder, T("FieldSettlement", "Settlement"), port.ResolveDisplayNameBySettlementEntry(entry));
		AppendDevNpcActionField(stringBuilder, T("FieldSettlementId", "Settlement ID"), (entry.SettlementId ?? "").Trim());
		AppendDevNpcActionField(stringBuilder, T("FieldOwnerHero", "Settlement owner"), port.ResolveHeroDisplay(entry.SettlementOwnerHeroId));
		AppendDevNpcActionField(stringBuilder, T("FieldOwnerClan", "Settlement owner clan"), port.ResolveClanDisplay(entry.SettlementOwnerClanId));
		AppendDevNpcActionField(stringBuilder, T("FieldOwnerKingdom", "Settlement owner kingdom"), port.ResolveKingdomDisplay(entry.SettlementOwnerKingdomId));
		AppendDevNpcActionField(stringBuilder, T("FieldPreviousHero", "Previous settlement owner"), port.ResolveHeroDisplay(entry.PreviousSettlementOwnerHeroId));
		AppendDevNpcActionField(stringBuilder, T("FieldPreviousClan", "Previous settlement owner clan"), port.ResolveClanDisplay(entry.PreviousSettlementOwnerClanId));
		AppendDevNpcActionField(stringBuilder, T("FieldPreviousKingdom", "Previous settlement owner kingdom"), port.ResolveKingdomDisplay(entry.PreviousSettlementOwnerKingdomId));
		AppendDevNpcActionField(stringBuilder, T("FieldActorHero", "Acting hero"), port.ResolveHeroDisplay(entry.ActorHeroId));
		AppendDevNpcActionField(stringBuilder, T("FieldActorClan", "Acting clan"), port.ResolveClanDisplay(entry.ActorClanId));
		AppendDevNpcActionField(stringBuilder, T("FieldActorKingdom", "Acting kingdom"), port.ResolveKingdomDisplay(entry.ActorKingdomId));
		AppendDevNpcActionField(stringBuilder, T("FieldTargetHero", "Target hero"), port.ResolveHeroDisplay(entry.TargetHeroId));
		AppendDevNpcActionField(stringBuilder, T("FieldTargetClan", "Target clan"), port.ResolveClanDisplay(entry.TargetClanId));
		AppendDevNpcActionField(stringBuilder, T("FieldTargetKingdom", "Target kingdom"), port.ResolveKingdomDisplay(entry.TargetKingdomId));
		AppendDevNpcActionField(stringBuilder, T("FieldRelatedHeroes", "Related hero IDs"), JoinDevActionIds(entry.RelatedHeroIds));
		AppendDevNpcActionField(stringBuilder, T("FieldRelatedClans", "Related clan IDs"), JoinDevActionIds(entry.RelatedClanIds));
		AppendDevNpcActionField(stringBuilder, T("FieldRelatedKingdoms", "Related kingdom IDs"), JoinDevActionIds(entry.RelatedKingdomIds));
		if (!port.HasStructuredNpcActionMetadata(entry))
		{
			stringBuilder.AppendLine();
			stringBuilder.AppendLine(T("MissingMetadata", "This action has limited structured metadata. It may be an older record, or the game may not have supplied enough target information."));
			stringBuilder.AppendLine(T("NewRecords", "Allow 1-2 game days to pass, then check newly generated action records."));
		}
		return stringBuilder.ToString().TrimEnd();
	}

	internal static string BuildDevNpcActionPreviewText(NpcActionEditorDisplayPort port, NpcActionEntry entry)
	{
		if (entry == null)
		{
			return T("Invalid", "Invalid action");
		}
		List<string> list = new List<string>();
		string text = BuildActorNarrativeForDisplay(port, entry);
		string text2 = GetNpcActionKindForDisplay(entry.ActionKind);
		string text3 = port.ResolveDisplayNameBySettlementEntry(entry);
		string text4 = port.ResolveKingdomDisplay(entry.SettlementOwnerKingdomId);
		string text5 = port.ResolveClanDisplay(entry.SettlementOwnerClanId);
		string text6 = port.ResolveKingdomDisplay(entry.TargetKingdomId);
		string text7 = port.ResolveClanDisplay(entry.TargetClanId);
		string text8 = port.ResolveHeroDisplay(entry.TargetHeroId);
		string text9 = port.ResolveClanDisplay(entry.ActorClanId);
		string text10 = port.ResolveKingdomDisplay(entry.ActorKingdomId);
		if (!string.IsNullOrWhiteSpace(text))
		{
			list.Add(text);
		}
		else if (!string.IsNullOrWhiteSpace(text2))
		{
			list.Add(T("ActionSummary", "Action: {KIND}", ("KIND", text2)));
		}
		if (!string.IsNullOrWhiteSpace(text3) && string.IsNullOrWhiteSpace(entry.SettlementId) && !text.Contains(text3))
		{
			list.Add(T("PreviewLocation", "Location: {SETTLEMENT}", ("SETTLEMENT", text3)));
		}
		if (!string.IsNullOrWhiteSpace(text9) && !string.IsNullOrWhiteSpace(text10))
		{
			list.Add(T("PreviewActorClanKingdom", "Their clan is {CLAN}, which serves {KINGDOM}", ("CLAN", text9), ("KINGDOM", text10)));
		}
		else if (!string.IsNullOrWhiteSpace(text9))
		{
			list.Add(T("PreviewActorClan", "Their clan is {CLAN}", ("CLAN", text9)));
		}
		else if (!string.IsNullOrWhiteSpace(text10))
		{
			list.Add(T("PreviewActorKingdom", "They serve {KINGDOM}", ("KINGDOM", text10)));
		}
		if (!string.IsNullOrWhiteSpace(text5) && !string.IsNullOrWhiteSpace(text4))
		{
			list.Add(T("PreviewOwnerClanKingdom", "At the time, it was controlled by {CLAN} and belonged to {KINGDOM}", ("CLAN", text5), ("KINGDOM", text4)));
		}
		else if (!string.IsNullOrWhiteSpace(text4))
		{
			list.Add(T("PreviewOwnerKingdom", "At the time, it belonged to {KINGDOM}", ("KINGDOM", text4)));
		}
		else if (!string.IsNullOrWhiteSpace(text5))
		{
			list.Add(T("PreviewOwnerClan", "At the time, it was controlled by {CLAN}", ("CLAN", text5)));
		}
		if (!string.IsNullOrWhiteSpace(text8))
		{
			list.Add(T("PreviewTargetHero", "Hero involved: {HERO}", ("HERO", text8)));
		}
		else if (!string.IsNullOrWhiteSpace(text7))
		{
			list.Add(T("PreviewTargetClan", "Clan involved: {CLAN}", ("CLAN", text7)));
		}
		else if (!string.IsNullOrWhiteSpace(text6))
		{
			list.Add(T("PreviewTargetKingdom", "Kingdom involved: {KINGDOM}", ("KINGDOM", text6)));
		}
		if (entry.Won.HasValue)
		{
			list.Add(T("ResultSummary", "Result: {RESULT}", ("RESULT", (entry.Won.Value ? T("Won", "Victory") : T("Lost", "Defeat")))));
		}
		if (entry.IsMajor)
		{
			list.Add(T("PreviewMajor", "Classified as a major action"));
		}
		if (list.Count == 0)
		{
			return T("NoSummary", "No readable summary is available for this record.");
		}
		if (!port.HasStructuredNpcActionMetadata(entry))
		{
			list.Add(T("LimitedMetadata", "This record has limited structured information"));
		}
		return string.Join(T("Separator", "; "), list) + T("Period", ".");
	}

	internal static string BuildDevNpcActionNarrative(NpcActionEditorDisplayPort port, NpcActionEntry entry)
	{
		if (entry == null)
		{
			return "";
		}
		List<string> list = new List<string>();
		string text = !string.IsNullOrWhiteSpace(entry.GameDate) ? entry.GameDate.Trim() : T("Day", "Day {DAY}", ("DAY", entry.Day.ToString()));
		string text2 = GetNpcActionKindForDisplay(entry.ActionKind);
		string text3 = port.ResolveHeroDisplay(entry.ActorHeroId);
		string text15 = string.IsNullOrWhiteSpace(text3) ? T("Actor", "This person") : text3;
		string text4 = port.ResolveDisplayNameBySettlementEntry(entry);
		string text5 = port.ResolveHeroDisplay(entry.TargetHeroId);
		string text6 = port.ResolveClanDisplay(entry.TargetClanId);
		string text7 = port.ResolveKingdomDisplay(entry.TargetKingdomId);
		string text8 = port.ResolveClanDisplay(entry.SettlementOwnerClanId);
		string text9 = port.ResolveKingdomDisplay(entry.SettlementOwnerKingdomId);
		string text10 = port.ResolveClanDisplay(entry.PreviousSettlementOwnerClanId);
		string text11 = port.ResolveKingdomDisplay(entry.PreviousSettlementOwnerKingdomId);
		string text12 = port.ResolveClanDisplay(entry.ActorClanId);
		string text13 = port.ResolveKingdomDisplay(entry.ActorKingdomId);
		list.Add(T("DateSentence", "{DATE}.", ("DATE", text)));
		string text14 = BuildActorNarrativeForDisplay(port, entry);
		if (!string.IsNullOrWhiteSpace(text14))
		{
			list.Add(T("NarrativeSentence", "{NARRATIVE}.", ("NARRATIVE", text14)));
		}
		if (!string.IsNullOrWhiteSpace(text2))
		{
			list.Add(T("KindSentence", "Action type: {KIND}.", ("KIND", text2)));
		}
		if (!string.IsNullOrWhiteSpace(text12) && !string.IsNullOrWhiteSpace(text13))
		{
			list.Add(T("ActorClanKingdom", "{ACTOR} belongs to {CLAN}, which serves {KINGDOM}.", ("ACTOR", text15), ("CLAN", text12), ("KINGDOM", text13)));
		}
		else if (!string.IsNullOrWhiteSpace(text12))
		{
			list.Add(T("ActorClan", "{ACTOR} belongs to {CLAN}.", ("ACTOR", text15), ("CLAN", text12)));
		}
		else if (!string.IsNullOrWhiteSpace(text13))
		{
			list.Add(T("ActorKingdom", "{ACTOR} serves {KINGDOM}.", ("ACTOR", text15), ("KINGDOM", text13)));
		}
		if (!string.IsNullOrWhiteSpace(text4))
		{
			list.Add(T("LocationSentence", "Location: {SETTLEMENT}.", ("SETTLEMENT", text4)));
		}
		if (!string.IsNullOrWhiteSpace(text8) && !string.IsNullOrWhiteSpace(text9))
		{
			list.Add(T("OwnerClanKingdom", "At the time, the settlement was controlled by {CLAN} and belonged to {KINGDOM}.", ("CLAN", text8), ("KINGDOM", text9)));
		}
		else if (!string.IsNullOrWhiteSpace(text8))
		{
			list.Add(T("OwnerClan", "At the time, the settlement was controlled by {CLAN}.", ("CLAN", text8)));
		}
		else if (!string.IsNullOrWhiteSpace(text9))
		{
			list.Add(T("OwnerKingdom", "At the time, the settlement belonged to {KINGDOM}.", ("KINGDOM", text9)));
		}
		if (!string.IsNullOrWhiteSpace(text10) && !string.IsNullOrWhiteSpace(text11))
		{
			list.Add(T("PreviousOwner", "Previously, it was controlled by {CLAN} and belonged to {KINGDOM}.", ("CLAN", text10), ("KINGDOM", text11)));
		}
		if (!string.IsNullOrWhiteSpace(text5))
		{
			list.Add(T("TargetHero", "Hero directly involved: {HERO}.", ("HERO", text5)));
		}
		if (!string.IsNullOrWhiteSpace(text6))
		{
			list.Add(T("TargetClan", "Clan involved: {CLAN}.", ("CLAN", text6)));
		}
		if (!string.IsNullOrWhiteSpace(text7))
		{
			list.Add(T("TargetKingdom", "Kingdom involved: {KINGDOM}.", ("KINGDOM", text7)));
		}
		if (entry.Won.HasValue)
		{
			list.Add(T("ResultSentence", "Result: {RESULT}.", ("RESULT", (entry.Won.Value ? T("Won", "Victory") : T("Lost", "Defeat")))));
		}
		if (entry.IsMajor)
		{
			list.Add(T("MajorSentence", "This is classified as a major action."));
		}
		return string.Join("", list);
	}

	internal static string GetDevNpcActionKindDisplay(NpcActionEditorDisplayPort port, string actionKind)
	{
		string text = (actionKind ?? "").Trim();
		string text2 = GetNpcActionKindForDisplay(text);
		if (string.IsNullOrWhiteSpace(text2))
		{
			return text;
		}
		return string.IsNullOrWhiteSpace(text) ? text2 : T("KindWithId", "{KIND} ({ID})", ("KIND", text2), ("ID", text));
	}

	internal static void AppendDevNpcActionField(StringBuilder stringBuilder, string label, string value)
	{
		if (stringBuilder == null || string.IsNullOrWhiteSpace(label) || string.IsNullOrWhiteSpace(value))
		{
			return;
		}
		stringBuilder.AppendLine(T("FieldValue", "{LABEL}: {VALUE}", ("LABEL", label), ("VALUE", value.Trim())));
	}

	internal static string JoinDevActionIds(List<string> ids)
	{
		if (ids == null || ids.Count == 0)
		{
			return "";
		}
		List<string> list = ids.Where((string x) => !string.IsNullOrWhiteSpace(x)).Select((string x) => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		return (list.Count == 0) ? "" : string.Join(", ", list);
	}

}
