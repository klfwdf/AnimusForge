using System;
using System.Collections.Generic;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge;

// Fixed, bounded catalog. No prose keyword inference, directory scan, game object or IO dependency.
internal static class DiplomacyEventIllustrations
{
    internal sealed class Entry
    {
        internal Entry(string id, DiplomacyEventScene scene, string culture = "") { Id = id; Scene = scene; Culture = culture; }
        internal string Id { get; }
        internal DiplomacyEventScene Scene { get; }
        internal string Culture { get; }
    }
    internal static readonly IReadOnlyList<Entry> Entries = Array.AsReadOnly(new[]
    {
        new Entry("00-approved-armistice", DiplomacyEventScene.Negotiation),
        new Entry("01-alliance", DiplomacyEventScene.Alliance),
        new Entry("03-broken-alliance", DiplomacyEventScene.Betrayal),
        new Entry("05-contested-crown", DiplomacyEventScene.Succession),
        new Entry("06-diplomatic-banquet", DiplomacyEventScene.Banquet),
        new Entry("07-aserai-grand-muster", DiplomacyEventScene.Muster, "aserai"),
        new Entry("08-sturgia-snow-muster", DiplomacyEventScene.Muster, "sturgia"),
        new Entry("09-vlandia-castle-review", DiplomacyEventScene.Muster, "vlandia"),
        new Entry("10-khuzait-steppe-muster", DiplomacyEventScene.Muster, "khuzait"),
        new Entry("11-battania-forest-muster", DiplomacyEventScene.Muster, "battania"),
        new Entry("12-imperial-plaza-review", DiplomacyEventScene.Muster, "empire"),
        new Entry("13-imperial-city-procession", DiplomacyEventScene.Triumph, "empire"),
        new Entry("14-army-departure", DiplomacyEventScene.Departure),
        new Entry("15-armies-clash", DiplomacyEventScene.Battle),
        new Entry("16-siege-encampment", DiplomacyEventScene.Siege),
        new Entry("17-holding-the-walls", DiplomacyEventScene.Defense),
        new Entry("18-war-aftermath", DiplomacyEventScene.Aftermath),
        new Entry("19-army-retreat", DiplomacyEventScene.Retreat)
    });

    internal static bool IsKnown(string id)
    {
        foreach (Entry entry in Entries) if (entry.Id == id) return true;
        return false;
    }

    private static readonly Random Random = new Random();
    internal static string Select(DiplomacyEventPresentation data, IReadOnlyList<string> recent = null)
    {
        if (data == null) return "";
        string previous = recent != null && recent.Count > 0 ? recent[recent.Count - 1] : "";
        var candidates = new List<Entry>(4);
        foreach (Entry entry in Entries)
        {
            if (entry.Id == previous) continue;
            bool exact = entry.Scene == data.Scene;
            bool generalMuster = data.Scene == DiplomacyEventScene.Muster && entry.Scene == DiplomacyEventScene.Departure;
            if ((!exact && !generalMuster) || (entry.Culture.Length != 0 && !string.Equals(entry.Culture, data.CultureId, StringComparison.OrdinalIgnoreCase))) continue;
            candidates.Add(entry);
        }
        // The approved fallback is decorative and culture-neutral, even when
        // there is no second illustration for the exact scene.
        if (candidates.Count == 0)
            foreach (Entry entry in Entries)
                if (entry.Culture.Length == 0 && entry.Id != previous) candidates.Add(entry);
        foreach (Entry entry in candidates)
            if (entry.Id == data.SelectedImageId) return entry.Id;
        return candidates.Count == 0 ? "" : candidates[Random.Next(candidates.Count)].Id;
    }
}
