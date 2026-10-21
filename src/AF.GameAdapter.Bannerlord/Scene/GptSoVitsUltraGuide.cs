using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace AnimusForge;

// Read-only native help. No discovery, model launch, download or configuration writes.
internal static class GptSoVitsUltraGuide
{
    internal const int ChapterCount = 6;
    internal static string Title(int chapter)
    {
        switch (chapter)
        {
            case 0: return GptSoVitsUiText.Text("UltraGuideVersion", "U guide 1 - Version and quality");
            case 1: return GptSoVitsUiText.Text("UltraGuideInstall", "U guide 2 - Installation and L coexistence");
            case 2: return GptSoVitsUiText.Text("UltraGuideUse", "U guide 3 - Enable and assign voices");
            case 3: return GptSoVitsUiText.Text("UltraGuideHardware", "U guide 4 - Hardware and measured limits");
            case 4: return GptSoVitsUiText.Text("UltraGuideVoices", "U guide 5 - Custom voice-pack requirements");
            case 5: return GptSoVitsUiText.Text("UltraGuideTroubleshoot", "U guide 6 - Language, tone and troubleshooting");
            default: throw new ArgumentOutOfRangeException(nameof(chapter));
        }
    }
    internal static string Body(int chapter)
    {
        switch (chapter)
        {
            case 0: return GptSoVitsUiText.Text("UltraGuideVersionBody", "AF-GPTSOVITS-U 0.1.1 is a local evaluation add-on, not a publicly released download. It uses official GPT-SoVITS V5 Dev, native 48kHz output and a fixed 32-step quality profile. It is not a renamed L package. More steps do not guarantee perfect acting or the best result for every voice.\nThe portable runtime needs an AF build that explicitly supports engine gpt-sovits-v5 for U. Older managed-TTS builds reject this engine even if their version number looks sufficient; never edit runtime.json to bypass that check.\nThe extracted payload is about 7.86GiB; reserve at least 20GB for download, extraction and cache. This is separate from RAM/VRAM. No U download link has been published. Existing recordings remain local evaluation assets pending redistribution review.");
            case 1: return GptSoVitsUiText.Text("UltraGuideInstallBody", "Close the game and launcher, then extract into a temporary folder. Copy AF-GPTSOVITS-U and the needed VoicePacks contents into the existing Modules/AnimusForge folder. runtime.json must be at AnimusForge/AF-GPTSOVITS-U/runtime.json; avoid a second AnimusForge wrapper.\nThis add-on contains no AF DLLs or SubModule.xml and cannot update an old AF. Preserve game files, settings, GCCZ and custom voice edits. Keep server.py, ultra_engine.py, app and runtime intact. No separate Python installation or port-9880 service is needed.\nAutomatic detection still prefers a valid L when both L and U are installed. To test U, close the game and move L outside this module first, keeping it as a backup. Do not rename L into U or run both workers. Reading this tutorial never starts or downloads a model.");
            case 2: return GptSoVitsUiText.Text("UltraGuideUseBody", "Select AF-GPTSOVITS-L / U (auto) in the TTS settings, enable speech and wait for Ready. Confirm the detected package is AF-GPTSOVITS-U, then use the local voice audition. Cold loading can take about a minute; avoid repeated refreshes.\nChoose a character voice in the encyclopedia before or after the first conversation. Five male and five female defaults keep the existing IDs; named characters retain their selection. Sparkle is one Custom character with emotional variants, not four separate characters. Custom is manual-only and never enters the default random pool.\nOnly the primary interlocutor speaks; bystanders retain text. Peaceful meetings are allowed, actual combat is blocked. Accepted complete replies are queued by sentence, not read as unchecked live tokens. Disabling speech or exiting AF releases only its owned worker. Refresh after installing new voices.");
            case 3: return GptSoVitsUiText.Text("UltraGuideHardwareBody", "U prioritizes quality, not the L 2GiB target. This Windows/CUDA build requires a compatible NVIDIA GPU; CPU-only, AMD and Intel operation is not validated. It refuses startup if less than 6GiB VRAM is currently free. That is a conservative startup guard, not a claim that inference consumes 6GiB.\nFirst smoke test: RTX 5070 Ti 16GB, no game running, PID-attributed WDDM sampling every 100ms. Dedicated VRAM peaked near 2292MiB; RAM near 4730MiB. A 14.4s Chinese clip took 21.33s to synthesize; cold readiness took 63.34s. Short spikes may be missed, and warm performance varies with text, reference and other load.\nFor concurrent gaming, 12GB/16GB GPUs and 16GB+ system RAM are sensible test configurations, not guarantees or reasons to buy hardware. 8GB gaming coexistence is NOT validated. This quality profile can be slower than real time. See the U document for multi-case measurements.");
            case 4: return GptSoVitsUiText.Text("UltraGuideVoicesBody", "Use VoicePacks/Male/<pack>, Female/<pack> or Custom/<pack>, with manifest.json directly inside that one package level. Keep stable unique IDs, English labels + permanent codes, gender/category, relative reference_file, real SHA-256, prompt_lang and exact prompt_text. Use the published manifest.example.json structure, replacing placeholder hashes.\nU enforces clean mono WAV references of 3–10 seconds, nonempty and at most 8MiB. PCM16 is recommended. Keep real source sample quality; artificial upsampling adds no detail. No music, other speakers, clipping or silence-only recordings. prompt_text must match the exact clip, at most 1000 characters; prompt_lang is zh/en/ja/yue for the recorded speech.\nEach character can have neutral/happy/angry/sad in emotion_variants, with its own reference, hash and transcript. Missing variants use the base. Arbitrary .pth/.ckpt weights are not loadable character packs. Use authorized recordings; code licenses do not grant recording/character redistribution rights.");
            case 5: return GptSoVitsUiText.Text("UltraGuideTroubleshootBody", "Speech output language is shared by the four providers. Blank/auto follows the reply; Chinese, Cantonese, English or Japanese names select the target. Matching text is kept; otherwise postprocessing translates only the spoken copy. Subtitles/history stay unchanged; failed translation stays text-only.\nExisting accepted MOOD selects local references: JOY/DELIGHTED -> happy, ANNOYED -> angry, otherwise neutral; sad is available in audition. No new action tags are introduced. V5 cannot guarantee anger/cuteness without suitable references or create every performance from one flat recording.\nNo sound: check provider, U compatibility, readiness, primary target, combat, voice hash/path and translation. Check Logs/TTS and Cache/TTS/U. Low free VRAM or OOM does not trigger a paid API or silently select L. Close other GPU apps or deliberately choose L/remote TTS. Never disable hash checks; do not publish tokens, keys or private dialogue. Update only the intended package with the game closed.");
            default: throw new ArgumentOutOfRangeException(nameof(chapter));
        }
    }
    internal static MultiSelectionInquiryData CreateContents()
    {
        var chapters = new List<InquiryElement>(ChapterCount);
        for (int i = 0; i < ChapterCount; i++) chapters.Add(new InquiryElement(i, Title(i), null));
        return new MultiSelectionInquiryData(GptSoVitsUiText.Text("UltraGuide", "U edition - quality, installation and voices"),
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
