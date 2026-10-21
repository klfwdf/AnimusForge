# AF-GPTSOVITS-U 0.1.1 — Quality-first edition: installation and voice packs

[简体中文](AF-GPTSOVITS-U.zh-CN.md)

Updated 2026-10-21. **Local evaluation candidate, not a public release or a game deployment.** L remains separate; this add-on does not change game/GCCZ/save files. These are U-specific instructions, not the L 2GiB target.

0.1.1 includes the missing `resampy` dependency for reference-audio resampling. The original 16kHz references did not trigger the omission; custom 44.1/48kHz references could fail. This revision does not change V5 weights or the 32-step profile.

## U guide 1 - Version and quality

AF-GPTSOVITS-U 0.1.1 is a local evaluation add-on, not a publicly released download. It uses official GPT-SoVITS V5 Dev, native 48kHz output and a fixed 32-step quality profile. It is not a renamed L package. More steps do not guarantee perfect acting or the best result for every voice.

The portable runtime needs an AF build that explicitly supports engine gpt-sovits-v5 for U. Older managed-TTS builds reject this engine even if their version number looks sufficient; never edit runtime.json to bypass that check.

The extracted payload is about 7.86GiB; reserve at least 20GB for download, extraction and cache. This is separate from RAM/VRAM. No U download link has been published. Existing recordings remain local evaluation assets pending redistribution review.

## U guide 2 - Installation and L coexistence

Close the game and launcher, then extract into a temporary folder. Copy AF-GPTSOVITS-U and the needed VoicePacks contents into the existing Modules/AnimusForge folder. runtime.json must be at AnimusForge/AF-GPTSOVITS-U/runtime.json; avoid a second AnimusForge wrapper.

This add-on contains no AF DLLs or SubModule.xml and cannot update an old AF. Preserve game files, settings, GCCZ and custom voice edits. Keep server.py, ultra_engine.py, app and runtime intact. No separate Python installation or port-9880 service is needed.

Automatic detection still prefers a valid L when both L and U are installed. To test U, close the game and move L outside this module first, keeping it as a backup. Do not rename L into U or run both workers. Reading this tutorial never starts or downloads a model.

## U guide 3 - Enable and assign voices

Select AF-GPTSOVITS-L / U (auto) in the TTS settings, enable speech and wait for Ready. Confirm the detected package is AF-GPTSOVITS-U, then use the local voice audition. Cold loading can take about a minute; avoid repeated refreshes.

Choose a character voice in the encyclopedia before or after the first conversation. Five male and five female defaults keep the existing IDs; named characters retain their selection. Sparkle is one Custom character with emotional variants, not four separate characters. Custom is manual-only and never enters the default random pool.

Only the primary interlocutor speaks; bystanders retain text. Peaceful meetings are allowed, actual combat is blocked. Accepted complete replies are queued by sentence, not read as unchecked live tokens. Disabling speech or exiting AF releases only its owned worker. Refresh after installing new voices.

## U guide 4 - Hardware and measured limits

U prioritizes quality, not the L 2GiB target. This Windows/CUDA build requires a compatible NVIDIA GPU; CPU-only, AMD and Intel operation is not validated. It refuses startup if less than 6GiB VRAM is currently free. That is a conservative startup guard, not a claim that inference consumes 6GiB.

First smoke test: RTX 5070 Ti 16GB, no game running, PID-attributed WDDM sampling every 100ms. Dedicated VRAM peaked near 2292MiB; RAM near 4730MiB. A 14.4s Chinese clip took 21.33s to synthesize; cold readiness took 63.34s. Short spikes may be missed, and warm performance varies with text, reference and other load.

For concurrent gaming, 12GB/16GB GPUs and 16GB+ system RAM are sensible test configurations, not guarantees or reasons to buy hardware. 8GB gaming coexistence is NOT validated. This quality profile can be slower than real time. See the U document for multi-case measurements.

## U guide 5 - Custom voice-pack requirements

Use VoicePacks/Male/<pack>, Female/<pack> or Custom/<pack>, with manifest.json directly inside that one package level. Keep stable unique IDs, English labels + permanent codes, gender/category, relative reference_file, real SHA-256, prompt_lang and exact prompt_text. Use the published manifest.example.json structure, replacing placeholder hashes.

U enforces clean mono WAV references of 3–10 seconds, nonempty and at most 8MiB. PCM16 is recommended. Keep real source sample quality; artificial upsampling adds no detail. No music, other speakers, clipping or silence-only recordings. prompt_text must match the exact clip, at most 1000 characters; prompt_lang is zh/en/ja/yue for the recorded speech.

Each character can have neutral/happy/angry/sad in emotion_variants, with its own reference, hash and transcript. Missing variants use the base. Arbitrary .pth/.ckpt weights are not loadable character packs. Use authorized recordings; code licenses do not grant recording/character redistribution rights.

## U guide 6 - Language, tone and troubleshooting

Speech output language is shared by the four providers. Blank/auto follows the reply; Chinese, Cantonese, English or Japanese names select the target. Matching text is kept; otherwise postprocessing translates only the spoken copy. Subtitles/history stay unchanged; failed translation stays text-only.

Existing accepted MOOD selects local references: JOY/DELIGHTED -> happy, ANNOYED -> angry, otherwise neutral; sad is available in audition. No new action tags are introduced. V5 cannot guarantee anger/cuteness without suitable references or create every performance from one flat recording.

No sound: check provider, U compatibility, readiness, primary target, combat, voice hash/path and translation. Check Logs/TTS and Cache/TTS/U. Low free VRAM or OOM does not trigger a paid API or silently select L. Close other GPU apps or deliberately choose L/remote TTS. Never disable hash checks; do not publish tokens, keys or private dialogue. Update only the intended package with the game closed.

