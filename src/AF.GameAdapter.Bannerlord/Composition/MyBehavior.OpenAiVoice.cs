using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace AnimusForge;

public partial class MyBehavior
{
    private const string OpenAiVoiceRefreshChoice = "\u0001refresh";

    // Game thread. The first speech after the server list is available picks a same-gender
    // voice and saves it in the campaign persona profile; until then the MCM voice is used.
    private string GetOrAssignOpenAiVoice(Hero hero, DuelSettings settings)
    {
        var profile = GetNpcPersonaProfile(hero, createIfMissing: false);
        string saved = (profile?.OpenAiVoiceId ?? "").Trim();
        OpenAiTtsVoiceCatalog.EnsureFetched(settings.TtsOpenAiApiUrl, settings.TtsOpenAiApiKey, force: false);
        if (saved.Length != 0) return saved;
        string picked = OpenAiTtsVoiceCatalog.For(settings.TtsOpenAiApiUrl, settings.TtsOpenAiVoice)?.PickRandom(hero.IsFemale);
        if (string.IsNullOrEmpty(picked)) return "";
        profile = GetNpcPersonaProfile(hero, createIfMissing: true);
        if (profile == null) return "";
        profile.OpenAiVoiceId = picked;
        SaveNpcPersonaProfile(hero, profile);
        Logger.Log("TtsVoices", "OpenAI voice assigned on first speech: hero=" + hero.StringId + " voice=" + picked);
        return picked;
    }

