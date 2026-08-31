# AF framework continuation — 2026-09-01

本轮实际实现框架与接线，不只是撰写规划。阶段 7 保持 VERIFY；阶段 8 仅准备态工具通过，不放行默认切换或破坏性清理。

## Git / Skill / 工作区

- canonical：`G:\AFMOD\AF-REFACTOR`，分支 `codex/af-main-refactor-continuation-20260831`。
- 起点：`49eeaf33`，开始时 clean。两次 fetch 确认远端 `origin/refactor/prepare-af-restructure` 最新为 `fc8c344e0734ee860ec4012fb29b09e61dbdb240`，相对旧 `182da1db` 仅改两份文档。
- `b0cc41da` 登记本轮意图/checkpoint；`2216df41` 正常 merge，保留原本机 5 个提交。没有 reset/rebase/强推或整文件覆盖。
- `D:\qq\af-skill.zip` SHA-256：`CDE1BAA4C069A0E45AB43E63BF377EDA7375A7A88EB5DD6DBFA6A978CB35FF79`。21 个文件与已 fetch Git blob 字节一致；工作树 CRLF 差异不代表来源漂移。只读取资料，未执行 ZIP 脚本或安装全局 Skill。
- NEW-10/GCCZ 未改；没有变更 GCCZ 可复用核心/桥规则、官方构建/覆盖脚本、程序集/模块身份或 save key/type。

## 三组实现与回滚提交

| 提交 | Owner / 实现 | 回滚边界 |
| --- | --- | --- |
| `b6b31bf3` | Test tooling：四 runner 共用明确来源的 replay 依赖框架 | 可单独反向提交；不修改 Stage 或游戏依赖 |
| `e9c41ff9` | Conversation/Memory lifecycle：请求级 receipt 与 Native 提交终态保护 | 可单独反向提交并重建；不是经济存档事务回滚 |
| `6d4269a0` | Validation：阶段 8 只读证据/清理/回滚门禁 | 只影响准备工具与模板，不影响生产入口 |

本 handoff 和公共台账另作本地文档提交。所有新提交尚未推送；源码回滚不撤销已经写入存档的资产/债务/领地副作用。真实测试必须有独立存档与明确重置点。

### 1. 可移植 replay 依赖框架

`tools/ReplayDependencies/` 由 shared targets、metadata-only 复制校验 helper、9-case 自测和 README 组成。四项目不再递归扫描另一台机器的 `F:\SteamLibrary\Modules`。

- 显式指定 GameRoot、固定 1.4 引用、Harmony/MCM/UIExtender 模块根与私有 managed runtime。
- TaleWorlds/SandBox 取固定引用；模块依赖取明确模块，不让旧 overlay MCM/Harmony 无声覆盖当前模块。
- 先检查模块 ID、程序集身份、完整选择计划、SHA256 和输出边界，再复制；同名冲突、递归重复候选、reparse point、缺依赖/参数、非标准输出层级都明确拒绝。
- 每 runner 写 `af-replay-dependencies.json`，包含 Stage hash 和 78 项依赖来源/hash。
- 只证明这四个 managed replay 的依赖边界，不代表 native/ONNX 推理或最终包依赖闭包。

### 2. 请求级提交框架

入口沿用现有 facade → `InteractionResultCommitter` → owner → memory；没有平行 Gateway、ModuleHost 或新默认入口。

- 原内容键忽略请求与 generation：相同文本的新请求可能被误挡，memory 失败后的旧请求又可能重放动作。源码红测与旧生产 DLL 回放均复现。
- 新 key 基于 runtime/save generation、TraceId、channel/session/subject 和已有 Courier direction；不改变 capture/session 生成。
- 动作前 reserve，完成后保留成功或失败终态；同请求 payload 变化、重入被拒绝，非 batch memory 也受保护。
- SHA256 摘要使用长度编码，缓存不保存原始对话；最多 512 项，只淘汰终态，不淘汰正在提交的项。
- duplicate 保留原 status/history/action flags，并标 `IsDuplicate`；跨 Host 不重复执行 `afterCommit`。
- 原公共 Native opt-in runner 在 callback 开始后 throw/null/failure 也不走 legacy fallback；不制造假 memory adapter 来改造其公开接口。
- 拒绝动作的 historyWritten 现在取决于实际 memory port 结果，不再无条件报告 true。

详见 `G:\AFMOD\AF-REFACTOR\docs\animusforge-request-commit-receipts.md`。

### 3. 阶段 8 证据门禁

