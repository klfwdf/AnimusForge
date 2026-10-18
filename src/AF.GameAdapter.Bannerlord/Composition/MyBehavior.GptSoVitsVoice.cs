using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace AnimusForge;

public partial class MyBehavior
{
    // Reuses the existing per-campaign persona JSON/chunked save owner; no global Hero-ID file.
    private string GetOrAssignGptSoVitsVoice(Hero hero)
    {
        var profile = GetNpcPersonaProfile(hero, createIfMissing: true);
        if (profile == null) return "";
        string saved = (profile.GptSoVitsVoiceId ?? "").Trim();
        // Explicit aliases migrate retired voices deterministically; never reroll.
        if (!string.IsNullOrWhiteSpace(saved))
        {
            string resolved = GptSoVitsVoiceRuntime.GetCatalog().ResolveAssignment(saved, hero.IsFemale).Id;
            if (resolved != saved)
            {
                profile.GptSoVitsVoiceId = resolved;
                SaveNpcPersonaProfile(hero, profile);
            }
            return resolved;
        }
        string picked = GptSoVitsVoiceRuntime.GetCatalog().ResolveAssignment(null, hero.IsFemale).Id;
        profile.GptSoVitsVoiceId = picked;
        SaveNpcPersonaProfile(hero, profile);
        return picked;
    }

    public static void OpenHeroGptSoVitsVoiceForExternal(Hero hero)
    {
        var owner = Campaign.Current?.GetCampaignBehavior<MyBehavior>();
        if (owner == null || hero == null) return;
        try
        {
            var catalog = GptSoVitsVoiceRuntime.GetCatalog();
            var profile = owner.GetNpcPersonaProfile(hero, createIfMissing: false);
            string current = profile?.GptSoVitsVoiceId ?? "";
            string currentLabel = GptSoVitsUiText.Text("VoiceUnassigned", "Unassigned; choose now or get a default gender-matched voice on first speech");
            if (!string.IsNullOrWhiteSpace(current))
            {
                try { currentLabel = catalog.Get(current).Label; }
                catch (System.IO.InvalidDataException) { currentLabel = GptSoVitsUiText.Text("VoiceMissing", "Saved voice missing; choose another voice"); }
            }
            var choices = new List<InquiryElement>
            {
                new InquiryElement("", GptSoVitsUiText.Text("RandomVoice", "Pick and save a random voice of the same gender"), null, true, GptSoVitsUiText.Text("CloudVoiceUnchanged", "Existing Volcengine voice is unchanged"))
            };
            foreach (var voice in catalog.ForSelection(hero.IsFemale))
            {
                string group = voice.IsCustom
                    ? GptSoVitsUiText.Text("CustomCategory", "[Custom / manual only] ")
                    : GptSoVitsUiText.Text(hero.IsFemale ? "FemaleCategory" : "MaleCategory", hero.IsFemale ? "[Female / default] " : "[Male / default] ");
                choices.Add(new InquiryElement(voice.Id, group + voice.Label, null, true, GptSoVitsUiText.Text("VoiceOnly", "Changes voice only, not text language or persona")));
            }
            long generation = SaveRuntimeGuard.CaptureGeneration();
            MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
                GptSoVitsUiText.Text("ChooseVoiceTitle", "Choose voice · ") + hero.Name, GptSoVitsUiText.Text("CurrentVoice", "Current: ") + currentLabel + GptSoVitsUiText.Text("VoiceSelectionHint", "\nDefault voices match gender. Custom voices are manual-only and never randomized. Select before any conversation; saved with this campaign."), choices,
                true, 1, 1, GptSoVitsUiText.Text("Save", "Save"), GptSoVitsUiText.Text("Cancel", "Cancel"), selected =>
                {
                    if (SaveRuntimeGuard.IsStale(generation, "gpt_voice_picker") || selected == null || selected.Count != 1) return;
                    try
                    {
                        var activeCatalog = GptSoVitsVoiceRuntime.GetCatalog();
                        string id = selected[0].Identifier as string;
                        string picked = activeCatalog.ResolveAssignment(id, hero.IsFemale).Id;
                        var latest = owner.GetNpcPersonaProfile(hero, createIfMissing: true);
                        if (latest == null) return;
                        latest.GptSoVitsVoiceId = picked;
                        owner.SaveNpcPersonaProfile(hero, latest);
                        EncyclopediaHeroPersonaPatch.QueueRefreshForHero(hero.StringId);
                        InformationManager.DisplayMessage(new InformationMessage(GptSoVitsUiText.Text("VoiceSaved", "Voice saved: ") + activeCatalog.Get(picked).Label));
                    }
                    catch (Exception ex) { InformationManager.DisplayMessage(new InformationMessage(GptSoVitsUiText.Text("VoiceNotSaved", "Voice was not saved: ") + TtsErrorUiText.Describe(ex.Message))); }
                }, null));
        }
        catch (Exception ex) { InformationManager.DisplayMessage(new InformationMessage(GptSoVitsUiText.Text("VoicePickerFailed", "Cannot open voice selection: ") + TtsErrorUiText.Describe(ex.Message))); }
    }
}
