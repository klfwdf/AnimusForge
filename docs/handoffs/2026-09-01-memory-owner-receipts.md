# LOCAL-7-D：真实 Memory owner 回执接续

## 结果与边界

- canonical worktree：`G:\AFMOD\AF-REFACTOR`，分支 `codex/af-main-refactor-continuation-20260831`。
- 接手 HEAD `9b8cb509`，意图 checkpoint `3fe3f656`；fetch 后远端仍 `fc8c344e`，没有待整合提交。
- 实现、测试、owner 文档与公共台账提交：`5d3dc5f0`；本 handoff 另作本地文档提交。源码回滚以 `5d3dc5f0` 的审阅后 inverse commit 为单位，保留 checkpoint 和全部历史，不 hard reset。
- `LOCAL-7-D` 的 batch runtime owner 结果代码已实现；阶段 7 仍 VERIFY。运行时接受不是磁盘持久化，也不是原子事务或实机验收。
- 本轮未推送、未部署/启动游戏、未读取或改写玩家存档、未切换默认入口、未改全局 Skill/构建脚本。NEW-10/GCCZ 保持 clean；没有 GCCZ 核心或桥规则变化，故不复制 AF 主体到 GCCZ。

## 具体变更

Owner：Memory/Persistence runtime acceptance，沿用现有 Conversation batch facade。

1. `MyBehavior.CommitExternalDialogueHistory` 在 live lookup 前检查主线程，必须取得当前 Campaign 的 Behavior，不回退静态 Instance；检查身份、空提交和 Hero 资格。
2. 原内部 `AppendDialogueHistory`、`AppendDialogueHistoryById`、`AppendDailyMemoryLineById` 返回真实接受结果，保留原 player → AFEF → assistant 顺序和异常兼容边界。
3. Daily 在 sanitizer 后确认 raw owner 字典中的 owner/day、本次 draft/line 引用；recent 确认发布后的列表引用。不能用 prompt 过滤后的 Read、旧同文本行或行数增加推断成功。
4. `MyBehaviorMemoryFacade.Commit` 仅在 owner 确认时写 success receipt；删除无条件 Applied、从 void 调用推断成功和“失败可直接重试”的旧分支/注释。
5. 4 个公开 void API 与旧 non-batch `Append` 因活跃兼容调用保留。未新增平行记忆管线，未改变 SyncData key/type、存档身份、保留窗口、主线程调度或 default channel flags。
6. `tools/ProductionOptInEntryReplayTests` 增加真实 DLL 缺 owner 回归、线程 guard fixture、void 签名和 raw-owner/sanitizer fixture。没有 fake 游戏 DLL，也没有初始化 Campaign。

性能：仅 append/commit 边界运行，Hero 优先直接 ID 查找；读回只在已经被 sanitizer 遍历的单 owner 草稿/行列表内检查，无新 Tick、后台队列或全世界扫描。

## 实际验证

日志：`G:\AFMOD\AF-REFACTOR\.tmp\validation\memory-owner-20260901-0504`（本机忽略的验证产物，不进入客户端包）。

| 检查 | 结果 |
| --- | --- |
| 旧生产 DLL 红测 | `NativeConversation/missing Campaign falsely acknowledged history: Applied`，证据 `production-owner-red.log` |
| 最终 Debug 1.4 生产 owner 回放 | 缺 Campaign 7、无虚假 receipt 7、线程 guard fixture 2、void compatibility 4，PASS |
| Raw owner readback fixture | 11 assertions PASS；包括错 owner/day、同文不同引用、sanitize 丢弃、260 行发布；非真实 Campaign |
| Interaction contract | 原 40 + Host 48 + Native callback 4 + request receipt 38 cases，PASS |
| ProductionConfigured/Detached/Courier Host | PASS；Configured 包含 12 个提交后故障、6 个重建 committer 用例 |
| Economy-aware executor / EconomyRewardDebt port | PASS；仍为契约/受控 owner，不是 live 资产验证 |
| Persistence/Profile/Config | PASS：95 keys / 121 bindings / 8 types；只修正 `_patienceStates_v1` 两条导航行号 37010/37019 → 37079/37088，key/ref/type/source 不变 |
| PersistenceIdentityAudit | PASS：99 SyncData signatures / 35 Campaign behaviors / 单模块 Bootstrap-only，原始基线 `d4cb1467` |
| Debug 与 Release 的 1.3/1.4/Bootstrap | 六项全部 0 warning / 0 error，project-local Stage success |
| Cleanup / diff / 独立代码审查 | 无冲突标记、无新增废旧平行路径，`git diff --check` PASS；独立审查无阻断项 |
| 真实 Campaign/Mission、AFEF、live Economy、旧档 | NOT-RUN：尚无授权部署和隔离存档的实机验收条件 |