`tools/PhaseEightReadiness/` 复用既有 8-ID 设计目录、两组 Bridge 和完整 18-case Composition 契约。检查 OFFLINE/LIVE/SAVE/RELEASE 分层、双 API、owner 审核、当前 Git/clean、附件/产物哈希、版本和时效，以及清理候选与确切回滚目标。

- 正向合成数据只得到 `FIXTURE-VALID`；真实材料齐备也仅到 `READY-FOR-OWNER-REVIEW`。
- `fullProjectReleaseReady=false`，delete/defaultSwitch/deploy/push/publish 授权始终 false。
- 不执行清单命令、不读取实际存档内容、不启动或部署游戏、不删除候选；哈希不证明人工证据真实性。
- 初版 8-ID 不是完整 20 领域签收，不替代实际 Campaign/Mission/旧档/AFEF、包 allowlist 和 owner 审查。

## 实际验证与日志

统一日志根：`G:\AFMOD\.build-cache\af-framework-20260901`。

| 验证 | 结果 |
| --- | --- |
| Debug 1.3 / 1.4 / Bootstrap Stage | 各 0 warning / 0 error；`stage.log` |
| Release 1.3 / 1.4 / Bootstrap Stage | 各 0 warning / 0 error；`release-stage.log` |
| InteractionPipeline | 原 40 cases + Host 48 + Native callback 4 + request receipt 38 PASS；`final-interaction.log` |
| ProductionConfiguredHost | 原三渠道 + 12 个提交后故障 + 6 个重建 committer 用例 PASS；`new-stage-ProductionConfiguredHostReplayTests.log` |
| Policy / WorldDiplomacy / TTS / ProductionOptIn | 四 runner 在新 Debug 1.4 Stage 全部 PASS；`new-stage-<Runner>.log` |
| ProductionEconomyAwareCommit / ProductionEconomyOwner | PASS；mixed/economy-only receipt 与 owner factory fail-closed |
| ProductionDetachedHost / ProductionCourierHost / ProductionValidationProvider | PASS；现有可控 Host/provider 测试，不是实际游戏 |
| ConfiguredChatGateway / EconomyRewardDebtPort / EconomyAwareActionPlanExecutor | PASS |
| Persistence/Profile/Config | PASS：95 keys / 121 bindings / 8 types；未改审计范围 |
| Persistence identity | PASS：sync=99 / behavior=35 / module=AnimusForge；`persistence-identity.log` |
| Dependency helper | 新 Stage 上 9/9 PASS；两项 MSBuild 参数/输出拒绝检查 PASS |
| PhaseEightReadiness | 主代理与子任务各重跑 44 tests PASS；`root-phase8-selftest.log` |
| 原 Bridge / Composition fixture | 10 cases/6 invariants 与 18 cases/24 invariants PASS |
| 实际 all-missing 门禁 | `BLOCKED / exit 2 / 0 accepted evidence`；`phase8-current-report.json`，未伪造 LIVE/SAVE PASS |

红测记录：`receipt-red.log`、`native-overload-red.log`、`production-receipt-red.log`，均在修复前失败，之后对应源码/生产回放通过。四 runner 的旧依赖加载失败另见 `before-<Runner>.log`。

聚焦 conflict/TODO/HACK/TEMP/旧内容键/旧 F 盘复制搜索、Python/JSON/路径/哈希检查、`git diff --check` 通过。保留仍有调用的 Legacy facade、memory append guard 和提交前 fallback；删除的是已被替代的内容去重/重复复制/提交后 fallback 旧路径，没有做阶段 8 破坏性清理。

## 本机重现

使用 SDK `C:\Users\28358\AppData\Local\Microsoft\dotnet\dotnet.exe`；CLI 状态放 `G:\AFMOD\.build-cache\af-refactor-cli`，NuGet 包使用 `G:\AFMOD\NEW-10\.dotnet_cli\.nuget\packages`，禁用 CLI telemetry。

```powershell
Set-Location -LiteralPath 'G:\AFMOD\AF-REFACTOR'
$env:DOTNET_ROOT = 'C:\Users\28358\AppData\Local\Microsoft\dotnet'
$env:PATH = $env:DOTNET_ROOT + ';' + $env:PATH
$env:DOTNET_CLI_HOME = 'G:\AFMOD\.build-cache\af-refactor-cli'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:NUGET_PACKAGES = 'G:\AFMOD\NEW-10\.dotnet_cli\.nuget\packages'

# 本轮分别以 Debug / Release 执行；只做 project-local Stage。
powershell -NoProfile -ExecutionPolicy Bypass -File .\一键编译覆盖推送\build_single_module.ps1 `
  -ProjectRoot 'G:\AFMOD\AF-REFACTOR' `
  -BannerlordRoot 'E:\steam\steamapps\common\Mount & Blade II Bannerlord' `
  -Bannerlord13ReferenceDir 'G:\AFMOD\NEW-10\_deps_auto' `
  -Bannerlord14ReferenceDir 'G:\AFMOD\NEW-10\.tmp\build_check\1.4' `
  -WorkshopContentDir 'E:\steam\steamapps\workshop\content\261550' `
  -RuntimeDependencyDir 'G:\AFMOD\NEW-10\AnimusForge\bin\Win64_Shipping_Client' `
  -Configuration Debug -Stage

