# AF-GPTSOVITS-U：源码与必要改造 / Source and adaptations

本分支 `AF-GPTSOVITS-U` 从公开 AF `main` 的 `d2d05f0c` 建立。新增提交只提供源码、必要 AF 接线、测试和说明；不上传便携 Python、模型权重、角色录音、私人日志或本地融合历史。它不是一个可以直接解压使用的 GitHub 模型发行包。

This branch starts from public AF main at `d2d05f0c`. Its new commits contain source, minimal AF integration, tests and documentation only. They add no portable Python, weights, voice recordings, private logs or local fusion history. The inherited AF baseline is not a newly packaged model distribution.

## 先分清版本 / Actual model families

**U 0.1.1 实际是 GPT-SoVITS V5 Dev，不是 V2Pro。** V2Pro 是 L 轻量推理方案的基础，原来对它的必要改造保留在 `tools/af-gptsovits-l/`，来自已经公开的 `AF-GPTSOVITS=L` 分支 `e455d07e`。不能把 U 的名称写成 V2Pro，也不能通过篡改 `runtime.json` 冒充另一种引擎。

**U 0.1.1 uses GPT-SoVITS V5 Dev, not V2Pro.** The existing public V2Pro/L adaptation is retained under `tools/af-gptsovits-l/`, sourced from public branch `AF-GPTSOVITS=L` at `e455d07e`. It is a separately labelled low-VRAM reference, not U's inference engine.

| 项目 / Item | V2Pro / L | V5 Dev / U |
|---|---|---|
| 推理派生类 / Inference adapter | `tools/af-gptsovits-l/lite_engine.py` | `tools/af-gptsovits-u/ultra_engine.py` |
| BERT | 保留上游实际使用的第 22 层特征，移除后续层和 MLM 输出 / retain the used layer-22 features; remove later layers and MLM head | 完整预训练前端，不沿用 L 的裁剪 / full pretrained frontend, no L truncation |
| 检查点搬运 / Checkpoint staging | CPU 读取并先转 FP16 / CPU loading and FP16 before GPU transfer | 同样避免额外 FP32 GPU 检查点副本 / also avoid a duplicate FP32 GPU checkpoint |
| 参考网络 / Reference networks | HuBERT / SV 按需上 GPU、随后移回 CPU / on-demand GPU offload | 常驻质量优先管线；不宣称采用 L 的 offload / resident quality-first pipeline, no L offload claim |
| CPU 参考缓存 / CPU reference cache | 最多 44 项 / 64 MiB | 最多 44 项 / 128 MiB |
| 解码设置 / Decoder profile | 原 V2Pro / original V2Pro | V5 Dev、32 步、CFG 1.30、原生 48 kHz / 32 steps, CFG 1.30, native 48 kHz |
| 并发 / Concurrency | 单任务、batch 1 / one synthesis job | 单任务、batch 1 / one synthesis job |
| 显存目标 / VRAM policy | 2 GiB 是目标，不是驱动硬上限 / target, not a hard driver cap | 不沿用 2 GiB 目标；启动检查至少 6 GiB 空闲显存 / no 2 GiB promise; 6 GiB free startup guard |

这些是独立派生类、服务包装与资源组织的改造，不是重新训练、量化或修改原始权重。保留原安装不动，所有构建输出写新目录。U 使用不同的模型家族；不能仅凭名字、完整 BERT 或采样步数保证每个人物的主观听感更好。

These are inference subclasses, service wrappers and resource-layout adaptations, not retraining, quantization or modification of original weights. Original installations remain read-only. U uses a different model family; its name, full BERT or step count does not guarantee better subjective quality for every character.

## 固定上游 / Pinned upstream

