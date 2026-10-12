using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AnimusForge;

// Consumption-boundary text projection only. Canonical save/history values are never rewritten.
internal static class NobleGatheringTextProjection
{
    private static readonly Regex Slots = new Regex(@"\{([A-Z_]+)\}", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private sealed class Template
    {
        internal readonly string Key, English;
        internal readonly string[] Names;
        internal readonly Regex Pattern;
        internal Template(string key, string english = null)
        {
            Key = key;
            English = english;
            string source = DiplomacyCompatibilityTexts.Get(key);
            var names = new List<string>();
            var pattern = new StringBuilder(@"\A");
            int position = 0;
            foreach (Match slot in Slots.Matches(source))
            {
                pattern.Append(Regex.Escape(source.Substring(position, slot.Index - position)));
                string name = slot.Groups[1].Value;
                names.Add(name);
                pattern.Append("(?<").Append(name).Append(name == "REASON" ? ">.*?)" : ">.+?)");
                position = slot.Index + slot.Length;
            }
            pattern.Append(Regex.Escape(source.Substring(position))).Append(@"\z");
            Names = names.ToArray();
            Pattern = new Regex(pattern.ToString(), RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);
        }

        internal bool TryMatch(string source, out Dictionary<string, string> values)
        {
            values = null;
            Match match = Pattern.Match(source);
            if (!match.Success) return false;
            var captured = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string name in Names)
            {
                string value = match.Groups[name].Value;
                if ((name == "DAYS" || name == "COST" || name == "COUNT")
                    && !double.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out _)) return false;
                captured[name] = value;
            }
            values = captured;
            return true;
        }
    }

    // These small grammars are compiled once on first display/request, never by Tick capture.
    private static readonly Lazy<Template[]> Display = new Lazy<Template[]>(() => new[]
    {
        new Template("Gathering_CancelPlayerReason", "The player ordered {HERO} to end the gathering immediately."),
        new Template("Gathering_CancelPlayerStatus", "{HERO} followed your order and cancelled the gathering at {SETTLEMENT}."),
        new Template("Gathering_CancelNpcReason", "{HERO} accepted the player’s persuasion and decided to end the gathering."),
        new Template("Gathering_CancelNpcStatus", "{HERO} accepted your persuasion and cancelled the gathering at {SETTLEMENT}."),
        new Template("Gathering_NeedGold", "Cannot host the gathering: you need {COST} denars."),
        new Template("Gathering_Cooldown", "Gathering preparation is still on cooldown for about {DAYS} days."),
        new Template("Gathering_TravellingTo", "Travelling to {SETTLEMENT}"),
        new Template("Gathering_NativeTravelFailed", "Native travel request failed: {ERROR}"),
        new Template("Gathering_MovementCheckFailed", "Movement check failed: {ERROR}"),
        new Template("Gathering_WeeklyStartTitle", "Noble gathering hosted - {HERO}"),
        new Template("Gathering_WeeklyAttendanceTitle", "Notable noble attends a gathering - {HERO}"),
        new Template("Gathering_WeeklyCancelTitle", "Noble gathering cancelled - {HERO}"),
        new Template("Gathering_ReasonSuffix", " Reason: {REASON}"),
        new Template("Gathering_CrisisWar", "The kingdom has entered a war; the host decided to cancel the gathering."),
        new Template("Gathering_CrisisSiege", "{SETTLEMENT} in the kingdom is under siege; the host decided to cancel the gathering."),
        new Template("Gathering_EndElapsed", "The gathering has ended."),
        new Template("Gathering_EndInvalid", "The gathering ended because its host or venue became invalid."),
        new Template("Gathering_DisabledReason", "Gatherings are disabled in MCM."),
        new Template("Gathering_WorldCommandsUnavailable", "The world-map command system is not initialized."),
        new Template("Gathering_HostOriginMissing", "The host’s original settlement could not be found."),
        new Template("Gathering_GuestOriginMissing", "The guest’s original settlement could not be found."),
        new Template("Gathering_Travelling", "Travelling"),
        new Template("Gathering_HasParty", "Already has a party"),
        new Template("Gathering_CannotMove", "Cannot currently move to a settlement"),
        new Template("Gathering_Unavailable", "The gathering system is not initialized."),
        new Template("Gathering_InvalidTag", "Gathering instruction was not executed: tag parameters could not be parsed."),
        new Template("Gathering_CancelMissing", "Cannot cancel the gathering: it does not exist or has already ended."),
        new Template("Gathering_CancelUnauthorizedDelegate", "Cannot cancel the gathering: the current speaker is neither your governor nor your companion and cannot carry out your order to end it."),
        new Template("Gathering_CancelUnauthorizedHost", "Cannot cancel the gathering: only its host can accept persuasion and cancel it."),
        new Template("Gathering_NoResolvedGuests", "Gathering not started: no eligible guests could be resolved."),
        new Template("Gathering_InvalidHero", "Invalid hero"),
        new Template("Gathering_NotSpawned", "Has not appeared in the world"),
        new Template("Gathering_UnderEighteen", "Under eighteen"),
        new Template("Gathering_ChildOrUnborn", "Underage or not yet born"),
        new Template("Gathering_CannotHostDisabled", "Cannot host the gathering: gatherings are disabled in MCM."),
        new Template("Gathering_CannotHostVenue", "Cannot host the gathering: select a valid town or castle."),
        new Template("Gathering_CannotHostLeader", "Cannot host the gathering: only a clan leader may host it."),
        new Template("Gathering_CannotHostOwnership", "Cannot host the gathering: the venue must belong to your clan."),
        new Template("Gathering_CannotHostSiege", "Cannot host the gathering: the settlement is under siege."),
        new Template("Gathering_CannotHostActive", "Cannot host the gathering: you already have an active gathering."),
        new Template("Gathering_CannotHostOccupied", "Cannot host the gathering: the settlement already has an active gathering."),
        new Template("Gathering_Prisoner", "Captured"),
        new Template("Gathering_Wounded", "Seriously wounded"),
        new Template("Gathering_NoIndependentParty", "No independent party"),
        new Template("Gathering_InArmy", "In an army"),
        new Template("Gathering_InBattleOrSiege", "In a battle or siege"),
        new Template("Gathering_HasNonIndependentParty", "Already has a non-independent party or is in a party"),
        new Template("Gathering_OriginMissing", "Original settlement could not be found"),
        new Template("Gathering_CannotHostInvalidGovernor", "Cannot host the gathering: the current speaker is not a valid governor."),
        new Template("Gathering_CannotHostNotGovernor", "Cannot host the gathering: the current speaker is not a governor."),
        new Template("Gathering_CannotHostGovernorOwnership", "Cannot host the gathering: the governor’s settlement does not belong to your clan."),
        new Template("Gathering_RoleKing", "king"),
        new Template("Gathering_RoleClanLeader", "clan leader"),
        new Template("Gathering_RoleNoble", "noble"),
        new Template("Gathering_UnknownHero", "Unknown noble"),
        new Template("Gathering_WeeklyStartWithGuests", "{HOST} ({ROLE}) hosted a noble gathering at {SETTLEMENT}, planned to last {DAYS} days. Notable guests who accepted the invitation include: {GUESTS}. This gathering has actually issued invitations and begun; it is not an unverified plan. Guests who have not arrived may only be described as having accepted an invitation or preparing to attend."),
        new Template("Gathering_WeeklyStartWithoutGuests", "{HOST} ({ROLE}) hosted a noble gathering at {SETTLEMENT}, planned to last {DAYS} days. This gathering has actually issued invitations and begun; it is not an unverified plan. Guests who have not arrived may only be described as having accepted an invitation or preparing to attend."),
        new Template("Gathering_WeeklyAttendance", "{HERO} ({ROLE}) has actually arrived at {SETTLEMENT} and attended the noble gathering hosted by {HOST}. This attendance has occurred and may be included in this week’s kingdom chronicle."),
        new Template("Gathering_WeeklyCancel", "{HOST} cancelled their noble gathering at {SETTLEMENT}; the guests have dispersed.{REASON}"),
        new Template("Gathering_WeeklyNameRole", "{HERO} ({ROLE})"),
    });
    private static readonly Lazy<Template[]> Facts = new Lazy<Template[]>(() => new[]
    {
        new Template("Gathering_FactPlayerCancel"), new Template("Gathering_FactNpcCancel"),
        new Template("Gathering_FactPlayerStartMore"), new Template("Gathering_FactPlayerStartExact")
    });
    private static readonly Lazy<Template[]> Weekly = new Lazy<Template[]>(() => new[]
    {
        new Template("Gathering_WeeklyStartWithGuests"), new Template("Gathering_WeeklyStartWithoutGuests"),
        new Template("Gathering_WeeklyAttendance"), new Template("Gathering_WeeklyCancel"),
        new Template("Gathering_WeeklyStartTitle"), new Template("Gathering_WeeklyAttendanceTitle"),
        new Template("Gathering_WeeklyCancelTitle")
    });
    private static readonly Lazy<Dictionary<string, string>> SelectorAliases = new Lazy<Dictionary<string, string>>(() =>
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var alias in new[] { ("Place", "settlement"), ("Hero", "heroes"), ("Clan", "clans"),
            ("Kingdom", "kingdoms"), ("Faction", "kingdoms"), ("Culture", "cultures"), ("Age", "age"), ("Gender", "gender") })
            values[DiplomacyCompatibilityTexts.Get("Gathering_Selector" + alias.Item1)] = alias.Item2;
        return values;
    });

    internal static string NormalizeSelectorKey(string key)
        => key != null && SelectorAliases.Value.TryGetValue(key, out string normalized) ? normalized : key;

    internal static string RawFormat(string key, params (string Name, object Value)[] values)
        => Render(DiplomacyCompatibilityTexts.Get(key), values);

    private static string Render(string source, params (string Name, object Value)[] values)
        => Slots.Replace(source, match =>
        {
            foreach (var value in values)
                if (value.Name == match.Groups[1].Value)
                    return Convert.ToString(value.Value, CultureInfo.CurrentCulture) ?? "";
            return match.Value;
        });

    private static (string Name, object Value)[] Variables(Dictionary<string, string> values)
        => values.Select(value => (value.Key, (object)value.Value)).ToArray();

    internal static string ProjectForUi(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return raw;
        foreach (Template template in Display.Value)
        {
            if (!template.TryMatch(raw, out var values)) continue;
            if (values.TryGetValue("REASON", out string reason)) values["REASON"] = ProjectForUi(reason);
            if (values.TryGetValue("ROLE", out string role)) values["ROLE"] = ProjectForUi(role);
            if (template.Key == "Gathering_WeeklyStartWithGuests")
                values["GUESTS"] = ProjectNamedGuestList(values["GUESTS"], true);
            return NobleGatheringUiText.Render("AF_DIP_UI_" + template.Key, template.English, Variables(values));
        }
        return raw;
    }

    internal static string ProjectForDiagnostic(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return raw;
        foreach (Template template in Display.Value)
        {
            if (!template.TryMatch(raw, out var values)) continue;
            if (values.TryGetValue("REASON", out string reason)) values["REASON"] = ProjectForDiagnostic(reason);
            return Render(template.English, Variables(values));
        }
        return raw;
    }

    // Call only when a confirmed system fact is inserted into model context.
    // AFEF envelopes are owned by the history parser; this method projects its body.
    internal static string ProjectFactBodyForPrompt(string raw)
    {
        if (string.IsNullOrEmpty(raw) || UseChinesePrompt) return raw;
        return ProjectPrompt(Facts.Value, raw);
    }

    internal static string ProjectWeeklyMaterialForPrompt(string type, string key, string raw)
    {
        if (string.IsNullOrEmpty(raw) || UseChinesePrompt
            || (type != "noble_gathering" && !(type == "raw_text" && (key ?? "").StartsWith("noble_gathering:", StringComparison.Ordinal)))) return raw;
        return ProjectPrompt(Weekly.Value, raw);
    }

    private static bool UseChinesePrompt => DiplomacyMiscPromptTexts.GetRequired("Gathering_ContentLanguage") == "zh-CN";

    private static string ProjectPrompt(Template[] catalog, string raw)
    {
        foreach (Template template in catalog)
        {
            if (!template.TryMatch(raw, out var values)) continue;
            if (values.TryGetValue("ROLE", out string role)) values["ROLE"] = ProjectRole(role);
            if (values.TryGetValue("REASON", out string reason)) values["REASON"] = ProjectReasonForPrompt(reason);
            if (template.Key == "Gathering_WeeklyStartWithGuests")
                values["GUESTS"] = ProjectNamedGuestList(values["GUESTS"]);
            return Render(DiplomacyMiscPromptTexts.GetRequired(template.Key), Variables(values));
        }
        return raw;
    }

    private static string ProjectRole(string role)
    {
        foreach (string key in new[] { "Gathering_RoleKing", "Gathering_RoleClanLeader", "Gathering_RoleNoble" })
            if (role == DiplomacyCompatibilityTexts.Get(key)) return DiplomacyMiscPromptTexts.GetRequired(key);
        return role;
    }

    private static readonly Lazy<Regex> NamedGuest = new Lazy<Regex>(() => new Regex(
        @"\G(?<HERO>.+?)" + Regex.Escape("（") + "(?<ROLE>"
        + string.Join("|", new[] { "Gathering_RoleKing", "Gathering_RoleClanLeader", "Gathering_RoleNoble" }
            .Select(key => Regex.Escape(DiplomacyCompatibilityTexts.Get(key)))) + ")"
        + Regex.Escape("）") + "(?:" + Regex.Escape(DiplomacyCompatibilityTexts.Get("Gathering_ListSeparator")) + @"|\z)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline));

    private static string ProjectNamedGuestList(string raw, bool forUi = false)
    {
        var result = new List<string>();
        int position = 0;
        foreach (Match guest in NamedGuest.Value.Matches(raw))
        {
            if (guest.Index != position) return raw;
            position += guest.Length;
            result.Add(forUi
                ? NobleGatheringUiText.Render("AF_DIP_UI_Gathering_WeeklyNameRole", "{HERO} ({ROLE})",
                    ("HERO", guest.Groups["HERO"].Value), ("ROLE", ProjectForUi(guest.Groups["ROLE"].Value)))
                : Render(DiplomacyMiscPromptTexts.GetRequired("Gathering_WeeklyNameRole"),
                    ("HERO", guest.Groups["HERO"].Value), ("ROLE", ProjectRole(guest.Groups["ROLE"].Value))));
        }
        return position == raw.Length && result.Count > 0
            ? string.Join(forUi ? NobleGatheringUiText.ListSeparator : DiplomacyMiscPromptTexts.GetRequired("Gathering_ListSeparator"), result) : raw;
    }

    private static string ProjectReasonForPrompt(string raw)
    {
        foreach (Template template in Display.Value)
        {
            if (!template.TryMatch(raw, out var values)) continue;
            if (values.TryGetValue("REASON", out string reason)) values["REASON"] = ProjectReasonForPrompt(reason);
            return Render(DiplomacyMiscPromptTexts.GetRequired(template.Key), Variables(values));
        }
        return raw;
    }
}
