using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using TaleWorlds.Localization;

namespace AnimusForge;

// Only called for classified execution facts. Save/receipt values and dynamic names stay intact.
internal static class EconomyActionFactProjection
{
    private static readonly Regex Slots = new Regex(@"\{(\d+)\}", RegexOptions.Compiled);
    private static readonly Regex UiSlots = new Regex(@"AFECO_SLOT(\d+)_END", RegexOptions.Compiled);
    private static readonly string[] Keys = { "S442", "S443", "S444", "S445", "S446", "S447", "S448", "S449", "S450", "S516", "S517", "S518", "S519", "S528", "S529", "S530", "S531", "S724", "S725", "S726", "S727", "GoldBatch" };
    private static readonly Lazy<Regex> Pattern = new Lazy<Regex>(() => new Regex(string.Join("|", Keys.Select(BuildPattern)), RegexOptions.Compiled | RegexOptions.CultureInvariant));

    private static string Source(string key)
        => key == "GoldBatch"
            ? EconomySourceTexts.Get("S451") + "{0}" + EconomySourceTexts.Get("S043") + "{1}" + EconomySourceTexts.Get("S044") + "{2}" + EconomySourceTexts.Get("S045") + "{3}" + EconomySourceTexts.Get("S452") + "{4}" + EconomySourceTexts.Get("S047")
            : EconomySourceTexts.Get(key);

    private static string BuildPattern(string key)
    {
        string source = Source(key);
        int offset = 0;
        var parts = new List<string> { "(?<" + key + ">" };
        foreach (Match slot in Slots.Matches(source))
        {
            parts.Add(Regex.Escape(source.Substring(offset, slot.Index - offset)));
            int index = int.Parse(slot.Groups[1].Value, CultureInfo.InvariantCulture);
            // The transfer owner repeats the exact receiver in its inventory clause.
            if (key == "S443" && index == 2) parts.Add(@"\k<S443v1>");
            else if (key == "S442" && index == 3) parts.Add(@"\k<S442v2>");
            else
            {
                bool number = key == "GoldBatch" || key == "S724" && index == 0 || key == "S726" && index == 0
                    || key == "S725" && index == 1 || key == "S727" && index == 2;
                parts.Add("(?<" + key + "v" + index + ">" + (number ? @"[+-]?\d+(?:[.,]\d+)?" : ".+?") + ")");
                // Public-trust suffix may legitimately be empty.
                if (key == "S724" && index == 1 || key == "S726" && index == 1 || key == "S725" && index == 2 || key == "S727" && index == 3)
                    parts[parts.Count - 1] = "(?<" + key + "v" + index + ">.*?)";
            }
            offset = slot.Index + slot.Length;
        }
        parts.Add(Regex.Escape(source.Substring(offset)) + ")");
        return string.Concat(parts);
    }

    internal static string ProjectForPrompt(string body) => Project(body, false);
    internal static string ProjectForUi(string body) => Project(body, true);

    private static string Project(string body, bool ui)
    {
        if (string.IsNullOrEmpty(body)) return body;
        return Pattern.Value.Replace(body, match =>
        {
            string key = Keys.First(k => match.Groups[k].Success);
            Func<int, string> value = index =>
            {
                if (key == "S443" && index == 2) index = 1;
                if (key == "S442" && index == 3) index = 2;
                return match.Groups[key + "v" + index].Value;
            };
            if (!ui)
                return Slots.Replace(EconomyPromptTexts.Get("Fact_" + key), slot => value(int.Parse(slot.Groups[1].Value, CultureInfo.InvariantCulture)));
            string fallback = Slots.Replace(English(key), slot => "{VALUE" + slot.Groups[1].Value + "}");
            var localized = new TextObject("{=AF_ECO_FACT_" + key + "}" + fallback);
            for (int index = 0; index < 5; index++) localized.SetTextVariable("VALUE" + index, "AFECO_SLOT" + index + "_END");
            string template = localized.ToString();
            return UiSlots.Replace(template, slot => value(int.Parse(slot.Groups[1].Value, CultureInfo.InvariantCulture)));
        });
    }

    internal static string English(string key) => key switch
    {
        "S442" => "You handed {1} denars to {2} on behalf of {0}; the money entered {3}'s inventory.",
        "S443" => "You handed {0} denars to {1}; the money entered {2}'s inventory.",
        "S444" => "You received {2} denars from {0}, representing {1}.",
        "S445" => "You received {1} denars from {0}.",
        "S446" => "You planned to hand over {0} denars, but only {1} were paid after your balance changed.",
        "S447" => "{0} planned to hand over {1} denars, but only {2} were paid.",
        "S448" => "You attempted to pay {1} denars on behalf of {0}, but {2} lacked cash; no payment occurred this round.",
        "S449" => "You attempted to pay {0} denars, but your balance was insufficient; no payment occurred this round.",
        "S450" => "{0} attempted to pay {1} denars, but the balance was insufficient; no payment occurred this round.",
        "S516" => "You handed {0} denars to the player; the money was deducted from your party's funds.",
        "S517" => "You planned to pay {0} denars, but only {1} were paid after your party's funds changed.",
        "S518" => "You attempted to pay {0} denars, but your party lacked funds; no payment occurred this round.",
        "S519" => "{0} attempted to pay {1} denars, but the party lacked funds; no payment occurred this round.",
        "S528" => "You handed {0} denars to the player; the money entered the player's inventory.",
        "S529" => "You planned to pay {0} denars, but only {1} were paid after the shop's funds changed.",
        "S530" => "You attempted to pay {0} denars, but the shop lacked cash; no payment occurred this round.",
        "S531" => "{0} attempted to pay {1} denars, but the shop lacked cash; no payment occurred this round.",
        "S724" => "Your personal trust in the player increased by {0}{1} because the value you actually delivered reached the cumulative threshold.",
        "S725" => "{0}'s personal trust in you increased by {1}{2} because the value actually delivered to you reached the cumulative threshold.",
        "S726" => "Your market trust in the player increased by {0}{1} because the value you actually delivered reached the cumulative threshold.",
        "S727" => "Market trust in you from {1}, represented by {0}, increased by {2}{3} because the value actually delivered to you reached the cumulative threshold.",
        "GoldBatch" => "Gold transfer summary: {0} attempted, {1} succeeded, {2} failed or insufficient; {3} denars actually transferred, worth {4} denars.",
        _ => throw new ArgumentOutOfRangeException(nameof(key))
    };
}
