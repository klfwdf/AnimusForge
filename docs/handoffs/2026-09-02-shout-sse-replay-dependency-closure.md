# LOCAL-7-C2：ShoutNetwork SSE replay dependency closure 接续

日期：2026-09-02。工作区：`G:\AFMOD\AF-REFACTOR`。分支：
`codex/af-main-refactor-continuation-20260831`。

> 2026-09-02 收尾：用户已明确授权关闭自动化并普通push GitHub。本文件中“未push”描述的是
> C2实现/验证切片结束时的边界；最终远端同步状态与制作组接续入口见
> `docs/handoffs/2026-09-02-github-publish-and-team-handoff.md`。

## 结论

- `LOCAL-7-C2` 已完成代码与离线验证，状态为 **VERIFY**。
- 基线 `8bf0c1e4`，意图checkpoint `28ad96f2`，实现提交 `ae49e3c8`。
- `ShoutNetworkSseReplayTests` 已从机器绑定的F盘/全Modules复制，切换到现有显式
  `BannerlordReplayDependencies.targets`；五个managed replay consumer现在使用同一依赖owner边界。
- 新source-only契约锁定五consumer inventory、net8.0、exact shared Import，并拒绝consumer-local
  copy target、机器绝对盘符、`Modules/**` / `Workshop/**`递归扫描和`AnimusForge.dll`复制。
- `Program.cs`、shared copy helper、生产C#、Stage、官方构建/部署脚本、游戏和存档均未修改。
- 没有push、部署、游戏覆盖、default cutover、NEW-10/GCCZ写入或QQ发送。

## 决定性旧缺口与红证据

旧`tools/ShoutNetworkSseReplayTests/ShoutNetworkSseReplayTests.csproj`自定义
`CopyBannerlordRuntimeForReplay`，直接读取：

```text
F:\SteamLibrary\...\bin\Win64_Shipping_Client\*.dll
F:\SteamLibrary\...\Modules\**\bin\Win64_Shipping_Client\*.dll
```

这绕过了`LOCAL-7-C`已有的显式GameRoot/reference、精确Harmony/MCM/UIExtender模块ID、private
runtime owner、程序集identity、输出边界和manifest校验。

有效红证据：

`G:\AFMOD\AF-REFACTOR\.tmp\validation\shout-sse-dependency-c2-red-20260902-031258`

红契约先让前四个consumer通过，再以
`REPLAY_PROJECT_SHARED_TARGETS: ShoutNetworkSseReplayTests must import ... exactly once`
失败。后续门禁还会独立拒绝旧local target、绝对盘符和递归Modules glob。

首次红测脚本在严格模式下直接访问不存在的XML`Target`属性，提前失败；已改为XPath后重新采集
有效红证据。无效目录`...031229`不计PASS或有效RED。

## 本轮实现与Removed

### 最小接线

`ShoutNetworkSseReplayTests.csproj`只做一个替换：删除完整local copy target，新增：

```xml
<Import Project="..\ReplayDependencies\BannerlordReplayDependencies.targets" />
```

没有并行保留旧target，没有新增PackageReference，没有修改SSE业务断言。

### Source consumer contract

新增`tools/ReplayDependencies/Test-ReplayProjectBoundary.ps1`：

- 固定盘点Policy、WorldDiplomacy、TTS、ProductionOptIn、ShoutNetwork SSE五个consumer；
- 每项必须是`net8.0`并恰好导入一次shared targets；
- consumer源码不得包含local dependency copy target、rooted drive、Modules/Workshop递归glob或
  `AnimusForge.dll`copy；
- 不读取游戏、模块、Stage、网络或存档，可作为机器无关的第一层红/绿契约。

`tools/ReplayDependencies/README.md`同步登记第五consumer、source contract和本机安全复现路径；
NuGet cache改为共享`G:\AFMOD\.dotnet_cli`，不再暗示向NEW-10写包缓存。

Removed：旧F盘runtime glob、`Modules\**`全树glob、`BannerlordRuntimeDll` / `BannerlordModuleDll`
items和consumer-local Copy target已全部删除；没有新增dead/orphan/duplicate path。

## 验证

最终证据目录：

`G:\AFMOD\AF-REFACTOR\.tmp\validation\shout-sse-dependency-c2-final-20260902-031458`

结果：

- source consumer boundary：5/5 PASS。
- 既有ReplayDependencies helper：9/9 PASS（missing reference、wrong module ID、output boundary、
  missing assembly、identity mismatch、空格/非ASCII、幂等、output conflict、nested duplicate）。
- 无显式properties负测：预期非0，并在shared targets以
  `Replay dependencies require explicit -p:GameRoot` fail-closed。
- Shout SSE Debug runner：PASS。
- Shout SSE Release runner：PASS。
- 两轮均覆盖success、thinking→plain retry、cancellation、stale generation、delta/final parity和
  ACTION isolation。
- 两份dependency manifest各78个唯一managed依赖，manifest SHA-256完全一致；每项含owner、绝对
  source path、程序集identity/version和SHA-256。
- 未复制`AnimusForge.dll`、native `onnxruntime.dll`或ONNX模型。
- independent final review：P0=0 / P1=0；targeted cleanup与`git diff --check` PASS。

核心命令：

