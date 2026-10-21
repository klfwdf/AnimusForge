# AF-GPTSOVITS-U — 改造源码分支 / Adaptation source branch

**U 基于 GPT-SoVITS V5 Dev；V2Pro/L 的低显存改造另行保留，不混淆版本。** 本分支新增源码与说明，不包含可直接安装的多 GB 模型包，不更新游戏或远端 main。

**U uses GPT-SoVITS V5 Dev; V2Pro/L adaptations remain separately labelled.** New commits publish source and documentation, not a ready-to-install multi-GB model bundle or a game deployment.

- [源码入口、V2Pro 与 U 改造对照 / Source map and adaptation comparison](docs/tts/AF-GPTSOVITS-U-SOURCE.md)
- [U 构建与测试 / U build and tests](tools/af-gptsovits-u/README.md)
- [中文玩家教程](docs/tts/AF-GPTSOVITS-U.zh-CN.md) · [English player guide](docs/tts/AF-GPTSOVITS-U.en.md)
- [既有 V2Pro/L 改造源码 / Existing V2Pro/L source](tools/af-gptsovits-l/README.md)

---
# AnimusForge
一款集成了LLM的骑马与砍杀2的mod

## 维护提示
当前主聊天链路是场景喊话链路，不走原版直接对话界面。
原版直接对话链路仅保留兼容/回退用途；后续改动请优先检查 `ShoutBehavior` 与 `MyBehavior.BuildShoutPromptContextForExternalInternal(...)`，不要默认把精力投在直接对话系统上。
