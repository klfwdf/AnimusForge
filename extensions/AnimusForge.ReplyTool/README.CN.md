# AnimusForge.ReplyTool 0.1.1

独立 Bannerlord 子模组，通过 Harmony 在内存中包装 AF 的流式/非流式发送委托。发送前将本轮用户文本移到 ID 配对的 `read_user_input` 已完成调用/结果，再声明并强制 `output_reply(content)`；响应中将该回复工具参数还原为 AF 原有正文。AF 的原始 URL、认证、发送器、取消、超时、准入、动作与记忆 owner 保持不变；不修改 AF 源码、DLL、设置或存档，不启动代理，不增加模型请求。

## 使用

运行 `python extensions/AnimusForge.ReplyTool/build.py --game-root "你的骑砍2目录"`。该脚本只在本子模组目录构建、测试和生成干净 Stage，不构建或覆盖 AF。`artifacts/latest-stage.txt` 指向本次 Stage。

从 Stage 的 CN 或 EN 中选择一个 `AnimusForge.ReplyTool` 文件夹，手动复制到游戏 `Modules/`。启动器勾选它，排在 `Bannerlord.Harmony` 与 `AnimusForge` 之后。CN/EN 只选择子模组固定工具提示资源，不强制模型回复语言；建议与 AF 内容包一致。回滚只需取消勾选，或移走本子模组文件夹。

启动接线结果及每次加工的 `request_prepared;input_wrapped=True/False;stream=True/False` 见本子模组 `Logs/ReplyTool.log`。日志不保存请求正文、API Key 或地址。无 MCM/终端设置；启停通过启动器完成。更新 DLL 后需重启游戏进程，已经加载的旧 DLL 不会热更新。

## 范围与行为

- 模型名称包含 `gemini`，接口路径以 `/chat/completions` 结束时启用。自定义别名不含此字符串时不处理；Gemini 原生、Anthropic 原生、Responses、TTS、生图、向量接口不处理。
- 自由对话、信使、场景喊话及其他使用两个共享传输入口的请求同样适用。原有阶段、提示词、JSON/标签及消费者保留，不复制游戏动作或记忆逻辑。
- 已有 `tools/functions/tool_choice/function_call/response_format` 或多候选请求原样放行。内部回复工具只作为文本承载，不执行任何模型工具动作。
- 输入包装只改变最终发送副本中的最新一条 user 消息；之前的历史、system 与原始存档保持。工具结果为 `{"source":"user_input","text":"原文"}`，调用/结果 ID 必须配对；完整的本轮包装不重复嵌套，旧轮包装不影响新轮。多模态只搬移 text 部分，图片留在 user 中；纯图片或未知内容类型不伪造文本。损坏的本轮输入调用/结果显式报错。
- 非流式解析完整参数；流式逐事件聚合工具参数，增量解码 JSON 转义和 Unicode 代理对，状态按请求隔离。支持普通正文兜底；工具正文与普通正文同时出现时优先工具正文。若普通正文已发布后才出现工具，报错，避免重复。
- 不完整 JSON、多工具、未知工具、工具输出达到长度上限或过滤均报错，不假装补全。已显示的流式部分由 AF 原有错误策略处理。HTTP 错误保留原状态和正文；不自动重试、不自动续写。不支持工具的服务可能报 400，停用本子模组即可撤销。
- 每次请求加工一次 JSON；每个 SSE 事件加工一次，参数解码总复杂度 O(n)。没有 Tick IO、轮询、游戏对象后台读取或逐片反射。

## 验证边界

`artifacts/verification.json` 记录双 API 引用版本与哈希、相同共享 DLL、当前已安装 AF DLL 的实际 Harmony 接线/传输测试、源码哈希及 AF DLL 前后哈希。测试使用本地确定性发送器，不调用真实模型。

离线验证不等于实机或上游服务验收；真实工具支持、模型回复完整率和三渠道游戏动作仍需实测。本机制不增加模型输出上限，不能保证消除所有截断。构建命令不部署、不推送；具体安装与真实 API 验证状态以对应收据为准。

## 参考

参考用户提供的源码解析中“工具承载正文”的协议思路，C# 实现独立编写，未复制三个项目的源码。固定版本入口：[RP-Hub](https://github.com/STA1N156/RP-Hub/tree/cd8f2a6d599230e8788a1888a478912c6e83d7fe)、[gcli2api](https://github.com/STA1N156/gcli2api/tree/af32928885c96d6d68656a3f6d3df4ee173b920f)、[AIStudio2API](https://github.com/STA1N156/AIStudio2API/tree/69398d8bd8219a0c0ef9208021c49f25e42a76d6)。