dotnet run --project .\tools\InteractionPipelineContractTests\InteractionPipelineContractTests.csproj
dotnet run --project .\tools\ProductionConfiguredHostReplayTests\ProductionConfiguredHostReplayTests.csproj
python -B -m unittest discover -s .\tools\PhaseEightReadiness -p 'test_*.py' -v
python -B .\tools\PhaseEightReadiness\readiness.py --project-root 'G:\AFMOD\AF-REFACTOR' `
  --manifest 'G:\AFMOD\AF-REFACTOR\docs\phase8\all-missing.evidence.json'
# 最后一项预期 exit 2；其余命令单独检查 LASTEXITCODE，不用后一个命令掩盖失败。
```

四个修复后的 runner 必须带六个明确 MSBuild 参数；完整命令与 helper 自测在 `G:\AFMOD\AF-REFACTOR\tools\ReplayDependencies\README.md`。不要用 `--no-build` 跳过依赖验证。其他已执行 runner 按同名 csproj 运行。

| 配置 / 产物 | ReferenceGameVersion | SHA-256 |
| --- | --- | --- |
| Debug Bootstrap | v1.3.15.110062 | `D6857554CAA4BECEA75B2CA32D4BDB2BBB14454EEE1D3FD9F4708249C05ACF01` |
| Debug 1.3 | v1.3.15.110062 | `84E29E3D405209CCB9213D150BBE2EBC261695C8265B9BBF6E87B7FA1DAC1460` |
| Debug 1.4 | v1.4.6.115628 | `BADC5EAD13631A53C313DB4CA6FFC54DFF8A5FB9427331A5BEC353C64B2481C1` |
| Release Bootstrap | v1.3.15.110062 | `3310BE7E337E78B3977107D05E9D6088A5EB49E99D9319FB938D97861AE48634` |
| Release 1.3 | v1.3.15.110062 | `01D33BF0D504593A27FF2E9E9282DE3EF71EF0CD5CCA8A26A100A746997299CD` |
| Release 1.4 | v1.4.6.115628 | `508F129AC4402F75BB0AC0B3808ACCDD1A92C6F308FA3E663C69606B31736085` |

四 runner 的新 manifest 已逐项确认绑定上述 Debug 1.4 hash，而不是旧的 `5FDA2138...` Stage。

## 不能声称已经正常的部分 / 下一任务

1. `LOCAL-7-D`：旧 `MyBehavior.AppendExternal*` 返回 void，可能吞错/缺 Behavior 时 no-op。当前 facade 的 Applied 不能证明 AFEF 真写入；下一步需要真实 owner result、读回与实机证据。
2. `LOCAL-7-E`：Courier economy-only 可能绕开后面的 session/delivery/terminal/消费状态检查。必须在经济副作用前补齐共同守卫和持久消费边界，再验证存读档；当前缓存不替代该规则。
3. Mixed Economy/legacy 可以部分成功后拒绝；自动补写 memory、补执行 afterCommit、经济补偿/恢复尚未实现。不得重放整套动作或伪造成功事实。
4. 512 项 process-local receipt 有淘汰窗口，进程重启/读档后不构成持久 exactly-once。旧 facade、capture、存档与领域权威继续保留。
5. 真实 Campaign/Mission、live 金币/库存/债务、AFEF、旧档未运行；新 Stage 未覆盖游戏。已询问备份后部署并使用独立新战役的授权，尚未收到明确回复；旧存档不动。
6. `.NET 10` 工具、独立 Xihai net472 runtime、真实外部 provider、最终 ZIP/package/安装/全部 20 领域组合仍未验收。本轮 Release Stage 通过不等于最终发布包通过。

本轮没有远端推送、全局 Skill 安装、默认切换、游戏启动/部署或原版文件修改。接续先做 D/E 两个真实 owner 边界，再按明确授权进入真实 Host；阶段 7 不标 DONE，阶段 8 执行仍 BLOCKED。
