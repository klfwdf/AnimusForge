using System;
using System.Collections.Generic;

namespace AnimusForge;

internal static class EconomyItemTypeDisplayTexts
{
    private static readonly Dictionary<string, (string Id, string English)> Labels = new Dictionary<string, (string, string)>(StringComparer.Ordinal)
    {
        [EconomySourceTexts.Get("S285")] = ("AF_ECO_UI_ItemType_S285", "whip"),
        [EconomySourceTexts.Get("S286")] = ("AF_ECO_UI_ItemType_S286", "dagger"),
        [EconomySourceTexts.Get("S287")] = ("AF_ECO_UI_ItemType_S287", "sword"),
        [EconomySourceTexts.Get("S288")] = ("AF_ECO_UI_ItemType_S288", "axe"),
        [EconomySourceTexts.Get("S289")] = ("AF_ECO_UI_ItemType_S289", "mace"),
        [EconomySourceTexts.Get("S290")] = ("AF_ECO_UI_ItemType_S290", "pick"),
        [EconomySourceTexts.Get("S291")] = ("AF_ECO_UI_ItemType_S291", "polearm"),
        [EconomySourceTexts.Get("S292")] = ("AF_ECO_UI_ItemType_S292", "arrows"),
        [EconomySourceTexts.Get("S293")] = ("AF_ECO_UI_ItemType_S293", "bolts"),
        [EconomySourceTexts.Get("S294")] = ("AF_ECO_UI_ItemType_S294", "sling stones"),
        [EconomySourceTexts.Get("S295")] = ("AF_ECO_UI_ItemType_S295", "ammunition"),
        [EconomySourceTexts.Get("S296")] = ("AF_ECO_UI_ItemType_S296", "bow"),
        [EconomySourceTexts.Get("S297")] = ("AF_ECO_UI_ItemType_S297", "crossbow"),
        [EconomySourceTexts.Get("S298")] = ("AF_ECO_UI_ItemType_S298", "sling"),
        [EconomySourceTexts.Get("S299")] = ("AF_ECO_UI_ItemType_S299", "throwing axe"),
        [EconomySourceTexts.Get("S300")] = ("AF_ECO_UI_ItemType_S300", "throwing knives"),
        [EconomySourceTexts.Get("S301")] = ("AF_ECO_UI_ItemType_S301", "javelins"),
        [EconomySourceTexts.Get("S302")] = ("AF_ECO_UI_ItemType_S302", "firearm"),
        [EconomySourceTexts.Get("S303")] = ("AF_ECO_UI_ItemType_S303", "shield"),
        [EconomySourceTexts.Get("S304")] = ("AF_ECO_UI_ItemType_S304", "banner"),
        [EconomySourceTexts.Get("S305")] = ("AF_ECO_UI_ItemType_S305", "alcohol"),
        [EconomySourceTexts.Get("S306")] = ("AF_ECO_UI_ItemType_S306", "food"),
        [EconomySourceTexts.Get("S307")] = ("AF_ECO_UI_ItemType_S307", "pack animal"),
        [EconomySourceTexts.Get("S308")] = ("AF_ECO_UI_ItemType_S308", "livestock"),
        [EconomySourceTexts.Get("S309")] = ("AF_ECO_UI_ItemType_S309", "timber"),
        [EconomySourceTexts.Get("S310")] = ("AF_ECO_UI_ItemType_S310", "iron"),
        [EconomySourceTexts.Get("S311")] = ("AF_ECO_UI_ItemType_S311", "salt"),
        [EconomySourceTexts.Get("S312")] = ("AF_ECO_UI_ItemType_S312", "tools"),
        [EconomySourceTexts.Get("S313")] = ("AF_ECO_UI_ItemType_S313", "pottery"),
        [EconomySourceTexts.Get("S314")] = ("AF_ECO_UI_ItemType_S314", "cloth"),
        [EconomySourceTexts.Get("S315")] = ("AF_ECO_UI_ItemType_S315", "raw materials"),
        [EconomySourceTexts.Get("S316")] = ("AF_ECO_UI_ItemType_S316", "oil"),
        [EconomySourceTexts.Get("S317")] = ("AF_ECO_UI_ItemType_S317", "valuables"),
        [EconomySourceTexts.Get("S318")] = ("AF_ECO_UI_ItemType_S318", "goods"),
        [EconomySourceTexts.Get("S319")] = ("AF_ECO_UI_ItemType_S319", "horse"),
        [EconomySourceTexts.Get("S320")] = ("AF_ECO_UI_ItemType_S320", "one-handed weapon"),
        [EconomySourceTexts.Get("S321")] = ("AF_ECO_UI_ItemType_S321", "two-handed weapon"),
        [EconomySourceTexts.Get("S322")] = ("AF_ECO_UI_ItemType_S322", "polearm"),
        [EconomySourceTexts.Get("S323")] = ("AF_ECO_UI_ItemType_S323", "thrown weapon"),
        [EconomySourceTexts.Get("S324")] = ("AF_ECO_UI_ItemType_S324", "helmet"),
        [EconomySourceTexts.Get("S325")] = ("AF_ECO_UI_ItemType_S325", "body armor"),
        [EconomySourceTexts.Get("S326")] = ("AF_ECO_UI_ItemType_S326", "leg armor"),
        [EconomySourceTexts.Get("S327")] = ("AF_ECO_UI_ItemType_S327", "hand armor"),
        [EconomySourceTexts.Get("S328")] = ("AF_ECO_UI_ItemType_S328", "book"),
        [EconomySourceTexts.Get("S329")] = ("AF_ECO_UI_ItemType_S329", "cape"),
        [EconomySourceTexts.Get("S330")] = ("AF_ECO_UI_ItemType_S330", "harness"),
        [EconomySourceTexts.Get("S028")] = ("AF_ECO_UI_ItemType_S028", "item"),
    };

    internal static string ForDisplay(string raw)
    {
        if (raw == null) return null;
        int suffix = raw.IndexOf(" (", StringComparison.Ordinal);
        if (suffix < 0) suffix = raw.IndexOf("/", StringComparison.Ordinal);
        string prefix = suffix < 0 ? raw : raw.Substring(0, suffix);
        return Labels.TryGetValue(prefix, out var label)
            ? new TaleWorlds.Localization.TextObject("{=" + label.Id + "}" + label.English).ToString() + (suffix < 0 ? "" : raw.Substring(suffix))
            : raw;
    }
}
