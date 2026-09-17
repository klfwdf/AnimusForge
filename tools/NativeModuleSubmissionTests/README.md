# Native 制作组服务与子 MOD API 验证

> 根整合状态（2026-09-16）：生产源码 `6e419f6d` 已本地提交，最终六Stage与4DLL1056元数据通过；整体收尾仍ACTIVE。当前边界以[总交接](../../docs/handoffs/2026-09-16-parallel-closeout-handoff.md)为准，以下包内记录保留原验证上下文。


本包是全范围收尾的 **Native 接口责任**，不是完整三渠道 SDK、完整线程安全改造或 LIVE 验收。生产修改起点 `154f7206`；最终合并提交/构建由根代理记录。测试与生产源使用 `source-review.json` 绑定，不用旧数字冒充新候选。

## 实际运行链

```text
独立 NativeModuleClient 程序集（无 friend 权限）
  AfApi.CreateDialogueClient / AfDialogueClient.SubmitNative
    CoreDialogueClient（内部命名空间、去重/容量）
      ShoutBehavior.SubmitModuleNativeDialogue
        原 RunNativeConversationMainThreadFuncAsync + 原 PendingOperationRegistry
          原 SubmitNativeConversationAdmittedAsync + 原上下文/会话/身份守卫
            fixture 网络正文
              原 ApplyNativeConversationGameActionsOnMainThreadAsync
                原 ExecuteNativeConversationActionDispatch
                  fixture 游戏动作
                    原 CompleteNativeConversationReplyOnMainThread
                      fixture 最底层记忆接受
                        internal receipt → 原 admission finally 释放 → public terminal DTO
```

主线程、admission、action dispatch、required-memory接受判断、回执插入和公开消费者均执行当前真实源码；仅声明的 TaleWorlds 对象、正文 provider、最底层 action/memory/TTS 是 fixture。不读取真实存档，不发真实 LLM，不覆盖游戏。

## 覆盖

- 41项检查：worker入口不读游戏对象；原主线程claim；32个并发重复请求共用一轮；不同payload/不同client ID隔离；容量128不淘汰；开始前取消与已claim TooLate；Dispose只取消自己的未开始请求。
- 原UI busy不被绕过，原UI正常完成仍保持。原方法空返回、错误文案不假成功；部分action/memory失败标UnknownAfterStart且不自动重试；首个真实receipt不被晚错误/取消覆盖。
- save generation、owner替换/退休、排队A→B conversation epoch、已准入后换目标、排队期间另一原UI回合先完成的presentation revision拒绝。
- 内部三组enum故意换成不同数值，完整调用断言仍通过：V1有显式映射而非内部enum cast。
- 原NativeAdmission44 / Presentation46 / Completion184 / PendingHistory111检查保留。前两个历史runner原先漏了既有GameLifetime待办registry依赖，本包仅追加实际源依赖和无状态fixture字段；不修改它们的业务断言。

## 执行

```powershell
$dotnet = (Resolve-Path .\local\dotnet\8.0.425\dotnet.exe).Path
python -X utf8 -B tools/NativeModuleSubmissionTests/run.py --dotnet $dotnet
python -X utf8 -B tools/NativeModuleSubmissionTests/run.py --dotnet $dotnet --reorder-core-enums
python -X utf8 -B tools/NativeModuleSubmissionTests/source_boundary.py
```

`--dotnet` 也可省略并使用 `DOTNET_EXE` 环境变量，最后才查找 PATH 中的 `dotnet`；路径不存在时在生成测试目录前退出。B0/B1 验证必须显式传入同一个本地 SDK 路径。

有效反例命令用 `--mutate`：`ignore-cancel`、`text-success`、`drop-receipt`、`replace-confirmed`、`replay-id`（破坏相同ID的终态复用，不声称是真实provider重复提交）、`skip-generation`、`skip-conversation`、`skip-revision`。只有编译成功后的明确行为断言 FAIL 计有效；工具超时、fixture缺类型、编译失败不计。测试入口catch异常后返回失败码，避免未捕获进程异常影响反例回收。

`.tmp/native-module-api-20260916/*-final.log` 是最终日志；早先 `ignore-cancel`/`text-success` 无final日志因测试异常入口未catch导致工具等待超时，不计证据。一般新fixture不对应main缺陷；epoch/revision为本包peer审查发现并修复的真实排队错目标/跨回合缺陷，专用guard mutation保留回归。

## 精确源码对照

`source_boundary.py` 仅逆变换3个批准改动文件：API增量、admission optional票据、completion尾部receipt；校验新7生产依赖hash。原默认UI入口、动作/记忆算法、Saveable/SyncData未改变。它不是游戏回放或删除旧代码依据。

| 源码位置（一基） | 符号 | 责任 |
|---|---|---|
| `src/modules/AF.Module.PublicApi/V1/AfApi.cs:55` | `public static AfDialogueClient CreateDialogueClient()` | 公共V1创建入口；旧查询/身份不变 |
| `src/modules/AF.Module.PublicApi/V1/AfDialogueClient.cs:53` | `public sealed class AfDialogueClient` | 独立子MOD namespace/submit/取消/结果DTO |
| `src/modules/AF.Module.PublicApi/Internal/AfV1DialogueProjection.cs:7` | `internal static class AfV1DialogueProjection` | 显式外部协议投影，不绑定内部enum数值 |
| `Refactor/Modules/CoreDialogueClient.cs:10` | `internal sealed class CoreDialogueClient` | 每client有界128去重、不同payload拒绝、不静默淘汰 |
| `Refactor/Modules/CoreDialogueOperation.cs:10` | `internal sealed class CoreDialogueOperation` | 单次claim、开始前取消、回执优先 |
| `Refactor/Modules/CoreDialogueServices.cs:7` | `internal static class CoreDialogueServices` | 同DLL内部服务复用真实Native owner |
| `ShoutBehavior.ModuleNativeSubmission.cs:10` | `internal static void SubmitModuleNativeDialogue` | owner/generation/epoch/revision绑定，原队列/准入接线 |
| `ShoutBehavior.NativeAdmission.cs:65` | `private async Task<string> SubmitNativeConversationAdmittedAsync` | optional内部operation记录准入，原UI调用不变 |
| `ShoutBehavior.NativeCompletion.cs:75` | `private string CompleteNativeConversationReplyOnMainThread` | 唯一动作/必要记忆完成尾部记录receipt |

## 仍未完成

- Scene主群组路径目前启动后fire-and-forget，`ProcessCapturedScenePlayerShoutAsync`返回不等于接力/旁听/最终效果完结；不能用已有single-target opt-in充当完整Scene API。
- Courier必须绑定运输session、预生成和到达唯一commit、双方身份/入站出站语义；现有接受internal ports/committer的opt-in不是公共安全入口。
- 主体→制作组既有13port/31call与新增制作组→Native服务是不同覆盖；本包未迁移所有制作组反向调用。
- 当前Native目标由主线程准入时解析；调用时只有owner/save/conversation/presentation stamps，没有后台Hero快照。需要严格‘选择某NPC后后台提交’的未来接口应使用主线程签发的不透明context票据，不能后台读Hero。
- 未运行当前候选真实独立子MOD加载、1.3/1.4游戏Campaign/Mission、旧存档/live Economy/AFEF。本包不切默认入口、不推送、不部署、不恢复自动化。
