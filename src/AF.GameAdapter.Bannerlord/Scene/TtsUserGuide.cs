using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace AnimusForge;

// On-click, localized native inquiries only. No config writes, filesystem reads or model loading.
internal static class TtsUserGuide
{
    internal const int ChapterCount = 8;
    internal static string Title(int chapter)
    {
        switch (chapter)
        {
            case 0: return GptSoVitsUiText.Text("GuideInstall", "1. Install and start");
            case 1: return GptSoVitsUiText.Text("GuideServices", "2. Choose a service");
            case 2: return GptSoVitsUiText.Text("GuideVoices", "3. Voice folders and characters");
            case 3: return GptSoVitsUiText.Text("GuideRecordings", "4. Custom recording requirements");
            case 4: return GptSoVitsUiText.Text("GuideManifest", "5. Custom manifest requirements");
            case 5: return GptSoVitsUiText.Text("GuideLanguage", "6. Language and emotional tone");
            case 6: return GptSoVitsUiText.Text("GuideBudget", "7. Playback and VRAM budget");
            case 7: return GptSoVitsUiText.Text("GuideTroubleshoot", "8. Refresh, errors and safe updates");
            default: throw new ArgumentOutOfRangeException(nameof(chapter));
        }
    }
    internal static string Body(int chapter)
    {
        switch (chapter)
        {
            case 0: return GptSoVitsUiText.Text("GuideInstallBody", "Install this optional add-on inside Modules/AnimusForge: AF-GPTSOVITS-L/runtime.json and VoicePacks must be directly below the AF module. Do not create AnimusForge/AnimusForge. It requires an AF build with managed TTS support; copying a folder cannot upgrade an older AF.\nSelect AF-GPTSOVITS and enable speech. AF verifies the package and starts it in the background; a valid L is preferred over U. Wait until ready, then play a test. Cold-start dialogue is not replayed later. Closing speech releases only the service started by AF.");
            case 1: return GptSoVitsUiText.Text("GuideServicesBody", "AF-GPTSOVITS: automatic local package, no URL or separate Python setup.\nGPT-SoVITS (manual API): start your own service, then set its URL and optional manifest path. The reference WAV must be accessible to that server; a remote server cannot read your local drive path.\nVolcengine: configure its own credentials and voice, and enable the dedicated service.\nOpenAI-compatible: configure the speech endpoint, its own key, model and voice. Streaming depends on the server. Cloud usage may be charged. Switching providers preserves settings; failures do not silently switch to paid services.");
            case 2: return GptSoVitsUiText.Text("GuideVoicesBody", "Local voices live in VoicePacks/Male/<pack>/, Female/<pack>/ or Custom/<pack>/. Each pack needs manifest.json. Only one package-directory level is discovered. Male/Female feed the same-gender random defaults; Custom is manual-only, even if a gender is recorded.\nUse the hero encyclopedia voice button before or after talking. Named characters keep their saved voice; ordinary soldiers/civilians are not permanent named bindings. Custom voices can be selected deliberately across genders. Stable IDs survive folder renames; changing/removing an ID can require reselection. Cloud voice mapping is separate.");
            case 3: return GptSoVitsUiText.Text("GuideRecordingsBody", "Use recordings you are allowed to use and redistribute. Recommended: one speaker, clean mono PCM16 WAV, about 3-10 seconds, clear speech, no music or overlapping voices; 16 kHz is a suitable reference input. Each reference must be nonempty and at most 8 MiB.\nThe transcript must match the recording verbatim; prompt_lang describes that recording (zh/en/ja/yue for L), not the desired output. Record each tone with the same speaker. After editing audio, update its SHA-256. A reference-only voice pack needs no model weights. Dropping arbitrary .pth/.ckpt files here does not load a new character model.");
            case 4: return GptSoVitsUiText.Text("GuideManifestBody", "manifest.json contains profiles. Each profile requires: unique id (1-128 characters, no outer spaces), label, speaker_metadata.gender (male/female), category (male/female/custom), reference_file (relative, inside this package), reference_sha256 (64-digit SHA-256), prompt_lang and accurate prompt_text (1-1000 characters). category must match its folder; use custom in Custom.\nOptional emotion_variants keys: neutral/happy/angry/sad; each supplies reference_file, reference_sha256, prompt_lang, prompt_text. Missing tones reuse the base reference. English labels + permanent codes such as AF-C100 are recommended. Avoid duplicate IDs. Per manifest: at most 2 MiB; discovery: 128 manifests / 8 MiB metadata / 512 voices.");
            case 5: return GptSoVitsUiText.Text("GuideLanguageBody", "Speech output language is shared by all four providers: blank/auto follows the reply; Chinese, Cantonese, English or Japanese (including Chinese names) requests that language. Existing postprocessing keeps matching text or privately translates it; subtitles and history stay unchanged. Missing/invalid translation stays text-only. The remote model/voice must support the chosen language.\nLocal tone comes from accepted postprocess MOOD: JOY/DELIGHTED -> happy, ANNOYED -> angry, otherwise neutral. Sad is available for audition. One voice ID contains all tones; missing variants fall back to its base. Tone references improve control but cannot guarantee acting quality. Local tone metadata is not sent to cloud providers.");
            case 6: return GptSoVitsUiText.Text("GuideBudgetBody", "L/U and manual local GPT-SoVITS synthesize only the primary scene interlocutor. Bystanders retain text; private dialogue keeps its target. Peaceful meetings are allowed; real battles block local synthesis. Local speech uses accepted complete replies, then queues sentences; it is not character-by-character live reading of unchecked LLM text.\nL targets roughly 2 GiB VRAM; this is NOT a hard GPU cap or proof that every 8 GiB card can run it beside the game. Game settings, other apps, drivers, first-use peaks and long lines matter. Disk size, system RAM and VRAM are different. If memory is tight, disable local speech or choose a configured remote service yourself.");
            case 7: return GptSoVitsUiText.Text("GuideTroubleshootBody", "After adding a managed voice pack, click Refresh local runtime status; reopening the game also reloads the catalog. For manual API with an explicit manifest, restart AF after editing it. Then select the voice in the encyclopedia and audition it.\nNo sound: check the speech switch, selected service, runtime ready state, voice ID, language/postprocess result and local battle/primary-target restrictions. Missing files, wrong SHA-256, duplicate IDs and unsupported languages fail safely. Inspect Logs/TTS; cache is under Cache/TTS. Do not publish keys/tokens/private dialogue.\nInstall only trusted packages. L add-ons must not replace AF DLLs, SubModule.xml or other gameplay files. Existing character recordings are local evaluation assets, not automatically cleared for public redistribution.");
            default: throw new ArgumentOutOfRangeException(nameof(chapter));
        }
    }
    internal static MultiSelectionInquiryData CreateContents()
    {
        var chapters = new List<InquiryElement>(ChapterCount);
        for (int i = 0; i < ChapterCount; i++) chapters.Add(new InquiryElement(i, Title(i), null));
        return new MultiSelectionInquiryData(GptSoVitsUiText.Text("Guide", "TTS user guide"),
            GptSoVitsUiText.Text("GuideChoose", "Choose a chapter. This guide does not start synthesis or change settings."),
            chapters, true, 1, 1, GptSoVitsUiText.Text("GuideRead", "Read guide"), GptSoVitsUiText.Text("GuideClose", "Close"),
            selected =>
            {
                if (selected == null || selected.Count != 1 || !(selected[0].Identifier is int chapter) || chapter < 0 || chapter >= ChapterCount) return;
                InformationManager.ShowInquiry(CreatePage(chapter), true);
            }, null);
    }
    internal static InquiryData CreatePage(int chapter)
        => new InquiryData(Title(chapter), Body(chapter), true, true,
            GptSoVitsUiText.Text("GuideBack", "Contents"), GptSoVitsUiText.Text("GuideClose", "Close"), Open, null);
    internal static void Open() => MBInformationManager.ShowMultiSelectionInquiry(CreateContents());
}
