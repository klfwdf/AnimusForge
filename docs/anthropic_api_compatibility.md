# Anthropic / DeepSeek Anthropic API 兼容说明

本文记录 AnimusForge 对 Anthropic Messages API 形状的兼容层。目标是让主 API、前处理 API、后处理 API、事件与叛乱 API 这四个端口在保留 OpenAI Chat Completions 兼容行为的同时，也能填写 Anthropic 风格的 base URL。

## 支持的地址

OpenAI 兼容地址继续按原逻辑处理：

```text
https://api.openai.com/v1
https://api.deepseek.com
https://api.deepseek.com/v1
```

Anthropic 兼容地址会自动请求 `v1/messages`：

```text
https://api.anthropic.com
https://api.anthropic.com/v1
https://api.anthropic.com/v1/messages
https://api.deepseek.com/anthropic
https://api.deepseek.com/anthropic/v1
https://api.deepseek.com/anthropic/v1/messages
```

例如填写：

```text
https://api.deepseek.com/anthropic
```

实际聊天请求会发送到：

```text
https://api.deepseek.com/anthropic/v1/messages
```

## 已接入的请求体

以下请求体已经统一经过 `LlmApiCompat` 处理：

- `DuelSettings.cs`：MCM 设置页四个 API 的连接测试、模型列表拉取。
- `AIConfigHandler.cs`：前处理规则路由、辅助简易对话、后处理动作标签调用。
- `ShoutNetwork.cs`：自由对话/场景喊话主回复，含非流式和流式。
- `MyBehavior.cs`：通用主/辅助/事件 API 调用，覆盖记忆压缩、周报、事件、叛乱命名等链路。
- `KnowledgeLibraryBehavior.cs`：知识库短文本/原型生成时的 LLM 请求。
- `ModOnboardingBehavior.cs`：首次引导和 API 修复流程里的连接测试、模型列表拉取。

`TtsEngine.cs` 的火山 TTS 请求不是 LLM Chat 请求，不走本兼容层。

## 转换规则

OpenAI Chat Completions 请求体会在发送前转换为 Anthropic Messages 请求体：

- `messages` 中的 `system` / `developer` 消息会合并到顶层 `system`。
- `user` / `assistant` 消息保留为 Anthropic `messages`。
- 连续同角色消息会合并，避免 Anthropic 对消息顺序更严格时失败。
- `max_tokens`、`temperature`、`top_p`、`stream` 会保留；Anthropic 温度会限制到 `0..1`。
- OpenAI-only 字段不会传给 Anthropic。
- 小 `max_tokens` 测试请求会自动不发送 thinking，避免连接测试被思维预算规则拒绝。
- 启用 thinking 且 `max_tokens >= 2048` 时，会把旧的 effort 设置转换成 Anthropic `thinking.budget_tokens`。

响应解析也统一兼容：

- OpenAI：`choices[0].message.content`、`choices[0].delta.content`。
- Anthropic 非流式：`content[].text`。
- Anthropic 流式：`content_block_delta` 中的 `delta.text`。
- 已有 Gemini 风格候选文本解析保留。

## 鉴权规则

普通 OpenAI 兼容端口使用：

```text
Authorization: Bearer <key>
```

官方 Anthropic 端口使用：

```text
x-api-key: <key>
anthropic-version: 2023-06-01
```

第三方 `/anthropic` 兼容端口使用：

```text
x-api-key: <key>
anthropic-version: 2023-06-01
Authorization: Bearer <key>
```

这样可以覆盖 DeepSeek Anthropic 兼容入口和多数 Anthropic-compatible 中转。

## 配置建议

DeepSeek OpenAI 兼容入口仍建议优先使用：

```text
API 地址：https://api.deepseek.com
模型名称：deepseek-v4-flash 或 deepseek-v4-pro
```

如果确实要测试 DeepSeek Anthropic 入口，则填写：

```text
API 地址：https://api.deepseek.com/anthropic
模型名称：对应 DeepSeek 文档里的 Anthropic 兼容模型名
```

模型列表拉取会尝试同风格 `/models`。如果某个 Anthropic-compatible 服务不提供模型列表，手动填写模型名即可。

## 验证记录

本次改动后已执行：

```powershell
dotnet build AnimusForge.csproj -c Debug /p:BannerlordApi=1.3 /p:BannerlordRoot="F:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord" --no-restore
dotnet build AnimusForge.csproj -c Debug /p:BannerlordApi=1.4 /p:BannerlordRoot="F:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord" --no-restore
```

验证结果：两个版本均通过。

游戏内建议测试四类配置：

- 主 API 连接测试。
- 前处理 API 连接测试和一次规则路由。
- 后处理 API 连接测试和一次动作标签输出。
- 事件与叛乱 API 连接测试和一次短周报/命名生成。

参考接口文档：

- Anthropic Messages API: https://docs.anthropic.com/en/api/messages
- Anthropic streaming: https://docs.anthropic.com/en/docs/build-with-claude/streaming
- DeepSeek Anthropic API guide: https://api-docs.deepseek.com/guides/anthropic_api