```powershell
$dotnet = 'G:\AFMOD\.dotnet-sdk\dotnet.exe'
$winps = "$env:SystemRoot\System32\WindowsPowerShell\v1.0\powershell.exe"
$props = @(
  '-p:GameRoot=E:\steam\steamapps\common\Mount & Blade II Bannerlord',
  '-p:Bannerlord14ReferencePath=G:\AFMOD\NEW-10\.tmp\build_check\1.4',
  '-p:ReplayHarmonyModulePath=E:\steam\steamapps\workshop\content\261550\2859188632',
  '-p:ReplayMcmModulePath=E:\steam\steamapps\workshop\content\261550\2859238197',
  '-p:ReplayUiExtenderModulePath=E:\steam\steamapps\workshop\content\261550\2859222409',
  '-p:ReplayPrivateRuntimePath=G:\AFMOD\NEW-10\AnimusForge\bin\Win64_Shipping_Client')

& $winps -NoProfile -NonInteractive -ExecutionPolicy Bypass -File `
  .\tools\ReplayDependencies\Test-ReplayProjectBoundary.ps1
& $dotnet run --project .\tools\ShoutNetworkSseReplayTests\ShoutNetworkSseReplayTests.csproj -c Debug @props
& $dotnet run --project .\tools\ShoutNetworkSseReplayTests\ShoutNetworkSseReplayTests.csproj -c Release @props
```

helper 9-case完整命令和所有log/hash见最终证据目录的`manifest.md`与`hashes.json`。

## 产物身份

| 产物 | SHA-256 | MVID |
| --- | --- | --- |
| Debug runner | `059E07B789389E79F8959B7882F067E4AFEF6E2DCBCA2E2D6EC5E28FFADDFDF8` | `f710ddf2-edef-451a-bef4-388ca040c0d4` |
| Release runner | `C7C586BD08E3839AB7CD3FBA28FCADF24EFE2D134BA18E8A0AEFE89E51D7891D` | `d40ea4dc-6821-45e6-bd11-e588201ae210` |
| Debug AF 1.4 Stage | `D806B988B51BF0532617F381D1287A3FD95C3989914184B0F6C5177D1A87B8FF` | `337e6131-fc69-4f31-a86b-ced3e1a65acd` |

Debug/Release dependency manifest SHA-256均为：
`67A5DE630580707B0D4BD4AD607CD854363D2A9B9DD3A8C8D884808C24BBD2A7`。

生产C#自`b93f93df`后未变化；所用Stage即M2 fresh Debug 1.4产物。这里的Release只证明
**Release配置的runner加载同一Debug AF Stage后通过**，不是Release AnimusForge构建或回放证据。

## 限制 / 未验证

- shared helper只拒绝selected dependency的同名冲突/嵌套重复，不会识别所有未选中stale DLL；
  `Program.cs`会递归搜索runner AppBase。本轮迁移前Debug/Release `bin`和`obj`均不存在，因此本轮
  证据为clean output；其他机器必须先审计并可逆备份旧runner output，不能静默信任或删除旧通配产物。
- 78项manifest证明本次SSE路径的显式managed加载闭包，不是native/ONNX/未来代码路径的全闭包。
- 本切片没有生产C#变更，所以1.3/1.4/Bootstrap六Stage N/A；没有重复运行无关生产矩阵。
- 真实Bannerlord Campaign/Mission、旧档、live Economy/AFEF/Duel等均不适用于本工具切片，阶段7
  的对应门禁仍为NOT-RUN。
- 阶段7保持VERIFY；阶段8破坏性清理、default cutover、部署和发布继续BLOCKED。

## 回滚

- 实现回滚：`git revert ae49e3c8`；台账同时退回ACTIVE。
- 若完全放弃C2，再单独`git revert 28ad96f2`。基线为`8bf0c1e4`。
- 文档/HANDOFF提交应单独revert；禁止hard reset、rebase或force push。
- 源码回滚不会清理ignored runner `bin/obj`或验证目录；回滚后这些产物全部视为stale，先可逆
  隔离项目本地output，再从回滚源码重新build/run。
- 本轮没有游戏、NEW-10、GCCZ或远端写入，无外部恢复动作。

## 下一精确任务

`LOCAL-7-C3 / LiveHostReadinessAudit explicit-root portability`：先以source/CLI红契约证明
`tools/LiveHostReadinessAudit/live_host_readiness_audit.py`仍默认选择F盘游戏根，且README仍把历史
`F:\AF测试重构`当当前project-local Stage。随后只让`--game-root`成为显式必填输入，保留repo-derived
project root，补纯fixture/CLI测试与文档；不得启动游戏、部署、读取真实存档或把readiness输出提升为LIVE。

## 新线程启动语

> 请读取 `G:\AFMOD\AF-REFACTOR\docs\handoffs\2026-09-02-shout-sse-replay-dependency-closure.md`，
> 在分支 `codex/af-main-refactor-continuation-20260831` 上继续。先fetch并核对Git/工作树，不pull、
> rebase或reset；确认HEAD至少包含`ae49e3c8`。按`LOCAL-7-C3`只修复LiveHostReadinessAudit的
> explicit-root portability：先红测F盘默认，再要求显式`--game-root`并用纯fixture/CLI契约验证。
> 不启动游戏、不部署、不读写真实存档、不改生产C#、不push；阶段7保持VERIFY、阶段8执行保持BLOCKED。
