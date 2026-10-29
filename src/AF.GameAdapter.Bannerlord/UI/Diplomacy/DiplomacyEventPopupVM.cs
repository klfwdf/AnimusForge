using System;
using AnimusForge.Refactor.Contracts;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace AnimusForge;

public sealed class DiplomacyEventPopupVM : ViewModel
{
    private readonly Action _close;
    internal static string Text(string key, string fallback) => new TextObject("{=AF_DIP_EVENT_" + key + "}" + fallback).ToString();

    internal DiplomacyEventPopupVM(DiplomacyEventPresentation data, string illustrationSprite, string chromeSprite, Action close)
    {
        _close = close;
        TitleText = string.IsNullOrWhiteSpace(data.Title) ? Text("Title", "Diplomatic event") : data.Title;
        StageText = data.Stage == DiplomacyEventStage.Started ? Text("Started", "Event begins") : Text("Ended", "Event concludes");
        BodyText = data.Body;
        ParticipantsText = string.Join(Text("Separator", ", "), data.Participants);
        NoticeText = data.PublicNotice;
        IllustrationSprite = illustrationSprite;
        ChromeSprite = chromeSprite;
        EffectsHeading = data.Stage == DiplomacyEventStage.Started ? Text("InitialEffects", "Immediate effects") : Text("FinalEffects", "Effects at conclusion");
        foreach (var effect in data.Effects) Effects.Add(new DiplomacyEventEffectVM(effect));
    }
    [DataSourceProperty] public string TitleText { get; }
    [DataSourceProperty] public string StageText { get; }
    [DataSourceProperty] public string BodyText { get; }
    [DataSourceProperty] public string NoticeText { get; }
    [DataSourceProperty] public bool HasNotice => NoticeText.Length != 0;
    [DataSourceProperty] public string ParticipantsText { get; }
    [DataSourceProperty] public bool HasParticipants => ParticipantsText.Length != 0;
    [DataSourceProperty] public string ParticipantsHeading => Text("Participants", "Participating kingdoms");
    [DataSourceProperty] public string EffectsHeading { get; }
    [DataSourceProperty] public string NoEffectsText => Text("NoEffects", "No confirmed changes to display.");
    [DataSourceProperty] public bool HasNoEffects => Effects.Count == 0;
    [DataSourceProperty] public string CloseText => Text("Acknowledge", "Acknowledge");
    [DataSourceProperty] public string IllustrationSprite { get; }
    [DataSourceProperty] public string ChromeSprite { get; }
    [DataSourceProperty] public bool HasIllustration => IllustrationSprite.Length != 0;
    [DataSourceProperty] public bool HasNoIllustration => !HasIllustration;
    [DataSourceProperty] public string IllustrationFallbackText => Text("Chronicle", "Chronicles of diplomacy");
    [DataSourceProperty] public MBBindingList<DiplomacyEventEffectVM> Effects { get; } = new MBBindingList<DiplomacyEventEffectVM>();
    public void ExecuteClose() => _close?.Invoke();
    public override void OnFinalize() { foreach (var row in Effects) row.OnFinalize(); base.OnFinalize(); }
}

public sealed class DiplomacyEventEffectVM : ViewModel
{
    internal DiplomacyEventEffectVM(DiplomacyEventEffect effect)
    {
        Text = effect.Text;
        Marker = effect.Tone == DiplomacyEventEffectTone.Beneficial ? "+" : effect.Tone == DiplomacyEventEffectTone.Harmful ? "−" : "•";
        MarkerColor = effect.Tone == DiplomacyEventEffectTone.Beneficial ? "#45633AFF" : effect.Tone == DiplomacyEventEffectTone.Harmful ? "#903E2FFF" : "#67543FFF";
    }
    [DataSourceProperty] public string Text { get; }
    [DataSourceProperty] public string Marker { get; }
    [DataSourceProperty] public string MarkerColor { get; }
}
