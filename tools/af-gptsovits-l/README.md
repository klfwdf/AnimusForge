# AF-GPTSOVITS-L · 改造源码与使用说明

这是 AF 专用轻量推理组合的开发源码，不是重新训练的模型，也不是已获准发布的安装包。当前实验输出与原 GPT-SoVITS 安装分开；游戏不在构建脚本的写入范围内。实际版本、测试结果和剩余阶段统一记录在[主台账](../../docs/animusforge-refactoring-and-repository-reorganization-plan.md#tts-guide-package-20261018)。


## 分发边界 / Distribution scope

- 本分支名称 **`AF-GPTSOVITS=L`**。GitHub 只保存改造源码、构建/打包/验证脚本和使用说明；**不提交 Python、模型权重、角色录音或大 ZIP，不使用 Git LFS**。
- AF 本体的 MCM / 终端「语音使用说明」位于 `main`，产品提交 `fb01ecbcb`；本分支以其已验证主线为基线，不合并旧本地交接历史。要出现新说明按钮必须更新兼容的 AF 主程序；附加语音 ZIP 不会替换主程序。
- [离线中英说明书](User-Guide.html) / [自定义清单模板](manifest.example.json)。模板故意不是可运行音色：须提供自己的录音、准确文本和真实 SHA-256；没有录音的语气项应删除。
- 同一说话人的各语气放进同一 profile 的 `emotion_variants`；Custom 不参加随机默认池。运行版本目前不热切换任意训练权重。
- The branch is source/documentation only; no LFS, weights, runtime or recordings are published. The TTS guide in main is available through both MCM and terminal; the optional L add-on never updates AF DLLs. Read the bilingual HTML guide and supply licensed recordings before using the example manifest.

## 本地可用附加包 / Local evaluation add-on

`package_addon.py` reads a trusted existing `build_bundle.py` output **without modifying it**. It checks the runtime file hashes, includes only `AF-GPTSOVITS-L/` and actual references in `VoicePacks/`, strips obsolete development-machine paths from voice metadata, and adds the offline guide/template. It excludes main-mod DLLs, SubModule.xml, ModuleData, gameplay configuration, logs and caches. Python dependency DLLs remain inside the portable runtime. It then extracts into a **new separate verification directory**, checks every file SHA-256 and ZIP CRC, and writes a receipt. Existing outputs are never replaced. An interrupted output is not a completed package; use a new output name after diagnosing the failure.

```powershell
& $python tools/af-gptsovits-l/package_addon.py `
  --module '<fresh-bundle>/AnimusForge' `
  --output '<output>/AF-GPTSOVITS-L_0.1.0_LocalTest.zip' `
  --verify-extract '<fresh-verification-directory>'
```

当前打包器明确面向现有 **五男＋五女＋花火一套 Custom** 的本地评估素材；不是任意录音授权审核工具。默认池参考文件按男女分类保存，花火各语气仍属于同一音色 ID。ZIP 内的附带说明告诉玩家解压到 `Modules` 并合并 `AnimusForge`，但不要盲目覆盖自己修改过的 VoicePacks。不要把验包目录的测试身份、缓存或日志重新装回 ZIP。生成完成后另行运行实际进程/合成测试；CRC/哈希通过不能代替模型启动和游戏验收。

This packaging tool requires the current 5 male + 5 female + 1 custom layout and leaves `redistribution_cleared=false` intact. The ZIP is for local evaluation only; license review is required before distributing it on GitHub/Nexus. Windows x64 and a compatible NVIDIA CUDA driver are needed for this particular portable runtime; no AMD/macOS or universal 8GB gaming guarantee is implied. The local runtime footprint and ZIP size are disk metrics, not VRAM usage.

## 运行方式

```text
AnimusForge/
  AF-GPTSOVITS-L/
    runtime.json
    runtime/python.exe
    server.py
    lite_engine.py
    app/
  VoicePacks/
    manifest.json
    Male/AF-M001/manifest.json
    Female/AF-F001/manifest.json
    Custom/AF-C001/manifest.json
```

- 必须配合本次新增托管接口的 AF；需包含 AF 接线提交 `64cbc4d1d` 或其后继版本；旧 AF 不会仅凭目录获得此功能。`runtime.json` 的 `af_runtime_api=1` 是协议版本，不是旧 DLL 的补丁。
- 安装版 AF 从实际模组根识别相对目录；启用语音并选择 **AF-GPTSOVITS** 后验证、启动包内 Python。不分 L/U 控件；两者同时存在时优先通过校验的 L。关闭语音释放 AF 自己启动的进程。
- **GPT-SoVITS（手动 API）** 是独立选项，使用手填本机 URL 和可选外部清单。自动模式忽略但不删除手动配置；缺包保持文字，不默默转手动/云服务。不抢占 9880、不停止玩家手开的服务、不自动调用付费 API。
- 冷启动在后台进行，状态从准备中变为可用；准备期间的对白仍显示文字，不排队等启动后补读旧台词。进程启动与健康检查期限为 120 秒（此前文件校验另计），失败后由玩家刷新重试，不无限自启。
- 服务只监听随机回环端口，实例身份放在 `X-AF-Instance`；仅开放合成和健康检查，不开放训练、远程管理或热切权重。
- 缓存和临时音频在模组 `Cache/TTS`，旋转日志在 `Logs/TTS`。默认不在日志记录对白和实例 token。

## 声音与能力

- 五男五女默认池；花火是一套 Custom 声音，语气参考归同一 `voice_id`。自定义不进随机池，原百科预选和角色绑定仍沿用。
- 英文名加永久短码显示，例如 `Sparkle · AF-C001`。`voice_id` 和旧别名保留，不按目录顺序重编号。
- 默认目录显式刷新后缓存；只扫描分类下一级包目录。有界读取，坏包隔离，冲突 ID 拒绝歧义项。任意训练权重不会自动加载。
- 当前声明前端为 `zh/en/ja/yue`；韩语 Windows 前端尚缺，遇韩文明确拒绝。真实语言测试结果见主台账；支持语言不代表保证母语口音或翻译正确。
- 仅主对象进入本地合成；旁人文字和互动正常。私人对话继续使用会话人物，不因 `AgentIndex=-1` 而禁声；火山逻辑不改。
- 新版 AF 已融合后处理 MOOD→本地请求私有语气：JOY/DELIGHTED 为 happy，ANNOYED 为 angry，其余中性，sad 可手工试听。主正文 TTS_TONE 只剥离，不决定语气。主 API SSE 保留完整校验，本地再分句；原 OpenAI 上游音频流式保留。

## 轻量化实现

`lite_engine.py` 是独立派生实现，官方 app 原件不改：

1. 只保留上游实际使用的第 22 层 BERT 特征，去掉其后的两层与无用 MLM 输出，不保存全部中间隐藏层。
2. T2S 权重先在 CPU 读取并转 FP16，再放入 GPU，避免检查点和中间 FP32 GPU 副本。
3. HuBERT/SV 参考网络仅在需要时上 GPU，计算后回到 CPU；语义生成、BERT 和解码仍使用 GPU，并非全 CPU 冒充优化。
4. 参考特征使用 CPU LRU，最多 44 项且主缓存最多 64 MiB；当前参考才放到 GPU。默认素材直接可用，无须玩家自己预处理。
5. 单任务、batch 1、无 CUDA Graph；后两项为原有条件，不作为新增优化成果。

2GB 是目标，不是驱动硬隔离上限。显存、系统内存、安装体积和速度分开报告；8GB 游戏共存仍需对应硬件实测。

## 开发工具

- `benchmark.py`：同输入原版/轻量合成及 BERT 特征对照；记录 torch allocated/reserved，整卡采样只作背景或增量估算。
- `build_bundle.py`：从已安装资源、导入追踪和参考清单构建全新输出目录；使用基础 Python 而不是含开发机路径的 venv launcher，附模型/入口哈希。重复运行必须换新输出目录，不覆盖原件。
- `portable_stress.py`：实际启动独立包，五男五女及花火语气/长句/日语 17 条合成，按 PID 以约 100ms 周期采样 Windows WDDM Dedicated/Shared Usage。退出清理自己的进程，不接触游戏。
- `tests/modules/AF.Module.Llm/GptSoVitsRuntimeTests`：真实进程/HTTP 所有权回放；`--real` 使用生产管理器、目录和网关启动真实包。此模式要求实验根已有正确模块身份与 ModuleData，只验证原件身份不变，不创建或覆盖游戏身份。

实验根的 `SubModule.xml`、空 `ModuleData`、`Cache`、`Logs` 不是附加包内容。未来附加包只准包含 `AF-GPTSOVITS-L/` 和 `VoicePacks/`；不要把整个实验根覆盖游戏，也不要把旧 TTS 工作树 DLL 覆盖其他制作组的新功能。模型与角色录音公开再分发许可尚未确认，当前仅本地评估。

## 从源码复现

此分支是 AF 接线 + 完整 L 推理/服务/构建/测试源码，**不是含 Python、权重、角色录音的即用安装包**。这些二进制资源不提交 Git。

输入由使用者本地提供：已运行的 Windows GPT-SoVITS v2Pro（当前上游目录契约 `a3035089c65ca63eb7777a4b0dafd198ebe36cda`）；安装根下 `app/`、`.venv312/Lib/site-packages/`、`python/cpython-3.12.15-windows-x86_64-none/`；哈希和文字配对正确且有使用许可的语音 manifest。当前构建器默认固定 5 male + 5 female + Sparkle Custom 的 ID（见代码）；其他布局/声音须显式适配。不同 GPU 需要可用的 PyTorch/CUDA，本版不保证 AMD/macOS 自动兼容。

占位路径替换为自己的路径，每次使用新的实验输出；脚本不会自行下载权重或素材：

```powershell
$python = '<installation>/.venv312/Scripts/python.exe'
& $python tools/af-gptsovits-l/benchmark.py --mode bert --app '<installation>/app' --manifest '<voices>/manifest.json' --output '<fresh-benchmark>/bert'
& $python tools/af-gptsovits-l/benchmark.py --mode lite --app '<installation>/app' --manifest '<voices>/manifest.json' --output '<fresh-benchmark>/lite'
& $python tools/af-gptsovits-l/build_bundle.py --installation '<installation>' --imports '<fresh-benchmark>/lite/imported-modules.json' --voices '<voices>/manifest.json' --output '<fresh-bundle>'
& $python tools/af-gptsovits-l/portable_stress.py --module '<fresh-bundle>/AnimusForge' --output '<fresh-stress>'
```

`runtime.json` 的 `gpu_target_mib=2048` 是目标。不要将 `gpu_target_verified=false` 或 `redistribution_cleared=false` 直接改成 true 冒充测试/授权。发布仍须代表性硬件与游戏并行显存采样、权重/素材/依赖许可审核。构建报告、私有路径、缓存和音频留在忽略的 artifacts 中。

## English summary

This branch contains AF integration plus complete L inference/build/test source, not a newly trained model or a public binary/model release. It does not include portable Python, weights or recordings. Use AF integration `64cbc4d1d` or later. Choose **AF-GPTSOVITS** and enable speech: compatible L/U packages are discovered under the module, with verified L preferred. Manual URL/manifest values are ignored but preserved. Choose **GPT-SoVITS (manual API)** explicitly for an existing local service. Missing/unready packages stay text-only, without paid fallback.

Only the primary scene interlocutor is synthesized; private conversation keeps its target. Custom voices remain manual-only and can be selected before talking. Existing postprocessing supplies typed local-tone metadata; main-reply tokens do not drive tone or gameplay. Local speech reads accepted complete replies sentence by sentence; latest OpenAI audio streaming stays intact.

The independent profile removes unused BERT layers/outputs, avoids duplicate CUDA checkpoint allocations, stages reference encoders on demand and bounds CPU reference caches. GPU synthesis is retained. The 2GB VRAM budget is a target, not a hard cap or an 8GB game coexistence guarantee. Korean is unavailable; supported language codes do not guarantee native accent or translation accuracy. Supply licensed build inputs in the documented Windows layout. Never ship lab module markers, overwrite the game with the lab root or redistribute uncleared recordings.


## Explicit speech language / 指定朗读语言

AF MCM / terminal exposes `TtsGptSoVitsOutputLanguage`. Blank keeps `auto`; Chinese or English language names map to `all_zh`, `all_yue`, `en`, `all_ja`. The API `prompt_lang` still describes the reference recording and is never changed to match the output. `/af/health` advertises `capabilities.text_language_selection=true`; new AF refuses forced-language speech on an older package that lacks this capability rather than silently accepting automatic language detection. Manual standard GPT-SoVITS API remains separate.

MCM / 终端可填写中文、粤语、英语、日语或英文名称；留空仍为自动。AF 在现有后处理中确认口语语言：相同输出 `keep`，AF 使用原始朗读文本而不是模型改写；不同才传隐藏译文。字幕、历史、游戏动作不使用译文。显式语言的后处理缺失/异常/超时保留文字，不硬按错误语言发音。国语与粤语不只凭汉字判定相同。

The translation sidecar is AF-only presentation metadata; this server receives plain speech text plus the validated `text_lang`. It does not parse LLM tags, call another LLM or translate on its own. Only reference mood selection stays with AF. Supported language fronts are Chinese/Mandarin, Cantonese, English and Japanese; selecting a language does not guarantee native accent or translation accuracy.

服务器只接收 AF 已分离的台词和语言代码，不解析游戏标签，不额外调用翻译模型。不可只替换 `server.py`：包内 `runtime.json` 的 `files.server.py` SHA256 必须同步，新构建脚本会自动生成。老包自动模式不受影响。U 包将来也须实现相同能力声明与参数契约；本轮未测试 U。

Validation: `python tools/af-gptsovits-l/test_language.py <fresh-workspace-fixture>` executes the actual handler with controlled model/audio leaves. It covers legacy auto, all four forced codes, unsupported input, reference-language preservation, authentication and serialization. Real-model tests are in AF's `GptSoVitsRuntimeTests --real`; neither test is permission to alter the installed game.

## 本轮验证 / Verification (2026-10-18)

- MCM/terminal guide: original dual 1.3/1.4 + Bootstrap build passed; actual settings/inquiry/localization fixtures: 853 (1.4 CN), 579 (1.3 EN). Native in-game rendering is still untested and the candidate was not deployed.
- Source handler: 39 checks; package boundaries/example: 16; generated example accepted by the production voice-catalog fixture (554 checks); relocated 5M/5F catalog (604 checks).
- Local add-on: ZIP64/Deflate, 5,292,142,349 bytes (~4.929 GiB), extracted indexed payload 7,833,160,621 bytes (~7.295 GiB), 25,357 indexed payload files. Every archived payload SHA-256 and ZIP CRC was checked during fresh-directory extraction. The index itself is an additional file. SHA-256: `85e5059da4da5a1771e5d48d99df7e138b6c0b0682c1acba5b2a4c749ac6dcbf`.
- Fresh relocated bundle: source-linked production AF manager/gateway/catalog launched the real portable Python/model, generated 5 nonempty WAV files in Chinese/English/Japanese/Cantonese, and rejected unauthenticated health access (403). The test added module identity only in its disposable verification directory, NOT in the ZIP or game. It shut down its owned process afterward. This is real synthesis, not game playback; recordings were not subjectively re-rated in this task.
- 本机本轮冷启动约 87 秒，首条合成约 13.4 秒；不是“开关打开立即零延迟出声”。后续此组短句约 4.4–10.7 秒，不能推算所有硬件和长句。此次未重新测量显存，不把 2 GiB 目标标记为已经广泛验证；8GB 显卡与游戏同时运行仍需实测。
- No game installation, public model/voice upload, LFS use, paid billing change or unrelated gameplay edit occurred. The ZIP remains a local evaluation artifact, not a redistribution-cleared release. Current evidence and remaining acceptance are in the main ledger.
