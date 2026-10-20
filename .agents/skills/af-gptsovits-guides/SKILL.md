---
name: af-gptsovits-guides
description: Maintain AF-GPTSOVITS L/U player tutorials, custom voice-pack requirements and their bilingual MCM/terminal help. Use when releasing or updating an AF speech package or its guide, not for unrelated gameplay or general TTS research.
metadata:
  version: "1.0.0"
---

# AF-GPTSOVITS 教程维护

在当前 AF 仓库维护玩家教程与游戏内入口，不安装全局 Skill、不自动发布或覆盖游戏。继承仓库 AGENTS 的双语、构建和玩家数据保护要求。

## 从实际版本开始

- 核实当前分支和请求的 L/U 版本；用 `runtime.json`、运行脚本、音色 reader 和实测证据确认功能。文件名相同不证明 ZIP 内容相同；不要把本地校验值套到网盘文件。
- L 的玩家文档：[`docs/tts/AF-GPTSOVITS-L.zh-CN.md`](../../../docs/tts/AF-GPTSOVITS-L.zh-CN.md)、[English](../../../docs/tts/AF-GPTSOVITS-L.en.md)。它们是当前版本说明，不是对未来版本的承诺。
- 新 U 全量版必须另写 `AF-GPTSOVITS-U.zh-CN.md` 和 `.en.md`：独立的下载、版本、安装结构、能力差异、显存/内存实测、兼容和排错。不得复制 L 的 2GiB 目标或 8GB 建议作为 U 的验收结论，也不得在尚未发布时捏造 U 链接。

## 教程必须回答

1. 下载文件、版本、链接、提取码、可信来源；明确哪些远端内容实际核验过。
2. 加到现有 `Modules/AnimusForge` 的准确目录、避免双层套目录、保留游戏 DLL/配置/自定义声音；区分便携包、原版训练环境和远端 API。
3. 实际 MCM 入口、等待就绪、试音、角色百科选声、默认男女池和非随机 Custom、主对话对象/战斗规则、刷新/退出。
4. 分开说明硬盘、RAM、VRAM；目标、采样测量和推荐值不能互换。测量附硬件、采样方法、场景、是否同开游戏；8GB 同开未经测量就不能保证，PyTorch allocated 不是进程总显存。
5. 角色参考音频、准确文本、参考语言、实际 SHA-256、稳定 ID、分类/路径、语气变体、授权与兼容范围。参考包不等于任意训练权重即插即用。
6. 按实际失败原因排错；不引导用户关安全校验、贴密钥/私聊、整目录覆盖或默默切付费服务。

## 核对真实实现

- `src/modules/AF.Module.Llm/Tts/GptSoVitsVoiceCatalog.cs`：扫描深度、字段、文件大小/数量、路径/哈希、默认/自定义池；[可编辑示例](../../../docs/tts/manifest.example.json)的占位哈希必须替换。
- `GptSoVitsRuntimePackage.cs`、`src/AF.GameAdapter.Bannerlord/Scene/GptSoVitsManagedRuntime.cs` 和目标包源码：自动识别、L/U 优先级、兼容和生命周期。不要因写教程改变 provider 索引或保存身份。
- `src/AF.GameAdapter.Bannerlord/Configuration/Mcm/DuelSettings.TtsLiteGuide.cs`、`TtsSettingsVisibility.cs`、`src/AF.GameAdapter.Bannerlord/Scene/GptSoVitsLiteGuide.cs` 与中英 `afllm_ui_strings`：保持同一 owner 的 MCM/终端入口、真实显隐与本地化。未来 U 接入独立 U 章节，不把 L 章节改名伪装。
- 教程点击只展示说明，不下载、不启动模型、不写配置；教程显隐不得给原有服务切换增加额外副作用。实际启停与保存仍沿用服务 owner。新入口按现有回调刷新，不能新增 Tick 扫盘。

## 验证和交付

- 同步 CN/EN 文档、真实 MCM 资源/fallback、示例和读者；检查链接、字段与数字。保留旧版 L 的可追溯说明。
- 用 `TerminalLiveSettingsTests` 的实际 MCM/终端测试验证两种语言、仅对应服务显示、切换/撤销/重开不污染其他选项、章节动作绑定原 owner。新增读取规则才扩展实际 catalog 测试。
- 只有文档/Skill 更新不运行游戏构建；代码/嵌入资源改动按原双 API/Bootstrap 流程。游戏截图、试听、旧档、显存压测与离线回放分别报告。
- 发布与安装以本轮用户授权为准。公开仓库只发源码/说明/合法示例；模型、私人日志、凭据和未核实可分发录音不随教程上传。安装先确认最新已装来源，只合入本次差异，保护 GCCZ 等其他制作组的功能。
- 唯一主台账记结果，HANDOFF 简述入口与未验项；本 Skill 不维护竞争的进度台账。
