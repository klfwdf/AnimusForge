# AF-GPTSOVITS-U — independent quality-first runtime

[玩家中文教程](../../docs/tts/AF-GPTSOVITS-U.zh-CN.md) · [English player guide](../../docs/tts/AF-GPTSOVITS-U.en.md)

## Scope / 范围

U 0.1.1 is a **local evaluation** candidate, not an approved public model/recording distribution. It uses official V5 Dev (32 sampling steps, CFG 1.30), its native 48kHz vocoder, s1v3 and full pretrained BERT/HuBERT. It is not L renamed, not training, and does not change existing L/game files. No universal “best voice” claim: source-reference quality and human audition still matter.

U 0.1.1 为本地评估版：官方 V5 Dev、32 步、CFG 1.30、原生 48kHz 声码器及完整预训练前端，不给 L 改名，不训练，不改现有游戏。这里只提交封装源码/说明，不提交权重、录音或便携二进制；正式公开分发另审许可。

### Bounded runtime / 运行边界

- Windows x64, portable Python 3.12.15 / torch 2.8.0+cu128. Tested RTX 5070 Ti, driver 617.42; not an AMD/Intel/CPU or 8GB-game coexistence certification.
- Model resident on GPU in FP16; checkpoint staged on CPU to avoid duplicate GPU loading. No L BERT pruning, quantization, Turbo fallback, CUDA Graph or FlashAttention-extension requirement.
- Single synthesis job, batch 1. Reference embeddings use a CPU LRU: at most 44 entries / 128MiB of cached tensor payload; only the active reference is moved to GPU. Hashing ≤8MiB occurs per accepted synthesis, not per game Tick.
- Startup requires at least 6GiB **free** VRAM, a conservative guard rather than actual usage. Nine-case measured peak: 2303.5MiB dedicated VRAM, 5176.8MiB process RSS. No hard memory cap. U does not inherit L's 2GiB target.
- Random loopback listener, per-AF-instance token, parent-process handle lifetime, API 1 exact U/V5 identity. Never bind to a public network or reuse port 9880.
- ≤32KiB HTTP body, ≤500 text characters, ≤1000 prompt characters, confined VoicePacks reference files ≤8MiB / mono WAV / 3–10 seconds. Output ≤32MiB PCM16 mono WAV. No arbitrary model switches or extra reference paths. zh/en/ja/yue only; Korean frontend is not shipped.
- Model failures do not silently call a cloud API. OOM makes the worker fail; AF controls restart/shutdown. Logs do not contain dialogue, reference transcripts or instance tokens.
- Existing AF owns target selection, game combat exclusion, accepted reply queue, language translation and postprocess MOOD. U only receives the chosen text and reference; it introduces no gameplay tags or new private conversation pipeline.

## Rebuild / 重建

### 0.1.1 dependency correction / 依赖修正

The 0.1.0 donor omitted `resampy`: its 16kHz references passed, but upstream `_set_prompt_semantic` needs the lazy import for other rates. 0.1.1 includes the pinned resampy 0.4.3 wheel (including its license), protects its files in `runtime.json`, and tests the actual upstream resampler at 16/22.05/24/32/44.1/48kHz without loading models. The inference source/weights/profile are unchanged. The matching L builder explicitly traces this dependency too; existing L installations are not modified.

0.1.0 的 16kHz 参考音频掩盖了 `resampy` 延迟导入缺失；0.1.1 补齐固定版本及许可证，校验六种采样率的实际重采样函数，不改变模型、权重或采样档。L 源码构建清单同步补齐依赖，但不改用户已安装的 L 包。

Download `resampy-0.4.3-py3-none-any.whl` from the exact PyPI URL in `runtime_dependencies.py`, saving it under `artifacts/`. The builder verifies SHA-256 `ad2ed64516b140a122d96704e32bc0f92b23f45419e8b8f478e5a05f83edcebd` before copying. It never installs into a global environment or downloads dependencies during player startup.

Use a trusted existing **normalized portable L** as the dependency-runtime donor, and a separately retained matching upstream app with the original full BERT. This tool is not a fresh pip environment installer. Inputs are read-only and must be explicitly supplied; output must be fresh and separate. Models are pinned to official revision/size/SHA in `download_models.py`; the source used for this candidate is `a3035089c65ca63eb7777a4b0dafd198ebe36cda`.

