# AF-GPTSOVITS-L 0.1.0 installation and voice-pack guide

For AnimusForge in Mount & Blade II: Bannerlord on Windows, using the managed lightweight L speech add-on. This is an inference package; players do not need to train a model. Updated: 2026-10-20.

[简体中文](AF-GPTSOVITS-L.zh-CN.md)

## 1. Download and version

Download: AF-GPTSOVITS-L_0.1.0.zip

Baidu Netdisk: [AF-GPTSOVITS-L_0.1.0.zip](https://pan.baidu.com/s/1Wsl1hXm5Mz2_7DRetX94FA?pwd=5qkj)

Extraction code: 5qkj

This is an optional lightweight voice add-on for AF, not the main AF mod or the upstream training distribution. An AF build with AF-GPTSOVITS support is required.

The local same-version test archive is about 5GB compressed / 7.3GiB extracted. Allow about 15GB for download, extraction and cache; check the actual shared file size. A future U edition needs its own download and guide; L memory figures do not apply to U.

## 2. Installation

1. Close the game and launcher; extract to a temporary folder first.

2. Put AF-GPTSOVITS-L and VoicePacks inside the existing Modules/AnimusForge folder. If the archive has an AnimusForge wrapper, merge its contents rather than nesting AnimusForge twice.

3. Check Modules/AnimusForge/AF-GPTSOVITS-L/runtime.json and Modules/AnimusForge/VoicePacks. Keep server.py, lite_engine.py, app and runtime intact.

4. The add-on must not replace AF bin, SubModule.xml, ModuleData or gameplay files. Preserve custom voices and existing manifests; never delete the whole AF module.

5. No separate Python installation or old port-9880 server is needed; AF owns its managed process.

```text
Mount & Blade II Bannerlord/
└─ Modules/
   └─ AnimusForge/
      ├─ SubModule.xml             # existing AF / 保留原有文件
      ├─ bin/                      # existing AF / 保留原有文件
      ├─ AF-GPTSOVITS-L/
      │  ├─ runtime.json
      │  ├─ server.py
      │  ├─ lite_engine.py
      │  ├─ app/
      │  └─ runtime/
      └─ VoicePacks/
         ├─ Male/
         ├─ Female/
         └─ Custom/
            └─ MyVoice-AF-C100/
               ├─ manifest.json
               ├─ reference.wav
               └─ tones/angry.wav
```

## 3. Enable and assign voices

In MCM TTS API / speech settings, select AF-GPTSOVITS-L / U (auto) and enable speech. AF detects a compatible package and prefers L when both are present. Wait for Ready, then test local speech. Cold starts can take tens of seconds to a minute or two; do not repeatedly refresh.

Choose a voice in the character encyclopedia even before the first conversation. Named characters retain their random selection. Male/Female are same-gender default pools; Custom is manual-only.

Local L/U speech reads only the primary interlocutor; bystanders keep text. Private dialogue uses its current target. Peaceful meetings are allowed; real combat blocks local synthesis. Accepted complete replies are read sentence by sentence, not unchecked token-by-token.

After adding voices, refresh local runtime status or restart. Disabling speech releases only the service AF started.

## 4. VRAM and performance

A compatible NVIDIA GPU with 8GB VRAM is a practical starting point for testing; 12GB/16GB leaves more room beside the game. This is not a guarantee or a reason to buy a GPU solely for this add-on. Cards with 6GB or less are not recommended for concurrent gaming; consider a manually selected remote TTS service. This portable build targets Windows/CUDA; AMD, Intel and CPU-only plug-and-play operation is not validated.

L targets roughly 2GiB VRAM, NOT a hard cap. On a 16GB RTX 5070 Ti, 17 real syntheses sampled every 100ms peaked around 1546MiB (1.51GiB) of process dedicated VRAM. This is not an 8GB-card/game coexistence test and may miss short spikes.

On 8GB cards, leave roughly 2–3GB headroom for speech, lower game texture/graphics settings and close other GPU apps. An audition is not a long-session stability test. Watch total dedicated VRAM, stalls and long lines; disable local speech or select remote TTS if needed. VRAM, system RAM and disk space are separate budgets.

### Assessing your configuration

| Total VRAM | Concurrent gaming guidance |
| --- | --- |
| 6GB or less | Not recommended; prefer a configured remote service |
| 8GB | Starting point for testing; leave roughly 2–3GB for speech and reduce game graphics as needed |
| 12GB / 16GB or more | More headroom, but game/mod/background usage still matters |

These are conservative usage recommendations based on local L tests, not GPU certification or a guaranteed minimum specification. VRAM figures come from Windows process dedicated-memory sampling on 2026-10-17, including startup; PyTorch allocated memory is not total GPU usage. System-memory pressure, incompatible drivers and slow storage can also affect operation.

## 5. Custom recordings

A reference voice pack is not a trained model: it uses recordings, exact transcripts and manifest.json. Arbitrary .pth/.ckpt files are not plug-and-play character packs.

Recommended: one speaker, clean mono PCM16 WAV, about 3–10 seconds per reference; 16kHz is a suitable input. Do not upsample merely to claim quality. Avoid music, other voices, clipping, reverb or silence-only clips. Each file must be nonempty and no larger than 8MiB.

prompt_text must match that exact recording (1–1000 characters); prompt_lang describes the reference audio, not the requested speech language. This L build recommends zh/en/ja/yue.

Use the same character for every emotional reference. Recalculate SHA-256 after editing WAV files and update the manifest. Use voices you have permission to use; public distribution also needs rights to the recordings/character assets. An open-source code license does not license voice recordings.

## 6. Folders and manifest

Example: VoicePacks/Custom/MyVoice-AF-C100/manifest.json, with reference.wav beside it and tones/angry.wav below it. Discovery scans one package-folder level under Male/Female/Custom; do not add another download-wrapper folder. category is male/female/custom respectively; default-pool category must match gender.

Each profiles entry needs a stable unique id (1–128 characters, no outer spaces), label, speaker_metadata.gender (male/female), category, reference_file, reference_sha256 (the real 64-digit SHA-256), prompt_lang and prompt_text. Prefer English labels plus a permanent style_target code, e.g. My Voice · AF-C100. Paths are relative to the pack and cannot escape it or use absolute drive paths.

Optional emotion_variants keys are neutral/happy/angry/sad; each has its own path, hash, language and exact transcript. Limits: 2MiB per manifest, 128 manifests / 8MiB metadata / 512 voices overall. See docs/tts/manifest.example.json on GitHub; replace its placeholder hashes before use.

### Editable manifest example

[manifest.example.json](manifest.example.json)

```json
{
  "schema_version": 1,
  "profiles": [
    {
      "id": "custom-myvoice-AF-C100",
      "label": "My Voice",
      "style_target": "AF-C100",
      "category": "custom",
      "speaker_metadata": {
        "gender": "female"
      },
      "reference_file": "reference.wav",
      "reference_sha256": "REPLACE_WITH_ACTUAL_64_HEX_SHA256",
      "prompt_lang": "zh",
      "prompt_text": "替换为录音中的准确文本。",
      "emotion_variants": {
        "angry": {
          "reference_file": "tones/angry.wav",
          "reference_sha256": "REPLACE_WITH_ACTUAL_64_HEX_SHA256",
          "prompt_lang": "zh",
          "prompt_text": "替换为生气录音中的准确文本。"
        }
      }
    }
  ]
}
```

Save the template as manifest.json inside your own pack, then set your own ID, label, transcripts, audio paths and real hashes. Remove the angry entry if that recording is absent. Do not overwrite the root VoicePacks manifest.

```powershell
# Run in your own voice-pack folder / 在自己的语音包目录执行
(Get-FileHash -LiteralPath .\reference.wav -Algorithm SHA256).Hash.ToLowerInvariant()
(Get-FileHash -LiteralPath .\tones\angry.wav -Algorithm SHA256).Hash.ToLowerInvariant()
```

Refresh the catalog, select the custom voice in the encyclopedia and audition neutral/angry variants. Test other supported languages and long sentences too; valid hashes and formatting do not guarantee audio quality. Share the common runtime and small references instead of duplicating the whole environment per voice.

## 7. Language and emotion

Speech output language accepts Chinese, Cantonese, English or Japanese, including their Chinese names. Blank/auto follows the reply. All four providers share it: matching text is kept; otherwise postprocessing privately translates speech without changing subtitles or history. Typing the Chinese word for Japanese selects a language code, not spoken content. Failed or missing translation stays text-only.

Local tone uses the existing postprocessed MOOD, not gameplay action tags: JOY/DELIGHTED maps to happy, ANNOYED to angry, otherwise neutral; sad is available for manual audition.

Keep one stable ID per character and put emotional references in emotion_variants; do not split Sparkle into four characters. Missing tones use the base reference. An angry reference helps but does not guarantee acting quality. Invented cute/flirty categories are not supported by the current automatic tag mapping.

## 8. Troubleshooting and updates

No audio: check speech enabled, selected provider, runtime ready state, primary target, encyclopedia voice, combat restrictions and valid postprocessed translation when a language is forced. Bad reference paths/hashes, duplicate IDs or missing model files are rejected; AF does not silently switch to a paid API.

Refresh or restart after adding packs. Re-extract an incomplete package; never replace it while running. If language selection reports an old runtime, update the matching L package instead of editing runtime.json to bypass verification.

Errors: Logs/TTS; temporary cache: Cache/TTS. Report the error, AF/package version, GPU and reproduction steps after redacting API keys, tokens and private dialogue.

Merge only the intended add-on; retain custom voices, character IDs, settings and other mods. Use trusted executable packages. The supplied share link does not establish redistribution rights for every recording. U requires a separate hardware and compatibility guide when released.

## Version and source notes

This guide follows the actual AF catalog, MCM and L runtime. The share URL/code were supplied by the distributor; independent download verification was unavailable. No local archive SHA-256 is asserted for the remote ZIP, and the release filename does not prove that every 0.1.0 archive is byte-identical. Update the matching runtime if language capability is missing.

[Upstream GPT-SoVITS](https://github.com/RVC-Boss/GPT-SoVITS) has a different scope from this reduced add-on. This L build claims only its integrated zh/en/ja/yue paths, not upstream training, other platforms or a working Korean frontend.

AF-GPTSOVITS-U needs a separate download, installation and measured-memory guide when released; this page does not announce a U release.
