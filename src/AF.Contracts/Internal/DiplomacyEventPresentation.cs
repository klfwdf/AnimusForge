using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge.Refactor.Contracts;

internal enum DiplomacyEventStage { Started, Ended }
internal enum DiplomacyEventScene { Unspecified, Negotiation, Alliance, Betrayal, Succession, Banquet, Muster, Departure, Battle, Siege, Defense, Aftermath, Retreat, Triumph }
internal enum DiplomacyEventEffectTone { Neutral, Beneficial, Harmful }

internal sealed class DiplomacyEventEffect
{
    internal DiplomacyEventEffect(string text, DiplomacyEventEffectTone tone = DiplomacyEventEffectTone.Neutral)
    { Text = text ?? ""; Tone = tone; }
    public string Text { get; }
    public DiplomacyEventEffectTone Tone { get; }
}

// A public-knowledge display snapshot, not an instruction to execute or publish anything.
// All methods consuming this contract run on the game/UI thread.
internal sealed class DiplomacyEventPresentation
{
    internal DiplomacyEventPresentation(string eventId, long generation, DiplomacyEventStage stage, string title, string body,
        IEnumerable<string> participants, IEnumerable<DiplomacyEventEffect> effects,
        DiplomacyEventScene scene = DiplomacyEventScene.Unspecified, string cultureId = "",
        string selectedImageId = "", string publicNotice = "")
    {
        if (string.IsNullOrWhiteSpace(eventId)) throw new ArgumentException("An event ID is required.", nameof(eventId));
        if (generation <= 0) throw new ArgumentOutOfRangeException(nameof(generation));
        if (!Enum.IsDefined(typeof(DiplomacyEventStage), stage)) throw new ArgumentOutOfRangeException(nameof(stage));
        EventId = eventId; Generation = generation; Stage = stage; Title = title ?? ""; Body = body ?? "";
        Participants = Array.AsReadOnly((participants ?? Enumerable.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray());
        Effects = Array.AsReadOnly((effects ?? Enumerable.Empty<DiplomacyEventEffect>()).Where(x => x != null && !string.IsNullOrWhiteSpace(x.Text)).ToArray());
        Scene = scene; CultureId = cultureId ?? ""; SelectedImageId = selectedImageId ?? ""; PublicNotice = publicNotice ?? "";
    }
    public string EventId { get; }
    public long Generation { get; }
    public DiplomacyEventStage Stage { get; }
    public string Title { get; }
    public string Body { get; }
    public IReadOnlyList<string> Participants { get; }
    public IReadOnlyList<DiplomacyEventEffect> Effects { get; }
    public DiplomacyEventScene Scene { get; }
    public string CultureId { get; }
    public string SelectedImageId { get; }
    public string PublicNotice { get; }
}