- [GPT-SoVITS source a3035089](https://github.com/RVC-Boss/GPT-SoVITS/tree/a3035089c65ca63eb7777a4b0dafd198ebe36cda)
- [Official model repository, pinned revision](https://huggingface.co/lj1995/GPT-SoVITS/tree/8f8857563c040d542de108a2c18b0425abd6c9c5)
- 原 17 项受保护运行文件（含模型和推理入口）在 0.1.1 中保持同一 SHA-256；仅新增 13 项依赖/检查文件和更新清单版本。 / All 17 original protected files retain their hashes; 13 dependency/probe files are added and the package version is updated.

## 必要源码 / Required source map

- `tools/af-gptsovits-u/download_models.py`：固定官方模型修订、大小与 SHA-256；不把权重放进 Git。 / Pinned official model revision, sizes and SHA-256; weights stay outside Git.
- `tools/af-gptsovits-u/build_bundle.py`：从已验证、已经便携化的 L 依赖环境派生独立 U，并恢复完整 BERT、换入 V5 Dev 和 48 kHz vocoder；不修改输入。 / Derive U from a verified normalized portable L runtime, restore full BERT and select V5 Dev plus its 48 kHz vocoder without editing inputs.
- `tools/af-gptsovits-u/ultra_engine.py`：CPU 检查点暂存、FP16、固定采样档、有界 CPU LRU、输入参考身份缓存。 / CPU checkpoint staging, FP16, fixed sampling profile and bounded reference cache.
- `tools/af-gptsovits-u/server.py`：随机回环地址、每实例身份、父进程生命周期、请求与音频边界、单任务合成；不开放任意换权重。 / Loopback-only per-instance service, parent lifetime, bounded requests/audio and serialized synthesis; no arbitrary weight switching.
- `tools/af-gptsovits-u/package_addon.py`：仅打包 `AnimusForge/AF-GPTSOVITS-U/` 与所需 `AnimusForge/VoicePacks/`；校验运行清单、参考录音、ZIP CRC 和回读哈希；不包含 AF DLL/游戏配置。 / Allowlisted model add-on packaging with reference/runtime/hash/CRC verification and no AF host DLL or game settings.
- `tools/af-gptsovits-u/test_server.py`、`portable_benchmark.py`：分别为不加载 GPU 模型的实际 HTTP handler 测试，以及真实模型音频/进程内存采样。两种证据不可混称。 / Actual-handler tests with a fake audio engine versus real-model audio/process-memory measurements; these are different evidence levels.
- `src/modules/AF.Module.Llm/Tts/GptSoVitsRuntimePackage.cs`：只额外允许 U 的 `gpt-sovits-v5` 身份；保留旧 V2Pro L/U 与有效 L 优先规则。 / Permit V5 only for U, preserving legacy V2Pro L/U and valid-L-first discovery.
- `DuelSettings.TtsUltraGuide.cs`、`GptSoVitsUltraGuide.cs` 与配套中英文资源：独立 U 教程入口，不增加 provider 索引，不复制玩家设置。 / Separate U guide using existing settings owners, without new provider indices or duplicated configuration.

主对话对象、和平会面/战斗规则、角色百科绑定、后处理 MOOD、按需翻译与分句队列仍归 AF。U 只接收 AF 已经决定好的文本、参考音色和语言，不自行解释其他游戏标签。

Primary-speaker filtering, peaceful-meeting/combat rules, encyclopedia voice assignment, postprocessed MOOD, translation and sentence queues remain AF responsibilities. U consumes the already-selected text, reference and language; it does not reinterpret gameplay tags.

## 如何复现 / Reproduction

1. 阅读 [U 开发入口](../../tools/af-gptsovits-u/README.md)；准备有合法使用权限的模型、参考音频，以及匹配的上游源码和已规范化便携 L。所需依赖不是通过本分支自动安装的。 / Read the U developer entry; supply authorized assets, matching upstream source and a verified normalized portable L runtime. This branch is not an environment auto-installer.
2. 按入口文档依次获取固定模型、构建新 U 目录、跑 handler/真实音频测试，再打本地评估 ZIP。不要把游戏目录作为验证输出。 / Follow the pinned download, fresh build, handler/real-audio validation and local ZIP steps. Never use the installed game as a test-output directory.
3. AF 接线通过原双 API/Bootstrap 构建流程验证；仅使用项目本地输出，不带 `-Deploy`。真实包测试必须已有合法模块身份，不创建/替换玩家的 `SubModule.xml`。 / Validate AF with its original dual-API/Bootstrap build, without deployment. Real-package tests require existing module identity and never create/replace a player's `SubModule.xml`.
4. `main` 与当前游戏不会因这个源码分支自动更新；部署应另行确认现有游戏来源，只合入 TTS 改动。 / Neither main nor the installed game is updated automatically by this source branch; integration/deployment must preserve the then-current game build.

## 0.1.1 修订与验证 / Dependency correction and validation

- 0.1.0 自带参考音频为 16kHz，未覆盖上游 `match_librosa=True` 的延迟 `resampy` 依赖。0.1.1 补齐固定 PyPI `resampy 0.4.3` 及许可证，所有新增运行文件纳入清单 SHA-256。 / 0.1.0's 16kHz references missed a lazy resampy import; 0.1.1 ships the pinned dependency/license and protects its files with SHA-256.
- `runtime_dependencies.py` 固定 wheel 下载地址、版本与哈希；仅用于新输出副本。`check_runtime.py` 从实际固定上游源码抽取原重采样函数，覆盖 16/22.05/24/32/44.1/48kHz → 16kHz；不加载权重或申请 CUDA。打包前执行相同检查。 / Pinned-wheel copying targets a fresh build only; the packaging gate exercises the actual upstream CPU resampler at six input rates without loading weights/CUDA.
- 重打包时仅从本轮源码生成 Documentation，忽略输入中旧教程/旧索引；新增重复条目门禁与 3 组回归，防止 ZIP 出现同名校验索引。 / Repackaging regenerates Documentation from source, excludes old indices, and rejects duplicate names; three regressions protect the boundary.
- 原始上游 `TTS.py` 与固定提交 `a3035089c65ca63eb7777a4b0dafd198ebe36cda` 一致；不需要遗漏在本仓库之外的私有推理补丁。V2Pro/L 来源仍为公开分支 `e455d07e`，本次额外只在其构建器显式导入 `resampy` 防止同样的漏打包；不重建或覆盖已有 L。 / Upstream source matches the pinned revision; no undisclosed private TTS.py patch is required. The only new change to the imported public L toolset is explicitly tracing resampy during build; existing L installations stay untouched.
- 验证：AF 1.3/1.4 + Bootstrap 原入口构建及双 Coup 门禁通过；48 项运行包/进程夹具断言通过；6 组真实 HTTP handler 测试通过（假音频引擎，隔离的 test-only httpx2）；双语真实 DLL/MCM/终端检查每种语言为 1.4: 1240、1.3: 903。18 个 Python 源码解析、14 对 UI key 和本地文档链接通过。 / These are production-source, isolated process/handler and actual-DLL configuration tests, not native game playback.
- 旧 U ZIP 的完整性复核已通过，但它缺少上述依赖；使用新 `AF-GPTSOVITS-U_0.1.1-local-evaluation.zip`，不要继续分发旧 0.1.0。最终大小和 SHA 见下面的归档记录。 / The old archive passed integrity checks but lacked the dependency. Use the corrected 0.1.1 archive, not the old 0.1.0; its final receipt follows below.
- 本次不运行 GPU 模型或游戏。此前 RTX 5070 Ti、无游戏并行的九例实测峰值为 2303.5 MiB 专用显存、5176.8 MiB 进程 RSS；不是本次新压测，不能作为 8GB 显卡游戏同开保证。 / Earlier nine-case measurements are retained evidence, not a new benchmark or 8GB gaming guarantee.

**目前没有在此分支提供 U 网盘链接。** 权重与角色录音再分发许可仍未核实；源码发布不会赋予这些资源的分发权。本地 ZIP 保持评估版标识，不应伪装成已经许可的公开音色发行包。

**No U cloud-download link is provided here.** Weight/recording redistribution clearance is still unverified. Publishing adaptation source grants no rights to redistribute those assets; the local ZIP remains an evaluation candidate.

玩家教程 / Player guides: [中文](AF-GPTSOVITS-U.zh-CN.md) · [English](AF-GPTSOVITS-U.en.md). 本次源码验证状态见[主台账](../animusforge-refactoring-and-repository-reorganization-plan.md#gptsovits-u-source-publication-20261021)。 / See the main ledger for validation status of this source publication.