生产 runner manifest 的 ImplementationSha256 已与最终 Debug 1.4 完整核对，未用第一轮中间 Stage 冒充最终产物。

| 最终产物 | SHA-256 |
| --- | --- |
| Debug Bootstrap | `D5D0A06729C7156DF6F26382F60E7614CF4C20B6D73D4C8D8E8971D34E053CE8` |
| Debug 1.3 | `37B59E3DA3ECE67FD689B08FCE08AD870E08EBBA7D9BB38A340186D501D432EF` |
| Debug 1.4 | `D2873A356864071B7EC7FD4A01074AD6E09372D9944B032FA426342AD25313F9` |
| Release Bootstrap | `17173048E43A3AB7FD3A317BF83B564D17D5EFAB2494A200D0241A5462535821` |
| Release 1.3 | `701AD4E9D1A55E91EAC60B887A6B555E0E43493CE5B276A23ED177C2ABD37332` |
| Release 1.4 | `35372485EC38229BFA0B6E0CDD8ACCEB3B0CA755A0760F4ECCB09C1B52C19319` |

## 重现

沿用 `G:\AFMOD\AF-REFACTOR\docs\handoffs\2026-09-01-framework-continuation.md` 的 SDK 环境与官方 `build_single_module.ps1 -Stage` 命令，分别运行 Debug / Release。固定引用仍为 1.3 `v1.3.15.110062` 和 1.4 `v1.4.6.115628`。不得覆盖游戏或放宽依赖校验。

```powershell
# 先按上份 handoff 设置 DOTNET_ROOT/PATH/CLI_HOME/NUGET_PACKAGES。
dotnet run --project .\tools\ProductionOptInEntryReplayTests\ProductionOptInEntryReplayTests.csproj `
  '-p:GameRoot=E:\steam\steamapps\common\Mount & Blade II Bannerlord' `
  '-p:Bannerlord14ReferencePath=G:\AFMOD\NEW-10\.tmp\build_check\1.4' `
  '-p:ReplayHarmonyModulePath=E:\steam\steamapps\workshop\content\261550\2859188632' `
  '-p:ReplayMcmModulePath=E:\steam\steamapps\workshop\content\261550\2859238197' `
  '-p:ReplayUiExtenderModulePath=E:\steam\steamapps\workshop\content\261550\2859222409' `
  '-p:ReplayPrivateRuntimePath=G:\AFMOD\NEW-10\AnimusForge\bin\Win64_Shipping_Client'
dotnet run --project .\tools\InteractionPipelineContractTests\InteractionPipelineContractTests.csproj
python -B .\tools\PersistenceProfileConfigContractTests\validate_persistence_profile_config.py
python -B .\tools\PersistenceIdentityAudit.py
```

其余 Host/Economy runner 按上述表格同名 csproj 运行；每条命令分别检查退出码。线程测试仅在独立无 Campaign 进程中临时模拟主线程 ID 不匹配并 finally 恢复；默认 managed driver 在所有线程返回 0，所以这不能冒充真实游戏调度证明。

## 未完成与下一精确任务

1. **下一轮优先 LOCAL-7-E**：从 Courier session/delivery/terminal/PostprocessConsumed 的真实 owner 入手，在 economy-only 副作用前验证和消费；保留存读档兼容，不能用 process-local receipt 替代持久业务守卫。
2. D 仍需真实 Campaign 下 Hero/非 Hero 的 user/assistant/AFEF 写入、读回与旧档往返证据。Daily 已原地修改或消费 pending weekly triggers 后仍可能失败，不可自动整套重试或反向删除假装回滚。
3. Detached Scene 的 session ID 传递（当前仍使用 -1）、旧 non-batch void Append、异步摘要主线程检查另列后续；不将本轮 batch 修复夸大为所有记忆入口完成。
4. Partial Economy → legacy 拒绝、memory/afterCommit 恢复仍未完成。缓存有界、可淘汰、不可跨重启/读档保证 exactly-once。
5. 阶段 8 仅准备；不得删除活跃 facade、切默认入口、最终发布或绕过真实验收门禁。回滚以小范围 inverse commit 为准，代码回滚不撤销已经发生的游戏副作用。