需要可信且已规范化的 L 便携包作为依赖环境来源，以及保留原始完整 BERT 的匹配上游源码；不自动升级已有 GPT-SoVITS 或修改游戏。所有输出使用新目录。下面参数换成自己的路径，不要使用游戏目录作为验证输出。

```powershell
python -B tools/af-gptsovits-u/download_models.py --output artifacts/u-models
python -B tools/af-gptsovits-u/build_bundle.py --portable-l '<verified-L-module>/AF-GPTSOVITS-L' --upstream-app '<pinned-upstream-app>' --models artifacts/u-models --resampy-wheel artifacts/resampy-0.4.3-py3-none-any.whl --output artifacts/u-bundle
artifacts/u-bundle/AnimusForge/AF-GPTSOVITS-U/runtime/python.exe -B tools/af-gptsovits-u/check_runtime.py --bundle artifacts/u-bundle/AnimusForge/AF-GPTSOVITS-U
python -B tools/af-gptsovits-u/test_server.py
python -B tools/af-gptsovits-u/portable_benchmark.py --module artifacts/u-bundle/AnimusForge --output artifacts/u-benchmark
python -B tools/af-gptsovits-u/package_addon.py --module artifacts/u-bundle/AnimusForge --output artifacts/AF-GPTSOVITS-U_0.1.1-local-evaluation.zip --verify-extract artifacts/u-relocated
```

`test_server.py` uses the actual HTTP handlers with a fake audio engine; it does not load GPU weights. Other commands require relevant build/test dependencies (FastAPI/numpy/soundfile and the TestClient client required by the installed Starlette version; this candidate uses test-only httpx2 2.16.0; benchmark psutil). Keep those test-only packages outside the player runtime. The portable player's Python/runtime is bundled already. `package_addon.py` first runs the portable dependency/resampler probe, checks all runtime hashes and referenced WAVs, keeps five male/five female/one Custom voice, excludes unrelated voice-pack files, then reads every ZIP entry to verify CRC and extracted hashes. No game DLL, settings, logs, private chats or SubModule.xml enter this add-on archive.

`portable_benchmark.py` starts/stops only its own worker and records true PID WDDM/RSS samples; it does not run Bannerlord. Full matrix: four same-text Sparkle tones, male/female Chinese, English, Japanese, Cantonese. RTF is measured, not assumed; first-call overhead is retained rather than hidden. Each request is serialized. Recorded WAVs are audition outputs, not evidence of subjective accent/emotion accuracy.

## AF integration / AF 接入

The sole model-discovery extension permits `gpt-sovits-v5` **only** for edition U; legacy v2Pro L/U compatibility and L-first discovery remain unchanged. Older AF will reject V5 U even if its numeric version satisfies `af_min_version`; ship a matching AF update separately after verifying the installed gameplay source. Do not disguise V5 as v2Pro to bypass the health identity. The archive itself cannot upgrade AF.

源码接入仅放行 U 的 V5 身份，保留原先 L/U 优先级、百科、语气和语言设置。MCM/终端新增独立 U 教程，不另建配置。要真正选 U，须先退出游戏，将有效 L 移到模组外保留；不要同时启动多个服务。当前游戏未被本任务更新。

Validation owners:
- `tests/modules/AF.Module.Llm/GptSoVitsRuntimeTests`: supported/unsupported engine matrix, priority, real process lifecycle fixture; `--real <relocated-module> <fresh-results>` calls production manager/catalog/gateway against the actual portable model.
- `tests/AF.GameAdapter.Bannerlord/TerminalLiveSettingsTests`: actual dual-build DLL/MCM/terminal, both UI languages, correct owner, provider visibility and read-only native guide pages.
- Use the original AF dual build entry. Do not introduce a separate game deployment path. Game rendering, old saves and broad hardware tests remain separate acceptance work.

## Source files / 文件职责

`download_models.py`: official pinned input only. `build_bundle.py`: independent derivation and input preservation. `ultra_engine.py`: V5 quality profile and bounded prompt cache. `server.py`: authenticated/bounded inference service and parent lifetime. `test_server.py`: handler negative cases. `portable_benchmark.py`: real output/performance evidence. `package_addon.py`: local-only archive and relocation verification. `User-Guide.html`: self-contained CN/EN player help, matching `docs/tts/AF-GPTSOVITS-U.*.md`; update together when releasing a new U version.

Never commit this worktree's excluded artifacts, models, voice recordings, private logs or local fusion history to the public main branch. Any later public source publication should be an explicit TTS-only integration onto the then-current public baseline.