## Exact add-on layout

```text
Modules/AnimusForge/
├─ SubModule.xml                    # existing AF; do not replace
├─ bin/                             # existing AF; separate compatible update
├─ AF-GPTSOVITS-U/
│  ├─ runtime.json
│  ├─ server.py
│  ├─ ultra_engine.py
│  ├─ app/                          # V5 Dev + 48kHz vocoder + frontend
│  ├─ runtime/                      # portable Windows Python/CUDA libraries
│  └─ Documentation/
└─ VoicePacks/
   ├─ Male/<pack>/manifest.json
   ├─ Female/<pack>/manifest.json
   └─ Custom/MyVoice-AF-C100/
      ├─ manifest.json
      ├─ reference.wav
      └─ tones/angry.wav
```

**Compatibility:** an older AF supporting L is not automatically compatible with this V5 U. Use the separate AF update that permits the tested `gpt-sovits-v5` engine. This add-on does not include that program update; do not bypass verification. MCM and terminal use the same settings owner: the automatic local provider has an independent U guide, while all eight L chapters remain.

## Nine real syntheses (game NOT running)

Machine: RTX 5070 Ti 16GB (16303MiB reported), driver 617.42. Dedicated/shared VRAM sampled per worker PID using Windows WDDM every 100ms; RSS is process working-set RAM. These are neither torch-allocated counters nor total GPU usage nor hard caps. Other background applications were not controlled; this is not a hardware purchase recommendation.

- Peak dedicated VRAM: **2303.5MiB (2.25GiB)**.
- Peak shared GPU memory: **82.0MiB**.
- Peak process RAM (RSS): **5176.8MiB (5.06GiB)**.
- Ready: **20.84s** (earlier cold smoke: **63.34s**).
- Output: **48000Hz / mono / PCM16 WAV**, batch 1; nine successes, owned process exited.

| Voice | Tone | Language | Synthesis s | Audio s | RTF |
| --- | --- | --- | ---: | ---: | ---: |
| female-sparkle | neutral | all_zh | 36.47 | 16.40 | 2.22 |
| female-sparkle | happy | all_zh | 12.81 | 14.84 | 0.86 |
| female-sparkle | angry | all_zh | 12.59 | 15.48 | 0.81 |
| female-sparkle | sad | all_zh | 12.70 | 13.00 | 0.98 |
| male-clear | angry | all_zh | 10.53 | 12.96 | 0.81 |
| female-gentle | happy | all_zh | 9.19 | 11.40 | 0.81 |
| female-sparkle | neutral | en | 10.95 | 8.28 | 1.32 |
| female-sparkle | happy | all_ja | 7.94 | 8.72 | 0.91 |
| female-sparkle | neutral | all_yue | 8.06 | 8.56 | 0.94 |

RTF = synthesis duration / audio duration; above 1 is slower than real time. The first request took 36.47s and later requests 7.94–12.81s; not every line is real-time. Four Sparkle samples deliberately use the same text with different tone references for human comparison. Generation success does not certify acting, word accuracy or native accents. **Player audition, long concurrent gaming, old saves and other GPUs remain untested.**

## Make your own character pack

Start with [manifest.example.json](manifest.example.json) in your own category/package folder. Replace the ID, label, transcript, paths and every placeholder hash; do not overwrite the root catalog. Prefer an English translated label and a permanent style_target such as AF-C100; keep the internal ID stable. Default categories must match gender; Custom is never randomly assigned. Limits: 2MiB per manifest, 128 manifests / 8MiB metadata / 512 voices in discovery; transcripts up to 1000 characters. AF retains its sentence splitting; the service accepts at most 500 text characters per request.

```powershell
# Run in your own pack folder / 在自己的语音包文件夹执行
(Get-FileHash -LiteralPath .\reference.wav -Algorithm SHA256).Hash.ToLowerInvariant()
(Get-FileHash -LiteralPath .\tones\angry.wav -Algorithm SHA256).Hash.ToLowerInvariant()
```

Record each tone separately but retain one character ID. Remove absent variants rather than inventing files. Keep one speaker, correct cuts and exact transcripts; never mix another character into the emotional references. 32 steps and 48kHz cannot repair poor source recordings. This U supports zh/en/ja/yue, not the Korean frontend, arbitrary character fine-tunes or training datasets.

## Provenance and permissions

- [Official GPT-SoVITS source](https://github.com/RVC-Boss/GPT-SoVITS), pinned source: `a3035089c65ca63eb7777a4b0dafd198ebe36cda`.
- [Official model repository](https://huggingface.co/lj1995/GPT-SoVITS/tree/8f8857563c040d542de108a2c18b0425abd6c9c5/gsv-v5-pretrained), pinned model revision: `8f8857563c040d542de108a2c18b0425abd6c9c5`.
- `s2Gv5dev.pth`: 766385517 bytes; SHA256 `16c11fc588a9229a479316a11fc0f518087e79773f5271666a90b39d50460407`.
- `vocoder.pth`: 57761163 bytes; SHA256 `b7543c83222aa2105bb4efb56845086bc5f2eeba61243d723ffa062052b3221f`.

Downloaded models were checked against pinned byte lengths and SHA-256. Code, weights, portable dependencies and character recordings have separate permissions; this candidate has redistribution_cleared=false. No public upload was made; this is not a final license decision. Review before sharing. U uses no quantization, L BERT pruning or automatic Turbo switch. There is no training UI, weight switching, public listener, arbitrary reference path or cloud fallback. The existing separate manual API route remains available to advanced users.
