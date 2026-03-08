# AnimusForge
一款集成了LLM的骑马与砍杀2的mod

## 维护提示
当前主聊天链路是场景喊话链路，不走原版直接对话界面。
原版直接对话链路仅保留兼容/回退用途；后续改动请优先检查 `ShoutBehavior` 与 `MyBehavior.BuildShoutPromptContextForExternalInternal(...)`，不要默认把精力投在直接对话系统上。
