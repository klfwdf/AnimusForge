using AnimusForge;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

internal static class PersonaMenuLifecycleReplay
{
    internal static void Run(Action<bool, string> check)
    {
        long generation = 1;
        Hero selected = null;
        List<Hero> editable = new();
        int closesA = 0, closesB = 0, saveCalls = 0;
        Hero a = new() { StringId = "persona-a", Name = "A" };
        Hero b = new() { StringId = "persona-b", Name = "B" };
        var profiles = new PersonaProfileStateOwner();
        profiles.Save(a.StringId, new() { Personality = "A personality", Background = "A background", VoiceId = "voice-a" }, (_, _) => { });
        profiles.Save(b.StringId, new() { Personality = "B personality", Background = "B background", VoiceId = "voice-b" }, (_, _) => { });
        var port = new PersonaEditorPort
        {
            CaptureGeneration = () => generation,
            IsCurrent = g => g == generation,
            GetSelectedHero = () => selected,
            SetSelectedHero = h => selected = h,
            GetEditableHeroes = () => editable,
            SetEditableHeroes = heroes => editable = heroes,
            GetNpcPersonaStrings = (Hero h, out string personality, out string background) => profiles.GetNpcPersonaStrings(h.StringId, out personality, out background),
            GetNpcVoiceId = h => profiles.GetNpcVoiceId(h.StringId),
            ClearPersona = h => { saveCalls++; profiles.Save(h.StringId, new(), (_, _) => { }); },
            SavePersonaText = (h, personality, background) => { saveCalls++; profiles.Save(h.StringId, new() { Personality = personality, Background = background, VoiceId = profiles.GetNpcVoiceId(h.StringId) }, (_, _) => { }); },
            SaveVoice = (h, voice) => { saveCalls++; var value = profiles.Get(h.StringId, true); value.VoiceId = voice; profiles.Save(h.StringId, value, (_, _) => { }); },
            ShowDevEditInquiry = _ => { }
        };
        var editor = new PersonaEditorController(port);
        editor.SynchronizeGeneration(generation);
        editor.OpenDevPersonaMenuFromExternal(a, () => closesA++);
        var old = MBInformationManager.Selection;
        editor.OpenDevPersonaMenuFromExternal(b, () => closesB++);
        old.Confirm(new() { new("clear_persona", "", null) });
        check(saveCalls == 0 && profiles.Get(b.StringId, false)?.Personality == "B personality", "actual old A inquiry cannot clear the same-generation current B profile");
        old.Cancel();
        check(closesA == 0 && closesB == 0 && editor.PersonaReturnAction != null, "actual old A cancel cannot consume B return callback");
        MBInformationManager.Selection.Confirm(new() { new("set_personality", "", null) });
        var currentText = DevTextEditorHelper.Confirm;
        currentText("  edited B  ");
        profiles.GetNpcPersonaStrings(b.StringId, out string personality, out string background);
        check(saveCalls == 1 && personality == "edited B" && background == "B background" && profiles.GetNpcVoiceId(b.StringId) == "voice-b", "actual current B text callback writes the sole profile owner and preserves background/voice");
        editor.OpenDevSetVoiceId(b);
        InformationManager.Text.Confirm("  voice-new  ");
        check(profiles.GetNpcVoiceId(b.StringId) == "voice-new", "actual voice callback trims and reads back current sole owner");
        var retiring = MBInformationManager.Selection;
        generation++;
        retiring.Confirm(new() { new("clear_persona", "", null) });
        retiring.Cancel();
        check(saveCalls == 2 && closesB == 0 && profiles.Get(b.StringId, false) != null, "actual retired-generation menu callbacks preserve profile and return action");
        editor.SynchronizeGeneration(generation);
        check(editor.PersonaReturnAction == null, "editor synchronization clears retired return action");
        editor.OpenDevPersonaMenuFromExternal(b, () => closesB++);
        MBInformationManager.Selection.Confirm(new() { new("clear_persona", "", null) });
        check(profiles.Get(b.StringId, false) == null && profiles.Get(a.StringId, false) != null && saveCalls == 3, "actual current clear removes only selected B from authoritative owner");
        MBInformationManager.Selection.Cancel();
        check(closesB == 1 && editor.PersonaReturnAction == null, "actual current cancel consumes selected B completion once");
    }
}