    public static void OpenHeroOpenAiVoiceForExternal(Hero hero)
    {
        var owner = Campaign.Current?.GetCampaignBehavior<MyBehavior>();
        var settings = DuelSettings.GetSettings();
        if (owner == null || hero == null || settings == null) return;
        try
        {
            var catalog = OpenAiTtsVoiceCatalog.For(settings.TtsOpenAiApiUrl, settings.TtsOpenAiVoice);
            if (catalog == null)
            {
                bool started = OpenAiTtsVoiceCatalog.EnsureFetched(settings.TtsOpenAiApiUrl, settings.TtsOpenAiApiKey, force: true);
                InformationManager.DisplayMessage(new InformationMessage(started
                    ? GptSoVitsUiText.Text("OpenAiVoiceListLoading", "Loading the voice list from the speech service; try again shortly.")
                    : GptSoVitsUiText.Text("OpenAiVoiceListUnavailable", "Cannot fetch the voice list: enter the speech API key and use a URL ending in /audio/speech.")));
                return;
            }
            // Keep trying the server list in the background; the built-in AF list is usable meanwhile.
            if (catalog.IsBuiltIn) OpenAiTtsVoiceCatalog.EnsureFetched(settings.TtsOpenAiApiUrl, settings.TtsOpenAiApiKey, force: false);
            var profile = owner.GetNpcPersonaProfile(hero, createIfMissing: false);
            string current = (profile?.OpenAiVoiceId ?? "").Trim();
            string currentLabel = current.Length == 0
                ? GptSoVitsUiText.Text("OpenAiVoiceUnassigned", "Unassigned; a same-gender voice from the service list is picked and saved on first speech")
                : catalog.Get(current)?.Label ?? GptSoVitsUiText.Text("OpenAiVoiceNotListed", "Not in the current service list: ") + current;
            var choices = new List<InquiryElement>
            {
                new InquiryElement("", GptSoVitsUiText.Text("RandomVoice", "Pick and save a random voice of the same gender"), null, true, GptSoVitsUiText.Text("VoiceOnly", "Changes voice only, not text language or persona")),
                new InquiryElement(OpenAiVoiceRefreshChoice, GptSoVitsUiText.Text("OpenAiRefreshList", "Refresh the voice list from the service"), null, true, "")
            };
            int own = hero.IsFemale ? 2 : 1;
            var ordered = new List<OpenAiTtsVoice>();
            foreach (var voice in catalog.Voices) if (voice.Gender == own) ordered.Add(voice);
            foreach (var voice in catalog.Voices) if (voice.Gender == 0) ordered.Add(voice);
            foreach (var voice in catalog.Voices) if (voice.Gender != own && voice.Gender != 0) ordered.Add(voice);
            foreach (var voice in ordered)
            {
                string tag = voice.Gender == 2 ? GptSoVitsUiText.Text("OpenAiFemaleTag", "[Female] ")
                    : voice.Gender == 1 ? GptSoVitsUiText.Text("OpenAiMaleTag", "[Male] ")
                    : GptSoVitsUiText.Text("OpenAiUnlabeledTag", "[Unlabeled] ");
                string moods = voice.HasHappy || voice.HasAngry ? GptSoVitsUiText.Text("OpenAiMoodTag", " · tone variants") : "";
                choices.Add(new InquiryElement(voice.Id, tag + voice.Label + moods, null, true, GptSoVitsUiText.Text("VoiceOnly", "Changes voice only, not text language or persona")));
            }
            long generation = SaveRuntimeGuard.CaptureGeneration();
            MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
                GptSoVitsUiText.Text("ChooseVoiceTitle", "Choose voice · ") + hero.Name,
                GptSoVitsUiText.Text("CurrentVoice", "Current: ") + currentLabel + GptSoVitsUiText.Text("OpenAiVoiceSelectionHint", "\nRandom picks use only male-/female- voices of this hero's gender. Voices with tone variants switch to _happy/_angry from the reply mood. Saved with this campaign."),
                choices, true, 1, 1, GptSoVitsUiText.Text("Save", "Save"), GptSoVitsUiText.Text("Cancel", "Cancel"), selected =>
                {
                    if (SaveRuntimeGuard.IsStale(generation, "openai_voice_picker") || selected == null || selected.Count != 1) return;
                    try
                    {
                        var latestSettings = DuelSettings.GetSettings();
                        string id = selected[0].Identifier as string ?? "";
                        if (id == OpenAiVoiceRefreshChoice)
                        {
                            OpenAiTtsVoiceCatalog.EnsureFetched(latestSettings?.TtsOpenAiApiUrl, latestSettings?.TtsOpenAiApiKey, force: true);
                            InformationManager.DisplayMessage(new InformationMessage(GptSoVitsUiText.Text("OpenAiRefreshStarted", "Refreshing the voice list; reopen this selection shortly.")));
                            return;
                        }
                        var activeCatalog = OpenAiTtsVoiceCatalog.For(latestSettings?.TtsOpenAiApiUrl, latestSettings?.TtsOpenAiVoice);
                        string picked = id.Length == 0 ? activeCatalog?.PickRandom(hero.IsFemale) : activeCatalog?.Get(id)?.Id;
                        if (string.IsNullOrEmpty(picked))
                        {
                            InformationManager.DisplayMessage(new InformationMessage(GptSoVitsUiText.Text("VoiceNotSaved", "Voice was not saved: ")
                                + GptSoVitsUiText.Text("OpenAiNoGenderVoice", "the service list has no matching voice; choose one manually.")));
                            return;
                        }
                        var latest = owner.GetNpcPersonaProfile(hero, createIfMissing: true);
                        if (latest == null) return;
                        latest.OpenAiVoiceId = picked;
                        owner.SaveNpcPersonaProfile(hero, latest);
                        EncyclopediaHeroPersonaPatch.QueueRefreshForHero(hero.StringId);
                        InformationManager.DisplayMessage(new InformationMessage(GptSoVitsUiText.Text("VoiceSaved", "Voice saved: ") + activeCatalog.Get(picked).Label));
                    }
                    catch (Exception ex) { InformationManager.DisplayMessage(new InformationMessage(GptSoVitsUiText.Text("VoiceNotSaved", "Voice was not saved: ") + ex.GetType().Name)); }
                }, null));
        }
        catch (Exception ex) { InformationManager.DisplayMessage(new InformationMessage(GptSoVitsUiText.Text("VoicePickerFailed", "Cannot open voice selection: ") + ex.GetType().Name)); }
    }
}
